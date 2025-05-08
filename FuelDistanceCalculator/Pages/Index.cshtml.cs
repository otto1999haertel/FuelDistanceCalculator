using System.Threading.Tasks;
using FuelDistanceCalculator.Constants;
using FuelDistanceCalculator.Data;
using FuelDistanceCalculator.Model;
using FuelDistanceCalculator.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;

namespace FuelDistanceCalculator.Pages;

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
    public int RadiusPlace1 { get; set; }


    [BindProperty]
    public string NamePlace2 { get; set; }

    [BindProperty]
    public int RadiusPlace2 { get; set; }

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
    public InputMode SelectInputMode { get; set; } = InputMode.man;

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
    }

    public async Task OnGetAsync()
    {
        ViewData["ContactName"] = ContactInfo.Name;
        NamePlace1 = "";
        RadiusPlace1 = 10;
        NamePlace2 = "";
        RadiusPlace2 = 10;
        SelectedFuelType = FuelType.Diesel;
        SelectInputMode = InputMode.auto;


        Console.WriteLine("get was executed and overwirte of values");
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

    public async Task OnPostCalculateAverageCost()
    {
        await GetCarsAndRespectivePricePerkm();
        _fuelPriceService = new FuelPriceService((int)FuelAmount, PricePerKm);
        Console.WriteLine("calculate average cost with seperate methode");
        Console.WriteLine("Name Place 1 " + NamePlace1);
        Console.WriteLine("Radius Place 1 " + RadiusPlace1);
        Console.WriteLine("Name Place 2 " + NamePlace2);
        Console.WriteLine("Radius Place 2 " + RadiusPlace2);

        // API-Aufruf zur Koordinatensuche
        ApiThrottle geoThrottle = new ApiThrottle();
        ApiThrottle fuelThrottle = new ApiThrottle();
        string fuelTypeForAPI = GetFuelTypeForAPI();
        var coordinatesPlace1 = await _geoLocationService.GetCoordinatesAsync(NamePlace1);
        var coordinatesPlace2 = await _geoLocationService.GetCoordinatesAsync(NamePlace2);
        if (coordinatesPlace1 != null && coordinatesPlace2 != null)
        {
            var gasStationsPlace1 = await fuelThrottle.ExecuteWithThrottle("FuelPrice",
                    () => _MarketfuelPriceService.GetGasStationsAsync(coordinatesPlace1.Latitude, coordinatesPlace1.Longitude, RadiusPlace1, fuelTypeForAPI));
            var gasStationsPlace2 = await fuelThrottle.ExecuteWithThrottle("FuelPrice",
                    () => _MarketfuelPriceService.GetGasStationsAsync(coordinatesPlace2.Latitude, coordinatesPlace2.Longitude, RadiusPlace2, fuelTypeForAPI));
            if(gasStationsPlace1.IsSuccess && gasStationsPlace2.IsSuccess){
                    Console.WriteLine("Gasstaion place 1 count" + " : " + gasStationsPlace1.Stations.Count);
                    Console.WriteLine("Gasstaion place 2 count" + " : " + gasStationsPlace2.Stations.Count);
                    double? averageCostPlace1 = _fuelPriceService.CalculateAverageCost(gasStationsPlace1.Stations);
                    double? averageCostPlace2 = _fuelPriceService.CalculateAverageCost(gasStationsPlace2.Stations);
                    if (averageCostPlace1 != null && averageCostPlace2 != null)
                    {
                        CalculationSucessful = true;
                        AverageCostPlace1 = (double)averageCostPlace1;
                        AverageCostPlace2 = (double)averageCostPlace2;
                    }
            }
            else{
                Console.WriteLine("Error in search Tanker-API Request" + gasStationsPlace1.ErrorMessage);
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = "Fehler bei Tankstellenabfrage";
            }
        }
    }
    public void OnPostSave()
    {
        Console.WriteLine("save with seperate method");
        // Speichern durchführen
        //DateTime dateTime = new DateTime().Date;
        //DateTime dbTime = dateTime;
        DateTime germanTime = TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin"));
        DateTime dbTime = DateTime.SpecifyKind(germanTime, DateTimeKind.Local); // Wichtig für PostgreSQL!

        Console.WriteLine($"{NamePlace1} : {FuelPrice1}");
        Console.WriteLine($"{NamePlace2} : {FuelPrice2}");
        Console.WriteLine($"Ausgewählte Spritart: {SelectedFuelType}");
        Console.WriteLine($"Zu tankende Menge: {FuelAmount}");
        Console.WriteLine(dbTime.ToString("HH:mm dd.MM.yyyy"));
        var tankinfo = new tankinfomodel
        {
            // date = DateTime.SpecifyKind(dbTime, DateTimeKind.Unspecified),
            timesaved = dbTime.ToString("dd.MM.yyyy HH:mm"),
            fueltype = SelectedFuelType.ToString(),
            fuelamount = FuelAmount,
            namegasstation1 = NamePlace1,
            fuelprice1 = FuelPrice1,
            namegasstation2 = NamePlace2,
            fuelprice2 = FuelPrice2
        };

        // Speichern in der Datenbank
        _context.TankinfoModel.Add(tankinfo);
        _context.SaveChanges();

        TempData["Message"] = "Daten wurden erfolgreich gespeichert!";
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
                    string finalAnswer = $"{station.Name}, {station.Place}, {station.Street}, {station.HouseNumber} Gesamtkosten: {(station.Price * FuelAmount + station.Distance * PricePerKm):F2} EUR, Entfernung {station.Distance}";
                    Console.WriteLine(finalAnswer);
                }
            }
            else{
                //TODO: Error Handling
                Console.WriteLine("Error in search Tanker-API Request" + gasStations.ErrorMessage);
                TempData["ToastType"] = "error";
                TempData["ToastMessage"] = "Fehler bei Tankstellenabfrage";
            }
        }
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