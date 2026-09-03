import { Injectable, Logger } from '@nestjs/common';

/**
 * PLACEHOLDER — no SMS gateway is configured in this environment (same
 * gap as NotificationsService and push notifications). OTP codes are
 * logged, not delivered. This must not be mistaken for a working OTP
 * delivery path when reasoning about what actually ships today; swapping
 * in a real SMS provider (Haiti-compatible gateway, per the product plan)
 * is the only change needed at the call site in SecurityService.
 */
@Injectable()
export class SmsService {
  private readonly logger = new Logger(SmsService.name);

  async sendOtp(phone: string, code: string): Promise<void> {
    this.logger.log(`[stub] would SMS OTP ${code} to ${phone}`);
  }
}
