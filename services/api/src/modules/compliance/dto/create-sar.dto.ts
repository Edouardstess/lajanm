import { ArrayMinSize, IsArray, IsString, IsUUID, MinLength } from 'class-validator';

export class CreateSarDto {
  @IsUUID()
  subjectUserId: string;

  @IsArray()
  @ArrayMinSize(1)
  @IsUUID('4', { each: true })
  relatedOperationIds: string[];

  @IsString()
  @MinLength(1)
  reason: string;
}
