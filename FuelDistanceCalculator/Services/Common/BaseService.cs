namespace FuelDistanceCalculator.Services.Common;

public abstract class BaseService
{
    protected string Mode { get; set; }

    protected HttpClient HttpRequestClient { get; set; }
}