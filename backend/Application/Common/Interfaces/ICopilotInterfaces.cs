using Application.DTOs;

namespace Application.Common.Interfaces;

public interface ICopilotIntentRouter
{
    Task<(string Intent, List<string> ExtractedEntities)> RouteIntentAsync(
        string query,
        string caseId,
        CancellationToken cancellationToken = default);

    Task<string> ClassifyIntentAsync(
        string caseId,
        string query,
        CancellationToken cancellationToken = default);

    string SanitizeInput(string query);
}

public interface ICopilotContextBuilder
{
    Task<CopilotGroundedContext> BuildContextAsync(
        string caseId,
        string query,
        string intent,
        List<string> entityTokens,
        CopilotQueryRequest request,
        string userId,
        string userRole,
        CancellationToken cancellationToken = default);
}

public interface ILLMService
{
    Task<CopilotResponseDto> GenerateGroundedAnswerAsync(
        string userQuery,
        CopilotGroundedContext context,
        CancellationToken cancellationToken = default);
}

public interface ICopilotCitationValidator
{
    CopilotResponseDto ValidateAndFilterCitations(
        CopilotResponseDto response,
        CopilotGroundedContext context);
}

public interface ICopilotService
{
    Task<CopilotResponseDto> AskCopilotAsync(
        CopilotQueryRequest request,
        string userId,
        string userRole,
        string userName,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    Task<CopilotConversationDto?> GetConversationAsync(
        string conversationId,
        string userId,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteConversationAsync(
        string conversationId,
        string userId,
        CancellationToken cancellationToken = default);

    Task<List<string>> GetSuggestedQuestionsAsync(
        string caseId,
        string userId,
        string userRole,
        CancellationToken cancellationToken = default);
}
