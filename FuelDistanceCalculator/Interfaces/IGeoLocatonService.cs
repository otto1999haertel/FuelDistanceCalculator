using FuelDistanceCalculator.Model;
namespace FuelDistanceCalculator.Services
{
    public interface IGeoLocationService
    {
        Task<CoordinatesDTO> GetCoordinatesAsync(string place);
        Task<string> GetAddressFromCoordinatesAsync(double latitude, double longitude);
        Task<List<GasStation>> CalculateDistance(string latitudeStart, string longitudeStart, List<GasStation> stations);
        public string NormalizeAddressKey(string place);
        public List<CoordinatesDTO> GetSearchPoints(List<CoordinatesDTO> route, double maxTotalDistanceKm = 15.0,  double intervalKm = 5.0);

        public Task<List<CoordinatesDTO>> GetRouteIncludingStartPoint(string startLatitude, string startLong, string endLatitude, string endLongitude);
    }
}