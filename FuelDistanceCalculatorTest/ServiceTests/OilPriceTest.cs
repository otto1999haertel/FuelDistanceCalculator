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
            PriceChange = new OilPriceChange(1.5, 3.0, 5.0)
        };

        //act
        OilPriceResult result = await _oilPriceService.GetOilPriceChangeAsync();


        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.PriceChange, Is.Not.Null);
        Assert.That(result.PriceChange.Day, Is.EqualTo(1.5));
        Assert.That(result.PriceChange.Week, Is.EqualTo(3.0));
        Assert.That(result.PriceChange.Month, Is.EqualTo(5.0));
    }
}