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
WORKDIR /repo/services/api
EXPOSE 3000
CMD ["node", "dist/main.js"]
