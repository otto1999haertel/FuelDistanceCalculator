using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
using FuelDistanceCalculator.Model;

namespace FuelDistanceCalculator.Services;

public class MarketFuelPriceService : IMarketFuelPriceService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _mode;
    private readonly IGeoLocationService _geoLocationService;
    public MarketFuelPriceService(IConfiguration configuration, HttpClient httpClient, IGeoLocationService geoLocationService)
    {
        _httpClient = httpClient;
        var apiKeyFromConfig = configuration["ApiSettings:TankApiKey"];
        _apiKey = string.IsNullOrEmpty(apiKeyFromConfig) ? throw new Exception("API Key missing") : apiKeyFromConfig;
        _mode = configuration["MODE_TYPE"] ?? "Production";
        _geoLocationService = geoLocationService;
        Console.WriteLine("API Key loaded: " + _apiKey);
        Console.WriteLine("Mode: " + _mode);
    }

    public async Task<GasStationResult> GetGasStationsAsync(double latitude, double longitude, double radius, string fueltype)
    {
        Console.WriteLine($"Called from Fuel API method with Thread {Thread.CurrentThread.ManagedThreadId}");
        Console.WriteLine($"Lat {latitude}, Long {longitude}, Radius {radius}, Fueltype {fueltype}");

        var requestUrl = $"https://creativecommons.tankerkoenig.de/api/v4/stations/search?apikey={_apiKey}&lat={latitude}&lng={longitude}&rad={radius}";
        Console.WriteLine("Request URL: " + requestUrl);
        try
        {
            if (DateTime.Now.Second == 0 && DateTime.Now.Minute % 5 == 0)
            {
                await Task.Delay(new Random().Next(400, 750));
            }
            string responseContent;
            Console.WriteLine("Mode " + _mode);
            if (_mode.Equals("Production"))
            {
                // Production: Echte HTTP-Anfrage
                HttpResponseMessage response = await _httpClient.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();  // Wirft Exception bei Fehlern (z. B. 404)
                responseContent = await response.Content.ReadAsStringAsync();
            }
            else
            {
                // Development/Test: Lade JSON aus File
                string jsonFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "Fuel_price_API_response.json");
                if (!File.Exists(jsonFilePath))
                {
                    throw new FileNotFoundException($"JSON-File nicht gefunden: {jsonFilePath}");
                }
                responseContent = await File.ReadAllTextAsync(jsonFilePath);
            }
            Console.WriteLine("Tank API Response: " + responseContent);

            // Versuche, allgemeines Fehlerobjekt zu lesen
            // Versuche, die eigentlichen Tankstellen-Daten zu lesen
            GasStationResponse gasStationResponse = null;
            try
            {
                gasStationResponse = JsonSerializer.Deserialize<GasStationResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                Console.WriteLine("Deserialisierung erfolgreich. Stations-Anzahl: " + (gasStationResponse?.Stations?.Count ?? 0));
            }
            catch (JsonException ex)
            {
                Console.WriteLine("Deserialisierungs-Fehler: " + ex.Message);
                // Logge responseContent hier, um das JSON zu inspizieren
            }
            catch (Exception ex)
            {
                Console.WriteLine("Allgemeiner Fehler: " + ex.Message);
            }
            List<GasStation> openStations = gasStationResponse?.Stations?
                .Where(station => station.IsOpen && station.Fuels.Any(x => !x.Name.IsNullOrEmpty() && x.Price.HasValue) && station.Dist.HasValue)
                .ToList() ?? new List<GasStation>();
            foreach (GasStation gS in openStations)
            {
                Console.WriteLine("Open Gasstations in Service " + gS.ToString());
                Console.WriteLine("Setting Price for Fuel Type: " + fueltype);
                gS.SetPrice(fueltype);
                gS.SetUpdateTime(fueltype);
                gS.SetUpdateAmount(fueltype);
                Console.WriteLine("Distance before calculation: " + gS.Dist);
                Console.WriteLine("Open Gasstations in Service " + gS.ToString());
            }


            return new GasStationResult
            {
                IsSuccess = true,
                Stations = openStations.Where(station => station.FuelTypePrice > 0).ToList(),
            };
        }
        catch (HttpRequestException httpEx)
        {
            Console.WriteLine("HTTP error: " + httpEx.Message);
            return new GasStationResult
            {
                IsSuccess = false,
                ErrorMessage = "Verbindungsfehler zur Tankstellen-API: " + httpEx.Message,
                Stations = new List<GasStation>()
            };
        }
        catch (JsonException jsonEx)
        {
            Console.WriteLine("Deserialization error: " + jsonEx.Message);
            return new GasStationResult
            {
                IsSuccess = false,
                ErrorMessage = "Fehler beim Verarbeiten der API-Antwort.",
                Stations = new List<GasStation>()
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine("General error: " + ex.Message);
            return new GasStationResult
            {
                IsSuccess = false,
                ErrorMessage = "Ein unerwarteter Fehler ist aufgetreten.",
                Stations = new List<GasStation>()
            };
        }
    }
}