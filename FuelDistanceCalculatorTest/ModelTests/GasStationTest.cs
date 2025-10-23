using System.Threading.Tasks;
using FuelDistanceCalculator.Model;

namespace FuelDistanceCalculatorTest.ModelTests
{
    public class GasStationTest : ServiceTestBase
    {
        private GasStation _testStationObject;
        [SetUp]
        public async Task Setup()
        {
            await base.Setup();
            _testStationObject = new GasStation();
        }

        [Test]
        public void CalculateTotalCostDoubleWayTest()
        {
            var gasStation = new GasStation
            {
                Id = "123",
                Name = "Test Station",
                Dist = 10.0, // 10 km
                Fuels = new List<Fuel>
                    {
                        new Fuel { Name = "Diesel", Price = 1.50 }
                    }
            };
            gasStation.SetPrice("Diesel"); // Setzt _fuelprice auf 1.50

            decimal fuelAmount = 20; // 20 Liter
            decimal pricePerKm = 0.25m; // 0.10 € pro km

            // Act
            decimal result = gasStation.CalculateTotalCostDoubleWay(fuelAmount, pricePerKm);

            // Assert
            decimal expectedFuelCost = fuelAmount * (decimal)gasStation.Fuels[0].Price; // 30.00 €
            decimal expectedTravelCost = pricePerKm * (decimal)gasStation.Dist * 2; // Faktor 2: hin und zurück
            decimal expectedTotal = Math.Round(expectedFuelCost + expectedTravelCost, 2, MidpointRounding.AwayFromZero); // 32.00 €
            Assert.That(expectedTotal.Equals(result));
        }

        [Test]
        [TestCase("Diesel", 1.619)]
        [TestCase("Super E5", 1.759)]
        [TestCase("Super E10", 1.699)]
        public void SetPriceSetsCorrectFuelPriceForEntireGasstaionTest(string fueltype, decimal expectedFuelTypePrice)
        {
            //Act
            GasStation testObject = this._fakeGasStationList.FirstOrDefault();

            testObject.SetPrice(fueltype);
            //Assrt
            Assert.That(testObject.FuelTypePrice.Equals(expectedFuelTypePrice));
        }

        [Test]
        [TestCase("Diesel", "2025-10-15T09:27:49+02")]
        [TestCase("Super E5", "2025-10-15T09:27:49+02")]
        [TestCase("Super E10", "2025-10-15T09:27:49+02")]
        public void SetUpDateTimeTest(string fuelType, string expectedDateTime)
        {
            //Act
            GasStation testObject = this._fakeGasStationList.FirstOrDefault();
            testObject.SetUpdateTime(fuelType);
            
            //Assrt
            Assert.That(testObject.LastUpdate.Equals(expectedDateTime));
        }
    }
}