# Fichiers déposés par les clients

Ce document décrit ce qui est contrôlé sur un fichier reçu, dans quel
ordre, et pourquoi. Le code est dans
`services/api/src/modules/uploads/file-security.ts`, ses tests dans
`file-security.spec.ts`.

## Le principe

**On ne fait confiance ni au nom du fichier, ni à son extension, ni au
`Content-Type` annoncé.** Ces trois valeurs viennent du client, donc d'un
attaquant potentiel. Seuls les octets décident.

## Ce qui est vérifié, dans l'ordre

| # | Contrôle | Refus type |
|---|---|---|
| 1 | Taille ≤ 8 Mio | rejeté par multer avant même d'arriver en mémoire (413) |
| 2 | Taille ≥ 1 Kio | aucune photo exploitable en dessous |
| 3 | Extension dans `.jpg .jpeg .png .webp` | `.pdf`, `.svg`, `.zip`, `.php`, double extension `photo.jpg.php` |
| 4 | `Content-Type` dans `image/jpeg image/png image/webp` | `application/pdf` |
| 5 | Les premiers octets ne sont pas un format hostile connu | HTML, `<script`, SVG, XML, PHP, `#!`, ZIP, ELF, exécutable Windows |
| 6 | Les octets correspondent à un format d'image accepté | fichier qui n'est aucun des trois |
| 7 | Le format réel correspond à l'extension | JPEG nommé `.png` |
| 8 | Le format réel correspond au `Content-Type` | PNG annoncé `image/jpeg` |

Les contrôles 5 à 8 sont ceux qui comptent. Un `.jpg` dont les octets
disent « HTML » est un fichier HTML, quoi qu'en dise son nom.

**Rien de refusé ne touche le disque.** Le stockage est en mémoire
(`FileInterceptor`), l'inspection passe d'abord, l'écriture ensuite.

## Ce qui est écrit

- Nom **régénéré** : `<uuid><extension validée>`. Aucun octet fourni par
  le client n'entre dans un chemin de fichier — la traversée de
  répertoire est donc structurellement impossible, pas filtrée.
- Permissions `0600`, répertoire `0700`. Ce sont des pièces d'identité.
- Répertoire `UPLOAD_DIR`, **hors de toute racine servie en statique**.
  Ces fichiers ne sont jamais rendus par URL.
- Le nom d'origine choisi par le client n'est pas conservé : il ne sert à
  rien et tout ce qui le manipulerait serait une surface d'attaque.
- Un SHA-256 est stocké et **revérifié à la relecture** : si le contenu du
  disque a changé, la lecture échoue au lieu de servir autre chose.

## Qui peut faire quoi

- Déposer : tout utilisateur authentifié, **20 fichiers par heure**
  (compteur Redis, voir `common/rate-limit.service.ts`).
- Rattacher à un dossier KYC : uniquement le propriétaire des fichiers.
  Un identifiant appartenant à quelqu'un d'autre renvoie **404 et non
  403** — distinguer « n'existe pas » de « pas à vous » indiquerait quels
  identifiants sont valides.
- La pièce d'identité et le selfi doivent être deux fichiers différents.

## Le contrôle côté application mobile

`apps/mobile/src/api/client.ts` refuse les extensions inconnues avant
l'envoi. **Ce n'est pas une mesure de sécurité** — c'est trivial à
contourner. Il évite seulement de faire monter huit mégaoctets sur un
réseau EDGE pour se faire refuser à l'arrivée.

## Limite connue

`UPLOAD_DIR` est un disque local. Sur Render en plan gratuit il est
éphémère : les pièces disparaissent au redéploiement. Acceptable en test,
**jamais en production** — il faut un stockage objet avant d'instruire de
vrais dossiers KYC. Voir `docs/release.md`.
