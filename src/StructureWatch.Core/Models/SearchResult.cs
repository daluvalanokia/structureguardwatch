// StructureWatch.Core/Models/SearchResult.cs
namespace StructureWatch.Core.Models;

/// <summary>Address search result from Nominatim.</summary>
public record SearchResult(
    string DisplayName,
    double Lat,
    double Lon,
    string City,
    string State,
    string Country
);
