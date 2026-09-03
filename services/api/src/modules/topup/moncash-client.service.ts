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

/**
 * Thin client around MonCash's payment API.
 *
 * IMPORTANT: this has not been exercised against a real MonCash sandbox —
 * this environment has no MonCash credentials or network access to
 * validate the request/response shape. The request/response contract
 * below follows the plan's architecture (initiate a payment, confirm via
 * a signed webhook) but MUST be verified against MonCash's actual sandbox
 * docs/responses before this ever runs against real money. Every failure
 * mode (bad response shape, timeout, non-2xx) is treated as
 * MonCashUnavailableError so the caller's retry path is exercised either
 * way — nothing here pretends a call succeeded that didn't.
 */
@Injectable()
export class MonCashClient {
  private readonly logger = new Logger(MonCashClient.name);

  constructor(private readonly config: ConfigService) {}

  async createPayment(orderId: string, amountHTG: number): Promise<CreatePaymentResult> {
    const baseUrl = this.config.get<string>('MONCASH_BASE_URL');
    const clientId = this.config.get<string>('MONCASH_CLIENT_ID');
    const clientSecret = this.config.get<string>('MONCASH_CLIENT_SECRET');

    if (!baseUrl || !clientId || !clientSecret) {
      throw new MonCashUnavailableError('MonCash credentials are not configured');
    }

    const controller = new AbortController();
    const timeout = setTimeout(() => controller.abort(), 10_000);

    try {
      const response = await fetch(`${baseUrl}/v1/CreatePayment`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Basic ${Buffer.from(`${clientId}:${clientSecret}`).toString('base64')}`,
        },
        body: JSON.stringify({ orderId, amount: amountHTG }),
        signal: controller.signal,
      });

      if (!response.ok) {
        throw new MonCashUnavailableError(`MonCash returned HTTP ${response.status}`);
      }

      const body = (await response.json()) as { reference?: string; gatewayUrl?: string };
      if (!body.reference || !body.gatewayUrl) {
        throw new MonCashUnavailableError('MonCash response missing reference/gatewayUrl');
      }

      return { reference: body.reference, gatewayUrl: body.gatewayUrl };
    } catch (error) {
      this.logger.warn(`MonCash createPayment failed for order ${orderId}: ${String(error)}`);
      throw new MonCashUnavailableError('MonCash is unavailable');
    } finally {
      clearTimeout(timeout);
    }
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
}
