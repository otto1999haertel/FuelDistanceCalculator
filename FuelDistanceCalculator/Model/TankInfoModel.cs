namespace FuelDistanceCalculator.Model;

public class tankinfomodel
{
    public int id { get; set; }  // Primärschlüssel (ID der Zeile)

    public string timesaved { get; set; } // Datum der Erfassung

    public string fueltype { get; set; } // Spritart (z.B. Benzin, Diesel)

    public double fuelamount { get; set; } // Menge des getankten Sprits (in Litern)

    public string namegasstation1 { get; set; } // Name oder Adresse von Tankstelle 1

    public double fuelprice1 { get; set; } // Preis des Sprits an Tankstelle 1

    public string namegasstation2 { get; set; } // Name oder Adresse von Tankstelle 2

    public double fuelprice2 { get; set; } // Preis des Sprits an Tankstelle 2
}

