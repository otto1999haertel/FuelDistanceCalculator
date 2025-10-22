using System.Text.Json.Serialization;
namespace FuelDistanceCalculator.Model;

public class GasStationResponse
{
    [JsonPropertyName("stations")]
    public List<GasStation> Stations { get; set; }
}