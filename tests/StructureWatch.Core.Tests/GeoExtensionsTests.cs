using Xunit;
using StructureWatch.Core.Extensions;

namespace StructureWatch.Core.Tests;

public class GeoExtensionsTests
{
    [Fact]
    public void ToWebMercator_ReturnsCorrectValues_ForKnownCoordinate()
    {
        // NYC ~ (40.7128, -74.0060)
        var (x, y) = GeoExtensions.ToWebMercator(40.7128, -74.0060);
        // Expected ~ (-8238531, 4974656) meters (approximate EPSG:3857)
        Assert.True(Math.Abs(x - (-8238531)) < 1000);
        Assert.True(Math.Abs(y - 4974656) < 1000);
    }

    [Fact]
    public void ToWebMercator_OriginIsZeroZero_ForNullIsland()
    {
        var (x, y) = GeoExtensions.ToWebMercator(0, 0);
        Assert.Equal(0, x, 0.01);
        Assert.Equal(0, y, 0.01);
    }

    [Fact]
    public void ComputeHeight_UsesExplicitHeight_WhenAvailable()
    {
        var tags = new Dictionary<string, string> { ["height"] = "45.5" };
        var h = GeoExtensions.ComputeHeight(tags);
        Assert.Equal(45.5, h);
    }

    [Fact]
    public void ComputeHeight_UsesLevelsTimes3_5_WhenNoHeight()
    {
        var tags = new Dictionary<string, string> { ["building:levels"] = "10" };
        var h = GeoExtensions.ComputeHeight(tags);
        Assert.Equal(35.0, h);
    }

    [Fact]
    public void ComputeHeight_ReturnsDefault10_WhenNoTags()
    {
        var tags = new Dictionary<string, string>();
        var h = GeoExtensions.ComputeHeight(tags);
        Assert.Equal(10.0, h);
    }

    [Theory]
    [InlineData("residential", "#3B82F6")]
    [InlineData("apartments", "#3B82F6")]
    [InlineData("commercial", "#F97316")]
    [InlineData("industrial", "#6B7280")]
    [InlineData("office", "#8B5CF6")]
    [InlineData("school", "#EF4444")]
    [InlineData("yes", "#14B8A6")]
    [InlineData("unknown_type", "#14B8A6")]
    public void ColorByType_ReturnsCorrectColor(string type, string expected)
    {
        var color = GeoExtensions.ColorByType(type);
        Assert.Equal(expected, color);
    }
}
