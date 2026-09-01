// StructureWatch.Core/Services/INominatimService.cs
using StructureWatch.Core.Models;

namespace StructureWatch.Core.Services;

public interface INominatimService
{
    /// <summary>Search for addresses/cities/states via OSM Nominatim API.</summary>
    Task<List<SearchResult>> SearchAsync(string query, int limit = 5);
}
