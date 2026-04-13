using System.Collections.Concurrent;
using System.Net;
using System.Text;
using FuelDistanceCalculator;
using Moq;

namespace FuelDistanceCalculatorTest.GlobalTests;

public class RequestProtectionMiddlewareTest
{

    [SetUp]
    public void SetUp()
    {
        // Clear _ipLog to ensure test isolation
        var ipLogField = typeof(RequestProtectionMiddleware)
            .GetField("_ipLog", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var ipLog = (ConcurrentDictionary<string, List<DateTime>>)ipLogField.GetValue(null);
        ipLog.Clear();
    }


    [Test]
    public async Task InvokeAsync_UnderRateLimit_ProceedsToNext()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
        context.Request.ContentLength = 1000; // Well below 1MB

        var nextInvoked = false;
        RequestDelegate next = (ctx) => { nextInvoked = true; return Task.CompletedTask; };
        var logger = new Mock<ILogger<RequestProtectionMiddleware>>();

        var middleware = new RequestProtectionMiddleware(next, logger.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.That(nextInvoked, Is.True);
        Assert.That(context.Response.StatusCode.Equals(200), Is.True); // Default status code
    }
    [Test]
    public async Task InvokeAsync_ExceedsRateLimit_Returns429()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
        context.Request.ContentLength = 1000;

        var nextInvoked = false;
        RequestDelegate next = (ctx) => { nextInvoked = true; return Task.CompletedTask; };
        var logger = new Mock<ILogger<RequestProtectionMiddleware>>();

        var middleware = new RequestProtectionMiddleware(next, logger.Object);

        // Simulate 20 requests within the time window
        var ipLogField = typeof(RequestProtectionMiddleware)
            .GetField("_ipLog", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var ipLog = (ConcurrentDictionary<string, List<DateTime>>)ipLogField.GetValue(null);
        ipLog[context.Connection.RemoteIpAddress.ToString()] = Enumerable
            .Repeat(DateTime.UtcNow, 20)
            .ToList();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.That(nextInvoked, Is.False);
        Assert.That(429.Equals(context.Response.StatusCode), "Expected status code 429 for rate limit exceeded.");
        Assert.That(context.Response.Headers["Retry-After"].Equals("10"), "Expected Retry-After header to be set.");
    }

    [Test]
    public async Task InvokeAsync_PayloadTooLarge_Returns413()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
        context.Request.ContentLength = 1024 * 1024 + 1; // Just over 1MB

        // Set Response.Body to a MemoryStream
        var responseBodyStream = new MemoryStream();
        context.Response.Body = responseBodyStream;

        var nextInvoked = false;
        RequestDelegate next = (ctx) => { nextInvoked = true; return Task.CompletedTask; };
        var logger = new Mock<ILogger<RequestProtectionMiddleware>>();

        var middleware = new RequestProtectionMiddleware(next, logger.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.That(nextInvoked, Is.False, "Next middleware should not be invoked.");
        Assert.That(context.Response.StatusCode, Is.EqualTo(413), "Status code should be 413.");
        Assert.That(context.Response.ContentType, Is.EqualTo("text/plain"), "Content type should be text/plain.");
        var responseBody = await ReadResponseBody(context.Response);
        Assert.That(responseBody, Is.EqualTo("Payload too large."), "Response body should match expected message.");
    }

    [Test]
    public async Task InvokeAsync_OldTimestampsRemoved_ProceedsToNext()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
        context.Request.ContentLength = 1000;

        var nextInvoked = false;
        RequestDelegate next = (ctx) => { nextInvoked = true; return Task.CompletedTask; };
        var logger = new Mock<ILogger<RequestProtectionMiddleware>>();

        var middleware = new RequestProtectionMiddleware(next, logger.Object);

        // Simulate 20 old requests
        var ipLogField = typeof(RequestProtectionMiddleware)
            .GetField("_ipLog", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        var ipLog = (ConcurrentDictionary<string, List<DateTime>>)ipLogField.GetValue(null);
        ipLog[context.Connection.RemoteIpAddress.ToString()] = Enumerable
            .Repeat(DateTime.UtcNow.AddSeconds(-11), 20)
            .ToList();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.That(nextInvoked, Is.True);
        Assert.That(context.Response.StatusCode.Equals(200), Is.True);
        //Assert.(ipLog[context.Connection.RemoteIpAddress.ToString()]); // Only the new request remains
    }

    [Test]
    public async Task InvokeAsync_ConcurrentRequests_HandlesSafely()
    {
        // Arrange
        var context1 = new DefaultHttpContext();
        var context2 = new DefaultHttpContext();
        context1.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
        context2.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
        context1.Request.ContentLength = 1000;
        context2.Request.ContentLength = 1000;

        var nextInvokedCount = 0;
        RequestDelegate next = (ctx) => { Interlocked.Increment(ref nextInvokedCount); return Task.CompletedTask; };
        var logger = new Mock<ILogger<RequestProtectionMiddleware>>();

        var middleware = new RequestProtectionMiddleware(next, logger.Object);

        // Act
        var tasks = new List<Task>();
        for (int i = 0; i < 10; i++)
        {
            tasks.Add(Task.Run(() => middleware.InvokeAsync(i % 2 == 0 ? context1 : context2)));
        }
        await Task.WhenAll(tasks);

        // Assert
        Assert.That(nextInvokedCount.Equals(10)); // All requests should pass initially
    }

    // Helper to read response body
    private async Task<string> ReadResponseBody(HttpResponse response)
    {
        response.Body.Seek(0, SeekOrigin.Begin); // Reset stream to start
        using var reader = new StreamReader(response.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync();
        response.Body.Seek(0, SeekOrigin.Begin); // Reset for potential further reads
        return body;
    }
}