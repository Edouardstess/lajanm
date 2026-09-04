# syntax=docker/dockerfile:1
FROM node:20-alpine AS build
WORKDIR /repo
COPY package.json package-lock.json ./
COPY services/api/package.json services/api/package.json
RUN npm ci --no-audit --no-fund
COPY services/api services/api
RUN npm run build -w @lajanm/api

FROM node:20-alpine AS runtime
ENV NODE_ENV=production
WORKDIR /repo
COPY package.json package-lock.json ./
COPY services/api/package.json services/api/package.json
RUN npm ci --omit=dev --no-audit --no-fund -w @lajanm/api
COPY --from=build /repo/services/api/dist services/api/dist

# Les migrations doivent pouvoir tourner depuis CETTE image, avant que
# l'API ne démarre. C'est pourquoi le script de production vise le
# data-source compilé (dist/database/data-source.js) et le CLI `typeorm`,
# qui est une dépendance de production : `npm run migration:run` passe par
# typeorm-ts-node-commonjs et src/*.ts, tous deux absents ici après
# --omit=dev. Voir migration:run:prod dans services/api/package.json.

# Un service financier ne tourne pas en root. L'image node fournit déjà un
# utilisateur `node` non privilégié ; il n'a besoin d'aucun droit d'écriture
# sur le code.
USER node

WORKDIR /repo/services/api
EXPOSE 3000

# Permet à l'orchestrateur de ne router du trafic que vers une instance
# réellement prête, et de redémarrer celles qui ne répondent plus.
HEALTHCHECK --interval=30s --timeout=5s --start-period=20s --retries=3 \
  CMD node -e "require('http').get('http://127.0.0.1:'+(process.env.PORT||3000)+'/health',r=>process.exit(r.statusCode===200?0:1)).on('error',()=>process.exit(1))"

# Migrations puis démarrage, en forme exec (tableau JSON) : c'est le
# Dockerfile qui porte la commande, et non le champ dockerCommand de
# render.yaml, dont les guillemets ont été conservés littéralement par
# l'hébergeur — sh recevait alors toute la ligne comme un unique nom de
# commande et sortait en 127 (« not found »).
CMD ["sh", "-c", "npm run migration:run:prod && node dist/main.js"]
