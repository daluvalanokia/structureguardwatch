// StructureWatch.Web/ViewModels/AnalysisViewModel.cs

namespace StructureWatch.Web.ViewModels;

public class AnalysisViewModel
{
    public string OsmId { get; set; } = default!;
    public string BuildingName { get; set; } = "Unknown";
    public string BuildingType { get; set; } = "yes";
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
