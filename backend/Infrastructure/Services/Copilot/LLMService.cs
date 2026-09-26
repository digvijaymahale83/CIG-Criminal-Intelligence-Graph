using Application.Common.Interfaces;
using Application.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Copilot;

public class LLMService : ILLMService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<LLMService> _logger;

    public LLMService(IConfiguration configuration, ILogger<LLMService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task<CopilotResponseDto> GenerateGroundedAnswerAsync(
        string userQuery,
        CopilotGroundedContext context,
        CancellationToken cancellationToken = default)
    {
        // Deterministic Grounded Reasoning Engine
        // Evaluates retrieved facts against query intent and builds explainable, citation-backed response.
        var response = new CopilotResponseDto
        {
            Intent = context.QueryIntent,
            ExecutedAtUtc = DateTime.UtcNow
        };

        // Check if query was asking about specific entity token that wasn't found
        if (context.ExtractedTokens.Count > 0 && context.Entities.Count == 0 &&
            context.QueryIntent != "INTEGRITY" && context.QueryIntent != "ALERT" &&
            context.QueryIntent != "CROSS_CASE" && context.QueryIntent != "TIMELINE" && context.QueryIntent != "LOCATION")
        {
            response.Answer = $"Insufficient evidence in current investigation data. No matching entity found for '{string.Join("', '", context.ExtractedTokens)}'.";
            response.Confidence = "UNKNOWN";
            response.ConfidenceScore = 0.10;
            response.SuggestedFollowUps = new List<string>
            {
                "Search the global repository for this entity name",
                "Show prominent entities in this case",
                "Show case summary"
            };
            return Task.FromResult(response);
        }

        // Branch by Intent
        switch (context.QueryIntent)
        {
            case "SHORTEST_PATH":
                HandleShortestPath(context, response);
                break;

            case "ENTITY_RELATIONSHIPS":
                HandleEntityRelationships(context, response);
                break;

            case "ENTITY_LOOKUP":
                HandleEntityLookup(context, response);
                break;

            case "CROSS_CASE":
                HandleCrossCase(context, response);
                break;

            case "TIMELINE":
                HandleTimeline(context, response);
                break;

            case "LOCATION":
                HandleLocation(context, response);
                break;

            case "EVIDENCE":
                HandleEvidence(context, response);
                break;

            case "ALERT":
                HandleAlerts(context, response);
                break;

            case "INTEGRITY":
                HandleIntegrity(context, response);
                break;

            case "GRAPH_ANALYTICS":
                HandleGraphAnalytics(context, response);
                break;

            case "MODEL_SIGNAL":
                HandleModelSignals(context, response);
                break;

            default:
                HandleGeneralSummary(context, response);
                break;
        }

        // Attach citations present in context
        response.EntityCitations = context.Entities.Take(5).ToList();
        response.EvidenceCitations = context.EvidenceItems.Take(5).ToList();
        response.TimelineCitations = context.TimelineEvents.Take(10).ToList();
        response.LocationCitations = context.Locations.Take(5).ToList();
        response.AlertCitations = context.Alerts.Take(5).ToList();
        response.ModelSignals = context.ModelPredictions.Take(5).ToList();
        response.CrossCaseConnections = context.CrossCaseConnections.Take(5).ToList();

        return Task.FromResult(response);
    }

    private void HandleShortestPath(CopilotGroundedContext ctx, CopilotResponseDto resp)
    {
        if (ctx.ShortestPath != null && ctx.ShortestPath.Found && ctx.ShortestPath.Nodes.Count > 1)
        {
            var pathNodes = ctx.ShortestPath.Nodes.Select(n => !string.IsNullOrEmpty(n.Name) ? n.Name : n.Label).ToList();
            var pathStr = string.Join(" → ", pathNodes);

            resp.Answer = $"A verified connection was found between {pathNodes.First()} and {pathNodes.Last()} across {ctx.ShortestPath.HopsCount} {(ctx.ShortestPath.HopsCount == 1 ? "step" : "steps")}: {pathStr}.";

            foreach (var edge in ctx.ShortestPath.Edges)
            {
                var srcNode = ctx.ShortestPath.Nodes.FirstOrDefault(n => n.Id == edge.Source);
                var tgtNode = ctx.ShortestPath.Nodes.FirstOrDefault(n => n.Id == edge.Target);
                var srcName = srcNode?.Name ?? edge.Source;
                var tgtName = tgtNode?.Name ?? edge.Target;
                var evIds = !string.IsNullOrEmpty(edge.SourceEvidenceId) ? new List<string> { edge.SourceEvidenceId } : new List<string>();

                resp.Claims.Add(new CopilotClaimDto
                {
                    Text = $"{srcName} connects to {tgtName} via {edge.Type}",
                    ClaimType = "FACT",
                    SourceIds = evIds.Count > 0 ? evIds : new List<string> { edge.Source }
                });
            }

            resp.RelationshipCitations = ctx.VerifiedRelationships.Take(5).ToList();
            resp.SuggestedFollowUps = new List<string>
            {
                "Show supporting evidence for this connection",
                "Check if any of these entities appear in another case",
                "Show timeline of interactions between these entities"
            };
        }
        else if (ctx.Entities.Count >= 2 && ctx.VerifiedRelationships.Any(r =>
            (r.SourceEntityId == ctx.Entities[0].EntityId && r.TargetEntityId == ctx.Entities[1].EntityId) ||
            (r.SourceEntityId == ctx.Entities[1].EntityId && r.TargetEntityId == ctx.Entities[0].EntityId)))
        {
            var rel = ctx.VerifiedRelationships.First(r =>
                (r.SourceEntityId == ctx.Entities[0].EntityId && r.TargetEntityId == ctx.Entities[1].EntityId) ||
                (r.SourceEntityId == ctx.Entities[1].EntityId && r.TargetEntityId == ctx.Entities[0].EntityId));

            var e1 = ctx.Entities[0].CanonicalName;
            var e2 = ctx.Entities[1].CanonicalName;

            resp.Answer = $"A verified relationship was found between {e1} and {e2}: {rel.RelationshipType} (Confidence: {rel.Confidence:P0}).";
            resp.Claims.Add(new CopilotClaimDto
            {
                Text = $"{e1} connects to {e2} via {rel.RelationshipType}",
                ClaimType = "FACT",
                SourceIds = rel.EvidenceIds.Count > 0 ? rel.EvidenceIds : new List<string> { rel.RelationshipId }
            });

            resp.RelationshipCitations = new List<RelationshipCitationDto> { rel };
            resp.SuggestedFollowUps = new List<string>
            {
                "Show supporting evidence for this connection",
                "Check if any of these entities appear in another case",
                "Show timeline of interactions between these entities"
            };
        }
        else if (ctx.Entities.Count >= 2)
        {
            var e1 = ctx.Entities[0].CanonicalName;
            var e2 = ctx.Entities[1].CanonicalName;

            resp.Answer = $"No verified relationship was found between '{e1}' and '{e2}' in current investigation records.";
            resp.Claims.Add(new CopilotClaimDto
            {
                Text = $"No direct or indirect verified path links {e1} and {e2}",
                ClaimType = "FACT",
                SourceIds = new List<string> { ctx.Entities[0].EntityId, ctx.Entities[1].EntityId }
            });

            // Check if model prediction exists
            if (ctx.ModelPredictions.Count > 0)
            {
                var pred = ctx.ModelPredictions[0];
                resp.ModelSignals.Add(pred);
                resp.Answer += $" However, a model-predicted connection was identified (Score: {pred.Score:F2}, Status: PENDING REVIEW). This is an algorithmic lead and does not constitute verified evidence.";
            }

            resp.SuggestedFollowUps = new List<string>
            {
                $"Show all verified relationships for {e1}",
                $"Show all verified relationships for {e2}",
                "Check for potential cross-case matches"
            };
        }
        else
        {
            resp.Answer = "Insufficient evidence to compute a connection path. Please specify two entities.";
        }
    }

    private void HandleEntityRelationships(CopilotGroundedContext ctx, CopilotResponseDto resp)
    {
        bool isMarathi = ctx.Language?.ToLowerInvariant() == "mr";

        if (ctx.Entities.Count == 0)
        {
            if (ctx.VerifiedRelationships.Count > 0)
            {
                var relDescriptions = ctx.VerifiedRelationships.Take(5).Select(r =>
                    $"{r.SourceEntityName} ↔ {r.TargetEntityName} ({r.RelationshipType}, {r.Confidence:P0})"
                ).ToList();

                if (isMarathi)
                {
                    resp.Answer = $"प्रकरण {ctx.CaseNumber} मधील प्रमुख पडताळलेले संबंध खालीलप्रमाणे आहेत:\n• " +
                                  string.Join("\n• ", relDescriptions);
                }
                else
                {
                    resp.Answer = $"The key verified relationships in Case {ctx.CaseNumber} are:\n• " +
                                  string.Join("\n• ", relDescriptions);
                }

                foreach (var r in ctx.VerifiedRelationships.Take(5))
                {
                    resp.Claims.Add(new CopilotClaimDto
                    {
                        Text = $"{r.SourceEntityName} connects to {r.TargetEntityName} via {r.RelationshipType}",
                        ClaimType = "FACT",
                        SourceIds = r.EvidenceIds.Count > 0 ? r.EvidenceIds : new List<string> { r.RelationshipId }
                    });
                }
                resp.RelationshipCitations = ctx.VerifiedRelationships.Take(5).ToList();
                resp.SuggestedFollowUps = isMarathi
                    ? new List<string> { "या संबंधांना कोणते पुरावे समर्थन देतात?", "या घटकांची वेळरेषा दाखवा", "इतर प्रकरणांशी संबंध तपासा" }
                    : new List<string> { "What evidence supports these connections?", "Show timeline of interactions", "Check cross-case links" };
                return;
            }

            resp.Answer = isMarathi
                ? "सध्याच्या तपास माहितीत कोणतेही पडताळलेले संबंध आढळले नाहीत."
                : "Insufficient evidence in the current investigation data. No matching entity or relationships found.";
            return;
        }

        var target = ctx.Entities[0];
        if (ctx.VerifiedRelationships.Count > 0)
        {
            var relDescriptions = ctx.VerifiedRelationships.Select(r =>
            {
                var other = r.SourceEntityId == target.EntityId ? r.TargetEntityName : r.SourceEntityName;
                return $"{r.RelationshipType} with {other} (Confidence: {r.Confidence:P0})";
            }).ToList();

            if (isMarathi)
            {
                resp.Answer = $"{target.CanonicalName} ({target.EntityType}) चे या प्रकरणातील {ctx.VerifiedRelationships.Count} पडताळलेले संबंध:\n• " +
                              string.Join("\n• ", relDescriptions);
            }
            else
            {
                resp.Answer = $"{target.CanonicalName} ({target.EntityType}) has {ctx.VerifiedRelationships.Count} verified {(ctx.VerifiedRelationships.Count == 1 ? "relationship" : "relationships")} in this case:\n• " +
                              string.Join("\n• ", relDescriptions);
            }

            foreach (var r in ctx.VerifiedRelationships)
            {
                resp.Claims.Add(new CopilotClaimDto
                {
                    Text = $"{target.CanonicalName} has verified {r.RelationshipType} relationship with {r.TargetEntityName}",
                    ClaimType = "FACT",
                    SourceIds = r.EvidenceIds.Count > 0 ? r.EvidenceIds : new List<string> { r.RelationshipId }
                });
            }

            resp.RelationshipCitations = ctx.VerifiedRelationships;
            resp.SuggestedFollowUps = isMarathi
                ? new List<string> { $"{target.CanonicalName} च्या संबंधांना कोणता पुरावा आहे?", $"{target.CanonicalName} ची वेळरेषा दाखवा", $"{target.CanonicalName} इतर प्रकरणांत आहे का?" }
                : new List<string>
                {
                    $"What evidence supports {target.CanonicalName}'s connections?",
                    $"What happened in {target.CanonicalName}'s timeline?",
                    $"Does {target.CanonicalName} appear in another case?"
                };
        }
        else
        {
            resp.Answer = isMarathi
                ? $"{target.CanonicalName} साठी या प्रकरणात कोणतेही पडताळलेले संबंध नोंदवलेले नाहीत."
                : $"No verified relationships were found for {target.CanonicalName} in this investigation.";
            resp.Claims.Add(new CopilotClaimDto
            {
                Text = $"{target.CanonicalName} has no recorded connections in case {ctx.CaseNumber}",
                ClaimType = "FACT",
                SourceIds = new List<string> { target.EntityId }
            });
        }
    }

    private void HandleEntityLookup(CopilotGroundedContext ctx, CopilotResponseDto resp)
    {
        if (ctx.Entities.Count == 0)
        {
            resp.Answer = "Insufficient evidence in the current investigation data. No matching entity found.";
            return;
        }

        var target = ctx.Entities[0];
        resp.Answer = $"Entity Record: {target.CanonicalName}\n• Type: {target.EntityType}\n• System ID: {target.EntityId}\n• Case: {ctx.CaseNumber}\n• Associated Relationships: {ctx.VerifiedRelationships.Count}";

        if (!string.IsNullOrWhiteSpace(ctx.DisambiguationNote))
        {
            resp.Warnings.Add(ctx.DisambiguationNote);
        }

        resp.Claims.Add(new CopilotClaimDto
        {
            Text = $"{target.CanonicalName} is an entity of type {target.EntityType} in case {ctx.CaseNumber}",
            ClaimType = "FACT",
            SourceIds = new List<string> { target.EntityId }
        });

        resp.SuggestedFollowUps = new List<string>
        {
            $"Who is connected to {target.CanonicalName}?",
            $"Where was {target.CanonicalName} observed?",
            $"Are there any alerts for {target.CanonicalName}?"
        };
    }

    private void HandleCrossCase(CopilotGroundedContext ctx, CopilotResponseDto resp)
    {
        if (ctx.CrossCaseConnections.Count > 0)
        {
            var connectionsList = ctx.CrossCaseConnections.Select(c =>
                $"• Confirmed Cross-Case Connection: {c.SourceEntityName} linked to {c.TargetEntityName} across {c.SourceCaseNumber} and {c.TargetCaseNumber} ({c.ConnectionType}, Confidence: {c.Confidence:P0})"
            ).ToList();

            resp.Answer = $"The following verified cross-case connections were discovered for Case {ctx.CaseNumber}:\n" +
                          string.Join("\n", connectionsList) +
                          "\n\nNote: Cross-case connections represent shared identifiers across independent jurisdictions and do not imply identity equivalence without supervisor review.";

            foreach (var c in ctx.CrossCaseConnections)
            {
                resp.Claims.Add(new CopilotClaimDto
                {
                    Text = $"Cross-case connection between {c.SourceEntityName} and {c.TargetEntityName}",
                    ClaimType = "FACT",
                    SourceIds = c.SupportingEvidence.Count > 0 ? c.SupportingEvidence.Select(e => e.EvidenceId).ToList() : new List<string> { c.SourceEntityId }
                });
            }

            resp.SuggestedFollowUps = new List<string>
            {
                "What evidence supports these cross-case connections?",
                "Which officers are handling the related cases?",
                "View cross-case timeline"
            };
        }
        else
        {
            resp.Answer = $"No approved cross-case connections were found for Case {ctx.CaseNumber} in current investigation data.";
            resp.Claims.Add(new CopilotClaimDto
            {
                Text = $"No cross-case linkages recorded for case {ctx.CaseNumber}",
                ClaimType = "FACT",
                SourceIds = new List<string>()
            });
        }
    }

    private void HandleTimeline(CopilotGroundedContext ctx, CopilotResponseDto resp)
    {
        if (ctx.TimelineEvents.Count > 0)
        {
            var eventItems = ctx.TimelineEvents.Select(e =>
            {
                var timeStr = e.Precision == "DATE_ONLY"
                    ? e.EventTimestampUtc.ToString("dd MMMM yyyy")
                    : e.EventTimestampUtc.ToString("dd MMMM yyyy HH:mm 'UTC'");

                var locStr = !string.IsNullOrWhiteSpace(e.Location) ? $" at {e.Location}" : "";
                return $"• {timeStr}: {e.EventType}{locStr}";
            }).ToList();

            resp.Answer = $"Chronological sequence of {ctx.TimelineEvents.Count} recorded events in Case {ctx.CaseNumber}:\n" +
                          string.Join("\n", eventItems);

            foreach (var ev in ctx.TimelineEvents)
            {
                resp.Claims.Add(new CopilotClaimDto
                {
                    Text = $"{ev.EventType} occurred on {(ev.Precision == "DATE_ONLY" ? ev.EventTimestampUtc.ToString("yyyy-MM-dd") : ev.EventTimestampUtc.ToString("O"))}",
                    ClaimType = "FACT",
                    SourceIds = ev.EvidenceIds
                });
            }

            resp.TimelineCitations = ctx.TimelineEvents;
            resp.SuggestedFollowUps = new List<string>
            {
                "Show geographic locations for these events",
                "Which entities participated in these events?",
                "Are there any temporal alerts or spikes?"
            };
        }
        else
        {
            resp.Answer = $"Insufficient evidence in the current investigation data. No timeline events recorded for Case {ctx.CaseNumber}.";
        }
    }

    private void HandleLocation(CopilotGroundedContext ctx, CopilotResponseDto resp)
    {
        if (ctx.Locations.Count > 0)
        {
            var locItems = ctx.Locations.Select(l =>
                $"• {l.LocationName} ({l.Latitude:F4}, {l.Longitude:F4}) — {l.Description ?? "Recorded location"}"
            ).ToList();

            resp.Answer = $"Geospatial locations recorded for Case {ctx.CaseNumber}:\n" +
                          string.Join("\n", locItems);

            foreach (var l in ctx.Locations)
            {
                resp.Claims.Add(new CopilotClaimDto
                {
                    Text = $"Location {l.LocationName} is recorded at coordinates ({l.Latitude:F4}, {l.Longitude:F4})",
                    ClaimType = "FACT",
                    SourceIds = new List<string> { l.LocationId }
                });
            }

            resp.LocationCitations = ctx.Locations;
            resp.SuggestedFollowUps = new List<string>
            {
                "Show travel sequences between these locations",
                "Did any temporal overlap occur at these coordinates?",
                "Which entities were present at these locations?"
            };
        }
        else
        {
            resp.Answer = $"No physical locations are recorded for this query in Case {ctx.CaseNumber}.";
        }
    }

    private void HandleEvidence(CopilotGroundedContext ctx, CopilotResponseDto resp)
    {
        if (ctx.EvidenceItems.Count > 0)
        {
            var evItems = ctx.EvidenceItems.Select(e =>
                $"• {e.FileName} (ID: {e.EvidenceId}, v{e.EvidenceVersion}) — SHA-256: {e.Sha256Hash[..16]}... [Integrity: {e.IntegrityStatus}]"
            ).ToList();

            resp.Answer = $"Supporting evidence documents cataloged for Case {ctx.CaseNumber}:\n" +
                          string.Join("\n", evItems);

            foreach (var e in ctx.EvidenceItems)
            {
                resp.Claims.Add(new CopilotClaimDto
                {
                    Text = $"Evidence {e.FileName} is registered in storage with SHA-256 {e.Sha256Hash}",
                    ClaimType = "FACT",
                    SourceIds = new List<string> { e.EvidenceId }
                });
            }

            resp.SuggestedFollowUps = new List<string>
            {
                "Verify integrity of these evidence items",
                "Show extracted entities from these files",
                "Show extraction review status"
            };
        }
        else
        {
            resp.Answer = $"No specific evidence files found matching query in Case {ctx.CaseNumber}.";
        }
    }

    private void HandleAlerts(CopilotGroundedContext ctx, CopilotResponseDto resp)
    {
        if (ctx.Alerts.Count > 0)
        {
            var alertItems = ctx.Alerts.Select(a =>
                $"• [{a.Severity}] {a.AlertType} (Score: {a.Score:F2}, Threshold: {a.Threshold:F2}): {a.Explanation}"
            ).ToList();

            resp.Answer = $"Investigative anomaly alerts identified for Case {ctx.CaseNumber}:\n" +
                          string.Join("\n", alertItems) +
                          "\n\nInvestigative Note: Alerts represent anomalous patterns exceeding analytical thresholds; they do not establish guilt or unlawful conduct.";

            foreach (var a in ctx.Alerts)
            {
                resp.Claims.Add(new CopilotClaimDto
                {
                    Text = $"Alert {a.AlertType} triggered with score {a.Score:F2} exceeding threshold {a.Threshold:F2}",
                    ClaimType = "FACT",
                    SourceIds = new List<string> { a.AlertId }
                });
            }

            resp.AlertCitations = ctx.Alerts;
            resp.SuggestedFollowUps = new List<string>
            {
                "What evidence triggered these alerts?",
                "Which entities are involved in high-severity alerts?",
                "Start review on top alert"
            };
        }
        else
        {
            resp.Answer = $"No active investigative anomaly alerts recorded for Case {ctx.CaseNumber}.";
        }
    }

    private void HandleIntegrity(CopilotGroundedContext ctx, CopilotResponseDto resp)
    {
        if (ctx.EvidenceItems.Count > 0)
        {
            var ev = ctx.EvidenceItems[0];
            if (ev.IntegrityStatus == "EVIDENCE_MODIFIED" || ev.IntegrityStatus == "HASH_MISMATCH")
            {
                resp.Answer = $"⚠ INTEGRITY WARNING: Evidence '{ev.FileName}' ({ev.EvidenceId}) currently fails integrity verification. Its physical file hash does not match the immutable ledger block hash. Physical file modifications have been detected.";
                resp.Warnings.Add($"Evidence {ev.EvidenceId} failed cryptographic integrity verification.");
            }
            else
            {
                resp.Answer = $"✓ Evidence '{ev.FileName}' ({ev.EvidenceId}) has a matching SHA-256 fingerprint ({ev.Sha256Hash[..16]}...) and valid append-only blockchain ledger registration. Cryptographic chain-of-custody is intact.";
            }

            resp.Claims.Add(new CopilotClaimDto
            {
                Text = $"Evidence {ev.EvidenceId} integrity status is {ev.IntegrityStatus}",
                ClaimType = "FACT",
                SourceIds = new List<string> { ev.EvidenceId }
            });

            resp.SuggestedFollowUps = new List<string>
            {
                "View full blockchain custody ledger history",
                "Re-verify physical file bytes now",
                "Audit complete blockchain ledger"
            };
        }
        else
        {
            resp.Answer = "No evidence record identified to verify integrity.";
        }
    }

    private void HandleGraphAnalytics(CopilotGroundedContext ctx, CopilotResponseDto resp)
    {
        if (ctx.Entities.Count > 0)
        {
            var topEntities = ctx.Entities.Take(5).Select(e =>
                $"• {e.CanonicalName} ({e.EntityType}) — System confidence: {e.Confidence:P0}"
            ).ToList();

            resp.Answer = $"Graph structural analysis for Case {ctx.CaseNumber} highlights the following prominent entities:\n" +
                          string.Join("\n", topEntities);

            resp.SuggestedFollowUps = new List<string>
            {
                "Who has the highest degree centrality?",
                "Show bridging relationships",
                "Run graph attention network link prediction"
            };
        }
        else
        {
            resp.Answer = $"Insufficient graph data available in Case {ctx.CaseNumber}.";
        }
    }

    private void HandleModelSignals(CopilotGroundedContext ctx, CopilotResponseDto resp)
    {
        if (ctx.ModelPredictions.Count > 0)
        {
            var lead = ctx.ModelPredictions[0];
            resp.ModelSignals.Add(lead);

            resp.Answer = $"The graph model identified a potential relationship (Score: {lead.Score:F2}, Status: Pending Review). This is an algorithmic prediction generated by the Graph Attention Network (GAT) and requires human investigator review. It is not yet a verified relationship.";

            resp.Claims.Add(new CopilotClaimDto
            {
                Text = $"Model predicted potential connection with score {lead.Score:F2}",
                ClaimType = "MODEL_PREDICTION",
                SourceIds = new List<string>()
            });

            resp.SuggestedFollowUps = new List<string>
            {
                "Show verified relationships for these entities",
                "What evidence could corroborate this prediction?",
                "Dismiss or approve this analytical lead"
            };
        }
        else
        {
            resp.Answer = $"No pending model predictions or GAT edge predictions recorded for Case {ctx.CaseNumber}.";
        }
    }

    private void HandleGeneralSummary(CopilotGroundedContext ctx, CopilotResponseDto resp)
    {
        resp.Answer = $"Investigation Summary for Case {ctx.CaseNumber} ({ctx.CaseTitle}):\n" +
                      $"• Total Identified Entities: {ctx.Entities.Count}\n" +
                      $"• Verified Relationships: {ctx.VerifiedRelationships.Count}\n" +
                      $"• Timeline Events: {ctx.TimelineEvents.Count}\n" +
                      $"• Geospatial Locations: {ctx.Locations.Count}\n" +
                      $"• Active Alerts: {ctx.Alerts.Count}\n" +
                      $"• Cataloged Evidence: {ctx.EvidenceItems.Count}";

        resp.SuggestedFollowUps = new List<string>
        {
            "Show the highest priority alerts",
            "Show the case timeline",
            "Who are the most connected entities?",
            "Show cross-case connections"
        };
    }
}
