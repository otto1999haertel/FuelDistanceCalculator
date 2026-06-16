using FuelDistanceCalculator.Interafces;
using FuelDistanceCalculator.Model;
using FuelDistanceCalculator.Services.Common;
using System.Text.Json;

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
        Console.WriteLine($"Called from Fuel API method with Thread {Thread.CurrentThread.ManagedThreadId}");

        var requestUrl = "https://query1.finance.yahoo.com/v8/finance/chart/BZ=F?interval=1d&range=1mo";

        string responseContent;
        try
        {
            if (Mode.Equals("Production"))
            {
                HttpResponseMessage response = await HttpRequestClient.GetAsync(requestUrl);
                response.EnsureSuccessStatusCode();
                responseContent = await response.Content.ReadAsStringAsync();
            }
            else
            {
                string jsonFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "Oil_price_API_response.json");
                if (!File.Exists(jsonFilePath))
                    throw new FileNotFoundException($"JSON-File nicht gefunden: {jsonFilePath}");

                responseContent = await File.ReadAllTextAsync(jsonFilePath);
            }
        }
        catch (HttpRequestException httpEx)
        {
            Console.WriteLine("HTTP error: " + httpEx.Message);
            return new OilPriceResult
            {
                IsSuccess = false,
                ErrorMessage = "Verbindungsfehler zur Öl-API: " + httpEx.Message
            };
        }

        try
        {
            using var doc = JsonDocument.Parse(responseContent);

            // Navigiere zu close-Array
            var closes = doc.RootElement
                .GetProperty("chart")
                .GetProperty("result")[0]
                .GetProperty("indicators")
                .GetProperty("quote")[0]
                .GetProperty("close")
                .EnumerateArray()
                .Select(x => x.GetDecimal())
                .ToList();

            // Aktueller Preis direkt aus meta
            var currentPrice = doc.RootElement
                .GetProperty("chart")
                .GetProperty("result")[0]
                .GetProperty("meta")
                .GetProperty("regularMarketPrice")
                .GetDecimal();

            if (closes.Count < 8)
                return new OilPriceResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Nicht genügend Datenpunkte für Berechnung."
                };

            var today = closes[^1];   // aktuellster Wert
            var yesterday = closes[^2];   // gestern
            var lastWeek = closes[^8];   // vor 7 Handelstagen
            var lastMonth = closes[0];    // ältester Wert ~1 Monat

            var priceChange = new OilPriceChange(
                day: CalculateChangePct(today, yesterday),
                week: CalculateChangePct(today, lastWeek),
                month: CalculateChangePct(today, lastMonth),
                currentPrice: (double)currentPrice
            );

            return new OilPriceResult
            {
                IsSuccess = true,
                PriceChange = priceChange
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine("Parse error: " + ex.Message);
            return new OilPriceResult
            {
                IsSuccess = false,
                ErrorMessage = "Fehler beim Verarbeiten der API-Antwort: " + ex.Message
            };
        }
    }

    private static double CalculateChangePct(decimal current, decimal reference)
    {
        if (reference == 0) return 0;
        return (double)Math.Round((current - reference) / reference * 100, 2);
    }
}