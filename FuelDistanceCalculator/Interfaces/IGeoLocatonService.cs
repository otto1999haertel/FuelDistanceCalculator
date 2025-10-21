using System.Collections.Generic;
using System.Threading.Tasks;

namespace FuelDistanceCalculator.Services
{
    public interface IGeoLocationService
    {
        Task<CoordinatesDTO> GetCoordinatesAsync(string place);
        Task<string> GetAddressFromCoordinatesAsync(double latitude, double longitude);
        Task<List<GasStation>> CalculateDistance(string latitudeStart, string longitudeStart, List<GasStation> stations);
    }
}