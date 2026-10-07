using System.ComponentModel.DataAnnotations;

namespace FuelDistanceCalculator.Model;

/// <summary>
/// Validiert eine Liste von Umkreis-Werten (IndexModel.RadiusPlaces).
/// Gleicher Wertebereich wie IndexModel.Radius ([Range(1,25)]) - ohne diese
/// Prüfung könnte ein manipulierter Request z.B. RadiusPlaces: [99999, 99999, ...]
/// senden und damit unverhältnismäßig große Umkreissuchen gegen die
/// kostenpflichtige Tankstellen-API auslösen.
/// </summary>
public class RadiusListAttribute : ValidationAttribute
{
    private readonly double _min;
    private readonly double _max;

    public RadiusListAttribute(double min, double max)
    {
        _min = min;
        _max = max;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not List<double> list)
        {
            return ValidationResult.Success;
        }

        if (list.Any(r => r < _min || r > _max))
        {
            return new ValidationResult($"Radius muss zwischen {_min} und {_max} liegen.");
        }

        return ValidationResult.Success;
    }
}