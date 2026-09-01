// StructureWatch.Agents/Tools/LoadCalculatorTool.cs
namespace StructureWatch.Agents.Tools;

/// <summary>
/// Tool bound to the TokenSaver agent for load capacity estimation.
/// Estimates based on building levels, type, and approximate footprint area.
/// </summary>
public static class LoadCalculatorTool
{
    public const string Name = "load_calculator";

    public const string Description = "Calculate load capacity based on building levels and type";

    public static object Definition => new
    {
        type = "function",
        function = new
        {
            name = Name,
            description = Description,
            parameters = new
            {
                type = "object",
                properties = new
                {
                    levels = new { type = "integer", description = "Number of building levels" },
                    buildingType = new { type = "string", description = "OSM building type (residential, commercial, etc.)" },
                    footprintAreaSqm = new { type = "number", description = "Building footprint area in square meters (optional)" }
                },
                required = new[] { "levels", "buildingType" }
            }
        }
    };

    public static string Execute(int levels, string buildingType, double? footprintAreaSqm = null)
    {
        // Base load capacity per level (kN/m²) — simplified engineering estimate
        double baseLoad = buildingType.ToLower() switch
        {
            "residential" or "apartments" or "house" => 2.0,
            "office" or "commercial" => 3.0,
            "retail" or "shop" => 4.0,
            "industrial" => 5.0,
            "school" or "hospital" => 3.5,
            _ => 2.5,
        };

        double totalLoad = baseLoad * levels;
        string capacity = totalLoad switch
        {
            < 10 => "Low capacity (< 10 kN/m²)",
            < 30 => "Moderate capacity (10-30 kN/m²)",
            < 60 => "High capacity (30-60 kN/m²)",
            _ => "Very high capacity (> 60 kN/m²)",
        };

        return $"{capacity} — Estimated {totalLoad:F1} kN/m² total for {levels} levels ({buildingType})";
    }
}
