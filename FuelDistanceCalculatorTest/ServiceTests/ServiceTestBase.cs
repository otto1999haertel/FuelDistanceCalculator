using FuelDistanceCalculator.Model;
using FuelDistanceCalculator.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using StackExchange.Redis;

namespace FuelDistanceCalculatorTest
{
    public class ServiceTestBase
    {
        protected List<GasStation> _fakeGasStationList;
        protected IMarketFuelPriceService _marketFuelPriceService;

        protected IGeoLocationService _geoLocationService;

        [SetUp]
        public virtual async Task Setup()
        {
            // Erstelle DI-Container
            var services = new ServiceCollection();
            Environment.SetEnvironmentVariable("MODE_TYPE", "Development");

            // Mock IConfiguration
            var mockConfiguration = new Mock<IConfiguration>();
            mockConfiguration.Setup(c => c["ApiSettings:TankApiKey"]).Returns("test-api-key");
            mockConfiguration.Setup(c => c["ApiSettings:OpenRouteServiceApiKey"]).Returns("test-ors-api-key");
            mockConfiguration.Setup(c => c["MODE_TYPE"]).Returns("Development"); // Add MODE_TYPE to configuration
            services.AddSingleton(mockConfiguration.Object); // Registriere IConfiguration

            // Mock IHttpClientFactory
            var mockHttpClientFactory = new Mock<IHttpClientFactory>();
            var httpClient = new HttpClient();
            mockHttpClientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
            services.AddSingleton(mockHttpClientFactory.Object);

            // Mock IConnectionMultiplexer (Redis)
            var mockConnectionMultiplexer = new Mock<IConnectionMultiplexer>();
            var mockDatabase = new Mock<IDatabase>();
            mockConnectionMultiplexer.Setup(c => c.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(mockDatabase.Object);
            services.AddSingleton(mockConnectionMultiplexer.Object); // Registriere IConnectionMultiplexer

            // Registriere IGeoLocationService mit GeoLocationService
            services.AddScoped<IGeoLocationService, GeoLocationService>();

            // Registriere IMarketFuelPriceService mit MarketFuelPriceService
            services.AddHttpClient<IMarketFuelPriceService, MarketFuelPriceService>();

            // Erstelle ServiceProvider
            var serviceProvider = services.BuildServiceProvider();

            // Hole MarketFuelPriceService
            _marketFuelPriceService = serviceProvider.GetRequiredService<IMarketFuelPriceService>();

            // Hole GeoLocationService
            _geoLocationService = serviceProvider.GetRequiredService<IGeoLocationService>();

            // Lade Tankstellen aus JSON-Datei
            _fakeGasStationList = await GetFakeGasStationsAsync();

            services.AddRazorPages();

            // Validiere, dass die Liste nicht leer ist
            Assert.That(_fakeGasStationList != null && _fakeGasStationList.Count > 0, Is.True, "Die Fake-Tankstellenliste ist leer oder null.");
        }

        [TearDown]
        public virtual void TearDown()
        {
            // Clean up environment variable
            Environment.SetEnvironmentVariable("MODE_TYPE", null);
        }

        private async Task<List<GasStation>> GetFakeGasStationsAsync()
        {
            // Beispielwerte für den API-Aufruf
            double latitude = 52.5200; // Beispiel: Berlin
            double longitude = 13.4050;
            double radius = 5.0; // 5 km
            string fuelType = "diesel";

            // Rufe GetGasStationsAsync auf
            var result = await _marketFuelPriceService.GetGasStationsAsync(latitude, longitude, radius, fuelType);

            return result.IsSuccess ? result.Stations : new List<GasStation>();
        }
    }
}