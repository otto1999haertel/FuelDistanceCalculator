namespace FuelDistanceCalculatorTest
{
   public class GasStationSerializationTest
    {
        private  string responseContent;
        [SetUp]
        public async Task Setup()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(),"Data", "Fuel_price_API_response.json");
            Assert.That(File.Exists(filePath), Is.True, $"Test JSON file not found at {filePath}");
            responseContent = await File.ReadAllTextAsync(filePath);
        }

        [Test]
        public void KTest()
        {
            Assert.Pass();
        }
    }
}