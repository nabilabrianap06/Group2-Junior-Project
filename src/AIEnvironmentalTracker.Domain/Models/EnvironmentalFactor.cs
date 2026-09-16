namespace AIEnvironmentalTracker.Domain.Models;

public class EnvironmentalFactor
{
    public int FactorId { get; set; }
    public string AiModelName { get; set; } = string.Empty;
    public double WaterFactorPerQuery { get; set; }
    public double Co2FactorPerQuery { get; set; }
    public double EnergyFactorPerQuery { get; set; }

    public static EnvironmentalFactor GetFactorsByModel(string modelName)
    {
        return new EnvironmentalFactor 
        { 
            AiModelName = modelName,
            Co2FactorPerQuery = 0.05,
            WaterFactorPerQuery = 0.1,
            EnergyFactorPerQuery = 0.8
        };
    }
}
