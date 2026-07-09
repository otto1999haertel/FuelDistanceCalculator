using AngleSharp.Html.Parser;
using FuelDistanceCalculator;
using FuelDistanceCalculator.Interfaces;
using FuelDistanceCalculator.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using System.Collections.Concurrent;
using System.Reflection;

namespace FuelDistanceCalculatorTest.PageTests;

[TestFixture]
public class ErrorPagesTest : PageTestBase
{
    [SetUp]
    public void ResetRateLimiter()
    {
        // IP-Log vor jedem Test leeren, um Seiteneffekte zu vermeiden
        var ipLogField = typeof(RequestProtectionMiddleware)
            .GetField("_ipLog", BindingFlags.NonPublic | BindingFlags.Static);
        var ipLog = (ConcurrentDictionary<string, List<DateTime>>)ipLogField.GetValue(null);
        ipLog?.Clear();
    }

    [Test]
    public async Task WhenRateLimitExceeded_ShouldReturn429WithCustomErrorPageHtml()
    {
        // 1. Arrange: Simuliere 20 Anfragen für die Test-IP der WebApplicationFactory
        // Standardmäßig nutzt die Factory oft "127.0.0.1" oder "::1"
        var testIp = "127.0.0.1";

        var ipLogField = typeof(RequestProtectionMiddleware)
            .GetField("_ipLog", BindingFlags.NonPublic | BindingFlags.Static);
        var ipLog = (ConcurrentDictionary<string, List<DateTime>>)ipLogField.GetValue(null);

        var fakeRequests = Enumerable.Repeat(DateTime.UtcNow, 40).ToList();

        ipLog["127.0.0.1"] = fakeRequests;
        ipLog["::1"] = fakeRequests;
        ipLog["unknown"] = fakeRequests;

        // 2. Act: Rufe die normale Startseite auf
        var response = await _client.GetAsync("/Index");

        // 3. Assert
        Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.TooManyRequests));

        var htmlContent = await response.Content.ReadAsStringAsync();
        var parser = new HtmlParser();
        var document = await parser.ParseDocumentAsync(htmlContent);

        var headingElement = document.QuerySelector("#ErrorHeading");
        Assert.That(headingElement?.TextContent, Does.Contain("Etwas ist schiefgelaufen"));

        var explanationElement = document.QuerySelector("#ErrorExplenation");
        Assert.That(explanationElement?.TextContent, Does.Contain("Zu viele Anfragen"));
    }

    [Test]
    public async Task WhenInternalServerErrorOccurs_ShouldReturn500WithErrorPageHtml()
    {
        var clientWithCrash = BuildFailedServer();

        // 2. Act: Rufe die Seite auf, die den kaputten Service nutzt
        var response = await clientWithCrash.GetAsync("/Index");

        // 3. Assert
        Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.InternalServerError));

        var htmlContent = await response.Content.ReadAsStringAsync();
        var parser = new HtmlParser();
        var document = await parser.ParseDocumentAsync(htmlContent);

        //hero-section text-center => Es ist etwas schiefgelaufen
        //<h2 class="mb-3">Interner Serverfehler</h2>
        
        var headingElement = document.QuerySelector("#ErrorHeading");
        Assert.That(headingElement?.TextContent, Does.Contain("Etwas ist schiefgelaufen"));

        // Hier prüfst du auf Elemente deiner normalen "Pages/Error.cshtml"
        var explanationElement = document.QuerySelector("#ErrorExplenation");
        Assert.That(explanationElement?.TextContent, Does.Contain("Interner Serverfehler"));
    }

    private HttpClient BuildFailedServer()
    {
        Environment.SetEnvironmentVariable("MODE_TYPE", "Testing");

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.UseSolutionRelativeContentRoot("FuelDistanceCalculator");

                // Override configuration BEFORE app build
                builder.ConfigureAppConfiguration((context, config) =>
                {
                    var dict = new Dictionary<string, string>
                    {
                        // ensure test has values so GeoLocationService doesn’t throw
                        ["ApiSettings:TankApiKey"] = string.Empty,
                        ["ApiSettings:OpenRouteServiceApiKey"] = string.Empty,
                        ["ApiSettings:OilPriceApiKey"] = string.Empty,
                        ["Redis:Configuration"] = "" // avoid actual Redis config
                    };
                    config.AddInMemoryCollection(dict);
                });

                builder.ConfigureTestServices(services =>
                {
                    // DataProtection auf In-Memory umstellen – kein Filesystem nötig
                    services.AddDataProtection()
                        .UseEphemeralDataProtectionProvider();

                    // Mock RedisCache completely
                    services.RemoveAll(typeof(IDistributedCache));
                    services.AddSingleton<IDistributedCache>(_ => new Mock<IDistributedCache>().Object);

                    services.AddSingleton<FuelPriceService>(_ => new FuelPriceService());
                    services.AddHttpClient<IMarketFuelPriceService, MarketFuelPriceService>();
                    services.AddScoped<IGeoLocationService, GeoLocationService>();
                });
            });

        return _factory.CreateClient();
    }
}