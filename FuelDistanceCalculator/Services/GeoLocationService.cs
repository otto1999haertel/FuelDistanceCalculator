using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
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

    private async Task<CoordinatesDTO> FetchCoordinatesFromApi(string place)
    {
        var url = $"https://nominatim.openstreetmap.org/search?q={place}&format=json";
        Console.WriteLine($"API Request: {url}");

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("User-Agent", "YourAppName/1.0 (your@email.com)");

        var response = await _httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
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
