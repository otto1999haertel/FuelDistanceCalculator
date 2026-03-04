using FuelDistanceCalculator.Model;
using FuelDistanceCalculator.Model.Dto;

namespace FuelDistanceCalculator.Interfaces
{
    public interface ISearchService
    {
        Task<SearchResult> SearchAsync(SearchParameters parameters);
        Task<List<SearchResult>> CompareAsync(List<SearchParameters> locations);
        List<GasStation> SortStations(List<GasStation> stations, string sortMode);
    }
}