namespace AIEnvironmentalTracker.Domain.Models;

public class AIUsageLog
{
    public int LogId { get; set; }
    public string DetectedApp { get; set; } = string.Empty;
    public double SessionDuration { get; set; }
    public int QueryCount { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    public void ScanDeviceActivity() { /* TODO */ }
    public void CreateUsageLog() { /* TODO */ }
}
