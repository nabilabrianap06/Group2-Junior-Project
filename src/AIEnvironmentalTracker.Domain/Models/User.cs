namespace AIEnvironmentalTracker.Domain.Models;

public class User
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "User";

    public void Login() { /* TODO */ }
    public void RegisterAccount() { /* TODO */ }
}
