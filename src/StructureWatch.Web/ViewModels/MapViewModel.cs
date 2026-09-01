// StructureWatch.Web/ViewModels/MapViewModel.cs
namespace StructureWatch.Web.ViewModels;

public class MapViewModel
{
    public string Title { get; set; } = "StructureWatch — Real-Map Mode";
    public double DefaultLat { get; set; } = 40.7589;
    public double DefaultLng { get; set; } = -73.9851;
    public int DefaultZoom { get; set; } = 15;
    public string? CurrentLocation { get; set; }
    public int BuildingCount { get; set; }
    public bool ScanActive { get; set; }
    public string? ScanLocation { get; set; }
}
