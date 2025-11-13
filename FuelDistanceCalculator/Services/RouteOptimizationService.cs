using FuelDistanceCalculator.Model;

public class RouteOptimizationService : IRouteOptimizationService
{
    public Task<GasStation> FindBestStationAsync(List<CoordinatesDTO> searchPointsOnRoute, FuelType fuelType, double litersToFill, double maxDistanceKm, double intervalKm)
    {
        //Foreach searpoint search for Gasstaions in maxDistance
        //Check Gasstations if in forward cone
        //Calculate total cost for each found GasStation
        throw new NotImplementedException();
    }
}