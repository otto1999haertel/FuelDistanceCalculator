using System.Text.Json.Serialization;
namespace FuelDistanceCalculator.Model;

public class Coordinates
{
    [JsonPropertyName("lat")]
    public double Lat { get; set; }

    [JsonPropertyName("lng")]
    public double Lng { get; set; }
}