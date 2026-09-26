using System.Security.Claims;
using Application.Common.Interfaces;
using Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v1")]
public class IntegrityLedgerController : ControllerBase
{
    private readonly IIntegrityLedgerService _ledgerService;
    private readonly ILogger<IntegrityLedgerController> _logger;

    public IntegrityLedgerController(
        IIntegrityLedgerService ledgerService,
        ILogger<IntegrityLedgerController> logger)
    {
        _ledgerService = ledgerService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the current cryptographic integrity status of an evidence item (fast query using registered metadata).
    /// </summary>
    [HttpGet("evidence/{evidenceId}/integrity")]
    [ProducesResponseType(typeof(EvidenceIntegrityStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEvidenceIntegrity(
        [FromRoute] string evidenceId,
        CancellationToken cancellationToken)
    {
        var status = await _ledgerService.GetEvidenceIntegrityStatusAsync(evidenceId, cancellationToken);
        if (status.Status == "MISSING_FILE" && status.Explanation.Contains("not found"))
        {
            return NotFound(new { error = $"Evidence item '{evidenceId}' was not found." });
        }
        return Ok(status);
    }

    /// <summary>
    /// Returns the full append-only ledger block history for a specific evidence item.
    /// </summary>
    [HttpGet("evidence/{evidenceId}/integrity/history")]
    [ProducesResponseType(typeof(List<EvidenceLedgerBlockDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEvidenceIntegrityHistory(
        [FromRoute] string evidenceId,
        CancellationToken cancellationToken)
    {
        var history = await _ledgerService.GetEvidenceHistoryAsync(evidenceId, cancellationToken);
        return Ok(history);
    }

    /// <summary>
    /// Performs live byte-level re-verification of the physical file on disk against its registered SHA-256 and ledger block.
    /// </summary>
    [HttpPost("evidence/{evidenceId}/integrity/verify")]
    [ProducesResponseType(typeof(EvidenceIntegrityStatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> VerifyEvidenceNow(
        [FromRoute] string evidenceId,
        CancellationToken cancellationToken)
    {
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "usr-investigator";
        var actorName = User.FindFirstValue(ClaimTypes.Name) ?? "Investigating Officer";

        var result = await _ledgerService.VerifyEvidenceAsync(evidenceId, actorId, actorName, cancellationToken);
        if (result.Status == "MISSING_FILE" && result.Explanation.Contains("does not exist in the database"))
        {
            return NotFound(new { error = $"Evidence item '{evidenceId}' was not found." });
        }
        return Ok(result);
    }

    /// <summary>
    /// Returns paginated ledger blocks from the append-only blockchain ledger.
    /// </summary>
    [HttpGet("integrity/ledger")]
    [ProducesResponseType(typeof(List<EvidenceLedgerBlockDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLedgerBlocks(
        [FromQuery] int limit = 50,
        [FromQuery] int offset = 0,
        CancellationToken cancellationToken = default)
    {
        var blocks = await _ledgerService.GetPaginatedLedgerAsync(limit, offset, cancellationToken);
        return Ok(blocks);
    }

    /// <summary>
    /// Retrieves full block details by block index.
    /// </summary>
    [HttpGet("integrity/ledger/{blockIndex:long}")]
    [ProducesResponseType(typeof(EvidenceLedgerBlockDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBlockByIndex(
        [FromRoute] long blockIndex,
        CancellationToken cancellationToken)
    {
        var block = await _ledgerService.GetBlockByIndexAsync(blockIndex, cancellationToken);
        if (block == null)
        {
            return NotFound(new { error = $"Ledger block #{blockIndex} was not found." });
        }
        return Ok(block);
    }

    /// <summary>
    /// Executes full end-to-end cryptographic chain validation from Genesis (Block #0) to the latest block.
    /// </summary>
    [HttpPost("integrity/ledger/verify-chain")]
    [ProducesResponseType(typeof(ChainValidationResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> VerifyChain(CancellationToken cancellationToken)
    {
        var result = await _ledgerService.VerifyChainAsync(cancellationToken);
        return Ok(result);
    }

    /// <summary>
    /// Returns evidence items pending registration in the cryptographic ledger.
    /// </summary>
    [HttpGet("integrity/reconciliation")]
    [ProducesResponseType(typeof(List<ReconciliationItemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReconciliationCandidates(CancellationToken cancellationToken)
    {
        var candidates = await _ledgerService.GetReconciliationCandidatesAsync(cancellationToken);
        return Ok(candidates);
    }

    /// <summary>
    /// Registers an authorized missing ledger entry for an unledgered evidence item.
    /// </summary>
    [HttpPost("integrity/reconciliation/{evidenceId}")]
    [ProducesResponseType(typeof(EvidenceLedgerBlockDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReconcileEvidence(
        [FromRoute] string evidenceId,
        CancellationToken cancellationToken)
    {
        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "usr-supervisor";
        var actorName = User.FindFirstValue(ClaimTypes.Name) ?? "Supervisor";

        try
        {
            var block = await _ledgerService.ReconcileEvidenceAsync(evidenceId, actorId, actorName, cancellationToken);
            return Ok(block);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to reconcile evidence '{EvidenceId}': {Message}", evidenceId, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = $"Reconciliation failed: {ex.Message}" });
        }
    }
}
