using FuelDistanceCalculator.Model;

namespace FuelDistanceCalculator.Interafces;

public interface IOilPriceService
{
    Task<OilPriceResult> GetOilPriceChangeAsync();
}