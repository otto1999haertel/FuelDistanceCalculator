using FuelDistanceCalculator.Services;
using FuelDistanceCalculator.Interfaces;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using StackExchange.Redis;

namespace FuelDistanceCalculatorTest.PageTests;

public abstract class PageTestBase
{
    protected HttpClient _client;
    protected WebApplicationFactory<Program> _factory;

    [SetUp]
    public void Setup()
    {
        Environment.SetEnvironmentVariable("MODE_TYPE", "Testing");

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.UseSolutionRelativeContentRoot("FuelDistanceCalculator");

                builder.ConfigureAppConfiguration((context, config) =>
                {
                    var dict = new Dictionary<string, string>
                    {
                        ["ApiSettings:TankApiKey"] = Environment.GetEnvironmentVariable("TANK_API_KEY") ?? "test",
                        ["ApiSettings:OpenRouteServiceApiKey"] = Environment.GetEnvironmentVariable("OPENROUTESERVICE_API_KEY") ?? "test",
                        ["ApiSettings:OilPriceApiKey"] = Environment.GetEnvironmentVariable("OIL_PRICE_API_KEY") ?? "test",
                        ["Redis:Configuration"] = "",
                        // Hoch genug, damit normale Tests den Limiter nie auslösen
                        ["RateLimit:PermitLimit"] = "1000",
                        ["RateLimit:UpstreamPermitLimit"] = "1000"
                    };
                    config.AddInMemoryCollection(dict);
                });

                builder.ConfigureTestServices(services =>
                {
                    var mockDatabase = new Mock<IDatabase>();
                    var mockMultiplexer = new Mock<IConnectionMultiplexer>();
                    mockMultiplexer
                        .Setup(c => c.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                        .Returns(mockDatabase.Object);

                    services.RemoveAll<IConnectionMultiplexer>();
                    services.AddSingleton<IConnectionMultiplexer>(mockMultiplexer.Object);

                    services.AddDataProtection()
                        .UseEphemeralDataProtectionProvider();

                    services.RemoveAll(typeof(IDistributedCache));
                    services.AddSingleton<IDistributedCache>(_ => new Mock<IDistributedCache>().Object);

                    services.AddSingleton<FuelPriceService>(_ => new FuelPriceService());
                    services.AddHttpClient<IMarketFuelPriceService, MarketFuelPriceService>();
                    services.AddScoped<IGeoLocationService, GeoLocationService>();
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