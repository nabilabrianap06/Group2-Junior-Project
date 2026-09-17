namespace AIEnvironmentalTracker.Domain.Models;

public class RegisteredUser : User
{
    // Attributes matching UML
    public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;

    // 1-to-Many Navigations based on UML associations
    public ICollection<AIUsageLog> UsageLogs { get; set; } = new List<AIUsageLog>();
    public ICollection<CarbonFootprintReport> CarbonFootprintReports { get; set; } = new List<CarbonFootprintReport>();
    public ICollection<RecommendationEngine> Recommendations { get; set; } = new List<RecommendationEngine>();
    public ICollection<AIComparison> AIComparisons { get; set; } = new List<AIComparison>();

    // Operations matching UML
    public void RecordAIUsage()
    {
        var log = new AIUsageLog();
        log.ScanDeviceActivity();
        log.CreateUsageLog();
        UsageLogs.Add(log);
    }

    public CarbonFootprintReport ViewCarbonFootprint()
    {
        return CarbonFootprintReports.LastOrDefault() ?? new CarbonFootprintReport();
    }

    public AIComparison CompareAIModels()
    {
        var comparison = new AIComparison();
        comparison.CompareModels();
        AIComparisons.Add(comparison);
        return comparison;
    }

    public IEnumerable<RecommendationEngine> ViewRecommendations()
    {
        return Recommendations;
    }
}
