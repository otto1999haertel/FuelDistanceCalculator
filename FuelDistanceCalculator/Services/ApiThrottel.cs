using System.Collections.Concurrent;

public class ApiThrottle
{
    private readonly TimeSpan _defaultInterval = TimeSpan.FromSeconds(1); // Standardintervall (1 Anfrage pro Sekunde)
    private readonly ConcurrentDictionary<string, DateTime> _lastCallTimes = new ConcurrentDictionary<string, DateTime>();

    // Gemeinsame Random-Instanz, um Mehrfachinitialisierungen zu vermeiden.
    private static readonly Random _random = new Random();

    private readonly SemaphoreSlim _semaphore;

    //Only one thread at a time can access the API
    public ApiThrottle(int maxConcurrentCalls = 1)
    {
        _semaphore = new SemaphoreSlim(maxConcurrentCalls, maxConcurrentCalls);
    }

    public async Task<T> ExecuteWithThrottle<T>(string apiKey, Func<Task<T>> apiCall, TimeSpan? interval = null)
    {
        Console.WriteLine($"[API Call Thread {Thread.CurrentThread.ManagedThreadId}] API-Throttle entered for {apiKey} at {DateTime.Now:HH:mm:ss.fff}, Semaphore Count: {_semaphore.CurrentCount}");
        var intervalToUse = interval ?? _defaultInterval;

        await _semaphore.WaitAsync();
        try
        {
            var timeSinceLastCall = DateTime.Now - _lastCallTimes.GetOrAdd(apiKey, DateTime.MinValue);
            var baseDelay = intervalToUse - timeSinceLastCall;
            if (baseDelay > TimeSpan.Zero)
            {
                // Jitter hinzufügen: z. B. random zwischen -20% und +20% des baseDelays
                double jitterFactor = 0.2; // Anpassbar, z. B. 0.2 für ±20%
                double jitter = (2 * _random.NextDouble() - 1) * jitterFactor * baseDelay.TotalMilliseconds; // Zufällig positiv/negativ
                var delayWithJitter = baseDelay.Add(TimeSpan.FromMilliseconds(jitter));

                // Stelle sicher, dass Delay nicht negativ wird
                if (delayWithJitter > TimeSpan.Zero)
                {
                    Console.WriteLine($"[API CallThread {Thread.CurrentThread.ManagedThreadId}] Delay added for {apiKey} ({delayWithJitter}) at {DateTime.Now:HH:mm:ss.fff} (including jitter)");
                    await Task.Delay(delayWithJitter);
                }
            }

            Console.WriteLine($"[API Call Thread {Thread.CurrentThread.ManagedThreadId}] Executing API call for {apiKey} at {DateTime.Now:HH:mm:ss.fff}");
            _lastCallTimes[apiKey] = DateTime.Now;
            var result = await apiCall();
            Console.WriteLine($"[API Call Thread {Thread.CurrentThread.ManagedThreadId}] API call for {apiKey} completed at {DateTime.Now:HH:mm:ss.fff}");
            return result;
        }
        finally
        {
            Console.WriteLine($"[API Call Thread {Thread.CurrentThread.ManagedThreadId}] Releasing semaphore for {apiKey} at {DateTime.Now:HH:mm:ss.fff}, New Semaphore Count: {_semaphore.CurrentCount + 1}");
            _semaphore.Release();
        }
    }
}
