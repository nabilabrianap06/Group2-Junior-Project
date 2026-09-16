using System;

namespace AIEnvironmentalTracker.Domain.Models;

public class CarbonFootprintReport
{
    public int ReportId { get; private set; }

    // Foreign Key for relation to RegisteredUser
    public int UserId { get; private set; }
    
    // Navigation Property
    // public virtual RegisteredUser User { get; private set; }

    public decimal TotalWaterUsage { get; private set; }
    public decimal TotalCO2Emission { get; private set; }
    public decimal TotalEnergyUsage { get; private set; }
    
    public DateTime LastUpdated { get; private set; }

    // Constructor ffor core
    private CarbonFootprintReport() { }

    //Main Constructor
    public CarbonFootprintReport(int userId)
    {
        UserId = userId;
        LastUpdated = DateTime.UtcNow;
    }

    /// <summary>
    /// Generate new report based on new calculation
    /// </summary>
    public void GenerateReport(decimal waterUsage, decimal co2Emission, decimal energyUsage)
    {
        if (waterUsage < 0 || co2Emission < 0 || energyUsage < 0)
            throw new ArgumentOutOfRangeException("Usage values cannot be negative.");

        TotalWaterUsage += waterUsage;
        TotalCO2Emission += co2Emission;
        TotalEnergyUsage += energyUsage;
        LastUpdated = DateTime.UtcNow;
    }

    /// <summary>
    /// Return total accumulative as tuple
    /// </summary>
    public (decimal Water, decimal CO2, decimal Energy) GetRunningTotals()
    {
        return (TotalWaterUsage, TotalCO2Emission, TotalEnergyUsage);
    }
}