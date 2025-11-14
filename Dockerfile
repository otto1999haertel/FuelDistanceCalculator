# Build-Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
RUN apt-get update && apt-get install -y tzdata && apt-get clean && rm -rf /var/lib/apt/lists/*
ENV TZ=Europe/Berlin
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone
ARG MODE_TYPE=Development
ENV MODE_TYPE=$MODE_TYPE

WORKDIR /src
COPY ["FuelDistanceCalculator.sln", "."]
COPY ["FuelDistanceCalculator/FuelDistanceCalculator.csproj", "FuelDistanceCalculator/"]
COPY ["FuelDistanceCalculatorTest/FuelDistanceCalculatorTests.csproj", "FuelDistanceCalculatorTest/"]
RUN dotnet restore "FuelDistanceCalculator.sln"
COPY . .
RUN dotnet build "FuelDistanceCalculator.sln" -c Release

RUN mkdir -p /app/test-output && \
    chmod -R 777 /src /app && \
    echo "MODE_TYPE in Build-Stage: $MODE_TYPE" && \
    if [ "$MODE_TYPE" = "Development" ]; then \
        echo "Running dotnet test..." && \
        dotnet test "FuelDistanceCalculator.sln" -c Release --verbosity detailed --logger "trx;LogFileName=/app/test-output/testresults.trx" --logger "console;verbosity=detailed" | tee /app/test-output/testoutput.txt || echo "Error: dotnet test failed" && \
        echo "Checking for test output files..." && \
        find / -name "testresults.trx" 2>/dev/null || echo "TRX file not found anywhere" && \
        find / -name "testoutput.txt" 2>/dev/null || echo "Test output file not found anywhere" && \
        ls -l /app/test-output/testresults.trx /app/test-output/testoutput.txt 2>/dev/null || echo "Warning: No files in /app/test-output"; \
    else \
        echo "Tests übersprungen, MODE_TYPE ist $MODE_TYPE"; \
    fi
RUN dotnet publish "FuelDistanceCalculator/FuelDistanceCalculator.csproj" -c Release -o /app/publish --no-restore

# Runtime-Stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
RUN apt-get update && apt-get install -y postgresql-client && apt-get clean && rm -rf /var/lib/apt/lists/*
COPY --from=build /app/publish .
COPY --from=build /app/test-output /app/test-output
COPY start.sh /app/start.sh
COPY create_tables.sql /app/create_tables.sql
RUN chmod +x /app/start.sh
ENTRYPOINT ["/bin/bash", "-c", "/app/start.sh"]