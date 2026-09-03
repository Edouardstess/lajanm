import Constants from 'expo-constants';
import * as SecureStore from 'expo-secure-store';

const TOKEN_KEY = 'lajanm.accessToken';

const API_BASE_URL =
  (Constants.expoConfig?.extra?.apiBaseUrl as string | undefined) ?? 'http://localhost:3000';

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    message: string,
  ) {
    super(message);
  }
}

export async function getToken(): Promise<string | null> {
  return SecureStore.getItemAsync(TOKEN_KEY);
}

export async function setToken(token: string | null): Promise<void> {
  if (token) {
    await SecureStore.setItemAsync(TOKEN_KEY, token);
  } else {
    await SecureStore.deleteItemAsync(TOKEN_KEY);
  }
}

/**
 * Hard ceiling on how long a request may hang. The target network is 2G/
 * EDGE, where fetch without a timeout can wait effectively forever: the
 * user is left on a spinner with no way to tell a slow request from a dead
 * one. 20s is generous for a slow-but-working connection while still
 * failing in a timeframe a person will wait through.
 */
const REQUEST_TIMEOUT_MS = 20_000;

/**
 * Thrown when a request never got an answer — timed out, or the device is
 * offline. Deliberately NOT an ApiError: the distinction is what lets a
 * screen say "not confirmed, try again" instead of claiming a definite
 * outcome. For a write, this is the genuinely ambiguous case — the request
 * may or may not have reached the server.
 */
export class NetworkError extends Error {
  constructor(public readonly timedOut: boolean) {
    super(timedOut ? 'Request timed out' : 'Network unavailable');
  }
}

interface RequestOptions {
  method?: 'GET' | 'POST' | 'PATCH' | 'DELETE';
  body?: unknown;
  authenticated?: boolean;
  timeoutMs?: number;
}

/**
 * Thin fetch wrapper: attaches the bearer token when requested, and turns
 * any non-2xx response into an ApiError carrying the server's message —
 * screens show that message (or a translated generic fallback) rather than
 * raw network/parsing errors, per the "no technical jargon" requirement.
 */
export async function apiRequest<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const headers: Record<string, string> = { 'Content-Type': 'application/json' };

  if (options.authenticated) {
    const token = await getToken();
    if (token) headers.Authorization = `Bearer ${token}`;
  }

  const controller = new AbortController();
  const timeoutMs = options.timeoutMs ?? REQUEST_TIMEOUT_MS;
  const timer = setTimeout(() => controller.abort(), timeoutMs);

  let response: Response;
  try {
    response = await fetch(`${API_BASE_URL}${path}`, {
      method: options.method ?? 'GET',
      headers,
      body: options.body ? JSON.stringify(options.body) : undefined,
      signal: controller.signal,
    });
  } catch (error) {
    // An abort here is our own timer firing; anything else is the device
    // being unable to reach the server at all. Both mean "no answer".
    throw new NetworkError((error as Error)?.name === 'AbortError');
  } finally {
    clearTimeout(timer);
  }

  const text = await response.text();

  // A gateway or captive portal can answer with HTML rather than JSON. That
  // is still a real HTTP response, so it must surface as an ApiError with
  // its status — not as a JSON.parse crash that screens would misread as a
  // connectivity problem.
  let data: { message?: string | string[] } | undefined;
  try {
    data = text ? JSON.parse(text) : undefined;
  } catch {
    if (!response.ok) throw new ApiError(response.status, `Request failed with status ${response.status}`);
    throw new ApiError(response.status, 'Unexpected response from the server');
  }

  if (!response.ok) {
    const message = data?.message ?? `Request failed with status ${response.status}`;
    throw new ApiError(response.status, Array.isArray(message) ? message.join(', ') : message);
  }

  return data as T;
}
