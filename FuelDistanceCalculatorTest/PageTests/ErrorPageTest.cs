using AngleSharp.Html.Parser;
using FuelDistanceCalculator.Interfaces;
using FuelDistanceCalculator.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using StackExchange.Redis;

namespace FuelDistanceCalculatorTest.PageTests;

[TestFixture]
public class ErrorPagesTest : PageTestBase
{
    [Test]
    public async Task WhenRateLimitExceeded_ShouldReturn429WithCustomErrorPageHtml()
    {
        const int permitLimit = 2;
        var client = BuildRateLimitedServer(permitLimit);

        HttpResponseMessage response = null!;
        // Etwas über dem Limit anfragen, bis der Limiter zuschlägt
        for (var i = 0; i < permitLimit + 5; i++)
        {
            response = await client.GetAsync("/Index");
            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                break;
        }

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

        var response = await clientWithCrash.GetAsync("/Index");

        Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.InternalServerError));

        var htmlContent = await response.Content.ReadAsStringAsync();
        var parser = new HtmlParser();
        var document = await parser.ParseDocumentAsync(htmlContent);

        var headingElement = document.QuerySelector("#ErrorHeading");
        Assert.That(headingElement?.TextContent, Does.Contain("Etwas ist schiefgelaufen"));

        var explanationElement = document.QuerySelector("#ErrorExplenation");
        Assert.That(explanationElement?.TextContent, Does.Contain("Interner Serverfehler"));
    }

    private HttpClient BuildRateLimitedServer(int permitLimit)
    {
        Environment.SetEnvironmentVariable("MODE_TYPE", "Testing");

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.UseSolutionRelativeContentRoot("FuelDistanceCalculator");

                builder.ConfigureAppConfiguration((context, config) =>
                {
                    var dict = new Dictionary<string, string>
                    {
                        ["ApiSettings:TankApiKey"] = "test",
                        ["ApiSettings:OpenRouteServiceApiKey"] = "test",
                        ["ApiSettings:OilPriceApiKey"] = "test",
                        ["Redis:Configuration"] = "",
                        ["RateLimit:PermitLimit"] = permitLimit.ToString(),
                        ["RateLimit:UpstreamPermitLimit"] = permitLimit.ToString()
                    };
                    config.AddInMemoryCollection(dict);
                });

                builder.ConfigureTestServices(services =>
                {
                    var mockDatabase = new Mock<IDatabase>();
                    var mockMultiplexer = new Mock<IConnectionMultiplexer>();
                    mockMultiplexer
                        .Setup(c => c.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                        .Returns(mockDatabase.Object);

                    services.RemoveAll<IConnectionMultiplexer>();
                    services.AddSingleton<IConnectionMultiplexer>(mockMultiplexer.Object);

                    services.AddDataProtection()
                        .UseEphemeralDataProtectionProvider();

                    services.RemoveAll(typeof(IDistributedCache));
                    services.AddSingleton<IDistributedCache>(_ => new Mock<IDistributedCache>().Object);

                    services.AddSingleton<FuelPriceService>(_ => new FuelPriceService());
                    services.AddHttpClient<IMarketFuelPriceService, MarketFuelPriceService>();
                    services.AddScoped<IGeoLocationService, GeoLocationService>();
                });
            });

        return _factory.CreateClient();
    }

    private HttpClient BuildFailedServer()
    {
        Environment.SetEnvironmentVariable("MODE_TYPE", "Testing");

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Testing");
                builder.UseSolutionRelativeContentRoot("FuelDistanceCalculator");

                builder.ConfigureAppConfiguration((context, config) =>
                {
                    var dict = new Dictionary<string, string>
                    {
                        ["ApiSettings:TankApiKey"] = string.Empty,
                        ["ApiSettings:OpenRouteServiceApiKey"] = string.Empty,
                        ["ApiSettings:OilPriceApiKey"] = string.Empty,
                        ["Redis:Configuration"] = ""
                    };
                    config.AddInMemoryCollection(dict);
                });

                builder.ConfigureTestServices(services =>
                {
                    services.AddDataProtection()
                        .UseEphemeralDataProtectionProvider();

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