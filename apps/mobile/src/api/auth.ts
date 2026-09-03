import { apiRequest } from './client';

export type UserTier = 'basic' | 'verified';

export interface AuthUser {
  id: string;
  phone: string;
  tier: UserTier;
  fullName: string | null;
  email: string | null;
}

export interface AuthResult {
  accessToken: string;
  user: AuthUser;
}

export function register(phone: string, pin: string) {
  return apiRequest<{ id: string; phone: string; tier: UserTier }>('/auth/register', {
    method: 'POST',
    body: { phone, pin },
  });
}

export function login(phone: string, pin: string, deviceId: string, deviceName?: string) {
  return apiRequest<AuthResult>('/auth/login', {
    method: 'POST',
    body: { phone, pin, deviceId, deviceName },
  });
}

export function changePin(currentPin: string, newPin: string) {
  return apiRequest<void>('/auth/pin', {
    method: 'PATCH',
    authenticated: true,
    body: { currentPin, newPin },
  });
}

export function updateProfile(fields: { fullName?: string; email?: string }) {
  return apiRequest<AuthUser>('/auth/profile', {
    method: 'PATCH',
    authenticated: true,
    body: fields,
  });
}

export interface DeviceSession {
  id: string;
  deviceId: string;
  deviceName: string | null;
  lastSeenAt: string;
  revokedAt: string | null;
}

export function listDevices() {
  return apiRequest<DeviceSession[]>('/auth/devices', { authenticated: true });
}

export function revokeDevice(deviceSessionId: string) {
  return apiRequest<void>(`/auth/devices/${deviceSessionId}`, {
    method: 'DELETE',
    authenticated: true,
  });
}
