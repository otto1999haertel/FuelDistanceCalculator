using FuelDistanceCalculator.Services.Discount;

namespace FuelDistanceCalculatorTest.ServiceTests;

public class DiscountApplierFactoryTest
{
    private static IEnumerable<TestCaseData> DiscountApplierTestCases()
    {
        yield return new TestCaseData("10", 50m, typeof(TotalAbsolutDiscount));
        yield return new TestCaseData("5%", 30m, typeof(UnitPercentageDiscount));
        yield return new TestCaseData("5%", 0m, typeof(UnitPercentageDiscount));
        yield return new TestCaseData("2.5ct", 20m, typeof(UnitAbsolutDiscount));
        yield return new TestCaseData("2.5ct", 0m, typeof(UnitAbsolutDiscount));
        yield return new TestCaseData("", 40m, null);
        yield return new TestCaseData("10", 0m, null);
    }

    [TestCaseSource(nameof(DiscountApplierTestCases))]
    public void CreateDiscountApplier_ValidInputs_ReturnsExpectedType(
        string discountAmount,
        decimal fuelAmount,
        Type expectedType)
    {
        // Act
        var result = DiscountApplierFactory.CreateDiscountApplier(
            "BrandA", discountAmount, fuelAmount);

        // Assert
        if (expectedType == null)
        {
            Assert.That(result, Is.Null);
        }
        else
        {
            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.InstanceOf(expectedType));
        }
    }
}