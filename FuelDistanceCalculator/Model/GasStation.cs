using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

public class GasStation
{
    [JsonPropertyName("country")]
    public string Country { get; set; }

    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("brand")]
    public string Brand { get; set; }

    [JsonPropertyName("street")]
    public string Street { get; set; }

    [JsonPropertyName("postalCode")]
    public string PostalCode { get; set; }  // Als string, da Postleitzahlen führende Nullen haben könnten

    [JsonPropertyName("place")]
    public string Place { get; set; }

    [JsonPropertyName("coords")]
    public Coordinates Coords { get; set; }

    [JsonPropertyName("isOpen")]
    public bool IsOpen { get; set; }

    [JsonPropertyName("closesAt")]
    public string ClosesAt { get; set; }  // Als string, kann bei Bedarf zu DateTime geparst werden

    [JsonPropertyName("dist")]
    public double? Dist { get; set; }  // Umbenannt für Klarheit, nullable wie dein Distance

    [JsonPropertyName("fuels")]
    public List<Fuel> Fuels { get; set; }

    [JsonPropertyName("volatility")]
    public int Volatility { get; set; }

    public decimal TotalCalculatedCoast { get; private set; }

    private decimal _fuelprice;
    
    public decimal? FuelTypePrice
    {
        get
        {
            return _fuelprice;
        }

        private set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException("Price cannot be negative.");
            }
            _fuelprice = (decimal)value;
        }
    }
    private string? m_lastUpdate;
    public string? LastUpdate
    {
        get { return m_lastUpdate; }
        private set { m_lastUpdate = value; }
    }

    public decimal CalculateTotalCost(double fuelAmount, double pricePerKm)
    {
        TotalCalculatedCoast = 0;
        return TotalCalculatedCoast;
    }

    public void SetPrice(string fuelType)
    {
        FuelTypePrice = Fuels.Where(x => x.Name.Equals(fuelType, StringComparison.OrdinalIgnoreCase))
                     .Select(x => (decimal)x.Price)
                     .FirstOrDefault();
    }
    
    public void SetUpdateTime(string fuelType)
    {
        m_lastUpdate = Fuels.Where(x=>x.Name.Equals(fuelType, StringComparison.OrdinalIgnoreCase))
                     .Select(x=> x.LastChange.Timestamp)
                     .FirstOrDefault();
    }


    // Überschreiben der ToString-Methode für bessere Debug-Ausgabe
    public override string ToString()
    {
        return $"GasStation Info:\n" +
           $"- Id: {Id}\n" +
           $"- Name: {Name}\n" +
           $"- Brand: {Brand}\n" +
           $"- Street: {Street}\n" +
           $"- Place: {Place}\n" +
           $"- Coordinates: {Coords.Lat}, {Coords.Lng}\n" +
           $"- Distance: {Dist} km\n" +
           $"- Is Open: {(IsOpen ? "Yes" : "No")}\n" +
           $"- PostCode: {PostalCode}\n" +
           $"- Cloases At: {ClosesAt}\n" + 
           $"- Last Update: {LastUpdate}\n";
    }
}
