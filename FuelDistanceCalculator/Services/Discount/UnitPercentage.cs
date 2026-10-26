using FuelDistanceCalculator.Model;
namespace FuelDistanceCalculator.Services.Discount;

public class UnitPercentage : DiscountApplier
{
    public UnitPercentage(string brand, decimal discountAmount) : base(brand, discountAmount)
    {
    }

    public override void ApplyDiscount(GasStation station)
    {
        if (station == null)
        {
            throw new ArgumentNullException(nameof(station));
        }

        if (!string.Equals(station.Brand, _brand, StringComparison.OrdinalIgnoreCase))
        {
            station.DiscountApplied = false;
            return;
        }
        

        if (station.FuelTypePrice.HasValue)
        {
            decimal discountedPrice = station.FuelTypePrice.Value * (1 - _discountAmount);
            station.FuelTypePrice = Math.Max(discountedPrice, 0); // Ensure price doesn't go below zero
            station.DiscountApplied = true;
        }
    }
}