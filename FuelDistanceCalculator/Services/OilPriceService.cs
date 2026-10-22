using FuelDistanceCalculator.Interfaces;
using FuelDistanceCalculator.Model;
using FuelDistanceCalculator.Services.Common;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;

namespace FuelDistanceCalculator.Services;

public class OilPriceService : BaseService, IOilPriceService
{
    private readonly IMemoryCache _cache;
    private const string CacheKey = "brent_oil_price";
    public OilPriceService(IConfiguration configuration, HttpClient httpClient, IMemoryCache cache)
    {
        var apiKeyFromConfig = configuration["ApiSettings:OilPriceApiKey"];
        Mode = configuration["MODE_TYPE"] ?? "Production";
        HttpRequestClient = httpClient;
        _cache = cache;
        Console.WriteLine("Mode: " + Mode);
    }
    public async Task<OilPriceResult> GetOilPriceChangeAsync()
    {
        // Immer zuerst API versuchen
        var result = await FetchAndCalculateAsync();

        if (result.IsSuccess)
        {
            var expiry = Math.Abs(result.PriceChange.Day) >= 10
                ? TimeSpan.FromHours(1)
                : TimeSpan.FromHours(3);

            // Erfolgreiches Ergebnis cachen
            _cache.Set(CacheKey, result, expiry);
            return result;
        }

        // API fehlgeschlagen → Fallback auf Cache
        if (_cache.TryGetValue(CacheKey, out OilPriceResult cached))
        {
            Console.WriteLine("API fehlgeschlagen – gecachter Wert wird verwendet");
            return cached;
        }

        // Weder API noch Cache verfügbar
        Console.WriteLine("API fehlgeschlagen und kein Cache vorhanden");
        return result;
    }

    private async Task<OilPriceResult> FetchAndCalculateAsync()
    {
        Console.WriteLine($"Called from Fuel API method with Thread {Thread.CurrentThread.ManagedThreadId}");

        var requestUrl = "https://query1.finance.yahoo.com/v8/finance/chart/BZ=F?interval=1d&range=1mo";
        var request = new HttpRequestMessage(HttpMethod.Get, requestUrl);
        request.Headers.Add("User-Agent", "FuelGo/1.0");
        string responseContent;
        try
        {
            if (Mode.Equals("Production"))
            {
                HttpResponseMessage response = await HttpRequestClient.SendAsync(request);
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

            var result = doc.RootElement.GetProperty("chart").GetProperty("result")[0];
            var meta = result.GetProperty("meta");

            var currentPrice = meta.GetProperty("regularMarketPrice").GetDecimal();
            var marketTime = meta.GetProperty("regularMarketTime").GetInt64();
            var gmtOffset = TimeSpan.FromSeconds(meta.GetProperty("gmtoffset").GetInt32());

            var utcTime = DateTimeOffset.FromUnixTimeSeconds(marketTime);
            var marketDate = utcTime.ToOffset(gmtOffset).Date;   // aktueller Handelstag (Börsenzeit)

            // Zeitstempel und Schlusskurse paarweise lesen, null-Werte verwerfen
            var timestamps = result.GetProperty("timestamp").EnumerateArray()
                .Select(x => x.GetInt64())
                .ToList();

            var closeValues = result.GetProperty("indicators")
                .GetProperty("quote")[0]
                .GetProperty("close")
                .EnumerateArray()
                .ToList();

            var points = timestamps
                .Zip(closeValues, (t, c) => (Time: DateTimeOffset.FromUnixTimeSeconds(t), Value: c))
                .Where(p => p.Value.ValueKind == JsonValueKind.Number)
                .Select(p => (p.Time, Close: p.Value.GetDecimal()))
                .ToList();

            // Historie = nur abgeschlossene Handelstage (ohne den heutigen, noch laufenden Balken)
            var history = points
                .Where(p => p.Time.ToOffset(gmtOffset).Date < marketDate)
                .ToList();

            if (history.Count < 7)
                return new OilPriceResult
                {
                    IsSuccess = false,
                    ErrorMessage = "Nicht genügend Datenpunkte für Berechnung."
                };

            var today = currentPrice;                       // Live-Preis aus meta
            var yesterday = history[^1].Close;              // letzter abgeschlossener Handelstag

            // Letzter Handelstag, der mindestens 7 Kalendertage zurückliegt
            var lastWeek = history
                .LastOrDefault(p => p.Time <= utcTime.AddDays(-7))
                .Close;
            if (lastWeek == 0) lastWeek = history[0].Close; // Fallback: ältester Wert

            var lastMonth = history[0].Close;               // ältester Wert im 1mo-Bereich

            var germanTimeZone = GetGermanTimeZone();
            var lastUpdated = TimeZoneInfo.ConvertTime(utcTime, germanTimeZone);

            var priceChange = new OilPriceChange(
                day: CalculateChangePct(today, yesterday),
                week: CalculateChangePct(today, lastWeek),
                month: CalculateChangePct(today, lastMonth),
                currentPrice: (double)currentPrice,
                lastUpdated: lastUpdated
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

    private static TimeZoneInfo GetGermanTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin"); // Linux / Docker
        }
        catch
        {
            return TimeZoneInfo.FindSystemTimeZoneById("Central European Standard Time"); // Windows
        }
    }
}