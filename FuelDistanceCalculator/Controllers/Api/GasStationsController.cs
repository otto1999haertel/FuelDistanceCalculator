using FuelDistanceCalculator.Model.Dto;
using FuelDistanceCalculator.Services;
using FuelDistanceCalculator.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FuelDistanceCalculator.Controllers.Api
{
    [ApiController]
    [Route("api/[controller]")]
    public class GasStationsController : ControllerBase
    {
        private readonly ISearchService _searchService;
        private readonly ILogger<GasStationsController> _logger;

public GasStationsController(ISearchService searchService, ILogger<GasStationsController> logger)
        {
            _searchService = searchService;
            _logger = logger;
        }

        /// <summary>
        /// Search for gas stations around a single location. All parameters must be supplied.
        /// </summary>
        [HttpPost("search")]
        public async Task<IActionResult> Search([FromBody] SearchParameters parameters)
        {
            if (parameters == null)
                return BadRequest("Search parameters must be provided.");

            var result = await _searchService.SearchAsync(parameters);
            return Ok(result);
        }

        /// <summary>
        /// Compare multiple locations (up to four) in one request.
        /// </summary>
        [HttpPost("compare")]
        public async Task<IActionResult> Compare([FromBody] CompareRequest request)
        {
            if (request?.Locations == null || !request.Locations.Any())
                return BadRequest("At least one location must be provided.");

            // limit to maximum 4 locations for performance
            var locations = request.Locations.Take(4).ToList();
            var results = await _searchService.CompareAsync(locations);
            return Ok(results);
        }

        /// <summary>
        /// Sort an existing list of stations. Useful for clients that already have results and just want to reorder them.
        /// </summary>
        [HttpPost("sort")]
        public IActionResult Sort([FromBody] SortRequest request)
        {
            if (request == null || request.Stations == null)
                return BadRequest("Stations must be provided.");

            var sorted = _searchService.SortStations(request.Stations, request.SortMode ?? string.Empty);
            return Ok(new SortResponse { Stations = sorted });
        }
    }
}
