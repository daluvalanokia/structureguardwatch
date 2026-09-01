// StructureWatch.Core/Services/IScanService.cs
using StructureWatch.Core.Models;

namespace StructureWatch.Core.Services;

public interface IScanService
{
    ScanState StartScan(string locationName, double lat, double lng, int zoom = 16);
    ScanState CompleteScan(int buildingCount);
    ScanState FailScan(string error);
    ScanState GetCurrentScan();
}
