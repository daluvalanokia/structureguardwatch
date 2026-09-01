// StructureWatch.Core/Services/OverpassService.cs
using System.Text.Json;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using StructureWatch.Core.Models;
using StructureWatch.Core.Extensions;

namespace StructureWatch.Core.Services;

public class OverpassService : IOverpassService
{
    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;
    private readonly TimeSpan _cacheTtl;
    private readonly SemaphoreSlim _rateGate;
    private readonly int _minIntervalMs;

    public OverpassService(HttpClient http, IMemoryCache cache, IConfiguration config)
    {
        _http = http;
        _cache = cache;
        _cacheTtl = TimeSpan.FromMinutes(config.GetValue("Overpass:CacheTtlMinutes", 5));
        _minIntervalMs = config.GetValue("Overpass:MinIntervalMs", 2000);
        _rateGate = new SemaphoreSlim(1, 1);
    }

    public async Task<List<BuildingFootprint>> FetchFootprintsAsync(double s, double w, double n, double e)
    {
        string cacheKey = $"fp_{s:F4}_{w:F4}_{n:F4}_{e:F4}";
        if (_cache.TryGetValue(cacheKey, out List<BuildingFootprint>? cached) && cached is not null)
            return cached;

        // Simple rate limiting — wait for previous request to complete its interval
        await _rateGate.WaitAsync();
        try
        {
            // Ensure minimum interval between requests
            await Task.Delay(_minIntervalMs);

            string query = $$"""
                [out:json][timeout:25];
                (
                  way["building"]({{s}},{{w}},{{n}},{{e}});
                  relation["building"]({{s}},{{w}},{{n}},{{e}});
                );
                out geom;
                """;

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("data", query)
            });

            var resp = await _http.PostAsync("https://overpass-api.de/api/interpreter", content);
            resp.EnsureSuccessStatusCode();

            var json = await resp.Content.ReadFromJsonAsync<JsonDocument>();
            var buildings = ParseOverpassResponse(json!);

            _cache.Set(cacheKey, buildings, _cacheTtl);
            return buildings;
        }
        finally
        {
            _rateGate.Release();
        }
    }

    private static List<BuildingFootprint> ParseOverpassResponse(JsonDocument doc)
    {
        var result = new List<BuildingFootprint>();

        foreach (var element in doc.RootElement.GetProperty("elements").EnumerateArray())
        {
            var footprint = new BuildingFootprint
            {
                OsmId = $"{element.GetProperty("type").GetString()}/{element.GetProperty("id").GetInt64()}",
                OsmType = element.GetProperty("type").GetString()!
            };

            if (element.TryGetProperty("tags", out var tagsEl))
            {
                foreach (var tag in tagsEl.EnumerateObject())
                    footprint.Tags[tag.Name] = tag.Value.GetString() ?? "";
                footprint.BuildingType = footprint.Tags.GetValueOrDefault("building") ?? "yes";
            }

            if (element.TryGetProperty("geometry", out var geomEl))
            {
                foreach (var pt in geomEl.EnumerateArray())
                {
                    double lat = pt.GetProperty("lat").GetDouble();
                    double lng = pt.GetProperty("lon").GetDouble();
                    footprint.Geometry.Add(new() { lat, lng });
                }
            }

            footprint.CalculatedHeight = GeoExtensions.ComputeHeight(footprint.Tags);

            // AABB in WebMercator
            double minX = double.MaxValue, maxX = double.MinValue, minY = double.MaxValue, maxY = double.MinValue;
            foreach (var pt in footprint.Geometry)
            {
                var (x, y) = GeoExtensions.ToWebMercator(pt[0], pt[1]);
                minX = Math.Min(minX, x); maxX = Math.Max(maxX, x);
                minY = Math.Min(minY, y); maxY = Math.Max(maxY, y);
            }
            footprint.MinX = minX; footprint.MaxX = maxX; footprint.MinY = minY; footprint.MaxY = maxY;

            result.Add(footprint);
        }

        return result;
    }
}
