using System.Collections.Generic;
using System.Linq;

namespace AIEnvironmentalTracker.Domain.Models;

public class AIComparison
{
    public int ComparisonId { get; set; }
    public List<string> ModelList { get; set; } = new();
    public double EfficiencyScore { get; set; }

    public IReadOnlyList<(string ModelName, double EfficiencyScore)> CompareModels(IEnumerable<EnvironmentalFactor> availableFactors)
    {
        var rankedModels = ModelList
            .Where(modelName => !string.IsNullOrWhiteSpace(modelName))
            .Select(modelName =>
            {
                var factor = EnvironmentalFactor.GetFactorsByModel(modelName, availableFactors);
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
            (factor.EnergyFactorPerQuery * 0.5m) +
            (factor.Co2FactorPerQuery * 0.3m) +
            (factor.WaterFactorPerQuery * 0.2m);

        return (double)Math.Round(100m / (1m + weightedImpact), 2);
    }
}
