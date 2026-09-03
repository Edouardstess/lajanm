import { Processor, WorkerHost } from '@nestjs/bullmq';
import { Job } from 'bullmq';
import { TOPUP_INITIATION_QUEUE, TopupService } from '../topup.service';

interface RetryInitiationJobData {
  transactionId: string;
  amountHTG: number;
}

@Processor(TOPUP_INITIATION_QUEUE)
export class TopupInitiationProcessor extends WorkerHost {
  constructor(private readonly topupService: TopupService) {
    super();
  }

  async process(job: Job<RetryInitiationJobData>): Promise<void> {
    const attempts = job.opts.attempts ?? 1;
    const isFinalAttempt = job.attemptsMade + 1 >= attempts;
    await this.topupService.retryInitiation(job.data.transactionId, job.data.amountHTG, isFinalAttempt);
  }
}
