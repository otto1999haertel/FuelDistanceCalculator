using Microsoft.EntityFrameworkCore;
using FuelDistanceCalculator.Data;
using FuelDistanceCalculator.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.AspNetCore.HttpOverrides;
using StackExchange.Redis;
using FuelDistanceCalculator;
using Moq; // nur nötig, wenn du direkt im Program testweise mockst

var builder = WebApplication.CreateBuilder(args);

// Datenbankverbindung setzen (PostgreSQL)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Registriere FuelPriceService als Singleton
builder.Services.AddSingleton<FuelPriceService>(provider =>
    new FuelPriceService());

// Registriere MarketFuelPriceService mit HttpClientFactory
builder.Services.AddHttpClient<MarketFuelPriceService>();

// Registriere Redis für Distributed Caching
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "redis:6379";
});

// 🔧 Testumgebung erkennen
var env = builder.Environment;

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
}
else
{
    // 👉 In allen anderen Umgebungen: echte Verbindung
    builder.Services.AddSingleton<IConnectionMultiplexer>(
        ConnectionMultiplexer.Connect("redis:6379"));
}

// Registriere IGeoLocationService mit GeoLocationService
builder.Services.AddScoped<IGeoLocationService, GeoLocationService>();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Sitzungsdauer
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Add services to the container
builder.Services.AddRazorPages();

builder.Services.AddAntiforgery();

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
    forwardedHeadersOptions.KnownNetworks.Clear();
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