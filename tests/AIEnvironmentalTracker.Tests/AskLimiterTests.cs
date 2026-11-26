using AIEnvironmentalTracker.Web.Services;
using Microsoft.Extensions.Options;

namespace AIEnvironmentalTracker.Tests;

public class AskLimiterTests
{
    private static (AskLimiter Limiter, FakeClock Clock) Create(int daily = 3, int gapSeconds = 0)
    {
        var clock = new FakeClock(new DateTimeOffset(2026, 11, 25, 10, 0, 0, TimeSpan.Zero));
        var options = Options.Create(new AskLimitOptions { DailyRequestsPerUser = daily, MinSecondsBetweenRequests = gapSeconds });
        return (new AskLimiter(options, clock), clock);
    }

    [Fact]
    public void AllowsUpToTheDailyLimit_ThenRefuses()
    {
        var (limiter, _) = Create(daily: 3);
        Assert.True(limiter.TryAcquire("u").Allowed);
        Assert.True(limiter.TryAcquire("u").Allowed);
        var third = limiter.TryAcquire("u");
        Assert.True(third.Allowed);
        Assert.Equal(0, third.RemainingToday);

        var fourth = limiter.TryAcquire("u");
        Assert.False(fourth.Allowed);
        Assert.Contains("resets at midnight UTC", fourth.Message);
    }

    [Fact]
    public void LimitIsPerUser()
    {
        var (limiter, _) = Create(daily: 1);
        Assert.True(limiter.TryAcquire("a").Allowed);
        Assert.False(limiter.TryAcquire("a").Allowed);
        Assert.True(limiter.TryAcquire("b").Allowed);
    }

    [Fact]
    public void AllowanceResetsOnTheNextUtcDay()
    {
        var (limiter, clock) = Create(daily: 1);
        Assert.True(limiter.TryAcquire("u").Allowed);
        Assert.False(limiter.TryAcquire("u").Allowed);
        clock.Advance(TimeSpan.FromHours(15)); // 01:00 the next day
        Assert.True(limiter.TryAcquire("u").Allowed);
    }

    [Fact]
    public void MinimumGap_IsEnforcedAndExpires()
    {
        var (limiter, clock) = Create(daily: 10, gapSeconds: 5);
        Assert.True(limiter.TryAcquire("u").Allowed);

        clock.Advance(TimeSpan.FromSeconds(2));
        var tooSoon = limiter.TryAcquire("u");
        Assert.False(tooSoon.Allowed);
        Assert.Contains("wait 3 second", tooSoon.Message);

        clock.Advance(TimeSpan.FromSeconds(3));
        Assert.True(limiter.TryAcquire("u").Allowed);
    }

    [Fact]
    public void RefusedRequests_DoNotConsumeAllowance()
    {
        var (limiter, clock) = Create(daily: 2, gapSeconds: 5);
        Assert.True(limiter.TryAcquire("u").Allowed);
        Assert.False(limiter.TryAcquire("u").Allowed); // too soon, refused
        clock.Advance(TimeSpan.FromSeconds(6));
        var second = limiter.TryAcquire("u");
        Assert.True(second.Allowed);
        Assert.Equal(0, second.RemainingToday);
    }

    [Fact]
    public void SeveralRequestsAtOnce_NeedEnoughAllowance()
    {
        var (limiter, _) = Create(daily: 3);
        var tooMany = limiter.TryAcquire("u", requests: 4);
        Assert.False(tooMany.Allowed);
        Assert.Contains("only 3 are left", tooMany.Message);

        var ok = limiter.TryAcquire("u", requests: 3);
        Assert.True(ok.Allowed);
    }

    [Fact]
    public void ZeroLimit_TurnsTheLimitOff()
    {
        var (limiter, _) = Create(daily: 0, gapSeconds: 0);
        for (var i = 0; i < 100; i++)
            Assert.True(limiter.TryAcquire("u").Allowed);
    }

    [Fact]
    public void InvalidArguments_AreRejected()
    {
        var (limiter, _) = Create();
        Assert.Throws<ArgumentException>(() => limiter.TryAcquire(" "));
        Assert.Throws<ArgumentOutOfRangeException>(() => limiter.TryAcquire("u", 0));
    }
}
