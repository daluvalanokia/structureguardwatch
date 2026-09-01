using Xunit;
using StructureWatch.Core.Models;
using StructureWatch.Core.Services;

namespace StructureWatch.Core.Tests;

public class ScanServiceTests
{
    [Fact]
    public void StartScan_SetsScanningStatus()
    {
        var service = new ScanService();
        var state = service.StartScan("Manhattan, NY", 40.7589, -73.9851, 16);

        Assert.Equal(ScanStatus.Scanning, state.Status);
        Assert.Equal("Manhattan, NY", state.LocationName);
        Assert.Equal(40.7589, state.Lat);
        Assert.Equal(-73.9851, state.Lng);
        Assert.Equal(16, state.Zoom);
        Assert.NotNull(state.StartedAt);
    }

    [Fact]
    public void CompleteScan_SetsCompleteStatusAndBuildingCount()
    {
        var service = new ScanService();
        service.StartScan("Chicago, IL", 41.8781, -87.6298);
        var state = service.CompleteScan(142);

        Assert.Equal(ScanStatus.Complete, state.Status);
        Assert.Equal(142, state.BuildingCount);
        Assert.NotNull(state.CompletedAt);
    }

    [Fact]
    public void FailScan_SetsFailedStatus()
    {
        var service = new ScanService();
        service.StartScan("Boston, MA", 42.3601, -71.0589);
        var state = service.FailScan("Overpass timeout");

        Assert.Equal(ScanStatus.Failed, state.Status);
        Assert.NotNull(state.CompletedAt);
    }

    [Fact]
    public void GetCurrentScan_ReturnsLatestState()
    {
        var service = new ScanService();
        service.StartScan("Seattle, WA", 47.6062, -122.3321);
        var current = service.GetCurrentScan();

        Assert.Equal("Seattle, WA", current.LocationName);
        Assert.Equal(ScanStatus.Scanning, current.Status);
    }
}
