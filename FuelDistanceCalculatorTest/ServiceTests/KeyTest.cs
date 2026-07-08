namespace FuelDistanceCalculatorTest.ServiceTests;

[TestFixture]
public class KeyTest
{
    private IConfiguration _config;

    [SetUp]
    public void Setup()
    {
        LoadEnvFile(".env.local");

        var envMappings = new Dictionary<string, string?>
        {
            ["ApiSettings:TankApiKey"] = Environment.GetEnvironmentVariable("TANK_API_KEY"),
            ["ApiSettings:OpenRouteServiceApiKey"] = Environment.GetEnvironmentVariable("OPENROUTESERVICE_API_KEY"),
        };

        _config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: true)
            .AddEnvironmentVariables()
            .AddInMemoryCollection(envMappings)
            .Build();
    }

    [Test]
    [TestCase("ApiSettings:TankApiKey", "TANK_API_KEY")]
    [TestCase("ApiSettings:OpenRouteServiceApiKey", "OPENROUTESERVICE_API_KEY")]
    [TestCase("REDIS_PASSWORD", "REDIS_PASSWORD")]
    public void KeysMustBePresentInConfiguration(string keyName, string configKeys)
    {
        // Act: Lese die Keys aus der Config
        string Key = _config[keyName];

        // Assert: Keys müssen gesetzt sein
        Assert.That(!string.IsNullOrWhiteSpace(Key),
            $"{configKeys} should not be empty. Current value: '{Key}'");
    }

    [Test]
    [Category("CI")] // Nur in CI ausführen
    [TestCase("ApiSettings:TankApiKey", "TANK_API_KEY")]
    [TestCase("ApiSettings:OpenRouteServiceApiKey", "OPENROUTESERVICE_API_KEY")]
    [TestCase("REDIS_PASSWORD", "REDIS_PASSWORD")]
    public void KeysInCIMustBeProductionKeys(string keyName, string configKeys)
    {
        // Dieser Test läuft NUR in der CI-Pipeline
        bool isCI = !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("CI"))
                    || !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"));

        if (!isCI)
        {
            Assert.Ignore("Test only runs in CI environment");
            return;
        }

        string key = _config[keyName];

        // Prüfe, dass es KEINE Fake-Keys sind
        var fakeKeyPrefixes = new[] { "fake", "test", "local", "dev", "dummy", "sample" };

        Assert.That(!fakeKeyPrefixes.Any(prefix =>
            keyName?.ToLower().Contains(prefix) ?? false),
            $"TankApiKey appears to be a fake/test key in CI: '{configKeys}'");

        // Keys sollten echte API-Key-Länge haben
        Assert.That(key?.Length >= 20,
            $"TankApiKey seems too short for production: {key?.Length} chars");

        Console.WriteLine("✓ Production API keys verified in CI environment");
    }

    private static void LoadEnvFile(string filePath)
    {
        // Suche die .env-Datei vom Test-Verzeichnis aus nach oben
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir != null)
        {
            var envFile = Path.Combine(dir.FullName, filePath);
            if (File.Exists(envFile))
            {
                foreach (var line in File.ReadAllLines(envFile))
                {
                    // Kommentare und leere Zeilen überspringen
                    if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                        continue;

                    var parts = line.Split('=', 2);
                    if (parts.Length == 2)
                    {
                        var key = parts[0].Trim();
                        var value = parts[1].Trim();
                        // Nur setzen wenn noch nicht gesetzt (echte Env-Vars haben Vorrang)
                        if (Environment.GetEnvironmentVariable(key) == null)
                            Environment.SetEnvironmentVariable(key, value);
                    }
                }
                return;
            }
            dir = dir.Parent;
        }
    }
}