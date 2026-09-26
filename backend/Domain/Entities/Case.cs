namespace Domain.Entities;

public class Case
{
    public string Id { get; set; } = string.Empty;
    public string CaseNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Classification { get; set; } = "RESTRICTED";
    public string Priority { get; set; } = "Medium"; // Low, Medium, High, Critical
    public string Status { get; set; } = "Active"; // Active, Suspended, Closed, Archived
    public string Category { get; set; } = "General";
    public string Jurisdiction { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string FirNumber { get; set; } = string.Empty;
    public string PoliceStation { get; set; } = string.Empty;
    public string? LeadOfficerId { get; set; }
    public string? LeadOfficerName { get; set; }
    public int EvidenceCount { get; set; } = 0;
    public int EntityCount { get; set; } = 0;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<Evidence> EvidenceItems { get; set; } = new List<Evidence>();
    public ICollection<EntityItem> Entities { get; set; } = new List<EntityItem>();
}
