using System.Globalization;
using System.Net;
using FuelDistanceCalculator.Interfaces;
using FuelDistanceCalculator.Model;
using FuelDistanceCalculator.Services;
using Moq;
using StackExchange.Redis;
using Testcontainers.Redis;

namespace FuelDistanceCalculatorTest.ServiceTests;

/// <summary>
/// Integrationstests für GeoLocationService mit echtem Redis (Testcontainers)
/// </summary>
[TestFixture]
public class GeoLocationServiceTest : ServiceTestBase
{
    private RedisContainer _redisContainer = null!;
    private IConnectionMultiplexer _redisConnection = null!;
    private IDatabase _redisDb = null!;
    private readonly TimeSpan cacheDuration = TimeSpan.FromDays(365);

    // ──────────────────────────────────────────────────────────────
    // Setup / Teardown
    // ──────────────────────────────────────────────────────────────

    [OneTimeSetUp]
    public async Task OneTimeSetup()
    {
        _redisContainer = new RedisBuilder()
            .WithImage("redis:latest")
            .Build();

        await _redisContainer.StartAsync();

        var connectionString = _redisContainer.GetConnectionString();
        _redisConnection = await ConnectionMultiplexer.ConnectAsync(connectionString);
        _redisDb = _redisConnection.GetDatabase();
    }

    [OneTimeTearDown]
    public async Task OneTimeTearDown()
    {
        await _redisConnection.DisposeAsync();
        await _redisContainer.DisposeAsync();
    }

    // ──────────────────────────────────────────────────────────────
    // Hilfsklasse: TestHttpMessageHandler
    // ──────────────────────────────────────────────────────────────
    private class TestHttpMessageHandler : DelegatingHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

