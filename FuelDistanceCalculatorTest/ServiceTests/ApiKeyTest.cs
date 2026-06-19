namespace FuelDistanceCalculatorTest.ServiceTests;

[TestFixture]
public class ApiKeyTest
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
    public void ApiKeys_ArePresent()
    {
        string tankKey = _config["ApiSettings:TankApiKey"];
        string openrKey = _config["ApiSettings:OpenRouteServiceApiKey"];

        Assert.That(!string.IsNullOrWhiteSpace(tankKey));
        Assert.That(!string.IsNullOrWhiteSpace(openrKey));

        Assert.That(tankKey.Length >= 10);
        Assert.That(openrKey.Length >= 10);
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