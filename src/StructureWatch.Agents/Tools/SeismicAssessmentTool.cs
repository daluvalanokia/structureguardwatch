// StructureWatch.Agents/Tools/SeismicAssessmentTool.cs
namespace StructureWatch.Agents.Tools;

/// <summary>
/// Tool bound to the TokenSaver agent for seismic risk assessment.
/// Provides a simplified risk rating based on building height and levels.
/// </summary>
public static class SeismicAssessmentTool
{
    public const string Name = "seismic_assessment";

    public const string Description = "Assess seismic risk based on building characteristics";

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
                    height = new { type = "number", description = "Building height in meters" },
                    levels = new { type = "integer", description = "Number of building levels" },
                    buildingType = new { type = "string", description = "OSM building type (optional)" }
                }
            }
        }
    };

    public static string Execute(double height, int levels, string? buildingType = null)
    {
        // Simplified seismic risk — taller buildings with more levels = higher risk
        string risk;
        if (height < 12 || levels <= 3)
            risk = "Low seismic risk — Low-rise structures typically perform well in seismic events";
        else if (height < 30 || levels <= 8)
            risk = "Moderate seismic risk — Mid-rise structures should comply with local seismic codes";
        else if (height < 60 || levels <= 15)
            risk = "High seismic risk — High-rise requires detailed seismic analysis and damping systems";
        else
            risk = "Very high seismic risk — Ultra high-rise requires advanced seismic isolation and dampers";

        return $"{risk} (Height: {height:F1}m, Levels: {levels}, Type: {buildingType ?? "unknown"})";
    }
}
