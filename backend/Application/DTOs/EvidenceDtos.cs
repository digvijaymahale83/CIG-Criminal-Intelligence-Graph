namespace Application.DTOs;

public class EvidenceDto
{
    public string Id { get; set; } = string.Empty;
    public string? CaseId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public string Sha256Hash { get; set; } = string.Empty;
    public string? UploadedById { get; set; }
    public string UploadedByName { get; set; } = string.Empty;
    public DateTime UploadedAtUtc { get; set; }
    public string ProcessingStatus { get; set; } = "UPLOADED";
    public string Clearance { get; set; } = "RESTRICTED";
    public string Description { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public string? ParentEvidenceId { get; set; }
    public string? ExtractionJobId { get; set; }
}

public class EvidenceUploadResultDto
{
    public string Id { get; set; } = string.Empty;
    public string? CaseId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Sha256Hash { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public string UploadedBy { get; set; } = string.Empty;
    public string Clearance { get; set; } = "RESTRICTED";
    public DateTime CreatedAt { get; set; }
    public long SizeBytes { get; set; }
    public int Version { get; set; } = 1;
    public string? ParentEvidenceId { get; set; }
    public string ProcessingStatus { get; set; } = "UPLOADED";
}
