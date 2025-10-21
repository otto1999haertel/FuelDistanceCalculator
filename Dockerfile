# Build-Stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
RUN apt-get update && apt-get install -y tzdata && apt-get clean && rm -rf /var/lib/apt/lists/*
ENV TZ=Europe/Berlin
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone
ARG MODE_TYPE=Production
ENV MODE_TYPE=$MODE_TYPE
WORKDIR /src
COPY . .
RUN dotnet restore "FuelDistanceCalculator.sln"

RUN dotnet build "FuelDistanceCalculator.sln" -c Release --no-restore
RUN mkdir -p /app/test-output && \
    echo "MODE_TYPE in Build-Stage: $MODE_TYPE" && \
    if [ "$MODE_TYPE" = "Development" ]; then dotnet test "FuelDistanceCalculator.sln" -c Release --no-build --verbosity detailed --logger "trx;LogFileName=/src/testresults.trx" --logger "console;verbosity=detailed" | tee /src/testoutput.txt; ls -l /src/testresults.trx /src/testoutput.txt || true; cp /src/testresults.trx /app/test-output/testresults.trx; cp /src/testoutput.txt /app/test-output/testoutput.txt; ls -l /app/test-output/testresults.trx /app/test-output/testoutput.txt || true; else echo "Tests übersprungen, MODE_TYPE ist $MODE_TYPE"; fi
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
ENV REDIS_HOST=redis:6379
ENV MODE_TYPE=$MODE_TYPE
ENTRYPOINT ["/bin/bash", "-c", "/app/start.sh && dotnet FuelDistanceCalculator.dll"]