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
                Dist = 10.0, // 10 km,
                Brand = "TestBrand",
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

        [TestCase("Aral", "Aral", 5.0, 1.425)]  // ✓ Korrekt: 1.50 - 5% = 1.425
        [TestCase("Shell", "Aral", 5.0, 1.50)]  // ✓ Korrekt: Kein Rabatt
        [TestCase("aral", "Aral", 10.0, 1.35)]  // ✓ Korrekt: 1.50 - 10% = 1.35
        [TestCase("Aral", "Aral", 0.0, 1.50)]   // ✓ Korrekt: 0% Rabatt
        public void CalculateDiscountForBrandTest(string stationBrand, string inputBrand, double discountPercent, double expectedPrice)
        {
            // Arrange
            var gasStation = new GasStation
            {
                Id = "123",
                Name = "Test Station",
                Brand = stationBrand,
                Dist = 10.0,
                Fuels = new List<Fuel>
                {
                    new Fuel { Name = "Diesel", Price = 1.50 }
                }
            };

            // Act
            gasStation.SetPrice("Diesel", inputBrand, (decimal)discountPercent);

            // Assert
            Assert.That(gasStation.FuelTypePrice, Is.EqualTo((decimal)expectedPrice).Within(0.001m));
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
        [TestCase("Diesel", "2025-10-15T09:40:49+02")]
        [TestCase("Super E5", "2025-10-15T09:30:49+02")]
        [TestCase("Super E10", "2025-10-15T09:35:49+02")]
        public void SetUpDateTimeTest(string fuelType, string expectedDateTime)
        {
            //Act
            GasStation testObject = this._fakeGasStationList.FirstOrDefault();
            testObject.SetUpdateTime(fuelType);
            
            //Assrt
            Assert.That(testObject.LastUpdate.Equals(expectedDateTime));
        }

        [Test]
        [TestCase("Diesel", -0.05)]
        [TestCase("Super E5", 0.10)]
        [TestCase("Super E10", 0.05)]
        public void SetUpdateAmountTest(string fuelType, decimal expectedAmount)
        {
            //Act
            GasStation testObject = this._fakeGasStationList.FirstOrDefault();
            testObject.SetUpdateAmount(fuelType);

            //Assrt
            Assert.That(testObject.UpdateAmount.Equals(expectedAmount));
        }
    }
}