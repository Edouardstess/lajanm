import { IsBoolean, IsInt, IsOptional, IsString, MaxLength, MinLength } from 'class-validator';

export class CreateFaqDto {
  @IsString()
  @MaxLength(100)
  category: string;

  @IsString()
  @MaxLength(500)
  question: string;

  @IsString()
  @MinLength(1)
  answer: string;

  @IsOptional()
  @IsInt()
  sortOrder?: number;

  @IsOptional()
  @IsBoolean()
  isPublished?: boolean;
}
