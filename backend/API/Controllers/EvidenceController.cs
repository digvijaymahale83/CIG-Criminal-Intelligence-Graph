using System.Security.Claims;
using Application.Common.Interfaces;
using Application.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v1/evidence")]
public class EvidenceController : ControllerBase
{
    private readonly IEvidenceService _evidenceService;
    private readonly IExtractionService _extractionService;
    private readonly IFileStorageService _storageService;
    private readonly ILogger<EvidenceController> _logger;

    public EvidenceController(
        IEvidenceService evidenceService,
        IExtractionService extractionService,
        IFileStorageService storageService,
        ILogger<EvidenceController> logger)
    {
        _evidenceService = evidenceService;
        _extractionService = extractionService;
        _storageService = storageService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<EvidenceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEvidence([FromQuery] string? caseId, [FromQuery] string? investigation_id, CancellationToken cancellationToken)
    {
        var targetCaseId = !string.IsNullOrEmpty(caseId) ? caseId : investigation_id;
        var list = await _evidenceService.GetEvidenceAsync(targetCaseId, cancellationToken);
        return Ok(list);
    }

    [HttpGet("{id}")]
    [ProducesResponseType(typeof(EvidenceDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEvidenceById(string id, CancellationToken cancellationToken)
    {
        var item = await _evidenceService.GetEvidenceByIdAsync(id, cancellationToken);
        if (item == null)
        {
            return NotFound(new { error = $"Evidence item with ID '{id}' was not found." });
        }

        return Ok(item);
    }

    [HttpGet("{id}/download")]
    public async Task<IActionResult> DownloadEvidence(string id, CancellationToken cancellationToken)
    {
        var item = await _evidenceService.GetEvidenceByIdAsync(id, cancellationToken);
        if (item == null)
        {
            return NotFound(new { error = $"Evidence '{id}' not found." });
        }

        var stream = await _storageService.GetFileAsync(item.StoragePath, cancellationToken);
        if (stream == null)
        {
            return NotFound(new { error = "Evidence file is missing on storage server." });
        }

        var contentType = !string.IsNullOrWhiteSpace(item.MimeType) ? item.MimeType : "application/octet-stream";
        return File(stream, contentType, item.FileName);
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(EvidenceUploadResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(50 * 1024 * 1024)] // 50MB
    public async Task<IActionResult> UploadEvidence(
        IFormFile? file,
        [FromForm] string? caseId,
        [FromForm] string? investigation_id,
        [FromForm] string? description,
        [FromForm] string? clearance,
        [FromForm] string? parentEvidenceId,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { error = "No evidence file was provided." });
        }

        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "usr-investigator";
        var actorName = User.FindFirstValue(ClaimTypes.Name) ?? "Investigating Officer";
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var targetCaseId = !string.IsNullOrEmpty(caseId) ? caseId : investigation_id;

        try
        {
            await using var stream = file.OpenReadStream();
            var result = await _evidenceService.UploadEvidenceAsync(
                stream,
                file.FileName,
                file.ContentType,
                file.Length,
                targetCaseId,
                description ?? string.Empty,
                clearance ?? "RESTRICTED",
                actorId,
                actorName,
                ipAddress,
                parentEvidenceId,
                cancellationToken);

            return CreatedAtAction(nameof(GetEvidenceById), new { id = result.Id }, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload evidence file: {FileName}", file.FileName);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = $"Evidence upload processing failed: {ex.Message}" });
        }
    }

    [HttpGet("{id}/extraction")]
    [ProducesResponseType(typeof(ExtractionResultDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetExtraction(string id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _extractionService.GetExtractionByEvidenceIdAsync(id, cancellationToken);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }

    [HttpPost("{id}/extraction/approve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ApproveExtractedItem(
        string id,
        [FromBody] ApproveEntityRequestDto request,
        CancellationToken cancellationToken)
    {
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "usr-investigator";
        var actorName = User.FindFirstValue(ClaimTypes.Name) ?? "Investigating Officer";
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        if (string.IsNullOrWhiteSpace(request.EntityId))
        {
            return BadRequest(new { error = "EntityId is required for entity approval." });
        }

        var success = await _extractionService.ApproveEntityAsync(
            id,
            request.EntityId,
            request.CorrectedRawValue,
            request.CorrectedNormalizedValue,
            actorId,
            actorName,
            ipAddress,
            cancellationToken);

        if (!success)
        {
            return NotFound(new { error = $"Entity '{request.EntityId}' was not found in evidence '{id}'." });
        }

        return Ok(new { message = "Entity approved and promoted to investigation knowledge graph.", entityId = request.EntityId });
    }

    [HttpPost("{id}/extraction/reject")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RejectExtractedItem(
        string id,
        [FromBody] RejectEntityRequestDto request,
        CancellationToken cancellationToken)
    {
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "usr-investigator";
        var actorName = User.FindFirstValue(ClaimTypes.Name) ?? "Investigating Officer";
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        if (string.IsNullOrWhiteSpace(request.EntityId))
        {
            return BadRequest(new { error = "EntityId is required for entity rejection." });
        }

        var success = await _extractionService.RejectEntityAsync(
            id,
            request.EntityId,
            request.Reason,
            actorId,
            actorName,
            ipAddress,
            cancellationToken);

        if (!success)
        {
            return NotFound(new { error = $"Entity '{request.EntityId}' was not found in evidence '{id}'." });
        }

        return Ok(new { message = "Entity rejected.", entityId = request.EntityId });
    }

    [HttpPost("{id}/extraction/entities/{entityId}/edit")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> EditExtractedEntity(
        string id,
        string entityId,
        [FromBody] EditEntityRequestDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RawValue))
        {
            return BadRequest(new { error = "RawValue cannot be empty." });
        }

        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "usr-investigator";
        var actorName = User.FindFirstValue(ClaimTypes.Name) ?? "Investigating Officer";
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var success = await _extractionService.EditEntityAsync(
            id,
            entityId,
            request.RawValue,
            request.NormalizedValue,
            actorId,
            actorName,
            ipAddress,
            cancellationToken);

        if (!success)
        {
            return NotFound(new { error = $"Entity '{entityId}' was not found in evidence '{id}'." });
        }

        return Ok(new { message = "Entity value corrected successfully.", entityId });
    }

    [HttpPost("{id}/extraction/relationships/approve")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ApproveExtractedRelationship(
        string id,
        [FromBody] ApproveRelationshipRequestDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RelationshipId))
        {
            return BadRequest(new { error = "RelationshipId is required." });
        }

        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "usr-investigator";
        var actorName = User.FindFirstValue(ClaimTypes.Name) ?? "Investigating Officer";
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var success = await _extractionService.ApproveRelationshipAsync(
            id,
            request.RelationshipId,
            actorId,
            actorName,
            ipAddress,
            cancellationToken);

        if (!success)
        {
            return NotFound(new { error = $"Relationship '{request.RelationshipId}' not found in evidence '{id}'." });
        }

        return Ok(new { message = "Relationship approved and promoted to investigation graph with provenance.", relationshipId = request.RelationshipId });
    }

    [HttpPost("{id}/extraction/relationships/reject")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RejectExtractedRelationship(
        string id,
        [FromBody] RejectRelationshipRequestDto request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RelationshipId))
        {
            return BadRequest(new { error = "RelationshipId is required." });
        }

        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "usr-investigator";
        var actorName = User.FindFirstValue(ClaimTypes.Name) ?? "Investigating Officer";
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var success = await _extractionService.RejectRelationshipAsync(
            id,
            request.RelationshipId,
            request.Reason,
            actorId,
            actorName,
            ipAddress,
            cancellationToken);

        if (!success)
        {
            return NotFound(new { error = $"Relationship '{request.RelationshipId}' not found in evidence '{id}'." });
        }

        return Ok(new { message = "Relationship rejected.", relationshipId = request.RelationshipId });
    }

    [HttpPost("{id}/verify-integrity")]
    [ProducesResponseType(typeof(IntegrityCheckResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> VerifyIntegrity(string id, CancellationToken cancellationToken)
    {
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "usr-investigator";
        var actorName = User.FindFirstValue(ClaimTypes.Name) ?? "Investigating Officer";
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var result = await _evidenceService.VerifyIntegrityAsync(id, actorId, actorName, ipAddress, cancellationToken);
        return Ok(result);
    }

    [HttpPost("{id}/process")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> RetryProcessing(string id, CancellationToken cancellationToken)
    {
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "usr-investigator";
        var actorName = User.FindFirstValue(ClaimTypes.Name) ?? "Investigating Officer";
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        var queued = await _evidenceService.RetryProcessingAsync(id, actorId, actorName, ipAddress, cancellationToken);
        if (!queued)
        {
            return NotFound(new { error = $"Evidence item '{id}' was not found." });
        }

        return Ok(new { message = "Evidence extraction job queued for background execution.", evidenceId = id });
    }
}
