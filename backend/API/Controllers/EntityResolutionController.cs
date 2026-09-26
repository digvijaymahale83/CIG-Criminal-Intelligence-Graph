using System.Security.Claims;
using Application.Common.Interfaces;
using Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v1/entity-resolution")]
[Authorize]
public class EntityResolutionController : ControllerBase
{
    private readonly IEntityResolutionService _resolutionService;
    private readonly ILogger<EntityResolutionController> _logger;

    public EntityResolutionController(
        IEntityResolutionService resolutionService,
        ILogger<EntityResolutionController> logger)
    {
        _resolutionService = resolutionService;
        _logger = logger;
    }

    /// <summary>
    /// Executes a controlled batch analysis across authorized cases to identify potential cross-case entity matches.
    /// </summary>
    [HttpPost("run")]
    [Authorize(Roles = "ADMIN,INVESTIGATOR,ANALYST")]
    public async Task<ActionResult<BatchResolutionResultDto>> RunBatchResolution(
        [FromBody] BatchResolutionRequestDto request,
        CancellationToken cancellationToken)
    {
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "USR-ANON";
        var actorName = User.FindFirstValue(ClaimTypes.Name) ?? "Investigator";

        var result = await _resolutionService.RunBatchResolutionAsync(
            request.CaseId,
            request.EntityTypes,
            request.MinimumScore,
            actorId,
            actorName,
            cancellationToken);

        return Ok(result);
    }

    /// <summary>
    /// Retrieves candidate cross-case entity matches with optional filtering by case, status, type, and score.
    /// </summary>
    [HttpGet("candidates")]
    public async Task<ActionResult<List<EntityMatchCandidateDto>>> GetCandidates(
        [FromQuery] string? caseId,
        [FromQuery] string? status,
        [FromQuery] string? entityType,
        [FromQuery] double? minimumScore,
        CancellationToken cancellationToken)
    {
        var candidates = await _resolutionService.GetCandidatesAsync(
            caseId,
            status,
            entityType,
            minimumScore,
            cancellationToken);

        return Ok(candidates);
    }

    /// <summary>
    /// Retrieves full candidate detail by ID.
    /// </summary>
    [HttpGet("candidates/{id}")]
    public async Task<ActionResult<EntityMatchCandidateDto>> GetCandidateById(
        string id,
        CancellationToken cancellationToken)
    {
        var candidate = await _resolutionService.GetCandidateByIdAsync(id, cancellationToken);
        if (candidate == null)
        {
            return NotFound(new { message = $"Entity resolution candidate '{id}' was not found." });
        }

        return Ok(candidate);
    }

    /// <summary>
    /// Retrieves a structured side-by-side comparison payload between source and target entities.
    /// </summary>
    [HttpGet("candidates/{id}/compare")]
    public async Task<ActionResult<CandidateComparisonDto>> GetCandidateComparison(
        string id,
        CancellationToken cancellationToken)
    {
        var comparison = await _resolutionService.GetCandidateComparisonAsync(id, cancellationToken);
        if (comparison == null)
        {
            return NotFound(new { message = $"Candidate comparison for '{id}' could not be assembled." });
        }

        return Ok(comparison);
    }

    /// <summary>
    /// Submits human investigator approval for a cross-case entity match candidate.
    /// Promotes verified connection and synchronizes cross-case links into Neo4j graph.
    /// </summary>
    [HttpPost("candidates/{id}/approve")]
    [Authorize(Roles = "ADMIN,INVESTIGATOR")]
    public async Task<ActionResult> ApproveCandidate(
        string id,
        [FromBody] CandidateReviewRequestDto? request,
        CancellationToken cancellationToken)
    {
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "USR-ANON";
        var actorName = User.FindFirstValue(ClaimTypes.Name) ?? "Investigator";
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var review = request ?? new CandidateReviewRequestDto { Status = "APPROVED" };
        review.Status = "APPROVED";

        var success = await _resolutionService.ReviewCandidateAsync(
            id,
            review,
            actorId,
            actorName,
            ipAddress,
            cancellationToken);

        if (!success)
        {
            return NotFound(new { message = $"Candidate '{id}' not found." });
        }

        return Ok(new { message = "Candidate approved successfully and promoted to verified cross-case intelligence.", candidateId = id });
    }

    /// <summary>
    /// Submits human investigator rejection for a candidate entity match.
    /// Prevents cross-case promotion and records reason in audit trail.
    /// </summary>
    [HttpPost("candidates/{id}/reject")]
    [Authorize(Roles = "ADMIN,INVESTIGATOR")]
    public async Task<ActionResult> RejectCandidate(
        string id,
        [FromBody] CandidateReviewRequestDto? request,
        CancellationToken cancellationToken)
    {
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "USR-ANON";
        var actorName = User.FindFirstValue(ClaimTypes.Name) ?? "Investigator";
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var review = request ?? new CandidateReviewRequestDto { Status = "REJECTED" };
        review.Status = "REJECTED";

        var success = await _resolutionService.ReviewCandidateAsync(
            id,
            review,
            actorId,
            actorName,
            ipAddress,
            cancellationToken);

        if (!success)
        {
            return NotFound(new { message = $"Candidate '{id}' not found." });
        }

        return Ok(new { message = "Candidate rejected successfully.", candidateId = id });
    }
}
