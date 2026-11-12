namespace AIEnvironmentalTracker.Web.Data;

public class UsageRecord
{
    public int Id { get; set; }
    public string ApplicationUserId { get; set; } = string.Empty;
    public string AiModelName { get; set; } = string.Empty;
    public int QueryCount { get; set; }
    public decimal WaterPerQuery { get; set; }
    public decimal Co2PerQuery { get; set; }
    public decimal EnergyPerQuery { get; set; }
    public decimal TotalWater { get; set; }
    public decimal TotalCo2 { get; set; }
    public decimal TotalEnergy { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
