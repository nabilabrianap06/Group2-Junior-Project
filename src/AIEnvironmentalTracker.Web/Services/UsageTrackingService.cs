using AIEnvironmentalTracker.Domain.Models;
using AIEnvironmentalTracker.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace AIEnvironmentalTracker.Web.Services;

/// <summary>A saved usage record together with the recommendations for its impact.</summary>
public sealed record RecordedUsage(UsageRecord Record, IReadOnlyList<RecommendationEngine> Recommendations);

/// <summary>Totals for one user, held in a <see cref="CarbonFootprintReport"/>.</summary>
public sealed record UsageSummary(int TotalQueries, CarbonFootprintReport Report);

/// <summary>Bucket size for <see cref="UsageTrackingService.GetTrendAsync"/>.</summary>
public enum TrendPeriod
{
    Daily,
    Weekly,
    Monthly
}

/// <summary>Totals for one bucket (day, week or month) of a trend. <see cref="Start"/> is the local start of the bucket.</summary>
public sealed record TrendPoint(DateTime Start, string Label, int Queries, decimal Water, decimal Co2, decimal Energy);

/// <summary>Totals for one AI model over the whole trend window.</summary>
public sealed record ModelBreakdown(string ModelName, int Queries, decimal Water, decimal Co2, decimal Energy);

/// <summary>Trend buckets (oldest first, empty buckets included) plus a per-model breakdown.</summary>
public sealed record UsageTrend(TrendPeriod Period, IReadOnlyList<TrendPoint> Points, IReadOnlyList<ModelBreakdown> Models);

/// <summary>Encapsulates impact calculation and PostgreSQL persistence for a signed-in user.</summary>
public class UsageTrackingService(ApplicationDbContext db)
{
    private const decimal MaxStoredImpact = 999_999_999_999m;

    public Task<RecordedUsage> RecordAsync(
        string userId, string modelName, int queryCount,
        decimal waterPerQuery, decimal co2PerQuery, decimal energyPerQuery,
        CancellationToken cancellationToken = default)
        => RecordAsync(userId, queryCount,
            new EnvironmentalFactor(modelName, waterPerQuery, co2PerQuery, energyPerQuery),
            cancellationToken);

    public async Task<RecordedUsage> RecordAsync(
        string userId, int queryCount, EnvironmentalFactor factor,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("A signed-in user is required.", nameof(userId));
        if (queryCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(queryCount), "Query count must be positive.");
        ArgumentNullException.ThrowIfNull(factor);

        var maxPerQuery = MaxStoredImpact / queryCount;
        if (factor.WaterFactorPerQuery > maxPerQuery ||
            factor.Co2FactorPerQuery > maxPerQuery ||
            factor.EnergyFactorPerQuery > maxPerQuery)
            throw new ArgumentOutOfRangeException(nameof(queryCount), "The calculated impact is too large to store.");

        var log = new AIUsageLog { DetectedApp = factor.AiModelName, QueryCount = queryCount };
        var report = new ImpactCalculator().ComputeImpact(log, factor);
        if (report.TotalWaterUsage > MaxStoredImpact ||
            report.TotalCO2Emission > MaxStoredImpact ||
            report.TotalEnergyUsage > MaxStoredImpact)
            throw new ArgumentOutOfRangeException(nameof(queryCount), "The calculated impact is too large to store.");

        var record = new UsageRecord
        {
            ApplicationUserId = userId,
            AiModelName = factor.AiModelName,
            QueryCount = queryCount,
            WaterPerQuery = factor.WaterFactorPerQuery,
            Co2PerQuery = factor.Co2FactorPerQuery,
            EnergyPerQuery = factor.EnergyFactorPerQuery,
            TotalWater = report.TotalWaterUsage,
            TotalCo2 = report.TotalCO2Emission,
            TotalEnergy = report.TotalEnergyUsage
        };

        db.UsageRecords.Add(record);
        await db.SaveChangesAsync(cancellationToken);

        var recommendations = new RecommendationEngine().FetchRecommendations(report);
        return new RecordedUsage(record, recommendations);
    }

