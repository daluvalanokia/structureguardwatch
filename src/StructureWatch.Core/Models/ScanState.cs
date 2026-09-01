// StructureWatch.Core/Models/ScanState.cs
namespace StructureWatch.Core.Models;

/// <summary>Represents the scan animation state when a location is selected.</summary>
public class ScanState
{
    public string LocationName { get; set; } = string.Empty;
    public double Lat { get; set; }
    public double Lng { get; set; }
    public int Zoom { get; set; } = 16;
    public ScanStatus Status { get; set; } = ScanStatus.Idle;
    public int BuildingCount { get; set; }
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public enum ScanStatus
{
    Idle,
    Scanning,
    Complete,
    Failed
}
