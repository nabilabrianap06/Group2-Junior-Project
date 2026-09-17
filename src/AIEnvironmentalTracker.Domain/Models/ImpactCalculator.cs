namespace AIEnvironmentalTracker.Domain.Models;

public class ImpactCalculator
{
    public double CalculatedEnergy { get; set; }
    public double CalculatedCO2 { get; set; }
    public double CalculatedWater { get; set; }

    public void ComputeImpact() { /* TODO */ }
}
