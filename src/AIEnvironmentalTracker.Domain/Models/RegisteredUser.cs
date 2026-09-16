namespace AIEnvironmentalTracker.Domain.Models;

public class RegisteredUser : User
{
    public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;

    public void RecordAIUsage() { /* TODO */ }
    public void ViewCarbonFootprint() { /* TODO */ }
    public void CompareAIModels() { /* TODO */ }
    public void ViewRecommendations() { /* TODO */ }
}
