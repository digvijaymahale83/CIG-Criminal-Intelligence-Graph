namespace Application.DTOs;

public class EntityDto
{
    public string Id { get; set; } = string.Empty;
    public string? CaseId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string CanonicalName { get; set; } = string.Empty;
    public string Name => CanonicalName; // compatibility alias
    public string NormalizedValue { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public string VerificationStatus { get; set; } = "UNVERIFIED";
    public string? RiskLevel { get; set; }
    public string? Risk => RiskLevel; // compatibility alias
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? VehicleNumber { get; set; }
    public string? AccountNumber { get; set; }
    public string? BankName { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public class CreateEntityDto
{
    public string? CaseId { get; set; }
    public string Type { get; set; } = "PERSON";
    public string Name { get; set; } = string.Empty;
    public string? CanonicalName { get; set; }
    public string? Risk { get; set; }
    public string? Description { get; set; }
    public string? Location { get; set; }
    public string? District { get; set; }
    public string? Phone { get; set; }
    public string? VehicleNumber { get; set; }
    public string? AccountNumber { get; set; }
    public string? Bank { get; set; }
}
