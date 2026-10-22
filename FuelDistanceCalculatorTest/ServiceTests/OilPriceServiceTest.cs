using System.Net;
using System.Text.Json;
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
        var expectedLastUpdated = new DateTimeOffset(2026, 10, 22, 10, 31, 17, TimeSpan.FromHours(2));

        // Act
        OilPriceResult result = await _oilPriceService.GetOilPriceChangeAsync();

        // Assert
        Assert.That(result.IsSuccess, Is.True, result.ErrorMessage);
        Assert.That(result.PriceChange, Is.Not.Null);
        Assert.That(result.PriceChange.CurrentPrice, Is.EqualTo(112.17).Within(0.01));
        Assert.That(result.PriceChange.Day, Is.EqualTo(3.11).Within(0.01));
        Assert.That(result.PriceChange.Week, Is.EqualTo(5.90).Within(0.01));
        Assert.That(result.PriceChange.Month, Is.EqualTo(13.02).Within(0.01));
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
        var first = await service.GetOilPriceChangeAsync(); // API erfolgreich → Cache befüllt
        var second = await service.GetOilPriceChangeAsync(); // API schlägt fehl → Cache Fallback

        // Assert
        Assert.That(first.IsSuccess, Is.True);
        Assert.That(second.IsSuccess, Is.True); // Cache-Fallback liefert trotzdem Success
        Assert.That(second.PriceChange.CurrentPrice,
            Is.EqualTo(first.PriceChange.CurrentPrice).Within(0.01));
    }

    [Test]
    [TestCase(80.12, "80,12 $")]
    [TestCase(75.5, "75,50 $")]
    [TestCase(100, "100,00 $")]
    public void DisplayCurrentPrice_ReturnsFormattedString(double currentPrice, string expectedDisplay)
    {
        // Arrange
        var priceChange = new OilPriceChange(0.34, -8.26, -28.00, currentPrice, DateTimeOffset.Now);

        // Act
        var display = priceChange.DisplayCurrentPriceWithCurrency();

        // Assert
        Assert.That(display, Is.EqualTo(expectedDisplay));
    }

    [Test]
    [TestCase(0.34, "+0,34 %")]
    [TestCase(-8.26, "-8,26 %")]
    [TestCase(10.00, "+10,00 %")]
    [TestCase(-10.00, "-10,00 %")]
    public void DisplayChangeWithSign_ReturnsFormattedString(double change, string expectedDisplay)
    {
        // Arrange
        var priceChange = new OilPriceChange(change, 0, 0, 80.12, DateTimeOffset.Now);

        // Act
        var display = priceChange.DisplayDayChangeWithSign(change);

        // Assert
        Assert.That(display, Is.EqualTo(expectedDisplay));
    }

    [Test]
    public async Task GetOilPriceChangeAsync_CloseArrayContainsNull_NullIsSkipped()
    {
        // Guard: Die Fixture muss tatsächlich ein null im close-Array enthalten
        var path = Path.Combine(Directory.GetCurrentDirectory(), "Data", "Oil_price_API_response.json");
        using var doc = JsonDocument.Parse(await File.ReadAllTextAsync(path));

        var hasNullClose = doc.RootElement
            .GetProperty("chart").GetProperty("result")[0]
            .GetProperty("indicators").GetProperty("quote")[0]
            .GetProperty("close").EnumerateArray()
            .Any(x => x.ValueKind == JsonValueKind.Null);

        Assert.That(hasNullClose, Is.True,
            "Die Test-JSON muss ein null im close-Array enthalten, sonst prüft dieser Test nichts.");

        // Act
        OilPriceResult result = await _oilPriceService.GetOilPriceChangeAsync();

        // Assert: Ohne den Null-Filter würde GetDecimal() eine Exception werfen -> IsSuccess = false
        Assert.That(result.IsSuccess, Is.True, result.ErrorMessage);

        // "Gestern" muss der letzte gültige Handelstag (20.10., 108.79) sein, nicht der null-Tag
        Assert.That(result.PriceChange.Day, Is.EqualTo(3.11).Within(0.01));
    }
}