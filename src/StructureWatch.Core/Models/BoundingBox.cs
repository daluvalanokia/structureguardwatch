// StructureWatch.Core/Models/BoundingBox.cs
namespace StructureWatch.Core.Models;

/// <summary>Axis-aligned bounding box in WebMercator (EPSG:3857) meters.</summary>
public class BoundingBox
{
    public double MinX { get; set; }
    public double MaxX { get; set; }
    public double MinY { get; set; }
    public double MaxY { get; set; }

    public double Width => MaxX - MinX;
    public double Height => MaxY - MinY;
    public double Area => Width * Height;

    public bool Intersects(BoundingBox other) =>
        MinX < other.MaxX && MaxX > other.MinX &&
        MinY < other.MaxY && MaxY > other.MinY;

    public double OverlapArea(BoundingBox other)
    {
        if (!Intersects(other)) return 0;
        double dx = Math.Min(MaxX, other.MaxX) - Math.Max(MinX, other.MinX);
        double dy = Math.Min(MaxY, other.MaxY) - Math.Max(MinY, other.MinY);
        return dx * dy;
    }

    public BoundingBox Translate(double dx, double dy) => new()
    {
        MinX = MinX + dx, MaxX = MaxX + dx,
        MinY = MinY + dy, MaxY = MaxY + dy,
    };

    public static BoundingBox FromFootprint(BuildingFootprint fp) => new()
    {
        MinX = fp.MinX, MaxX = fp.MaxX,
        MinY = fp.MinY, MaxY = fp.MaxY,
    };
}
