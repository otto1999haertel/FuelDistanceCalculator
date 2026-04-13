namespace FuelDistanceCalculator.Constants;

public static class FuelTypeHelper
{
    public static readonly Dictionary<FuelType, string> FuelTypeNames = new()
    {
        { FuelType.Diesel, "Diesel" },
        { FuelType.SuperE10, "Super E10" },
        { FuelType.SuperE5, "Super E5" },
    };
}