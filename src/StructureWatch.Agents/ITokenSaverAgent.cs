// StructureWatch.Agents/ITokenSaverAgent.cs
using StructureWatch.Agents.Dtos;

namespace StructureWatch.Agents;

public interface ITokenSaverAgent
{
    Task<AnalysisResponse> RunAnalysisAsync(string osmId, Dictionary<string, string> tags);
}
