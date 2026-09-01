// StructureWatch.Core/Models/AnalysisResult.cs
namespace StructureWatch.Core.Models;

/// <summary>DTO for TokenSaver agent analysis output (mirrors Agents.Dtos.AnalysisResponse).</summary>
public class AnalysisResult
{
    public string LoadCapacity { get; set; } = string.Empty;
    public string StructuralIntegrity { get; set; } = string.Empty;
    public string SeismicRisk { get; set; } = string.Empty;
    public string WindLoad { get; set; } = string.Empty;
    public string OccupancyClass { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public List<string> RiskFactors { get; set; } = new();
}
