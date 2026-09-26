using Application.Common.Interfaces;
using Application.DTOs;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class EntityService : IEntityService
{
    private readonly IAppDbContext _dbContext;
    private readonly IAuditService _auditService;
    private readonly ILogger<EntityService> _logger;

    public EntityService(IAppDbContext dbContext, IAuditService auditService, ILogger<EntityService> logger)
    {
        _dbContext = dbContext;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<List<EntityDto>> GetEntitiesAsync(string? caseId, string? type, string? risk, string? search, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Entities.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(caseId))
        {
            query = query.Where(e => e.CaseId == caseId);
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            query = query.Where(e => e.Type.ToLower() == type.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(risk))
        {
            query = query.Where(e => e.RiskLevel != null && e.RiskLevel.ToLower() == risk.ToLower());
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var s = search.Trim().ToLower();
            query = query.Where(e => e.CanonicalName.ToLower().Contains(s) || e.Description.ToLower().Contains(s) || e.Location.ToLower().Contains(s));
        }

        return await query
            .OrderByDescending(e => e.Confidence)
            .ThenBy(e => e.CanonicalName)
            .Select(e => new EntityDto
            {
                Id = e.Id,
                CaseId = e.CaseId,
                Type = e.Type,
                CanonicalName = e.CanonicalName,
                NormalizedValue = e.NormalizedValue,
                Confidence = e.Confidence,
                VerificationStatus = e.VerificationStatus,
                RiskLevel = e.RiskLevel,
                Description = e.Description,
                Location = e.Location,
                District = e.District,
                PhoneNumber = e.PhoneNumber,
                VehicleNumber = e.VehicleNumber,
                AccountNumber = e.AccountNumber,
                BankName = e.BankName,
                CreatedAtUtc = e.CreatedAtUtc,
                UpdatedAtUtc = e.UpdatedAtUtc
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<EntityDto?> GetEntityByIdAsync(string id, CancellationToken cancellationToken = default)
    {
        var e = await _dbContext.Entities
            .AsNoTracking()
            .FirstOrDefaultAsync(item => item.Id == id, cancellationToken);

        if (e == null) return null;

        return new EntityDto
        {
            Id = e.Id,
            CaseId = e.CaseId,
            Type = e.Type,
            CanonicalName = e.CanonicalName,
            NormalizedValue = e.NormalizedValue,
            Confidence = e.Confidence,
            VerificationStatus = e.VerificationStatus,
            RiskLevel = e.RiskLevel,
            Description = e.Description,
            Location = e.Location,
            District = e.District,
            PhoneNumber = e.PhoneNumber,
            VehicleNumber = e.VehicleNumber,
            AccountNumber = e.AccountNumber,
            BankName = e.BankName,
            CreatedAtUtc = e.CreatedAtUtc,
            UpdatedAtUtc = e.UpdatedAtUtc
        };
    }

    public async Task<EntityDto> CreateEntityAsync(CreateEntityDto dto, string createdById, string createdByName, string? ipAddress, CancellationToken cancellationToken = default)
    {
        var entityName = !string.IsNullOrWhiteSpace(dto.CanonicalName) ? dto.CanonicalName : dto.Name;
        var entityId = $"ent-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6]}";

        var entity = new EntityItem
        {
            Id = entityId,
            CaseId = dto.CaseId,
            Type = dto.Type.ToUpperInvariant(),
            CanonicalName = entityName,
            NormalizedValue = entityName.Trim().ToLowerInvariant(),
            Confidence = 1.0,
            VerificationStatus = "VERIFIED",
            RiskLevel = dto.Risk ?? "MEDIUM",
            Description = dto.Description ?? string.Empty,
            Location = dto.Location ?? string.Empty,
            District = dto.District ?? string.Empty,
            PhoneNumber = dto.Phone,
            VehicleNumber = dto.VehicleNumber,
            AccountNumber = dto.AccountNumber,
            BankName = dto.Bank,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Entities.Add(entity);

        if (!string.IsNullOrEmpty(dto.CaseId))
        {
            var caseRecord = await _dbContext.Cases.FirstOrDefaultAsync(c => c.Id == dto.CaseId, cancellationToken);
            if (caseRecord != null)
            {
                caseRecord.EntityCount++;
                caseRecord.UpdatedAtUtc = DateTime.UtcNow;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditService.LogAsync(
            createdById,
            createdByName,
            "ENTITY_CREATED",
            "entity",
            entityId,
            $"Added entity: {entityName} ({dto.Type})",
            null,
            ipAddress,
            cancellationToken);

        return new EntityDto
        {
            Id = entity.Id,
            CaseId = entity.CaseId,
            Type = entity.Type,
            CanonicalName = entity.CanonicalName,
            NormalizedValue = entity.NormalizedValue,
            Confidence = entity.Confidence,
            VerificationStatus = entity.VerificationStatus,
            RiskLevel = entity.RiskLevel,
            Description = entity.Description,
            Location = entity.Location,
            District = entity.District,
            PhoneNumber = entity.PhoneNumber,
            VehicleNumber = entity.VehicleNumber,
            AccountNumber = entity.AccountNumber,
            BankName = entity.BankName,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc
        };
    }
}
