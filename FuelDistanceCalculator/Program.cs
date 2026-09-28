using FuelDistanceCalculator;
using FuelDistanceCalculator.Interfaces;
using FuelDistanceCalculator.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using StackExchange.Redis;

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

// Eigene Middleware für Rate Limiting / Request-Schutz
if (!isE2E)
{
    app.UseMiddleware<RequestProtectionMiddleware>();
    Console.WriteLine("RequestProtectionMiddleware is enabled.");
}
else
{
    Console.WriteLine("E2E Mode: RequestProtectionMiddleware is disabled.");
}

app.UseSession();
app.UseAuthorization();
app.UseAntiforgery();

app.MapHealthChecks("/healthz");
app.MapRazorPages();

// Port kommt aus ASPNETCORE_URLS (Dockerfile: http://+:8080)
app.Run();

public partial class Program { }