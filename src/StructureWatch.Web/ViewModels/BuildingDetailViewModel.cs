// StructureWatch.Web/ViewModels/BuildingDetailViewModel.cs
using StructureWatch.Core.Models;

namespace StructureWatch.Web.ViewModels;

public class BuildingDetailViewModel
{
    public string OsmId { get; set; } = default!;
    public string Name { get; set; } = "Unknown";
    public string Address { get; set; } = "N/A";
    public string BuildingType { get; set; } = "yes";
    public int Levels { get; set; }
    public string Height { get; set; } = "N/A";
    public double CalculatedHeight { get; set; }
    public Dictionary<string, string> Tags { get; set; } = new();
    public List<List<double>> Geometry { get; set; } = new();
    public string ColorHex { get; set; } = "#14B8A6";
    public AnalysisResult? Analysis { get; set; }
}
