using System.Collections.Concurrent;
using FuelDistanceCalculator.Constants;
using FuelDistanceCalculator.Interfaces;
using FuelDistanceCalculator.Model;
using FuelDistanceCalculator.Services.Discount;
using Newtonsoft.Json;

namespace FuelDistanceCalculator.Services;

/// <summary>
/// Service class for handling tank station search and calculation logic
/// Extracted from IndexModel to improve modularity
/// </summary>
public class TankStationSearchService
{
    private readonly IMarketFuelPriceService _marketFuelPriceService;
    private readonly IGeoLocationService _geoLocationService;

    public TankStationSearchService(
        IMarketFuelPriceService marketFuelPriceService,
        IGeoLocationService geoLocationService)
    {
        _marketFuelPriceService = marketFuelPriceService;
        _geoLocationService = geoLocationService;
    }

    /// <summary>
    /// Executes the "Optimal Search" logic for finding cheapest tank stations
    /// </summary>
    public async Task<SearchResult> ExecuteSearchAsync(
        string place,
        double radius,
        FuelType fuelType,
        decimal pricePerKm,
        decimal fuelAmount,
        string? stationBrand,
        string? discountPercentOrAbsolute)
    {
        var result = new SearchResult();
        
        try
        {
            // Get coordinates for the place
            var coordinates = await _geoLocationService.GetCoordinatesAsync(place);
            
            if (coordinates == null)
            {
                result.Success = false;
                result.ErrorMessage = "Error getting coordinates";
                return result;
            }

            // Preserve the resolved coordinates so the view can center the map on the searched location.
            result.Latitude = coordinates.Latitude;
            result.Longitude = coordinates.Longitude;

            // Convert fuel type to API string
            var fuelTypeForApi = GetFuelTypeForApi(fuelType);
            
            // Get gas stations from API
            var gasStationsResult = await _marketFuelPriceService.GetGasStationsAsync(
                coordinates.Latitude, 
                coordinates.Longitude, 
                radius, 
                fuelTypeForApi);

            if (!gasStationsResult.IsSuccess)
            {
                result.Success = false;
                result.ErrorMessage = gasStationsResult.ErrorMessage;
                return result;
            }

            // Calculate distance from API
            var stationsWithDistance = await _geoLocationService.CalculateDistanceFromAPI(
                coordinates.Latitude, 
                coordinates.Longitude, 
                gasStationsResult.Stations);

            // Apply discount and calculate costs
            var applier = DiscountApplierFactory.CreateDiscountApplier(
                stationBrand ?? string.Empty, 
                discountPercentOrAbsolute ?? string.Empty, 
                fuelAmount);

            var cheapestStations = TankCostService.GetCheapestStation(
                stationsWithDistance, 
                pricePerKm, 
                fuelAmount, 
                fuelTypeForApi, 
                stationBrand ?? string.Empty, 
                applier);

            // Calculate savings using the original method name (contains intentional typo)
            decimal savingsToNearest = 0;
            decimal savingsToCheapest = 0;
            TankCostService.CaluclateSavings(stationsWithDistance, ref savingsToNearest, ref savingsToCheapest);

            result.Stations = cheapestStations;
            result.SavingsToNearestStation = savingsToNearest;
            result.SavingsToCheapestStation = savingsToCheapest;
            result.Success = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Error during search: {ex.Message}";
        }

        return result;
    }

    /// <summary>
    /// Calculates average costs for multiple locations in parallel. Returns results for each location
    /// even if some fail individually, marking failed locations with error messages.
    /// </summary>
    public async Task<AverageCostResult> CalculateAverageCostsAsync(
        List<string> places,
        List<double> radiusPlaces,
        FuelType fuelType)
    {
        var result = new AverageCostResult();
        
        try
        {
            // Filter empty/whitespace locations
            var filteredPlaces = places
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList();

            if (!filteredPlaces.Any())
            {
                result.Success = true;
                return result;
            }

            // Execute all calculations in parallel
            var taskList = filteredPlaces
                .Select((place, index) => Task.Run(() => 
                    CalculateSingleAverageCost(place, radiusPlaces, index)))
                .ToList();

            await Task.WhenAll(taskList);

            // Collect results
            foreach (var locationTask in taskList)
            {
                var locationResult = locationTask.Result;
                result.Locations.Add(locationResult);
            }
            
            // Check if any locations failed - overall success=true if at least one succeeded
            result.Success = result.Locations.Any(l => l.Success);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Error calculating average costs: {ex.Message}";
        }

        return result;
    }

    /// <summary>
    /// Calculates average cost for a single location
    /// </summary>
    private async Task<LocationCostResult> CalculateSingleAverageCost(
        string place,
        List<double> radiusPlaces,
        int index)
    {
        var result = new LocationCostResult { Name = place };
        
        try
        {
            var coordinates = await _geoLocationService.GetCoordinatesAsync(place);
            
            if (coordinates != null)
            {
                var radius = (index >= radiusPlaces.Count) ? 10 : radiusPlaces[index];
                var fuelTypeForApi = GetFuelTypeForApi(FuelType.Diesel); // TODO: Use parameter
                
                var gasStationsResult = await _marketFuelPriceService.GetGasStationsAsync(
                    coordinates.Latitude, 
                    coordinates.Longitude, 
                    radius, 
                    fuelTypeForApi);

                if (gasStationsResult.IsSuccess)
                {
                    // Get average cost from FuelPriceService
                    var avgCostResult = new FuelPriceService().CalculateAverageCost(gasStationsResult.Stations);
                    result.AverageCost = avgCostResult ?? 0m;
                    result.Success = true;
                }
            }
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"Error: {ex.Message}";
        }

        return result;
    }

    /// <summary>
    /// Converts a FuelType enum value to the API-compatible string representation.
    /// </summary>
    private string GetFuelTypeForApi(FuelType fuelType)
    {
        return fuelType switch
        {
            FuelType.Diesel => FuelType.Diesel.ToString(),
            FuelType.SuperE5 => "Super E5",
            FuelType.SuperE10 => "Super E10",
            _ => string.Empty
        };
    }
}

/// <summary>
/// Result class for search operations
/// </summary>
public class SearchResult
{
    public bool Success { get; set; }
    public List<GasStation>? Stations { get; set; }
    public decimal SavingsToNearestStation { get; set; }
    public decimal SavingsToCheapestStation { get; set; }
    /// <summary>
    /// Latitude of the geocoded location used to center the map. Defaults to 0 when geocoding fails.
    /// </summary>
    public double Latitude { get; set; }
    /// <summary>
    /// Longitude of the geocoded location used to center the map. Defaults to 0 when geocoding fails.
    /// </summary>
    public double Longitude { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Result class for average cost calculations
/// </summary>
public class AverageCostResult
{
    public bool Success { get; set; }
    public List<LocationCostResult> Locations { get; set; } = new();
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Result for a single location's average cost calculation
/// </summary>
public class LocationCostResult
{
    public string Name { get; set; } = "";
    public decimal AverageCost { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
