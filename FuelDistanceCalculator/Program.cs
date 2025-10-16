using Microsoft.EntityFrameworkCore;
using FuelDistanceCalculator.Data;
using Npgsql;
using FuelDistanceCalculator.Services;
using Microsoft.Extensions.Caching.Distributed;
using FuelDistanceCalculator;
using Microsoft.AspNetCore.HttpOverrides;


var builder = WebApplication.CreateBuilder(args);




// Datenbankverbindung setzen (SQLite)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Registrieren des FuelPriceService in der DI-Container
// 🚀 Registriere FuelPriceService als Singleton, ABER mit einem Factory-Provider
builder.Services.AddSingleton<FuelPriceService>(provider =>
    new FuelPriceService(10, 2.5m));

// 🚀 Registriere MarketFuelPriceService mit HttpClientFactory
builder.Services.AddHttpClient<MarketFuelPriceService>();

// 🚀 Registriere Redis für Distributed Caching
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "redis:6379";
});

// 🚀 Registriere GeoLocationService mit HttpClient UND Redis-Cache
builder.Services.AddHttpClient<GeoLocationService>(); // HttpClient für API-Calls
builder.Services.AddScoped<GeoLocationService>(provider =>
{
    var httpClientFactory = provider.GetRequiredService<IHttpClientFactory>();
    var cache = provider.GetRequiredService<IDistributedCache>();
    return new GeoLocationService(httpClientFactory, builder.Configuration, Environment.GetEnvironmentVariable("MODE_TYPE"));
});

// Add services to the container.
builder.Services.AddRazorPages();

var app = builder.Build();
// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// 🔐 ForwardedHeaders-Setup (soll IMMER vor Middleware kommen)
var forwardedHeadersOptions = new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
};

if (app.Environment.IsDevelopment())
{
    // 🔓 In Dev: keine Proxy-Beschränkung → erlaubt Tests über localhost / :8080
    forwardedHeadersOptions.KnownNetworks.Clear();
    forwardedHeadersOptions.KnownProxies.Clear();
}
else
{
    // 🔒 In Production: Nur IP des NGINX-Proxys vertrauen (Docker Bridge IP)
    forwardedHeadersOptions.KnownProxies.Add(System.Net.IPAddress.Parse("172.19.0.5"));
}

app.UseForwardedHeaders(forwardedHeadersOptions);


app.UseHttpsRedirection();
app.UseRouting();

// Wichtig: Vor allen anderen Middlewares aufrufen
// 🛡 Eigene Middleware, z. B. für Rate Limiting
app.UseMiddleware<RequestProtectionMiddleware>();

app.UseAuthorization();

app.UseStaticFiles();
app.MapRazorPages();
app.Run("http://0.0.0.0:8080"); //Anwendung aluscht auf allen IPs nicht nur auf localhost
