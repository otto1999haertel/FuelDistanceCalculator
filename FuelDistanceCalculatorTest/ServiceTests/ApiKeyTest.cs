using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace FuelDistanceCalculatorTest.ServiceTests
{
    [TestFixture]
    public class ApiKeyTest
    {
        [Test]
        public void ApiKeysMustBePresentInConfiguration()
        {
            // Build IConfiguration wie in deiner App
            var config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")}.json", optional: true)
                .AddEnvironmentVariables() // ENV vars haben Priorität
                .Build();

            // Act: Lese die Keys aus der Config
            string tankKey = config["ApiSettings:TankApiKey"];
            string openrKey = config["ApiSettings:OpenRouteServiceApiKey"];

            // Assert: Keys müssen gesetzt sein
            Assert.That(!string.IsNullOrWhiteSpace(tankKey), 
                $"TankApiKey should not be empty. Current value: '{tankKey}'");
            Assert.That(!string.IsNullOrWhiteSpace(openrKey), 
                $"OpenRouteServiceApiKey should not be empty. Current value: '{openrKey}'");
        }
    }
}