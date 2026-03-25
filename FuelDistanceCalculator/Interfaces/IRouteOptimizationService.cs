using FuelDistanceCalculator.Model;
namespace FuelDistanceCalculator.Services;
public interface IRouteOptimizationService
{
    public Task<GasStation> FindBestStationAsync(List<CoordinatesDTO> searchPointsOnRoute,
        FuelType fuelType,
        double litersToFill,
        double maxDistanceKm,
        double intervalKm);
}
