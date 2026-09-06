import { BadRequestException } from '@nestjs/common';
import { inspectUpload, MAX_FILE_BYTES, MIN_FILE_BYTES } from './file-security';

/** Un JPEG plausible : signature correcte, puis du remplissage. */
function jpeg(size = MIN_FILE_BYTES + 10): Buffer {
  const b = Buffer.alloc(size, 0x20);
  Buffer.from([0xff, 0xd8, 0xff, 0xe0]).copy(b, 0);
  return b;
}

function png(size = MIN_FILE_BYTES + 10): Buffer {
  const b = Buffer.alloc(size, 0x20);
  Buffer.from([0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a]).copy(b, 0);
  return b;
}

function webp(size = MIN_FILE_BYTES + 10): Buffer {
  const b = Buffer.alloc(size, 0x20);
  b.write('RIFF', 0, 'ascii');
  b.write('WEBP', 8, 'ascii');
  return b;
}

/** Le fichier hostile type : une extension d'image, du HTML dedans. */
function html(size = MIN_FILE_BYTES + 10): Buffer {
  const b = Buffer.alloc(size, 0x20);
  b.write('<!DOCTYPE html><script>alert(1)</script>', 0, 'ascii');
  return b;
}

describe('inspectUpload', () => {
  describe('accepte', () => {
    it.each([
      ['photo.jpg', 'image/jpeg', jpeg(), 'jpeg'],
      ['photo.JPEG', 'image/jpeg', jpeg(), 'jpeg'],
      ['carte.png', 'image/png', png(), 'png'],
      ['selfi.webp', 'image/webp', webp(), 'webp'],
    ])('%s annoncé %s', (name, mime, buffer, format) => {
      expect(inspectUpload(name, mime, buffer)).toMatchObject({ format });
    });

    it('tolère un Content-Type avec paramètre', () => {
      expect(inspectUpload('a.jpg', 'image/jpeg; charset=binary', jpeg())).toMatchObject({
        format: 'jpeg',
      });
    });
  });

  describe('refuse sur l’extension', () => {
    it.each([
      ['script.svg', 'image/jpeg'],
      ['archive.zip', 'image/jpeg'],
      ['payload.php', 'image/jpeg'],
      ['sans-extension', 'image/jpeg'],
      ['fini-par-un-point.', 'image/jpeg'],
    ])('%s', (name, mime) => {
      expect(() => inspectUpload(name, mime, jpeg())).toThrow(BadRequestException);
    });

    // Une double extension ne trompe pas : seul le dernier point compte.
    it('lit la DERNIÈRE extension, pas la première', () => {
      expect(() => inspectUpload('photo.jpg.php', 'image/jpeg', jpeg())).toThrow(
        BadRequestException,
      );
    });

    // Le nom peut contenir n'importe quoi ; il n'est jamais utilisé pour
    // écrire sur le disque, et l'extension est lue sur le dernier segment.
    it('ignore une tentative de traversée de répertoire dans le nom', () => {
      expect(inspectUpload('../../../etc/passwd.jpg', 'image/jpeg', jpeg())).toMatchObject({
        format: 'jpeg',
      });
    });
  });

  describe('refuse sur le contenu réel', () => {
    // LE cas qui justifie tout ce fichier.
    it('rejette du HTML déguisé en .jpg', () => {
      expect(() => inspectUpload('innocent.jpg', 'image/jpeg', html())).toThrow(
        /looks like HTML/,
      );
    });

    it.each([
      ['<?php echo 1; ?>', 'PHP'],
      ['#!/bin/sh\nrm -rf /', 'shell'],
      ['<svg xmlns="http://www.w3.org/2000/svg"><script/></svg>', 'SVG'],
    ])('rejette %s', (content) => {
      const b = Buffer.alloc(MIN_FILE_BYTES + 10, 0x20);
      b.write(content, 0, 'ascii');
      expect(() => inspectUpload('image.jpg', 'image/jpeg', b)).toThrow(BadRequestException);
    });

    it.each([
      [Buffer.from([0x50, 0x4b, 0x03, 0x04]), 'ZIP'],
      [Buffer.from([0x7f, 0x45, 0x4c, 0x46]), 'ELF'],
      [Buffer.from('MZ', 'ascii'), 'PE Windows'],
    ])('rejette un %s renommé en image', (magic) => {
      const b = Buffer.alloc(MIN_FILE_BYTES + 10, 0x20);
      magic.copy(b, 0);
      expect(() => inspectUpload('image.png', 'image/png', b)).toThrow(BadRequestException);
    });

    it('rejette des octets qui ne sont aucun format connu', () => {
      const b = Buffer.alloc(MIN_FILE_BYTES + 10, 0x41);
      expect(() => inspectUpload('image.jpg', 'image/jpeg', b)).toThrow(
        /not a JPEG, PNG or WebP/,
      );
    });
  });

  describe('refuse une incohérence entre le nom et le contenu', () => {
    it('rejette un JPEG nommé .png', () => {
      expect(() => inspectUpload('image.png', 'image/png', jpeg())).toThrow(
        /content is JPEG but the name says "\.png"/,
      );
    });

    it('rejette un PNG annoncé image/jpeg', () => {
      expect(() => inspectUpload('image.png', 'image/jpeg', png())).toThrow(BadRequestException);
    });
  });

  describe('refuse sur la taille', () => {
    it('rejette au-delà du plafond', () => {
      expect(() => inspectUpload('gros.jpg', 'image/jpeg', jpeg(MAX_FILE_BYTES + 1))).toThrow(
        /larger than/,
      );
    });

    it('rejette en dessous du plancher', () => {
      expect(() => inspectUpload('minus.jpg', 'image/jpeg', jpeg(MIN_FILE_BYTES - 1))).toThrow(
        /too small/,
      );
    });
  });
});
