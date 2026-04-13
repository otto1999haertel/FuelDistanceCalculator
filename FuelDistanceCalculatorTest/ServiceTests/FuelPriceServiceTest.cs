using FuelDistanceCalculator.Services;
using FuelDistanceCalculator.Model;

namespace FuelDistanceCalculatorTest.ServiceTests;

public class FuelPriceServiceTest : ServiceTestBase
{
    [Test]
    public async Task GetGasStationsAsync_ReturnsStationsList_Test()
    {
        // Act
        FuelPriceService fuelPriceService = new FuelPriceService();
        decimal? calculatedAverage = fuelPriceService.CalculateAverageCost(_fakeGasStationList);

        // Assert
        Assert.That(calculatedAverage, Is.Not.Null);
        Assert.That(calculatedAverage, Is.EqualTo(GetExpectedAverageFuelPrice(_fakeGasStationList)));
    }

    [Test]
    [TestCaseSource(nameof(GetTestCases))]
    public async Task GetGasStationsAsync_EmptyStationsList_ReturnsZero_Test(List<GasStation> emptyStationsList)
    {
        //Arrange
        FuelPriceService fuelPriceService = new FuelPriceService();

        //Act
        decimal? calculatedAverage = fuelPriceService.CalculateAverageCost(emptyStationsList);

        //Assert
        Assert.That(calculatedAverage, Is.EqualTo(0));
    }

    private decimal? GetExpectedAverageFuelPrice(List<GasStation> gasStations)
    {
        if (gasStations == null || gasStations.Count == 0)
        {
            return 0;
        }
        return gasStations.OrderBy(gs => gs.FuelTypePrice).ToList().Take(10).Average(gs => gs.FuelTypePrice);
    }

    private static IEnumerable<TestCaseData> GetTestCases()
    {
        yield return new TestCaseData(new List<GasStation>()).SetName("EmptyList");
        yield return new TestCaseData(null).SetName("NullList");
    }
}