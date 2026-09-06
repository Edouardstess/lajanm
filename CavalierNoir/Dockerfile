# ---------------------------------------------------------------------------
# Étape 1 — restauration et compilation
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Les fichiers projet sont copiés d'abord : la couche de restauration NuGet est
# alors réutilisée tant que les dépendances ne changent pas.
COPY Directory.Build.props Directory.Packages.props ./
COPY src/CavalierNoir.Domain/CavalierNoir.Domain.csproj src/CavalierNoir.Domain/
COPY src/CavalierNoir.Application/CavalierNoir.Application.csproj src/CavalierNoir.Application/
COPY src/CavalierNoir.Infrastructure/CavalierNoir.Infrastructure.csproj src/CavalierNoir.Infrastructure/
COPY src/CavalierNoir.Web/CavalierNoir.Web.csproj src/CavalierNoir.Web/
COPY tests/CavalierNoir.Tests.Unit/CavalierNoir.Tests.Unit.csproj tests/CavalierNoir.Tests.Unit/
RUN dotnet restore src/CavalierNoir.Web/CavalierNoir.Web.csproj

COPY . .
RUN dotnet publish src/CavalierNoir.Web/CavalierNoir.Web.csproj \
        --configuration Release \
        --no-restore \
        --output /app/publish \
        /p:UseAppHost=false

# ---------------------------------------------------------------------------
# Étape 2 — image d'exécution
# ---------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Les données de fuseau horaire sont nécessaires : l'exercice quotidien est
# planifié à 6 h, heure de Port-au-Prince.
RUN apt-get update \
    && apt-get install -y --no-install-recommends tzdata curl \
    && rm -rf /var/lib/apt/lists/*

# L'application ne tourne pas en root.
RUN groupadd --system cavaliernoir \
    && useradd --system --gid cavaliernoir --create-home cavaliernoir

COPY --from=build /app/publish .

# Répertoires inscriptibles : base SQLite, fichiers téléversés, courriels de test.
RUN mkdir -p /app/App_Data /app/wwwroot/uploads \
    && chown -R cavaliernoir:cavaliernoir /app

USER cavaliernoir

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_RUNNING_IN_CONTAINER=true \
    TZ=America/Port-au-Prince

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=5s --start-period=40s --retries=3 \
    CMD curl --fail --silent http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "CavalierNoir.Web.dll"]
