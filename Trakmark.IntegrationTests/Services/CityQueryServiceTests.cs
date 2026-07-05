using Microsoft.EntityFrameworkCore;
using Trakmark.Data;
using Trakmark.Data.Entities;
using Trakmark.Services;

namespace Trakmark.IntegrationTests.Services;

/// <summary>
/// Integration tests for <see cref="CityQueryService"/> covering the View Cities read-side
/// spec scenarios: ordering, empty catalog, name search, state filter, paging, and
/// creator resolution (email with raw-identifier fallback).
/// </summary>
[Collection(IntegrationTestCollection.Name)]
public sealed class CityQueryServiceTests : IAsyncLifetime
{
    private readonly DatabaseFixture _fixture;

    /// <summary>Initializes a new instance of <see cref="CityQueryServiceTests"/>.</summary>
    public CityQueryServiceTests(DatabaseFixture fixture)
    {
        ArgumentNullException.ThrowIfNull(fixture);
        _fixture = fixture;
    }

    /// <inheritdoc/>
    public async Task InitializeAsync() => await _fixture.ResetAsync();

    /// <inheritdoc/>
    public Task DisposeAsync() => Task.CompletedTask;

    private static CityEntity City(string id, string name, string state, string createdBy) =>
        new()
        {
            CityId = id,
            Name = name,
            State = state,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedByUserId = createdBy,
        };

    private async Task SeedAsync(params CityEntity[] cities)
    {
        await using var context = _fixture.CreateContext();
        context.Cities.AddRange(cities);
        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetCitiesAsync_MultipleCities_OrdersByStateThenName()
    {
        // Arrange
        await SeedAsync(
            City("CTY-A1", "Springfield", "IL", "USR-SEED0001"),
            City("CTY-A2", "Chicago", "IL", "USR-SEED0001"),
            City("CTY-A3", "Austin", "TX", "USR-SEED0001"),
            City("CTY-A4", "Phoenix", "AZ", "USR-SEED0001")
        );

        await using var context = _fixture.CreateContext();
        var service = new CityQueryService(context);

        // Act
        var page = await service.GetCitiesAsync(null, null, 1, 25);

        // Assert
        var ordered = page.Items.Select(i => (i.StateAbbreviation, i.Name)).ToList();
        Assert.Equal(
            new[]
            {
                ("AZ", "Phoenix"),
                ("IL", "Chicago"),
                ("IL", "Springfield"),
                ("TX", "Austin"),
            },
            ordered
        );
    }

    [Fact]
    public async Task GetCitiesAsync_EmptyCatalog_ReturnsEmptyPageWithZeroTotal()
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var service = new CityQueryService(context);

        // Act
        var page = await service.GetCitiesAsync(null, null, 1, 25);

        // Assert
        Assert.Empty(page.Items);
        Assert.Equal(0, page.TotalCount);
    }

    [Fact]
    public async Task GetCitiesAsync_SearchTerm_FiltersByNameCaseInsensitively()
    {
        // Arrange
        await SeedAsync(
            City("CTY-B1", "Springfield", "IL", "USR-SEED0001"),
            City("CTY-B2", "Chicago", "IL", "USR-SEED0001"),
            City("CTY-B3", "Springdale", "AR", "USR-SEED0001")
        );

        await using var context = _fixture.CreateContext();
        var service = new CityQueryService(context);

        // Act
        var page = await service.GetCitiesAsync("spring", null, 1, 25);

        // Assert
        var names = page.Items.Select(i => i.Name).OrderBy(n => n).ToList();
        string[] expectedNames = ["Springdale", "Springfield"];
        Assert.Equal(expectedNames, names);
        Assert.Equal(2, page.TotalCount);
    }

    [Fact]
    public async Task GetCitiesAsync_SearchTermWithNoMatch_ReturnsEmpty()
    {
        // Arrange
        await SeedAsync(City("CTY-C1", "Chicago", "IL", "USR-SEED0001"));

        await using var context = _fixture.CreateContext();
        var service = new CityQueryService(context);

        // Act
        var page = await service.GetCitiesAsync("nonexistent", null, 1, 25);

        // Assert
        Assert.Empty(page.Items);
        Assert.Equal(0, page.TotalCount);
    }

    [Fact]
    public async Task GetCitiesAsync_StateFilter_NarrowsToThatState()
    {
        // Arrange
        await SeedAsync(
            City("CTY-D1", "Springfield", "IL", "USR-SEED0001"),
            City("CTY-D2", "Chicago", "IL", "USR-SEED0001"),
            City("CTY-D3", "Austin", "TX", "USR-SEED0001")
        );

        await using var context = _fixture.CreateContext();
        var service = new CityQueryService(context);

        // Act
        var page = await service.GetCitiesAsync(null, "IL", 1, 25);

        // Assert
        Assert.All(page.Items, i => Assert.Equal("IL", i.StateAbbreviation));
        Assert.Equal(2, page.TotalCount);
    }

    [Fact]
    public async Task GetCitiesAsync_LowercaseStateFilter_MatchesCaseInsensitively()
    {
        // Arrange
        await SeedAsync(
            City("CTY-DL1", "Springfield", "IL", "USR-SEED0001"),
            City("CTY-DL2", "Austin", "TX", "USR-SEED0001")
        );

        await using var context = _fixture.CreateContext();
        var service = new CityQueryService(context);

        // Act
        var page = await service.GetCitiesAsync(null, "il", 1, 25);

        // Assert
        Assert.All(page.Items, i => Assert.Equal("IL", i.StateAbbreviation));
        Assert.Equal(1, page.TotalCount);
    }

