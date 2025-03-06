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
    public string NameGasStation1 {get;set;}

    [BindProperty]
    public string NameGasStation2 {get;set;}
       
    [BindProperty]
    public bool CalculationSucessful { get; set; }

    [BindProperty]
    public double TotalCost1 { get; private set; }

    [BindProperty]
    public double TotalCost2 { get; private set; }

    [BindProperty]
    public FuelType SelectedFuelType { get; set; }

    [BindProperty]
    public double FuelAmountBreakEven { get; set; }

    [BindProperty]
    public string NameGasStationBreakEven{get;set;}

    [BindProperty]
    public bool BreakEvenAnalysisDeterministic {get;set;}
    
    
    [BindProperty] 
    public InputMode SelectInputMode {get;set;}  = InputMode.man;

    [BindProperty]
    public VolumeUnit VolumeUnit{
            get => VolumeUnit.Liter;
    }

    [BindProperty]
    public int Radius {get; set;}   

    [BindProperty]
    public string Place {get; set;} 

    [BindProperty]
    public List<GasStation> CheapestResultStations { get; set; }

    public Dictionary<string, double> CarsAndRespectivePricePerkm { get; private set; } = new Dictionary<string, double>();

    [BindProperty]
    public string SelectedCarType{get; set;}

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
        NameGasStation1 = "Tankstelle 1";
        NameGasStation2 = "Tankstelle 2";
        SelectedFuelType = FuelType.Diesel;
        SelectInputMode = InputMode.auto;


        Console.WriteLine("get was executed and overwirte of values");
        FuelAmount = 0;
        PricePerKm = 0.60;
        FuelPrice1 = 0;
        Distance1 = 0;
        FuelPrice2 = 0;
        Distance2 = 0;
        Radius = 10;
        Place = "";

        CheapestResultStations = new List<GasStation>();

        if (TempData["TotalCost1"] != null && TempData["TotalCost2"] != null)
        {
            TotalCost1 = Convert.ToDouble(TempData["TotalCost1"]);
            TotalCost2 = Convert.ToDouble(TempData["TotalCost2"]);
            CalculationSucessful = true; // Falls es berechnete Werte gibt, setze auf erfolgreich
        }
        await GetCarsAndRespectivePricePerkm();
        SelectedCarType = CarsAndRespectivePricePerkm.FirstOrDefault().Key;
    }

    public async Task OnPostCalculateTotalCost()
    {
        await GetCarsAndRespectivePricePerkm();
        _fuelPriceService = new FuelPriceService((int)FuelAmount, PricePerKm);
        Console.WriteLine("calculate with seperate methode");
        TotalCost1 = _fuelPriceService.CalculateEntireCost(FuelPrice1, Distance1);
                TotalCost2 = _fuelPriceService.CalculateEntireCost(FuelPrice2, Distance2);
                if (TotalCost1 > 0 && TotalCost2 > 0)
                {
                    Console.WriteLine($"{NameGasStation1} : {TotalCost1}");
                     Console.WriteLine($"{NameGasStation2} : {TotalCost2}");
                    CalculationSucessful = true;
                    TempData["FuelPrice1"] = TotalCost1.ToString(); // Speichern in TempData
                    TempData["FuelPrice2"] = TotalCost2.ToString(); // Speichern in TempData
                    string [] tempBreakEvenAnalysis =  new string [2];
                    tempBreakEvenAnalysis = _fuelPriceService.AnalyseBreakEven(FuelPrice1, Distance1, NameGasStation1, FuelPrice2,Distance2, NameGasStation2);
                    if(tempBreakEvenAnalysis.Length ==2){
                        NameGasStationBreakEven = tempBreakEvenAnalysis[0];
                        double ergTemp=0;
                        double.TryParse(tempBreakEvenAnalysis[1], out ergTemp);
                        FuelAmountBreakEven = ergTemp;
                        BreakEvenAnalysisDeterministic =true;
                    }
                    else{
                        BreakEvenAnalysisDeterministic =false; 
                    }
                }
    }
    public void OnPostSave(){
        Console.WriteLine("save with seperate method");
                // Speichern durchführen
                //DateTime dateTime = new DateTime().Date;
                //DateTime dbTime = dateTime;
                DateTime germanTime = TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.FindSystemTimeZoneById("Europe/Berlin"));
                DateTime dbTime = DateTime.SpecifyKind(germanTime, DateTimeKind.Local); // Wichtig für PostgreSQL!

                Console.WriteLine($"{NameGasStation1} : {FuelPrice1}");
                Console.WriteLine($"{NameGasStation2} : {FuelPrice2}");
                Console.WriteLine($"Ausgewählte Spritart: {SelectedFuelType}");
                Console.WriteLine($"Zu tankende Menge: {FuelAmount}");
                Console.WriteLine(dbTime.ToString("HH:mm dd.MM.yyyy"));
                var tankinfo = new tankinfomodel
                {
                    // date = DateTime.SpecifyKind(dbTime, DateTimeKind.Unspecified),
                    timesaved  = dbTime.ToString("dd.MM.yyyy HH:mm"),
                    fueltype = SelectedFuelType.ToString(),
                    fuelamount = FuelAmount,
                    namegasstation1 = NameGasStation1,
                    fuelprice1 = FuelPrice1,
                    namegasstation2 = NameGasStation2,
                    fuelprice2 = FuelPrice2
                };

                // Speichern in der Datenbank
                _context.TankinfoModel.Add(tankinfo);
                _context.SaveChanges();

                TempData["Message"] = "Daten wurden erfolgreich gespeichert!";
    }

    public async Task OnPostSearch(){
        await GetCarsAndRespectivePricePerkm();
        Console.WriteLine("Search for optimum was executed");
        Console.WriteLine("Input mode in search case: " + SelectInputMode.ToString());
            Console.WriteLine("Radius " + Radius);
            Console.WriteLine("Place " + Place);
            Console.WriteLine("Fuel type  " + SelectedFuelType.ToString().ToLower());
            Console.WriteLine("Fuel Amount " + FuelAmount);
            Console.WriteLine("Price pro kilometer " + PricePerKm);
            string fuelTypeForAPI = "";
            switch (SelectedFuelType){
                case FuelType.Diesel:
                    fuelTypeForAPI = SelectedFuelType.ToString().ToLower();
                break;
                case FuelType.SuperE5:
                    fuelTypeForAPI = "e5";
                break;
                case FuelType.SuperE10:
                    fuelTypeForAPI="e10";
                break;
            }

            // API-Aufruf zur Koordinatensuche
            ApiThrottle geoThrottle = new ApiThrottle();
            ApiThrottle fuelThrottle = new ApiThrottle();

            var coordinates = await _geoLocationService.GetCoordinatesAsync(Place);
            Console.WriteLine("Koordinates from API " + coordinates);
            if(coordinates!=null){
                var gasStations = await fuelThrottle.ExecuteWithThrottle("FuelPrice", 
                () => _MarketfuelPriceService.GetGasStationsAsync(coordinates.Latitude, coordinates.Longitude, Radius, fuelTypeForAPI));
                Console.WriteLine("Response in Index, Listlänge" + gasStations.Count);
                CheapestResultStations = TankCostService.GetCheapestStations(gasStations,FuelAmount,PricePerKm);
                foreach (var station in CheapestResultStations)
                {
                    string finalAnswer = $"{station.Name}, {station.Place}, {station.Street}, {station.HouseNumber} Gesamtkosten: {(station.Price * FuelAmount + station.Distance * PricePerKm):F2} EUR, Entfernung {station.Distance}";
                    Console.WriteLine(finalAnswer);
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

    private async Task GetCarsAndRespectivePricePerkm()
    {
        Console.WriteLine($"Current Directory: {Directory.GetCurrentDirectory()}");
        if(CarsAndRespectivePricePerkm.Count!=0){
             return;
        }
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "ADAC_car_data.json");
            Console.WriteLine("Combined Path: " + filePath);
            var jsonContent = await System.IO.File.ReadAllTextAsync(filePath);
            CarsAndRespectivePricePerkm = JsonConvert.DeserializeObject<Dictionary<string, double>>(jsonContent);
    }
}