using FuelDistanceCalculator.Constants;

namespace FuelDistanceCalculator.Constants
{
    public static class FuelTypeExtensions
    {
        public static string ToApiString(this FuelType fuelType)
        {
            return fuelType switch
            {
                FuelType.Diesel => "Diesel",
                FuelType.SuperE5 => "Super E5",
                FuelType.SuperE10 => "Super E10",
                _ => fuelType.ToString()
            };
        }
    }
}