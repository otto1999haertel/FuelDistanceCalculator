using System.Collections.Concurrent;
using System.Threading.Tasks;
using FuelDistanceCalculator.Constants;
using FuelDistanceCalculator.Data;
using FuelDistanceCalculator.Model;
using FuelDistanceCalculator.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;

namespace FuelDistanceCalculator.Pages;

[IgnoreAntiforgeryToken]
public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;

    private readonly AppDbContext _context;
    private FuelPriceService _fuelPriceService;

    private readonly MarketFuelPriceService _MarketfuelPriceService;
    private readonly IGeoLocationService _geoLocationService;

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
    public bool CalculationSucessful { get; set; }

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
    public bool  IsProduction { get; private set; }

    public IndexModel(ILogger<IndexModel> logger, FuelPriceService fuelPrice, AppDbContext context, MarketFuelPriceService marketFuelPriceService, IGeoLocationService geoLocationService)
    {
        _logger = logger;
        _fuelPriceService = fuelPrice;
        _context = context;
        _MarketfuelPriceService = marketFuelPriceService;
        _geoLocationService = geoLocationService;
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
    }

    public async Task OnGetAsync()
    {
        Console.WriteLine("get was executed and overwirte of values");
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
            CalculationSucessful = true; // Falls es berechnete Werte gibt, setze auf erfolgreich
        }
        await GetCarsAndRespectivePricePerkm();
    }

    public async Task OnPostSearch()
    {
        await GetCarsAndRespectivePricePerkm();
        Console.WriteLine("Search for optimum was executed");
        Console.WriteLine("Input mode in search case: " + SelectInputMode.ToString());
        Console.WriteLine("Radius " + Radius);
        Console.WriteLine("Place " + Place);
        Console.WriteLine("Fuel type  " + SelectedFuelType.ToString().ToLower());
        Console.WriteLine("Fuel Amount " + FuelAmount);
        Console.WriteLine("Price pro kilometer " + PricePerKm);
        CalculatedAverageCosts = new ConcurrentDictionary<string, decimal>();
        string fuelTypeForAPI = GetFuelTypeForAPI();


        // API-Aufruf zur Koordinatensuche
        ApiThrottle geoThrottle = new ApiThrottle();
        ApiThrottle fuelThrottle = new ApiThrottle();

        var coordinates = await _geoLocationService.GetCoordinatesAsync(Place);
        LongitudePlace = (double)(coordinates?.Longitude ?? 0);
        LatitudePlace = (double)(coordinates?.Latitude ?? 0);
        Console.WriteLine("Koordinates from API " + coordinates);
        if (coordinates != null)
        {
            var gasStations = await fuelThrottle.ExecuteWithThrottle("FuelPrice",
            () => _MarketfuelPriceService.GetGasStationsAsync(coordinates.Latitude, coordinates.Longitude, Radius, fuelTypeForAPI));
            gasStations.Stations = await fuelThrottle.ExecuteWithThrottle("DistanceCalculation",
            () => _geoLocationService.CalculateDistance(coordinates.Latitude.ToString(), coordinates.Longitude.ToString(), gasStations.Stations));
            if (gasStations.IsSuccess)
            {
                Console.WriteLine("Response in Index, Listlänge" + gasStations.Stations.Count);
                CheapestResultStations = TankCostService.GetCheapestStations(gasStations.Stations, FuelAmount, PricePerKm);
            }
            else
            {
                Console.WriteLine("Error in search Tanker-API Request" + gasStations.ErrorMessage);
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = "Fehler bei Tankstellenabfrage";
            }
        }
        else
        {
            TempData["ToastType"] = "error";
            TempData["ToastMessage"] = "Fehler bei der Koordinatenabfrage";
        }
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
        ApiThrottle geoThrottle = new ApiThrottle(maxConcurrentCalls:1);
        ApiThrottle fuelThrottle = new ApiThrottle(maxConcurrentCalls:1);

        await GetCarsAndRespectivePricePerkm();
        _fuelPriceService = new FuelPriceService((int)FuelAmount, PricePerKm);
        string fuelTypeForAPI = GetFuelTypeForAPI();
        object lockObj = new object();
        List<Task> tasks = NamePlaces
            .Select((name, index) => (Name: name, Index: index))
            .Where(x => !string.IsNullOrWhiteSpace(x.Name))
            .Select(x => Task.Run(()=>CalculateAverageCost(lockObj, x.Index, fuelThrottle, fuelTypeForAPI)))
            .ToList();

        await Task.WhenAll(tasks);
        foreach (var (type, message) in _toastMessages)
        {
            TempData["ToastType"] = type;
            TempData["ToastMessage"] = message;
        }
        Console.WriteLine("Calculated Average Costs: " + CalculatedAverageCosts.Count);
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
                    () => _MarketfuelPriceService.GetGasStationsAsync(coordinatesPlace.Latitude, coordinatesPlace.Longitude, radiusPlace, fuelTypeForAPI));
                if (gasStationsPlace1.IsSuccess)
                {
                    CalculatedAverageCosts[NamePlaces[i]] = _fuelPriceService.CalculateAverageCost(gasStationsPlace1.Stations) ?? 0.0m;
                }
                else
                {
                    CalculatedAverageCosts[NamePlaces[i]] = 0.0m;
                    _toastMessages.Add(("error","Fehler bei Tankstellenabfrage"));
                }
                Console.WriteLine($"Calculated Average Cost for {NamePlaces[i]}: {CalculatedAverageCosts[NamePlaces[i]]}");
            }
            else
            {
                CalculatedAverageCosts[NamePlaces[i]] = 0.0m;
                _toastMessages.Add(("error","Fehler bei Koordinatenabfrage"));
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