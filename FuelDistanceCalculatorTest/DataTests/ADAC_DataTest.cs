using FuelDistanceCalculator.Services;

namespace FuelDistanceCalculatorTest.DataTest;


[TestFixture]
public class ADAC_DataTest
{
    private string filePath;
    [SetUp]
    public async Task Setup()
    {
        filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "ADAC_car_data.json");
        Assert.That(File.Exists(filePath), Is.True, $"Test JSON file not found at {filePath}");
    }

    [Test]
    [TestCase(1220, 0.98)]
    public async Task JSON_Holds_Correct_Number_Of_Cars_Test(int expectedCarCount, double tolerance)
    {
        Dictionary<string, decimal> carsAndRespectivePricePerkm =  await CarDataParser.ParseCarData(filePath);
        Assert.That(carsAndRespectivePricePerkm, Is.Not.Null, "Deserialized object is null.");
        int minExpected = (int)(expectedCarCount * tolerance);
        int actual = carsAndRespectivePricePerkm.Count;
        Assert.That(actual, Is.InRange(minExpected, expectedCarCount),
            $"Expected between {minExpected} and {expectedCarCount} cars, but found {actual}. " +
            $"({(double)actual / expectedCarCount * 100:F1}% of expected)");
    }

    [Test]
    public async Task MetaDataCheck_Test()
    {
        Dictionary<string, string> carsAndRespectivePricePerkm =  await CarDataParser.GetMetaData(filePath);
        Assert.That(carsAndRespectivePricePerkm, Is.Not.Null, "Deserialized object is null.");
        Assert.That(!string.IsNullOrEmpty(carsAndRespectivePricePerkm["generated_at"]));
    }
}