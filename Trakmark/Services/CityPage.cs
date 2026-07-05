namespace Trakmark.Services;

/// <summary>
/// A single page of city read-model projections together with the total count of
/// cities matching the current search and state filter (across all pages).
/// </summary>
/// <param name="Items">The city projections on the requested page.</param>
/// <param name="TotalCount">The total number of cities matching the current filter.</param>
public sealed record CityPage(IReadOnlyList<CityListItem> Items, int TotalCount);
