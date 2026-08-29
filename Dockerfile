```dockerfile
# Étape 1 : Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY . .

RUN dotnet restore
RUN dotnet publish -c Release -o /app


# Étape 2 : Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app

COPY --from=build /app .

# Render fournit le port via PORT.
# L'application écoute uniquement en HTTP.
ENV ASPNETCORE_URLS=http://0.0.0.0:10000
ENV ASPNETCORE_HTTP_PORTS=10000
ENV ASPNETCORE_HTTPS_PORTS=""

EXPOSE 10000

ENTRYPOINT ["dotnet", "Gite_Planning.dll"]
```

