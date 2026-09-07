# Image du service web d'orientation scolaire.
#
# Le contexte de build est le dossier projet_orientation/ : l'image a besoin
# du modèle sérialisé (model/) ET des modules config.py et preprocessing.py,
# car le pipeline sauvegardé référence la classe NettoyeurValeurs définie
# dans preprocessing.py. Sans ce fichier, joblib.load échoue.
#
# Construction et exécution en local :
#   cd projet_orientation
#   docker build -f web/Dockerfile -t orientation .
#   docker run --rm -p 8000:8000 orientation

FROM python:3.11-slim

ENV PYTHONUNBUFFERED=1 \
    PYTHONDONTWRITEBYTECODE=1 \
    PIP_NO_CACHE_DIR=1

WORKDIR /app

# Les dépendances d'abord : cette couche est mise en cache tant que
# requirements.txt ne change pas.
COPY web/requirements.txt web/requirements.txt
RUN pip install --no-cache-dir -r web/requirements.txt

# Modules partagés avec le projet, indispensables au rechargement du modèle.
COPY config.py preprocessing.py ./

# Le modèle entraîné et ses métadonnées.
COPY model/modele_orientation.joblib model/metadonnees_modele.json model/

# Le service lui-même.
COPY web/serveur.py web/page.html web/

# Utilisateur non privilégié : le service n'a besoin d'aucun droit d'écriture.
RUN useradd --create-home --uid 10001 service && chown -R service:service /app
USER service

EXPOSE 8000

# Vérifie que le modèle est chargé, pas seulement que le processus tourne :
# /health répond 503 tant que le pipeline n'est pas en mémoire.
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
    CMD python -c "import os,sys,urllib.request; \
port=os.environ.get('PORT','8000'); \
sys.exit(0 if urllib.request.urlopen(f'http://127.0.0.1:{port}/health').status==200 else 1)"

# Forme shell pour que $PORT, fourni par Render, soit bien substitué.
CMD uvicorn web.serveur:app --host 0.0.0.0 --port ${PORT:-8000}
