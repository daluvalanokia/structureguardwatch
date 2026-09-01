// StructureWatch.Web/ViewModels/CollisionViewModel.cs
using StructureWatch.Core.Models;

namespace StructureWatch.Web.ViewModels;

public class CollisionViewModel
{
    public string SelectedOsmId { get; set; } = default!;
    public string SelectedName { get; set; } = "Unknown";
    public double DragDx { get; set; }
    public double DragDy { get; set; }
    public bool Clear { get; set; }
    public List<CollisionEntry> Interferences { get; set; } = new();
    public int TotalBuildingsChecked { get; set; }
}
