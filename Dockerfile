# Build-Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build

RUN apt-get update
RUN apt-get install -y tzdata

ENV TZ=Europe/Berlin
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone

# Definiere ein Build-Argument für MODE_TYPE mit Standardwert
ARG MODE_TYPE=Production

# Setze MODE_TYPE als Umgebungsvariable für Build- und Runtime-Stage
ENV MODE_TYPE=$MODE_TYPE

WORKDIR /src

# Kopiere den gesamten Code ins Image
COPY . .

# Wiederherstelle NuGet-Pakete
RUN dotnet restore "FuelDistanceCalculator.sln"

# Baue die Solution
RUN dotnet build "FuelDistanceCalculator.sln" -c Release --no-restore

# Führe Tests nur aus, wenn MODE_TYPE=Development
RUN echo "MODE_TYPE in Build-Stage: $MODE_TYPE" && \
    if [ "$MODE_TYPE" = "Development" ]; then dotnet test "FuelDistanceCalculator.sln" -c Release --no-build --verbosity normal --logger "trx;LogFileName=/src/testresults.trx"; else echo "Tests übersprungen, MODE_TYPE ist $MODE_TYPE"; fi

# Baue die Anwendung
RUN dotnet publish "FuelDistanceCalculator/FuelDistanceCalculator.csproj" -c Release -o /app/publish --no-restore

# Runtime-Stage
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

# Setze MODE_TYPE für die Runtime-Stage (falls nicht schon übernommen)
ENV MODE_TYPE=$MODE_TYPE

# ENTRYPOINT ändern, um das Start-Skript zuerst auszuführen
ENTRYPOINT ["/bin/bash", "-c", "/app/start.sh && dotnet FuelDistanceCalculator.dll"]