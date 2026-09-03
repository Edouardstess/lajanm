import { IsString, Matches } from 'class-validator';

export class RegisterDto {
  @IsString()
  @Matches(/^\+?[0-9]{8,15}$/, { message: 'phone must be a valid phone number' })
  phone: string;

  @IsString()
  @Matches(/^[0-9]{4,6}$/, { message: 'pin must be 4 to 6 digits' })
  pin: string;
}
