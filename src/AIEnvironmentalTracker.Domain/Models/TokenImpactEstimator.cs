// =====================================================================================================
// TokenImpactEstimator - SOURCES, METHOD AND LIMITATIONS
// =====================================================================================================
//
// PRIMARY SOURCE
//   Google (2025). "Measuring the environmental impact of delivering AI at Google Scale".
//   arXiv:2508.15734v1  https://arxiv.org/abs/2508.15734  (HTML: https://arxiv.org/html/2508.15734v1)
//   The equations below follow Sec. 3.2 (energy), Sec. 3.3 (emissions) and the water definition of the
//   same paper. The constants come from its Table 1/Table 2, Sec. 4.3 and Appendix A.
//
// METHOD (Google's accounting chain, applied to one request)
//   1. Energy.   E_total = sum( P_total x t x PUE )          [Wh]   (Sec. 3.2)
//                E_overhead = sum( P_total x t x (PUE - 1) )        => E_overhead / E_total = (PUE - 1) / PUE
//   2. Carbon.   CO2e = E_total x EF + (Scope1 + Scope3) / Q  [g]   (Sec. 3.3)
//                EF = market-based grid emission factor of the previous calendar year (2024: 94 gCO2e/kWh).
//                (Scope1 + Scope3) / Q = embodied emissions of accelerators and host CPU/DRAM per prompt
//                (0.010 g per median prompt, Table 2).
//   3. Water.    Water = ( E_total - E_overhead ) x WUE        [mL]  (Sec. 3.3 / water definition)
//                WUE = 1.15 L/kWh (Google "WUE Category 2", consumptive, 2023 and 2024).
//                Unit note: Wh x (L/kWh) = mL.
//
// CONSTANTS (Google, median Gemini Apps text prompt, May 2025)
//   Energy 0.24 Wh (Table 1: accelerators 0.14, CPU+DRAM 0.06, idle 0.02, overhead 0.02)
//   PUE 1.09 (fleet-wide average, Sec. 4.3) | EF 94 gCO2e/kWh (2024 market-based; location-based was 345)
//   Embodied 0.010 gCO2e | WUE 1.15 L/kWh
//   Published headline of the paper: 0.24 Wh, 0.03 gCO2e, 0.26 mL.
//   Check with this class: 0.24 Wh -> 0.0326 gCO2e and 0.2532 mL, i.e. the published 0.03 g and 0.26 mL
//   after the paper's own rounding (the water figure differs by about 3 percent).
//
// WHAT THE GROUP ADDED (assumptions, NOT from Google)
//   - TOKEN SCALING: Google does not publish the token count of the median prompt. The group assumes
//     500 tokens and scales energy, embodied carbon linearly with total tokens (input + output).
//   - Everything else (carbon, water) is then derived from the scaled energy by the chain above.
//
// LIMITATIONS - read before quoting any number from this app
//   1. SCOPE OF THE SOURCE. The figures describe the median *Gemini Apps text prompt* served by Google
//      in May 2025. The paper makes no claim that they apply to other models, other Gemini variants,
//      other providers, images, audio or video. Here the SAME factor is used for every Gemini model, so
//      the app cannot tell a Flash-Lite from a Pro model.
//   2. LINEAR TOKEN SCALING IS A SIMPLIFICATION. Real energy depends on input vs. output tokens,
//      context length, batching, hardware and caching. The 500-token reference is an assumption.
//   3. EMISSION FACTOR. 94 gCO2e/kWh is Google's market-based figure, which credits Google's clean
//      energy purchases. The location-based figure (345 gCO2e/kWh in 2024) would give about 3.7 times
//      more operational carbon. Use ImpactParameters.GoogleLocationBased2025 to see that case.
//      The paper does not state which year's factor was used for the May 2025 headline; 2024 is the
//      value its own rule ("previous calendar year") implies.
//   4. WATER. Only on-site cooling water as consumptive WUE; the paper's headline (0.26 mL) and the
//      formula result (0.253 mL) differ slightly because of rounding in the published inputs.
//   5. EXCLUDED BY THE SOURCE: model training, data storage, external networking and end-user devices.
//   6. FIRST-PARTY DATA. Authors are Google employees using internal telemetry; the paper does not
//      describe external verification.
//   7. These are ESTIMATES, not measurements.
//
// ALTERNATIVE CONSIDERED (not used): EcoLogits methodology, https://ecologits.ai/latest/methodology/llm_inference/
//   (docs CC BY-SA 4.0, code MPL-2.0). It needs the active-parameter count of the model, which is not
//   public for Gemini models, so it was not adopted. See docs/decisions-and-system-design.md.
// =====================================================================================================

namespace AIEnvironmentalTracker.Domain.Models;

