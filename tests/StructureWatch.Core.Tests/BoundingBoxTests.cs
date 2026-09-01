using Xunit;
using StructureWatch.Core.Models;

namespace StructureWatch.Core.Tests;

public class BoundingBoxTests
{
    [Fact]
    public void Intersects_ReturnsTrue_ForOverlappingBoxes()
    {
        var a = new BoundingBox { MinX = 0, MaxX = 10, MinY = 0, MaxY = 10 };
        var b = new BoundingBox { MinX = 5, MaxX = 15, MinY = 5, MaxY = 15 };

        Assert.True(a.Intersects(b));
    }

    [Fact]
    public void Intersects_ReturnsFalse_ForNonOverlappingBoxes()
    {
        var a = new BoundingBox { MinX = 0, MaxX = 10, MinY = 0, MaxY = 10 };
        var b = new BoundingBox { MinX = 20, MaxX = 30, MinY = 20, MaxY = 30 };

        Assert.False(a.Intersects(b));
    }

    [Fact]
    public void OverlapArea_ReturnsCorrectValue_ForPartialOverlap()
    {
        var a = new BoundingBox { MinX = 0, MaxX = 10, MinY = 0, MaxY = 10 };
        var b = new BoundingBox { MinX = 5, MaxX = 15, MinY = 5, MaxY = 15 };

        double area = a.OverlapArea(b);
        Assert.Equal(25.0, area); // 5×5 overlap
    }

    [Fact]
    public void OverlapArea_ReturnsZero_ForNonOverlappingBoxes()
    {
        var a = new BoundingBox { MinX = 0, MaxX = 10, MinY = 0, MaxY = 10 };
        var b = new BoundingBox { MinX = 100, MaxX = 110, MinY = 100, MaxY = 110 };

        Assert.Equal(0.0, a.OverlapArea(b));
    }

    [Fact]
    public void Translate_MovesBoxByVector()
    {
        var box = new BoundingBox { MinX = 0, MaxX = 10, MinY = 0, MaxY = 10 };
        var moved = box.Translate(50, 50);

        Assert.Equal(50, moved.MinX);
        Assert.Equal(60, moved.MaxX);
        Assert.Equal(50, moved.MinY);
        Assert.Equal(60, moved.MaxY);
    }

    [Fact]
    public void FromFootprint_CreatesBoxFromBuilding()
    {
        var fp = new BuildingFootprint { MinX = 100, MaxX = 200, MinY = 300, MaxY = 400 };
        var box = BoundingBox.FromFootprint(fp);

        Assert.Equal(100, box.MinX);
        Assert.Equal(200, box.MaxX);
        Assert.Equal(300, box.MinY);
        Assert.Equal(400, box.MaxY);
    }
}
