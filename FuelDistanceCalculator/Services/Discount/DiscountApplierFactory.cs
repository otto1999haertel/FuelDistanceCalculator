using Microsoft.IdentityModel.Tokens;

namespace FuelDistanceCalculator.Services.Discount;

public static class DiscountApplierFactory
{
    public static DiscountApplier CreateDiscountApplier(string brand, string discountAmount, decimal fuelAmount)
    {
        if (discountAmount.IsNullOrEmpty())
        {
            return null; // No discount to apply
        }
        if(fuelAmount > 0 && DiscountParser.TryParseDiscountOnTotalAbsolut(discountAmount, out decimal parsedDiscountAmount))
        {
            return new TotalAbsolutDiscount(brand, parsedDiscountAmount);
        }
        else if (DiscountParser.TryParseDiscountOnUnitAbsolut(discountAmount, out decimal parsedPercentage))
        {
            return new UnitAbsolutDiscount(brand, parsedPercentage);
        }
        else if (DiscountParser.TryParseDiscountOnUnitPercentage(discountAmount, out parsedPercentage))
        {
            return new UnitPercentageDiscount(brand, parsedPercentage);
        }
        else
        {
            return null;
        }
    }
}