/// <summary>
/// Inputs of the impact chain. The defaults are Google's published values for May 2025;
/// pass another instance to explore other assumptions (for example a location-based emission factor).
/// </summary>
/// <param name="ReferenceEnergyWh">Energy of the reference prompt in Wh.</param>
/// <param name="ReferenceTokenCount">Tokens assumed for the reference prompt (a group assumption).</param>
/// <param name="Pue">Power usage effectiveness, at least 1.</param>
/// <param name="EmissionFactorGramsPerKWh">Grid emission factor in gCO2e per kWh.</param>
/// <param name="EmbodiedCo2GramsPerReferencePrompt">Scope 1 + 3 (embodied) emissions of the reference prompt in g.</param>
/// <param name="WaterUseEffectivenessLitersPerKWh">Consumptive water use effectiveness in L per kWh.</param>
public sealed record ImpactParameters(
    decimal ReferenceEnergyWh,
    int ReferenceTokenCount,
    decimal Pue,
    decimal EmissionFactorGramsPerKWh,
    decimal EmbodiedCo2GramsPerReferencePrompt,
    decimal WaterUseEffectivenessLitersPerKWh)
{
    /// <summary>Google, May 2025, market-based emission factor (arXiv:2508.15734).</summary>
    public static ImpactParameters Google2025 { get; } = new(
        ReferenceEnergyWh: 0.24m,
        ReferenceTokenCount: 500,
        Pue: 1.09m,
        EmissionFactorGramsPerKWh: 94m,
        EmbodiedCo2GramsPerReferencePrompt: 0.010m,
        WaterUseEffectivenessLitersPerKWh: 1.15m);

    /// <summary>Same as <see cref="Google2025"/> but with Google's 2024 location-based factor (345 gCO2e/kWh).</summary>
    public static ImpactParameters GoogleLocationBased2025 { get; } = Google2025 with { EmissionFactorGramsPerKWh = 345m };

    internal void Validate()
    {
        if (ReferenceEnergyWh < 0 || EmissionFactorGramsPerKWh < 0 ||
            EmbodiedCo2GramsPerReferencePrompt < 0 || WaterUseEffectivenessLitersPerKWh < 0)
            throw new ArgumentOutOfRangeException(nameof(ImpactParameters), "Impact parameters cannot be negative.");
        if (ReferenceTokenCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(ReferenceTokenCount), "The reference token count must be positive.");
        if (Pue < 1)
            throw new ArgumentOutOfRangeException(nameof(Pue), "PUE cannot be below 1.");
    }
}

/// <summary>Result of the impact chain for one request, with the intermediate values kept for transparency.</summary>
public sealed record ImpactEstimate(
    int TotalTokens,
    decimal EnergyWh,
    decimal OverheadEnergyWh,
    decimal OperationalCo2Grams,
    decimal EmbodiedCo2Grams,
    decimal WaterMl)
{
    /// <summary>Operational plus embodied emissions in g CO2e.</summary>
    public decimal Co2Grams => OperationalCo2Grams + EmbodiedCo2Grams;
}

/// <summary>
/// Turns a token count reported by an AI provider into an <see cref="EnvironmentalFactor"/> with
/// Google's energy -> carbon -> water chain. See the comment at the top of this file for sources,
/// the exact equations and the limitations. Results are estimates, not measurements.
/// </summary>
public static class TokenImpactEstimator
{
    /// <summary>Published headline for the median Gemini Apps text prompt (May 2025), used for display.</summary>
    public const decimal ReferenceEnergyWh = 0.24m;
    public const decimal ReferenceCo2Grams = 0.03m;
    public const decimal ReferenceWaterMl = 0.26m;

    /// <summary>The group's assumption for the token count of that median prompt (not published by Google).</summary>
    public const int ReferenceTokenCount = 500;

    /// <summary>Runs the chain for <paramref name="totalTokens"/> tokens.</summary>
    public static ImpactEstimate Estimate(int totalTokens, ImpactParameters? parameters = null)
    {
        if (totalTokens < 0)
            throw new ArgumentOutOfRangeException(nameof(totalTokens), "Token count cannot be negative.");

        parameters ??= ImpactParameters.Google2025;
        parameters.Validate();

        var ratio = (decimal)totalTokens / parameters.ReferenceTokenCount;

        // 1. Energy (Wh). Overhead is the (PUE - 1) share of the total, so overhead / total = (PUE - 1) / PUE.
        var energyWh = parameters.ReferenceEnergyWh * ratio;
        var overheadWh = energyWh * (parameters.Pue - 1m) / parameters.Pue;

        // 2. Carbon (g). Wh / 1000 = kWh; kWh x g/kWh = g.
        var operationalCo2 = energyWh / 1000m * parameters.EmissionFactorGramsPerKWh;
        var embodiedCo2 = parameters.EmbodiedCo2GramsPerReferencePrompt * ratio;

        // 3. Water (mL). Wh x (L/kWh) = mL, taken on the energy without data-center overhead.
        var waterMl = (energyWh - overheadWh) * parameters.WaterUseEffectivenessLitersPerKWh;

        return new ImpactEstimate(totalTokens, energyWh, overheadWh, operationalCo2, embodiedCo2, waterMl);
    }

    /// <summary>
    /// Creates the per-query factor for one request of <paramref name="totalTokens"/> tokens.
    /// </summary>
    public static EnvironmentalFactor CreateFactor(string modelName, int totalTokens, ImpactParameters? parameters = null)
    {
        var estimate = Estimate(totalTokens, parameters);

        return new EnvironmentalFactor(
            modelName,
            waterFactor: estimate.WaterMl,
            co2Factor: estimate.Co2Grams,
            energyFactor: estimate.EnergyWh);
    }
}
