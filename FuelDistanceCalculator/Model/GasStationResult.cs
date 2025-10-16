
public class GasStationResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public List<GasStation>? Stations { get; set; }

    public static implicit operator GasStationResult(List<GasStation> v)
    {
        throw new NotImplementedException();
    }
}
