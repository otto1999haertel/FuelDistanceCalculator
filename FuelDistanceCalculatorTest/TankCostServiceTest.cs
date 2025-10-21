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
    
    }
}