using System.ComponentModel.DataAnnotations;
using FuelDistanceCalculator.Services; // Ensure this matches your DiscountParser namespace

namespace FuelDistanceCalculator.Validation;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class DiscountFormatAttribute : ValidationAttribute
{
    public DiscountFormatAttribute()
        : base("Ungültiges Rabattformat.")
    {
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        // Treat null or empty string as valid (optional field).
        // Use [Required] on the property if you ever want to make it mandatory.
        if (value is not string input || string.IsNullOrWhiteSpace(input))
        {
            return ValidationResult.Success;
        }

        // Delegate validation directly to DiscountParser
        Console.WriteLine($"Validating discount input: '{input}'");
        if (DiscountParser.TryParseDiscountOnTotalAbsolut(input, out _) || 
            DiscountParser.TryParseDiscountOnUnitPercentage(input, out _)
            || DiscountParser.TryParseDiscountOnUnitAbsolut(input, out _)
            )
        {
            return ValidationResult.Success;
        }

        return new ValidationResult(ErrorMessage);
    }
}