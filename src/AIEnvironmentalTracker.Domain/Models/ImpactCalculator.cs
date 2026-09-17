namespace AIEnvironmentalTracker.Domain.Models;

public class ImpactCalculator
{
    public double CalculatedEnergy { get; set; }
    public double CalculatedCO2 { get; set; }
    public double CalculatedWater { get; set; }

    public CarbonFootprintReport ComputeImpact(AIUsageLog usageLog)
    {
        ArgumentNullException.ThrowIfNull(usageLog);

        var environmentalFactor = EnvironmentalFactor.GetFactorsByModel(usageLog.DetectedApp);
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

        var report = new CarbonFootprintReport();
        report.GenerateReport(CalculatedWater, CalculatedCO2, CalculatedEnergy);

        return report;
    }
}
