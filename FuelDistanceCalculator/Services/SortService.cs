using FuelDistanceCalculator.Model;

namespace FuelDistanceCalculator.Services;

public static class SortService
{
    public static List<GasStation> SortStations(List<GasStation> stations, SortModeEnum sortMode)
    {
        return sortMode switch
        {
            SortModeEnum.fuelPrice => stations.OrderBy(gs => gs.FuelTypePrice).ToList(),
            SortModeEnum.totalCost=> stations.OrderBy(gs => gs.TotalCalculatedCoast).ToList(),
            SortModeEnum.distance => stations.OrderBy(gs => gs.Dist).ToList(),
            _ => stations
        };
    }
}