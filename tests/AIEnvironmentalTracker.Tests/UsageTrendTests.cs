using AIEnvironmentalTracker.Web.Data;
using AIEnvironmentalTracker.Web.Services;

namespace AIEnvironmentalTracker.Tests;

public class UsageTrendTests
{
    private static readonly TimeZoneInfo Jakarta = TimeZoneInfo.FindSystemTimeZoneById("Asia/Jakarta"); // UTC+7, no DST
    private static readonly DateTime Now = new(2026, 11, 26, 3, 0, 0, DateTimeKind.Utc);               // 10:00 Thu 26 Nov in Jakarta

    private static UsageRecord Record(string userId, DateTime utc, string model, int queries) => new()
    {
        ApplicationUserId = userId, AiModelName = model, QueryCount = queries,
        TotalWater = queries, TotalCo2 = queries * 2m, TotalEnergy = queries * 3m, Timestamp = utc
    };

    private static async Task<UsageTrackingService> CreateAsync()
    {
        var db = TestSupport.CreateDb();
        await TestSupport.AddUserAsync(db, "u1");
        await TestSupport.AddUserAsync(db, "u2");
        db.UsageRecords.AddRange(
            Record("u1", new DateTime(2026, 11, 25, 17, 30, 0, DateTimeKind.Utc), "A", 1),  // 00:30 on 26 Nov in Jakarta
            Record("u1", new DateTime(2026, 11, 26, 1, 0, 0, DateTimeKind.Utc), "A", 2),
            Record("u1", new DateTime(2026, 11, 24, 5, 0, 0, DateTimeKind.Utc), "B", 4),
            Record("u1", new DateTime(2026, 11, 16, 5, 0, 0, DateTimeKind.Utc), "B", 8),    // Monday of the previous week
            Record("u1", new DateTime(2026, 10, 2, 5, 0, 0, DateTimeKind.Utc), "A", 16),
            Record("u1", new DateTime(2025, 11, 20, 5, 0, 0, DateTimeKind.Utc), "A", 32),   // older than 12 months
            Record("u1", new DateTime(2025, 12, 20, 5, 0, 0, DateTimeKind.Utc), "C", 64),
            Record("u2", new DateTime(2026, 11, 26, 1, 0, 0, DateTimeKind.Utc), "A", 1000)); // another user
        await db.SaveChangesAsync();
        return new UsageTrackingService(db);
    }

    [Fact]
    public async Task Daily_HasFourteenBuckets_AndEmptyDaysAreZero()
    {
        var trend = await (await CreateAsync()).GetTrendAsync("u1", TrendPeriod.Daily, Jakarta, Now);
        Assert.Equal(14, trend.Points.Count);
        Assert.Equal("26 Nov", trend.Points[^1].Label);
        Assert.Equal(0, trend.Points[^2].Queries);   // 25 Nov: nothing in Jakarta time
        Assert.Equal(4, trend.Points[^3].Queries);   // 24 Nov
    }

    [Fact]
    public async Task Daily_BucketsFollowTheUsersTimeZone()
    {
        var service = await CreateAsync();
        var jakarta = await service.GetTrendAsync("u1", TrendPeriod.Daily, Jakarta, Now);
        var utc = await service.GetTrendAsync("u1", TrendPeriod.Daily, null, Now);

        Assert.Equal(3, jakarta.Points[^1].Queries);   // 1 + 2 both on 26 Nov in Jakarta
        Assert.Equal(6m, jakarta.Points[^1].Co2);
        Assert.Equal(1, utc.Points[^2].Queries);       // in UTC the first record belongs to 25 Nov
    }

    [Fact]
    public async Task Weekly_StartsOnMonday()
    {
        var trend = await (await CreateAsync()).GetTrendAsync("u1", TrendPeriod.Weekly, Jakarta, Now);
        Assert.Equal(12, trend.Points.Count);
        Assert.Equal("wk 23 Nov", trend.Points[^1].Label);
        Assert.Equal(7, trend.Points[^1].Queries);     // 3 + 4
        Assert.Equal("wk 16 Nov", trend.Points[^2].Label);
        Assert.Equal(8, trend.Points[^2].Queries);
    }

    [Fact]
    public async Task Monthly_CoversTwelveMonths_AndDropsOlderRecords()
    {
        var trend = await (await CreateAsync()).GetTrendAsync("u1", TrendPeriod.Monthly, Jakarta, Now);
        Assert.Equal(12, trend.Points.Count);
        Assert.Equal("Dec 2025", trend.Points[0].Label);
        Assert.Equal(64, trend.Points[0].Queries);
        Assert.Equal("Nov 2026", trend.Points[^1].Label);
        Assert.Equal(15, trend.Points[^1].Queries);    // 1 + 2 + 4 + 8
        Assert.Equal(16, trend.Points[^2].Queries);    // Oct
        Assert.Equal(15 + 16 + 64, trend.Points.Sum(p => p.Queries)); // the 32 from Nov 2025 is outside
    }

    [Fact]
    public async Task OtherUsersData_IsNeverIncluded()
    {
        var trend = await (await CreateAsync()).GetTrendAsync("u1", TrendPeriod.Daily, Jakarta, Now);
        Assert.DoesNotContain(trend.Points, p => p.Queries >= 1000);
    }

    [Fact]
    public async Task ModelBreakdown_IsSortedByCo2()
    {
        var trend = await (await CreateAsync()).GetTrendAsync("u1", TrendPeriod.Weekly, Jakarta, Now);
        // The 12-week window starts on 7 Sep: A has 1 + 2 + 16 = 19 queries (incl. 2 Oct), B has 4 + 8 = 12.
        Assert.Equal(["A", "B"], trend.Models.Select(m => m.ModelName));
        Assert.Equal(19, trend.Models[0].Queries);
        Assert.Equal(12, trend.Models[1].Queries);
    }

    [Fact]
    public async Task UnknownUser_GivesZerosAndNoModels()
    {
        var trend = await (await CreateAsync()).GetTrendAsync("nobody", TrendPeriod.Daily, Jakarta, Now);
        Assert.All(trend.Points, p => Assert.Equal(0, p.Queries));
        Assert.Empty(trend.Models);
    }

    [Fact]
    public async Task BlankUser_IsRejected()
    {
        var service = await CreateAsync();
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetTrendAsync(" ", TrendPeriod.Daily, Jakarta, Now));
    }
}
