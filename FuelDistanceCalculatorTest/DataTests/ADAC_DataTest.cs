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
    [TestCase(1220)]
    [Ignore("Until Python script and new PDF is ready")]
    public void JSON_Holds_Correct_Number_Of_Cars_Test(int expectedCarCount)
    {
        Dictionary<string,decimal> carsAndRespectivePricePerkm = JsonConvert.DeserializeObject<Dictionary<string, decimal>>(responseContent);
        Assert.That(carsAndRespectivePricePerkm, Is.Not.Null, "Deserialized object is null.");
        Assert.That(carsAndRespectivePricePerkm.Count, Is.EqualTo(expectedCarCount), $"Expected {expectedCarCount} cars, but found {carsAndRespectivePricePerkm.Count}.");
    }
}