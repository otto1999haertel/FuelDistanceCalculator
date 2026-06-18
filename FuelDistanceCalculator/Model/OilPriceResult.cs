namespace FuelDistanceCalculator.Model;

public class OilPriceResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public OilPriceChange? PriceChange { get; set; }
}