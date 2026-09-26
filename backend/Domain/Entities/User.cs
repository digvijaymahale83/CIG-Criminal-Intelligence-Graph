namespace Domain.Entities;

public class User
{
    public string Id { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = "INVESTIGATOR"; // ADMIN, INVESTIGATOR, ANALYST
    public string BadgeNumber { get; set; } = string.Empty;
    public string Agency { get; set; } = "Maharashtra Police";
    public string Rank { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
