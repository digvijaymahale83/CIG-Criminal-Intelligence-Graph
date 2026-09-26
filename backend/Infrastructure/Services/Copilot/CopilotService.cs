using System.Collections.Concurrent;
using Application.Common.Interfaces;
using Application.DTOs;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Copilot;

public class CopilotService : ICopilotService
{
    private readonly IAppDbContext _dbContext;
    private readonly ICopilotIntentRouter _intentRouter;
    private readonly ICopilotContextBuilder _contextBuilder;
    private readonly ILLMService _llmService;
    private readonly ICopilotCitationValidator _citationValidator;
    private readonly IAuditService _auditService;
    private readonly ILogger<CopilotService> _logger;

    // In-memory thread-safe conversation cache scoped by conversationId and caseId
    private static readonly ConcurrentDictionary<string, CopilotConversationDto> _conversations = new();

    public CopilotService(
        IAppDbContext dbContext,
        ICopilotIntentRouter intentRouter,
        ICopilotContextBuilder contextBuilder,
        ILLMService llmService,
        ICopilotCitationValidator citationValidator,
        IAuditService auditService,
        ILogger<CopilotService> logger)
    {
        _dbContext = dbContext;
        _intentRouter = intentRouter;
        _contextBuilder = contextBuilder;
        _llmService = llmService;
        _citationValidator = citationValidator;
        _auditService = auditService;
        _logger = logger;
    }

