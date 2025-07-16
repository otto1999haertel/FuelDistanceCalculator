using System.Collections.Generic;
using System.Runtime.Intrinsics;
public class FuelPriceService
{
    private double fuelAmount;
    private double pricePerkilometer;

    public FuelPriceService(double FuelAmount, double pricePerkilometer){
        this.pricePerkilometer=pricePerkilometer;
        this.fuelAmount = FuelAmount;
    }

    public double? CalculateAverageCost(List<GasStation> gasStations)
    {
        if (gasStations == null || gasStations.Count == 0){
            return 0.0;
        }

        return gasStations.OrderBy(gs => gs.Price).ToList().Take(10).Average(gs => gs.Price);
    }

    public double CalculateEntireCost(double pricePerLiter, double distance){
        return (fuelAmount * pricePerLiter) + (distance *pricePerkilometer);
    }

    public string [] AnalyseBreakEven(double pricePerLiter1, double distance1, string nameGasStation1, double pricePerLiter2, double distance2, string nameGasStation2 ){
        // Erstelle Vektoren für die Tankstellen
        var vector1 = new Vector2((float)pricePerLiter1, (float)(pricePerkilometer * distance1));
        var vector2 = new Vector2((float)pricePerLiter2, (float)(pricePerkilometer * distance2));

        // Berechne den Break-Even-Punkt (Tankmenge)
        double breakEvenFuelAmount = (vector2.Y - vector1.Y) / (vector1.X - vector2.X);

        // Überprüfe, ob es einen Break-Even-Punkt gibt
        if (vector1.X == vector2.X)
        {
            // Wenn die Vektoren in der x-Komponente gleich sind, gibt es keinen Break-Even-Punkt
            return new string [0];
        }
        Console.WriteLine($"Break Even Amount {breakEvenFuelAmount}");
        
        double FuelAmount1AfterBreakEven = CalculateEntireCostAfterBreakEven(pricePerLiter1,distance1,breakEvenFuelAmount+0.1);
        Console.WriteLine("Cost 1 after break Even" + FuelAmount1AfterBreakEven);
        double FuelAmount2AfterBreakEven = CalculateEntireCostAfterBreakEven(pricePerLiter2,distance2,breakEvenFuelAmount+0.1);
        Console.WriteLine("Cost 2 after break Even" + FuelAmount2AfterBreakEven);
        string [] breakEven = new string[2];
        if (FuelAmount1AfterBreakEven < FuelAmount2AfterBreakEven)
        {
            breakEven[0]= nameGasStation1;
            breakEven[1]= breakEvenFuelAmount.ToString();
        }
        else
        {
             breakEven[0]= nameGasStation2;
            breakEven[1]= breakEvenFuelAmount.ToString();
        }
        
        return breakEven;
    }

    private double CalculateEntireCostAfterBreakEven(double pricePerLiter, double distance, double fuelAmount){
        return (fuelAmount * pricePerLiter)+ (distance * pricePerkilometer);
    }

    public struct Vector2
{
    public double X { get; set; }
    public double Y { get; set; }

    public Vector2(double x, double y)
    {
        X = x;
        Y = y;
    }

    // Optional: Überladung des Operators für die Subtraktion von Vektoren
    public static Vector2 operator -(Vector2 v1, Vector2 v2)
    {
        return new Vector2(v1.X - v2.X, v1.Y - v2.Y);
    }
}
}