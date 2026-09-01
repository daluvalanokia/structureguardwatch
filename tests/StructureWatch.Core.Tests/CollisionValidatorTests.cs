using StructureWatch.Core.Models;
using StructureWatch.Core.Services;

namespace StructureWatch.Core.Tests;

public class CollisionValidatorTests
{
    private readonly CollisionValidator _validator = new();

    private static BuildingFootprint MakeBuilding(string id, double minX, double maxX, double minY, double maxY)
        => new()
        {
            OsmId = id,
            MinX = minX, MaxX = maxX, MinY = minY, MaxY = maxY,
            Tags = new() { ["name"] = $"Building-{id}" },
            BuildingType = "yes",
            CalculatedHeight = 10,
            Geometry = new() { new() { minY, minX }, new() { maxY, maxX } }
        };

    [Fact]
    public void NoCollision_WhenGhostIsFarAway()
    {
        var selected = MakeBuilding("A", 0, 10, 0, 10);
        var others = new List<BuildingFootprint>
        {
            MakeBuilding("B", 100, 110, 100, 110),
            MakeBuilding("C", 200, 210, 200, 210),
        };

        var result = _validator.CheckAabb(selected, (50, 50), others);

        Assert.True(result.Clear);
        Assert.Empty(result.Interferences);
    }

    [Fact]
    public void DetectsCollision_WhenGhostOverlapsBuilding()
    {
        var selected = MakeBuilding("A", 0, 10, 0, 10);
        var others = new List<BuildingFootprint>
        {
            MakeBuilding("B", 12, 22, 12, 22),
        };

        // Move ghost by (5, 5) → AABB becomes (5,15)×(5,15), overlaps B's (12,22)×(12,22)
        var result = _validator.CheckAabb(selected, (5, 5), others);

        Assert.False(result.Clear);
        Assert.Single(result.Interferences);
        Assert.Equal("B", result.Interferences[0].OsmId);
        Assert.True(result.Interferences[0].OverlapAreaSqM > 0);
    }

    [Fact]
    public void SkipsSelfInCollisionCheck()
    {
        var selected = MakeBuilding("A", 0, 10, 0, 10);
        var others = new List<BuildingFootprint>
        {
            selected, // same building in list — should be skipped
            MakeBuilding("B", 5, 15, 5, 15),
        };

        var result = _validator.CheckAabb(selected, (0, 0), others);

        // A overlaps B at (0,0) drag (which is identity), B should be found
        Assert.False(result.Clear);
        Assert.Contains(result.Interferences, i => i.OsmId == "B");
        Assert.DoesNotContain(result.Interferences, i => i.OsmId == "A");
    }

    [Fact]
    public void SortsInterferencesByOverlapAreaDescending()
    {
        var selected = MakeBuilding("A", 0, 10, 0, 10);
        var others = new List<BuildingFootprint>
        {
            MakeBuilding("Small", 11, 13, 11, 13),   // small overlap
            MakeBuilding("Large", 5, 20, 5, 20),      // large overlap
        };

        var result = _validator.CheckAabb(selected, (3, 3), others);

        Assert.False(result.Clear);
        Assert.Equal(2, result.Interferences.Count);
        // Large overlap should come first
        Assert.True(result.Interferences[0].OverlapAreaSqM >= result.Interferences[1].OverlapAreaSqM);
    }

    [Fact]
    public void ReturnsClear_WhenOthersListIsEmpty()
    {
        var selected = MakeBuilding("A", 0, 10, 0, 10);
        var result = _validator.CheckAabb(selected, (50, 50), new List<BuildingFootprint>());

        Assert.True(result.Clear);
        Assert.Empty(result.Interferences);
    }
}
