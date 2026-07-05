namespace Trakmark.Services;

/// <summary>
/// A read-model projection of a persisted city for display in the View Cities page.
/// Carries only the fields the UI needs; the EF <c>CityEntity</c> never leaves the
/// query service boundary. Pure data carrier with structural equality.
/// </summary>
/// <param name="Name">The city's name.</param>
/// <param name="StateAbbreviation">The two-letter abbreviation of the city's state.</param>
/// <param name="StateName">The full display name of the city's state.</param>
/// <param name="CreatedAt">The timestamp at which the city was persisted.</param>
/// <param name="CreatedBy">
/// The resolved creator display identity: the creating user's email when their account
/// resolves to one, otherwise the raw <c>RegisteredUserId</c> fallback.
/// </param>
public sealed record CityListItem(
    string Name,
    string StateAbbreviation,
    string StateName,
    DateTimeOffset CreatedAt,
    string CreatedBy
);
