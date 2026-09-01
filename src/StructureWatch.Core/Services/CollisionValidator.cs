// StructureWatch.Core/Services/CollisionValidator.cs
using StructureWatch.Core.Models;

namespace StructureWatch.Core.Services;

public class CollisionValidator
{
    /// <summary>
    /// Checks the ghost box (selected building AABB translated by drag vector)
    /// against all other loaded footprints using AABB overlap.
    /// </summary>
    public CollisionResult CheckAabb(
        BuildingFootprint selected,
        (double dx, double dy) dragVector,
        IReadOnlyList<BuildingFootprint> others)
    {
        var selectedBox = BoundingBox.FromFootprint(selected);
        var ghostBox = selectedBox.Translate(dragVector.dx, dragVector.dy);

        var interferences = new List<CollisionEntry>();

        foreach (var b in others)
        {
            if (b.OsmId == selected.OsmId) continue;

            var bBox = BoundingBox.FromFootprint(b);
            if (!ghostBox.Intersects(bBox)) continue;

            double overlapArea = ghostBox.OverlapArea(bBox);
            interferences.Add(new CollisionEntry(b.OsmId, b.Name, overlapArea));
        }

        var sorted = interferences.OrderByDescending(i => i.OverlapAreaSqM).ToList();
        return new CollisionResult(Clear: sorted.Count == 0, Interferences: sorted);
    }
}
