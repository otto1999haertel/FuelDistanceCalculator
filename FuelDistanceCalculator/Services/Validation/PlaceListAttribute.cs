using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace FuelDistanceCalculator.Model;

/// <summary>
/// Validiert eine Liste von Ortsnamen (z.B. IndexModel.NamePlaces):
/// - maximale Anzahl Einträge (Schutz vor Masseneinschleusung von Geocoding-Aufrufen
///   pro Request - das Rate-Limiting zählt Requests, nicht Array-Elemente)
/// - maximale Länge pro Eintrag
/// - Zeichen-Whitelist, identisch zur Prüfung auf IndexModel.Place, da NamePlaces[i]
///   über denselben Pfad (_geoLocationService.GetCoordinatesAsync) verarbeitet wird.
/// </summary>
public class PlaceListAttribute : ValidationAttribute
{
    private readonly int _maxCount;
    private readonly int _maxItemLength;

    // Deckt reine PLZ ("91052"), Ortsnamen ("Erlangen") und volle Adressen
    // ("Helene-Richter-Straße, 91052 Erlangen") ab, inkl. deutscher Umlaute/ß
    // über \p{L} (Unicode-Buchstabenkategorie statt nur a-zA-Z).
    // Leere Strings sind hier erlaubt (Platzhalter-Zeilen im Formular),
    // werden im Handler selbst per string.IsNullOrWhiteSpace übersprungen.
    private static readonly Regex Allowed = new(@"^[\p{L}0-9\s.,\-\/]*$", RegexOptions.Compiled);

    public PlaceListAttribute(int maxCount, int maxItemLength)
    {
        _maxCount = maxCount;
        _maxItemLength = maxItemLength;
    }

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not List<string> list)
        {
            return ValidationResult.Success;
        }

        if (list.Count > _maxCount)
        {
            return new ValidationResult($"Maximal {_maxCount} Orte erlaubt.");
        }

        foreach (var entry in list)
        {
            if (entry is null)
            {
                continue;
            }

            if (entry.Length > _maxItemLength)
            {
                return new ValidationResult($"Ortsname darf maximal {_maxItemLength} Zeichen lang sein.");
            }

            if (!Allowed.IsMatch(entry))
            {
                return new ValidationResult("Ortsname enthält ungültige Zeichen.");
            }
        }

        return ValidationResult.Success;
    }
}