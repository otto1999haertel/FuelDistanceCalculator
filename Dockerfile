# ----------------------------------------------------
# 1. BUILD-STAGE: Kompilieren und Veröffentlichen
# ----------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

# Installation von tzdata und Setzen der Zeitzone in einem Schritt
RUN apt-get update && \
    apt-get install -y tzdata && \
    rm /etc/localtime && \
    ln -sf /usr/share/zoneinfo/Europe/Berlin /etc/localtime && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

WORKDIR /src

COPY ["FuelDistanceCalculator.sln", "."]
COPY ["FuelDistanceCalculator/FuelDistanceCalculator.csproj", "FuelDistanceCalculator/"]
COPY ["FuelDistanceCalculatorTest/FuelDistanceCalculatorTests.csproj", "FuelDistanceCalculatorTest/"]
RUN dotnet restore "FuelDistanceCalculator.sln"

COPY . .
WORKDIR /src/FuelDistanceCalculator
RUN dotnet publish -c Release -o /app/publish

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

RUN apt-get update && \
    apt-get install -y tzdata && \
    ln -sf /usr/share/zoneinfo/Europe/Berlin /etc/localtime && \
    echo "Europe/Berlin" > /etc/timezone && \
    apt-get clean && \
    rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .
COPY Scripts/start.sh /app/start.sh

RUN dos2unix /app/start.sh && chmod +x /app/start.sh

RUN id app 2>/dev/null || useradd -r -s /bin/false app && \
    mkdir -p /app/dataprotection-keys && \
    chown -R app:app /app/dataprotection-keys && \
    chown -R app:app /app

USER app

ENTRYPOINT ["/bin/bash", "-c", "/app/start.sh"]