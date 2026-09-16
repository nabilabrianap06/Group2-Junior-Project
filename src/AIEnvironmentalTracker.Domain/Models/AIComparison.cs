using System.Collections.Generic;

namespace AIEnvironmentalTracker.Domain.Models;

public class AIComparison
{
    public int ComparisonId { get; set; }
    public List<string> ModelList { get; set; } = new();
    public double EfficiencyScore { get; set; }

    public void CompareModels() { /* TODO */ }
}
