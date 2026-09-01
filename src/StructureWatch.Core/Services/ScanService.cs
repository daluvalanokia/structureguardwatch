// StructureWatch.Core/Services/ScanService.cs
using StructureWatch.Core.Models;

namespace StructureWatch.Core.Services;

/// <summary>
/// Manages the scan animation lifecycle when a user selects a location from search.
/// In the .NET solution this drives the scan state server-side; the frontend
/// mirrors it with the radar CSS animation.
/// </summary>
public class ScanService : IScanService
{
    private ScanState _current = new();

    public ScanState StartScan(string locationName, double lat, double lng, int zoom = 16)
    {
        _current = new ScanState
        {
            LocationName = locationName,
            Lat = lat,
            Lng = lng,
            Zoom = zoom,
            Status = ScanStatus.Scanning,
            StartedAt = DateTime.UtcNow,
        };
        return _current;
    }

    public ScanState CompleteScan(int buildingCount)
    {
        _current.Status = ScanStatus.Complete;
        _current.BuildingCount = buildingCount;
        _current.CompletedAt = DateTime.UtcNow;
        return _current;
    }

    public ScanState FailScan(string error)
    {
        _current.Status = ScanStatus.Failed;
        _current.CompletedAt = DateTime.UtcNow;
        return _current;
    }

    public ScanState GetCurrentScan() => _current;
}