    public async Task<CopilotResponseDto> AskCopilotAsync(
        CopilotQueryRequest request,
        string userId,
        string userRole,
        string userName,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.CaseId))
            throw new ArgumentException("CaseId is required to query the Investigation Copilot.", nameof(request.CaseId));
        if (string.IsNullOrWhiteSpace(request.Query))
            throw new ArgumentException("Query text is required.", nameof(request.Query));

        // 1. Authorization & Case Scoping
        var caseRecord = await _dbContext.Cases
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.CaseId || c.CaseNumber == request.CaseId, cancellationToken);

        if (caseRecord == null)
        {
            throw new KeyNotFoundException($"Investigation case '{request.CaseId}' was not found.");
        }

        var normalizedRole = userRole?.Trim().ToUpperInvariant() ?? string.Empty;
        if (normalizedRole != "ADMIN" && normalizedRole != "INVESTIGATOR" && normalizedRole != "ANALYST" && normalizedRole != "OFFICER" && normalizedRole != "SUPERVISOR")
        {
            await _auditService.LogAsync(
                userId,
                userName,
                "COPILOT_ACCESS_DENIED",
                "Case",
                request.CaseId,
                $"Unauthorized user role '{userRole}' attempted to query copilot for case {caseRecord.CaseNumber}",
                null,
                ipAddress,
                cancellationToken);

            throw new UnauthorizedAccessException($"User role '{userRole}' is not authorized to access investigation copilot.");
        }

        // 2. Manage Scoped Conversation Memory
        var convId = string.IsNullOrWhiteSpace(request.ConversationId)
            ? $"conv-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8]}"
            : request.ConversationId;

        // Ensure conversation is strictly scoped to this case
        if (_conversations.TryGetValue(convId, out var existingConv))
        {
            if (existingConv.CaseId != caseRecord.Id)
            {
                // Cross-case boundary violation prevented: reset conversation
                _conversations.TryRemove(convId, out _);
                convId = $"conv-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8]}";
            }
        }

        // 3. Intent Routing & Token Extraction
        var (intent, extractedTokens) = await _intentRouter.RouteIntentAsync(request.Query, caseRecord.Id, cancellationToken);

        // 4. Grounded Context Building (Case-Scoped)
        var context = await _contextBuilder.BuildContextAsync(
            caseRecord.Id,
            request.Query,
            intent,
            extractedTokens,
            request,
            userId,
            userRole ?? "INVESTIGATOR",
            cancellationToken);

        // 5. LLM Synthesis (Deterministic Engine / Provider Abstraction)
        var rawAnswer = await _llmService.GenerateGroundedAnswerAsync(request.Query, context, cancellationToken);
        rawAnswer.ConversationId = convId;

        // 6. Anti-Hallucination Citation Validation
        var validatedResponse = _citationValidator.ValidateAndFilterCitations(rawAnswer, context);

        // 7. Update Scoped Conversation History
        var conversation = _conversations.GetOrAdd(convId, id => new CopilotConversationDto
        {
            ConversationId = id,
            UserId = userId,
            CaseId = caseRecord.Id,
            CreatedAtUtc = DateTime.UtcNow
        });

        conversation.Messages.Add(new CopilotConversationMessageDto
        {
            Role = "user",
            Content = request.Query,
            TimestampUtc = DateTime.UtcNow
        });

        conversation.Messages.Add(new CopilotConversationMessageDto
        {
            Role = "assistant",
            Content = validatedResponse.Answer,
            TimestampUtc = DateTime.UtcNow
        });
        conversation.UpdatedAtUtc = DateTime.UtcNow;

        // 8. Audit Logging
        var auditMeta = $"{{\"query\":\"{System.Text.Encodings.Web.JavaScriptEncoder.Default.Encode(request.Query)}\",\"intent\":\"{intent}\",\"confidence\":\"{validatedResponse.Confidence}\",\"evidenceCitationsCount\":{validatedResponse.EvidenceCitations.Count},\"warningsCount\":{validatedResponse.Warnings.Count}}}";

        await _auditService.LogAsync(
            userId,
            userName,
            "COPILOT_QUERY",
            "Case",
            caseRecord.Id,
            $"Queried Copilot on Case {caseRecord.CaseNumber} (Intent: {intent}, Confidence: {validatedResponse.Confidence})",
            auditMeta,
            ipAddress,
            cancellationToken);

        if (validatedResponse.Warnings.Any(w => w.Contains("Integrity Alert", StringComparison.OrdinalIgnoreCase)))
        {
            await _auditService.LogAsync(
                userId,
                userName,
                "COPILOT_INTEGRITY_WARNING",
                "Case",
                caseRecord.Id,
                $"Copilot query encountered evidence with integrity mismatch",
                $"{{\"warnings\":[\"{string.Join("\",\"", validatedResponse.Warnings)}\"]}}",
                ipAddress,
                cancellationToken);
        }

        return validatedResponse;
    }

    public Task<CopilotConversationDto?> GetConversationAsync(
        string conversationId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (_conversations.TryGetValue(conversationId, out var conv) && conv.UserId == userId)
        {
            return Task.FromResult<CopilotConversationDto?>(conv);
        }
        return Task.FromResult<CopilotConversationDto?>(null);
    }

    public Task<bool> DeleteConversationAsync(
        string conversationId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        if (_conversations.TryGetValue(conversationId, out var conv) && conv.UserId == userId)
        {
            return Task.FromResult(_conversations.TryRemove(conversationId, out _));
        }
        return Task.FromResult(false);
    }

    public async Task<List<string>> GetSuggestedQuestionsAsync(
        string caseId,
        string userId,
        string userRole,
        CancellationToken cancellationToken = default)
    {
        var suggestions = new List<string>();

        var caseItem = await _dbContext.Cases
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == caseId || c.CaseNumber == caseId, cancellationToken);

        if (caseItem == null) return suggestions;

        var prominentEntities = await _dbContext.Entities
            .AsNoTracking()
            .Where(e => e.CaseId == caseItem.Id)
            .Take(2)
            .ToListAsync(cancellationToken);

        if (prominentEntities.Count >= 2)
        {
            suggestions.Add($"How are {prominentEntities[0].CanonicalName} and {prominentEntities[1].CanonicalName} connected?");
            suggestions.Add($"Who is connected to {prominentEntities[0].CanonicalName}?");
        }
        else if (prominentEntities.Count == 1)
        {
            suggestions.Add($"Who is connected to {prominentEntities[0].CanonicalName}?");
            suggestions.Add($"What evidence is associated with {prominentEntities[0].CanonicalName}?");
        }

        suggestions.Add("What are the highest-priority investigative alerts?");
        suggestions.Add("Show the chronological timeline of events.");
        suggestions.Add("Does this case have any cross-case connections?");
        suggestions.Add("Is all case evidence cryptographically verified on the integrity ledger?");

        return suggestions;
    }
}
