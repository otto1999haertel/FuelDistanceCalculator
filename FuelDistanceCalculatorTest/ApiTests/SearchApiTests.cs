using NUnit.Framework;
using FuelDistanceCalculator.Controllers.Api;
using FuelDistanceCalculator.Model.Dto;
using FuelDistanceCalculator.Model;
using FuelDistanceCalculator.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace FuelDistanceCalculatorTest.ApiTests
{
    [TestFixture]
    public class SearchApiTests
    {
        private Mock<FuelDistanceCalculator.Interfaces.ISearchService> _searchServiceMock;
        private GasStationsController _controller;

        [SetUp]
        public void Setup()
        {
            _searchServiceMock = new Mock<FuelDistanceCalculator.Interfaces.ISearchService>();

            var logger = Mock.Of<ILogger<GasStationsController>>();
            _controller = new GasStationsController(_searchServiceMock.Object, logger);
        }

        [Test]
        public async Task Search_NullParameters_ReturnsBadRequest()
        {
            var result = await _controller.Search(null);
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task Search_ValidParameters_ReturnsOkWithResult()
        {
            var parameters = new SearchParameters
            {
                Place = "Test",
                Radius = 5,
                FuelAmount = 10,
                PricePerKm = 0.3m,
                FuelType = FuelType.Diesel,
                StationBrand = "",
                DiscountPercent = 0,
                SortMode = "fuelPrice"
            };

            var expected = new SearchResult
            {
                Parameters = parameters,
                Stations = new List<GasStation>()
            };
            _searchServiceMock.Setup(s => s.SearchAsync(It.IsAny<SearchParameters>()))
                              .ReturnsAsync(expected);

            var actionResult = await _controller.Search(parameters);

            Assert.That(actionResult, Is.InstanceOf<OkObjectResult>());
            var ok = (OkObjectResult)actionResult;
            Assert.That(ok.Value, Is.SameAs(expected));
        }

        [Test]
        public async Task Compare_EmptyList_ReturnsBadRequest()
        {
            var request = new CompareRequest { Locations = new List<SearchParameters>() };
            var result = await _controller.Compare(request);
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task Sort_NullStations_ReturnsBadRequest()
        {
            var result = _controller.Sort(null);
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }

        [Test]
        public void Sort_ValidStations_ReturnsSorted()
        {
            var stations = new List<GasStation> { new GasStation { FuelTypePrice = 2 }, new GasStation { FuelTypePrice = 1 } };
            var request = new SortRequest { Stations = stations, SortMode = "fuelPrice" };

            _searchServiceMock.Setup(s => s.SortStations(stations, "fuelPrice")).Returns(stations.OrderBy(s => s.FuelTypePrice).ToList());

            var actionResult = _controller.Sort(request);
            Assert.That(actionResult, Is.InstanceOf<OkObjectResult>());
            var ok = (OkObjectResult)actionResult;
            var response = (SortResponse)ok.Value;
            Assert.That(response.Stations.First().FuelTypePrice, Is.EqualTo(1));
        }
    }
}
