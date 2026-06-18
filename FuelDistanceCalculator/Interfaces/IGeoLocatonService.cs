using FuelDistanceCalculator.Model;
namespace FuelDistanceCalculator.Interfaces;

public interface IGeoLocationService
{
    Task<CoordinatesDTO> GetCoordinatesAsync(string place);
    Task<string> GetAddressFromCoordinatesAsync(double latitude, double longitude);
    public Task<List<GasStation>> CalculateDistanceFromAPI(double latitudeStart, double longitudeStart, List<GasStation> stations);

    public string NormalizeAddressKey(string place);
    public List<CoordinatesDTO> GetSearchPoints(List<CoordinatesDTO> route, double maxTotalDistanceKm = 15.0, double intervalKm = 5.0);

    public Task<List<CoordinatesDTO>> GetRouteIncludingStartPoint(double startLatitude, double startLong, double endLatitude, double endLongitude);

    public bool IsInForwardCone(CoordinatesDTO searchPoint, CoordinatesDTO nextRoutePoint, CoordinatesDTO checkPoint, double maxRadiusKm);
}