using FuelDistanceCalculator.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace FuelDistanceCalculatorTest.PageTests
{
    public class PageTestBase
    {
        protected HttpClient _client;
        protected WebApplicationFactory<Program> _factory;

        [SetUp]
        public async Task Setup()
        {
            // Setze ENV‑Var für Tests (lokal & CI)
            Environment.SetEnvironmentVariable("MODE_TYPE", "Testing");
            
            // Wenn du über GitHub Actions API‑Keys als ENV setzt:
            // z. B. TANK_API_KEY, OPENROUTESERVICE_API_KEY
            // dann sind diese jetzt schon gesetzt (CI oder lokal) und werden von IConfiguration gelesen.

            _factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseEnvironment("Testing");
                    builder.UseSolutionRelativeContentRoot("FuelDistanceCalculator");

                    // Hier stellen wir sicher, dass EnvironmentVars
                    // direkt in die App‑Konfiguration einfließen:
                    builder.ConfigureAppConfiguration((context, config) =>
                    {
                        config.AddEnvironmentVariables(); // GitHub Actions ENV übernehmen
                    });

                    builder.ConfigureTestServices(services =>
                    {
                        // Mock Redis so that Session / Cache don’t fail
                        services.RemoveAll(typeof(IDistributedCache));
                        services.AddSingleton<IDistributedCache>(_ =>
                            new Mock<IDistributedCache>().Object
                        );

                        // Optional: Wenn dein GeoLocationService nicht null‑safe ist,
                        // kannst du hier override/mock implementieren.
                        // Beispiel: services.AddSingleton<IGeoLocationService>(_ => new FakeGeoLocationService());

                        // Andere Mocks deiner Wahl:
                        services.AddSingleton<FuelPriceService>(_ => new FuelPriceService());
                        services.AddHttpClient<MarketFuelPriceService>();
                        services.AddScoped<GeoLocationService, GeoLocationService>();
                    });
                });

            _client = _factory.CreateClient();
        }

        [TearDown]
        public void Cleanup()
        {
            _client?.Dispose();
            _factory?.Dispose();
        }
    }
}