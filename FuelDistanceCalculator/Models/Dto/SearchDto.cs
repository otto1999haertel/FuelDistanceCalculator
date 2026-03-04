using System.Collections.Generic;
using FuelDistanceCalculator.Constants;

namespace FuelDistanceCalculator.Model.Dto
{
    public class SearchParameters
    {
        public required string Place { get; set; }
        public int Radius { get; set; }
        public decimal FuelAmount { get; set; }
        public decimal PricePerKm { get; set; }
        public FuelType FuelType { get; set; }
        public string? StationBrand { get; set; }
        public decimal DiscountPercent { get; set; }
        public string? SortMode { get; set; }
    }

    public class SearchResult
    {
        public required SearchParameters Parameters { get; set; }
        public List<GasStation> Stations { get; set; } = new List<GasStation>();
        public decimal SavingsToNearestStation { get; set; }
        public decimal SavingsToCheapestStation { get; set; }
        public CoordinatesDTO? Coordinates { get; set; } // optional useful for clients
    }

    public class SortRequest
    {
        public required List<GasStation> Stations { get; set; }
        public string? SortMode { get; set; }
    }

    public class SortResponse
    {
        public List<GasStation> Stations { get; set; }
    }

    public class CompareRequest
    {
        public required List<SearchParameters> Locations { get; set; }
    }
}
