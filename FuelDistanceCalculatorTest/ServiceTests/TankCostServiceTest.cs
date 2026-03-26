using NUnit.Framework.Internal;
using FuelDistanceCalculator.Model;
using FuelDistanceCalculator.Services;

namespace FuelDistanceCalculatorTest.ServiceTests;

public class TankCostServiceTest : ServiceTestBase
{
    [Test]
    public async Task SettingFuelAmountToZeroLeadsToOrderingTheListTest()
    {
        // Arrange
        decimal fuelAmount = 0m; // Beispiel: 50 Liter
        decimal pricePerKilometer = 0.25m; // Beispiel: 0,20 Euro pro Kilometer

        //Act
        List<GasStation> result = TankCostService.GetCheapestStationsTotalCostDiscountRelAbs(_fakeGasStationList, fuelAmount, pricePerKilometer, "diesel");

        TestContext.WriteLine("Test: Anzahl der zurückgegebenen Tankstellen: " + result.Count);
        Assert.That(CheckOrderAscendingFuelAmountZero(result), Is.True);
        Assert.That(result.Count > 0);
    }

    [Test]
    public async Task SettingFuelAmountToNonZeroLeadsToOrderingByTotalCostTest()
    {
        // Arrange
        decimal fuelAmount = 50m; // Beispiel: 50 Liter
        decimal pricePerKilometer = 0.25m; // Beispiel: 0,20 Euro pro Kilometer

        //Act
        List<GasStation> result = TankCostService.GetCheapestStationsTotalCostDiscountRelAbs(_fakeGasStationList, fuelAmount, pricePerKilometer, "diesel");

        TestContext.WriteLine("Test: Anzahl der zurückgegebenen Tankstellen: " + result.Count);
        Assert.That(result.Count > 0);
        Assert.That(CheckOrderByTotalCost(result), Is.True);
    }



    [Test]
    [TestCaseSource(nameof(EmptyListCases))]
    public async Task EmptyGasStationListReturnsEmptyListTest(List<GasStation> emptyStationList)
    {
        // Arrange
        decimal fuelAmount = 50m; // Beispiel: 50 Liter
        decimal pricePerKilometer = 0.25m; // Beispiel: 0,20 Euro pro Kilometer

        //Act
        List<GasStation> result = TankCostService.GetCheapestStationsTotalCostDiscountRelAbs(emptyStationList, fuelAmount, pricePerKilometer, "diesel");

        //Assert
        Assert.That(result.Count == 0);
    }

    [Test]
    [TestCaseSource(nameof(CalculateSavngsTestCaseSource))]
    public void CalculateSavingsTeset(List<GasStation> gasStations)
    {
        decimal execpectSavingsNearest = 8.75m;
        decimal expectedSavingsCheapest = 1.75m;
        decimal calculatedSavingsNearest = 0;
        decimal calculatedSavingsChepast = 0;
        TankCostService.CaluclateSavings(gasStations, ref calculatedSavingsNearest, ref calculatedSavingsChepast);
        Assert.That(execpectSavingsNearest.Equals(calculatedSavingsNearest));
        Assert.That(expectedSavingsCheapest.Equals(calculatedSavingsChepast));
    }

    [Test]
    [TestCaseSource(nameof(EmptyListCases))]
    public void CalculateSavingsEmptyTeset(List<GasStation> gasStations)
    {
        decimal calculatedSavingsNearest = 0;
        decimal calculatedSavingsChepast = 0;
        Exception exceptionthrown;
        Assert.DoesNotThrow(() => TankCostService.CaluclateSavings(gasStations, ref calculatedSavingsNearest, ref calculatedSavingsChepast));
    }

