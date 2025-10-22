using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.IdentityModel.Tokens;
using NUnit.Framework.Internal;

namespace FuelDistanceCalculatorTest
{
    public class TankCostServiceTest : BaseTestMarketfuelpriceService
    {
        [Test]
        public async Task SettingFuelAmountToZeroLeadsToOrderingTheListTest()
        {
            // Arrange
            decimal fuelAmount = 0m; // Beispiel: 50 Liter
            decimal pricePerKilometer = 0.25m; // Beispiel: 0,20 Euro pro Kilometer

            //Act
            List<GasStation> result = TankCostService.GetCheapestStations(_fakeGasStationList, fuelAmount, pricePerKilometer);

            TestContext.WriteLine("Test: Anzahl der zurückgegebenen Tankstellen: " + result.Count);
            Assert.That(CheckOrderAscendingFuelAmountZero(result), Is.True);
            Assert.That(result.Count <= 10);
        }

        [Test]
        public async Task SettingFuelAmountToNonZeroLeadsToOrderingByTotalCostTest()
        {
            // Arrange
            decimal fuelAmount = 50m; // Beispiel: 50 Liter
            decimal pricePerKilometer = 0.25m; // Beispiel: 0,20 Euro pro Kilometer

            //Act
            List<GasStation> result = TankCostService.GetCheapestStations(_fakeGasStationList, fuelAmount, pricePerKilometer);

            TestContext.WriteLine("Test: Anzahl der zurückgegebenen Tankstellen: " + result.Count);
            Assert.That(result.Count <= 10);
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
            List<GasStation> result = TankCostService.GetCheapestStations(emptyStationList, fuelAmount, pricePerKilometer);

            //Assert
            Assert.That(result.Count == 0);
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
    
    }
}