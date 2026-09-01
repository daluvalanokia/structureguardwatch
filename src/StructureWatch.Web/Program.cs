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

// Create database on startup (no migrations needed — schema auto-generated).
// Wrapped in try/catch: a LocalDB/connection failure here must NEVER take down
// the whole web server — it should log and let the app start so map/search/etc.
// (which don't need the DB) keep working, and so the real error is visible
// instead of "web server is no longer running".
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<StructureWatchDbContext>();
        db.Database.EnsureCreated();
        logger.LogInformation("Database ready.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Database initialization failed — app will continue running without DB persistence. " +
            "Check that SQL Server LocalDB is installed and the connection string in appsettings.json is correct.");
    }
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
