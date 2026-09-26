using System;

namespace Application.DTOs;

public class TranslationRequestDto
{
    public string Text { get; set; } = string.Empty;
    public string SourceLanguage { get; set; } = "auto"; // "en", "mr", "auto"
    public string TargetLanguage { get; set; } = "mr";   // "en", "mr"
    public string? EvidenceId { get; set; }
}

public class TranslationResponseDto
{
    public string OriginalText { get; set; } = string.Empty;
    public string TranslatedText { get; set; } = string.Empty;
    public string DetectedSourceLanguage { get; set; } = "en";
    public string TargetLanguage { get; set; } = "mr";
    public string Provider { get; set; } = "SystemDictionary"; // "GoogleCloudTranslation", "SystemDictionary"
    public bool IsMachineTranslation { get; set; } = true;
    public string Disclaimer { get; set; } = "Machine translation is provided for investigative reference only and is not authoritative evidence. Original evidence remains unchanged.";
    public DateTime TranslatedAtUtc { get; set; } = DateTime.UtcNow;
}
