import { IsString, Matches } from 'class-validator';

export class ChangePinDto {
  @IsString()
  @Matches(/^[0-9]{4,6}$/)
  currentPin: string;

  @IsString()
  @Matches(/^[0-9]{4,6}$/, { message: 'newPin must be 4 to 6 digits' })
  newPin: string;
}
