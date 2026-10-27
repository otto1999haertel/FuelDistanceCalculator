using FuelDistanceCalculator.Model;

namespace FuelDistanceCalculator.Services.Discount;


public class UnitAbsolutDiscount : DiscountApplier
{
    public UnitAbsolutDiscount(string brand, decimal discountAmount) : base(brand, discountAmount)
    {
    }

    public override void ApplyDiscount(GasStation station)
    {
        base.ApplyDiscount(station);
        if (!string.Equals(station.Brand, _brand, StringComparison.OrdinalIgnoreCase))
        {
            station.DiscountApplied = false;
            return;
        }

        if (station.FuelTypePrice.HasValue)
        {
            decimal discountedPrice = station.FuelTypePrice.Value - _discountAmount;
            station.FuelTypePrice = Math.Max(discountedPrice, 0); // Ensure price doesn't go below zero
            station.DiscountApplied = true;
        }
    }
}