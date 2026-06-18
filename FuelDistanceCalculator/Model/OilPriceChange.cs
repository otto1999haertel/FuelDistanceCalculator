namespace FuelDistanceCalculator.Model;

public class OilPriceChange
{
    public double Day { get; private set; }
    public double Week { get; private set; }
    public double Month { get; private set; }

    public double CurrentPrice { get; private set; }

    public DateTimeOffset? LastUpdated { get; private set; }

    public OilPriceChange(double day, double week, double month, double currentPrice, DateTimeOffset? lastUpdated = null)
    {
        Day = day;
        Week = week;
        Month = month;
        CurrentPrice = currentPrice;
        LastUpdated = lastUpdated;
    }
}