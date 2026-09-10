namespace AIEnvironmentalTracker.Web.Models;

public class AIUsageLog
{
    public int LogId { get; set; }
    public string? UserId { get; set; } // Relasi ke Identity User
    public string? ModelName { get; set; }
    public int InputTokenCount { get; set; }
    public int OutputTokenCount { get; set; }
    public int TotalTokenCount { get; set; }
    public double EnergyUsage { get; set; }
    public double CO2Emission { get; set; }
    public double WaterUsage { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}