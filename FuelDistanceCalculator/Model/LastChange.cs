using System.Text.Json.Serialization;

public class LastChange
{
    [JsonPropertyName("amount")]
    public double Amount { get; set; }

    [JsonPropertyName("timestamp")]
    public string Timestamp { get; set; }  // Als string, kann bei Bedarf zu DateTime geparst werden
}