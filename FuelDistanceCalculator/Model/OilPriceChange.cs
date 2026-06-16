namespace FuelDistanceCalculator.Model;

public class OilPriceChange
{
    public double Day { get; private set; }
    public double Week { get; private set; }
    public double Month { get; private set; }

    public OilPriceChange(double day, double week, double month)
    {
        Day = day;
        Week = week;
        Month = month;
    }
}