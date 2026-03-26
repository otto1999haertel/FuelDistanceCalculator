using System.Collections.Concurrent;
using FuelDistanceCalculator.Constants;
using FuelDistanceCalculator.Data;
using FuelDistanceCalculator.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using FuelDistanceCalculator.Model;

namespace FuelDistanceCalculator.Pages;

[IgnoreAntiforgeryToken]
public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;

    private readonly AppDbContext _context;
    private FuelPriceService _fuelPriceService;

    private readonly IMarketFuelPriceService _MarketfuelPriceService;
    private readonly IGeoLocationService _geoLocationService;

    private readonly ConcurrentBag<(string Type, string Message)> _toastMessages = new ConcurrentBag<(string, string)>();
    private IConfiguration _configuration;

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
    public string DiscountPercentOrAbsolute{get; set; }
    
    public string SortMode { get; set; }

    private const string StationsSessionKey = "Stations"; // Neuer Schlüssel für vollständige GasStation-Objekte

    private const string InputDataSessionKey = "InputData";

    public IndexModel(ILogger<IndexModel> logger, FuelPriceService fuelPrice, AppDbContext context, MarketFuelPriceService marketFuelPriceService, IGeoLocationService geoLocationService, IConfiguration configuration)
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
        IsProduction = configuration["MODE_TYPE"]?.Equals("Production") == true;
        _configuration = configuration;
        CalculatedAverageCosts = new ConcurrentDictionary<string, decimal>();
        SearchExecuted = false;
        SortMode = "totalCost";
        StationBrand = string.Empty;
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
        }
        await GetCarsAndRespectivePricePerkm();
    }

    public async Task OnPostSearch()
    {
        await GetCarsAndRespectivePricePerkm();
        Console.WriteLine($"Search for optimum was executed. Input mode: {SelectInputMode}, Radius: {Radius}, Place: {Place}, Fuel type: {SelectedFuelType}, Fuel Amount: {FuelAmount}, Price per km: {PricePerKm}");
        CalculatedAverageCosts = new ConcurrentDictionary<string, decimal>();
        string fuelTypeForAPI = GetFuelTypeForAPI();

        ApiThrottle geoThrottle = new ApiThrottle();
        ApiThrottle fuelThrottle = new ApiThrottle();

        var coordinates = await _geoLocationService.GetCoordinatesAsync(Place);
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
                //Prozentualer Rabatt
                Console.WriteLine($"Discount input: {DiscountPercentOrAbsolute}, Fuel Amount: {FuelAmount}");
                if (string.IsNullOrEmpty(DiscountPercentOrAbsolute))
                {
                    DiscountPercentOrAbsolute="0";
                }
                if(FuelAmount<=0 && DiscountParser.TryParseDiscountPercent(DiscountPercentOrAbsolute,out decimal discouuntValue))
                {
                    Console.WriteLine($"Parsed discount value: {discouuntValue}");
                    CheapestResultStations = TankCostService.GetCheapestStationDiscountPerCent(gasStations.Stations, fuelTypeForAPI, StationBrand, discouuntValue);
                }
                //Prozentuale Rabatt oder Absoluter Rabatt
                else if(FuelAmount>0)
                {
                    if(DiscountParser.TryParseDiscountPercent(DiscountPercentOrAbsolute, out decimal discountDecimal) || decimal.TryParse(DiscountPercentOrAbsolute, out discountDecimal))
                    {
                        Console.WriteLine($"Parsed discount value: {discountDecimal}");
                        CheapestResultStations = TankCostService.GetCheapestStationsTotalCostDiscountRelAbs(gasStations.Stations, FuelAmount, PricePerKm, fuelTypeForAPI, StationBrand, discountDecimal);
                    }
                    Console.WriteLine("Could not be parsed no calculation");
                }  
                
                decimal savingsToNearestTemp = 0;
                decimal savingsToCheapestTemp = 0;
                TankCostService.CaluclateSavings(gasStations.Stations, ref savingsToNearestTemp, ref savingsToCheapestTemp);
                SavingsToNearestStation = savingsToNearestTemp;
                SavingsToCheapestStation = savingsToCheapestTemp;
                CheapestResultStations = CheapestResultStations.Take(10).ToList();
                if (CheapestResultStations != null && CheapestResultStations.Any())
                {
                    // Protokolliere die Werte vor der Serialisierung
                    foreach (var station in CheapestResultStations)
                    {
                        Console.WriteLine($"Station: {station.Name}, FuelTypePrice: {station.FuelTypePrice}, TotalCalculatedCoast: {station.TotalCalculatedCoast}, LastUpdate: {station.LastUpdate}");
                    }

                    // Speichere die vollständigen GasStation-Objekte in der Session
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
        try
        {
            Console.WriteLine($"sortMode: {sortMode}");
            if (string.IsNullOrEmpty(sortMode))
            {
                return BadRequest("sortMode ist erforderlich");
            }
            SortMode = sortMode;

            // Lade die gespeicherten GasStation-Objekte aus der Session
            var stationsJson = HttpContext.Session.GetString(StationsSessionKey);
            Console.WriteLine($"Stations JSON from session: {stationsJson}");
            if (string.IsNullOrEmpty(stationsJson))
            {
                return Content("<p>Keine Tankstellen in der Sitzung gespeichert</p>");
            }

            var stations = JsonConvert.DeserializeObject<List<GasStation>>(stationsJson);
            if (stations == null || !stations.Any())
            {
                return Content("<p>Keine Tankstellen verfügbar</p>");
            }

            // Protokolliere die deserialisierten Werte
            Console.WriteLine($"Deserialized stations count: {stations.Count}");
            foreach (var station in stations)
            {
                Console.WriteLine($"Station: {station.Name}, FuelTypePrice: {station.FuelTypePrice}, TotalCalculatedCoast: {station.TotalCalculatedCoast}, LastUpdate: {station.LastUpdate}");
            }

            // Sortiere die Stationen
            var sortedStations = SortService.SortStations(stations, sortMode);

            // Erstelle ein IndexModel-Objekt
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
                    SortMode = (string)null,
                    Discount = string.Empty
                });
            var model = new IndexModel(_logger, _fuelPriceService, _context, (MarketFuelPriceService)_MarketfuelPriceService, _geoLocationService,_configuration)
            {
                CheapestResultStations = sortedStations,
                FuelAmount = inputData?.FuelAmount ?? 0,
                PricePerKm = inputData?.PricePerKm ?? 0,
                SelectedFuelType = inputData?.SelectedFuelType ?? FuelType.Diesel,
                SelectedCarType = inputData?.SelectedCarType ?? "",
                SavingsToCheapestStation = inputData?.SavingsToCheapestStation ?? 0,
                SavingsToNearestStation = inputData?.SavingsToNearestStation ?? 0,
                SortMode = sortMode ?? "",
                DiscountPercentOrAbsolute = inputData?.Discount ?? ""
            };

            var updatedInputData = new
            {
                FuelAmount = model.FuelAmount,
                PricePerKm = model.PricePerKm,
                SelectedFuelType = model.SelectedFuelType,
                SelectedCarType = model.SelectedCarType,
                SavingsToCheapestStation = model.SavingsToCheapestStation,
                SavingsToNearestStation = model.SavingsToNearestStation,
                SortMode = sortMode ?? "",
                DiscountPercentOrAbsolute = model.DiscountPercentOrAbsolute
            };
            HttpContext.Session.SetString(InputDataSessionKey, JsonConvert.SerializeObject(updatedInputData));
            Console.WriteLine($"Amount of stations to sort: {model.CheapestResultStations.Count}");

            // Aktualisiere die Session mit den sortierten Stationen
            HttpContext.Session.SetString(StationsSessionKey, JsonConvert.SerializeObject(sortedStations));

            // Gib die Partial View mit dem IndexModel zurück
            Console.WriteLine("Sort Mode after Sorting " + model.SortMode);
            Console.WriteLine("FuelAmount after Sorting " + model.FuelAmount);
            return Partial("_StationListPartial", model);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fehler in OnPostSort: {ex.Message}\n{ex.StackTrace}");
            return StatusCode(500, $"Interner Serverfehler: {ex.Message}");
        }
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