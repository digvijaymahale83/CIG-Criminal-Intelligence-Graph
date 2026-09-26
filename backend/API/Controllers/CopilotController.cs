using System.Security.Claims;
using Application.Common.Interfaces;
using Application.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/v1/copilot")]
public class CopilotController : ControllerBase
{
    private readonly ICopilotService _copilotService;
    private readonly ILogger<CopilotController> _logger;

    public CopilotController(ICopilotService copilotService, ILogger<CopilotController> logger)
    {
        _copilotService = copilotService;
        _logger = logger;
    }

    /// <summary>
    /// Executes a natural-language grounded investigative query against the active case context.
    /// </summary>
    [HttpPost("query")]
    [ProducesResponseType(typeof(CopilotResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> QueryCopilot(
        [FromBody] CopilotQueryRequest request,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "usr-investigator";
        var userRole = User.FindFirstValue(ClaimTypes.Role) ?? "INVESTIGATOR";
        var userName = User.FindFirstValue(ClaimTypes.Name) ?? "Investigator";
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();

        try
        {
            var response = await _copilotService.AskCopilotAsync(
                request,
                userId,
                userRole,
                userName,
                ipAddress,
                cancellationToken);

            return Ok(response);
        }
        catch (UnauthorizedAccessException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { error = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error executing copilot query for case {CaseId}: {Message}", request.CaseId, ex.Message);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An internal error occurred while processing the investigation query." });
        }
    }

    /// <summary>
    /// Retrieves active conversation messages for the current investigator.
    /// </summary>
    [HttpGet("conversations/{conversationId}")]
    [ProducesResponseType(typeof(CopilotConversationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConversation(
        [FromRoute] string conversationId,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "usr-investigator";
        var conv = await _copilotService.GetConversationAsync(conversationId, userId, cancellationToken);
        if (conv == null)
        {
            return NotFound(new { error = $"Conversation '{conversationId}' not found." });
        }
        return Ok(conv);
    }

    /// <summary>
    /// Purges conversation history for privacy and scoped data retention.
    /// </summary>
    [HttpDelete("conversations/{conversationId}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteConversation(
        [FromRoute] string conversationId,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "usr-investigator";
        var deleted = await _copilotService.DeleteConversationAsync(conversationId, userId, cancellationToken);
        if (!deleted)
        {
            return NotFound(new { error = $"Conversation '{conversationId}' not found." });
        }
        return NoContent();
    }

    /// <summary>
    /// Returns dynamic suggested questions grounded in the active case's entities and signals.
    /// </summary>
    [HttpGet("suggested-questions")]
    [ProducesResponseType(typeof(List<string>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSuggestedQuestions(
        [FromQuery] string caseId,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "usr-investigator";
        var userRole = User.FindFirstValue(ClaimTypes.Role) ?? "INVESTIGATOR";

        var suggestions = await _copilotService.GetSuggestedQuestionsAsync(
            caseId,
            userId,
            userRole,
            cancellationToken);

        return Ok(suggestions);
    }
}
