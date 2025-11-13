using FuelDistanceCalculator.Model;

public class RouteOptimizationService : IRouteOptimizationService
{
    public Task<GasStation> FindBestStationAsync(List<CoordinatesDTO> searchPointsOnRoute, FuelType fuelType, double litersToFill, double maxDistanceKm, double intervalKm)
    {
        throw new NotImplementedException();
    }
}