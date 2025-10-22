using FuelDistanceCalculator.Model;
namespace FuelDistanceCalculator.Services
{
    public interface IGeoLocationService
    {
        Task<CoordinatesDTO> GetCoordinatesAsync(string place);
        Task<string> GetAddressFromCoordinatesAsync(double latitude, double longitude);
        Task<List<GasStation>> CalculateDistance(string latitudeStart, string longitudeStart, List<GasStation> stations);
        public string NormalizeAddressKey(string place);
    }
}