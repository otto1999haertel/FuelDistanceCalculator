using FuelDistanceCalculator.Model;

namespace FuelDistanceCalculator.Services.Discount;


public class TotalAbsolutDiscount : DiscountApplier
{
    public TotalAbsolutDiscount(string brand, decimal discountAmount) : base(brand, discountAmount)
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

        station.TotalCombinedCosts -= _discountAmount;
        station.DiscountApplied = true;
    }
}