using FuelDistanceCalculator.Interfaces;
using FuelDistanceCalculator.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using StackExchange.Redis;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics;

var builder = WebApplication.CreateBuilder(args);
var env = builder.Environment;

var isTesting = env.IsEnvironment("Testing");
var isE2E = builder.Configuration["MODE_TYPE"] == "E2E";

// ---------------------------------------------------------------------------
// Konfiguration prüfen (Fail-Fast in Produktion)
// ---------------------------------------------------------------------------
var redisHost = builder.Configuration["REDIS_HOST"];
var redisPassword = builder.Configuration["REDIS_PASSWORD"];

if (!env.IsDevelopment() && !isTesting &&
    (string.IsNullOrWhiteSpace(redisHost) || string.IsNullOrWhiteSpace(redisPassword)))
{
    throw new InvalidOperationException("REDIS_HOST und REDIS_PASSWORD müssen gesetzt sein.");
}

// Der E2E-Modus deaktiviert den Request-Schutz. In Produktion darf er nie aktiv sein.
if (isE2E && env.IsProduction())
{
    throw new InvalidOperationException("MODE_TYPE=E2E ist in der Production-Umgebung nicht erlaubt.");
}

// ---------------------------------------------------------------------------
// Services
// ---------------------------------------------------------------------------
builder.Services.AddSingleton<FuelPriceService>(provider => new FuelPriceService());

// HttpClients mit Timeout, damit externe APIs die App nicht blockieren
builder.Services.AddHttpClient<IMarketFuelPriceService, MarketFuelPriceService>()
    .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(10));
builder.Services.AddHttpClient<IOilPriceService, OilPriceService>()
    .ConfigureHttpClient(c => c.Timeout = TimeSpan.FromSeconds(10));

// Redis: eine einzige Verbindung, die auch der Distributed Cache nutzt.
// AbortOnConnectFail = false: die App startet auch, wenn Redis kurz später bereit ist.
var redisOptions = ConfigurationOptions.Parse(
    string.IsNullOrWhiteSpace(redisHost) ? "localhost:6379" : redisHost);
if (!string.IsNullOrWhiteSpace(redisPassword))
{
    redisOptions.Password = redisPassword;
}
redisOptions.AbortOnConnectFail = false;

builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisOptions));
builder.Services.AddStackExchangeRedisCache(_ => { });
builder.Services.AddOptions<RedisCacheOptions>()
    .Configure<IConnectionMultiplexer>((options, mux) =>
        options.ConnectionMultiplexerFactory = () => Task.FromResult(mux));

// Data Protection: Schlüssel persistent speichern (Volume /app/dataprotection-keys),
// sonst sind Sessions/Antiforgery-Tokens nach jedem Deploy ungültig.
var dataProtection = builder.Services.AddDataProtection().SetApplicationName("FuelGo");
if (!isTesting)
{
    dataProtection.PersistKeysToFileSystem(new DirectoryInfo("/app/dataprotection-keys"));
}

var cookieSecurePolicy = env.IsProduction()
    ? CookieSecurePolicy.Always
    : CookieSecurePolicy.SameAsRequest;

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = cookieSecurePolicy;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddAntiforgery(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = cookieSecurePolicy;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

builder.Services.AddHsts(options => options.MaxAge = TimeSpan.FromDays(365));

builder.Services.AddScoped<IGeoLocationService, GeoLocationService>();
builder.Services.AddRazorPages();
builder.Services.AddMemoryCache();
builder.Services.AddHealthChecks();

// Nur nginx darf X-Forwarded-* setzen (Docker-Netze aus docker-compose.yml / docker-compose.e2e.yml)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

    if (env.IsDevelopment())
    {
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    }
    else
    {
        options.KnownIPNetworks.Add(new System.Net.IPNetwork(System.Net.IPAddress.Parse("172.19.0.0"), 24));
        options.KnownIPNetworks.Add(new System.Net.IPNetwork(System.Net.IPAddress.Parse("172.20.0.0"), 24));
    }
});

