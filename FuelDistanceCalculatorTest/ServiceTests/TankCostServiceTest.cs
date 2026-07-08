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
        List<GasStation> result = TankCostService.GetCheapestStation(_fakeGasStationList, pricePerKilometer, fuelAmount, "diesel");

        TestContext.Out.WriteLine("Test: Anzahl der zurückgegebenen Tankstellen: " + result.Count);
        Assert.That(CheckOrderAscendingFuelAmountZero(result), Is.True);
        Assert.That(result.Count > 0);
    }

    [Test]
    public async Task SettingFuelAmountToNonZeroLeadsToOrderingByTotalCostTest()
    {
        // Arrange
        decimal fuelAmount = 50m; // Beispiel: 50 Liter
        decimal pricePerKilometer = 0.25m; // Beispiel: 0,20 Euro pro Kilometer
        int originalCount = _fakeGasStationList.Count;
        //Act
        List<GasStation> result = TankCostService.GetCheapestStation(_fakeGasStationList, fuelAmount, pricePerKilometer, "diesel");

        TestContext.Out.WriteLine("Test: Anzahl der zurückgegebenen Tankstellen: " + result.Count);
        Assert.That(result.Count.Equals(originalCount), Is.True);
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
        List<GasStation> result = TankCostService.GetCheapestStation(emptyStationList, fuelAmount, pricePerKilometer, "diesel");

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
    [TestCase(0, "20%", true)]
    [TestCase(0, "20", false)]
    [TestCase(40, "15%", true)]
    [TestCase(40, "20", true)]
    public void GetCheapestGasStationPerceantageDiscountTest(decimal fuelAmount, string discountPercentOrAbsolute, bool validDiscount)
    {
        List<GasStation> CheapestResultStations = new List<GasStation>();
        List<GasStation> testData = CheapestGasStationPerceantageDiscountTestCaseSource();

        string fuelTypeForAPI = "diesel";
        string StationBrand = "Aral";
        decimal pricePerKm = 0.25m;
        decimal expectedPrice = 0m;
        CheapestResultStations = TankCostService.GetCheapestStation(testData, pricePerKm, fuelAmount, fuelTypeForAPI, StationBrand, discountPercentOrAbsolute);

        foreach (var station in CheapestResultStations)
        {
            if (station.Brand.Equals(StationBrand, StringComparison.OrdinalIgnoreCase))
            {
                expectedPrice = testData
                            .Where(s => s.Name.Equals(station.Name, StringComparison.OrdinalIgnoreCase))
                            .SelectMany(s => s.Fuels)
                            .Where(f => f.Name.Equals(fuelTypeForAPI, StringComparison.OrdinalIgnoreCase))
                            .Select(f => (decimal)f.Price)
                            .FirstOrDefault();
                if (fuelAmount == 0 && !DiscountParser.TryParseDiscountPercent(discountPercentOrAbsolute, out decimal discountDecimal))
                {
                    Assert.That(station.FuelTypePrice.Equals(expectedPrice));
                }
                else if (decimal.TryParse(discountPercentOrAbsolute, out discountDecimal))
                {
                    expectedPrice = pricePerKm * (decimal)station.Dist * 2m + expectedPrice * fuelAmount - discountDecimal;
                    Assert.That(station.TotalCalculatedCoast, Is.EqualTo(expectedPrice).Within(0.001m));
                }
                else if (DiscountParser.TryParseDiscountPercent(discountPercentOrAbsolute, out discountDecimal))
                {
                    expectedPrice = Math.Round(expectedPrice * (1 - discountDecimal), 3, MidpointRounding.AwayFromZero);
                    Assert.That(station.FuelTypePrice, Is.EqualTo(expectedPrice).Within(0.001m));
                }
                Assert.That(station.DiscountApplied.Equals(validDiscount));
            }
            else
            {
                Assert.That(station.DiscountApplied, Is.False);
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