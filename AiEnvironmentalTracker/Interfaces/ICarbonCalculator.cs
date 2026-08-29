namespace AiEnvironmentalTracker.Interfaces
{
    /// <summary>
    /// Converts energy consumption (kWh) into CO2-equivalent emissions (grams).
    /// </summary>
    public interface ICarbonCalculator
    {
        double CalculateCarbonGrams(double energyKWh);
    }
}
