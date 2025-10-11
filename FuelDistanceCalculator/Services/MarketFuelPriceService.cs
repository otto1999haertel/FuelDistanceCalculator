using System;
using System.Text.Json;
using FluentMigrator.Builders.IfDatabase;

namespace FuelDistanceCalculator.Services;

public class MarketFuelPriceService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _mode;
    public MarketFuelPriceService(IConfiguration configuration, HttpClient httpClient)
    {
        _httpClient = httpClient;
        _apiKey = configuration["ApiSettings:TankApiKey"]
                  ?? throw new Exception("API Key missing");
        _mode = Environment.GetEnvironmentVariable("MODE_TYPE");
    }
    
    public async Task<GasStationResult> GetGasStationsAsync(double latitude, double longitude, double radius, string fueltype)
{
    Console.WriteLine("Called from API method");
    Console.WriteLine($"Lat {latitude}, Long {longitude}, Radius {radius}, Fueltype {fueltype}");
    Console.WriteLine("API Key " + _apiKey); 
    
    var requestUrl = $"https://creativecommons.tankerkoenig.de/json/list.php?lat={latitude}&lng={longitude}&rad={radius}&sort=dist&type={fueltype}&apikey={_apiKey}";

    try
    {
        if(DateTime.Now.Second==0 && DateTime.Now.Minute%5==0){
            await Task.Delay(new Random().Next(400, 750));
        }
        string responseContent;
        Console.WriteLine("Mode " + _mode);
            if (_mode == "Production")
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
        Console.WriteLine("API Response: " + responseContent);

        // Versuche, allgemeines Fehlerobjekt zu lesen
        var errorCheck = JsonSerializer.Deserialize<ResponseModelMarketFuelPriceService>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (errorCheck == null || !errorCheck.ok)
        {
            return new GasStationResult
            {
                IsSuccess = false,
                ErrorMessage = errorCheck?.message ?? "API returned error without message",
                Stations = new List<GasStation>()
            };
        }

        // Versuche, die eigentlichen Tankstellen-Daten zu lesen
        var gasStationResponse = JsonSerializer.Deserialize<GasStationResponse>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        List<GasStation> openStations = gasStationResponse?.Stations?
            .Where(station => station.IsOpen && station.Price.HasValue && station.Distance.HasValue)
            .ToList() ?? new List<GasStation>();

        foreach (GasStation gS in openStations)
        {
            Console.WriteLine("Open Gasstations in Service " + gS.ToString());
        }

        return new GasStationResult
        {
            IsSuccess = true,
            Stations = openStations
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
