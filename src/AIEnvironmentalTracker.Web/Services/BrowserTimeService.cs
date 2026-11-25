using Microsoft.JSInterop;

namespace AIEnvironmentalTracker.Web.Services;

/// <summary>
/// Converts UTC timestamps from the database to the time zone of the user's browser.
/// Registered as scoped, so each Blazor circuit (browser tab) keeps its own zone.
/// </summary>
public class BrowserTimeService(IJSRuntime js)
{
    private TimeZoneInfo? zone;

    public bool IsLoaded => zone is not null;

    /// <summary>The time zone in use, for example "Asia/Jakarta".</summary>
    public string ZoneId => (zone ?? TimeZoneInfo.Local).Id;

    /// <summary>Reads the browser time zone. Call from OnAfterRenderAsync, when JS interop is available.</summary>
    public async Task EnsureLoadedAsync()
    {
        if (zone is not null) return;

        try
        {
            var id = await js.InvokeAsync<string>("oxygenai.getTimeZone");
            zone = !string.IsNullOrWhiteSpace(id) && TimeZoneInfo.TryFindSystemTimeZoneById(id, out var found)
                ? found
                : TimeZoneInfo.Local;
        }
        catch (Exception ex) when (ex is JSException or InvalidOperationException or TaskCanceledException)
        {
            zone = TimeZoneInfo.Local;
        }
    }

    public DateTime ToLocal(DateTime utc)
        => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), zone ?? TimeZoneInfo.Local);
}
