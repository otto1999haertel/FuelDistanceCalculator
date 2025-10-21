using System.Text.Json;
using Microsoft.IdentityModel.Tokens;
namespace FuelDistanceCalculatorTest
{
    public class GasStationSerializationTest
    {
        private string responseContent;
        [SetUp]
        public async Task Setup()
        {
            var filePath = Path.Combine(Directory.GetCurrentDirectory(), "Data", "Fuel_price_API_response.json");
            Assert.That(File.Exists(filePath), Is.True, $"Test JSON file not found at {filePath}");
            responseContent = await File.ReadAllTextAsync(filePath);
        }

        [Test]
        public void GasStationSerializationFromFileTest()
        {
            GasStationResponse gasStationResponse = null;
            Assert.DoesNotThrow(() =>
            {
                gasStationResponse = JsonSerializer.Deserialize<GasStationResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }, "Deserialization threw an exception.");
            Assert.That(gasStationResponse, Is.Not.Null, "Deserialized object is null.");
            Assert.That(gasStationResponse.Stations.IsNullOrEmpty(), Is.False, "Stations list is null");
            GasStation firstStation = gasStationResponse.Stations[0];
            Assert.That(firstStation.Name.IsNullOrEmpty(), Is.False, "First station name is null or empty.");
            Assert.That(firstStation.Fuels.IsNullOrEmpty(), Is.False, "First station fuels list is null or empty.");
            Assert.That(firstStation.Fuels.Count().Equals(3), Is.True, "First station fuels count does not match expected value.");
            Assert.That(firstStation.Fuels[0].Name.IsNullOrEmpty(), Is.False, "First fuel name is null or empty.");
            Assert.That(firstStation.Fuels[0].Price, Is.GreaterThan(0), "First fuel price is null.");
            Assert.That(firstStation.Fuels[0].LastChange.Timestamp.IsNullOrEmpty(), Is.False, "First fuel last changed is null or empty.");  
            Assert.That(firstStation.Brand.IsNullOrEmpty(), Is.False, "First station brand is null or empty.");
            Assert.That(firstStation.Dist.Value, Is.GreaterThan(0), "First station distance is null.");
            Assert.That(firstStation.ClosesAt.IsNullOrEmpty(), Is.False, "First station closing time is null.");
            Assert.That(firstStation.Coords, Is.Not.Null, "First station coordinates is null.");
            Assert.That(firstStation.Coords.Lat, Is.GreaterThan(0), "First station latitude is zero.");
            Assert.That(firstStation.Coords.Lng, Is.GreaterThan(0), "First station latitude is zero.");
        }
    }
}