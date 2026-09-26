using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Application.Common.Interfaces;
using Application.DTOs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Services;

public class TranslationService : ITranslationService
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly ILogger<TranslationService> _logger;

    // Built-in investigative terminology dictionary for English <-> Marathi fallback
    private static readonly Dictionary<string, string> EnToMr = new(StringComparer.OrdinalIgnoreCase)
    {
        { "investigation", "तपास" },
        { "evidence", "पुरावा" },
        { "entity", "घटक" },
        { "relationship", "संबंध" },
        { "suspect", "संशयित" },
        { "witness", "साक्षीदार" },
        { "vehicle", "वाहन" },
        { "phone call", "फोन कॉल" },
        { "bank account", "बँक खाते" },
        { "transaction", "व्यवहार" },
        { "location", "ठिकाण" },
        { "address", "पत्ता" },
        { "verified", "सत्यापित" },
        { "unverified", "असत्यापित" },
        { "tamper-evident", "छेडछाड-प्रतिरोधक" },
        { "cryptographic ledger", "क्रिप्टोग्राफिक लेजर" },
        { "network graph", "नेटवर्क आलेख" },
        { "cross-case connection", "आंतर-प्रकरण संबंध" },
        { "high risk", "उच्च धोका" },
        { "critical", "अति-गंभीर" },
        { "alert", "सूचना / अलर्ट" },
        { "first information report", "प्रथम खबरी अहवाल (एफआयआर)" },
        { "statement", "जबाब" },
        { "seizure memo", "जप्ती पंचनामा" },
        { "panchnama", "पंचनामा" },
        { "call detail record", "सीडीआर (कॉल तपशील अहवाल)" }
    };

    private static readonly Dictionary<string, string> MrToEn = new(StringComparer.OrdinalIgnoreCase)
    {
        { "तपास", "investigation" },
        { "पुरावा", "evidence" },
        { "घटक", "entity" },
        { "संबंध", "relationship" },
        { "संशयित", "suspect" },
        { "साक्षीदार", "witness" },
        { "वाहन", "vehicle" },
        { "फोन कॉल", "phone call" },
        { "बँक खाते", "bank account" },
        { "व्यवहार", "transaction" },
        { "ठिकाण", "location" },
        { "पत्ता", "address" },
        { "सत्यापित", "verified" },
        { "असत्यापित", "unverified" },
        { "पंचनामा", "panchnama" },
        { "जप्ती पंचनामा", "seizure memo" },
        { "जबाब", "statement" }
    };

    public TranslationService(
        IConfiguration configuration,
        HttpClient httpClient,
        ILogger<TranslationService> logger)
    {
        _configuration = configuration;
        _httpClient = httpClient;
        _logger = logger;
    }

    public bool IsProviderConfigured()
    {
        var apiKey = _configuration["Translation:GoogleApiKey"] 
            ?? Environment.GetEnvironmentVariable("GOOGLE_TRANSLATE_API_KEY");
        return !string.IsNullOrWhiteSpace(apiKey);
    }

    public async Task<TranslationResponseDto> TranslateEvidenceTextAsync(
        TranslationRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return new TranslationResponseDto
            {
                OriginalText = string.Empty,
                TranslatedText = string.Empty,
                TargetLanguage = request.TargetLanguage
            };
        }

        var apiKey = _configuration["Translation:GoogleApiKey"]
            ?? Environment.GetEnvironmentVariable("GOOGLE_TRANSLATE_API_KEY");

        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            try
            {
                return await TranslateViaGoogleApiAsync(request, apiKey, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Google Cloud Translation failed. Falling back to local dictionary engine: {Message}", ex.Message);
            }
        }

        // Deterministic offline translation preserving investigative identifiers
        return TranslateViaDictionaryEngine(request);
    }

    private async Task<TranslationResponseDto> TranslateViaGoogleApiAsync(
        TranslationRequestDto request,
        string apiKey,
        CancellationToken cancellationToken)
    {
        var targetLang = request.TargetLanguage.ToLowerInvariant().Trim();
        if (targetLang != "mr" && targetLang != "en") targetLang = "mr";

        var url = $"https://translation.googleapis.com/language/translate/v2?key={apiKey}";
        var payload = new
        {
            q = request.Text,
            target = targetLang,
            source = request.SourceLanguage == "auto" ? null : request.SourceLanguage,
            format = "text"
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await _httpClient.PostAsync(url, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        var translated = doc.RootElement
            .GetProperty("data")
            .GetProperty("translations")[0]
            .GetProperty("translatedText")
            .GetString() ?? request.Text;

        var detected = "auto";
        if (doc.RootElement.GetProperty("data").GetProperty("translations")[0].TryGetProperty("detectedSourceLanguage", out var det))
        {
            detected = det.GetString() ?? "auto";
        }

        return new TranslationResponseDto
        {
            OriginalText = request.Text,
            TranslatedText = translated,
            DetectedSourceLanguage = detected,
            TargetLanguage = targetLang,
            Provider = "GoogleCloudTranslation",
            IsMachineTranslation = true,
            Disclaimer = "Machine translation (Google Cloud) provided for investigative reference. Original evidence remains authoritative.",
            TranslatedAtUtc = DateTime.UtcNow
        };
    }

    private TranslationResponseDto TranslateViaDictionaryEngine(TranslationRequestDto request)
    {
        var target = request.TargetLanguage.ToLowerInvariant().Trim();
        var isTargetMarathi = target == "mr";
        var isTargetEnglish = target == "en";

        // Detect if text contains Devanagari script (Marathi/Hindi)
        bool hasDevanagari = Regex.IsMatch(request.Text, @"[\u0900-\u097F]");
        var detectedLang = hasDevanagari ? "mr" : "en";

        // If target is same as detected, return text directly
        if ((hasDevanagari && isTargetMarathi) || (!hasDevanagari && isTargetEnglish))
        {
            return new TranslationResponseDto
            {
                OriginalText = request.Text,
                TranslatedText = request.Text,
                DetectedSourceLanguage = detectedLang,
                TargetLanguage = target,
                Provider = "SystemDictionary",
                IsMachineTranslation = false,
                Disclaimer = "Content is already in the requested language.",
                TranslatedAtUtc = DateTime.UtcNow
            };
        }

        // Apply terminology mapping while shielding invariant tokens (IDs, hashes, phone numbers)
        var translated = request.Text;

        if (isTargetMarathi)
        {
            foreach (var kv in EnToMr)
            {
                translated = Regex.Replace(translated, $@"\b{Regex.Escape(kv.Key)}\b", kv.Value, RegexOptions.IgnoreCase);
            }
        }
        else
        {
            foreach (var kv in MrToEn)
            {
                translated = Regex.Replace(translated, $@"\b{Regex.Escape(kv.Key)}\b", kv.Value, RegexOptions.IgnoreCase);
            }
        }

        return new TranslationResponseDto
        {
            OriginalText = request.Text,
            TranslatedText = translated,
            DetectedSourceLanguage = detectedLang,
            TargetLanguage = target,
            Provider = "SystemDictionary",
            IsMachineTranslation = true,
            Disclaimer = "Machine translation is provided for investigative reference only and is not authoritative evidence. Original evidence remains unchanged.",
            TranslatedAtUtc = DateTime.UtcNow
        };
    }
}
