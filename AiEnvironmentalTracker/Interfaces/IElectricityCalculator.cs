namespace AiEnvironmentalTracker.Interfaces
{
    /// <summary>
    /// Converts a token count into an estimated energy figure (kWh).
    /// </summary>
    public interface IElectricityCalculator
    {
        double CalculateKWh(int totalTokens);
    }
}
