namespace AIEnvironmentalTracker.Domain.Models;

public class ImpactCalculator
{
    public decimal CalculatedEnergy { get; private set; }
    public decimal CalculatedCO2 { get; private set; }
    public decimal CalculatedWater { get; private set; }

    public CarbonFootprintReport ComputeImpact(AIUsageLog usageLog, IEnumerable<EnvironmentalFactor> availableFactors)
    {
        ArgumentNullException.ThrowIfNull(usageLog);

        var environmentalFactor = EnvironmentalFactor.GetFactorsByModel(usageLog.DetectedApp, availableFactors);
        return ComputeImpact(usageLog, environmentalFactor);
    }

    public CarbonFootprintReport ComputeImpact(AIUsageLog usageLog, EnvironmentalFactor environmentalFactor)
    {
        ArgumentNullException.ThrowIfNull(usageLog);
        ArgumentNullException.ThrowIfNull(environmentalFactor);

        if (usageLog.QueryCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(usageLog), "Query count must be positive.");

        var queryCount = usageLog.QueryCount;

        CalculatedEnergy = queryCount * environmentalFactor.EnergyFactorPerQuery;
        CalculatedCO2 = queryCount * environmentalFactor.Co2FactorPerQuery;
        CalculatedWater = queryCount * environmentalFactor.WaterFactorPerQuery;

        var report = new CarbonFootprintReport(usageLog.RegisteredUserId);
        report.GenerateReport(CalculatedWater, CalculatedCO2, CalculatedEnergy);

        return report;
    }
}
