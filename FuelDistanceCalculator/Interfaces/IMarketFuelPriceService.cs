using FuelDistanceCalculator.Model;
namespace FuelDistanceCalculator.Services
{
    public interface IMarketFuelPriceService
    {
        Task<GasStationResult> GetGasStationsAsync(double latitude, double longitude, double radius, string fueltype);
    }
}