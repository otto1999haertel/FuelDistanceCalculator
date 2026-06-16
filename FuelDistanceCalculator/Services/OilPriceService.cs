using FuelDistanceCalculator.Interafces;
using FuelDistanceCalculator.Model;
using FuelDistanceCalculator.Services.Common;

namespace FuelDistanceCalculator.Services;

public class OilPriceService : BaseService, IOilPriceService
{

    public OilPriceService(IConfiguration configuration, HttpClient httpClient)
    {
        var apiKeyFromConfig = configuration["ApiSettings:OilPriceApiKey"];
        Mode = configuration["MODE_TYPE"] ?? "Production";
        HttpRequestClient = httpClient;
        Console.WriteLine("Mode: " + Mode);
    }
    public async Task<OilPriceResult> GetOilPriceChangeAsync()
    {
        // Simulate fetching oil price change data from an API or database
         Console.WriteLine($"Called from Fuel API method with Thread {Thread.CurrentThread.ManagedThreadId}");


        var requestUrl = $"https://query1.finance.yahoo.com/v8/finance/chart/BZ=F?interval=1d&range=1mo";
        // For demonstration purposes, we return a hardcoded result

        string responseContent;
        try
        {
            if (Mode.Equals("Production"))
            {
                // Production: Echte HTTP-Anfrage
                HttpResponseMessage response = await HttpRequestClient.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();  // Wirft Exception bei Fehlern (z. B. 404)
                responseContent = await response.Content.ReadAsStringAsync();
            }
            else
            {
                // Development/Test: Lade JSON aus File
                string jsonFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "Oil_price_API_response.json");
                if (!File.Exists(jsonFilePath))
                {
                    throw new FileNotFoundException($"JSON-File nicht gefunden: {jsonFilePath}");
                }
                responseContent = await File.ReadAllTextAsync(jsonFilePath);
            }
        }
        catch (HttpRequestException httpEx)
        {
            Console.WriteLine("HTTP error: " + httpEx.Message);
            return new OilPriceResult
            {
                IsSuccess = false,
                ErrorMessage = "Verbindungsfehler zur Tankstellen-API: " + httpEx.Message
            };
        }
        var priceChange = new OilPriceChange(day: 1.5, week: -0.5, month: 2.0, currentPrice: 80.0);
        return new OilPriceResult
        {
            IsSuccess = true,
            PriceChange = priceChange
        };
    }
}