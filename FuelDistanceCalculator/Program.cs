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
    new FuelPriceService(10, 2.5m));

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

// Add services to the container
builder.Services.AddRazorPages();

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

// Eigene Middleware für Rate Limiting
app.UseMiddleware<RequestProtectionMiddleware>();

app.UseAuthorization();

app.UseStaticFiles();
app.MapRazorPages();
app.Run("http://0.0.0.0:8080");