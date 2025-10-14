using System.Text.Json.Serialization;

public class Fuel
{
    [JsonPropertyName("category")]
    public string Category { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("price")]
    public double? Price { get; set; }

    [JsonPropertyName("lastChange")]
    public LastChange LastChange { get; set; }
}