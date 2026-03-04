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
    private readonly ILogger<MarketFuelPriceService> _logger;

    public MarketFuelPriceService(IConfiguration configuration, HttpClient httpClient, IGeoLocationService geoLocationService, ILogger<MarketFuelPriceService> logger)
    {
        _httpClient = httpClient;
        _apiKey = configuration["ApiSettings:TankApiKey"]
                  ?? throw new Exception("API Key missing");
        _mode = Environment.GetEnvironmentVariable("MODE_TYPE");
        _geoLocationService = geoLocationService;
        _logger = logger;
    }

    public async Task<GasStationResult> GetGasStationsAsync(double latitude, double longitude, double radius, string fueltype, string brand, decimal discount)
    {
        _logger.LogInformation("Called from Fuel API method with Thread {ThreadId}", Thread.CurrentThread.ManagedThreadId);
        _logger.LogInformation("Lat {Latitude}, Long {Longitude}, Radius {Radius}, Fueltype {FuelType}", latitude, longitude, radius, fueltype);

        var requestUrl = $"https://creativecommons.tankerkoenig.de/api/v4/stations/search?apikey={_apiKey}&lat={latitude}&lng={longitude}&rad={radius}";
        _logger.LogInformation("Request URL: {RequestUrl}", requestUrl);
        try
        {
            if (DateTime.Now.Second == 0 && DateTime.Now.Minute % 5 == 0)
            {
                await Task.Delay(new Random().Next(400, 750));
            }
            string responseContent;
            _logger.LogInformation("Mode: {Mode}", _mode);
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
            _logger.LogInformation("API Response received, length: {Length}", responseContent.Length);

            // Versuche, allgemeines Fehlerobjekt zu lesen
            // Versuche, die eigentlichen Tankstellen-Daten zu lesen
            GasStationResponse gasStationResponse = null;
            try
            {
                gasStationResponse = JsonSerializer.Deserialize<GasStationResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                _logger.LogInformation("Deserialization successful. Stations count: {Count}", gasStationResponse?.Stations?.Count ?? 0);
            }
            catch (JsonException ex)
            {
                _logger.LogError("Deserialization error: {Message}", ex.Message);
                // Logge responseContent hier, um das JSON zu inspizieren
            }
            catch (Exception ex)
            {
                _logger.LogError("General error during deserialization: {Message}", ex.Message);
            }
            List<GasStation> openStations = gasStationResponse?.Stations?
                .Where(station => station.IsOpen && station.Fuels.Any(x => !x.Name.IsNullOrEmpty() && x.Price.HasValue) && station.Dist.HasValue)
                .ToList() ?? new List<GasStation>();
            foreach (GasStation gS in openStations)
            {
                _logger.LogDebug("Processing open gas station: {Station}", gS.ToString());
                _logger.LogDebug("Setting price for fuel type: {FuelType}", fueltype);
                gS.SetPrice(fueltype, brand, discount);
                gS.SetUpdateTime(fueltype);
                gS.SetUpdateAmount(fueltype);
                _logger.LogDebug("Distance before calculation: {Distance}", gS.Dist);
            }


            return new GasStationResult
            {
                IsSuccess = true,
                Stations = openStations.Where(station => station.FuelTypePrice > 0).ToList(),
            };
        }
        catch (HttpRequestException httpEx)
        {
            _logger.LogError("HTTP error: {Message}", httpEx.Message);
            return new GasStationResult
            {
                IsSuccess = false,
                ErrorMessage = "Verbindungsfehler zur Tankstellen-API: " + httpEx.Message,
                Stations = new List<GasStation>()
            };
        }
        catch (JsonException jsonEx)
        {
            _logger.LogError("Deserialization error: {Message}", jsonEx.Message);
            return new GasStationResult
            {
                IsSuccess = false,
                ErrorMessage = "Fehler beim Verarbeiten der API-Antwort.",
                Stations = new List<GasStation>()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError("General error: {Message}", ex.Message);
            return new GasStationResult
            {
                IsSuccess = false,
                ErrorMessage = "Ein unerwarteter Fehler ist aufgetreten.",
                Stations = new List<GasStation>()
            };
        }
    }
}
