using System;

namespace AiEnvironmentalTracker.Models
{
    /// <summary>
    /// Holds the computed environmental footprint for a single AI query,
    /// expressed in energy, carbon, and water metrics along with a
    /// human-readable analogy string.
    /// </summary>
    public class EnvironmentalImpact
    {
        /// <summary>Estimated energy consumed in kilowatt-hours.</summary>
        public double EnergyKWh { get; set; }

        /// <summary>Estimated CO2-equivalent emissions in grams.</summary>
        public double CarbonGrams { get; set; }

        /// <summary>Estimated water usage in millilitres.</summary>
        public double WaterML { get; set; }

        /// <summary>
        /// Plain-language comparison so users can intuitively grasp the numbers
        /// (e.g. "equivalent to charging a phone for 0.4 minutes").
        /// </summary>
        public string AnalogyString { get; set; } = string.Empty;
    }
}