    [Fact]
    public async Task GetCitiesAsync_ClearedStateFilter_ReturnsAllStates()
    {
        // Arrange
        await SeedAsync(
            City("CTY-E1", "Springfield", "IL", "USR-SEED0001"),
            City("CTY-E2", "Austin", "TX", "USR-SEED0001")
        );

        await using var context = _fixture.CreateContext();
        var service = new CityQueryService(context);

        // Act
        var page = await service.GetCitiesAsync(null, null, 1, 25);

        // Assert
        var states = page.Items.Select(i => i.StateAbbreviation).Distinct().OrderBy(s => s).ToList();
        string[] expectedStates = ["IL", "TX"];
        Assert.Equal(expectedStates, states);
    }

    [Fact]
    public async Task GetCitiesAsync_MoreMatchesThanPageSize_ReturnsPageSizeAndTotalCount()
    {
        // Arrange
        var cities = Enumerable
            .Range(0, 30)
            .Select(i => City($"CTY-P{i:D4}", $"City{i:D4}", "IL", "USR-SEED0001"))
            .ToArray();
        await SeedAsync(cities);

        await using var context = _fixture.CreateContext();
        var service = new CityQueryService(context);

        // Act
        var page = await service.GetCitiesAsync(null, null, 1, 25);

        // Assert
        Assert.Equal(25, page.Items.Count);
        Assert.Equal(30, page.TotalCount);
    }

    [Fact]
    public async Task GetCitiesAsync_SecondPage_ReturnsRemainingItems()
    {
        // Arrange
        var cities = Enumerable
            .Range(0, 30)
            .Select(i => City($"CTY-Q{i:D4}", $"City{i:D4}", "IL", "USR-SEED0001"))
            .ToArray();
        await SeedAsync(cities);

        await using var context = _fixture.CreateContext();
        var service = new CityQueryService(context);

        // Act
        var page = await service.GetCitiesAsync(null, null, 2, 25);

        // Assert
        Assert.Equal(5, page.Items.Count);
        Assert.Equal(30, page.TotalCount);
    }

    [Fact]
    public async Task GetCitiesAsync_CreatorResolvesToIdentityUser_ShowsEmail()
    {
        // Arrange
        const string accountId = "account-guid-1";
        const string registeredUserId = "USR-CRTOR1";
        const string email = "creator@test.com";

        await using (var seedContext = _fixture.CreateContext())
        {
            seedContext.Users.Add(
                new ApplicationUser
                {
                    Id = accountId,
                    UserName = email,
                    NormalizedUserName = email.ToUpperInvariant(),
                    Email = email,
                    NormalizedEmail = email.ToUpperInvariant(),
                }
            );
            seedContext.RegisteredUsers.Add(
                new RegisteredUserEntity
                {
                    RegisteredUserId = registeredUserId,
                    AccountId = accountId,
                    CreatedAt = DateTimeOffset.UtcNow,
                    CreatedByUserId = registeredUserId,
                }
            );
            seedContext.Cities.Add(City("CTY-CR1", "Chicago", "IL", registeredUserId));
            await seedContext.SaveChangesAsync();
        }

        await using var context = _fixture.CreateContext();
        var service = new CityQueryService(context);

        // Act
        var page = await service.GetCitiesAsync(null, null, 1, 25);

        // Assert
        var item = Assert.Single(page.Items);
        Assert.Equal(email, item.CreatedBy);
    }

    [Fact]
    public async Task GetCitiesAsync_CreatorHasNoIdentityUser_FallsBackToRawIdentifier()
    {
        // Arrange
        const string registeredUserId = "USR-ORPHN1";
        await SeedAsync(City("CTY-OR1", "Chicago", "IL", registeredUserId));

        await using var context = _fixture.CreateContext();
        var service = new CityQueryService(context);

        // Act
        var page = await service.GetCitiesAsync(null, null, 1, 25);

        // Assert
        var item = Assert.Single(page.Items);
        Assert.Equal(registeredUserId, item.CreatedBy);
    }

    [Fact]
    public async Task GetCitiesAsync_UnknownStateAbbreviation_FallsBackToRawAbbreviation()
    {
        // Arrange
        await SeedAsync(City("CTY-ZZ1", "Nowhere", "ZZ", "USR-SEED0001"));

        await using var context = _fixture.CreateContext();
        var service = new CityQueryService(context);

        // Act
        var page = await service.GetCitiesAsync(null, null, 1, 25);

        // Assert
        var item = Assert.Single(page.Items);
        Assert.Equal("ZZ", item.StateName);
    }

    [Theory]
    [InlineData(0, 25)]
    [InlineData(-1, 25)]
    [InlineData(1, 0)]
    [InlineData(1, -5)]
    public async Task GetCitiesAsync_PageNumberOrSizeBelowOne_ThrowsArgumentOutOfRange(
        int pageNumber,
        int pageSize
    )
    {
        // Arrange
        await using var context = _fixture.CreateContext();
        var service = new CityQueryService(context);

        // Act / Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(
            () => service.GetCitiesAsync(null, null, pageNumber, pageSize)
        );
    }
}
