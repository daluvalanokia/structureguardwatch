// StructureWatch.Agents/Tools/OccupancyClassifierTool.cs
namespace StructureWatch.Agents.Tools;

/// <summary>
/// Tool bound to the TokenSaver agent for occupancy classification.
/// Maps OSM building types to standard occupancy categories (IBC/ASCE 7).
/// </summary>
public static class OccupancyClassifierTool
{
    public const string Name = "occupancy_classifier";

    public const string Description = "Classify occupancy based on building type";

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
                    buildingType = new { type = "string", description = "OSM building tag value" }
                },
                required = new[] { "buildingType" }
            }
        }
    };

    public static string Execute(string buildingType)
    {
        return buildingType.ToLower() switch
        {
            "residential" or "apartments" or "house" or "detached"
                => "Residential (R-2) — Multi-family or single-family dwelling",
            "commercial" or "retail" or "shop" or "mall"
                => "Mercantile (M) — Retail sales of merchandise",
            "office"
                => "Business (B) — Office, professional services",
            "industrial" or "warehouse" or "factory"
                => "Industrial (F) — Fabrication, manufacturing, storage",
            "school" or "college" or "university"
                => "Educational (E) — K-12 or higher education",
            "hospital" or "clinic"
                => "Institutional (I-2) — Medical care, 24-hr occupancy",
            "public" or "civic" or "government"
                => "Assembly (A) — Public gathering space",
            "church" or "place_of_worship"
                => "Assembly (A-3) — Places of worship",
            "hotel" or "motel" or "guest_house"
                => "Residential (R-1) — Transient lodging",
            "parking"
                => "Utility/Miscellaneous (U) — Parking structures",
            _ => "Unknown — No standard classification for this building type",
        };
    }
}
