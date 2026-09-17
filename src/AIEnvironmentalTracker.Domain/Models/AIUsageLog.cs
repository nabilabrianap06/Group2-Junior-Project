namespace AIEnvironmentalTracker.Domain.Models;

public class AIUsageLog
{
    public int LogId { get; set; }

    // User Navigation
    public int UserId { get; set; }
    public User? User { get; set; }

    // Usage Data
    public string DetectedApp { get; set; } = string.Empty;
    public double SessionDuration { get; set; } // Duration in minutes
    public int QueryCount { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Environmental Factor Foreign Key (Optional: tracks which baseline factors were applied)
    public int? EnvironmentalFactorId { get; set; }
    public EnvironmentalFactor? EnvironmentalFactor { get; set; }

    // Domain Helper Methods
    public double CalculateEstimatedEnergyKWh(double energyPerQueryKWh, double energyPerMinuteKWh)
    {
        return (QueryCount * energyPerQueryKWh) + (SessionDuration * energyPerMinuteKWh);
    }

    public void UpdateSessionDetails(double addedDurationMinutes, int addedQueries)
    {
        if (addedDurationMinutes < 0 || addedQueries < 0)
            throw new ArgumentException("Added duration and query counts must be non-negative.");

        SessionDuration += addedDurationMinutes;
        QueryCount += addedQueries;
    }
}
