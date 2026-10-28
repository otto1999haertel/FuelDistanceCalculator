using System.Collections.Concurrent;
using System.ComponentModel.DataAnnotations;
using FuelDistanceCalculator.Constants;
using FuelDistanceCalculator.Interfaces;
using FuelDistanceCalculator.Model;
using FuelDistanceCalculator.Services;
using FuelDistanceCalculator.Services.Discount;
using FuelDistanceCalculator.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.RateLimiting;
using Newtonsoft.Json;

namespace FuelDistanceCalculator.Pages;

/// <summary>
/// PageModel for the Index page handling fuel distance calculator functionality
/// Modularized to separate UI logic from business logic
/// </summary>
public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly FuelPriceService _fuelPriceService;
    private readonly TankStationSearchService _tankStationSearchService;
    private readonly IMarketFuelPriceService _marketFuelPriceService;
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

    /// <summary>
    /// Stores calculated average costs for each location in comparison mode
    /// </summary>
    public ConcurrentDictionary<string, decimal> CalculatedAverageCosts { get; private set; } = new();

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

    /// <summary>
    /// Contains the list of cheapest stations found by search
    /// </summary>
    public List<GasStation> CheapestResultStations { get; private set; } = new();

    public Dictionary<string, decimal> CarsAndRespectivePricePerkm { get; private set; } = new();

    [BindProperty, StringLength(150, ErrorMessage = "Fahrzeugbezeichnung zu lang.")]
    [CarTypeExists(ErrorMessage = "Unbekannter Fahrzeugtyp.")]
    public string? SelectedCarType { get; set; }

    public bool IsProduction { get; private set; }
    /// <summary>
    /// Indicates whether a search has been executed
    /// </summary>
    public bool SearchExecuted { get; private set; }
    public decimal SavingsToNearestStation { get; private set; }
    public decimal SavingsToCheapestStation { get; private set; }

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
    public OilPriceChange? OilPriceChange { get; private set; }
    public SortModeEnum SortMode { get; private set; }

    /// <summary>
    /// Indicates whether advanced options have values and should be expanded by default
    /// </summary>
    public bool AdvancedHasValue =>
        !string.IsNullOrWhiteSpace(StationBrand)
        || !string.IsNullOrWhiteSpace(DiscountPercentOrAbsolute)
        || !string.IsNullOrWhiteSpace(SelectedCarType);

    private const string StationsSessionKey = "Stations";
    private const string InputDataSessionKey = "InputData";
    private const int MaxCarTypeQueryLength = 100;

    /// <summary>
    /// Constructor that injects all required dependencies for modular architecture
    /// </summary>
    public IndexModel(
        ILogger<IndexModel> logger,
        FuelPriceService fuelPriceService,
        TankStationSearchService tankStationSearchService,
        IMarketFuelPriceService marketFuelPriceService,
        IGeoLocationService geoLocationService,
        IOilPriceService oilPriceService,
        IConfiguration configuration)
    {
        _logger = logger;
        _fuelPriceService = fuelPriceService;
        _tankStationSearchService = tankStationSearchService;
        _marketFuelPriceService = marketFuelPriceService;
        _geoLocationService = geoLocationService;
        _oilPriceService = oilPriceService;
        _configuration = configuration;

        IsProduction = configuration["MODE_TYPE"]?.Equals("Production") == true;
        SearchExecuted = false;
        SortMode = SortModeEnum.totalCost;
    }

    /// <summary>
    /// GET handler for the Index page. Initializes the model with default values and loads necessary data.
    /// </summary>
    public async Task OnGetAsync()
    {
        Console.WriteLine("get was executed and overwrite of values");
        
        // Initialize view data
        ViewData["ContactName"] = ContactInfo.Name;

        // Initialize form defaults for comparison mode
        NamePlaces = new List<string> { "", "" };
        RadiusPlaces = new List<double> { 10, 10 };

        // Set default fuel type and input mode
        SelectedFuelType = FuelType.Diesel;
        SelectInputMode = InputMode.auto;

        // Set default calculation parameters
        FuelAmount = 0;
        PricePerKm = 0.25m;
        FuelPrice1 = 0;
        FuelPrice2 = 0;
        Distance2 = 0;
        Radius = 10;
        Place = "";

        // Initialize results collection
        CheapestResultStations = new List<GasStation>();
        CalculatedAverageCosts = new ConcurrentDictionary<string, decimal>();

        // Restore average costs from TempData if available
        if (TempData["AverageCostPlace1"] != null && TempData["AverageCostPlace2"] != null)
        {
            AverageCostPlace1 = Convert.ToDouble(TempData["AverageCostPlace1"]);
            AverageCostPlace2 = Convert.ToDouble(TempData["AverageCostPlace2"]);
        }

        // Load car data and oil price information
        await GetCarsAndRespectivePricePerkm();
        await GetOilPriceChange();
    }

    /// <summary>
    /// POST handler for searching tank stations. Validates input and delegates to TankStationSearchService.
    /// </summary>
    [EnableRateLimiting("upstream")]
    public async Task OnPostSearch()
    {
        // Initialize results collection
        CheapestResultStations = new List<GasStation>();

        // Load necessary data
        await GetCarsAndRespectivePricePerkm();
        await GetOilPriceChange();

        // Validate Place parameter
        if (string.IsNullOrWhiteSpace(Place))
        {
            ModelState.AddModelError(nameof(Place), "Bitte einen Standort eingeben.");
        }

        // Validate entire model
        bool flowControl = ValidateModel();
        if (!flowControl)
        {
            return;
        }

        Console.WriteLine($"Search for optimum was executed. Input mode: {SelectInputMode}, Radius: {Radius}, Place: {Place}, Fuel type: {SelectedFuelType}, Fuel Amount: {FuelAmount}, Price per km: {PricePerKm}");
        
        // Delegate to TankStationSearchService for search logic
        var searchResult = await _tankStationSearchService.ExecuteSearchAsync(
            Place!,
            Radius,
            SelectedFuelType,
            PricePerKm,
            FuelAmount,
            StationBrand,
            DiscountPercentOrAbsolute);

        // Center the map on the geocoded location (restored behavior the map depends on).
        LatitudePlace = searchResult.Latitude;
        LongitudePlace = searchResult.Longitude;

        if (searchResult.Success && searchResult.Stations != null)
        {
            CheapestResultStations = searchResult.Stations;
            SavingsToNearestStation = searchResult.SavingsToNearestStation;
            SavingsToCheapestStation = searchResult.SavingsToCheapestStation;

            // Log found stations
            foreach (var station in CheapestResultStations)
            {
                Console.WriteLine($"Station: {station.Name}, FuelTypePrice: {station.FuelTypePrice}, TotalCalculatedCoast: {station.TotalCombinedCosts}, LastUpdate: {station.LastUpdate}");
            }

            // Save to session
            HttpContext.Session.SetString(StationsSessionKey, JsonConvert.SerializeObject(CheapestResultStations));
        }
        else
        {
            TempData["ToastType"] = "error";
            TempData["ToastMessage"] = $"Fehler bei Suche: {searchResult.ErrorMessage}";
        }

        // Store input data in session for sorting
        var inputData = new
        {
            FuelAmount,
            PricePerKm,
            fuelTypeForAPI = GetFuelTypeForAPI(),
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

    public string ToDisplayFloatingPoint(string obj)
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

    /// <summary>
    /// POST handler for calculating average costs across multiple locations. Delegates to TankStationSearchService.
    /// </summary>
    [EnableRateLimiting("upstream")]
    public async Task OnPostCalculateAverageCost()
    {
        CheapestResultStations = new List<GasStation>();

        // Load necessary data
        await GetCarsAndRespectivePricePerkm();
        await GetOilPriceChange();

        // Validate model
        bool flowControl = ValidateModel();
        if (!flowControl)
        {
            return;
        }

        // Delegate to TankStationSearchService for parallel calculation
        var result = await _tankStationSearchService.CalculateAverageCostsAsync(
            NamePlaces, 
            RadiusPlaces, 
            SelectedFuelType);

        // Store results in CalculatedAverageCosts dictionary
        foreach (var location in result.Locations)
        {
            CalculatedAverageCosts[location.Name] = location.AverageCost;
        }

        // Update average cost fields for view (convert decimal to double)
        if (CalculatedAverageCosts.Count > 0)
        {
            AverageCostPlace1 = (double)CalculatedAverageCosts.Values.ElementAtOrDefault(0);
            AverageCostPlace2 = (double)CalculatedAverageCosts.Values.ElementAtOrDefault(1);
        }

        // Handle toast messages from service
        foreach (var (type, message) in _toastMessages)
        {
            TempData["ToastType"] = type;
            TempData["ToastMessage"] = message;
        }

        if (!result.Success)
        {
            TempData["ToastType"] = "error";
            TempData["ToastMessage"] = result.ErrorMessage;
        }
    }

    /// <summary>
    /// POST handler for sorting tank stations. Delegates to SortService and returns partial view.
    /// </summary>
    public async Task<IActionResult> OnPostSort(SortModeEnum sortMode)
    {
        if (!ModelState.IsValid)
        {
            return new JsonResult(new { success = false, message = "Invalid model state" });
        }

        if (!Enum.IsDefined(typeof(SortModeEnum), sortMode))
        {
            return new JsonResult(new { success = false, message = "Invalid sort mode" });
        }

        try
        {
            SortMode = sortMode;

            // Get and sort stations from session
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

            // Sort stations using SortService
            var sortedStations = SortService.SortStations(stations, sortMode);

            // Get input data from session
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

            // Restore the persisted search context onto this model so the re-rendered
            // list keeps its display state. Without this the partial (rendered with
            // `this` since the nodularization refactor) sees FuelAmount/savings = 0 and
            // hides the Ersparnis even when switching back to "Gesamtkosten" sorting.
            FuelAmount = inputData?.FuelAmount ?? 0;
            PricePerKm = inputData?.PricePerKm ?? 0;
            SelectedFuelType = inputData?.SelectedFuelType ?? FuelType.Diesel;
            SelectedCarType = inputData?.SelectedCarType ?? "";
            SavingsToCheapestStation = inputData?.SavingsToCheapestStation ?? 0;
            SavingsToNearestStation = inputData?.SavingsToNearestStation ?? 0;
            DiscountPercentOrAbsolute = inputData?.DiscountPercentOrAbsolute ?? "";

            // Update model with sorted stations
            CheapestResultStations = sortedStations;

            // Persist the updated context back to the session
            var updatedInputData = new
            {
                FuelAmount,
                PricePerKm,
                SelectedFuelType,
                SelectedCarType,
                SavingsToCheapestStation,
                SavingsToNearestStation,
                SortMode = sortMode,
                DiscountPercentOrAbsolute
            };
            HttpContext.Session.SetString(InputDataSessionKey, JsonConvert.SerializeObject(updatedInputData));
            HttpContext.Session.SetString(StationsSessionKey, JsonConvert.SerializeObject(sortedStations));

            return Partial("_StationListPartial", this);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fehler in OnPostSort");
            return StatusCode(500, "Interner Serverfehler.");
        }
    }

    /// <summary>
    /// Loads car data and fuel prices from ADAC JSON file. Results are cached after first load.
    /// </summary>
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

    /// <summary>
    /// Fetches oil price change data from the oil price service. Returns default values on failure.
    /// </summary>
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

    /// <summary>
    /// Converts FuelType enum to API-compatible string.
    /// </summary>
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

    /// <summary>
    /// Validates the model and returns a list of validation errors if any.
    /// </summary>
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