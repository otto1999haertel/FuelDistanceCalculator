namespace FuelDistanceCalculator.Model;
public class GasStationResult
{
    public bool IsSuccess { get; set; }
    public string? Message { get; set; }
    public string ToastType { get; set; } = "success";
    public List<GasStation>? Stations { get; set; }

     public static GasStationResult Success(List<GasStation> stations) =>
        new() { IsSuccess = true, Stations = stations };

    public static GasStationResult Warning(List<GasStation> stations, string msg) =>
        new() { IsSuccess = true, Stations = stations, Message = msg, ToastType = "warning" };

    public static GasStationResult Error(string msg) =>
        new() { IsSuccess = false, Message = msg, ToastType = "error" };
}
