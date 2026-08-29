namespace AiEnvironmentalTracker.Interfaces
{
    /// <summary>
    /// Converts energy consumption (kWh) into estimated water usage (mL)
    /// based on data-centre Water Usage Effectiveness.
    /// </summary>
    public interface IWaterCalculator
    {
        double CalculateWaterML(double energyKWh);
    }
}
