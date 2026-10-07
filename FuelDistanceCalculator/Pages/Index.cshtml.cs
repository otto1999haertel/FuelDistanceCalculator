using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using FuelDistanceCalculator.Constants;
using FuelDistanceCalculator.Interfaces;
using FuelDistanceCalculator.Model;
using FuelDistanceCalculator.Services;
using FuelDistanceCalculator.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Newtonsoft.Json;

namespace FuelDistanceCalculator.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private FuelPriceService _fuelPriceService;
    private readonly IMarketFuelPriceService _MarketfuelPriceService;
    private readonly IGeoLocationService _geoLocationService;
    private readonly IOilPriceService _oilPriceService;
    private readonly ConcurrentBag<(string Type, string Message)> _toastMessages = new();
    private readonly IConfiguration _configuration;

    // 0 ist ein gültiger Sentinel-Wert ("Bei 0 wird nach Spritpreisen sortiert")
    [BindProperty, Range(0, 500, ErrorMessage = "Ungültige Tankmenge.")]
    public decimal FuelAmount { get; set; }

    [BindProperty, Range(0.00, 10, ErrorMessage = "Ungültiger Preis/km.")]
    public decimal PricePerKm { get; set; }

    [BindProperty, Range(0, 10, ErrorMessage = "Ungültiger Kraftstoffpreis.")]
    public double FuelPrice1 { get; set; }

    [BindProperty, Range(0, 25)]
    public double Distance2 { get; set; }

    [BindProperty, Range(0, 10, ErrorMessage = "Ungültiger Kraftstoffpreis.")]
    public double FuelPrice2 { get; set; }

    // Whitelist + Count-/Längenlimit
    [BindProperty, PlaceList(maxCount: 10, maxItemLength: 150)]
    public List<string> NamePlaces { get; set; } = new();

    [BindProperty, RadiusList(1, 25)]
    public List<double> RadiusPlaces { get; set; } = new();

    public ConcurrentDictionary<string, decimal> CalculatedAverageCosts { get; set; } = new();

    public double AverageCostPlace1 { get; private set; }
    public double AverageCostPlace2 { get; private set; }

    [BindProperty, EnumDataType(typeof(FuelType), ErrorMessage = "Ungültiger Kraftstofftyp.")]
    public FuelType SelectedFuelType { get; set; }

    [BindProperty, EnumDataType(typeof(InputMode), ErrorMessage = "Ungültiger Eingabemodus.")]
    public InputMode SelectInputMode { get; set; } = InputMode.auto;

    [BindProperty, Range(1, 25)]
    public int Radius { get; set; }

    // Optional auf Model-Ebene; Validierung erfolgt gezielt in OnPostSearch
    [BindProperty, StringLength(150)]
    [RegularExpression(@"^[\p{L}0-9\s.,\-\/]*$", ErrorMessage = "Ungültige Zeichen im Ortsnamen.")]
    public string? Place { get; set; }

    [BindProperty, Range(-90, 90, ErrorMessage = "Ungültiger Breitengrad.")]
    public double LongitudePlace { get; set; }

    [BindProperty, Range(-180, 180, ErrorMessage = "Ungültiger Längengrad.")]
    public double LatitudePlace { get; set; }

    public List<GasStation> CheapestResultStations { get; set; } = new();

    public Dictionary<string, decimal> CarsAndRespectivePricePerkm { get; private set; } = new();

    [BindProperty, StringLength(150, ErrorMessage = "Fahrzeugbezeichnung zu lang.")]
    [CarTypeExists(ErrorMessage = "Unbekannter Fahrzeugtyp.")]
    public string? SelectedCarType { get; set; }

    public bool IsProduction { get; private set; }
    public bool SearchExecuted { get; private set; }
    public decimal SavingsToNearestStation { get; set; }
    public decimal SavingsToCheapestStation { get; set; }

    // Als string? markiert (optionales Feld)
    [BindProperty, StringLength(50)]
    [RegularExpression(@"^[\p{L}0-9\s.,\-\/]*$", ErrorMessage = "Ungültige Zeichen in Tankstellenmarke.")]
    public string? StationBrand { get; set; }

    // Als string? markiert (optionales Feld)
    [BindProperty]
    [DiscountFormat(ErrorMessage = "Ungültiges Rabattformat.")]
    [StringLength(10)]
    public string? DiscountPercentOrAbsolute { get; set; }

    public string? DataSourceDate { get; private set; }
    public OilPriceChange? OilPriceChange { get; set; }
    public SortModeEnum SortMode { get; set; }

    private const string StationsSessionKey = "Stations";
    private const string InputDataSessionKey = "InputData";
    private const int MaxCarTypeQueryLength = 100;

    public IndexModel(
        ILogger<IndexModel> logger,
        FuelPriceService fuelPrice,
        IMarketFuelPriceService marketFuelPriceService,
        IGeoLocationService geoLocationService,
        IOilPriceService oilPriceService,
        IConfiguration configuration)
    {
        _logger = logger;
        _fuelPriceService = fuelPrice;
        _MarketfuelPriceService = marketFuelPriceService;
        _geoLocationService = geoLocationService;
        _oilPriceService = oilPriceService;
        _configuration = configuration;

        IsProduction = configuration["MODE_TYPE"]?.Equals("Production") == true;
        SearchExecuted = false;
        SortMode = SortModeEnum.totalCost;
    }

    public async Task OnGetAsync()
    {
        Console.WriteLine("get was executed and overwrite of values");
        ViewData["ContactName"] = ContactInfo.Name;

        NamePlaces = new List<string> { "", "" };
        RadiusPlaces = new List<double> { 10, 10 };

        SelectedFuelType = FuelType.Diesel;
        SelectInputMode = InputMode.auto;

        FuelAmount = 0;
        PricePerKm = 0.25m;
        FuelPrice1 = 0;
        FuelPrice2 = 0;
        Distance2 = 0;
        Radius = 10;
        AverageCostPlace1 = 0;
        AverageCostPlace2 = 0;
        Place = "";

        CheapestResultStations = new List<GasStation>();

        if (TempData["AverageCostPlace1"] != null && TempData["AverageCostPlace2"] != null)
        {
            AverageCostPlace1 = Convert.ToDouble(TempData["AverageCostPlace1"]);
            AverageCostPlace2 = Convert.ToDouble(TempData["AverageCostPlace2"]);
        }

        await GetCarsAndRespectivePricePerkm();
        await GetOilPriceChange();
    }

    [EnableRateLimiting("upstream")]
    public async Task OnPostSearch()
    {
        CheapestResultStations = new List<GasStation>();

        await GetCarsAndRespectivePricePerkm();
        await GetOilPriceChange();

        // 1. Manuelle Prüfung für den Pflichtparameter Place
        if (string.IsNullOrWhiteSpace(Place))
        {
            ModelState.AddModelError(nameof(Place), "Bitte einen Standort eingeben.");
        }

        // 2. Das gesamte Modell nach dem Setzen von nullable Typen validieren
        bool flowControl = ValidateModel();
        if (!flowControl)
        {
            return;
        }

        Console.WriteLine($"Search for optimum was executed. Input mode: {SelectInputMode}, Radius: {Radius}, Place: {Place}, Fuel type: {SelectedFuelType}, Fuel Amount: {FuelAmount}, Price per km: {PricePerKm}");
        CalculatedAverageCosts = new ConcurrentDictionary<string, decimal>();
        string fuelTypeForAPI = GetFuelTypeForAPI();

        ApiThrottle fuelThrottle = new ApiThrottle();

        var coordinates = await _geoLocationService.GetCoordinatesAsync(Place!);
        LongitudePlace = coordinates?.Longitude ?? 0;
        LatitudePlace = coordinates?.Latitude ?? 0;
        Console.WriteLine($"Coordinates from API: {coordinates}");

        if (coordinates != null)
        {
            var gasStations = await fuelThrottle.ExecuteWithThrottle("FuelPrice",
                () => _MarketfuelPriceService.GetGasStationsAsync(coordinates.Latitude, coordinates.Longitude, Radius, fuelTypeForAPI));

            if (gasStations.IsSuccess)
            {
                gasStations.Stations = await fuelThrottle.ExecuteWithThrottle("DistanceCalculation",
                    () => _geoLocationService.CalculateDistanceFromAPI(coordinates.Latitude, coordinates.Longitude, gasStations.Stations));

                Console.WriteLine($"Response in Index, List length: {gasStations.Stations.Count}");
                Console.WriteLine($"Discount input: {DiscountPercentOrAbsolute}, Fuel Amount: {FuelAmount}");

                CheapestResultStations = TankCostService.GetCheapestStation(
                    gasStations.Stations, PricePerKm, FuelAmount, fuelTypeForAPI, StationBrand ?? string.Empty, DiscountPercentOrAbsolute ?? string.Empty);

                decimal savingsToNearestTemp = 0;
                decimal savingsToCheapestTemp = 0;
                TankCostService.CaluclateSavings(gasStations.Stations, ref savingsToNearestTemp, ref savingsToCheapestTemp);
                SavingsToNearestStation = savingsToNearestTemp;
                SavingsToCheapestStation = savingsToCheapestTemp;

                if (CheapestResultStations != null && CheapestResultStations.Any())
                {
                    foreach (var station in CheapestResultStations)
                    {
                        Console.WriteLine($"Station: {station.Name}, FuelTypePrice: {station.FuelTypePrice}, TotalCalculatedCoast: {station.TotalCalculatedCoast}, LastUpdate: {station.LastUpdate}");
                    }

                    HttpContext.Session.SetString(StationsSessionKey, JsonConvert.SerializeObject(CheapestResultStations));
                }
            }
            else
            {
                Console.WriteLine($"Error in Tanker-API Request: {gasStations.ErrorMessage}");
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = "Fehler bei Tankstellenabfrage";
            }
        }
        else
        {
            TempData["ToastType"] = "error";
            TempData["ToastMessage"] = "Fehler bei der Koordinatenabfrage";
        }

        var inputData = new
        {
            FuelAmount,
            PricePerKm,
            fuelTypeForAPI,
            SelectedCarType,
            SavingsToCheapestStation,
            SavingsToNearestStation,
            SortMode
        };
        HttpContext.Session.SetString(InputDataSessionKey, JsonConvert.SerializeObject(inputData));
        SearchExecuted = true;
    }

    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> OnPostUpdateLocation([FromBody] Dictionary<string, double> coords)
    {
        Console.WriteLine("Update Location was called");
        if (!coords.TryGetValue("latitude", out var latitude) || !coords.TryGetValue("longitude", out var longitude))
        {
            return BadRequest(new { success = false, message = "Invalid coordinates" });
        }

        if (latitude is < -90 or > 90 || longitude is < -180 or > 180)
        {
            return BadRequest(new { success = false, message = "Invalid coordinates" });
        }

        LatitudePlace = latitude;
        LongitudePlace = longitude;

        Place = await _geoLocationService.GetAddressFromCoordinatesAsync(latitude, longitude);
        Console.WriteLine("Place received from coordinates " + Place);
        return new JsonResult(new { success = true, address = Place });
    }

    public async Task<JsonResult> OnGetPricePerKm(string carType)
    {
        Console.WriteLine("Get Price Per km handler");
        Console.WriteLine("Car type " + carType);

        if (string.IsNullOrWhiteSpace(carType) || carType.Length > 150)
        {
            return new JsonResult(new { pricePerKm = 0m });
        }

        await GetCarsAndRespectivePricePerkm();
        if (CarsAndRespectivePricePerkm.ContainsKey(carType))
        {
            PricePerKm = CarsAndRespectivePricePerkm[carType];
            return new JsonResult(new { pricePerKm = PricePerKm });
        }

        return new JsonResult(new { pricePerKm = PricePerKm });
    }

    public string ToDisplay(string obj)
    {
        return obj?.Replace(".", ",") ?? string.Empty;
    }

    public async Task<JsonResult> OnGetFilterCarTypes(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return new JsonResult(new { filteredCars = Array.Empty<string>() });
        }

        if (query.Length > MaxCarTypeQueryLength)
        {
            query = query[..MaxCarTypeQueryLength];
        }

        await GetCarsAndRespectivePricePerkm();
        var filteredCars = CarsAndRespectivePricePerkm.Keys
            .Where(car => car.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return new JsonResult(new { filteredCars });
    }

    [EnableRateLimiting("upstream")]
    public async Task OnPostCalculateAverageCost()
    {
        CheapestResultStations = new List<GasStation>();

        await GetCarsAndRespectivePricePerkm();
        await GetOilPriceChange();
        bool flowControl = ValidateModel();
        if (!flowControl)
        {
            return;
        }

        ThreadPool.SetMinThreads(10, 10);
        CalculatedAverageCosts = new ConcurrentDictionary<string, decimal>();
        ApiThrottle fuelThrottle = new ApiThrottle(maxConcurrentCalls: 1);


        _fuelPriceService = new FuelPriceService();
        string fuelTypeForAPI = GetFuelTypeForAPI();
        object lockObj = new object();

        List<Task> tasks = NamePlaces
            .Select((name, index) => (Name: name, Index: index))
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .Select(x => Task.Run(() => CalculateAverageCost(lockObj, x.Index, fuelThrottle, fuelTypeForAPI)))
            .ToList();

        await Task.WhenAll(tasks);

        foreach (var (type, message) in _toastMessages)
        {
            TempData["ToastType"] = type;
            TempData["ToastMessage"] = message;
        }
    }

    public async Task<IActionResult> OnPostSort(SortModeEnum sortMode)
    {
        if (!ModelState.IsValid) return new JsonResult(new { success = false, message = "Invalid model state" });

        if (!Enum.IsDefined(typeof(SortModeEnum), sortMode))
        {
            return new JsonResult(new { success = false, message = "Invalid sort mode" });
        }

        try
        {
            SortMode = sortMode;

            var stationsJson = HttpContext.Session.GetString(StationsSessionKey);
            if (string.IsNullOrEmpty(stationsJson))
            {
                return Content("<p>Keine Tankstellen in der Sitzung gespeichert</p>");
            }

            var stations = JsonConvert.DeserializeObject<List<GasStation>>(stationsJson);
            if (stations == null || !stations.Any())
            {
                return Content("<p>Keine Tankstellen verfügbar</p>");
            }

            var sortedStations = SortService.SortStations(stations, sortMode);

            var inputJson = HttpContext.Session.GetString(InputDataSessionKey);
            var inputData = string.IsNullOrEmpty(inputJson)
                ? null
                : JsonConvert.DeserializeAnonymousType(inputJson, new
                {
                    FuelAmount = 0m,
                    PricePerKm = 0m,
                    SelectedFuelType = FuelType.Diesel,
                    SelectedCarType = "",
                    SavingsToCheapestStation = 0m,
                    SavingsToNearestStation = 0m,
                    SortMode = (string)null!,
                    DiscountPercentOrAbsolute = string.Empty
                });

            var model = new IndexModel(_logger, _fuelPriceService, _MarketfuelPriceService, _geoLocationService, _oilPriceService, _configuration)
            {
                CheapestResultStations = sortedStations,
                FuelAmount = inputData?.FuelAmount ?? 0,
                PricePerKm = inputData?.PricePerKm ?? 0,
                SelectedFuelType = inputData?.SelectedFuelType ?? FuelType.Diesel,
                SelectedCarType = inputData?.SelectedCarType ?? "",
                SavingsToCheapestStation = inputData?.SavingsToCheapestStation ?? 0,
                SavingsToNearestStation = inputData?.SavingsToNearestStation ?? 0,
                SortMode = sortMode,
                DiscountPercentOrAbsolute = inputData?.DiscountPercentOrAbsolute ?? ""
            };

            var updatedInputData = new
            {
                FuelAmount = model.FuelAmount,
                PricePerKm = model.PricePerKm,
                SelectedFuelType = model.SelectedFuelType,
                SelectedCarType = model.SelectedCarType,
                SavingsToCheapestStation = model.SavingsToCheapestStation,
                SavingsToNearestStation = model.SavingsToNearestStation,
                SortMode = sortMode,
                DiscountPercentOrAbsolute = model.DiscountPercentOrAbsolute
            };
            HttpContext.Session.SetString(InputDataSessionKey, JsonConvert.SerializeObject(updatedInputData));
            HttpContext.Session.SetString(StationsSessionKey, JsonConvert.SerializeObject(sortedStations));

            return Partial("_StationListPartial", model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler in OnPostSort");
            return StatusCode(500, "Interner Serverfehler.");
        }
    }

    private async Task CalculateAverageCost(object lockObj, int i, ApiThrottle fuelThrottle, string fuelTypeForAPI)
    {
        try
        {
            var coordinatesPlace = await _geoLocationService.GetCoordinatesAsync(NamePlaces[i]);

            if (coordinatesPlace != null)
            {
                double radiusPlace = (i >= RadiusPlaces.Count) ? 10 : RadiusPlaces.ElementAt(i);
                lock (lockObj)
                {
                    RadiusPlaces[i] = radiusPlace;
                }
                var gasStationsPlace1 = await fuelThrottle.ExecuteWithThrottle("FuelPrice",
                    () => _MarketfuelPriceService.GetGasStationsAsync(coordinatesPlace.Latitude, coordinatesPlace.Longitude, radiusPlace, fuelTypeForAPI));

                if (gasStationsPlace1.IsSuccess)
                {
                    CalculatedAverageCosts[NamePlaces[i]] = _fuelPriceService.CalculateAverageCost(gasStationsPlace1.Stations) ?? 0.0m;
                }
                else
                {
                    CalculatedAverageCosts[NamePlaces[i]] = 0.0m;
                    _toastMessages.Add(("error", "Fehler bei Tankstellenabfrage"));
                }
            }
            else
            {
                CalculatedAverageCosts[NamePlaces[i]] = 0.0m;
                _toastMessages.Add(("error", "Fehler bei Koordinatenabfrage"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler bei der Verarbeitung von {Place}", NamePlaces[i]);
            CalculatedAverageCosts[NamePlaces[i]] = 0.0m;
            _toastMessages.Add(("error", $"Fehler bei der Verarbeitung von {NamePlaces[i]}"));
        }
    }

    private async Task GetCarsAndRespectivePricePerkm()
    {
        if (CarsAndRespectivePricePerkm.Count > 0)
        {
            return;
        }
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "ADAC_car_data.json");
        CarsAndRespectivePricePerkm = await CarDataParser.ParseCarData(filePath);
        Dictionary<string, string> carsMetaData = await CarDataParser.GetMetaData(filePath);
        if (carsMetaData != null && carsMetaData.ContainsKey("generated_at"))
        {
            DataSourceDate = carsMetaData["source"];
        }
    }

    private async Task GetOilPriceChange()
    {
        ApiThrottle oilPriceThrottle = new ApiThrottle();
        var oilPriceResult = await oilPriceThrottle.ExecuteWithThrottle("OilPrice", () => _oilPriceService.GetOilPriceChangeAsync());

        if (oilPriceResult.IsSuccess)
        {
            OilPriceChange = oilPriceResult.PriceChange;
        }
        else
        {
            _logger.LogWarning("Fehler bei Ölpreisänderungsabfrage: {ErrorMessage}", oilPriceResult.ErrorMessage);
            TempData["ToastType"] = "error";
            TempData["ToastMessage"] = "Fehler bei Ölpreisänderungsabfrage";
            OilPriceChange = new OilPriceChange(0, 0, 0, 0);
        }
    }

    private string GetFuelTypeForAPI()
    {
        return SelectedFuelType switch
        {
            FuelType.Diesel => FuelType.Diesel.ToString(),
            FuelType.SuperE5 => "Super E5",
            FuelType.SuperE10 => "Super E10",
            _ => string.Empty
        };
    }

    private bool ValidateModel()
    {
        if (!TryValidateModel(this))
        {
            var errors = ModelState
                .Where(x => x.Value?.Errors.Count > 0)
                .SelectMany(x => x.Value!.Errors.Select(e =>
                    $"{x.Key}: {(string.IsNullOrEmpty(e.ErrorMessage) ? e.Exception?.Message : e.ErrorMessage)}"))
                .ToList();

            TempData["ToastType"] = "error";
            TempData["ToastMessage"] = "Validierungsfehler: " + string.Join(" | ", errors);
            return false;
        }
        return true;
    }
    
}