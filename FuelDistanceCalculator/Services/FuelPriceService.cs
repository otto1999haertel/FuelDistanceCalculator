using System.Collections.Generic;
using System.Runtime.Intrinsics;
public class FuelPriceService
{
    private decimal fuelAmount;
    private decimal pricePerkilometer;

    public FuelPriceService(decimal FuelAmount, decimal pricePerkilometer){
        this.pricePerkilometer=pricePerkilometer;
        this.fuelAmount = FuelAmount;
    }

    public decimal? CalculateAverageCost(List<GasStation> gasStations)
    {
        if (gasStations == null || gasStations.Count == 0){
            return 0;
        }

        return gasStations.OrderBy(gs => gs.FuelTypePrice).ToList().Take(10).Average(gs => gs.FuelTypePrice);
    }
}
