using Newtonsoft.Json;

namespace FuelDistanceCalculator.Model;

public class GasStation
{
    [JsonProperty("country")]
    public string Country { get; set; }

    [JsonProperty("id")]
    public string Id { get; set; }

    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("brand")]
    public string Brand { get; set; }

    [JsonProperty("street")]
    public string Street { get; set; }

    [JsonProperty("postalCode")]
    public string PostalCode { get; set; }

    [JsonProperty("place")]
    public string Place { get; set; }

    [JsonProperty("coords")]
    public Coordinates Coords { get; set; }

    [JsonProperty("isOpen")]
    public bool IsOpen { get; set; }

    [JsonProperty("closesAt")]
    public string ClosesAt { get; set; }

    [JsonProperty("dist")]
    public double? Dist { get; set; }

    [JsonProperty("fuels")]
    public List<Fuel> Fuels { get; set; }

    [JsonProperty("volatility")]
    public int Volatility { get; set; }

    [JsonProperty("totalCalculatedCoast")]
    public decimal TotalCalculatedCoast
    {
        get => _totalCoast;
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException("TotalCalculatedCoast cannot be negative.");
            }
            _totalCoast = value;
        }
    }
    private decimal _totalCoast;

    [JsonProperty("fuelTypePrice")]
    public decimal? FuelTypePrice
    {
        get => _fuelPrice;
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException("FuelTypePrice cannot be negative.");
            }
            _fuelPrice = value;
        }
    }
    private decimal? _fuelPrice;

    [JsonProperty("lastUpdate")]
    public string? LastUpdate { get; set; }

    [JsonProperty("updateAmount")]
    public decimal? UpdateAmount { get; set; }

    public decimal CalculateTotalCostDoubleWay(decimal fuelAmount, decimal pricePerKm)
    {
        if (fuelAmount <= 0 || pricePerKm < 0 || Dist == null || Dist < 0)
        {
            throw new ArgumentException("Ungültige Eingabewerte: FuelAmount muss positiv sein, PricePerKm nicht negativ, Dist vorhanden und nicht negativ.");
        }

        decimal dist = (decimal)Dist.Value;
        decimal fuelCost = (_fuelPrice ?? 0) * fuelAmount;
        decimal travelCost = pricePerKm * dist * 2m;
        decimal rawTotal = fuelCost + travelCost;
        TotalCalculatedCoast = Math.Round(rawTotal, 2, MidpointRounding.AwayFromZero);

        Console.WriteLine($"Total Cost for station {Name} (ID: {Id}): {_totalCoast:F2} € " +
                          $"(Fuel Cost: {fuelCost:F2}, Travel Cost: {travelCost:F2}, " +
                          $"Fuel Price: {_fuelPrice:F3}, Fuel Amount: {fuelAmount:F0}, Price per Km: {pricePerKm:F2}, Distance: {dist:F2})");

        return TotalCalculatedCoast;
    }

    public void SetPrice(string fuelType, string brand = "", decimal discount = 0)
    {
        FuelTypePrice = Fuels?.Where(x => x.Name.Equals(fuelType, StringComparison.OrdinalIgnoreCase))
                    .Select(x => (decimal)x.Price)
                    .FirstOrDefault() ?? 0;
        
        // only apply discount if we actually have a brand string and it matches the requested brand
        if (!string.IsNullOrEmpty(Brand) &&
            Brand.Equals(brand, StringComparison.OrdinalIgnoreCase) &&
            FuelTypePrice.HasValue && discount > 0)
        {
            Console.WriteLine($"Applying discount of {discount}% for brand {brand} on station {Name}");
            decimal discountAmount = (FuelTypePrice.Value * discount) / 100m;
            FuelTypePrice -= discountAmount;
            FuelTypePrice = Math.Round(FuelTypePrice.Value, 3);
        }
        
        Console.WriteLine($"SetPrice called for FuelType: {fuelType}, Brand: {brand}, Discount: {discount}. Resulting FuelTypePrice: {FuelTypePrice}");
    }

    public void SetUpdateTime(string fuelType)
    {
        LastUpdate = Fuels?.Where(x => x.Name.Equals(fuelType, StringComparison.OrdinalIgnoreCase))
                     .Select(x => x.LastChange?.Timestamp)
                     .FirstOrDefault();
    }

    public void SetUpdateAmount (string fuelType)
    {
        UpdateAmount = Fuels?.Where(x => x.Name.Equals(fuelType, StringComparison.OrdinalIgnoreCase))
                     .Select(x => (decimal?)x.LastChange?.Amount)
                     .FirstOrDefault();
    }

    public override string ToString()
    {
        return $"GasStation Info:\n" +
               $"- Id: {Id}\n" +
               $"- Name: {Name}\n" +
               $"- Brand: {Brand}\n" +
               $"- Street: {Street}\n" +
               $"- Place: {Place}\n" +
               $"- Coordinates: {Coords?.Lat}, {Coords?.Lng}\n" +
               $"- Distance: {Dist} km\n" +
               $"- Is Open: {(IsOpen ? "Yes" : "No")}\n" +
               $"- PostCode: {PostalCode}\n" +
               $"- Closes At: {ClosesAt}\n" +
               $"- Last Update: {LastUpdate}\n" +
               $"- FuelTypePrice: {FuelTypePrice}\n" +
               $"- TotalCalculatedCoast: {TotalCalculatedCoast}";
    }
}