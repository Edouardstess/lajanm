#!/usr/bin/env bash
#
# Crée la clé de signature Android de Lajan'm.
#
# ┌────────────────────────────────────────────────────────────────────┐
# │ À EXÉCUTER SUR VOTRE MACHINE, PAS DANS UNE SESSION PARTAGÉE.       │
# │                                                                    │
# │ Cette clé est le fondement de la confiance qu'Android accorde aux  │
# │ mises à jour de l'application. Qui la détient peut publier une     │
# │ version que les téléphones de vos clients installeront comme       │
# │ authentique — sur un portefeuille, c'est tout le modèle de         │
# │ sécurité. Elle ne doit donc jamais transiter par une conversation, │
# │ un ticket, un e-mail, ni le dépôt Git.                             │
# │                                                                    │
# │ Elle est aussi IRREMPLAÇABLE : sans elle, plus aucune mise à jour  │
# │ n'est possible et il faut republier sous un nouvel identifiant,    │
# │ en perdant les installations et les avis.                          │
# └────────────────────────────────────────────────────────────────────┘
#
#   bash scripts/create-android-keystore.sh
#
set -euo pipefail

KEYSTORE="${1:-lajanm-release.keystore}"
ALIAS="${2:-lajanm}"

command -v keytool >/dev/null 2>&1 || {
  echo "keytool est introuvable. Installez un JDK 17 (Temurin, Zulu, ou celui d'Android Studio)." >&2
  exit 1
}

[ -e "$KEYSTORE" ] && {
  echo "$KEYSTORE existe déjà. Refus de l'écraser : ce serait perdre la clé." >&2
  exit 1
}

# Mot de passe tiré au hasard plutôt que choisi : un mot de passe qu'on
# retient est un mot de passe qu'on devine.
PASSWORD="$(LC_ALL=C tr -dc 'A-Za-z0-9' < /dev/urandom | head -c 40)"

# Validité de 10 000 jours (~27 ans) : Google exige que la clé reste
# valide jusqu'au 22 octobre 2033 au minimum.
keytool -genkeypair -v \
  -keystore "$KEYSTORE" \
  -alias "$ALIAS" \
  -keyalg RSA -keysize 4096 -validity 10000 \
  -storepass "$PASSWORD" -keypass "$PASSWORD" \
  -dname "CN=Lajan'm, OU=Mobile, O=Lajan'm, L=Port-au-Prince, C=HT" >/dev/null

cat <<EOF

Clé créée : $KEYSTORE

1) SAUVEGARDEZ CE FICHIER ET CE MOT DE PASSE hors de la machine
   (gestionnaire de mots de passe, coffre chiffré). Perdus, ils ne se
   régénèrent pas.

2) Créez trois secrets dans GitHub :
   Settings > Secrets and variables > Actions > New repository secret

   ANDROID_KEYSTORE_BASE64
$(base64 -w0 "$KEYSTORE" 2>/dev/null || base64 -i "$KEYSTORE")

   ANDROID_KEYSTORE_PASSWORD
$PASSWORD

   ANDROID_KEY_ALIAS
$ALIAS

3) Effacez la sortie de ce terminal une fois les secrets créés.

À partir de là, chaque APK produit par le workflow porte la même
signature, et les mises à jour s'installent par-dessus la précédente
sans désinstallation.
EOF
