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
    Console.WriteLine("Lat " + latitude);
    Console.WriteLine("Long " + longitude);
    Console.WriteLine("Radius " + radius);
    Console.WriteLine("Fueltype " + fueltype);  
    Console.WriteLine("API Key " + _apiKey); 
    
    var requestUrl = $"https://creativecommons.tankerkoenig.de/json/list.php?lat={latitude}&lng={longitude}&rad={radius}&sort=dist&type={fueltype}&apikey={_apiKey}";

    var response = await _httpClient.GetAsync(requestUrl);

    if (!response.IsSuccessStatusCode)
    {
        //throw new Exception($"API request failed with status code {response.StatusCode}");
        //TODO return Statuscode via Out Parameter
        return new GasStationResult
        {
            IsSuccess = false,
            ErrorMessage = $"API request failed with HTTP status code {response.StatusCode}",
            Stations = new List<GasStation>()
        };
    }

    var responseContent = await response.Content.ReadAsStringAsync();
    Console.WriteLine("Response in Service: " + responseContent);
    ResponseModelMarketFuelPriceService? error = null;
    try
        {
            error = JsonSerializer.Deserialize<ResponseModelMarketFuelPriceService>(responseContent, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
    catch{
        Console.WriteLine("Fail during desrialization");
    }
    if(!error.ok){
        return new GasStationResult
            {
                IsSuccess = false,
                ErrorMessage = $"{error.message}",
                Stations = new List<GasStation>()
            };
    }
    
    var gasStationResponse = JsonSerializer.Deserialize<GasStationResponse>(responseContent, new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true
    });

    // Filter the gas stations where IsOpen is true
    //Gets every gas station
    List<GasStation> openStations = gasStationResponse?.Stations?
    .Where(station => station.IsOpen && station.Price.HasValue && station.Distance.HasValue)
    .ToList() ?? new List<GasStation>();
    foreach(GasStation gS in openStations){
        Console.WriteLine("Open Gasstations in Service " + gS.ToString());
    }
        return new GasStationResult
        {
            IsSuccess = true,
            Stations = openStations
        };
    }
}
