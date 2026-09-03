import { IsEnum } from 'class-validator';
import { FraudFlagStatus } from '../entities/fraud-flag.entity';

export class ResolveFlagDto {
  @IsEnum([
    FraudFlagStatus.RESOLVED,
    FraudFlagStatus.CONFIRMED_SUSPECT,
    FraudFlagStatus.FALSE_POSITIVE,
  ])
  status: FraudFlagStatus;
}
