using FuelDistanceCalculator.Model;
using FuelDistanceCalculator.Model.Dto;
using FuelDistanceCalculator.Constants;
using FuelDistanceCalculator.Services;
using Moq;

namespace FuelDistanceCalculatorTest.ServiceTests
{
    [TestFixture]
    public class SearchServiceTests
    {
        private Mock<IGeoLocationService> _geoMock;
        private Mock<IMarketFuelPriceService> _fuelMock;
        private SearchService _service;
        
        [SetUp]
        public void Setup()
        {
            _geoMock = new Mock<IGeoLocationService>();
            _fuelMock = new Mock<IMarketFuelPriceService>();
            var logger = Mock.Of<ILogger<SearchService>>();
            _service = new SearchService(_geoMock.Object, _fuelMock.Object, logger);
        }

        [Test]
        public async Task SearchAsync_GeoFails_ReturnsEmptyResult()
        {
            _geoMock.Setup(g => g.GetCoordinatesAsync(It.IsAny<string>())).ReturnsAsync((CoordinatesDTO)null);
            var result = await _service.SearchAsync(new SearchParameters { Place = "X" });
            Assert.That(result.Stations, Is.Empty);
        }

        [Test]
        public async Task SearchAsync_FuelFails_ReturnsEmptyStations()
        {
            _geoMock.Setup(g => g.GetCoordinatesAsync(It.IsAny<string>())).ReturnsAsync(new CoordinatesDTO { Latitude = 1, Longitude = 2 });
            _fuelMock.Setup(f => f.GetGasStationsAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>()))
                     .ReturnsAsync(new GasStationResult { IsSuccess = false, ErrorMessage = "error" });

            var result = await _service.SearchAsync(new SearchParameters { Place = "X" });
            Assert.That(result.Stations, Is.Empty);
        }

        [Test]
        public async Task SearchAsync_HappyPath_ReturnsOrderedAndSavings()
        {
            _geoMock.Setup(g => g.GetCoordinatesAsync(It.IsAny<string>())).ReturnsAsync(new CoordinatesDTO { Latitude = 0, Longitude = 0 });

            var stations = new List<GasStation>
            {
                new GasStation { Name = "A", IsOpen=true, Dist = 1, Fuels = new List<Fuel>{ new Fuel{ Name="Diesel", Price=1.0} } },
                new GasStation { Name = "B", IsOpen=true, Dist = 2, Fuels = new List<Fuel>{ new Fuel{ Name="Diesel", Price=0.5} } }
            };
            _fuelMock.Setup(f => f.GetGasStationsAsync(0,0,It.IsAny<double>(),"Diesel",It.IsAny<string>(),It.IsAny<decimal>() ))
                     .ReturnsAsync(new GasStationResult { IsSuccess = true, Stations = stations });
            _geoMock.Setup(g => g.CalculateDistanceFromAPI(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<GasStation>>() ))
                    .ReturnsAsync((string a,string b,List<GasStation> list)=> list);
            var param = new SearchParameters { Place = "X", FuelAmount = 10, PricePerKm = 1, FuelType = FuelType.Diesel };
            var result = await _service.SearchAsync(param);
            Assert.That(result.Stations.First().Name, Is.EqualTo("B"));
            Assert.That(result.SavingsToCheapestStation, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public async Task CompareAsync_ComposesMultipleResults()
        {
            var p1 = new SearchParameters { Place = "X" };
            var p2 = new SearchParameters { Place = "Y" };
            _service = new SearchService(_geoMock.Object, _fuelMock.Object, Mock.Of<ILogger<SearchService>>());
            _geoMock.Setup(g => g.GetCoordinatesAsync(It.IsAny<string>())).ReturnsAsync(new CoordinatesDTO { Latitude = 0, Longitude=0 });
            _fuelMock.Setup(f => f.GetGasStationsAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<double>(),It.IsAny<string>(),It.IsAny<string>(),It.IsAny<decimal>()))
                     .ReturnsAsync(new GasStationResult { IsSuccess = true, Stations = new List<GasStation>() });

            var results = await _service.CompareAsync(new List<SearchParameters>{p1,p2});
            Assert.That(results.Count, Is.EqualTo(2));
        }
    }
}
