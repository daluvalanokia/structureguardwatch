// StructureWatch.Web/Controllers/AnalysisController.cs
using Microsoft.AspNetCore.Mvc;
using StructureWatch.Agents;
using StructureWatch.Data;
using StructureWatch.Data.Entities;

namespace StructureWatch.Web.Controllers;

public class AnalysisController : Controller
{
    private readonly ITokenSaverAgent _agent;
    private readonly StructureWatchDbContext _db;

    public AnalysisController(ITokenSaverAgent agent, StructureWatchDbContext db)
    {
        _agent = agent;
        _db = db;
    }

    // POST /api/analyze
    [HttpPost("/api/analyze")]
    public async Task<IActionResult> Analyze([FromBody] AnalyzeRequest req)
    {
        if (string.IsNullOrEmpty(req.OsmId) || req.Tags is null)
            return BadRequest("OsmId and Tags are required");

        var result = await _agent.RunAnalysisAsync(req.OsmId, req.Tags);

        // Persist to database
        var entity = new BuildingAnalysisEntity
        {
            OsmId = req.OsmId,
            BuildingName = req.Tags.GetValueOrDefault("name") ?? "Unknown",
            BuildingType = req.Tags.GetValueOrDefault("building") ?? "yes",
            Height = req.Tags.GetValueOrDefault("height"),
            Levels = req.Tags.GetValueOrDefault("building:levels"),
            LoadCapacity = result.LoadCapacity,
            StructuralIntegrity = result.StructuralIntegrity,
            SeismicRisk = result.SeismicRisk,
            WindLoad = result.WindLoad,
            OccupancyClass = result.OccupancyClass,
            Summary = result.Summary,
            RiskFactors = result.RiskFactors,
            AnalyzedDate = DateTime.UtcNow
        };
        _db.BuildingAnalyses.Add(entity);
        await _db.SaveChangesAsync();

        return Ok(result);
    }
}

public class AnalyzeRequest
{
    public string OsmId { get; set; } = default!;
    public Dictionary<string, string> Tags { get; set; } = new();
}
