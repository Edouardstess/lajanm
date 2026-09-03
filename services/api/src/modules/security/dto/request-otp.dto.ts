import { IsEnum } from 'class-validator';
import { OtpPurpose } from '../entities/otp-code.entity';

export class RequestOtpDto {
  @IsEnum(OtpPurpose)
  purpose: OtpPurpose;
}
