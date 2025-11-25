# ----------------------------------------------------
# 1. BUILD-STAGE: Kompilieren und Veröffentlichen
# ----------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG MODE_TYPE=Development
ENV MODE_TYPE=$MODE_TYPE

# Installation von tzdata und Setzen der Zeitzone in einem Schritt
RUN apt-get update && \
    apt-get install -y tzdata && \
    rm /etc/localtime && \
    ln -sf /usr/share/zoneinfo/Europe/Berlin /etc/localtime && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /src

# Kopiere Projektdateien und stelle Abhängigkeiten wieder her (Nutzt Docker Layer Caching optimal)
COPY ["FuelDistanceCalculator.sln", "."]
COPY ["FuelDistanceCalculator/FuelDistanceCalculator.csproj", "FuelDistanceCalculator/"]
COPY ["FuelDistanceCalculatorTest/FuelDistanceCalculatorTests.csproj", "FuelDistanceCalculatorTest/"]
RUN dotnet restore "FuelDistanceCalculator.sln"

# Kopiere restlichen Code und führe Publish aus
COPY . .
WORKDIR /src/FuelDistanceCalculator
RUN dotnet publish -c Release -o /app/publish
# Der vorherige 'dotnet restore' macht '--no-restore' hier effizienter.

# Führe Tests nur im Development-Modus aus
# Hinweis: Das Test-Output wird NICHT ins finale Image kopiert!
RUN mkdir -p /app/test-output && \
    echo "MODE_TYPE in Build-Stage: $MODE_TYPE" && \
    if [ "$MODE_TYPE" = "Development" ]; then \
        echo "Running dotnet test..." && \
        dotnet test /src/FuelDistanceCalculator.sln -c Release --verbosity detailed --logger "trx;LogFileName=/app/test-output/testresults.trx" --logger "console;verbosity=detailed" | tee /app/test-output/testoutput.txt || echo "Error: dotnet test failed"; \
    else \
        echo "Tests übersprungen, MODE_TYPE ist $MODE_TYPE"; \
    fi


# ----------------------------------------------------
# 2. RUNTIME-STAGE: Schlankes Image für die Ausführung
# ----------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Installation von Tools (postgresql-client, dos2unix) in einer Zeile
RUN apt-get update && \
    apt-get install -y postgresql-client dos2unix && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

# Kopiere die veröffentlichten Artefakte aus der 'build'-Stage
COPY --from=build /app/publish .

# Kopiere Skripte und SQL-Dateien
COPY start.sh /app/start.sh
COPY create_tables.sql /app/create_tables.sql

# dos2unix anwenden und Ausführungsrechte setzen
RUN dos2unix /app/start.sh && chmod +x /app/start.sh

ENTRYPOINT ["/bin/bash", "-c", "/app/start.sh"]