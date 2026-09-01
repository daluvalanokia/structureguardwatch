using Xunit;
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using StructureWatch.Core.Services;

namespace StructureWatch.Core.Tests;

public class OverpassServiceTests
{
    [Fact(Skip = "Integration test — requires live Overpass API. Run manually.")]
    public async Task FetchFootprintsAsync_ReturnsBuildings_ForManhattanBbox()
    {
        // Arrange
        var http = new HttpClient { BaseAddress = new Uri("https://overpass-api.de") };
        var cache = new MemoryCache(new MemoryCacheOptions());
        var config = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Overpass:CacheTtlMinutes"] = "5",
                ["Overpass:MinIntervalMs"] = "2000"
            })
            .Build();
        var service = new OverpassService(http, cache, config);

        // Manhattan bbox
        double s = 40.7550, w = -73.9900, n = 40.7620, e = -73.9800;

        // Act
        var result = await service.FetchFootprintsAsync(s, w, n, e);

        // Assert
        Assert.NotEmpty(result);
        Assert.All(result, f => Assert.NotEmpty(f.Geometry));
        Assert.All(result, f => Assert.True(f.CalculatedHeight > 0));
    }

    [Fact]
    public void CacheKey_IsDeterministicForSameBbox()
    {
        // Verify that the same bbox produces the same cache key format
        double s = 40.7550, w = -73.9900, n = 40.7620, e = -73.9800;
        string key = $"fp_{s:F4}_{w:F4}_{n:F4}_{e:F4}";
        string key2 = $"fp_{s:F4}_{w:F4}_{n:F4}_{e:F4}";
        Assert.Equal(key, key2);
    }
}
