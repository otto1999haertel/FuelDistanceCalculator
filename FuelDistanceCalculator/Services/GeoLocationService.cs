using FuelDistanceCalculator.Model;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace FuelDistanceCalculator.Services;

public class GeoLocationService : IGeoLocationService
{
    private readonly IDatabase _redisDb;
    private readonly HttpClient _httpClient;

    private readonly TimeSpan cacheDuration = TimeSpan.FromDays(365);

    private readonly string _apiKey;

    private readonly string _mode;

    private readonly string _requestSuccess;
    private const double EarthRadiusMeters = 6371000; // Erdradius in Metern

    private double ToRadians(double degrees) => degrees * Math.PI / 180;

    private double ToDegrees(double radians) => radians * 180 / Math.PI;

    private double NormalizeAngle(double angle) => (angle % 360 + 360) % 360;   

    public GeoLocationService(IHttpClientFactory httpClientFactory, IConfiguration configuration, IConnectionMultiplexer redis)
    {
        _httpClient = httpClientFactory.CreateClient();
        _redisDb = redis.GetDatabase();
        var apiKeyFromConfig = configuration["ApiSettings:OpenRouteServiceApiKey"];
        _apiKey = string.IsNullOrEmpty(apiKeyFromConfig) ? throw new Exception("API Key missing") : apiKeyFromConfig;
        _mode = Environment.GetEnvironmentVariable("MODE_TYPE") ?? "Production";
        _requestSuccess = configuration["RequestSuccess"] ?? "true";
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
            Console.WriteLine("lat:" + cachedData.FirstOrDefault(x => x.Name == "lat").Value);
            Console.WriteLine("lon:" + cachedData.FirstOrDefault(x => x.Name == "lon").Value);

            return new CoordinatesDTO
            {
                Latitude = double.Parse(cachedData.First(x => x.Name == "lat").Value.ToString(), CultureInfo.InvariantCulture),
                Longitude = double.Parse(cachedData.First(x => x.Name == "lon").Value.ToString(), CultureInfo.InvariantCulture)
            };
        }

        Console.WriteLine($" Cache-Miss für {place}, API wird aufgerufen...");
        var coordinates = await FetchCoordinatesFromApi(place);

        if (coordinates == null) return null;

