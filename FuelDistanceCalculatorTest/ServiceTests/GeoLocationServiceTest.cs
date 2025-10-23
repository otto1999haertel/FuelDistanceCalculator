using FuelDistanceCalculator.Model;
using NUnit.Framework.Internal;

namespace FuelDistanceCalculatorTest.ServiceTests
{
    public class GeoLocationServiceTest : BaseTest
    {
        [Test]
        [TestCase(" 123 Main St, Anytown ", "123 main st, anytown")]
        [TestCase("Östritzer-Über-Äpfel-Straße", "oestritzer-ueber-aepfel-strasse")]
        public async Task NormalizePlace_Test(string inputPlace, string expectedNormalized)
        {
            // Act
            string normalizedPlace = _geoLocationService.NormalizeAddressKey(inputPlace);

            // Assert
            Assert.That(normalizedPlace, Is.EqualTo(expectedNormalized));
        }
    }
}