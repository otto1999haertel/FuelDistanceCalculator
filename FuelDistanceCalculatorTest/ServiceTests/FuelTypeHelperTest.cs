namespace FuelDistanceCalculatorTest.ServiceTests;

public class FuelTypeHelperTest
{
    [Test]
    [TestCase(FuelType.Diesel, "Diesel")]
    [TestCase(FuelType.SuperE10, "Super E10")]
    [TestCase(FuelType.SuperE5, "Super E5")]
    public void EnsureCorrectReturnFromDictionary_Test(FuelType fuelType, string expectedName)
    {
        // Act
        FuelTypeHelper.FuelTypeNames.TryGetValue(fuelType, out string actualName);

        // Assert
        Assert.That(actualName, Is.EqualTo(expectedName));
    }
}