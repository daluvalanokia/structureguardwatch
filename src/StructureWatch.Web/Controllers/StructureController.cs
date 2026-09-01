// StructureWatch.Web/Controllers/StructureController.cs
using Microsoft.AspNetCore.Mvc;
using StructureWatch.Core.Services;
using StructureWatch.Core.Models;

namespace StructureWatch.Web.Controllers;

public class StructureController : Controller
{
    private readonly CollisionValidator _validator;
    private readonly IOverpassService _overpass;

    public StructureController(CollisionValidator validator, IOverpassService overpass)
    {
        _validator = validator;
        _overpass = overpass;
    }

    // GET /api/structures/{osmId}?bbox=south,west,north,east
    [HttpGet("/api/structures/{osmId}")]
    public async Task<IActionResult> Get(string osmId, [FromQuery] string bbox)
    {
        var parts = bbox.Split(',');
        if (parts.Length != 4) return BadRequest("bbox required to locate building");

        var footprints = await _overpass.FetchFootprintsAsync(
            double.Parse(parts[0]), double.Parse(parts[1]),
            double.Parse(parts[2]), double.Parse(parts[3]));

        var building = footprints.FirstOrDefault(f => f.OsmId == osmId);
        if (building is null) return NotFound($"Building {osmId} not in current viewport");

        return Ok(new
        {
            building.OsmId,
            building.Name,
            building.Address,
            building.Levels,
            building.Height,
            building.BuildingType,
            building.CalculatedHeight,
            building.Tags,
            building.Geometry
        });
    }

    // POST /api/collisions
    [HttpPost("/api/collisions")]
    public IActionResult Collisions([FromBody] CollisionRequest req)
    {
        if (req.Selected is null || req.AllBuildings is null || req.AllBuildings.Count == 0)
            return BadRequest("Missing required fields");

        var result = _validator.CheckAabb(req.Selected, (req.DragDx, req.DragDy), req.AllBuildings);
        return Ok(result);
    }
}

public class CollisionRequest
{
    public BuildingFootprint? Selected { get; set; }
    public double DragDx { get; set; }
    public double DragDy { get; set; }
    public List<BuildingFootprint>? AllBuildings { get; set; }
}