builder.WebHost.ConfigureKestrel(kestrel =>
{
    kestrel.AddServerHeader = false;
    kestrel.Limits.MaxRequestBodySize = 64 * 1024;
});

    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = (ctx, _) =>
        {
            ctx.HttpContext.Response.Headers.RetryAfter = "10";
            return ValueTask.CompletedTask;
        };

        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(ctx =>
        {
            if (ctx.Features.Get<IStatusCodeReExecuteFeature>() is not null ||
                ctx.Request.Path.StartsWithSegments("/healthz"))
                return RateLimitPartition.GetNoLimiter("skip");

            var config = ctx.RequestServices.GetRequiredService<IConfiguration>();
            var limit = config.GetValue("RateLimit:PermitLimit", 20);

            return RateLimitPartition.GetSlidingWindowLimiter(ClientKey(ctx), _ =>
                new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = limit,
                    Window = TimeSpan.FromSeconds(10),
                    SegmentsPerWindow = 5,
                    QueueLimit = 0
                });
        });

        options.AddPolicy("upstream", ctx =>
        {
            var config = ctx.RequestServices.GetRequiredService<IConfiguration>();
            var limit = config.GetValue("RateLimit:UpstreamPermitLimit", 10);
            return RateLimitPartition.GetSlidingWindowLimiter(ClientKey(ctx), _ =>
                new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = limit,
                    Window = TimeSpan.FromMinutes(1),
                    SegmentsPerWindow = 6,
                    QueueLimit = 0
                });
        });
    });

    static string ClientKey(HttpContext ctx)
    {
        var ip = ctx.Connection.RemoteIpAddress;
        if (ip is null) return "unknown";
        if (ip.IsIPv4MappedToIPv6) ip = ip.MapToIPv4();
        if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetworkV6)
        {
            var bytes = ip.GetAddressBytes();
            Array.Clear(bytes, 8, 8);
            return new System.Net.IPAddress(bytes) + "/64";
        }
        return ip.ToString();
    }


var app = builder.Build();

// ---------------------------------------------------------------------------
// HTTP-Pipeline
// ---------------------------------------------------------------------------
// Muss ganz vorne stehen, damit Scheme und Client-IP für alle folgenden Middlewares stimmen
app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/Error{0}");

app.UseStaticFiles();
app.UseRouting();

app.UseRateLimiter();


app.UseSession();
app.UseAuthorization();
app.UseAntiforgery();

app.MapHealthChecks("/healthz");

app.Use(async (ctx, next) =>
{
    var nonce = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(16));
    ctx.Items["csp-nonce"] = nonce;

    var csp =
            "default-src 'self'; " +
            $"script-src 'self' 'nonce-{nonce}' https://www.googletagmanager.com https://unpkg.com https://cdnjs.cloudflare.com; " +
            "style-src 'self' https://unpkg.com https://cdnjs.cloudflare.com 'unsafe-inline'; " +
            "img-src 'self' data: https://unpkg.com https://*.tile.openstreetmap.org " +
                "https://*.google-analytics.com https://*.googletagmanager.com; " +
            "font-src 'self' https://cdnjs.cloudflare.com; " +
            "connect-src 'self' https://*.google-analytics.com https://*.analytics.google.com https://*.googletagmanager.com; " +
            "object-src 'none'; " +
            "base-uri 'self'; " +
            "form-action 'self'; " +
            "frame-ancestors 'none'";

    //Content-Security-Policy-Report-Only
    ctx.Response.Headers["Content-Security-Policy"] = csp; // erst testen, dann auf Content-Security-Policy umstellen
    await next();
});

app.MapRazorPages();

// Port kommt aus ASPNETCORE_URLS (Dockerfile: http://+:8080)
app.Run();

public partial class Program { }