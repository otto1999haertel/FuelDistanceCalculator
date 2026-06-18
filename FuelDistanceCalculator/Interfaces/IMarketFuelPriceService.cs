using FuelDistanceCalculator.Model;
namespace FuelDistanceCalculator.Interfaces;

public interface IMarketFuelPriceService
{
    Task<GasStationResult> GetGasStationsAsync(double latitude, double longitude, double radius, string fueltype);
}