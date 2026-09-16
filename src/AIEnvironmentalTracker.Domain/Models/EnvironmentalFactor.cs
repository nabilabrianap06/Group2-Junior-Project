using System;
using System.Collections.Generic;
using System.Linq;

namespace AIEnvironmentalTracker.Domain.Models;

/// <summary>
/// Value object / Entity configuration for environmental factor per AI model.
/// </summary>
public class EnvironmentalFactor
{
    // Primary Key for EF Core
    public int FactorId { get; private set; }

    public string AiModelName { get; private set; } = string.Empty;

    // Decimal for precision
    public decimal WaterFactorPerQuery { get; private set; }
    public decimal Co2FactorPerQuery { get; private set; }
    public decimal EnergyFactorPerQuery { get; private set; }

    // Parameterless constructor for core
    private EnvironmentalFactor() { }

    // Main Constructor (Encapsulation & Validayion)
    public EnvironmentalFactor(string aiModelName, decimal waterFactor, decimal co2Factor, decimal energyFactor)
    {
        AiModelName = aiModelName ?? throw new ArgumentNullException(nameof(aiModelName), "Model name cannot be null.");
        
        ValidateFactors(waterFactor, co2Factor, energyFactor);
        
        WaterFactorPerQuery = waterFactor;
        Co2FactorPerQuery = co2Factor;
        EnergyFactorPerQuery = energyFactor;
    }

    /// <summary>
    /// Factory method to find summary based on model
    /// </summary>
    public static EnvironmentalFactor GetFactorsByModel(string modelName, IEnumerable<EnvironmentalFactor> availableFactors)
    {
        if (string.IsNullOrWhiteSpace(modelName))
            throw new ArgumentException("Model name cannot be empty.", nameof(modelName));

        return availableFactors.FirstOrDefault(f => 
            f.AiModelName.Equals(modelName, StringComparison.OrdinalIgnoreCase)) 
            ?? throw new InvalidOperationException($"Environmental factors for model '{modelName}' not found in database.");
    }

    private void ValidateFactors(decimal water, decimal co2, decimal energy)
    {
        if (water < 0 || co2 < 0 || energy < 0)
            throw new ArgumentOutOfRangeException("Environmental factors cannot be negative.");
    }
}