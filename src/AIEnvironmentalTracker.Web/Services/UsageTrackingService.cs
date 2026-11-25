using AIEnvironmentalTracker.Domain.Models;
using AIEnvironmentalTracker.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace AIEnvironmentalTracker.Web.Services;

/// <summary>A saved usage record together with the recommendations for its impact.</summary>
public sealed record RecordedUsage(UsageRecord Record, IReadOnlyList<RecommendationEngine> Recommendations);

/// <summary>Totals for one user, held in a <see cref="CarbonFootprintReport"/>.</summary>
public sealed record UsageSummary(int TotalQueries, CarbonFootprintReport Report);

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
