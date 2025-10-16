using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

public class GeoLocationService
{
    private readonly IDatabase _redisDb;
    private readonly HttpClient _httpClient;

    // Cache-Zeit (1 Jahr)
    private readonly TimeSpan cacheDuration = TimeSpan.FromDays(365);

    private readonly string _apiKey;

    private readonly string _mode;

    public GeoLocationService(IHttpClientFactory httpClientFactory, IConfiguration configuration, string mode)
    {
        _httpClient = httpClientFactory.CreateClient();

        // Verbindung zu Redis herstellen (StackExchange.Redis)
        var redis = ConnectionMultiplexer.Connect("redis:6379"); // Falls Docker, sonst "localhost:6379"
        _redisDb = redis.GetDatabase();
        _apiKey = configuration["ApiSettings:OpenRouteServiceApiKey"]
                  ?? throw new Exception("API Key missing");
        _mode = mode;
    }

    public async Task<CoordinatesDTO> GetCoordinatesAsync(string place)
    {
        if (place == null || place.Trim().Equals(string.Empty)) return null;
        place = NormalizeAddressKey(place);
        string cacheKey = $"geo:{place.ToLower()}";

        // 🔍 Prüfe, ob Daten als Hash im Redis-Cache vorhanden sind
        var cachedData = await _redisDb.HashGetAllAsync(cacheKey);
        if (cachedData.Length > 0)
        {
            Console.WriteLine($"[Redis HIT for place]  {place}!");

            return new CoordinatesDTO
            {
                Latitude = double.Parse(cachedData.FirstOrDefault(x => x.Name == "lat").Value),
                Longitude = double.Parse(cachedData.FirstOrDefault(x => x.Name == "lon").Value)
            };
        }

        Console.WriteLine($" Cache-Miss für {place}, API wird aufgerufen...");
        var coordinates = await FetchCoordinatesFromApi(place);

        if (coordinates == null) return null;

        // 🚀 Speichern in Redis als Hash (1 Jahr Cache-Zeit)
        await _redisDb.HashSetAsync(cacheKey, new HashEntry[]
        {
            new HashEntry("lat", coordinates.Latitude),
            new HashEntry("lon", coordinates.Longitude)
        });

        // Ablaufzeit setzen (optional)
        await _redisDb.KeyExpireAsync(cacheKey, cacheDuration);

        // Reverse-Cache setzen (Koordinaten -> Ort)
        string latKey = coordinates.Latitude.ToString("F3", CultureInfo.InvariantCulture);
        string lonKey = coordinates.Longitude.ToString("F3", CultureInfo.InvariantCulture);
        string reverseKey = $"geo:reverse:{latKey}:{lonKey}";

        await _redisDb.StringSetAsync(reverseKey, place, cacheDuration);

        return coordinates;
    }

    public async Task<string> GetAddressFromCoordinatesAsync(double latitude, double longitude)
    {
        string latKey = latitude.ToString("F3", CultureInfo.InvariantCulture);
        string lonKey = longitude.ToString("F3", CultureInfo.InvariantCulture);
        string cacheKey = $"geo:reverse:{latKey}:{lonKey}";

        // Prüfe Redis-Cache
        var cachedAddress = await _redisDb.StringGetAsync(cacheKey);
        if (cachedAddress.HasValue)
        {
            Console.WriteLine($"[Redis HIT for coordinates] {cacheKey}");
            return cachedAddress;
        }

        Console.WriteLine($"[Redis MISS] {cacheKey}");
        var url = $"https://nominatim.openstreetmap.org/reverse?lat={latKey}&lon={lonKey}&format=json";
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("User-Agent", "FuelGo/1.0");

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception("Fehler beim Reverse Geocoding");
        }

        var json = JObject.Parse(await response.Content.ReadAsStringAsync());
        var address = json["address"];
        if (address == null) return null;

        // Adresse zusammenbauen
        string road = address["road"]?.ToString();
        string house = address["house_number"]?.ToString();
        string postcode = address["postcode"]?.ToString();
        string city = address["city"]?.ToString() ?? address["town"]?.ToString() ?? address["village"]?.ToString();

