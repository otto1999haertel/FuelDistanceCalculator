using FuelDistanceCalculator.Model;

namespace FuelDistanceCalculator.Interfaces;

public interface IOilPriceService
{
    Task<OilPriceResult> GetOilPriceChangeAsync();
}