using System.Text.RegularExpressions;
using Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services.Copilot;

public class CopilotIntentRouter : ICopilotIntentRouter
{
    private readonly IAppDbContext _dbContext;
    private readonly ILogger<CopilotIntentRouter> _logger;

    public CopilotIntentRouter(IAppDbContext dbContext, ILogger<CopilotIntentRouter> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<(string Intent, List<string> ExtractedEntities)> RouteIntentAsync(
        string query,
        string caseId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return ("GENERAL_CASE_SUMMARY", new List<string>());
        }

        var normalizedQuery = query.Trim().ToLowerInvariant();

        // 1. Detect Intent (English & Marathi support)
        string intent;
        if (Regex.IsMatch(normalizedQuery, @"\b(path between|shortest path|how (is|are).*(connected|linked)|connection between|how do.*connect)\b") ||
            Regex.IsMatch(normalizedQuery, @"(कसा जोडलेला|कसे जोडलेले|मार्ग|जोडणारा दुवा|संबंध काय आहे)"))
        {
            intent = "SHORTEST_PATH";
        }
        else if (Regex.IsMatch(normalizedQuery, @"\b(cross[\s-]cases?|other cases?|another case|shared between|in another investigation|across.*cases?)\b") ||
                 Regex.IsMatch(normalizedQuery, @"(इतर गुन्हे|दुसऱ्या प्रकरणात|सामायिक|आंतर-प्रकरण)"))
        {
            intent = "CROSS_CASE";
        }
        else if (Regex.IsMatch(normalizedQuery, @"\b(integrity|tamper|verif(ied|ication|y)|validat(ed|ion)|ledger|blockchain|hash mismatch|sha-256|chain status)\b") ||
                 Regex.IsMatch(normalizedQuery, @"(सत्यता|छेडछाड|पडताळणी|लेजर|हॅश|ब्लॉकचेन)"))
        {
            intent = "INTEGRITY";
        }
        else if (Regex.IsMatch(normalizedQuery, @"\b(alerts?|anomal(y|ies)|suspicious signal|priority alert)\b") ||
                 Regex.IsMatch(normalizedQuery, @"(अलर्ट|सूचना|संशयास्पद|धोका)"))
        {
            intent = "ALERT";
        }
        else if (Regex.IsMatch(normalizedQuery, @"\b(model signal|gat prediction|predicted|predictions?|ai prediction|potential link)\b") ||
                 Regex.IsMatch(normalizedQuery, @"(मॉडेल|संकेत|अंदाज|संभाव्य दुवा)"))
        {
            intent = "MODEL_SIGNAL";
        }
        else if (Regex.IsMatch(normalizedQuery, @"\b(timeline|what happened|sequence|chronology|before|after|when did|events?)\b") ||
                 Regex.IsMatch(normalizedQuery, @"(वेळरेषा|घटना|क्रम|कधी घडले|तारीख)"))
        {
            intent = "TIMELINE";
        }
        else if (Regex.IsMatch(normalizedQuery, @"\b(where was|where did|locations?|geospatial|visited|travel|overlap|places?)\b") ||
                 Regex.IsMatch(normalizedQuery, @"(कुठे|ठिकाण|पत्ता|पत्ते|स्थान)"))
        {
            intent = "LOCATION";
        }
        else if (Regex.IsMatch(normalizedQuery, @"\b(what evidence|which document|proof|source for|supporting evidence|which file|mention(ed)?)\b") ||
                 Regex.IsMatch(normalizedQuery, @"(पुरावा|कागदपत्र|दस्तऐवज|पुरावे)"))
        {
            intent = "EVIDENCE";
        }
        else if (Regex.IsMatch(normalizedQuery, @"\b(centrality|most connected|key entities|broker|bridge node|pagerank|structurally)\b") ||
                 Regex.IsMatch(normalizedQuery, @"(प्रमुख घटक|महत्त्वाचे घटक|मध्यवर्ती|सर्वाधिक जोडलेले)"))
        {
            intent = "GRAPH_ANALYTICS";
        }
        else if (Regex.IsMatch(normalizedQuery, @"\b(who is connected|associates of|contacts of|connected with|relationships of|phones of|vehicles of|associated with|who communicates)\b") ||
                 Regex.IsMatch(normalizedQuery, @"(प्रमुख संबंध|कोण जोडलेले आहे|संबंध कोणते|संबंधितांची नावे|संबंध)"))
        {
            intent = "ENTITY_RELATIONSHIPS";
        }
        else if (Regex.IsMatch(normalizedQuery, @"\b(who is|what is|tell me about|details of|profile of|information on)\b") ||
                 Regex.IsMatch(normalizedQuery, @"(माहिती सांगा|तपशील|कोण आहे|काय आहे)"))
        {
            intent = "ENTITY_LOOKUP";
        }
        else
        {
            intent = "GENERAL_CASE_SUMMARY";
        }

        // 2. Extract Entity Tokens from query and match against Case Entities
        var tokens = new List<string>();

        // Check for quoted tokens: "Person A"
        var quotedMatches = Regex.Matches(query, "\"([^\"]+)\"");
        foreach (Match m in quotedMatches)
        {
            if (m.Groups.Count > 1 && !string.IsNullOrWhiteSpace(m.Groups[1].Value))
            {
                tokens.Add(m.Groups[1].Value.Trim());
            }
        }

        // Load case entity names and identifiers to find entity mentions in query
        var caseEntities = await _dbContext.Entities
            .AsNoTracking()
            .Where(e => e.CaseId == caseId)
            .Select(e => new { e.Id, e.CanonicalName, e.NormalizedValue })
            .ToListAsync(cancellationToken);

        foreach (var entity in caseEntities)
        {
            if (!string.IsNullOrWhiteSpace(entity.CanonicalName) &&
                normalizedQuery.Contains(entity.CanonicalName.ToLowerInvariant()))
            {
                if (!tokens.Contains(entity.CanonicalName))
                {
                    tokens.Add(entity.CanonicalName);
                }
            }
            else if (!string.IsNullOrWhiteSpace(entity.NormalizedValue) &&
                     normalizedQuery.Contains(entity.NormalizedValue.ToLowerInvariant()))
            {
                if (!tokens.Contains(entity.NormalizedValue))
                {
                    tokens.Add(entity.NormalizedValue);
                }
            }
        }

        // If no known entities matched, extract proper nouns (capitalized words) as candidate tokens
        if (tokens.Count == 0)
        {
            var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "What", "When", "Where", "Which", "Who", "Whom", "Whose", "Why", "How",
                "Show", "Tell", "List", "Give", "Find", "Search", "Provide", "Explain",
                "Does", "Did", "Do", "Is", "Are", "Was", "Were", "Can", "Could", "Would",
                "Should", "Has", "Have", "Had", "The", "This", "That", "These", "Those",
                "Case", "Cases", "Investigation", "Evidence", "Files", "Overview", "Summary"
            };

            var wordMatches = Regex.Matches(query, @"\b[A-Z][a-z]+(?:\s+[A-Z][a-z]+)?\b");
            foreach (Match m in wordMatches)
            {
                var val = m.Value.Trim();
                if (!tokens.Contains(val) && val.Length > 2 && !stopWords.Contains(val))
                {
                    tokens.Add(val);
                }
            }
        }

        _logger.LogInformation("Copilot intent classified: {Intent}, extracted tokens: [{Tokens}]", intent, string.Join(", ", tokens));
        return (intent, tokens);
    }

    public async Task<string> ClassifyIntentAsync(
        string caseId,
        string query,
        CancellationToken cancellationToken = default)
    {
        var (intent, _) = await RouteIntentAsync(query, caseId, cancellationToken);
        return intent;
    }

    public string SanitizeInput(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return string.Empty;

        // Strip prompt injection attempts and enforce safe boundary representation
        var sanitized = Regex.Replace(
            query,
            @"(?i)(ignore\s+all\s+previous\s+instructions|system\s+prompt|you\s+are\s+now|act\s+as\s+a|declare\s+.*guilty)",
            "[DISARMED_INSTRUCTION]");

        return sanitized.Trim();
    }
}
