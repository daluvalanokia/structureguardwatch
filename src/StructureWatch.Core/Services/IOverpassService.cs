// StructureWatch.Core/Services/IOverpassService.cs
using StructureWatch.Core.Models;

namespace StructureWatch.Core.Services;

public interface IOverpassService
{
    /// <summary>Fetch building footprints from OSM Overpass API for a viewport bbox.</summary>
    Task<List<BuildingFootprint>> FetchFootprintsAsync(double south, double west, double north, double east);
}
