using FuelDistanceCalculator.Model;

namespace FuelDistanceCalculatorTest.ModelTests;

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
            Dist = 8.65, // 10 km,
            Brand = "TestBrand",
            Fuels = new List<Fuel>
                    {
                        new Fuel { Name = "Diesel", Price = 1.479 }
                    }
        };
        gasStation.SetPrice("Diesel"); // Setzt _fuelprice auf 1.50

        decimal fuelAmount = 20; // 20 Liter
        decimal pricePerKm = 0.25m; // 0.10 € pro km

        // Act
        decimal result = gasStation.CalculateTotalCostDoubleWayWithDiscountGreaterOne(fuelAmount, pricePerKm);

        // Assert
        decimal expectedFuelCost = fuelAmount * (decimal)gasStation.Fuels[0].Price; // 30.00 €
        decimal expectedTravelCost = pricePerKm * (decimal)gasStation.Dist * 2; // Faktor 2: hin und zurück
        decimal expectedTotal = Math.Round(expectedFuelCost + expectedTravelCost, 3, MidpointRounding.AwayFromZero); // 32.00 €
        Assert.That(expectedTotal.Equals(result));
    }

    [Test]
    [TestCase(30)]
    [TestCase(0)]
    public void CalculateTotalCostsDifferentiatesSingleCostsTest(decimal fuelAmount)
    {
        var gasStation = new GasStation
        {
            Id = "123",
            Name = "Test Station",
            Dist = 8.65, // 10 km,
            Brand = "TestBrand",
            Fuels = new List<Fuel>
                    {
                        new Fuel { Name = "Diesel", Price = 1.479 }
                    }
        };
        gasStation.SetPrice("Diesel"); // Setzt _fuelprice auf 1.50

        decimal pricePerKm = 0.25m; // 0.10 € pro km

        // Act
       gasStation.CalculateTotalCostDoubleWayWithDiscountGreaterOne(fuelAmount, pricePerKm);
       var expectedTravelCost = Math.Round(pricePerKm * (decimal)gasStation.Dist * 2,3,MidpointRounding.AwayFromZero); // Faktor 2: hin und zurück
        // Assert
        Assert.AreEqual(gasStation.TotalTravelCost, expectedTravelCost);
        if (fuelAmount > 0)
        {
            var expectedFuelCost = fuelAmount * 1.479m;
            Assert.AreEqual(gasStation.TotalFuelCost, expectedFuelCost);
        }
        else
        {
            Assert.AreEqual(gasStation.TotalFuelCost, 0);
        }

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

    [Test]
    [TestCase(1, "+1,00 €")]
    [TestCase(-1, "-1,00 €")]
    [TestCase(1.2, "+1,20 €")]
    [TestCase(-1.2, "-1,20 €")]
    [TestCase(1.23, "+1,23 €")]
    [TestCase(-1.23, "-1,23 €")]
    public void UpdateAmountDisplayedWithPlusTest(decimal updateAmount, string expectedDisplay)
    {
        //Act
        GasStation testObject = new GasStation();
        testObject.UpdateAmount = updateAmount;

        //Assert
        string displayAmount = testObject.DisplayUpdateAmount();

        Assert.That(displayAmount.Equals(expectedDisplay));
    }
}