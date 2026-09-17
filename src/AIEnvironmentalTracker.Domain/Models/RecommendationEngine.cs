namespace AIEnvironmentalTracker.Domain.Models;

public class RecommendationEngine
{
    public int RecommendationId { get; set; }
    public string AdviceText { get; set; } = string.Empty;
    public string TargetMetric { get; set; } = string.Empty;

    public void FetchRecommendations() { /* TODO */ }
}
