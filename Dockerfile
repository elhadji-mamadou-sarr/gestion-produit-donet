# ── Stage 1 : Build ────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copier uniquement le csproj d'abord → layer cache optimisé
COPY GestionProduits.Api.csproj ./
RUN dotnet restore

# Copier le reste et publier en mode Release
COPY . ./
RUN dotnet publish -c Release -o /app/publish

# ── Stage 2 : Runtime ──────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Créer le dossier logs avec les bons droits
RUN mkdir -p /app/logs && chmod 755 /app/logs

COPY --from=build /app/publish ./

# Port exposé (HTTP uniquement en dev Docker)
EXPOSE 8080

# Variable d'environnement pour indiquer à ASP.NET Core d'écouter sur 8080
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development

ENTRYPOINT ["dotnet", "GestionProduits.Api.dll"]