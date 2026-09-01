// StructureWatch.Web/Controllers/SearchController.cs
using Microsoft.AspNetCore.Mvc;
using StructureWatch.Core.Services;

namespace StructureWatch.Web.Controllers;

public class SearchController : Controller
{
    private readonly INominatimService _nominatim;

    public SearchController(INominatimService nominatim)
    {
        _nominatim = nominatim;
    }

    // GET /api/search?q=New+York
    [HttpGet("/api/search")]
    public async Task<IActionResult> Search([FromQuery] string q)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 3)
            return Ok(new List<object>());

        var results = await _nominatim.SearchAsync(q, limit: 5);
        return Ok(results.Select(r => new
        {
            displayName = r.DisplayName,
            lat = r.Lat,
            lon = r.Lon,
            city = r.City,
            state = r.State,
            country = r.Country,
            label = new[] { r.City, r.State, r.Country }.Where(s => !string.IsNullOrEmpty(s)).Aggregate((a, b) => $"{a}, {b}")
        }));
    }
}
