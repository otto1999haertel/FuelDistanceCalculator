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
    public void KeysMustBePresentInConfiguration()
    {
        // Act: Lese die Keys aus der Config
        string tankKey = _config["ApiSettings:TankApiKey"];
        string openrKey = _config["ApiSettings:OpenRouteServiceApiKey"];
        string redisPassword = _config["REDIS_PASSWORD"];

        // Assert: Keys müssen gesetzt sein
        Assert.That(!string.IsNullOrWhiteSpace(tankKey),
            $"TankApiKey should not be empty. Current value: '{tankKey}'");
        Assert.That(!string.IsNullOrWhiteSpace(openrKey),
            $"OpenRouteServiceApiKey should not be empty. Current value: '{openrKey}'");
        Assert.That(!string.IsNullOrWhiteSpace(redisPassword),
            $"REDIS_PASSWORD should not be empty. Current value: '{redisPassword}'");
    }

    [Test]
    [Category("CI")] // Nur in CI ausführen
    public void KeysInCIMustBeProductionKeys()
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
        string redisPassword = _config["REDIS_PASSWORD"];

        // Prüfe, dass es KEINE Fake-Keys sind
        var fakeKeyPrefixes = new[] { "fake", "test", "local", "dev", "dummy", "sample" };

        Assert.That(!fakeKeyPrefixes.Any(prefix =>
            tankKey?.ToLower().Contains(prefix) ?? false),
            $"TankApiKey appears to be a fake/test key in CI: '{tankKey}'");

        Assert.That(!fakeKeyPrefixes.Any(prefix =>
            openrKey?.ToLower().Contains(prefix) ?? false),
            $"OpenRouteServiceApiKey appears to be a fake/test key in CI: '{openrKey}'");

        Assert.That(!fakeKeyPrefixes.Any(prefix =>
            redisPassword?.ToLower().Contains(prefix) ?? false),
            $"REDIS_PASSWORD appears to be a fake/test password in CI: '{redisPassword}'");

        // Keys sollten echte API-Key-Länge haben
        Assert.That(tankKey?.Length >= 20,
            $"TankApiKey seems too short for production: {tankKey?.Length} chars");
        Assert.That(openrKey?.Length >= 20,
            $"OpenRouteServiceApiKey seems too short for production: {openrKey?.Length} chars");

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