// StructureWatch.Core/Services/NominatimService.cs
using System.Text.Json;
using StructureWatch.Core.Models;

namespace StructureWatch.Core.Services;

public class NominatimService : INominatimService
{
    private readonly HttpClient _http;
    private readonly ILogger<NominatimService> _logger;

    public NominatimService(HttpClient http, ILogger<NominatimService> logger)
    {
        _http = http;
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("StructureWatch/1.0");
        _logger = logger;
    }

    public async Task<List<SearchResult>> SearchAsync(string query, int limit = 5)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 3)
            return new List<SearchResult>();

        var url = $"https://nominatim.openstreetmap.org/search?q={Uri.EscapeDataString(query)}&format=json&limit={limit}&addressdetails=1";

        try
        {
            var resp = await _http.GetAsync(url);
            resp.EnsureSuccessStatusCode();

            var json = await resp.Content.ReadFromJsonAsync<JsonDocument>();
            var results = new List<SearchResult>();

            foreach (var el in json!.RootElement.EnumerateArray())
            {
                var addr = el.GetProperty("address");
                var city = addr.GetProperty("city").GetString()
                    ?? addr.GetProperty("town").GetString()
                    ?? addr.GetProperty("village").GetString()
                    ?? addr.GetProperty("county").GetString()
                    ?? "";

                results.Add(new SearchResult(
                    DisplayName: el.GetProperty("display_name").GetString() ?? "",
                    Lat: el.GetProperty("lat").GetDouble(),
                    Lon: el.GetProperty("lon").GetDouble(),
                    City: city,
                    State: addr.GetProperty("state").GetString() ?? "",
                    Country: addr.GetProperty("country").GetString() ?? ""
                ));
            }

            _logger.LogInformation("Nominatim search for '{Query}' returned {Count} results", query, results.Count);
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Nominatim search failed for '{Query}'", query);
            return new List<SearchResult>();
        }
    }
}
