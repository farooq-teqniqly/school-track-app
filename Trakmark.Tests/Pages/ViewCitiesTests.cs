using Bunit;
using Bunit.TestDoubles;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using NSubstitute;
using Trakmark.Components.Pages;
using Trakmark.Services;

namespace Trakmark.Tests.Pages;

/// <summary>bUnit tests for the <see cref="ViewCities"/> Blazor component.</summary>
public sealed class ViewCitiesTests : BunitContext
{
    private readonly ICityQueryService _mockQueryService;

    /// <summary>Initializes a new instance of <see cref="ViewCitiesTests"/>.</summary>
    public ViewCitiesTests()
    {
        _mockQueryService = Substitute.For<ICityQueryService>();

        AddAuthorization().SetAuthorized("admin@test.com").SetRoles("Admin");
        Services.AddLogging();
        Services.AddSingleton(_mockQueryService);
        SetRendererInfo(new RendererInfo("Server", isInteractive: true));
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    private void SetupPage(CityPage page) =>
        _mockQueryService
            .GetCitiesAsync(
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<int>(),
                Arg.Any<int>()
            )
            .Returns(page);

    private static CityPage PageOf(int totalCount, params CityListItem[] items) =>
        new(items, totalCount);

    [Fact]
    public void ViewCities_WithItems_RendersRows()
    {
        // Arrange
        SetupPage(
            PageOf(
                2,
                new CityListItem("Chicago", "IL", "Illinois", DateTimeOffset.UtcNow, "a@test.com"),
                new CityListItem("Austin", "TX", "Texas", DateTimeOffset.UtcNow, "b@test.com")
            )
        );

        // Act
        var cut = Render<ViewCities>();

        // Assert
        Assert.Equal(2, cut.FindAll(".city-row").Count);
        Assert.Contains("Chicago", cut.Markup);
        Assert.Contains("IL", cut.Markup);
        Assert.Contains("a@test.com", cut.Markup);
    }

    [Fact]
    public void ViewCities_WithItems_RendersCreatedAtAsUtcTimeElement()
    {
        // Arrange
        var createdAt = new DateTimeOffset(2026, 7, 5, 12, 30, 0, TimeSpan.Zero);
        SetupPage(PageOf(1, new CityListItem("Chicago", "IL", "Illinois", createdAt, "a@test.com")));

        // Act
        var cut = Render<ViewCities>();

        // Assert
        var time = cut.Find(".city-row time");
        Assert.Equal(createdAt.ToString("O"), time.GetAttribute("data-utc"));
    }

    [Fact]
    public void ViewCities_JsInteropDisconnected_RendersWithoutThrowing()
    {
        // Arrange
        SetupPage(
            PageOf(1, new CityListItem("Chicago", "IL", "Illinois", DateTimeOffset.UtcNow, "a@test.com"))
        );
        JSInterop.SetupVoid("trakmark.formatLocalTimes")
            .SetException(new JSDisconnectedException("circuit gone"));

        // Act
        var cut = Render<ViewCities>();

        // Assert
        Assert.Single(cut.FindAll(".city-row"));
        Assert.Empty(cut.FindAll("#error-alert"));
    }

    [Fact]
    public void ViewCities_EmptyResult_ShowsEmptyMessageAndNoRows()
    {
        // Arrange
        SetupPage(PageOf(0));

        // Act
        var cut = Render<ViewCities>();

        // Assert
        Assert.Empty(cut.FindAll(".city-row"));
        Assert.NotEmpty(cut.FindAll("#empty-message"));
    }

    [Fact]
    public void ViewCities_QueryServiceThrows_ShowsErrorAndNoRows()
    {
        // Arrange
        _mockQueryService
            .GetCitiesAsync(
                Arg.Any<string?>(),
                Arg.Any<string?>(),
                Arg.Any<int>(),
                Arg.Any<int>()
            )
            .Returns<Task<CityPage>>(_ => throw new InvalidOperationException("boom"));

        // Act
        var cut = Render<ViewCities>();

        // Assert
        Assert.NotEmpty(cut.FindAll("#error-alert"));
        Assert.Empty(cut.FindAll(".city-row"));
    }

    [Fact]
    public async Task ViewCities_SearchTermEntered_CallsServiceWithTermAndResetsToPageOne()
    {
        // Arrange
        SetupPage(PageOf(50, new CityListItem("Chicago", "IL", "Illinois", DateTimeOffset.UtcNow, "a@test.com")));
        var cut = Render<ViewCities>();
        await cut.Find("#next-page-btn").ClickAsync(new MouseEventArgs());
        _mockQueryService.ClearReceivedCalls();

        // Act
        await cut.Find("#city-search").ChangeAsync(new ChangeEventArgs { Value = "spring" });

        // Assert
        await _mockQueryService
            .Received(1)
            .GetCitiesAsync("spring", Arg.Any<string?>(), 1, Arg.Any<int>());
    }

    [Fact]
    public async Task ViewCities_StateSelected_CallsServiceWithAbbreviationAndResetsToPageOne()
    {
        // Arrange
        SetupPage(PageOf(50, new CityListItem("Chicago", "IL", "Illinois", DateTimeOffset.UtcNow, "a@test.com")));
        var cut = Render<ViewCities>();
        await cut.Find("#next-page-btn").ClickAsync(new MouseEventArgs());
        _mockQueryService.ClearReceivedCalls();

        // Act
        await cut.Find("#state-filter").ChangeAsync(new ChangeEventArgs { Value = "IL" });

        // Assert
        await _mockQueryService
            .Received(1)
            .GetCitiesAsync(Arg.Any<string?>(), "IL", 1, Arg.Any<int>());
    }

    [Fact]
    public async Task ViewCities_NextPageClicked_AdvancesPageNumberAndCallsService()
    {
        // Arrange
        SetupPage(PageOf(50, new CityListItem("Chicago", "IL", "Illinois", DateTimeOffset.UtcNow, "a@test.com")));
        var cut = Render<ViewCities>();
        _mockQueryService.ClearReceivedCalls();

        // Act
        await cut.Find("#next-page-btn").ClickAsync(new MouseEventArgs());

        // Assert
        await _mockQueryService
            .Received(1)
            .GetCitiesAsync(Arg.Any<string?>(), Arg.Any<string?>(), 2, Arg.Any<int>());
    }
}
