namespace FuelDistanceCalculator.Model;

public class OilPriceChange
{
    private static readonly System.Globalization.CultureInfo _de = 
    new System.Globalization.CultureInfo("de-DE");
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

    public string DisplayDayChangeWithSign()
    {
        return DisplayDayChangeWithSign(Day);
    }

    public string DisplayWeekChangeWithSign()
    {
        return DisplayDayChangeWithSign(Week);
    }

    public string DisplayMonthChangeWithSign()
    {
        return DisplayDayChangeWithSign(Month);
    }

    public string DisplayDayChangeWithSign(double change)
    {
        string sign = change > 0 ? "+" : "";
        return $"{sign}{change.ToString("F2", _de)} %";
    }

    public string DisplayCurrentPriceWithCurrency()
    {
        string displayValue = CurrentPrice.ToString().Replace('.', ',');
        if(displayValue.Split(',').Length == 1)
        {
            displayValue += ",00";
        }
        else if(displayValue.Split(',')[1].Length == 1)
        {
            displayValue += "0";
        }
        return $"{displayValue:F2} $";
    }
}