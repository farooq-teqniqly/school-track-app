using Bunit;
using Bunit.TestDoubles;
using Trakmark.Components.Layout;

namespace Trakmark.Tests.Layout;

/// <summary>bUnit tests for <see cref="TopNavMenu"/>.</summary>
public sealed class TopNavMenuTests : BunitContext
{
    /// <summary>Provides serializable auth scenario names and expected Add Cities link counts.</summary>
    public static TheoryData<string, int> AddCitiesLinkVisibilityData() =>
        new()
        {
            { "admin", 1 },
            { "user", 0 },
            { "anon", 0 },
        };

    private static void SetupAuth(BunitAuthorizationContext auth, string scenario)
    {
        switch (scenario)
        {
            case "admin":
                auth.SetAuthorized("admin@test.com").SetRoles("Admin");
                break;
            case "user":
                auth.SetAuthorized("user@test.com");
                break;
        }
    }

    [Theory]
    [MemberData(nameof(AddCitiesLinkVisibilityData))]
    public void AddCitiesLink_AuthScenario_ControlsVisibility(string scenario, int expectedCount)
    {
        // Arrange
        var auth = AddAuthorization();
        SetupAuth(auth, scenario);

        // Act
        var cut = Render<TopNavMenu>();

        // Assert
        Assert.Equal(expectedCount, cut.FindAll("a[href='admin/cities/add']").Count);
    }

    [Theory]
    [MemberData(nameof(AddCitiesLinkVisibilityData))]
    public void ViewCitiesLink_AuthScenario_ControlsVisibility(string scenario, int expectedCount)
    {
        // Arrange
        var auth = AddAuthorization();
        SetupAuth(auth, scenario);

        // Act
        var cut = Render<TopNavMenu>();

        // Assert
        Assert.Equal(expectedCount, cut.FindAll("a[href='admin/cities']").Count);
    }
}
