using FuelDistanceCalculator.Model;
using FuelDistanceCalculator.Services;
using Microsoft.IdentityModel.Tokens;

namespace FuelDistanceCalculatorTest.ServiceTests
{
    public class MarketFuelPriceServiceTest : BaseTest
    {
        [Test]
        public async Task GetGasStationsAsync_DevelopmentMode_ReturnsFakeData()
        {
            // Arrange
            double latitude = 49.5937712;
            double longitude = 11.0320692;
            double radius = 5;
            string fueltype = "diesel";

            // Act
            GasStationResult temp = await _marketFuelPriceService.GetGasStationsAsync(latitude, longitude, radius, fueltype);
            List<GasStation> resultStations = temp.Stations;

            // Assert
            foreach (var station in resultStations)
            {
                Assert.That(station.IsOpen, Is.True);
                Assert.That(station.Fuels.Any(x => !x.Name.IsNullOrEmpty()), Is.True);
                Assert.That(station.Fuels.Any(x => x.Price.HasValue), Is.True);
                Assert.That(station.Dist.HasValue, Is.True);

            }
        }
    }
}