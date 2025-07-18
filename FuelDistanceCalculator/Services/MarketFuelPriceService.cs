using System;
using System.Text.Json;

namespace FuelDistanceCalculator.Services;

public class MarketFuelPriceService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    public MarketFuelPriceService(IConfiguration configuration, HttpClient httpClient)
    {
        _httpClient = httpClient;
        _apiKey = configuration["ApiSettings:TankApiKey"]
                  ?? throw new Exception("API Key missing");
    }
    
    public async Task<GasStationResult> GetGasStationsAsync(double latitude, double longitude, double radius, string fueltype)
{
    Console.WriteLine("Called from API method");
    Console.WriteLine($"Lat {latitude}, Long {longitude}, Radius {radius}, Fueltype {fueltype}");
    Console.WriteLine("API Key " + _apiKey); 
    
    var requestUrl = $"https://creativecommons.tankerkoenig.de/json/list.php?lat={latitude}&lng={longitude}&rad={radius}&sort=dist&type={fueltype}&apikey={_apiKey}";

    try
    {
        var response = await _httpClient.GetAsync(requestUrl);

        if (!response.IsSuccessStatusCode)
        {
            return new GasStationResult
            {
                IsSuccess = false,
                ErrorMessage = $"API request failed with HTTP status code {response.StatusCode}",
                Stations = new List<GasStation>()
            };
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        Console.WriteLine("Response in Service: " + responseContent);

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
