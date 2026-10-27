using FuelDistanceCalculator.Model;

namespace FuelDistanceCalculator.Services.Discount;

public abstract class DiscountApplier
{
    protected decimal _discountAmount;
    protected string _brand;

    public DiscountApplier(string brand, decimal discountAmount)
    {
        if (discountAmount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(discountAmount), "Discount amount cannot be negative.");
        }
        _discountAmount = discountAmount;
        _brand = brand;
    }
    public virtual void ApplyDiscount(GasStation station)
    {
        if (station == null)
        {
            throw new ArgumentNullException(nameof(station));
        }
        if (station.DiscountApplied)
        {
            return; // Discount already applied, skip further processing
        }
    }
}