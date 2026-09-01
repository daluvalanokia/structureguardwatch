// StructureWatch.Agents/Dtos/AnalysisResponse.cs
using System.Text.Json.Serialization;

namespace StructureWatch.Agents.Dtos;

public class AnalysisResponse
{
    [JsonPropertyName("loadCapacity")]
    public string LoadCapacity { get; set; } = string.Empty;

    [JsonPropertyName("structuralIntegrity")]
    public string StructuralIntegrity { get; set; } = string.Empty;

    [JsonPropertyName("seismicRisk")]
    public string SeismicRisk { get; set; } = string.Empty;

    [JsonPropertyName("windLoad")]
    public string WindLoad { get; set; } = string.Empty;

    [JsonPropertyName("occupancyClass")]
    public string OccupancyClass { get; set; } = string.Empty;

    [JsonPropertyName("summary")]
    public string Summary { get; set; } = string.Empty;

    [JsonPropertyName("riskFactors")]
    public List<string> RiskFactors { get; set; } = new();
}
