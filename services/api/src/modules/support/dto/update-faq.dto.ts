import { IsBoolean, IsInt, IsOptional, IsString, MaxLength, MinLength } from 'class-validator';

export class UpdateFaqDto {
  @IsOptional()
  @IsString()
  @MaxLength(100)
  category?: string;

  @IsOptional()
  @IsString()
  @MaxLength(500)
  question?: string;

  @IsOptional()
  @IsString()
  @MinLength(1)
  answer?: string;

  @IsOptional()
  @IsInt()
  sortOrder?: number;

  @IsOptional()
  @IsBoolean()
  isPublished?: boolean;
}
