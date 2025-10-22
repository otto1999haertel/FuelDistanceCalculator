namespace FuelDistanceCalculator.Model;
public class ResponseModelMarketFuelPriceService
{
    public string status { get; set; } = string.Empty;
    public bool ok { get; set; }
    public string message { get; set; } = string.Empty;
}
