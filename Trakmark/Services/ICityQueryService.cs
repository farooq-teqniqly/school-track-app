namespace Trakmark.Services;

/// <summary>
/// Read-side query service over the persisted <c>Cities</c> catalog. Returns projected
/// <see cref="CityListItem"/> read models (never the EF entity), supporting name search,
/// state filtering, and paging.
/// </summary>
/// <remarks>
/// This interface is the intended home of a future <c>(name, state)</c> existence check
/// used by the later Add Cities inline duplicate-guard change. That method will be added
/// here rather than in a separate read service (see <c>design.md</c>).
/// </remarks>
public interface ICityQueryService
{
    /// <summary>
    /// Returns a page of persisted cities ordered by state abbreviation then city name,
    /// narrowed by an optional case-insensitive name search and an optional exact
    /// state-abbreviation filter, together with the total matching count.
    /// </summary>
    /// <param name="searchTerm">
    /// Optional case-insensitive substring to match against the city name; when null or
    /// empty, no name filter is applied.
    /// </param>
    /// <param name="stateAbbreviation">
    /// Optional two-letter state abbreviation to filter by; when null or empty, cities in
    /// all states are returned.
    /// </param>
    /// <param name="pageNumber">The 1-based page number to return.</param>
    /// <param name="pageSize">The maximum number of cities to return for the page.</param>
    Task<CityPage> GetCitiesAsync(
        string? searchTerm,
        string? stateAbbreviation,
        int pageNumber,
        int pageSize
    );
}
