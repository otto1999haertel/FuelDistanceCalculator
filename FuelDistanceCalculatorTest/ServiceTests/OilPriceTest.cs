using FuelDistanceCalculator.Model;

namespace FuelDistanceCalculatorTest.ServiceTests;

public class OilPriceTest : ServiceTestBase
{

    [Test]
    public async Task GetOilPriceChangeAsync_ReturnsSuccessResult()
    {
        // Arrange
        var expectedResult = new OilPriceResult
        {
            IsSuccess = true,
            PriceChange = new OilPriceChange(-4.26, -14.46, -28.96, 79.63)
        };

        //act
        OilPriceResult result = await _oilPriceService.GetOilPriceChangeAsync();


        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.PriceChange, Is.Not.Null);
        Assert.That(result.PriceChange.CurrentPrice, Is.EqualTo(79.63).Within(0.01));
        Assert.That(result.PriceChange.Day,   Is.EqualTo(-4.26).Within(0.01));
        Assert.That(result.PriceChange.Week,  Is.EqualTo(-14.46).Within(0.01));
        Assert.That(result.PriceChange.Month, Is.EqualTo(-28.96).Within(0.01));
    }
}