    [Test]
    [TestCase("101%", false)]
    [TestCase("15%", true)]
    [TestCase("-10%", false)]
    [TestCase("0%", false)]
    public void GetCheapestGasStationPerceantageDiscountTest(string discountPercentOrAbsolute, bool validDiscount)
    {
        List<GasStation> CheapestResultStations = new List<GasStation>();
        List<GasStation> testData = CheapestGasStationPerceantageDiscountTestCaseSource();

        string fuelTypeForAPI = "diesel";
        string StationBrand = "Aral";// Beispiel: 15% Rabatt auf den Dieselpreis der Aral-Station
        if(DiscountParser.TryParseDiscountPercent(discountPercentOrAbsolute, out decimal discountDecimal) || decimal.TryParse(discountPercentOrAbsolute, out discountDecimal))
        {
            CheapestResultStations = TankCostService.GetCheapestStationDiscountPerCent(testData, fuelTypeForAPI, StationBrand, discountDecimal);
            foreach(var station in CheapestResultStations)
            {
                if(station.Brand.Equals(StationBrand, StringComparison.OrdinalIgnoreCase))
                {
                    decimal expectedPrice = station.Fuels.Where(f => f.Name.Equals(fuelTypeForAPI, StringComparison.OrdinalIgnoreCase))
                                            .Select(f => (decimal)f.Price)
                                            .FirstOrDefault();
                    expectedPrice = Math.Round(expectedPrice * (1 - discountDecimal), 3, MidpointRounding.AwayFromZero);
                    Assert.That(station.FuelTypePrice, Is.EqualTo(expectedPrice).Within(0.001m));
                    Assert.That(station.DiscountApplied.Equals(validDiscount));
                }
                else
                {
                    Assert.That(station.DiscountApplied, Is.False);
                }
            }
        }
        else
        {
            Assert.That(validDiscount.Equals(false));
        }
    }


    [Test]
    [TestCase("-10")]
    [TestCase("10")]
    [TestCase("20")]
    [TestCase("0")]
    public void GetCheapestGasStationAmountDiscountTest(string discountPercentOrAbsolute)
    {
        List<GasStation> CheapestResultStations = new List<GasStation>();
        List<GasStation> testData = CheapestGasStationPerceantageDiscountTestCaseSource();
        decimal FuelAmount = 30;
        decimal PricePerKm = 0.25m;
        string fuelTypeForAPI = "diesel";
        string StationBrand = "Aral";
        if(DiscountParser.TryParseDiscountPercent(discountPercentOrAbsolute, out decimal discountDecimal) || decimal.TryParse(discountPercentOrAbsolute, out discountDecimal))
        {
            CheapestResultStations = TankCostService.GetCheapestStationsTotalCostDiscountRelAbs(testData, FuelAmount, PricePerKm, fuelTypeForAPI, StationBrand, discountDecimal);
            foreach(var station in CheapestResultStations)
            {
                 var expectedPrice = testData.Where(f => f.Name.Equals(station.Name, StringComparison.OrdinalIgnoreCase))
                                                                    .Select(f => f.Fuels.Where(fuel => fuel.Name.Equals(fuelTypeForAPI, StringComparison.OrdinalIgnoreCase)))
                                                                    .FirstOrDefault()
                                                                    .Select(fuel => fuel.Price).FirstOrDefault();
                if(station.Brand.Equals(StationBrand, StringComparison.OrdinalIgnoreCase) && discountDecimal > 0)
                {
                    expectedPrice =   expectedPrice * (double)FuelAmount + (double)station.Dist * (double)PricePerKm * 2 - (double)discountDecimal;         
                    Assert.That(station.TotalCalculatedCoast, Is.EqualTo(expectedPrice).Within(0.001m));
                    Assert.That(station.DiscountApplied, Is.True);
                }
                else
                {
                    expectedPrice =   expectedPrice * (double)FuelAmount + (double)station.Dist * (double)PricePerKm * 2;         
                    Assert.That(station.TotalCalculatedCoast, Is.EqualTo(expectedPrice).Within(0.001m));
                    Assert.That(station.DiscountApplied,Is.False);
                }
            }
        }
    }
    

