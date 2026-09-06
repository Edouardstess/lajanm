import { BadRequestException } from '@nestjs/common';
import { formatPhone, normalizePhone } from './phone';

describe('normalizePhone', () => {
  it.each([
    ['+50939000001', '+50939000001'],
    ['+509 39 00 00 01', '+50939000001'],
    ['+509-39-00-00-01', '+50939000001'],
    ['0050939000001', '+50939000001'],
    ['50939000001', '+50939000001'],
    ['39000001', '+50939000001'],
    ['39 00 00 01', '+50939000001'],
  ])('ramène %s à %s', (input, expected) => {
    expect(normalizePhone(input)).toBe(expected);
  });

  // Régression : « 00 » en tête d'un numéro national de 8 chiffres n'est
  // pas un préfixe international. Le couper donnait « +509000002 », un
  // numéro qui n'existe pas — et l'argent serait parti nulle part.
  it('ne confond pas un numéro national commençant par 00 avec un préfixe international', () => {
    expect(normalizePhone('00000002')).toBe('+50900000002');
    expect(normalizePhone('0050900000002')).toBe('+50900000002');
  });

  it('conserve un numéro étranger explicitement international', () => {
    expect(normalizePhone('+33 6 12 34 56 78')).toBe('+33612345678');
  });

  // Le cœur du défaut corrigé : ces trois saisies désignaient trois
  // comptes différents alors qu'il s'agit d'une seule personne.
  it('fait converger les saisies d’un même numéro', () => {
    const forms = ['+50939000001', '50939000001', '39000001', '+509 39-00-00-01'];
    const canonical = forms.map(normalizePhone);
    expect(new Set(canonical).size).toBe(1);
  });

  it.each([
    ['', 'vide'],
    ['abc', 'sans chiffre'],
    ['123', 'trop court'],
    ['612345678', 'étranger sans +'],
    ['1234567890123456', 'trop long'],
  ])('refuse %s (%s)', (input) => {
    expect(() => normalizePhone(input)).toThrow(BadRequestException);
  });
});

describe('formatPhone', () => {
  it('découpe un numéro haïtien', () => {
    expect(formatPhone('+50939000001')).toBe('+509 39 00 00 01');
  });

  it('laisse intact un indicatif dont on ignore le découpage', () => {
    expect(formatPhone('+33612345678')).toBe('+33612345678');
  });
});
