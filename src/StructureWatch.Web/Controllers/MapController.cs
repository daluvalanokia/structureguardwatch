// StructureWatch.Web/Controllers/MapController.cs
using Microsoft.AspNetCore.Mvc;
using StructureWatch.Core.Services;

namespace StructureWatch.Web.Controllers;

public class MapController : Controller
{
    private readonly IOverpassService _overpass;
    private readonly ILogger<MapController> _logger;

    public MapController(IOverpassService overpass, ILogger<MapController> logger)
    {
        _overpass = overpass;
        _logger = logger;
    }

    // GET / → Map view
    public IActionResult Index() => View();

    // GET /api/footprints?bbox=south,west,north,east
    [HttpGet("/api/footprints")]
    public async Task<IActionResult> Footprints([FromQuery] string bbox)
    {
        var parts = bbox.Split(',');
        if (parts.Length != 4) return BadRequest("bbox must be: south,west,north,east");

        if (!double.TryParse(parts[0], out var s) ||
            !double.TryParse(parts[1], out var w) ||
            !double.TryParse(parts[2], out var n) ||
            !double.TryParse(parts[3], out var e))
            return BadRequest("Invalid bbox values");

        try
        {
            var footprints = await _overpass.FetchFootprintsAsync(s, w, n, e);
            _logger.LogInformation("Fetched {Count} footprints for bbox {Bbox}", footprints.Count, bbox);
            return Ok(footprints);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Overpass fetch failed for bbox {Bbox}", bbox);
            return StatusCode(502, "Failed to fetch building footprints from OSM");
        }
    }
}
