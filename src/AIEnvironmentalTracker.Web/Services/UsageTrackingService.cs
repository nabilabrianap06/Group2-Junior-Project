using AIEnvironmentalTracker.Domain.Models;
using AIEnvironmentalTracker.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace AIEnvironmentalTracker.Web.Services;

/// <summary>Encapsulates impact calculation and PostgreSQL persistence for a signed-in user.</summary>
public class UsageTrackingService(ApplicationDbContext db)
{
    private const decimal MaxStoredImpact = 999_999_999_999m;

    public async Task<UsageRecord> RecordAsync(
        string userId, string modelName, int queryCount,
        decimal waterPerQuery, decimal co2PerQuery, decimal energyPerQuery,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            throw new ArgumentException("A signed-in user is required.", nameof(userId));
        if (queryCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(queryCount), "Query count must be positive.");

        var factor = new EnvironmentalFactor(modelName, waterPerQuery, co2PerQuery, energyPerQuery);
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
        return record;
    }

    public Task<List<UsageRecord>> GetRecentAsync(string userId, CancellationToken cancellationToken = default)
        => db.UsageRecords.AsNoTracking()
            .Where(record => record.ApplicationUserId == userId)
            .OrderByDescending(record => record.Timestamp)
            .Take(20)
            .ToListAsync(cancellationToken);
}