    public Task<List<UsageRecord>> GetRecentAsync(string userId, CancellationToken cancellationToken = default)
        => db.UsageRecords.AsNoTracking()
            .Where(record => record.ApplicationUserId == userId)
            .OrderByDescending(record => record.Timestamp)
            .Take(20)
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Aggregates the user's records into daily (last 14 days), weekly (last 12 weeks, Monday start)
    /// or monthly (last 12 months) buckets. Buckets follow <paramref name="zone"/> (the user's time zone),
    /// and buckets without records are returned as zeros so a chart has no gaps.
    /// </summary>
    public async Task<UsageTrend> GetTrendAsync(
        string userId, TrendPeriod period, TimeZoneInfo? zone = null, DateTime? nowUtc = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("A signed-in user is required.", nameof(userId));

        zone ??= TimeZoneInfo.Utc;
        var utcNow = DateTime.SpecifyKind(nowUtc ?? DateTime.UtcNow, DateTimeKind.Utc);
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, zone);

        var (firstStart, step, label) = period switch
        {
            TrendPeriod.Daily => (localNow.Date.AddDays(-13), (Func<DateTime, DateTime>)(d => d.AddDays(1)), (Func<DateTime, string>)(d => d.ToString("dd MMM"))),
            TrendPeriod.Weekly => (StartOfWeek(localNow.Date).AddDays(-7 * 11), d => d.AddDays(7), d => "wk " + d.ToString("dd MMM")),
            TrendPeriod.Monthly => (new DateTime(localNow.Year, localNow.Month, 1).AddMonths(-11), d => d.AddMonths(1), d => d.ToString("MMM yyyy")),
            _ => throw new ArgumentOutOfRangeException(nameof(period))
        };

        // Local start of the window, converted back to UTC so the database can filter on the index.
        var windowStartUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(firstStart, DateTimeKind.Unspecified), zone);

        var rows = await db.UsageRecords.AsNoTracking()
            .Where(record => record.ApplicationUserId == userId && record.Timestamp >= windowStartUtc)
            .Select(record => new { record.Timestamp, record.AiModelName, record.QueryCount, record.TotalWater, record.TotalCo2, record.TotalEnergy })
            .ToListAsync(cancellationToken);

        var local = rows.Select(row => new
        {
            Local = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(row.Timestamp, DateTimeKind.Utc), zone),
            row.AiModelName, row.QueryCount, row.TotalWater, row.TotalCo2, row.TotalEnergy
        }).ToList();

        var points = new List<TrendPoint>();
        for (var start = firstStart; start <= localNow; start = step(start))
        {
            var end = step(start);
            var bucket = local.Where(row => row.Local >= start && row.Local < end).ToList();
            points.Add(new TrendPoint(start, label(start),
                bucket.Sum(row => row.QueryCount), bucket.Sum(row => row.TotalWater),
                bucket.Sum(row => row.TotalCo2), bucket.Sum(row => row.TotalEnergy)));
        }

        var models = local
            .GroupBy(row => row.AiModelName)
            .Select(group => new ModelBreakdown(group.Key, group.Sum(row => row.QueryCount),
                group.Sum(row => row.TotalWater), group.Sum(row => row.TotalCo2), group.Sum(row => row.TotalEnergy)))
            .OrderByDescending(model => model.Co2)
            .ThenBy(model => model.ModelName)
            .ToList();

        return new UsageTrend(period, points, models);
    }

    private static DateTime StartOfWeek(DateTime date)
        => date.AddDays(-(((int)date.DayOfWeek + 6) % 7)); // Monday

    /// <summary>Sums every record of the user and returns the totals as a report.</summary>
    public async Task<UsageSummary> GetSummaryAsync(string userId, CancellationToken cancellationToken = default)
    {
        var totals = await db.UsageRecords.AsNoTracking()
            .Where(record => record.ApplicationUserId == userId)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                Queries = group.Sum(record => record.QueryCount),
                Water = group.Sum(record => record.TotalWater),
                Co2 = group.Sum(record => record.TotalCo2),
                Energy = group.Sum(record => record.TotalEnergy)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var report = new CarbonFootprintReport(0);
        if (totals is not null)
            report.GenerateReport(totals.Water, totals.Co2, totals.Energy);

        return new UsageSummary(totals?.Queries ?? 0, report);
    }
}
