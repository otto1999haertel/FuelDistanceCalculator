using FuelDistanceCalculator.Model;
using FuelDistanceCalculator.Model.Dto;
using FuelDistanceCalculator.Constants;

using FuelDistanceCalculator.Interfaces;

namespace FuelDistanceCalculator.Services;

public class SearchService : ISearchService
{
    private readonly IGeoLocationService _geoLocationService;
    private readonly IMarketFuelPriceService _marketFuelPriceService;
    private readonly ApiThrottle _geoThrottle = new ApiThrottle();
    private readonly ApiThrottle _fuelThrottle = new ApiThrottle();
    private readonly ILogger<SearchService> _logger;

    public SearchService(IGeoLocationService geoLocationService,
                         IMarketFuelPriceService marketFuelPriceService,
                         ILogger<SearchService> logger)
    {
        _geoLocationService = geoLocationService;
        _marketFuelPriceService = marketFuelPriceService;
        _logger = logger;
    }

    public async Task<SearchResult> SearchAsync(SearchParameters parameters)
    {
        var result = new SearchResult { Parameters = parameters };

        if (parameters == null || string.IsNullOrWhiteSpace(parameters.Place))
        {
            _logger.LogWarning("SearchAsync called with empty parameters");
            return result;
        }

        // 1. geocode
        var coords = await _geoThrottle.ExecuteWithThrottle("Geo",
            () => _geoLocationService.GetCoordinatesAsync(parameters.Place));

        if (coords == null)
        {
            _logger.LogWarning("Geocoding returned null for place {Place}", parameters.Place);
            return result;
        }

        result.Coordinates = coords;

        // 2. fetch fuel stations
        var fuelTypeString = parameters.FuelType.ToApiString();
        var gasStations = await _fuelThrottle.ExecuteWithThrottle("FuelPrice",
            () => _marketFuelPriceService.GetGasStationsAsync(coords.Latitude, coords.Longitude,
                parameters.Radius, fuelTypeString, parameters.StationBrand, parameters.DiscountPercent));

        if (!gasStations.IsSuccess)
        {
            _logger.LogWarning("Fuel price API returned failure: {Error}", gasStations.ErrorMessage);
            result.Stations = new List<GasStation>();
            return result;
        }

        // 3. calculate distance for each station
        gasStations.Stations = await _fuelThrottle.ExecuteWithThrottle("DistanceCalculation",
            () => _geoLocationService.CalculateDistanceFromAPI(coords.Latitude.ToString(), coords.Longitude.ToString(), gasStations.Stations));

        // 4. compute cheapest ordering
        result.Stations = TankCostService.GetCheapestStationsAccordTotalCost(
            gasStations.Stations,
            parameters.FuelAmount,
            parameters.PricePerKm,
            fuelTypeString,
            parameters.StationBrand,
            parameters.DiscountPercent);

        // We cannot pass properties by ref, so use temporaries
        decimal nearest = 0, cheapest = 0;
        TankCostService.CalculateSavings(gasStations.Stations, ref nearest, ref cheapest);
        result.SavingsToNearestStation = nearest;
        result.SavingsToCheapestStation = cheapest;

        // 5. optional sort
        if (!string.IsNullOrEmpty(parameters.SortMode))
        {
            result.Stations = SortService.SortStations(result.Stations, parameters.SortMode);
        }

        return result;
    }

    public async Task<List<SearchResult>> CompareAsync(List<SearchParameters> locations)
    {
        if (locations == null || !locations.Any())
            return new List<SearchResult>();

        var tasks = locations.Select(SearchAsync);
        return (await Task.WhenAll(tasks)).ToList();
    }

    public List<GasStation> SortStations(List<GasStation> stations, string sortMode)
    {
        return SortService.SortStations(stations, sortMode);
    }
}
