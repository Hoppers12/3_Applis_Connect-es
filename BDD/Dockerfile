# Étape 1 : Image de build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copier les fichiers de projet et restaurer les dépendances
COPY *.csproj ./
RUN dotnet restore

# Copier tout le code source et exécuter le build
COPY . ./
RUN dotnet build "BDD.csproj" -c Debug -o /app/build /p:Platform="AnyCPU"

# Étape 2 : Image runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Copier les fichiers compilés depuis l'étape de build
COPY --from=build /app/build .

# Expose le port utilisé par l'application
EXPOSE 5225

# Commande pour démarrer l'application
ENTRYPOINT ["dotnet", "BDD.dll"]
