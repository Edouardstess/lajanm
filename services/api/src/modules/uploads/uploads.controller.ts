import {
  Controller,
  Post,
  UploadedFile as UploadedFileParam,
  UseGuards,
  UseInterceptors,
} from '@nestjs/common';
import { FileInterceptor } from '@nestjs/platform-express';
import { CurrentUser } from '../auth/decorators/current-user.decorator';
import { JwtAuthGuard } from '../auth/guards/jwt-auth.guard';
import { MAX_FILE_BYTES } from './file-security';
import { UploadsService } from './uploads.service';

@UseGuards(JwtAuthGuard)
@Controller('uploads')
export class UploadsController {
  constructor(private readonly uploadsService: UploadsService) {}

  /**
   * Dépôt d'une pièce pour la vérification d'identité.
   *
   * `limits.fileSize` fait rejeter le flux par multer dès que le plafond
   * est franchi, sans attendre la fin du transfert : sans cela, un client
   * pourrait faire allouer 500 Mo en mémoire avant que le contrôle de
   * taille ne s'exécute. `files: 1` interdit d'en glisser plusieurs dans
   * la même requête.
   *
   * Le stockage est en mémoire et non sur disque : rien ne doit être
   * écrit avant d'avoir été inspecté.
   */
  @Post('kyc')
  @UseInterceptors(
    FileInterceptor('file', {
      limits: { fileSize: MAX_FILE_BYTES, files: 1, fields: 0 },
    }),
  )
  upload(
    @CurrentUser() user: { id: string },
    @UploadedFileParam() file: Express.Multer.File | undefined,
  ) {
    return this.uploadsService.accept(user.id, file);
  }
}
