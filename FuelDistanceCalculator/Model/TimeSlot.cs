using System.Text.Json.Serialization;

public class TimeSlot
{
    [JsonPropertyName("open")]
    public string Open { get; set; }

    [JsonPropertyName("close")]
    public string Close { get; set; }
}