namespace Domain.Entities;

public class EntityItem
{
    public string Id { get; set; } = string.Empty;
    public string? CaseId { get; set; }
    public string Type { get; set; } = "PERSON"; // PERSON, PHONE, VEHICLE, LOCATION, ORGANIZATION, ACCOUNT, DEVICE, DOCUMENT, EVENT
    public string CanonicalName { get; set; } = string.Empty;
    public string NormalizedValue { get; set; } = string.Empty;
    public double Confidence { get; set; } = 1.0;
    public string VerificationStatus { get; set; } = "UNVERIFIED"; // UNVERIFIED, VERIFIED, REJECTED
    public string? RiskLevel { get; set; } = "MEDIUM"; // LOW, MEDIUM, HIGH, CRITICAL
    public string Description { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? Country { get; set; } = "India";
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? GeocodePrecision { get; set; }
    public string? GeocodeSource { get; set; }
    public string? PhoneNumber { get; set; }
    public string? VehicleNumber { get; set; }
    public string? AccountNumber { get; set; }
    public string? BankName { get; set; }
    public string? AliasesJson { get; set; }
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation
    public Case? Case { get; set; }

    public List<string> GetAliases()
    {
        if (string.IsNullOrWhiteSpace(AliasesJson)) return new List<string>();
        try
        {
            var list = System.Text.Json.JsonSerializer.Deserialize<List<string>>(AliasesJson);
            return list ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }
}

