// StructureWatch.Data/Entities/BuildingAnalysisEntity.cs
namespace StructureWatch.Data.Entities;

public class BuildingAnalysisEntity
{
    public int Id { get; set; }
    public string OsmId { get; set; } = default!;
    public string? BuildingName { get; set; }
    public string? BuildingType { get; set; }
    public string? Height { get; set; }
    public string? Levels { get; set; }
    public string LoadCapacity { get; set; } = string.Empty;
    public string StructuralIntegrity { get; set; } = string.Empty;
    public string SeismicRisk { get; set; } = string.Empty;
    public string WindLoad { get; set; } = string.Empty;
    public string OccupancyClass { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public List<string> RiskFactors { get; set; } = new();
    public DateTime AnalyzedDate { get; set; }
}
