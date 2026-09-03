import { IsString, MinLength } from 'class-validator';

/**
 * idDocumentUrl/selfieUrl are meant to be object-storage URLs, but no
 * upload service exists yet in this MVP (see docs/architecture.md — object
 * storage is a separate infra task). Until then this accepts any non-empty
 * string reference rather than pretending uploads are already wired with a
 * strict URL format the client can't actually produce.
 */
export class SubmitKycDto {
  @IsString()
  @MinLength(1)
  idDocumentUrl: string;

  @IsString()
  @MinLength(1)
  selfieUrl: string;
}
