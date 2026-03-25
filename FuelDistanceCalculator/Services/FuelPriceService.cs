using FuelDistanceCalculator.Model;

namespace FuelDistanceCalculator.Services;

public class FuelPriceService
{

    public FuelPriceService()
    {
    }

    public decimal? CalculateAverageCost(List<GasStation> gasStations)
    {
        if (gasStations == null || gasStations.Count == 0)
        {
            return 0;
        }
        return gasStations.OrderBy(gs => gs.FuelTypePrice).ToList().Take(10).Average(gs => gs.FuelTypePrice);
    }
}
