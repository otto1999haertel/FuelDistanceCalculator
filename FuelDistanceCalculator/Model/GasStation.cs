using Newtonsoft.Json;

namespace FuelDistanceCalculator.Model;

public class GasStation
{
    [JsonProperty("country")]
    public string? Country { get; set; }

    [JsonProperty("id")]
    public string? Id { get; set; }

    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("brand")]
    public string? Brand { get; set; }

    [JsonProperty("street")]
    public string? Street { get; set; }

    [JsonProperty("postalCode")]
    public string? PostalCode { get; set; }

    [JsonProperty("place")]
    public string? Place { get; set; }

    [JsonProperty("coords")]
    public Coordinates? Coords { get; set; }

    [JsonProperty("isOpen")]
    public bool IsOpen { get; set; }

    [JsonProperty("closesAt")]
    public string? ClosesAt { get; set; }

    [JsonProperty("dist")]
    public double? Dist { get; set; }

    [JsonProperty("fuels")]
    public List<Fuel>? Fuels { get; set; }

    [JsonProperty("volatility")]
    public int Volatility { get; set; }

    [JsonProperty("totalCalculatedCoast")]
    public decimal TotalCombinedCosts
    {
        get => _totalCost;
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException("TotalCalculatedCoast cannot be negative.");
            }
            _totalCost = value;
        }
    }
    private decimal _totalCost;

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

    public bool IsRoutingDistanceCalculated { get; set; } = false;

    [JsonProperty("discountApplied")]

    public bool DiscountApplied { get; private set; } = false;

    [JsonProperty("totalFuelCost")]
    public decimal TotalFuelCost {get; private set;}

    [JsonProperty("totalTravelCost")]
    public decimal TotalTravelCost {get; private set;}


    public decimal CalculateTotalCostDoubleWayWithDiscountGreaterOne(decimal fuelAmount, decimal pricePerKm, string brand = "", decimal discountAmount = 0)
    {
        if (fuelAmount < 0 || pricePerKm < 0 || Dist == null || Dist < 0)
        {
            throw new ArgumentException("Ungültige Eingabewerte: FuelAmount muss positiv sein, PricePerKm nicht negativ, Dist vorhanden und nicht negativ.");
        }

        decimal dist = (decimal)Dist.Value;
        decimal fuelCost = (_fuelPrice ?? 0) * fuelAmount;
        TotalFuelCost = fuelCost>0 ? Math.Round(fuelCost,3,MidpointRounding.AwayFromZero) : 0;
        decimal travelCost = pricePerKm * dist * 2m;
        TotalTravelCost = travelCost>0 ? Math.Round(travelCost,3,MidpointRounding.AwayFromZero) : 0;
        if (discountAmount > 1 && Brand.Equals(brand, StringComparison.OrdinalIgnoreCase))
        {
            _totalCost = fuelCost + travelCost - discountAmount;
            if (_totalCost < 0)
            {
                _totalCost = 0;
            }
            DiscountApplied = true;
        }
        else
        {
            TotalCombinedCosts = fuelCost + travelCost;
            DiscountApplied &= DiscountApplied;
        }
        TotalCombinedCosts = Math.Round(TotalCombinedCosts, 3, MidpointRounding.AwayFromZero);
        Console.WriteLine($"Total Cost for station {Name} (ID: {Id}): {_totalCost:F2} € " +
                          $"(Fuel Cost: {fuelCost:F2}, Travel Cost: {travelCost:F2}, " +
                          $"Fuel Price: {_fuelPrice:F3}, Fuel Amount: {fuelAmount:F1}, Price per Km: {pricePerKm:F2}, Distance: {dist:F2})");

        return TotalCombinedCosts;
    }

    public void SetPrice(string fuelType)
    {
        _fuelPrice = Fuels?.Where(x => x.Name.Equals(fuelType, StringComparison.OrdinalIgnoreCase))
                    .Select(x => (decimal)x.Price)
                    .FirstOrDefault() ?? 0;
    }

    public void SetPriceWithPercentageDiscount(string fuelType, string brand, decimal discountPercent)
    {
        SetPrice(fuelType);
        if (Brand.Equals(brand, StringComparison.OrdinalIgnoreCase) && FuelTypePrice.HasValue && discountPercent > 0)
        {
            if (discountPercent > 0 && discountPercent < 1)
            {
                Console.WriteLine($"Applying discount of {discountPercent}% for brand {brand} on station {Name}");
                decimal discountAmount = FuelTypePrice.Value * discountPercent;
                _fuelPrice -= discountAmount;
                _fuelPrice = Math.Round(FuelTypePrice.Value, 3);
                DiscountApplied = true;
            }
            else
            {
                DiscountApplied = false;
            }
        }
        else
        {
            DiscountApplied = false;
        }


        Console.WriteLine($"SetPrice called for FuelType: {fuelType}, Brand: {brand}, Discount: {discountPercent}. Resulting FuelTypePrice: {FuelTypePrice}");

    }

    public void SetUpdateTime(string fuelType)
    {
        LastUpdate = Fuels?.Where(x => x.Name.Equals(fuelType, StringComparison.OrdinalIgnoreCase))
                     .Select(x => x.LastChange?.Timestamp)
                     .FirstOrDefault();
    }

    public void SetUpdateAmount(string fuelType)
    {
        UpdateAmount = Fuels?.Where(x => x.Name.Equals(fuelType, StringComparison.OrdinalIgnoreCase))
                     .Select(x => (decimal?)x.LastChange?.Amount)
                     .FirstOrDefault();
    }

    public string DisplayUpdateAmount()
    {
        if (UpdateAmount.HasValue)
        {
            string signValue = UpdateAmount.Value > 0 ? $"+{UpdateAmount.Value:F2} €" : $"{UpdateAmount.Value:F2} €";
            return signValue.Replace(".", ",");
        }
        else
        {
            return "N/A";
        }
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
               $"- TotalCalculatedCoast: {TotalCombinedCosts}";
    }
}