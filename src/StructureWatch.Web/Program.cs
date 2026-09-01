// StructureWatch.Web/Program.cs
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using StructureWatch.Core.Services;
using StructureWatch.Data;
using StructureWatch.Agents;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// EF Core
builder.Services.AddDbContext<StructureWatchDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("Default")));

// Memory cache
builder.Services.AddMemoryCache();

// Overpass service — HttpClient with Polly retry + rate limiting
builder.Services.AddHttpClient<IOverpassService, OverpassService>()
    .AddTransientHttpErrorHandler(p => p.WaitAndRetryAsync(3, attempt =>
        TimeSpan.FromSeconds(Math.Pow(2, attempt))));

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

// Auto-migrate on startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<StructureWatchDbContext>();
    db.Database.Migrate();
}

app.UseStaticFiles();
app.UseRouting();
app.MapControllers();
app.Run();
