# Utiliser l'image SDK pour compiler et exécuter l'application
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copier le projet et tout le code source
COPY . .

# Restaurer les dépendances et publier l'application
RUN dotnet publish "BDD.csproj" -c Release -o /publish

# Utiliser l'image runtime pour exécuter l'application
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app

# Copier les fichiers publiés depuis l'étape précédente
COPY --from=build /publish .

# Exposer le port utilisé par l'application
EXPOSE 5225

# Commande pour démarrer l'application
ENTRYPOINT ["dotnet", "BDD.dll"]
