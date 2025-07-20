# Verwende das .NET SDK-Image zum Bauen der Anwendung
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

RUN apt-get update
RUN apt-get install -y tzdata

ENV TZ=Europe/Berlin
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone

WORKDIR /src

# Kopiere den gesamten Code ins Image
COPY . .

# Wiederherstelle NuGet-Pakete
RUN dotnet restore "FuelDistanceCalculator/FuelDistanceCalculator.csproj"

# Baue die Anwendung
RUN dotnet publish "FuelDistanceCalculator/FuelDistanceCalculator.csproj" -c Release -o /app/publish --restore

# Verwende das .NET Runtime-Image für die finale App
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base

WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Installiere PostgreSQL-Client im Haupt-Container
RUN apt-get update && apt-get install -y postgresql-client

# Kopiere die gebaute Anwendung aus dem Build-Container
COPY --from=build /app/publish .

# Kopiere das Start-Skript in den Container
COPY start.sh /app/start.sh
COPY create_tables.sql /app/create_tables.sql

# Setze die Berechtigungen für das Skript
RUN chmod +x /app/start.sh

# Setze die Umgebungsvariable für den Redis-Host
ENV REDIS_HOST=redis:6379

# ENTRYPOINT ändern, um das Start-Skript zuerst auszuführen
ENTRYPOINT ["/bin/bash", "-c", "/app/start.sh && dotnet FuelDistanceCalculator.dll"]
