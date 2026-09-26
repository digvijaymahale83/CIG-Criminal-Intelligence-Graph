namespace Application.DTOs;

public class ExtractedEntityDto
{
    public string Id { get; set; } = string.Empty;
    public string ExtractionJobId { get; set; } = string.Empty;
    public string EvidenceId { get; set; } = string.Empty;
    public string? CaseId { get; set; }
    public string EntityType { get; set; } = "PERSON";
    public string RawValue { get; set; } = string.Empty;
    public string NormalizedValue { get; set; } = string.Empty;
    public double Confidence { get; set; } = 1.0;
    public string SourceLocation { get; set; } = string.Empty;
    public string ReviewStatus { get; set; } = "PENDING"; // PENDING, APPROVED, REJECTED
    public string? PromotedEntityId { get; set; }
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class ExtractedRelationshipDto
{
    public string Id { get; set; } = string.Empty;
    public string ExtractionJobId { get; set; } = string.Empty;
    public string EvidenceId { get; set; } = string.Empty;
    public string? CaseId { get; set; }
    public string? SourceExtractedEntityId { get; set; }
    public string? TargetExtractedEntityId { get; set; }
    public string SourceNormalizedValue { get; set; } = string.Empty;
    public string TargetNormalizedValue { get; set; } = string.Empty;
    public string RelationshipType { get; set; } = "ASSOCIATED_WITH";
    public double Confidence { get; set; } = 1.0;
    public string SourceLocation { get; set; } = string.Empty;
    public string ReviewStatus { get; set; } = "PENDING"; // PENDING, APPROVED, REJECTED
    public string? PromotedRelationshipId { get; set; }
    public string? ReviewedBy { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public class ExtractedEventDto
{
    public string Id { get; set; } = string.Empty;
    public string ExtractionJobId { get; set; } = string.Empty;
    public string EvidenceId { get; set; } = string.Empty;
    public string? CaseId { get; set; }
    public string EventType { get; set; } = "INCIDENT";
    public DateTime? EventTimestampUtc { get; set; }
    public string? Location { get; set; }
    public string? RelatedEntitiesJson { get; set; }
    public double Confidence { get; set; } = 1.0;
    public string SourceLocation { get; set; } = string.Empty;
    public string ReviewStatus { get; set; } = "PENDING";
    public DateTime CreatedAtUtc { get; set; }
}

public class ExtractionResultDto
{
    public string EvidenceId { get; set; } = string.Empty;
    public string? CaseId { get; set; }
    public string Status { get; set; } = "REVIEW_REQUIRED";
    public string? ErrorMessage { get; set; }
    public string? RawTextSnippet { get; set; }
    public List<ExtractedEntityDto> Entities { get; set; } = new();
    public List<ExtractedRelationshipDto> Relationships { get; set; } = new();
    public List<ExtractedEventDto> Events { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class ApproveEntityRequestDto
{
    public string? EntityId { get; set; }
    public string? CorrectedRawValue { get; set; }
    public string? CorrectedNormalizedValue { get; set; }
}

public class RejectEntityRequestDto
{
    public string? EntityId { get; set; }
    public string? Reason { get; set; }
}

public class EditEntityRequestDto
{
    public string RawValue { get; set; } = string.Empty;
    public string? NormalizedValue { get; set; }
}

public class ApproveRelationshipRequestDto
{
    public string? RelationshipId { get; set; }
}

public class RejectRelationshipRequestDto
{
    public string? RelationshipId { get; set; }
    public string? Reason { get; set; }
}

public class IntegrityCheckResultDto
{
    public string EvidenceId { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Status { get; set; } = "VALID"; // VALID, TAMPERED, FILE_MISSING
    public string StoredHash { get; set; } = string.Empty;
    public string ComputedHash { get; set; } = string.Empty;
    public bool HashesMatch { get; set; }
    public long FileSizeBytes { get; set; }
    public DateTime CheckedAtUtc { get; set; } = DateTime.UtcNow;
    public string Message { get; set; } = string.Empty;
}

public class ProcessEvidenceRequestDto
{
    public string? EvidenceId { get; set; }
    public bool ForceReprocess { get; set; } = false;
}
