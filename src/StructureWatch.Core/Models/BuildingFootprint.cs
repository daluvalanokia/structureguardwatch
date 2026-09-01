// StructureWatch.Core/Models/BuildingFootprint.cs
namespace StructureWatch.Core.Models;

public class BuildingFootprint
{
    public string OsmId { get; set; } = default!;
    public string OsmType { get; set; } = default!;   // "way" | "relation"
    public List<List<double>> Geometry { get; set; } = new(); // [[lat, lng], ...]
    public Dictionary<string, string> Tags { get; set; } = new();
    public double CalculatedHeight { get; set; }
    public string BuildingType { get; set; } = "yes";

    // Computed AABB in WebMercator (EPSG:3857) meters
    public double MinX { get; set; }
    public double MaxX { get; set; }
    public double MinY { get; set; }
    public double MaxY { get; set; }

    public string Name => Tags.GetValueOrDefault("name") ?? "Unknown";
    public string Address
    {
        get
        {
            var num = Tags.GetValueOrDefault("addr:housenumber");
            var street = Tags.GetValueOrDefault("addr:street");
            if (num is not null && street is not null) return $"{num} {street}";
            if (street is not null) return street;
            return "N/A";
        }
    }
    public int Levels => int.TryParse(Tags.GetValueOrDefault("building:levels"), out var v) ? v : 0;
    public string Height => Tags.GetValueOrDefault("height") ?? $"{CalculatedHeight:F1} m (calc)";
}