    private bool CheckOrderAscendingFuelAmountZero(List<GasStation> stations)
    {
        for (int i = 0; i < stations.Count - 1; i++)
        {
            if (stations[i].FuelTypePrice > stations[i + 1].FuelTypePrice)
            {
                return false; // Nicht aufsteigend
            }
        }
        return true; // Aufsteigend
    }

    private bool CheckOrderByTotalCost(List<GasStation> stations)
    {
        for (int i = 0; i < stations.Count - 1; i++)
        {
            if (stations[i].TotalCalculatedCoast > stations[i + 1].TotalCalculatedCoast)
            {
                return false; // Nicht aufsteigend
            }
        }
        return true; // Nach Gesamtkosten sortiert
    }

    private static IEnumerable<TestCaseData> EmptyListCases()
    {
        yield return new TestCaseData(new List<GasStation>()).SetName("EmptyList");
        yield return new TestCaseData(null).SetName("NullList");
    }

    private static IEnumerable<TestCaseData> CalculateSavngsTestCaseSource()
    {
        yield return new TestCaseData(
        new List<GasStation>
                    {
                            new GasStation
                            {
                                Name = "Station A",
                                FuelTypePrice = 1.60m,
                                TotalCalculatedCoast = 85.00m,  // 50L * 1.60 + 10km * 0.25 = 80 + 2.5 = 82.5 → aber wir setzen direkt
                                Dist = 5.0
                            },
                            new GasStation
                            {
                                Name = "Station B",
                                FuelTypePrice = 1.50m,
                                TotalCalculatedCoast = 78.00m,  // günstigster Literpreis, aber weiter weg
                                Dist = 15.0
                            },
                            new GasStation
                            {
                                Name = "Station C",
                                FuelTypePrice = 1.55m,
                                TotalCalculatedCoast = 76.25m,  // insgesamt günstigste Gesamtkosten
                                Dist = 7.0
                            }
                    }
        );
    }

    private static List<GasStation> CheapestGasStationPerceantageDiscountTestCaseSource()
    {
        return 
        new List<GasStation>
                    {
                            new GasStation
                            {
                                Brand = "Aral",
                                Name="Aral Station 1",
                                Fuels = new List<Fuel>
                                {
                                    new Fuel { Name = "Diesel", Price = 2.00 },
                                    new Fuel { Name = "Super E5", Price = 1.80 },
                                    new Fuel { Name = "Super E10", Price = 1.90 }
                                    
                                },
                                IsOpen = true,
                                //TotalCalculatedCoast = 85.00m,  // 50L * 1.60 + 10km * 0.25 = 80 + 2.5 = 82.5 → aber wir setzen direkt
                                Dist = 5.0
                            },
                            new GasStation
                            {
                                Brand = "Esso",
                                Name = "Esso Station",
                                Fuels = new List<Fuel>
                                {
                                    new Fuel { Name = "Diesel", Price = 1.90 },
                                    new Fuel { Name = "Super E5", Price = 1.70 },
                                    new Fuel { Name = "Super E10", Price = 1.80 }
                                    
                                },
                                IsOpen = true,
                                //TotalCalculatedCoast = 78.00m,  // günstigster Literpreis, aber weiter weg
                                Dist = 15.0
                            },
                            new GasStation
                            {
                                Brand = "Aral",
                                Name = "Aral Station 2",
                                Fuels = new List<Fuel>
                                {
                                    new Fuel { Name = "Diesel", Price = 1.90 },
                                    new Fuel { Name = "Super E5", Price = 1.70 },
                                    new Fuel { Name = "Super E10", Price = 1.80 }
                                    
                                },
                                 IsOpen = true,
                                //TotalCalculatedCoast = 76.25m,  // insgesamt günstigste Gesamtkosten
                                Dist = 7.0
                            }
                    };
    }

}