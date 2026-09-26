using System;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v1/translation")]
public class TranslationController : ControllerBase
{
    private readonly ITranslationService _translationService;

    public TranslationController(ITranslationService translationService)
    {
        _translationService = translationService;
    }

    /// <summary>
    /// Translates evidence text or investigative documentation into Marathi or English.
    /// Note: Machine translations are non-authoritative references. Original evidence bytes,
    /// hashes, and database records remain immutable.
    /// </summary>
    [HttpPost("translate")]
    [ProducesResponseType(typeof(TranslationResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TranslateText(
        [FromBody] TranslationRequestDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return BadRequest(new { error = "Text to translate is required." });
        }

        var result = await _translationService.TranslateEvidenceTextAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Checks if an external cloud translation provider is configured.
    /// </summary>
    [HttpGet("provider-status")]
    public IActionResult GetProviderStatus()
    {
        return Ok(new
        {
            isCloudProviderConfigured = _translationService.IsProviderConfigured(),
            defaultFallback = "SystemDictionary (English <-> Marathi)",
            supportedLanguages = new[] { "en", "mr", "hi", "ta", "te", "bn", "gu", "kn" }
        });
    }
}
