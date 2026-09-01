// StructureWatch.Core/Models/CollisionResult.cs
namespace StructureWatch.Core.Models;

public record CollisionEntry(string OsmId, string Name, double OverlapAreaSqM);
public record CollisionResult(bool Clear, List<CollisionEntry> Interferences);
