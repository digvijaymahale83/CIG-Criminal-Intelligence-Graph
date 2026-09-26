using System.Threading;
using System.Threading.Tasks;
using Application.DTOs;

namespace Application.Common.Interfaces;

public interface ITranslationService
{
    Task<TranslationResponseDto> TranslateEvidenceTextAsync(
        TranslationRequestDto request,
        CancellationToken cancellationToken = default);

    bool IsProviderConfigured();
}
