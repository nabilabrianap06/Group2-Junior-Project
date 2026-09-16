namespace AIEnvironmentalTracker.Domain.Models;

public class CarbonFootprintReport
{
    public int ReportId { get; set; }
    public double TotalWaterUsage { get; set; }
    public double TotalCO2Emission { get; set; }
    public double TotalEnergyUsage { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    public void GenerateReport(double water, double co2, double energy)
    {
        TotalWaterUsage += water;
        TotalCO2Emission += co2;
        TotalEnergyUsage += energy;
        LastUpdated = DateTime.UtcNow;
    }

    public (double Water, double CO2, double Energy) GetRunningTotals()
    {
        return (TotalWaterUsage, TotalCO2Emission, TotalEnergyUsage);
    }
}
