using System.Text.Json;
using Application.Common.Interfaces;
using Application.DTOs;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class EvidenceService : IEvidenceService
{
    private readonly IAppDbContext _dbContext;
    private readonly IFileStorageService _storageService;
    private readonly IHashService _hashService;
    private readonly IAuditService _auditService;
    private readonly IExtractionService _extractionService;
    private readonly IIntegrityLedgerService _ledgerService;
    private readonly ILogger<EvidenceService> _logger;

    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".csv", ".xlsx", ".xls", ".txt", ".json", ".jpg", ".jpeg", ".png"
    };

    public EvidenceService(
        IAppDbContext dbContext,
        IFileStorageService storageService,
        IHashService hashService,
        IAuditService auditService,
        IExtractionService extractionService,
        IIntegrityLedgerService ledgerService,
        ILogger<EvidenceService> logger)
    {
        _dbContext = dbContext;
        _storageService = storageService;
        _hashService = hashService;
        _auditService = auditService;
        _extractionService = extractionService;
        _ledgerService = ledgerService;
        _logger = logger;
    }

    public async Task<List<EvidenceDto>> GetEvidenceAsync(string? caseId, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.EvidenceItems.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(caseId))
        {
            query = query.Where(e => e.CaseId == caseId);
        }

        return await query
            .OrderByDescending(e => e.UploadedAtUtc)
            .Select(e => new EvidenceDto
            {
                Id = e.Id,
                CaseId = e.CaseId,
                FileName = e.FileName,
                MimeType = e.MimeType,
                FileSize = e.FileSize,
                StoragePath = e.StoragePath,
                Sha256Hash = e.Sha256Hash,
                UploadedById = e.UploadedById,
                UploadedByName = e.UploadedByName,
                UploadedAtUtc = e.UploadedAtUtc,
                ProcessingStatus = e.ProcessingStatus,
                Clearance = e.Clearance,
                Description = e.Description,
                Version = e.Version,
                ParentEvidenceId = e.ParentEvidenceId
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<EvidenceDto?> GetEvidenceByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.EvidenceItems
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

        if (item == null) return null;

        var latestJob = await _dbContext.ExtractionJobs
            .AsNoTracking()
            .Where(j => j.EvidenceId == id)
            .OrderByDescending(j => j.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return new EvidenceDto
        {
            Id = item.Id,
            CaseId = item.CaseId,
            FileName = item.FileName,
            MimeType = item.MimeType,
            FileSize = item.FileSize,
            StoragePath = item.StoragePath,
            Sha256Hash = item.Sha256Hash,
            UploadedById = item.UploadedById,
            UploadedByName = item.UploadedByName,
            UploadedAtUtc = item.UploadedAtUtc,
            ProcessingStatus = item.ProcessingStatus,
            Clearance = item.Clearance,
            Description = item.Description,
            Version = item.Version,
            ParentEvidenceId = item.ParentEvidenceId,
            ExtractionJobId = latestJob?.Id
        };
    }

    public async Task<EvidenceUploadResultDto> UploadEvidenceAsync(
        Stream fileStream,
        string originalFileName,
        string mimeType,
        long fileSize,
        string? caseId,
        string description,
        string clearance,
        string uploaderId,
        string uploaderName,
        string? ipAddress,
        string? parentEvidenceId = null,
        CancellationToken cancellationToken = default)
    {
        // 1. Validation
        var extension = Path.GetExtension(originalFileName);
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
        {
            throw new ArgumentException($"File type '{extension}' is not permitted. Supported: PDF, CSV, XLSX, TXT, JSON, JPG, PNG.");
        }

        if (fileSize > 50 * 1024 * 1024)
        {
            throw new ArgumentException("Evidence file exceeds 50 MB maximum permitted size limit.");
        }

        // 2. Real byte-level SHA-256 calculation
        var sha256Hex = _hashService.ComputeSha256Hex(fileStream);

        // 3. Storage
        var storagePath = await _storageService.SaveFileAsync(fileStream, originalFileName, cancellationToken);

        // 4. Versioning lineage
        int version = 1;
        if (!string.IsNullOrEmpty(parentEvidenceId))
        {
            var parent = await _dbContext.EvidenceItems.FirstOrDefaultAsync(e => e.Id == parentEvidenceId, cancellationToken);
            if (parent != null)
            {
                version = parent.Version + 1;
            }
        }
        else if (!string.IsNullOrEmpty(caseId))
        {
            var existingWithSameName = await _dbContext.EvidenceItems
                .Where(e => e.CaseId == caseId && e.FileName == originalFileName)
                .OrderByDescending(e => e.Version)
                .FirstOrDefaultAsync(cancellationToken);
            if (existingWithSameName != null)
            {
                version = existingWithSameName.Version + 1;
                parentEvidenceId = existingWithSameName.Id;
            }
        }

        // 5. Persistence
        var evidenceId = $"ev-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6]}";

        var evidence = new Evidence
        {
            Id = evidenceId,
            CaseId = caseId,
            FileName = originalFileName,
            MimeType = mimeType,
            FileSize = fileSize,
            StoragePath = storagePath,
            Sha256Hash = sha256Hex,
            UploadedById = uploaderId,
            UploadedByName = uploaderName,
            UploadedAtUtc = DateTime.UtcNow,
            ProcessingStatus = "QUEUED",
            Clearance = string.IsNullOrWhiteSpace(clearance) ? "RESTRICTED" : clearance,
            Description = description ?? string.Empty,
            Version = version,
            ParentEvidenceId = parentEvidenceId
        };

        _dbContext.EvidenceItems.Add(evidence);

        // Update case evidence count if associated
        if (!string.IsNullOrEmpty(caseId))
        {
            var caseRecord = await _dbContext.Cases.FirstOrDefaultAsync(c => c.Id == caseId, cancellationToken);
            if (caseRecord != null)
            {
                caseRecord.EvidenceCount++;
                caseRecord.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        // 6. Audit log
        var meta = $"{{\"fileName\":\"{originalFileName}\",\"sha256\":\"{sha256Hex}\",\"size\":{fileSize},\"version\":{version}}}";
        await _auditService.LogAsync(
            uploaderId,
            uploaderName,
            "EVIDENCE_UPLOADED",
            "Evidence",
            evidenceId,
            $"Uploaded evidence: {originalFileName} (v{version}, {(fileSize / 1024.0):F1} KB) SHA-256: {sha256Hex[..16]}...",
            meta,
            ipAddress,
            cancellationToken);

        _logger.LogInformation("Evidence {EvidenceId} (v{Version}) uploaded with authentic SHA-256: {Sha256}", evidenceId, version, sha256Hex);

        // 7. Append block to Evidence Integrity Ledger (Append-Only Blockchain)
        try
        {
            var ledgerAction = version == 1 ? "UPLOAD" : "VERSION_CREATED";
            await _ledgerService.AppendBlockAsync(
                evidenceId,
                version,
                sha256Hex,
                ledgerAction,
                uploaderId,
                uploaderName,
                meta,
                ipAddress,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register evidence {EvidenceId} on the integrity ledger: {Message}", evidenceId, ex.Message);
            // Non-fatal: evidence persistence succeeded, but unledgered state can be reconciled via /reconciliation
        }

        // 8. Automatically queue extraction job
        try
        {
            await _extractionService.QueueExtractionJobAsync(evidenceId, uploaderId, uploaderName, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not automatically queue extraction for {EvidenceId}: {Message}", evidenceId, ex.Message);
        }

        var extUpper = extension.TrimStart('.').ToUpperInvariant();
        return new EvidenceUploadResultDto
        {
            Id = evidenceId,
            CaseId = caseId,
            FileName = originalFileName,
            FileType = extUpper,
            Description = description ?? string.Empty,
            Sha256Hash = sha256Hex,
            StoragePath = storagePath,
            UploadedBy = uploaderName,
            Clearance = evidence.Clearance,
            CreatedAt = evidence.UploadedAtUtc,
            SizeBytes = fileSize,
            Version = version,
            ParentEvidenceId = parentEvidenceId,
            ProcessingStatus = "QUEUED"
        };
    }

    public async Task<IntegrityCheckResultDto> VerifyIntegrityAsync(
        string evidenceId,
        string actorId,
        string actorName,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var evidence = await _dbContext.EvidenceItems.FirstOrDefaultAsync(e => e.Id == evidenceId, cancellationToken);
        if (evidence == null)
        {
            return new IntegrityCheckResultDto
            {
                EvidenceId = evidenceId,
                Status = "FILE_MISSING",
                Message = "Evidence record not found in database."
            };
        }

        var stream = await _storageService.GetFileAsync(evidence.StoragePath, cancellationToken);
        if (stream == null)
        {
            return new IntegrityCheckResultDto
            {
                EvidenceId = evidence.Id,
                FileName = evidence.FileName,
                Status = "FILE_MISSING",
                StoredHash = evidence.Sha256Hash,
                ComputedHash = string.Empty,
                HashesMatch = false,
                FileSizeBytes = 0,
                CheckedAtUtc = DateTime.UtcNow,
                Message = "Raw evidence file is missing from secure storage."
            };
        }

        using (stream)
        {
            var computedHash = _hashService.ComputeSha256Hex(stream);
            var isValid = string.Equals(computedHash, evidence.Sha256Hash, StringComparison.OrdinalIgnoreCase);
            var status = isValid ? "VALID" : "TAMPERED";

            await _auditService.LogAsync(
                actorId,
                actorName,
                "EVIDENCE_INTEGRITY_CHECKED",
                "Evidence",
                evidence.Id,
                $"Integrity check performed: status is {status}. Stored: {evidence.Sha256Hash[..16]}..., Computed: {computedHash[..16]}...",
                JsonSerializer.Serialize(new { storedHash = evidence.Sha256Hash, computedHash, status, isValid }),
                ipAddress,
                cancellationToken);

            return new IntegrityCheckResultDto
            {
                EvidenceId = evidence.Id,
                FileName = evidence.FileName,
                Status = status,
                StoredHash = evidence.Sha256Hash,
                ComputedHash = computedHash,
                HashesMatch = isValid,
                FileSizeBytes = evidence.FileSize,
                CheckedAtUtc = DateTime.UtcNow,
                Message = isValid
                    ? "Cryptographic SHA-256 byte digest matches stored signature. Evidence is intact."
                    : "TAMPERING DETECTED: File bytes do not match stored cryptographic digest."
            };
        }
    }

    public async Task<bool> RetryProcessingAsync(
        string evidenceId,
        string actorId,
        string actorName,
        string? ipAddress,
        CancellationToken cancellationToken = default)
    {
        var evidence = await _dbContext.EvidenceItems.FirstOrDefaultAsync(e => e.Id == evidenceId, cancellationToken);
        if (evidence == null) return false;

        evidence.ProcessingStatus = "QUEUED";
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            actorId,
            actorName,
            "EVIDENCE_PROCESSING_RETRIED",
            "Evidence",
            evidence.Id,
            $"Processing retry requested by {actorName}",
            null,
            ipAddress,
            cancellationToken);

        await _extractionService.QueueExtractionJobAsync(evidence.Id, actorId, actorName, cancellationToken);
        return true;
    }
}
