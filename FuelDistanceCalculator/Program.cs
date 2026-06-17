using FuelDistanceCalculator.Services;
using Microsoft.AspNetCore.HttpOverrides;
using StackExchange.Redis;
using FuelDistanceCalculator;
using Moq;
using FuelDistanceCalculator.Interafces;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

// Registriere FuelPriceService als Singleton
builder.Services.AddSingleton<FuelPriceService>(provider =>
    new FuelPriceService());

// Registriere MarketFuelPriceService mit HttpClientFactory über Interface
builder.Services.AddHttpClient<IMarketFuelPriceService, MarketFuelPriceService>();

var env = builder.Environment;
string redisConnectionString = $"{Environment.GetEnvironmentVariable("REDIS_HOST")}";
Console.WriteLine("Redis Connection String: " + redisConnectionString); 

// Registriere Redis für Distributed Caching
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
});



// 🔧 Testumgebung erkennen
// Registriere IConnectionMultiplexer für GeoLocationService
if (env.IsEnvironment("Testing"))
{
    // 👉 Im Test: Mock-Redis anlegen
    var mockConnectionMultiplexer = new Mock<IConnectionMultiplexer>();
    var mockDatabase = new Mock<IDatabase>();
    mockConnectionMultiplexer
        .Setup(c => c.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
        .Returns(mockDatabase.Object);

    builder.Services.AddSingleton<IConnectionMultiplexer>(mockConnectionMultiplexer.Object);
    builder.Services.AddDataProtection()
        .SetApplicationName("FuelGo");
}
else
{
    // 👉 In allen anderen Umgebungen: echte Verbindung
    builder.Services.AddSingleton<IConnectionMultiplexer>(
        ConnectionMultiplexer.Connect(redisConnectionString));
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo("/app/dataprotection-keys"))
        .SetApplicationName("FuelGo");
}

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Sitzungsdauer
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Registriere IGeoLocationService mit GeoLocationService
builder.Services.AddScoped<IGeoLocationService, GeoLocationService>();
//Registriere IOilPriceService mit OilPriceService
builder.Services.AddHttpClient<IOilPriceService, OilPriceService>();

// Add services to the container
builder.Services.AddRazorPages();

builder.Services.AddAntiforgery();

builder.Services.AddMemoryCache();

var app = builder.Build();

// Configure the HTTP request pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// ForwardedHeaders-Setup
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};

if (app.Environment.IsDevelopment())
{
    forwardedHeadersOptions.KnownIPNetworks.Clear();
    forwardedHeadersOptions.KnownProxies.Clear();
}
else
{
    forwardedHeadersOptions.KnownProxies.Add(System.Net.IPAddress.Parse("172.19.0.5"));
}

app.UseForwardedHeaders(forwardedHeadersOptions);

app.UseHttpsRedirection();
app.UseRouting();
app.UseAntiforgery();

app.UseSession();

// Eigene Middleware für Rate Limiting
app.UseMiddleware<RequestProtectionMiddleware>();

app.UseAuthorization();

app.UseStaticFiles();

app.MapRazorPages();

// ✅ Kein Portbinding in Tests
if (app.Environment.IsEnvironment("Testing"))
{
    app.Run();
}
else
{
    app.Run("http://0.0.0.0:8080");
}

public partial class Program { }