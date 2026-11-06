using FuelDistanceCalculator.Constants;
using FuelDistanceCalculator.Services;

namespace FuelDistanceCalculatorTest.ServiceTests;

public class TankStationSearchServiceTest : ServiceTestBase
{
    [Test]
    [TestCase(20)]
    [TestCase(0)]
    public async Task ExecuteSearchAsync_ShouldWork(decimal fuelAmount)
    {
        // Arrange
        var place = "Berlin";
        var radius = 10.0;
        var fuelType = FuelType.Diesel;
        var pricePerKm = 0.25m;
        string? stationBrand = null;
        string? discountPercentOrAbsolute = null;
        TankStationSearchService tankStationSearchService = new TankStationSearchService(
            _marketFuelPriceService,
            _geoLocationService);

        // Act
        var result = await tankStationSearchService.ExecuteSearchAsync(
            place, radius, fuelType, pricePerKm, fuelAmount, stationBrand, discountPercentOrAbsolute);

        // Assert
        Assert.IsTrue(result.Success);

        result.Stations.ForEach(station =>
        {
            Assert.That(fuelAmount > 0 ? station.TotalCombinedCosts > 0 : station.TotalCombinedCosts == 0);
            Assert.False(string.IsNullOrEmpty(station.Brand));
            Assert.IsNotNull(station.Dist);
            Assert.IsNotNull(station.FuelTypePrice);
        });
    }

    [Test]
    [TestCase(20)]
    [TestCase(0)]
    public async Task ExecuteSearchAsync_ShouldSortAccordingly(decimal fuelAmount)
    {
        // Arrange
        var place = "Berlin";
        var radius = 10.0;
        var fuelType = FuelType.Diesel;
        var pricePerKm = 0.25m;
        string? stationBrand = null;
        string? discountPercentOrAbsolute = null;
        TankStationSearchService tankStationSearchService = new TankStationSearchService(
            _marketFuelPriceService,
            _geoLocationService);

        // Act
        var result = await tankStationSearchService.ExecuteSearchAsync(
            place, radius, fuelType, pricePerKm, fuelAmount, stationBrand, discountPercentOrAbsolute);

        // Assert
        Assert.IsTrue(result.Success);

        for (int i = 0; i < result.Stations.Count - 1; i++)
        {
            if (fuelAmount > 0)
            {
                Assert.That(result.Stations[i].TotalCombinedCosts, Is.LessThanOrEqualTo(result.Stations[i + 1].TotalCombinedCosts));
            }
            else
            {
                Assert.That(result.Stations[i].FuelTypePrice, Is.LessThanOrEqualTo(result.Stations[i + 1].FuelTypePrice));
            }

        }
    }

    [Test]
    public async Task Switching_FuelType_InSearch_ShouldWork()
    {
        // Arrange
        var place = "Berlin";
        var radius = 10.0;
        var pricePerKm = 0.25m;
        decimal fuelAmount = 20;
        string? stationBrand = null;
        string? discountPercentOrAbsolute = null;
        TankStationSearchService tankStationSearchService = new TankStationSearchService(
            _marketFuelPriceService,
            _geoLocationService);

        // Act
        var resultDiesel = await tankStationSearchService.ExecuteSearchAsync(
            place, radius, FuelType.Diesel, pricePerKm, fuelAmount, stationBrand, discountPercentOrAbsolute);

        var resultSuperE5 = await tankStationSearchService.ExecuteSearchAsync(
            place, radius, FuelType.SuperE5, pricePerKm, fuelAmount, stationBrand, discountPercentOrAbsolute);

        // Assert
        string id = "64c43b30-4e90-4eae-aab5-44358c1ce7e5";
        Assert.That(resultDiesel.Stations.FirstOrDefault(x => x.Id.Equals(id)).FuelTypePrice,
        Is.Not.EqualTo(resultSuperE5.Stations.FirstOrDefault(x => x.Id.Equals(id)).FuelTypePrice));
    }

    [Test]
    public async Task Switching_FuelType_In_Compare_ShouldWork()
    {
        // Arrange
        TankStationSearchService tankStationSearchService = new TankStationSearchService(
            _marketFuelPriceService,
            _geoLocationService);

        List<string> places = new List<string> { "Berlin", "Hamburg" };
        List<double> radiusPlaces = new List<double> { 10, 20 };

        // Act
        var resultDiesel = await tankStationSearchService.CalculateAverageCostsAsync(places, radiusPlaces, FuelType.Diesel);
        var resultSuperE5 = await tankStationSearchService.CalculateAverageCostsAsync(places, radiusPlaces, FuelType.SuperE5);

        //Assert
        Assert.That(resultDiesel.Locations.FirstOrDefault().AverageCost,
        Is.Not.EqualTo(resultSuperE5.Locations.FirstOrDefault().AverageCost));
    }
}