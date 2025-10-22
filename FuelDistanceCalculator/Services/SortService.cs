using FuelDistanceCalculator.Model;

public static class SortService
{
    public static List<GasStation> SortStations(List<GasStation> stations, string sortMode)
    {
        Console.WriteLine("Sorting stations by mode: " + sortMode + "and amount of stations: " + stations.Count);
        return sortMode switch
        {
            "fuelPrice" => stations.OrderBy(gs => gs.FuelTypePrice).ToList(),
            "totalCost" => stations.OrderBy(gs => gs.TotalCalculatedCoast).ToList(),
            "distance" => stations.OrderBy(gs => gs.Dist).ToList(),
            _ => stations
        };
    }
}