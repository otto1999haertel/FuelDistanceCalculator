using System.Globalization;

namespace FuelDistanceCalculator.Services;

public static class DiscountParser
{
    public static bool TryParseDiscountOnTotalAbsolut(string input, out decimal discountValueAbsolute)
    {
        discountValueAbsolute = 0;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }

        input = input.Trim();
        if (decimal.TryParse(input, NumberStyles.Any, CultureInfo.InvariantCulture, out discountValueAbsolute))
        {
            if(discountValueAbsolute < 0)
            {
                return false; // Negative discounts are not valid
            }
            return true;
        }
        else
        {
            return false; // Parsing failed
        }
    }

    public static bool TryParseDiscountOnUnitPercentage(string input, out decimal discountValueOnUnit)
    {
        discountValueOnUnit = 0;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }
        input = input.Trim();
        bool isPerUnit = input.EndsWith("%");
        if(!isPerUnit)
        {
            return false; // Not a per-unit percentage
        }
        string numericPart = input.Substring(0, input.Length - 1); // Remove "%"
        if (decimal.TryParse(numericPart, NumberStyles.Any, CultureInfo.InvariantCulture, out discountValueOnUnit))
        {
            if (discountValueOnUnit < 0 || discountValueOnUnit > 100)
            {
                return false; // Negative discounts are not valid
            }
            discountValueOnUnit /= 100; // Convert percentage to decimal
            return true;
        }
        else
        {
            return false; // Parsing failed
        }

    }

    public static bool TryParseDiscountOnUnitAbsolut(string input, out decimal discountValueOnUnit)
    {
        discountValueOnUnit = 0;
        if (string.IsNullOrWhiteSpace(input))
        {
            return false;
        }
        input = input.Trim();
        bool isPerUnit = input.EndsWith("ct");
        if(!isPerUnit)
        {
            return false; // Not a per-unit discount
        }
        string numericPart = input.Substring(0, input.Length - 2); // Remove "ct"
        if (decimal.TryParse(numericPart, NumberStyles.Any, CultureInfo.InvariantCulture, out discountValueOnUnit))
        {
            if (discountValueOnUnit < 0)
            {
                return false; // Negative discounts are not valid
            }
             return true;
        }
        else
        {
            return false; // Parsing failed
        }

    }
}
