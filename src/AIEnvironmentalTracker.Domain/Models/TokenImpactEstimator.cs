namespace AIEnvironmentalTracker.Domain.Models;

/// <summary>
/// Turns a token count reported by an AI provider into an <see cref="EnvironmentalFactor"/>
/// using a token-weighted approximation of Google's published estimate for the median
/// Gemini Apps text prompt (May 2025): 0.24 Wh energy, 0.03 gCO2e and 0.26 mL water.
/// Google does not publish the token count of that median prompt, so the group assumes
/// a reference of 500 tokens. Results are estimates, not measurements.
/// </summary>
public static class TokenImpactEstimator
{
    public const decimal ReferenceEnergyWh = 0.24m;
    public const decimal ReferenceCo2Grams = 0.03m;
    public const decimal ReferenceWaterMl = 0.26m;
    public const int ReferenceTokenCount = 500;

    /// <summary>
    /// Creates the per-query factor for one request of <paramref name="totalTokens"/> tokens.
    /// </summary>
    public static EnvironmentalFactor CreateFactor(string modelName, int totalTokens)
    {
        if (totalTokens < 0)
            throw new ArgumentOutOfRangeException(nameof(totalTokens), "Token count cannot be negative.");

        var ratio = (decimal)totalTokens / ReferenceTokenCount;

        return new EnvironmentalFactor(
            modelName,
            waterFactor: ReferenceWaterMl * ratio,
            co2Factor: ReferenceCo2Grams * ratio,
            energyFactor: ReferenceEnergyWh * ratio);
    }
}