        public TestHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
            => _handler(request, ct);
    }

    // ──────────────────────────────────────────────────────────────
    // Hilfsmethode: Erstelle Service mit echtem Redis + Mock HTTP
    // ──────────────────────────────────────────────────────────────
    private IGeoLocationService CreateGeoLocationService(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>? httpHandler = null)
    {
        var services = new ServiceCollection();

        // ECHTER Redis
        services.AddSingleton<IConnectionMultiplexer>(_redisConnection);

        // Mock Configuration
        var config = new Mock<IConfiguration>();
        config.Setup(c => c["ApiSettings:OpenRouteServiceApiKey"]).Returns("fake-ors-key");
        config.Setup(c => c["MODE_TYPE"]).Returns("Development");
        services.AddSingleton(config.Object);

        // Mock HttpClient
        var handler = new TestHttpMessageHandler(httpHandler ?? ((req, ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(@"{""features"":[{""geometry"":{""coordinates"":[13.405,52.52]}}]}")
            })));

        var httpClient = new HttpClient(handler) { BaseAddress = new Uri("http://localhost") };

        var httpFactory = new Mock<IHttpClientFactory>();
        httpFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);
        services.AddSingleton(httpFactory.Object);

        // Service
        services.AddScoped<IGeoLocationService, GeoLocationService>();

        var sp = services.BuildServiceProvider();
        return sp.GetRequiredService<IGeoLocationService>();
    }

    // ──────────────────────────────────────────────────────────────
    // TEST 1: NormalizeAddressKey
    // ──────────────────────────────────────────────────────────────
    [Test]
    [TestCase(" 123 Main St, Anytown ", "123 main st, anytown")]
    [TestCase("Östritzer-Über-Äpfel-Straße", "oestritzer-ueber-aepfel-strasse")]
    [TestCase("  München  ", "muenchen")]
    public void NormalizeAddressKey_ReturnsExpected(string input, string expected)
    {
        // Arrange
        var service = CreateGeoLocationService();

        // Act
        var result = service.NormalizeAddressKey(input);

        // Assert
        Assert.That(result, Is.EqualTo(expected));
    }

    // ──────────────────────────────────────────────────────────────
    // TEST 2: Cache-Hit → Kein API-Call
    // ──────────────────────────────────────────────────────────────


    [Test]
    public async Task GetCoordinatesAsync_CacheHit_ReturnsCachedData_WithoutApiCall()
    {
        // Arrange
        string place = "Hamburg";
        string cacheKey = $"geo:{place.ToLower()}";

        // Cache leeren
        await _redisDb.KeyDeleteAsync(cacheKey);

        // SPEICHERE ALS STRING mit F6 (wie im Produktivcode!)
        await _redisDb.HashSetAsync(cacheKey, new[]
        {
                new HashEntry("lat", 53.551.ToString("F3", CultureInfo.InvariantCulture)), // "53.551000"
                new HashEntry("lon", 9.993.ToString("F3", CultureInfo.InvariantCulture))   // "9.993000"
            });
        await _redisDb.KeyExpireAsync(cacheKey, TimeSpan.FromDays(365));

        int requestCount = 0;
        var service = CreateGeoLocationService((req, ct) =>
        {
            requestCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(@"{""features"":[{""geometry"":{""coordinates"":[0,0]}}]}")
            });
        });

        // Act
        var result = await service.GetCoordinatesAsync(place);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Latitude, Is.EqualTo(53.551).Within(0.000001));
        Assert.That(result.Longitude, Is.EqualTo(9.993).Within(0.000001));
        Assert.That(requestCount, Is.EqualTo(0), "API wurde aufgerufen trotz Cache-Hit!");
    }

    [Test]
    public async Task GetPlaceAsync_CacheHit_ReturnsCachedData_WithoutApiCall()
    {
        // Arrange
        string fullAddress = $"{"Musterstrasse"} {2}, {1234} {"Berlin"}".Trim();
        double lat = 53.551;
        double lon = 9.993;
        string cacheKey = $"geo:reverse:{lat.ToString().Replace(',', '.')}:{lon.ToString().Replace(',', '.')}";

        // Cache leeren
        await _redisDb.KeyDeleteAsync(cacheKey);

        // SPEICHERE ALS STRING mit F6 (wie im Produktivcode!)
        await _redisDb.StringSetAsync(cacheKey, fullAddress, this.cacheDuration);

        int requestCount = 0;
        var service = CreateGeoLocationService((req, ct) =>
        {
            requestCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(@"{""features"":[{""geometry"":{""coordinates"":[0,0]}}]}")
            });
        });

        // Act
        var result = await service.GetAddressFromCoordinatesAsync(lat, lon);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo(fullAddress), "API wurde aufgerufen trotz Cache-Hit!");
    }

    [Test]
    public async Task GetCoordinatsFromPLZNormalizesAndReturnsCoordinates()
    {
        // Arrange
        string place = "12345";
        string cacheKey = $"geo:{place.ToLower() + ", deutschland"}";

        // Cache leeren
        // SPEICHERE ALS STRING mit F6 (wie im Produktivcode!)
        await _redisDb.HashSetAsync(cacheKey, new[]
        {
                new HashEntry("lat", 53.551.ToString("F3", CultureInfo.InvariantCulture)),
                new HashEntry("lon", 9.993.ToString("F3", CultureInfo.InvariantCulture))
            });

        int requestCount = 0;
        var service = CreateGeoLocationService((req, ct) =>
        {
            requestCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(@"{""features"":[{""geometry"":{""coordinates"":[10.000,50.000]}}]}")
            });
        });
        await _redisDb.KeyExpireAsync(cacheKey, TimeSpan.FromDays(365));

        // Act
        var result = await service.GetCoordinatesAsync(place);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Latitude, Is.EqualTo(53.551).Within(0.000001));
        Assert.That(result.Longitude, Is.EqualTo(9.993).Within(0.000001));
        Assert.That(requestCount, Is.EqualTo(0), "API wurde aufgerufen trotz Cache!");
    }

    [Test]
    public async Task GetDistanceTest()
    {
        double lat = 53.551;
        double lon = 9.993;
        // Arrange
        var service = CreateGeoLocationService(null);
        GasStation gasStation1 = new GasStation();
        Coordinates coordinates = new Coordinates();
        coordinates.Lat = lat;
        coordinates.Lng = lon;
        gasStation1.Coords = coordinates;
        gasStation1.Dist = 3;

        GasStation gasStation2 = new GasStation();
        gasStation2.Coords = coordinates;
        gasStation2.Dist = 4;


        //Act
        List<GasStation> result = await service.CalculateDistanceFromAPI(lon, lat, new List<GasStation>() { gasStation1, gasStation2 });
        Assert.That(result[0].Dist.Equals((double)7.68));
        Assert.That(result[0].IsRoutingDistanceCalculated.Equals(true));
        Assert.That(result[1].Dist.Equals((double)4));
        Assert.That(result[1].IsRoutingDistanceCalculated.Equals(false));
    }

    [Test]
    public async Task CalculateRoutePointsTest()
    {
        // Arrange
        var service = CreateGeoLocationService();
        CoordinatesDTO startPoint = new CoordinatesDTO();
        startPoint.Longitude = 14.0129574;
        startPoint.Latitude = 51.348037;
        List<CoordinatesDTO> result = await service.GetRouteIncludingStartPoint(startPoint.Latitude, startPoint.Longitude, 0, 0);
        Assert.That(result[0].Longitude.Equals(startPoint.Longitude));
        Assert.That(result[0].Latitude.Equals(startPoint.Latitude));
        Assert.That(result.Last().Longitude.Equals(13.737048));
        Assert.That(result.Last().Latitude.Equals(51.049407));

        //Act
        List<CoordinatesDTO> serachPoints = service.GetSearchPoints(result, 15, 3);

        //Assert
        Assert.That(serachPoints.Count.Equals(5));
    }
}