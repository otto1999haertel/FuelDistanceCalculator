using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using FuelDistanceCalculator.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NUnit.Framework;

namespace FuelDistanceCalculatorTest
{
    public class BaseTestMarketfuelpriceService
    {
        protected List<GasStation> _fakeGasStationList;
        protected MarketFuelPriceService _marketFuelPriceService;

        [SetUp]
        public async Task Setup()
        {
            // Erstelle DI-Container
            var services = new ServiceCollection();

            // Mock IConfiguration
            var mockConfiguration = new Mock<IConfiguration>();
            mockConfiguration.Setup(c => c["ApiSettings:TankApiKey"]).Returns("test-api-key");

            // Mock GeoLocationService
            var mockGeoLocationService = new Mock<IGeoLocationService>();

            // HttpClient für Development-Modus (wird nicht verwendet, da JSON-Datei geladen wird)
            services.AddHttpClient<MarketFuelPriceService>();

            // Registriere MarketFuelPriceService
            services.AddScoped(_ => new MarketFuelPriceService(mockConfiguration.Object, new HttpClient(), mockGeoLocationService.Object));

            // Erstelle ServiceProvider
            var serviceProvider = services.BuildServiceProvider();

            // Hole MarketFuelPriceService
            _marketFuelPriceService = serviceProvider.GetRequiredService<MarketFuelPriceService>();

            // Lade Tankstellen aus JSON-Datei
            _fakeGasStationList = await GetFakeGasStationsAsync();

            // Validiere, dass die Liste nicht leer ist
            Assert.That(_fakeGasStationList != null && _fakeGasStationList.Count > 0, Is.True, "Die Fake-Tankstellenliste ist leer oder null.");
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

        [Test]
        public void Test1()
        {
            // Beispieltest: Prüfe, ob die Liste Tankstellen enthält
            Assert.That(_fakeGasStationList, Is.Not.Empty, "Die Tankstellenliste sollte nicht leer sein.");
        }
    }
}