using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using StructureWatch.Core.Services;
using StructureWatch.Data;
using StructureWatch.Agents;

var builder = WebApplication.CreateBuilder(args);

// MVC + Razor Views
builder.Services.AddControllersWithViews();

// EF Core — LocalDB for localhost dev
builder.Services.AddDbContext<StructureWatchDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// Memory cache (for Overpass footprint caching)
builder.Services.AddMemoryCache();

// Overpass service — HttpClient (rate limiting handled in-service via SemaphoreSlim)
builder.Services.AddHttpClient<IOverpassService, OverpassService>();

// Nominatim address search service
builder.Services.AddHttpClient<INominatimService, NominatimService>((sp, client) =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
});

// Scan service (singleton — tracks current scan state)
builder.Services.AddSingleton<IScanService, ScanService>();

// Collision validator (singleton — stateless)
builder.Services.AddSingleton<CollisionValidator>();

// TokenSaver agent client
builder.Services.AddHttpClient<ITokenSaverAgent, TokenSaverAgent>((sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var baseUrl = config["TokenSaver:BaseUrl"] ?? "https://app.base44.com";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

var app = builder.Build();

// Create database on startup (no migrations needed — schema auto-generated)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<StructureWatchDbContext>();
    db.Database.EnsureCreated();
}

// Static files (wwwroot — JS, CSS, Leaflet)
app.UseStaticFiles();

// Routing
app.UseRouting();

// Default route: /Map/Index
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Map}/{action=Index}/{id?}");

app.Run();
