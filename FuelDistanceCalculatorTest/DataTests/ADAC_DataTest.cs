using Newtonsoft.Json;

namespace FuelDistanceCalculatorTest.DataTest;


[TestFixture]
public class ADAC_DataTest
{
    private string responseContent;
    [SetUp]
    public async Task Setup()
    {
        var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "ADAC_car_data.json");
        Assert.That(File.Exists(filePath), Is.True, $"Test JSON file not found at {filePath}");
        responseContent = await File.ReadAllTextAsync(filePath);
    }

    [Test]
    [TestCase(1220, 0.98)] // 98% Toleranz = mindestens 1195 Einträge
    public void JSON_Holds_Correct_Number_Of_Cars_Test(int expectedCarCount, double tolerance)
    {
        Dictionary<string, decimal> carsAndRespectivePricePerkm =
            JsonConvert.DeserializeObject<Dictionary<string, decimal>>(responseContent);

        Assert.That(carsAndRespectivePricePerkm, Is.Not.Null, "Deserialized object is null.");

        int minExpected = (int)(expectedCarCount * tolerance);
        int actual = carsAndRespectivePricePerkm.Count;

        Assert.That(actual, Is.InRange(minExpected, expectedCarCount),
            $"Expected between {minExpected} and {expectedCarCount} cars, but found {actual}. " +
            $"({(double)actual / expectedCarCount * 100:F1}% of expected)");
    }
}