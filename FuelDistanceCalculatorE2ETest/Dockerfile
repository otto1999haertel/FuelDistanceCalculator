FROM mcr.microsoft.com/playwright/dotnet:v1.61.0-noble

WORKDIR /tests
COPY . .
RUN dotnet restore
RUN dotnet build

ENTRYPOINT ["dotnet", "test", "--logger", "trx;LogFileName=e2e-results.trx"]