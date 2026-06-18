using FuelDistanceCalculator.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace FuelDistanceCalculatorTest.PageTests;

public class PageTestBase
{
    protected HttpClient _client;
    protected WebApplicationFactory<Program> _factory;

    [SetUp]
    public void Setup()
    {
        // Set env to Testing
        Environment.SetEnvironmentVariable("MODE_TYPE", "Testing");

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.UseSolutionRelativeContentRoot("FuelDistanceCalculator");

                // Override configuration BEFORE app build
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    var dict = new Dictionary<string, string>
                    {
                        // ensure test has values so GeoLocationService doesn’t throw
                        ["ApiSettings:TankApiKey"] = Environment.GetEnvironmentVariable("TANK_API_KEY") ?? "test",
                        ["ApiSettings:OpenRouteServiceApiKey"] = Environment.GetEnvironmentVariable("OPENROUTESERVICE_API_KEY") ?? "test",
                        ["ApiSettings:OilPriceApiKey"] = Environment.GetEnvironmentVariable("OIL_PRICE_API_KEY") ?? "test",
                        ["Redis:Configuration"] = "" // avoid actual Redis config
                    };
                    config.AddInMemoryCollection(dict);
                });

                builder.ConfigureTestServices(services =>
                {
                    // DataProtection auf In-Memory umstellen – kein Filesystem nötig
                    services.AddDataProtection()
                        .UseEphemeralDataProtectionProvider();

                    // Mock RedisCache completely
                    services.RemoveAll(typeof(IDistributedCache));
                    services.AddSingleton<IDistributedCache>(_ => new Mock<IDistributedCache>().Object);

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