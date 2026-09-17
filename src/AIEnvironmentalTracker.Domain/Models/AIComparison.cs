using System.Collections.Generic;
using System.Linq;

namespace AIEnvironmentalTracker.Domain.Models;

public class AIComparison
{
    public int ComparisonId { get; set; }
    public List<string> ModelList { get; set; } = new();
    public double EfficiencyScore { get; set; }

    public IReadOnlyList<(string ModelName, double EfficiencyScore)> CompareModels()
    {
        var rankedModels = ModelList
            .Where(modelName => !string.IsNullOrWhiteSpace(modelName))
            .Select(modelName =>
            {
                var factor = EnvironmentalFactor.GetFactorsByModel(modelName);
                var score = CalculateEfficiencyScore(factor);

                return (ModelName: modelName, EfficiencyScore: score);
            })
            .OrderByDescending(result => result.EfficiencyScore)
            .ToList();

        EfficiencyScore = rankedModels.FirstOrDefault().EfficiencyScore;

        return rankedModels;
    }

    private static double CalculateEfficiencyScore(EnvironmentalFactor factor)
    {
        var weightedImpact =
            (factor.EnergyFactorPerQuery * 0.5) +
            (factor.Co2FactorPerQuery * 0.3) +
            (factor.WaterFactorPerQuery * 0.2);

        return Math.Round(100 / (1 + weightedImpact), 2);
    }
}
