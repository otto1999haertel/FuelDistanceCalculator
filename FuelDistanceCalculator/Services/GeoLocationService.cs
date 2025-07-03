using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;

public class GeoLocationService
{
    private readonly IDatabase _redisDb;
    private readonly HttpClient _httpClient;
    
    // Cache-Zeit (1 Jahr)
    private readonly TimeSpan cacheDuration = TimeSpan.FromDays(365);

    public GeoLocationService(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient();
        
        // Verbindung zu Redis herstellen (StackExchange.Redis)
        var redis = ConnectionMultiplexer.Connect("redis:6379"); // Falls Docker, sonst "localhost:6379"
        _redisDb = redis.GetDatabase();
    }

    public async Task<CoordinatesDTO> GetCoordinatesAsync(string place)
    {
        if(place==null || place.Trim().Equals(string.Empty)) return null;
        string cacheKey = $"geo:{place.ToLower()}";

        // 🔍 Prüfe, ob Daten als Hash im Redis-Cache vorhanden sind
        var cachedData = await _redisDb.HashGetAllAsync(cacheKey);
        if (cachedData.Length > 0)
        {
            Console.WriteLine($"Cache-Hit für {place}!");

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
                Console.WriteLine($"[Redis HIT] {cacheKey}");
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
                await _redisDb.StringSetAsync(cacheKey, fullAddress, cacheDuration);
            }

            return fullAddress;
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
}
