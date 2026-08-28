# Étape 1 : Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copier uniquement le projet
COPY Gite_Planning.fsproj ./
RUN dotnet restore

# Copier le reste du code (sans obj/bin)
COPY Controllers/ ./Controllers/
COPY Models/ ./Models/
COPY Views/ ./Views/
COPY wwwroot/ ./wwwroot/
COPY Program.fs ./
COPY appsettings*.json ./

RUN dotnet publish -c Release -o /app

# Étape 2 : Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Gite_Planning.dll"]
