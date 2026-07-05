using Microsoft.Extensions.Logging;

namespace Trakmark.Components.Pages;

/// <summary>Source-generated log extensions for <see cref="ViewCities"/>.</summary>
internal static partial class ViewCitiesLog
{
    [LoggerMessage(Level = LogLevel.Warning, Message = "Failed to load the city catalog.")]
    public static partial void LogCityQueryFailed(this ILogger<ViewCities> logger, Exception exception);
}
