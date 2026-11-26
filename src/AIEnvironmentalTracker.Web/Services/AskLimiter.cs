using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace AIEnvironmentalTracker.Web.Services;

/// <summary>Settings bound from the "AskLimits" configuration section.</summary>
public class AskLimitOptions
{
    public const string SectionName = "AskLimits";

    /// <summary>Gemini requests one user may send per UTC day. 0 or less turns the limit off.</summary>
    public int DailyRequestsPerUser { get; set; } = 30;

    /// <summary>Minimum seconds between two requests of the same user. 0 or less turns it off.</summary>
    public int MinSecondsBetweenRequests { get; set; } = 3;
}

/// <summary>Outcome of <see cref="AskLimiter.TryAcquire"/>.</summary>
public sealed record AskLimitResult(bool Allowed, string? Message, int RemainingToday)
{
    public static AskLimitResult Allow(int remaining) => new(true, null, remaining);
    public static AskLimitResult Deny(string message, int remaining) => new(false, message, remaining);
}

/// <summary>
/// Protects the shared Gemini quota. One API key serves every user and the free tier has a small daily budget,
/// so each user gets a daily allowance and a short pause between requests.
/// The counters live in memory: they reset when the app restarts and are not shared between several instances.
/// Every attempt that reaches Google counts, also when Google then reports an error, so failures cannot be used to hammer the API.
/// </summary>
public sealed class AskLimiter(IOptions<AskLimitOptions> options, TimeProvider clock)
{
    private sealed class Window
    {
        public DateOnly Day;
        public int Used;
        public DateTimeOffset Last = DateTimeOffset.MinValue;
    }

    private readonly AskLimitOptions settings = options.Value;
    private readonly ConcurrentDictionary<string, Window> windows = new();

    /// <summary>Reserves <paramref name="requests"/> requests for the user, or says why not.</summary>
    public AskLimitResult TryAcquire(string userId, int requests = 1)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("A signed-in user is required.", nameof(userId));
        if (requests <= 0)
            throw new ArgumentOutOfRangeException(nameof(requests), "The number of requests must be positive.");

        var now = clock.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var window = windows.GetOrAdd(userId, _ => new Window { Day = today });

        lock (window)
        {
            if (window.Day != today)
            {
                window.Day = today;
                window.Used = 0;
            }

            var daily = settings.DailyRequestsPerUser;
            var remaining = daily > 0 ? Math.Max(0, daily - window.Used) : int.MaxValue;

            if (settings.MinSecondsBetweenRequests > 0)
            {
                var wait = window.Last.AddSeconds(settings.MinSecondsBetweenRequests) - now;
                if (wait > TimeSpan.Zero)
                    return AskLimitResult.Deny($"Please wait {Math.Ceiling(wait.TotalSeconds):0} second(s) before the next request.", remaining);
            }

            if (daily > 0 && window.Used + requests > daily)
                return AskLimitResult.Deny(
                    remaining == 0
                        ? $"You have used your {daily} Gemini requests for today. The allowance resets at midnight UTC."
                        : $"This needs {requests} request(s) but only {remaining} are left today.",
                    remaining);

            window.Used += requests;
            window.Last = now;
            return AskLimitResult.Allow(daily > 0 ? daily - window.Used : int.MaxValue);
        }
    }
}
