using NUnit.Framework.Internal;
using FuelDistanceCalculator.Model;

namespace FuelDistanceCalculatorTest.ServiceTests
{
    public class TankCostServiceTest : ServiceTestBase
    {
        [Test]
        public async Task SettingFuelAmountToZeroLeadsToOrderingTheListTest()
        {
            // Arrange
            decimal fuelAmount = 0m; // Beispiel: 50 Liter
            decimal pricePerKilometer = 0.25m; // Beispiel: 0,20 Euro pro Kilometer

            //Act
            List<GasStation> result = TankCostService.GetCheapestStations(_fakeGasStationList, fuelAmount, pricePerKilometer, "diesel");

            TestContext.WriteLine("Test: Anzahl der zurückgegebenen Tankstellen: " + result.Count);
            Assert.That(CheckOrderAscendingFuelAmountZero(result), Is.True);
            Assert.That(result.Count>0);
        }

        [Test]
        public async Task SettingFuelAmountToNonZeroLeadsToOrderingByTotalCostTest()
        {
            // Arrange
            decimal fuelAmount = 50m; // Beispiel: 50 Liter
            decimal pricePerKilometer = 0.25m; // Beispiel: 0,20 Euro pro Kilometer

            //Act
            List<GasStation> result = TankCostService.GetCheapestStations(_fakeGasStationList, fuelAmount, pricePerKilometer, "diesel");

            TestContext.WriteLine("Test: Anzahl der zurückgegebenen Tankstellen: " + result.Count);
            Assert.That(result.Count >0);
            Assert.That(CheckOrderByTotalCost(result), Is.True);
        }

        [Test]
        [TestCaseSource(nameof(GetTestCases))]
        public async Task EmptyGasStationListReturnsEmptyListTest(List<GasStation> emptyStationList)
        {
            // Arrange
            decimal fuelAmount = 50m; // Beispiel: 50 Liter
            decimal pricePerKilometer = 0.25m; // Beispiel: 0,20 Euro pro Kilometer

            //Act
            List<GasStation> result = TankCostService.GetCheapestStations(emptyStationList, fuelAmount, pricePerKilometer, "diesel");

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

        private static IEnumerable<TestCaseData> GetTestCases()
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
    
    }
}