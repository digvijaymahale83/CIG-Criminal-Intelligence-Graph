using System.Threading;
using System.Threading.Tasks;
using Application.DTOs;

namespace Application.Common.Interfaces;

public interface IDashboardService
{
    Task<DashboardDto> GetCaseDashboardAsync(
        string caseId,
        string userId,
        string userRole,
        CancellationToken cancellationToken = default);
}
