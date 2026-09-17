namespace AIEnvironmentalTracker.Domain.Models;

public class AIUsageLog
{
    // Attributes matching UML
    public int LogId { get; set; }
    public string DetectedApp { get; set; } = string.Empty;
    public double SessionDuration { get; set; }
    public int QueryCount { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Foreign Key / Navigation back to RegisteredUser
    public int RegisteredUserId { get; set; }
    public RegisteredUser? RegisteredUser { get; set; }

    // Operations matching UML
    public void ScanDeviceActivity()
    {
        // Logic to monitor background running processes / AI applications
    }

    public void CreateUsageLog()
    {
        // Persistence or factory initialization logic
    }

    // Helper linking AIUsageLog to ImpactCalculator as shown by the dependency arrow
    public ImpactCalculator CalculateImpact(EnvironmentalFactor factors)
    {
        var calculator = new ImpactCalculator();
        calculator.ComputeImpact(this, factors);
        return calculator;
    }
}
