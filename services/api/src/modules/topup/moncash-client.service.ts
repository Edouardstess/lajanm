import { Injectable, Logger } from '@nestjs/common';
import { ConfigService } from '@nestjs/config';
import { createHmac, timingSafeEqual } from 'crypto';

export class MonCashUnavailableError extends Error {}

export interface CreatePaymentResult {
  /** MonCash's own reference for this transaction — becomes the ledger idempotency key. */
  reference: string;
  /** URL the client redirects to in order to complete payment on MonCash's side. */
  gatewayUrl: string;
}

export interface CreatePayoutResult {
  /** MonCash's own reference for this disbursement. */
  reference: string;
}

/**
 * Thin client around MonCash's payment API.
 *
 * IMPORTANT: this has not been exercised against a real MonCash sandbox —
 * this environment has no MonCash credentials or network access to
 * validate the request/response shape. The request/response contracts
 * below follow the plan's architecture but MUST be verified against
 * MonCash's actual sandbox docs/responses before this ever runs against
 * real money. Every failure mode (bad response shape, timeout, non-2xx)
 * is treated as MonCashUnavailableError so the caller's fallback path is
 * exercised either way — nothing here pretends a call succeeded that
 * didn't.
 *
 * createPayout is modeled as synchronous (MonCash confirms or rejects the
 * disbursement in the same HTTP response) — unlike createPayment, which
 * only initiates a payment later confirmed by webhook. This asymmetry is
 * a documented assumption, not a verified fact about MonCash's real API.
 */
@Injectable()
export class MonCashClient {
  private readonly logger = new Logger(MonCashClient.name);

  constructor(private readonly config: ConfigService) {}

  async createPayment(orderId: string, amountHTG: number): Promise<CreatePaymentResult> {
    const body = await this.post<{ reference?: string; gatewayUrl?: string }>('/v1/CreatePayment', {
      orderId,
      amount: amountHTG,
    });
    if (!body.reference || !body.gatewayUrl) {
      throw new MonCashUnavailableError('MonCash response missing reference/gatewayUrl');
    }
    return { reference: body.reference, gatewayUrl: body.gatewayUrl };
  }

  async createPayout(orderId: string, amountHTG: number, recipientPhone: string): Promise<CreatePayoutResult> {
    const body = await this.post<{ reference?: string }>('/v1/CreatePayout', {
      orderId,
      amount: amountHTG,
      receiver: recipientPhone,
    });
    if (!body.reference) {
      throw new MonCashUnavailableError('MonCash response missing reference');
    }
    return { reference: body.reference };
  }

  /**
   * Verifies the webhook's HMAC-SHA256 signature before any payload field
   * is trusted. A webhook whose signature doesn't check out is dropped
   * unconditionally, never processed "just in case" — see MonCash
   * integration note in docs/topup.md on why replay/forgery matters here.
   */
  verifyWebhookSignature(rawBody: string, signatureHeader: string | undefined): boolean {
    const secret = this.config.get<string>('MONCASH_WEBHOOK_SECRET');
    if (!secret || !signatureHeader) return false;

    const expected = createHmac('sha256', secret).update(rawBody).digest('hex');
    const expectedBuffer = Buffer.from(expected, 'hex');
    const providedBuffer = Buffer.from(signatureHeader, 'hex');

    if (expectedBuffer.length !== providedBuffer.length) return false;
    return timingSafeEqual(expectedBuffer, providedBuffer);
  }

  private async post<T>(path: string, payload: unknown): Promise<T> {
    const baseUrl = this.config.get<string>('MONCASH_BASE_URL');
    const clientId = this.config.get<string>('MONCASH_CLIENT_ID');
    const clientSecret = this.config.get<string>('MONCASH_CLIENT_SECRET');

    if (!baseUrl || !clientId || !clientSecret) {
      throw new MonCashUnavailableError('MonCash credentials are not configured');
    }

    const controller = new AbortController();
    const timeout = setTimeout(() => controller.abort(), 10_000);

    try {
      const response = await fetch(`${baseUrl}${path}`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Basic ${Buffer.from(`${clientId}:${clientSecret}`).toString('base64')}`,
        },
        body: JSON.stringify(payload),
        signal: controller.signal,
      });

      if (!response.ok) {
        throw new MonCashUnavailableError(`MonCash returned HTTP ${response.status}`);
      }

      return (await response.json()) as T;
    } catch (error) {
      this.logger.warn(`MonCash ${path} failed: ${String(error)}`);
      throw new MonCashUnavailableError('MonCash is unavailable');
    } finally {
      clearTimeout(timeout);
    }
  }
}
