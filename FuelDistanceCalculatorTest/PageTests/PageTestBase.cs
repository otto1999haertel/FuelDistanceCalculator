using FuelDistanceCalculator.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;
using StackExchange.Redis;

namespace FuelDistanceCalculatorTest.PageTests
{
    public class PageTestBase
    {
        protected HttpClient _client;
        protected WebApplicationFactory<Program> _factory;
        [SetUp]
        public async Task Setup()
        {
            Environment.SetEnvironmentVariable("MODE_TYPE", "Testing");

            _factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.UseEnvironment("Testing");

                    builder.ConfigureTestServices(services =>
                    {
                        // Mock Services
                        services.AddSingleton<FuelPriceService>(_ => new FuelPriceService());

                        services.AddHttpClient<MarketFuelPriceService>();

                        services.AddScoped<GeoLocationService, GeoLocationService>();

                        // Mock Redis
                        var mockConnectionMultiplexer = new Mock<IConnectionMultiplexer>();
                        var mockDatabase = new Mock<IDatabase>();
                        mockConnectionMultiplexer
                            .Setup(c => c.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                            .Returns(mockDatabase.Object);

                        services.AddSingleton<IConnectionMultiplexer>(mockConnectionMultiplexer.Object);

                        // Session, RazorPages, etc. sind bereits in Program.cs enthalten
                        // Du kannst sie aber überschreiben, wenn nötig
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