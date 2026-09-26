namespace Application.DTOs;

public class CaseDto
{
    public string Id { get; set; } = string.Empty;
    public string CaseNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Classification { get; set; } = "RESTRICTED";
    public string Priority { get; set; } = "Medium";
    public string Status { get; set; } = "Active";
    public string Category { get; set; } = "General";
    public string Jurisdiction { get; set; } = string.Empty;
    public string District { get; set; } = string.Empty;
    public string FirNumber { get; set; } = string.Empty;
    public string PoliceStation { get; set; } = string.Empty;
    public string? LeadOfficerId { get; set; }
    public string? LeadOfficerName { get; set; }
    public int EvidenceCount { get; set; }
    public int EntityCount { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public class CreateCaseDto
{
    public string? CaseNumber { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Classification { get; set; }
    public string? Priority { get; set; }
    public string? Category { get; set; }
    public string? Jurisdiction { get; set; }
    public string? District { get; set; }
    public string? FirNumber { get; set; }
    public string? PoliceStation { get; set; }
}

public class UpdateCaseDto
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Classification { get; set; }
    public string? Priority { get; set; }
    public string? Status { get; set; }
    public string? Category { get; set; }
    public string? Jurisdiction { get; set; }
    public string? District { get; set; }
}

public class CaseStatsSummaryDto
{
    public int ActiveInvestigations { get; set; }
    public int TotalEntities { get; set; }
    public int TotalInvestigations { get; set; }
    public int HighRiskAlerts { get; set; }
    public int TotalEvidence { get; set; }
    public int ConnectedNetworks { get; set; }
}
