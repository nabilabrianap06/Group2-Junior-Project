namespace AIEnvironmentalTracker.Domain.Models;

public class ImpactCalculator
{
    public decimal CalculatedEnergy { get; set; }
    public decimal CalculatedCO2 { get; set; }
    public decimal CalculatedWater { get; set; }

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

        var queryCount = Math.Max(usageLog.QueryCount, 0);

        CalculatedEnergy = queryCount * environmentalFactor.EnergyFactorPerQuery;
        CalculatedCO2 = queryCount * environmentalFactor.Co2FactorPerQuery;
        CalculatedWater = queryCount * environmentalFactor.WaterFactorPerQuery;

        var report = new CarbonFootprintReport(usageLog.RegisteredUserId);
        report.GenerateReport(CalculatedWater, CalculatedCO2, CalculatedEnergy);

        return report;
    }
}
