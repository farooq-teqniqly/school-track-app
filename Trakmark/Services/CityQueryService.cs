using Microsoft.EntityFrameworkCore;
using Trakmark.Data;
using Trakmark.Data.Entities;
using Trakmark.Helpers;

namespace Trakmark.Services;

/// <summary>
/// Read-side query service over the persisted <c>Cities</c> catalog. Projects
/// <see cref="CityEntity"/> to <see cref="CityListItem"/>, resolving the creator's
/// display identity via a read-time join across the Identity schema.
/// </summary>
public sealed class CityQueryService : ICityQueryService
{
    private readonly ApplicationDbContext _context;

    /// <summary>Initializes a new instance of <see cref="CityQueryService"/>.</summary>
    public CityQueryService(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc/>
    public async Task<CityPage> GetCitiesAsync(
        string? searchTerm,
        string? stateAbbreviation,
        int pageNumber,
        int pageSize
    )
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(pageNumber, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(pageSize, 1);

        var query = _context.Cities.AsQueryable();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            // EF Core cannot translate ToUpperInvariant() to SQL, so ToUpper() is used
            // here (matching SaveCitiesBatchService). SQL UPPER() is equivalent for the
            // ASCII-only U.S. city names this catalog contains.
            var upperSearch = searchTerm.ToUpper();
#pragma warning disable CA1862
            query = query.Where(c => c.Name.ToUpper().Contains(upperSearch));
#pragma warning restore CA1862
        }

        if (!string.IsNullOrEmpty(stateAbbreviation))
        {
            var upperState = stateAbbreviation.ToUpper();
#pragma warning disable CA1862
            query = query.Where(c => c.State.ToUpper() == upperState);
#pragma warning restore CA1862
        }

        var totalCount = await query.CountAsync();

        var rows = await query
            .OrderBy(c => c.State)
            .ThenBy(c => c.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new CityRow(
                c.Name,
                c.State,
                c.CreatedAt,
                _context
                    .RegisteredUsers.Where(ru => ru.RegisteredUserId == c.CreatedByUserId)
                    .Join(_context.Users, ru => ru.AccountId, u => u.Id, (ru, u) => u.Email)
                    .FirstOrDefault() ?? c.CreatedByUserId
            ))
            .ToListAsync();

        var items = rows.Select(r => new CityListItem(
                r.Name,
                r.State,
                StateHelper.GetByAbbreviation(r.State)?.Name ?? r.State,
                r.CreatedAt,
                r.CreatedBy
            ))
            .ToList();

        return new CityPage(items, totalCount);
    }

    /// <summary>
    /// SQL-translatable intermediate projection carrying the raw state abbreviation and
    /// the resolved creator identity before the in-memory state-name lookup is applied.
    /// </summary>
    private sealed record CityRow(
        string Name,
        string State,
        DateTimeOffset CreatedAt,
        string CreatedBy
    );
}
