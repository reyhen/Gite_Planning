# Étape 1 : Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copier uniquement le projet
COPY Gite_Planning/*.fsproj ./Gite_Planning/
RUN dotnet restore Gite_Planning/Gite_Planning.fsproj

# Copier le reste du code
COPY Gite_Planning/. ./Gite_Planning/

RUN dotnet publish Gite_Planning/Gite_Planning.fsproj -c Release -o /app

# Étape 2 : Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "Gite_Planning.dll"]


