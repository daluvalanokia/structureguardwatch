// StructureWatch.Core/Extensions/GeoExtensions.cs
using System.Globalization;

namespace StructureWatch.Core.Extensions;

public static class GeoExtensions
{
    private const double R = 6378137.0; // Earth radius (WGS84 semi-major axis)
    private const double OriginShift = Math.PI * R;
    private const double DefaultHeight = 10.0;
    private const double MetersPerLevel = 3.5;

    /// <summary>WGS84 lat/lng → WebMercator x/y (EPSG:3857) in meters.</summary>
    public static (double x, double y) ToWebMercator(double lat, double lng)
    {
        double x = lng * OriginShift / 180.0;
        double y = Math.Log(Math.Tan((90.0 + lat) * Math.PI / 360.0)) / (Math.PI / 180.0);
        y = y * OriginShift / 180.0;
        return (x, y);
    }

    /// <summary>Compute building height from OSM tags: explicit height > levels×3.5 > 10m default.</summary>
    public static double ComputeHeight(IReadOnlyDictionary<string, string> tags)
    {
        if (tags.TryGetValue("height", out var h) && double.TryParse(h, NumberStyles.Any, CultureInfo.InvariantCulture, out var height))
            return height;

        if (tags.TryGetValue("building:levels", out var lv) && int.TryParse(lv, out var levels))
            return levels * MetersPerLevel;

        return DefaultHeight;
    }

    /// <summary>Color by building type for 3D rendering.</summary>
    public static string ColorByType(string buildingType) => buildingType switch
    {
        "residential" or "apartments" or "house" => "#3B82F6",   // blue
        "commercial" or "retail" or "shop"       => "#F97316",   // orange
        "industrial"                              => "#6B7280",   // gray
        "office"                                  => "#8B5CF6",   // purple
        "school" or "hospital" or "public"       => "#EF4444",   // red
        _                                         => "#14B8A6",   // teal (default)
    };
}
