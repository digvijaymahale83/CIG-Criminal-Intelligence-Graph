using Application.DTOs;

namespace Application.Common.Interfaces;

public interface ICaseService
{
    Task<List<CaseDto>> GetCasesAsync(string? status, string? search, CancellationToken cancellationToken = default);
    Task<CaseDto?> GetCaseByIdAsync(string id, CancellationToken cancellationToken = default);
    Task<CaseDto> CreateCaseAsync(CreateCaseDto dto, string createdById, string createdByName, string? ipAddress, CancellationToken cancellationToken = default);
    Task<CaseDto?> UpdateCaseAsync(string id, UpdateCaseDto dto, string updatedById, string updatedByName, string? ipAddress, CancellationToken cancellationToken = default);
    Task<bool> DeleteCaseAsync(string id, string actorId, string actorName, string? ipAddress, CancellationToken cancellationToken = default);
    Task<CaseStatsSummaryDto> GetStatsSummaryAsync(CancellationToken cancellationToken = default);
}
