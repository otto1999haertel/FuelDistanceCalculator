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
    private readonly GeoLocationService _geoLocationService;

    [BindProperty]
    public double FuelAmount { get; set; } // Globale Tankmenge für beide Tankstellen
    [BindProperty]
    public double PricePerKm { get; set; } // Preis pro Kilometer für beide Tankstellen

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

    [BindProperty]
    public Dictionary<string, double> CalculatedAverageCosts { get; set; }

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

    public Dictionary<string, double> CarsAndRespectivePricePerkm { get; private set; } = new Dictionary<string, double>();

    [BindProperty]
    public string SelectedCarType { get; set; }

    public IndexModel(ILogger<IndexModel> logger, FuelPriceService fuelPrice, AppDbContext context, MarketFuelPriceService marketFuelPriceService, GeoLocationService geoLocationService)
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

        CalculatedAverageCosts = new Dictionary<string, double>();
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
        PricePerKm = 0.25;
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
            if(gasStations.IsSuccess){
                Console.WriteLine("Response in Index, Listlänge" + gasStations.Stations.Count);
                CheapestResultStations = TankCostService.GetCheapestStations(gasStations.Stations, FuelAmount, PricePerKm);
                foreach (var station in CheapestResultStations)
                {
                    string finalAnswer = $"{station.Name}, {station.Place}, {station.Street}, {station.HouseNumber} Gesamtkosten: {(station.Price * FuelAmount + station.Distance * PricePerKm):F2} EUR, Entfernung {station.Distance}, Latitude {station.Latitude}, Longitude {station.Longitude}";
                    Console.WriteLine(finalAnswer);
                }
            }
            else{
                Console.WriteLine("Error in search Tanker-API Request" + gasStations.ErrorMessage);
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = "Fehler bei Tankstellenabfrage";
            }
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
        Console.WriteLine("Place recevied from ccordinates" + Place);
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
        foreach (var key in Request.Form.Keys)
        {
            Console.WriteLine($"FORM: {key} = {Request.Form[key]}");
        }
        Console.WriteLine("Anzahl der Orte: " + NamePlaces.Count);
        CalculatedAverageCosts = new Dictionary<string, double>();
        ApiThrottle geoThrottle = new ApiThrottle();
        ApiThrottle fuelThrottle = new ApiThrottle();

        await GetCarsAndRespectivePricePerkm();
        _fuelPriceService = new FuelPriceService((int)FuelAmount, PricePerKm);
        string fuelTypeForAPI = GetFuelTypeForAPI();
        for (int i = 0; i < NamePlaces.Count; i++)
        {
            Console.WriteLine($"NamePlaces[{i}]: {NamePlaces[i]}, Radius: {RadiusPlaces[i]}");
            if (!string.IsNullOrWhiteSpace(NamePlaces[i]))
            {
                var coordinatesPlace = await _geoLocationService.GetCoordinatesAsync(NamePlaces[i]);

                if (coordinatesPlace != null)
                {
                    double radiusPlace = (i >= RadiusPlaces.Count || RadiusPlaces[i] <= 0) ? 10 : RadiusPlaces[i];
                    var gasStationsPlace1 = await fuelThrottle.ExecuteWithThrottle("FuelPrice",
                    () => _MarketfuelPriceService.GetGasStationsAsync(coordinatesPlace.Latitude, coordinatesPlace.Longitude, radiusPlace, fuelTypeForAPI));
                    CalculatedAverageCosts[NamePlaces[i]] = _fuelPriceService.CalculateAverageCost(gasStationsPlace1.Stations) ?? 0.0;
                }
            }
        }
        Console.WriteLine("Calculated Average Costs: " + CalculatedAverageCosts.Count);
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
        CarsAndRespectivePricePerkm = JsonConvert.DeserializeObject<Dictionary<string, double>>(jsonContent);
    }

    private string GetFuelTypeForAPI()
    {
        switch (SelectedFuelType)
        {
            case FuelType.Diesel:
                return SelectedFuelType.ToString().ToLower();
            case FuelType.SuperE5:
                return "e5";
            case FuelType.SuperE10:
                return "e10";
        }
        return string.Empty;
    }
}