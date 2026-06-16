using FuelDistanceCalculator.Model;

namespace FuelDistanceCalculatorTest.ServiceTests;

public class OilPriceTest : ServiceTestBase
{

    [Test]
    [Ignore("TODO Implement JSON Parsin")]
    public async Task GetOilPriceChangeAsync_ReturnsSuccessResult()
    {
        // Arrange
        var expectedResult = new OilPriceResult
        {
            IsSuccess = true,
            PriceChange = new OilPriceChange(-5.42, -15.50, -29.83, 78.66)
        };

        //act
        OilPriceResult result = await _oilPriceService.GetOilPriceChangeAsync();


        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.PriceChange, Is.Not.Null);
        Assert.That(result.PriceChange.Day, Is.EqualTo(expectedResult.PriceChange.Day));
        Assert.That(result.PriceChange.Week, Is.EqualTo(expectedResult.PriceChange.Week));
        Assert.That(result.PriceChange.Month, Is.EqualTo(expectedResult.PriceChange.Month));
        Assert.That(result.PriceChange.CurrentPrice, Is.EqualTo(expectedResult.PriceChange.CurrentPrice));
    }
}