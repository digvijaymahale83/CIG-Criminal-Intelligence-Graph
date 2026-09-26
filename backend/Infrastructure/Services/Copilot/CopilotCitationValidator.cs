using Application.Common.Interfaces;
using Application.DTOs;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Copilot;

public class CopilotCitationValidator : ICopilotCitationValidator
{
    private readonly ILogger<CopilotCitationValidator> _logger;

    public CopilotCitationValidator(ILogger<CopilotCitationValidator> logger)
    {
        _logger = logger;
    }

    public CopilotResponseDto ValidateAndFilterCitations(
        CopilotResponseDto response,
        CopilotGroundedContext context)
    {
        var validEvidenceIds = new HashSet<string>(context.EvidenceItems.Select(e => e.EvidenceId), StringComparer.OrdinalIgnoreCase);
        var validEntityIds = new HashSet<string>(context.Entities.Select(e => e.EntityId), StringComparer.OrdinalIgnoreCase);
        var validRelIds = new HashSet<string>(context.VerifiedRelationships.Select(r => r.RelationshipId), StringComparer.OrdinalIgnoreCase);
        var validEventIds = new HashSet<string>(context.TimelineEvents.Select(t => t.EventId), StringComparer.OrdinalIgnoreCase);
        var validAlertIds = new HashSet<string>(context.Alerts.Select(a => a.AlertId), StringComparer.OrdinalIgnoreCase);
        var validLocationIds = new HashSet<string>(context.Locations.Select(l => l.LocationId), StringComparer.OrdinalIgnoreCase);

        // Filter Evidence Citations
        var validEvidenceCitations = new List<EvidenceCitationDto>();
        foreach (var citation in response.EvidenceCitations)
        {
            if (validEvidenceIds.Contains(citation.EvidenceId))
            {
                // Attach genuine integrity status from context
                var original = context.EvidenceItems.First(e => e.EvidenceId.Equals(citation.EvidenceId, StringComparison.OrdinalIgnoreCase));
                citation.IntegrityStatus = original.IntegrityStatus;
                citation.Sha256Hash = original.Sha256Hash;
                validEvidenceCitations.Add(citation);
            }
            else
            {
                response.Warnings.Add($"Citation '{citation.EvidenceId}' was rejected because it does not exist in the retrieved case evidence.");
                _logger.LogWarning("Hallucinated evidence citation rejected: {EvidenceId}", citation.EvidenceId);
            }
        }
        response.EvidenceCitations = validEvidenceCitations;

        // Filter Entity Citations
        response.EntityCitations = response.EntityCitations
            .Where(e => validEntityIds.Contains(e.EntityId))
            .ToList();

        // Filter Relationship Citations
        response.RelationshipCitations = response.RelationshipCitations
            .Where(r => validRelIds.Contains(r.RelationshipId))
            .ToList();

        // Filter Timeline Citations
        response.TimelineCitations = response.TimelineCitations
            .Where(t => validEventIds.Contains(t.EventId))
            .ToList();

        // Filter Alert Citations
        response.AlertCitations = response.AlertCitations
            .Where(a => validAlertIds.Contains(a.AlertId))
            .ToList();

        // Filter Location Citations
        response.LocationCitations = response.LocationCitations
            .Where(l => validLocationIds.Contains(l.LocationId))
            .ToList();

        // Validate individual claims
        var allValidSourceIds = new HashSet<string>(validEvidenceIds, StringComparer.OrdinalIgnoreCase);
        allValidSourceIds.UnionWith(validEntityIds);
        allValidSourceIds.UnionWith(validRelIds);
        allValidSourceIds.UnionWith(validEventIds);
        allValidSourceIds.UnionWith(validAlertIds);
        allValidSourceIds.UnionWith(validLocationIds);

        foreach (var claim in response.Claims)
        {
            if (claim.SourceIds.Count > 0)
            {
                var supportedSources = claim.SourceIds.Where(s => allValidSourceIds.Contains(s)).ToList();
                if (supportedSources.Count < claim.SourceIds.Count)
                {
                    claim.IsSupported = supportedSources.Count > 0;
                    claim.SourceIds = supportedSources;
                    if (!claim.IsSupported)
                    {
                        claim.ClaimType = "UNSUPPORTED";
                        response.Warnings.Add($"Claim '{claim.Text}' lacks valid supporting evidence and was marked unsupported.");
                    }
                }
            }
            else if (claim.ClaimType == "FACT")
            {
                claim.IsSupported = false;
                claim.ClaimType = "UNSUPPORTED";
            }
        }

        // Integrity Warnings Check (Phase 9 Integration)
        foreach (var ev in response.EvidenceCitations)
        {
            if (ev.IntegrityStatus.Equals("EVIDENCE_MODIFIED", StringComparison.OrdinalIgnoreCase) ||
                ev.IntegrityStatus.Equals("HASH_MISMATCH", StringComparison.OrdinalIgnoreCase))
            {
                response.Warnings.Add(
                    $"Integrity Alert: Supporting evidence '{ev.FileName}' ({ev.EvidenceId}) currently fails cryptographic verification ({ev.IntegrityStatus}). File content differs from registered ledger hash.");
            }
        }

        // Calculate Grounded Confidence
        if (response.Claims.Count == 0 || response.Answer.Contains("Insufficient evidence", StringComparison.OrdinalIgnoreCase))
        {
            response.Confidence = "UNKNOWN";
            response.ConfidenceScore = 0.10;
        }
        else if (response.Claims.Any(c => !c.IsSupported))
        {
            response.Confidence = "LOW";
            response.ConfidenceScore = 0.35;
        }
        else if (response.ModelSignals.Count > 0 && response.EvidenceCitations.Count == 0 && response.RelationshipCitations.Count == 0)
        {
            response.Confidence = "MODEL_SIGNAL";
            response.ConfidenceScore = 0.55;
        }
        else if (response.EvidenceCitations.Count >= 2 || response.RelationshipCitations.Count >= 2)
        {
            response.Confidence = "HIGH";
            response.ConfidenceScore = 0.95;
        }
        else
        {
            response.Confidence = "MEDIUM";
            response.ConfidenceScore = 0.75;
        }

        return response;
    }
}
