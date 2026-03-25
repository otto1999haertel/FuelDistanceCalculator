namespace FuelDistanceCalculatorTest.ServiceTests;

[TestFixture]
public class ApiKeyTest
{
    private IConfiguration _config;

    [SetUp]
    public void Setup()
    {
        _config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
    }

    [Test]
    public void ApiKeysMustBePresentInConfiguration()
    {
        // Act: Lese die Keys aus der Config
        string tankKey = _config["ApiSettings:TankApiKey"];
        string openrKey = _config["ApiSettings:OpenRouteServiceApiKey"];

        // Assert: Keys müssen gesetzt sein
        Assert.That(!string.IsNullOrWhiteSpace(tankKey),
            $"TankApiKey should not be empty. Current value: '{tankKey}'");
        Assert.That(!string.IsNullOrWhiteSpace(openrKey),
            $"OpenRouteServiceApiKey should not be empty. Current value: '{openrKey}'");
    }

    [Test]
    [Category("CI")] // Nur in CI ausführen
    public void ApiKeysInCIMustBeProductionKeys()
    {
        // Dieser Test läuft NUR in der CI-Pipeline
        bool isCI = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CI"))
                    || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));

        if (!isCI)
        {
            Assert.Ignore("Test only runs in CI environment");
            return;
        }

        string tankKey = _config["ApiSettings:TankApiKey"];
        string openrKey = _config["ApiSettings:OpenRouteServiceApiKey"];

        // Prüfe, dass es KEINE Fake-Keys sind
        var fakeKeyPrefixes = new[] { "fake", "test", "local", "dev", "dummy", "sample" };

        Assert.That(!fakeKeyPrefixes.Any(prefix =>
            tankKey?.ToLower().Contains(prefix) ?? false),
            $"TankApiKey appears to be a fake/test key in CI: '{tankKey}'");

        Assert.That(!fakeKeyPrefixes.Any(prefix =>
            openrKey?.ToLower().Contains(prefix) ?? false),
            $"OpenRouteServiceApiKey appears to be a fake/test key in CI: '{openrKey}'");

        // Keys sollten echte API-Key-Länge haben
        Assert.That(tankKey?.Length >= 20,
            $"TankApiKey seems too short for production: {tankKey?.Length} chars");
        Assert.That(openrKey?.Length >= 20,
            $"OpenRouteServiceApiKey seems too short for production: {openrKey?.Length} chars");

        Console.WriteLine("✓ Production API keys verified in CI environment");
    }
}