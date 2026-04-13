using System.Globalization;

namespace FuelDistanceCalculator.Services;

public static class DiscountParser
{
    public static bool TryParseDiscountPercent(string input, out decimal discountValuePercent)
    {
        discountValuePercent=0;
        if (string.IsNullOrWhiteSpace(input))
        { 
            return false;
        }

        input = input.Trim();
        bool isPercentage = input.EndsWith("%");
        string numericPart = isPercentage ? input.Substring(0, input.Length - 1) : input;

        if (decimal.TryParse(numericPart, NumberStyles.Any, CultureInfo.InvariantCulture, out discountValuePercent))
        {

            if (discountValuePercent < 0 || discountValuePercent > 100)
            {
                return false; // Negative discounts are not valid
            }
            if (isPercentage)
            {
                discountValuePercent /= 100; // Convert percentage to decimal
                return true;
            }
            return false;
        }
        else
        {
            return false; // Parsing failed
        }
    }
}
