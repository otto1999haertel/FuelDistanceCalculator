using System.ComponentModel.DataAnnotations;
using FuelDistanceCalculator.Pages;
using FuelDistanceCalculator.Services; // Anpassen an deine Namespaces

namespace FuelDistanceCalculator.Validation;

[AttributeUsage(AttributeTargets.Property)]
public class CarTypeExistsAttribute : ValidationAttribute
{
    public CarTypeExistsAttribute() : base("Unbekannter Fahrzeugtyp.") { }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        // 1. Wenn kein Fahrzeug gewählt wurde, ist die Validierung erfolgreich (optionales Feld)
        if (value is not string carType || string.IsNullOrWhiteSpace(carType))
        {
            return ValidationResult.Success;
        }

        // 2. Prüfen, ob wir im Kontext des IndexModel sind
        if (validationContext.ObjectInstance is IndexModel model)
        {
            // Falls das Dictionary noch nicht geladen wurde, laden wir es hier synchron nach
            if (model.CarsAndRespectivePricePerkm == null || model.CarsAndRespectivePricePerkm.Count == 0)
            {
                var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "ADAC_car_data.json");
                if (File.Exists(filePath))
                {
                    // Synchronous read inside attribute validation
                    var carDataTask = CarDataParser.ParseCarData(filePath);
                    var carData = carDataTask.GetAwaiter().GetResult();

                    if (carData != null && carData.ContainsKey(carType))
                    {
                        return ValidationResult.Success;
                    }
                }
            }
            else if (model.CarsAndRespectivePricePerkm.ContainsKey(carType))
            {
                return ValidationResult.Success;
            }
        }

        return new ValidationResult(ErrorMessage);
    }
}