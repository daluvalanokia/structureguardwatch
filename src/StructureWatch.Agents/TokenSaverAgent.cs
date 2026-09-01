// StructureWatch.Agents/TokenSaverAgent.cs
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StructureWatch.Agents.Dtos;
using StructureWatch.Agents.Tools;

namespace StructureWatch.Agents;

public class TokenSaverAgent : ITokenSaverAgent
{
    private readonly HttpClient _http;
    private readonly string _agentAppId;
    private readonly string _model;
    private readonly int _maxTokens;
    private readonly ILogger<TokenSaverAgent> _logger;

    public TokenSaverAgent(HttpClient http, IConfiguration config, ILogger<TokenSaverAgent> logger)
    {
        _http = http;
        _agentAppId = config["TokenSaver:AgentAppId"]
            ?? throw new InvalidOperationException("TokenSaver:AgentAppId not configured");
        _model = config["TokenSaver:Model"] ?? "gpt-4o";
        _maxTokens = int.TryParse(config["TokenSaver:MaxTokens"], out var t) ? t : 2000;
        _logger = logger;
    }

    public async Task<AnalysisResponse> RunAnalysisAsync(string osmId, Dictionary<string, string> tags)
    {
        _logger.LogInformation("Running TokenSaver analysis for building {OsmId}", osmId);

        string prompt = BuildPrompt(osmId, tags);

        var requestBody = new
        {
            model = _model,
            messages = new[]
            {
                new { role = "system", content = SystemPrompt },
                new { role = "user", content = prompt }
            },
            tools = new object[]
            {
                LoadCalculatorTool.Definition,
                SeismicAssessmentTool.Definition,
                OccupancyClassifierTool.Definition,
            },
            tool_choice = "auto",
            max_tokens = _maxTokens,
            response_format = new { type = "json_object" }
        };

        var response = await _http.PostAsJsonAsync("/v1/chat/completions", requestBody);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var content = json!.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString()!;

        // Check for tool calls first
        var messageEl = json.RootElement.GetProperty("choices")[0].GetProperty("message");
        if (messageEl.TryGetProperty("tool_calls", out var toolCallsEl))
        {
            // Execute each tool and incorporate results
            foreach (var tc in toolCallsEl.EnumerateArray())
            {
                var fnName = tc.GetProperty("function").GetProperty("name").GetString();
                var args = tc.GetProperty("function").GetProperty("arguments").GetString();

                string toolResult = ExecuteTool(fnName!, args!, tags);
                _logger.LogInformation("Tool {Tool} returned: {Result}", fnName, toolResult);
            }
        }

        var result = JsonSerializer.Deserialize<AnalysisResponse>(content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

        _logger.LogInformation("Analysis complete for {OsmId}: {Summary}", osmId, result.Summary);
        return result;
    }

    private static string BuildPrompt(string osmId, Dictionary<string, string> tags)
    {
        var tagLines = tags.Select(kv => $"  {kv.Key}: {kv.Value}");
        return $$"""
            Analyze the following building from OpenStreetMap and provide a
            physical property assessment. Return JSON with fields:
            loadCapacity, structuralIntegrity, seismicRisk, windLoad,
            occupancyClass, summary, riskFactors (array of strings).

            Building OSM ID: {{osmId}}
            OSM Tags:
            {{string.Join("\n", tagLines)}}
            """;
    }

    private const string SystemPrompt = """
        You are a structural engineering AI assistant. Given building metadata
        from OpenStreetMap, provide a realistic physical property analysis.
        Use available tools (load_calculator, seismic_assessment, occupancy_classifier)
        when applicable. Always return a structured JSON response with all fields filled.
        """;

    private static string ExecuteTool(string name, string args, Dictionary<string, string> tags)
    {
        // Parse args (simple JSON)
        using var argsDoc = JsonDocument.Parse(args);
        var argsRoot = argsDoc.RootElement;

        return name switch
        {
            LoadCalculatorTool.Name => LoadCalculatorTool.Execute(
                argsRoot.GetProperty("levels").GetInt32(),
                argsRoot.GetProperty("buildingType").GetString()!,
                argsRoot.TryGetProperty("footprintAreaSqm", out var fa) ? fa.GetDouble() : null),

            SeismicAssessmentTool.Name => SeismicAssessmentTool.Execute(
                argsRoot.GetProperty("height").GetDouble(),
                argsRoot.GetProperty("levels").GetInt32(),
                argsRoot.TryGetProperty("buildingType", out var bt) ? bt.GetString() : null),

            OccupancyClassifierTool.Name => OccupancyClassifierTool.Execute(
                argsRoot.GetProperty("buildingType").GetString()!),

            _ => $"Unknown tool: {name}",
        };
    }
}
