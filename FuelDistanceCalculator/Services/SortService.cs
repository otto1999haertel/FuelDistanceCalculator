using FuelDistanceCalculator.Model;

namespace FuelDistanceCalculator.Services;

public static class SortService
{
    public static List<GasStation> SortStations(List<GasStation> stations, string sortMode)
    {
        return sortMode switch
        {
            "fuelPrice" => stations.OrderBy(gs => gs.FuelTypePrice).ToList(),
            "totalCost" => stations.OrderBy(gs => gs.TotalCalculatedCoast).ToList(),
            "distance" => stations.OrderBy(gs => gs.Dist).ToList(),
            _ => stations
        };
    }
}