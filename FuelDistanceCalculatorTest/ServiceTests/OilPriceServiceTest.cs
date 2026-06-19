using System.Net;
using FuelDistanceCalculator.Model;
using FuelDistanceCalculator.Services;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Moq.Protected;

namespace FuelDistanceCalculatorTest.ServiceTests;

public class OilPriceServiceTest : ServiceTestBase
{

    [Test]
    public async Task GetOilPriceChangeAsync_ReturnsSuccessResult()
    {
        // Arrange
        var expectedLastUpdated = new DateTimeOffset(2026, 6, 19, 9, 37, 51, TimeSpan.FromHours(2));        
        
        // Act
        OilPriceResult result = await _oilPriceService.GetOilPriceChangeAsync();

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.PriceChange, Is.Not.Null);
        Assert.That(result.PriceChange.CurrentPrice, Is.EqualTo(80.12).Within(0.01));
        Assert.That(result.PriceChange.Day,   Is.EqualTo(0.34).Within(0.01));
        Assert.That(result.PriceChange.Week,  Is.EqualTo(-8.26).Within(0.01));
        Assert.That(result.PriceChange.Month, Is.EqualTo(-28.00).Within(0.01));
        Assert.That(result.PriceChange.LastUpdated, Is.EqualTo(expectedLastUpdated));
    }

    [Test]
    public async Task GetOilPriceChangeAsync_WhenApiFails_ReturnsCachedValue()
    {
        // Arrange
        var callCount = 0;
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                // Erster Aufruf: Erfolg, zweiter Aufruf: Fehler
                return callCount == 1
                    ? new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.OK,
                        Content = new StringContent(
                            File.ReadAllText("Data/Oil_price_API_response.json"))
                    }
                    : new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.ServiceUnavailable
                    };
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var cache = new MemoryCache(new MemoryCacheOptions());
        var mockConfig = new Mock<IConfiguration>();
        mockConfig.Setup(c => c["MODE_TYPE"]).Returns("Production");

        var service = new OilPriceService(mockConfig.Object, httpClient, cache);

        // Act
        var first  = await service.GetOilPriceChangeAsync(); // API erfolgreich → Cache befüllt
        var second = await service.GetOilPriceChangeAsync(); // API schlägt fehl → Cache Fallback

        // Assert
        Assert.That(first.IsSuccess, Is.True);
        Assert.That(second.IsSuccess, Is.True); // Cache-Fallback liefert trotzdem Success
        Assert.That(second.PriceChange.CurrentPrice,
            Is.EqualTo(first.PriceChange.CurrentPrice).Within(0.01));
    }
}