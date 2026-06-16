using FuelDistanceCalculator.Constants;
using FuelDistanceCalculator.Model;
namespace FuelDistanceCalculator.Interafces;
public interface IRouteOptimizationService
{
    public Task<GasStation> FindBestStationAsync(List<CoordinatesDTO> searchPointsOnRoute,
        FuelType fuelType,
        double litersToFill,
        double maxDistanceKm,
        double intervalKm);
}
