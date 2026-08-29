using System;
using System.Text;
using AiEnvironmentalTracker.Interfaces;
using AiEnvironmentalTracker.Models;

namespace AiEnvironmentalTracker.Services
{
    /// <summary>
    /// Central calculation engine that converts raw token counts from the
    /// Gemini API into environmental impact estimates.
    ///
    /// Baseline coefficients are drawn from published research on LLM
    /// inference workloads (see inline references).  Each sub-calculator
    /// is exposed through its own interface so individual steps can be
    /// swapped or unit-tested in isolation.
    /// </summary>
    public class CalculationEngine : IElectricityCalculator, ICarbonCalculator, IWaterCalculator
    {
        // ── Baseline coefficients ───────────────────────────────────────
        // Source: IEA + various LLM carbon-footprint studies (2023-2024).
        // These are order-of-magnitude estimates for a mid-size model like
        // Gemini 1.5 Flash running on a typical Google data-centre.

        /// <summary>kWh consumed per single token (derived from ~0.0003 kWh / 1 000 tokens).</summary>
        private const double KWhPerToken = 0.0003 / 1000.0;

        /// <summary>Global-average grid carbon intensity in grams CO2eq per kWh.</summary>
        private const double CarbonIntensityGramsPerKWh = 400.0;

        /// <summary>Data-centre Water Usage Effectiveness: mL of water per kWh.</summary>
        private const double WaterUsageEffectivenessMLPerKWh = 1.5;

        // ── Analogy reference points ────────────────────────────────────
        // Smartphone battery ≈ 0.015 kWh (15 Wh), full charge takes ~60 min.
        private const double SmartphoneBatteryKWh = 0.015;
        private const double SmartphoneChargeMinutes = 60.0;

        // Standard small water bottle = 600 mL.
        private const double WaterBottleML = 600.0;

        // ── IElectricityCalculator ──────────────────────────────────────

        public double CalculateKWh(int totalTokens)
        {
            if (totalTokens < 0)
                throw new ArgumentOutOfRangeException(nameof(totalTokens));

            return totalTokens * KWhPerToken;
        }

        // ── ICarbonCalculator ───────────────────────────────────────────

        public double CalculateCarbonGrams(double energyKWh)
        {
            if (energyKWh < 0)
                throw new ArgumentOutOfRangeException(nameof(energyKWh));

            return energyKWh * CarbonIntensityGramsPerKWh;
        }

        // ── IWaterCalculator ────────────────────────────────────────────

        public double CalculateWaterML(double energyKWh)
        {
            if (energyKWh < 0)
                throw new ArgumentOutOfRangeException(nameof(energyKWh));

            return energyKWh * WaterUsageEffectivenessMLPerKWh;
        }

        // ── High-level convenience method ───────────────────────────────

        /// <summary>
        /// Runs the full pipeline: tokens -> energy -> carbon + water,
        /// then builds a readable analogy string from the results.
        /// </summary>
        public EnvironmentalImpact ComputeImpact(int totalTokens)
        {
            double energy = CalculateKWh(totalTokens);
            double carbon = CalculateCarbonGrams(energy);
            double water  = CalculateWaterML(energy);

            string analogy = BuildAnalogy(energy, water);

            return new EnvironmentalImpact
            {
                EnergyKWh    = Math.Round(energy, 8),
                CarbonGrams  = Math.Round(carbon, 6),
                WaterML      = Math.Round(water, 6),
                AnalogyString = analogy
            };
        }

        // ── Private helpers ─────────────────────────────────────────────

        /// <summary>
        /// Translates raw numbers into something a non-technical user can
        /// picture: phone-charge minutes and fractions of a water bottle.
        /// </summary>
        private static string BuildAnalogy(double energyKWh, double waterML)
        {
            double chargeMinutes = (energyKWh / SmartphoneBatteryKWh) * SmartphoneChargeMinutes;
            double bottleFraction = waterML / WaterBottleML;

            var sb = new StringBuilder();
            sb.Append($"Roughly {chargeMinutes:F2} minutes of smartphone charging");
            sb.Append($" and {bottleFraction:F4} of a 600 mL water bottle.");

            return sb.ToString();
        }
    }
}