        string fullAddress = $"{road} {house}, {postcode} {city}".Trim();

        // Caching in Redis
        if (!string.IsNullOrWhiteSpace(fullAddress))
        {
            // Reverse-Cache setzen (Koordinaten -> Adresse)
            await _redisDb.StringSetAsync(cacheKey, fullAddress, cacheDuration);

            // Forward-Cache setzen (Ort -> Koordinaten), falls noch nicht vorhanden
            string forwardKey = $"geo:{NormalizeAddressKey(fullAddress)}";
            bool forwardExists = await _redisDb.KeyExistsAsync(forwardKey);

            if (!forwardExists)
            {
                await _redisDb.HashSetAsync(forwardKey, new HashEntry[]
                {
                new HashEntry("lat", latitude),
                new HashEntry("lon", longitude)
                });
                await _redisDb.KeyExpireAsync(forwardKey, cacheDuration);
            }
        }

        return fullAddress;
    }

    public async Task<double> CalculateDistance(string latitudeStart, string longitudeStart, string latitudeEnd, string longitudeEnd)
    {
        var url = $"https://api.openrouteservice.org/v2/directions/driving-car?api_key={_apiKey}&start={longitudeStart},{latitudeStart}&end={longitudeEnd},{latitudeEnd}"; // Example: Munich center
        Console.WriteLine($"Routing API Request: {url}");
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("User-Agent", "FuelGo/1.0");
        string responseString = "";
        if (_mode == "Production")
        {
            var response = await _httpClient.SendAsync(request);
            Console.WriteLine("Response from routing service " + response.StatusCode);
            if (response.IsSuccessStatusCode)
            {
                responseString = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Response String Routing Service: " + responseString);
            }
            else
            {
                return -1;
            }
        }
        else
        {
            // Development/Test: Lade JSON aus File
            string jsonFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "Routing_Service_API_response.json");
            if (!File.Exists(jsonFilePath))
            {
                throw new FileNotFoundException($"JSON-File nicht gefunden: {jsonFilePath}");
            }
            responseString = await File.ReadAllTextAsync(jsonFilePath);
        }
        using JsonDocument doc = JsonDocument.Parse(responseString);
        JsonElement root = doc.RootElement;

        double totalDistance = root
            .GetProperty("features")[0]
            .GetProperty("properties")
            .GetProperty("summary")
            .GetProperty("distance")
            .GetDouble();
        Console.WriteLine($"Calculated distance: {totalDistance} meters");
        return Math.Round(totalDistance / 1000.0, 2); // Convert to kilometers
    }

    private async Task<CoordinatesDTO> FetchCoordinatesFromApi(string place)
    {
        var url = $"https://nominatim.openstreetmap.org/search?q={place}&format=json";
        Console.WriteLine($"API Request: {url}");

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("User-Agent", "FuelGo/1.0");

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            //TODO return status code via out parameter
            throw new Exception("Error fetching coordinates");
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        var json = JArray.Parse(responseContent);

        if (json.Count > 0)
        {
            return new CoordinatesDTO
            {
                Latitude = double.Parse(json[0]["lat"].ToString()),
                Longitude = double.Parse(json[0]["lon"].ToString())
            };
        }

        return null;
    }

    private string NormalizeAddressKey(string place)
    {
        if (string.IsNullOrWhiteSpace(place))
            return "";

        var normalized = place
            .Trim()
            .ToLowerInvariant()
            .Replace("ß", "ss")
            .Replace("ä", "ae")
            .Replace("ö", "oe")
            .Replace("ü", "ue");

        // Mehrfache Leerzeichen durch eines ersetzen
        normalized = Regex.Replace(normalized, @"\s+", " ");

        // Sonderzeichen rausfiltern (optional)
        normalized = Regex.Replace(normalized, @"[^a-z0-9\s,.-]", "");

        return normalized;
    }
}
