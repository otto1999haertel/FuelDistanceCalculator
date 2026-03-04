using System.Collections.Concurrent;
using FuelDistanceCalculator.Constants;
using FuelDistanceCalculator.Data;
using FuelDistanceCalculator.Services;
using FuelDistanceCalculator.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using FuelDistanceCalculator.Model;
using FuelDistanceCalculator.Model.Dto;

namespace FuelDistanceCalculator.Pages;

[IgnoreAntiforgeryToken]
public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;

    private readonly AppDbContext _context;
    private FuelPriceService _fuelPriceService;

private readonly IMarketFuelPriceService _marketFuelPriceService;
private readonly IGeoLocationService _geoLocationService;
private readonly ISearchService _searchService;

    private readonly ConcurrentBag<(string Type, string Message)> _toastMessages = new ConcurrentBag<(string, string)>();

    [BindProperty]
    public decimal FuelAmount { get; set; } // Globale Tankmenge für beide Tankstellen
    [BindProperty]
    public decimal PricePerKm { get; set; } // Preis pro Kilometer für beide Tankstellen

    [BindProperty]
    public double Distance1 { get; set; }
    [BindProperty]
    public double FuelPrice1 { get; set; }

    [BindProperty]
    public double Distance2 { get; set; }
    [BindProperty]
    public double FuelPrice2 { get; set; }

    [BindProperty]
    public string NamePlace1 { get; set; }

    [BindProperty]
    public List<string> NamePlaces { get; set; }

    [BindProperty]
    public List<double> RadiusPlaces { get; set; }

    //Thread-safe Dictionary für parallele Berechnungen
    [BindProperty]
    public ConcurrentDictionary<string, decimal> CalculatedAverageCosts { get; set; }

    [BindProperty]
    public double AverageCostPlace1 { get; private set; }

    [BindProperty]
    public double AverageCostPlace2 { get; private set; }

    [BindProperty]
    public FuelType SelectedFuelType { get; set; }

    [BindProperty]
    public double FuelAmountBreakEven { get; set; }

    [BindProperty]
    public string NameGasStationBreakEven { get; set; }

    [BindProperty]
    public bool BreakEvenAnalysisDeterministic { get; set; }


    [BindProperty]
    public InputMode SelectInputMode { get; set; } = InputMode.auto;

    [BindProperty]
    public VolumeUnit VolumeUnit
    {
        get => VolumeUnit.Liter;
    }

    [BindProperty]
    public int Radius { get; set; }

    [BindProperty]
    public string Place { get; set; }

    [BindProperty]
    public double LongitudePlace { get; set; }

    [BindProperty]
    public double LatitudePlace { get; set; }

    [BindProperty]
    public List<GasStation> CheapestResultStations { get; set; }

    public Dictionary<string, decimal> CarsAndRespectivePricePerkm { get; private set; } = new Dictionary<string, decimal>();

    [BindProperty]
    public string SelectedCarType { get; set; }

    [BindProperty]
    public bool IsProduction { get; private set; }

    [BindProperty]
    public bool SearchExecuted { get; private set; }

    [BindProperty]
    public decimal SavingsToNearestStation { get;  set; }
    [BindProperty]
    public decimal SavingsToCheapestStation { get; set; }

    [BindProperty]
    public string StationBrand{get; set; }

    [BindProperty]
    public decimal DiscountPercent{get; set; }
    
    public string SortMode { get; set; }

    private const string StationsSessionKey = "Stations"; // Neuer Schlüssel für vollständige GasStation-Objekte

    private const string InputDataSessionKey = "InputData";

    public IndexModel(ILogger<IndexModel> logger, FuelPriceService fuelPrice, AppDbContext context,
                  IMarketFuelPriceService marketFuelPriceService, IGeoLocationService geoLocationService,
                  ISearchService searchService)
    {
        _logger = logger;
        _fuelPriceService = fuelPrice;
        _context = context;
        _marketFuelPriceService = marketFuelPriceService;
        _geoLocationService = geoLocationService;
        _searchService = searchService;
        if (NamePlaces == null || !NamePlaces.Any())
        {
            NamePlaces = new List<string>();
        }

        if (RadiusPlaces == null || !RadiusPlaces.Any())
        {
            RadiusPlaces = new List<double>();
        }
        IsProduction = Environment.GetEnvironmentVariable("MODE_TYPE").Equals("Production");
        CalculatedAverageCosts = new ConcurrentDictionary<string, decimal>();
        SearchExecuted = false;
        SortMode = "totalCost";
        StationBrand = string.Empty;
        DiscountPercent = 0;
    }

    public async Task OnGetAsync()
    {
        _logger.LogInformation("OnGetAsync executed - overwriting values");
        ViewData["ContactName"] = ContactInfo.Name;
        NamePlaces.Add("");
        RadiusPlaces.Add(10);
        NamePlaces.Add("");
        RadiusPlaces.Add(10);
        SelectedFuelType = FuelType.Diesel;
        SelectInputMode = InputMode.auto;



        FuelAmount = 0;
        PricePerKm = 0.25m;
        FuelPrice1 = 0;
        Distance1 = 0;
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
    }

    public async Task OnPostSearch()
    {
        await GetCarsAndRespectivePricePerkm();
        Console.WriteLine($"Search for optimum was executed. Input mode: {SelectInputMode}, Radius: {Radius}, Place: {Place}, Fuel type: {SelectedFuelType}, Fuel Amount: {FuelAmount}, Price per km: {PricePerKm}");
        CalculatedAverageCosts = new ConcurrentDictionary<string, decimal>();

        var parameters = new SearchParameters
        {
            Place = Place,
            Radius = Radius,
            FuelAmount = FuelAmount,
            PricePerKm = PricePerKm,
            FuelType = SelectedFuelType,
            StationBrand = StationBrand,
            DiscountPercent = DiscountPercent,
            SortMode = SortMode
        };

        var result = await _searchService.SearchAsync(parameters);

        // map results to model
        LongitudePlace = result.Parameters?.Place != null ? (await _geoLocationService.GetCoordinatesAsync(result.Parameters.Place))?.Longitude ?? 0 : 0;
        LatitudePlace = result.Parameters?.Place != null ? (await _geoLocationService.GetCoordinatesAsync(result.Parameters.Place))?.Latitude ?? 0 : 0;

        CheapestResultStations = result.Stations?.Take(10).ToList() ?? new List<GasStation>();
        SavingsToNearestStation = result.SavingsToNearestStation;
        SavingsToCheapestStation = result.SavingsToCheapestStation;

        if (CheapestResultStations.Any())
        {
            foreach (var station in CheapestResultStations)
            {
                Console.WriteLine($"Station: {station.Name}, FuelTypePrice: {station.FuelTypePrice}, TotalCalculatedCoast: {station.TotalCalculatedCoast}, LastUpdate: {station.LastUpdate}");
            }
            HttpContext.Session.SetString(StationsSessionKey, JsonConvert.SerializeObject(CheapestResultStations));
        }

        var inputData = new
        {
            FuelAmount,
            PricePerKm,
            FuelType = parameters.FuelType,
            SelectedCarType,
            SavingsToCheapestStation,
            SavingsToNearestStation,
            SortMode
        };
        HttpContext.Session.SetString(InputDataSessionKey, JsonConvert.SerializeObject(inputData));
        SearchExecuted = true;
    }

    // optional bei JS-only Requests ohne Token
    public async Task<IActionResult> OnPostUpdateLocation([FromBody] Dictionary<string, double> coords)
    {
        Console.WriteLine("Update Locatiion was called");
        if (!coords.TryGetValue("latitude", out var latitude) || !coords.TryGetValue("longitude", out var longitude))
        {
            return BadRequest(new { success = false, message = "Invalid coordinates" });
        }

        LatitudePlace = latitude;
        LongitudePlace = longitude;

        // Serverseitig Adresse ermitteln
        Place = await _geoLocationService.GetAddressFromCoordinatesAsync(latitude, longitude);
        Console.WriteLine("Place recevied from ccordinates " + Place);
        return new JsonResult(new { success = true, address = Place });
    }
    // Speichern-Methode, wird durch den Speichern-Button ausgelöst
    public IActionResult OnPostSaveData()
    {
        _logger.LogInformation("Speichern-Methode wurde aufgerufen.");  // Loggen für Debugging
        // Dummy-Speichern-Logik (diese wird später durch eine DB ersetzt)
        TempData["Message"] = "Daten wurden nicht erfolgreich gespeichert!";

        // Weiterleitung zurück zur Index-Seite
        return RedirectToPage();
    }

    public async Task<JsonResult> OnGetPricePerKm(string carType)
    {
        Console.WriteLine("Get Price Per km handler");
        Console.WriteLine("Car type " + carType);
        await GetCarsAndRespectivePricePerkm();
        if (CarsAndRespectivePricePerkm.ContainsKey(carType))
        {
            Console.WriteLine("Key found");
            PricePerKm = CarsAndRespectivePricePerkm[carType];
            return new JsonResult(new { pricePerKm = PricePerKm });
        }
        else
        {
            Console.WriteLine("Key not found");
        }

        return new JsonResult(new { pricePerKm = PricePerKm });
    }


    public string ToDisplay(string obj)
    {
        return obj.ToString().Replace(".", ",");
    }

    public async Task<JsonResult> OnGetFilterCarTypes(string query)
    {
        // Stelle sicher, dass das Dictionary bereits geladen ist
        Console.WriteLine("Server filter car types was called with input " + query);
        await GetCarsAndRespectivePricePerkm();
        Console.WriteLine("Cars Dictionary Einträge: " + CarsAndRespectivePricePerkm.Count);
        // Führe die Filterung basierend auf dem Query-String durch (Groß-/Kleinschreibung ignorieren)
        var filteredCars = CarsAndRespectivePricePerkm.Keys
            .Where(car => car.ToLower().Contains(query.ToLower()))
            .ToList();
        Console.WriteLine("Filtered results: " + filteredCars.Count);

        return new JsonResult(new { filteredCars });
    }

    public async Task OnPostCalculateAverageCost()
    {
        ThreadPool.SetMinThreads(10, 10);
        ThreadPool.GetAvailableThreads(out int workerThreads, out int completionPortThreads);
        Console.WriteLine($"[Calculation Post Thread {Thread.CurrentThread.ManagedThreadId}] Available Worker Threads: {workerThreads}, Completion Port Threads: {completionPortThreads} at {DateTime.Now:HH:mm:ss.fff}");
        foreach (var key in Request.Form.Keys)
        {
            Console.WriteLine($"FORM: {key} = {Request.Form[key]}");
        }
        Console.WriteLine("Anzahl der Orte: " + NamePlaces.Count);
        CalculatedAverageCosts = new ConcurrentDictionary<string, decimal>();
        ApiThrottle geoThrottle = new ApiThrottle(maxConcurrentCalls: 1);
        ApiThrottle fuelThrottle = new ApiThrottle(maxConcurrentCalls: 1);

        await GetCarsAndRespectivePricePerkm();
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
        Console.WriteLine("Calculated Average Costs: " + CalculatedAverageCosts.Count);
    }

    public async Task<IActionResult> OnPostSort(string sortMode)
    {
        if (string.IsNullOrEmpty(sortMode))
            return BadRequest("sortMode ist erforderlich");

        SortMode = sortMode;

        // Lade gespeicherte Stationen aus Session
        var stationsJson = HttpContext.Session.GetString(StationsSessionKey);
        if (string.IsNullOrEmpty(stationsJson))
        {
            return Content("<p>Keine Tankstellen in der Sitzung gespeichert</p>");
        }

        var stations = JsonConvert.DeserializeObject<List<GasStation>>(stationsJson) ?? new List<GasStation>();
        if (!stations.Any())
        {
            return Content("<p>Keine Tankstellen verfügbar</p>");
        }

        // Sortieren über SearchService
        var sortedStations = _searchService.SortStations(stations, sortMode);
        CheapestResultStations = sortedStations;

        // Aktualisiere Sessionwerte
        HttpContext.Session.SetString(StationsSessionKey, JsonConvert.SerializeObject(sortedStations));

        // InputData aus Session wiederherstellen, damit PartialView alle Felder hat
        var inputJson = HttpContext.Session.GetString(InputDataSessionKey);
        if (!string.IsNullOrEmpty(inputJson))
        {
            var stored = JsonConvert.DeserializeAnonymousType(inputJson, new
            {
                FuelAmount = 0m,
                PricePerKm = 0m,
                SelectedFuelType = FuelType.Diesel,
                SelectedCarType = "",
                SavingsToCheapestStation = 0m,
                SavingsToNearestStation = 0m,
                SortMode = (string)null
            });
            FuelAmount = stored.FuelAmount;
            PricePerKm = stored.PricePerKm;
            SelectedFuelType = stored.SelectedFuelType;
            SelectedCarType = stored.SelectedCarType;
            SavingsToCheapestStation = stored.SavingsToCheapestStation;
            SavingsToNearestStation = stored.SavingsToNearestStation;
        }

        return Partial("_StationListPartial", this);
    }

    private async Task CalculateAverageCost(object lockObj, int i, ApiThrottle fuelThrottle, string fuelTypeForAPI)
    {
        try
        {
            Console.WriteLine($"[Calculation Thread {Thread.CurrentThread.ManagedThreadId}] Task for {NamePlaces[i]} started at {DateTime.Now:HH:mm:ss.fff}");
            var coordinatesPlace = await _geoLocationService.GetCoordinatesAsync(NamePlaces[i]);

            if (coordinatesPlace != null)
            {
                double radiusPlace = (i >= RadiusPlaces.Count || RadiusPlaces.ElementAt(i) == null) ? 10 : RadiusPlaces.ElementAt(i);
                lock (lockObj)
                {
                    RadiusPlaces[i] = radiusPlace;
                }
                var gasStationsPlace1 = await fuelThrottle.ExecuteWithThrottle("FuelPrice",
                    () => _marketFuelPriceService.GetGasStationsAsync(coordinatesPlace.Latitude, coordinatesPlace.Longitude, radiusPlace, fuelTypeForAPI, StationBrand, DiscountPercent));
                if (gasStationsPlace1.IsSuccess)
                {
                    CalculatedAverageCosts[NamePlaces[i]] = _fuelPriceService.CalculateAverageCost(gasStationsPlace1.Stations) ?? 0.0m;
                }
                else
                {
                    CalculatedAverageCosts[NamePlaces[i]] = 0.0m;
                    _toastMessages.Add(("error", "Fehler bei Tankstellenabfrage"));
                }
                Console.WriteLine($"Calculated Average Cost for {NamePlaces[i]}: {CalculatedAverageCosts[NamePlaces[i]]}");
            }
            else
            {
                CalculatedAverageCosts[NamePlaces[i]] = 0.0m;
                _toastMessages.Add(("error", "Fehler bei Koordinatenabfrage"));
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fehler bei {NamePlaces[i]}: {ex.Message}");
            CalculatedAverageCosts[NamePlaces[i]] = 0.0m;
            _toastMessages.Add(("error", $"Fehler bei der Verarbeitung von {NamePlaces[i]}: {ex.Message}"));
        }
    }

    private async Task GetCarsAndRespectivePricePerkm()
    {
        Console.WriteLine($"Current Directory: {Directory.GetCurrentDirectory()}");
        if (CarsAndRespectivePricePerkm.Count > 0)
        {
            return;
        }
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "ADAC_car_data.json");
        Console.WriteLine("Combined Path: " + filePath);
        var jsonContent = await System.IO.File.ReadAllTextAsync(filePath);
        CarsAndRespectivePricePerkm = JsonConvert.DeserializeObject<Dictionary<string, decimal>>(jsonContent);
    }

    private string GetFuelTypeForAPI()
    {
        switch (SelectedFuelType)
        {
            case FuelType.Diesel:
                return SelectedFuelType.ToString();
            case FuelType.SuperE5:
                return "Super E5";
            case FuelType.SuperE10:
                return "Super E10";
        }
        return string.Empty;
    }
}