        // 🚀 Speichern in Redis als Hash (1 Jahr Cache-Zeit)
        await _redisDb.HashSetAsync(cacheKey, new HashEntry[]
        {
            new HashEntry("lat", coordinates.Latitude.ToString("F3", CultureInfo.InvariantCulture)),
            new HashEntry("lon", coordinates.Longitude.ToString("F3", CultureInfo.InvariantCulture))
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

        public async Task<List<GasStation>> CalculateDistanceFromAPI(double latitudeStart, double longitudeStart, List<GasStation> stations)
    {
        Console.WriteLine($"Calculating routing distances from {latitudeStart}, {longitudeStart}");
        foreach (GasStation station in stations)
        {
            string responseString = await GetRouteAndDistanceFromAPI(latitudeStart, longitudeStart, station.Coords.Lat, station.Coords.Lng);

            if (!string.IsNullOrEmpty(responseString))
            {
                using JsonDocument doc = JsonDocument.Parse(responseString);
                JsonElement root = doc.RootElement;

                double totalDistance = root
                    .GetProperty("features")[0]
                    .GetProperty("properties")
                    .GetProperty("summary")
                    .GetProperty("distance")
                    .GetDouble();
                station.Dist = Math.Round(totalDistance / 1000.0, 2); // in km
                 station.IsRoutingDistanceCalculated = true;
                Console.WriteLine($"Calculated routing distance: {station.Dist} meters for Gas Station {station.Name}");
            }
            else
            {
                station.IsRoutingDistanceCalculated = false;
            }
        }
        return stations;
    }
    public async Task<List<CoordinatesDTO>> GetRouteIncludingStartPoint(double startLatitude, double startLong, double endLatitude, double endLongitude)
    {
        string response = await GetRouteAndDistanceFromAPI(startLatitude, startLong, endLatitude, endLongitude, "Routing_Service_Big_Route_response.json");
        List<CoordinatesDTO> result = new List<CoordinatesDTO>();
        CoordinatesDTO coordinatesStart = new CoordinatesDTO();
        coordinatesStart.Latitude = startLatitude;
        coordinatesStart.Longitude = startLong;
        result.Add(coordinatesStart);
        if (!string.IsNullOrEmpty(response))
        {
            using JsonDocument doc = JsonDocument.Parse(response);
            JsonElement root = doc.RootElement;
            var coords = root
            .GetProperty("features")[0]
            .GetProperty("geometry")
            .GetProperty("coordinates")
            .EnumerateArray();
            foreach (var coord in coords)
            {
                CoordinatesDTO coordinatesDTO = new CoordinatesDTO();
                coordinatesDTO.Longitude = coord[0].GetDouble();
                coordinatesDTO.Latitude = coord[1].GetDouble();
                result.Add(coordinatesDTO);
            }
        }
        return result;

    }

    public List<CoordinatesDTO> GetSearchPoints(List<CoordinatesDTO> route, double maxTotalDistanceKm,  double intervalKm)
    {
        if(maxTotalDistanceKm>15 || maxTotalDistanceKm / intervalKm > 5)
        {
            return new List<CoordinatesDTO>();
        }
        List<CoordinatesDTO> searchPoint = new List<CoordinatesDTO>();
        var searchPoints = new List<CoordinatesDTO>();
        double accumulatedDistance = 0.0;     // Gesamtdistanz vom Start
        double segmentDistance = 0.0;         // Distanz seit letztem Suchpunkt
        double intervalMeters = intervalKm * 1000;
        double maxDistanceMeters = maxTotalDistanceKm * 1000;

        // Startpunkt hinzufügen
        searchPoints.Add(route[0]);

        for (int i = 1; i < route.Count; i++)
        {
            double segment = CalculateDistance(
                route[i - 1].Latitude, route[i - 1].Longitude,
                route[i].Latitude, route[i].Longitude);

            accumulatedDistance += segment;
            segmentDistance += segment;

            // Prüfen: 5 km Intervall
            if (segmentDistance >= intervalMeters)
            {
                searchPoints.Add(route[i]);
                segmentDistance = 0.0; // Reset für nächstes Intervall
            }

            // Prüfen: 15 km Gesamtgrenze
            if (accumulatedDistance > maxDistanceMeters)
                break;
        }

        return searchPoints;
    }
    
    public string NormalizeAddressKey(string place)
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
    
    public bool IsInForwardCone(CoordinatesDTO searchPoint, CoordinatesDTO nextRoutePoint, CoordinatesDTO checkPoint, double maxRadiusKm)
    {
        double distanceKm = CalculateDistance(searchPoint.Latitude, searchPoint.Longitude, checkPoint.Latitude, checkPoint.Longitude); ;
        if (distanceKm > maxRadiusKm) return false;
        double bearingRoute = CalculateBearing(searchPoint, nextRoutePoint);
        double bearingToCheck = CalculateBearing(searchPoint, checkPoint);
        double angleDiff = Math.Abs(NormalizeAngle(bearingToCheck - bearingRoute));
        if (angleDiff > 180) angleDiff = 360 - angleDiff;
        return angleDiff <= 90.0;
    }

    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        double dLat = ToRadians(lat2 - lat1);
        double dLon = ToRadians(lon2 - lon1);

        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return (EarthRadiusMeters * c);
    }
    
    private double CalculateBearing(CoordinatesDTO from, CoordinatesDTO to)
    {
        double lat1 = ToRadians(from.Latitude);
        double lat2 = ToRadians(to.Latitude);
        double dLon = ToRadians(to.Longitude - from.Longitude);

        double y = Math.Sin(dLon) * Math.Cos(lat2);
        double x = Math.Cos(lat1) * Math.Sin(lat2) -
                Math.Sin(lat1) * Math.Cos(lat2) * Math.Cos(dLon);

        double bearing = Math.Atan2(y, x);
        return NormalizeAngle(ToDegrees(bearing));
    }

    private async Task<CoordinatesDTO> FetchCoordinatesFromApi(string place)
    {
        var url = $"https://nominatim.openstreetmap.org/search?q={place}&format=json";
        Console.WriteLine($"API Request: {url}");

        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Add("User-Agent", "FuelGo/1.0");

        var response = await _httpClient.SendAsync(request);
        response.EnsureSuccessStatusCode();

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

    private async Task<string> GetRouteAndDistanceFromAPI(double latitudeStart, double longitudeStart, double latitudeEnd, double longitudeEnd, string jsonFile="Routing_Service_One_Station_response.json")
    {
        string responseString = "";
        Console.WriteLine("Start Coordinates: " + latitudeStart + ", " + longitudeStart);
        Console.WriteLine("End Coordinates: " + latitudeEnd + ", " + longitudeEnd);
        if (_mode == "Production")
        {
            var url = $"https://api.openrouteservice.org/v2/directions/driving-car?api_key={_apiKey}&start={longitudeStart},{latitudeStart}&end={longitudeEnd},{latitudeEnd}"; // Example: Munich center
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("User-Agent", "FuelGo/1.0");
            var response = await _httpClient.SendAsync(request);
            Console.WriteLine("Response from routing service " + response.StatusCode);
            if (response.IsSuccessStatusCode)
            {
                responseString = await response.Content.ReadAsStringAsync();
                Console.WriteLine("Response String Routing Service: " + responseString);
            }
        }
        else
        {
            //TODO: create new JSON File for big route Grossgrabe -> Dresden
            if(_requestSuccess.ToLower() == "false")
            {
                Console.WriteLine("Simulating failed API request for routing service.");
                return responseString;
            }
            string jsonFilePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", jsonFile);
            if (!File.Exists(jsonFilePath))
            {
                throw new FileNotFoundException($"JSON-File nicht gefunden: {jsonFilePath}");
            }
            responseString = await File.ReadAllTextAsync(jsonFilePath);
        }
        return responseString;
    }
}
