using Microsoft.EntityFrameworkCore;
using FuelDistanceCalculator.Data;
using FuelDistanceCalculator.Services;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.AspNetCore.HttpOverrides;
using StackExchange.Redis;
using FuelDistanceCalculator;

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

// Registriere IConnectionMultiplexer für GeoLocationService
builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect("redis:6379"));

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
    // In Dev: keine Proxy-Beschränkung
    forwardedHeadersOptions.KnownNetworks.Clear();
    forwardedHeadersOptions.KnownProxies.Clear();
}
else
{
    // In Production: Nur IP des NGINX-Proxys vertrauen
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
app.Run("http://0.0.0.0:8080");