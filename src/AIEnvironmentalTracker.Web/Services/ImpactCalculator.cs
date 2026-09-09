namespace AIEnvironmentalTracker.Web.Services;

public class ImpactCalculator
{
    // Faktor referensi berdasarkan Google Gemini Apps median prompt
    private const double ReferenceEnergyWh = 0.24;
    private const double ReferenceCO2g = 0.03;
    private const double ReferenceWaterMl = 0.26;
    private const int ReferenceTokenCount = 500; 

    public record EnvironmentalImpact(double EnergyWh, double CO2g, double WaterMl);

    public EnvironmentalImpact CalculateImpact(int totalTokens)
    {
        if (totalTokens <= 0) return new EnvironmentalImpact(0, 0, 0);

        double tokenRatio = (double)totalTokens / ReferenceTokenCount;

        return new EnvironmentalImpact(
            EnergyWh: Math.Round(ReferenceEnergyWh * tokenRatio, 4),
            CO2g: Math.Round(ReferenceCO2g * tokenRatio, 4),
            WaterMl: Math.Round(ReferenceWaterMl * tokenRatio, 4)
        );
    }
}