using System;
using System.Collections.Concurrent;

namespace FuelDistanceCalculator;


public class RequestProtectionMiddleware : IRequestMiddleWare
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestProtectionMiddleware> _logger;

    private const int MAX_REQUESTS_PER_WINDOW = 20;
    private static readonly TimeSpan TIME_WINDOW = TimeSpan.FromSeconds(10);
    private const int MAX_REQUEST_SIZE_BYTES = 1024 * 1024;

    private static readonly ConcurrentDictionary<string, List<DateTime>> _ipLog = new();

    public RequestProtectionMiddleware(RequestDelegate next, ILogger<RequestProtectionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        _logger.LogInformation("Client-IP: {IP}, X-Forwarded-For: {XFF}",
            context.Connection.RemoteIpAddress,
            context.Request.Headers["X-Forwarded-For"].FirstOrDefault());
        
        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var now = DateTime.UtcNow;
        var timestamps = _ipLog.GetOrAdd(ip, _ => new List<DateTime>());

        lock (timestamps)
        {
            timestamps.RemoveAll(t => now - t > TIME_WINDOW);
            if (timestamps.Count >= MAX_REQUESTS_PER_WINDOW)
            {
                _logger.LogWarning("Rate limit exceeded: {IP} - {Count} requests in {Window}s", ip, timestamps.Count, TIME_WINDOW.TotalSeconds);
                context.Response.StatusCode = 429;
                context.Response.Headers["Retry-After"] = TIME_WINDOW.TotalSeconds.ToString();
                return;
            }
            timestamps.Add(now);
        }

        if (context.Request.ContentLength > MAX_REQUEST_SIZE_BYTES)
        {
            _logger.LogWarning("Payload too large from {IP}: {Size} bytes", ip, context.Request.ContentLength);
            context.Response.StatusCode = 413;
            context.Response.ContentType = "text/plain";
            await context.Response.WriteAsync("Payload too large.");
            return;
        }

        _logger.LogInformation("Request allowed from {IP} - {Count} in window", ip, timestamps.Count);
        await _next(context);
    }
}
