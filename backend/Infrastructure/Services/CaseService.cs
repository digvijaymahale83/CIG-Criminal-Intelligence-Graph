using Application.Common.Interfaces;
using Application.DTOs;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class CaseService : ICaseService
{
    private readonly IAppDbContext _dbContext;
    private readonly IAuditService _auditService;
    private readonly ILogger<CaseService> _logger;

    public CaseService(IAppDbContext dbContext, IAuditService auditService, ILogger<CaseService> logger)
    {
        _dbContext = dbContext;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<List<CaseDto>> GetCasesAsync(string? status, string? search, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Cases.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(status))
        {
            query = query.Where(c => c.Status.ToLower() == status.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(c => c.Title.ToLower().Contains(s) || c.CaseNumber.ToLower().Contains(s) || c.Description.ToLower().Contains(s));
        }

        return await query
            .OrderByDescending(c => c.UpdatedAtUtc)
            .Select(c => MapToDto(c))
            .ToListAsync(cancellationToken);
    }

    public async Task<CaseDto?> GetCaseByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var item = await _dbContext.Cases
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return item == null ? null : MapToDto(item);
    }

    public async Task<CaseDto> CreateCaseAsync(CreateCaseDto dto, string createdById, string createdByName, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var year = DateTime.UtcNow.Year;
        var existingCount = await _dbContext.Cases.CountAsync(cancellationToken);
        var catPrefix = string.IsNullOrWhiteSpace(dto.Category) ? "GEN" : dto.Category[..Math.Min(3, dto.Category.Length)].ToUpperInvariant();
        var caseNumber = !string.IsNullOrWhiteSpace(dto.CaseNumber)
            ? dto.CaseNumber
            : $"MH-{catPrefix}-{year}-{(existingCount + 1):D4}";

        var id = $"inv-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6]}";

        var newCase = new Case
        {
            Id = id,
            CaseNumber = caseNumber,
            Title = dto.Title,
            Description = dto.Description ?? string.Empty,
            Classification = dto.Classification ?? "RESTRICTED",
            Priority = dto.Priority ?? "Medium",
            Status = "Active",
            Category = dto.Category ?? "General",
            Jurisdiction = dto.Jurisdiction ?? string.Empty,
            District = dto.District ?? string.Empty,
            FirNumber = dto.FirNumber ?? string.Empty,
            PoliceStation = dto.PoliceStation ?? string.Empty,
            LeadOfficerId = createdById,
            LeadOfficerName = createdByName,
            EvidenceCount = 0,
            EntityCount = 0,
            CreatedBy = createdByName,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Cases.Add(newCase);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            createdById,
            createdByName,
            "CASE_CREATED",
            "case",
            id,
            $"Created investigation case: {caseNumber} - {dto.Title}",
            null,
            ipAddress,
            cancellationToken);

        _logger.LogInformation("Successfully created case {CaseId} ({CaseNumber})", id, caseNumber);
        return MapToDto(newCase);
    }

    public async Task<CaseDto?> UpdateCaseAsync(string id, UpdateCaseDto dto, string updatedById, string updatedByName, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.Cases.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (existing == null) return null;

        if (!string.IsNullOrWhiteSpace(dto.Title)) existing.Title = dto.Title;
        if (dto.Description != null) existing.Description = dto.Description;
        if (!string.IsNullOrWhiteSpace(dto.Classification)) existing.Classification = dto.Classification;
        if (!string.IsNullOrWhiteSpace(dto.Priority)) existing.Priority = dto.Priority;
        if (!string.IsNullOrWhiteSpace(dto.Status)) existing.Status = dto.Status;
        if (!string.IsNullOrWhiteSpace(dto.Category)) existing.Category = dto.Category;
        if (dto.Jurisdiction != null) existing.Jurisdiction = dto.Jurisdiction;
        if (dto.District != null) existing.District = dto.District;
        existing.UpdatedAtUtc = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            updatedById,
            updatedByName,
            "CASE_UPDATED",
            "case",
            id,
            $"Updated case details: {existing.CaseNumber}",
            null,
            ipAddress,
            cancellationToken);

        return MapToDto(existing);
    }

    public async Task<bool> DeleteCaseAsync(string id, string actorId, string actorName, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.Cases.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (existing == null) return false;

        _dbContext.Cases.Remove(existing);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            actorId,
            actorName,
            "CASE_DELETED",
            "case",
            id,
            $"Removed case file: {existing.CaseNumber}",
            null,
            ipAddress,
            cancellationToken);

        return true;
    }

    public async Task<CaseStatsSummaryDto> GetStatsSummaryAsync(CancellationToken cancellationToken = default)
    {
        var activeCount = await _dbContext.Cases.CountAsync(c => c.Status == "Active", cancellationToken);
        var totalCases = await _dbContext.Cases.CountAsync(cancellationToken);
        var totalEntities = await _dbContext.Entities.CountAsync(cancellationToken);
        var totalEvidence = await _dbContext.EvidenceItems.CountAsync(cancellationToken);
        var criticalAlerts = await _dbContext.Entities.CountAsync(e => e.RiskLevel == "CRITICAL", cancellationToken);
        var connectedNetworks = await _dbContext.Relationships.Select(r => r.CaseId).Distinct().CountAsync(cancellationToken);

        return new CaseStatsSummaryDto
        {
            ActiveInvestigations = activeCount,
            TotalInvestigations = totalCases,
            TotalEntities = totalEntities,
            TotalEvidence = totalEvidence,
            HighRiskAlerts = criticalAlerts,
            ConnectedNetworks = connectedNetworks > 0 ? connectedNetworks : (activeCount > 0 ? 1 : 0)
        };
    }

    private static CaseDto MapToDto(Case c) => new()
    {
        Id = c.Id,
        CaseNumber = c.CaseNumber,
        Title = c.Title,
        Description = c.Description,
        Classification = c.Classification,
        Priority = c.Priority,
        Status = c.Status,
        Category = c.Category,
        Jurisdiction = c.Jurisdiction,
        District = c.District,
        FirNumber = c.FirNumber,
        PoliceStation = c.PoliceStation,
        LeadOfficerId = c.LeadOfficerId,
        LeadOfficerName = c.LeadOfficerName,
        EvidenceCount = c.EvidenceCount,
        EntityCount = c.EntityCount,
        CreatedBy = c.CreatedBy,
        CreatedAtUtc = c.CreatedAtUtc,
        UpdatedAtUtc = c.UpdatedAtUtc
    };
}
