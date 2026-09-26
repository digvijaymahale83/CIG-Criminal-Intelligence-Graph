using Application.DTOs;

namespace Application.Common.Interfaces;

public interface IEntityService
{
    Task<List<EntityDto>> GetEntitiesAsync(string? caseId, string? type, string? risk, string? search, CancellationToken cancellationToken = default);
    Task<EntityDto?> GetEntityByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<EntityDto> CreateEntityAsync(CreateEntityDto dto, string createdById, string createdByName, string? ipAddress, CancellationToken cancellationToken = default);
}
