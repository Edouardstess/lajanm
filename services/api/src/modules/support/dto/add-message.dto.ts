import { IsString, MinLength } from 'class-validator';

export class AddMessageDto {
  @IsString()
  @MinLength(1)
  body: string;
}
