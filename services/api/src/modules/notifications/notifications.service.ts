import { Injectable, Logger } from '@nestjs/common';

export interface NotificationEvent {
  type: 'wallet.credit' | 'wallet.debit' | 'topup.completed' | 'payout.completed';
  title: string;
  body: string;
}

/**
 * PLACEHOLDER — there is no push notification (Expo push tokens / FCM /
 * APNs) or SMS gateway integration in this environment. This service
 * exists so call sites (wallet transfers, top-up/payout completion) are
 * already wired to "notify the user" in the right place; swapping this
 * implementation for a real one is the only change needed once a push
 * provider and SMS gateway are chosen. Until then, this only logs — it
 * must never be mistaken for delivered notifications when reasoning about
 * what the product actually does today.
 */
@Injectable()
export class NotificationsService {
  private readonly logger = new Logger(NotificationsService.name);

  async notify(userId: string, event: NotificationEvent): Promise<void> {
    this.logger.log(`[stub] would notify user ${userId}: ${event.type} — ${event.title}`);
  }
}
