using System.Collections.Concurrent;

public class ApiThrottle
{
    private readonly TimeSpan _defaultInterval = TimeSpan.FromSeconds(1); // Standardintervall (1 Anfrage pro Sekunde)
    private readonly ConcurrentDictionary<string, DateTime> _lastCallTimes = new ConcurrentDictionary<string, DateTime>();

    // Gemeinsame Random-Instanz, um Mehrfachinitialisierungen zu vermeiden.
    private static readonly Random _random = new Random();

    public async Task<T> ExecuteWithThrottle<T>(string apiKey, Func<Task<T>> apiCall, TimeSpan? interval = null)
    {
        Console.WriteLine("API-Throttle entered");
        var intervalToUse = interval ?? _defaultInterval;
        var timeSinceLastCall = DateTime.Now - _lastCallTimes.GetOrAdd(apiKey, DateTime.MinValue);

        if (timeSinceLastCall < intervalToUse)
        {
            // Warten, bis der Mindestzeitraum abgelaufen ist
            Console.WriteLine("Delay added");
            await Task.Delay(intervalToUse - timeSinceLastCall);
        }
        // Zufällige Verzögerung hinzufügen (z. B. zwischen 10 und 30 Sekunden)
        int randomDelaySeconds = _random.Next(100, 300); // 10 bis 30 Sekunden
        await Task.Delay(TimeSpan.FromMilliseconds(randomDelaySeconds));

        // Führe den API-Aufruf aus
        _lastCallTimes[apiKey] = DateTime.Now;
        return await apiCall();
    }
}
