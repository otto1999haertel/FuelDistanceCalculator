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

        [Test] 
        [TestCase("49.5937712", "11.0320692")] //Erlangen 11.0320692, 49.5937712
        public async Task CalculateDistance_Test(string lat, string lon)
        {
            //Arrange

            List<GasStation> testGasstaion = new List<GasStation>()
            {
                this._fakeGasStationList.FirstOrDefault()
            };
            //Test Coord: lat: 49.5937712 long: 11.0320692
            double? distanceFromGastStaionAPI = testGasstaion[0].Dist;
            //Act
            List<GasStation> results = await _geoLocationService.CalculateDistance(lat, lon, testGasstaion);
            
            //Assert
            Assert.That(results[0].Dist.Equals(distanceFromGastStaionAPI), Is.False);
        }
    }
}