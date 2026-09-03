import { IsEmail, IsOptional, IsString, MaxLength } from 'class-validator';

export class UpdateProfileDto {
  @IsOptional()
  @IsString()
  @MaxLength(128)
  fullName?: string;

  @IsOptional()
  @IsEmail()
  email?: string;
}
