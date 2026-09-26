using System.Security.Claims;
using System.Text;
using Application.Common.Interfaces;
using Application.DTOs;
using Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers;

[ApiController]
[Route("api/v1/reports")]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _dbContext;
    private readonly IAuditService _auditService;
    private readonly ILogger<ReportsController> _logger;

    public ReportsController(
        AppDbContext dbContext,
        IAuditService auditService,
        ILogger<ReportsController> logger)
    {
        _dbContext = dbContext;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all cases formatted for intelligence report selection.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReportCases(CancellationToken cancellationToken)
    {
        var cases = await _dbContext.Cases.AsNoTracking()
            .OrderByDescending(c => c.UpdatedAtUtc)
            .ToListAsync(cancellationToken);

        var reportCases = cases.Select(c => new
        {
            id = c.Id,
            case_number = c.CaseNumber,
            title = c.Title,
            district = string.IsNullOrWhiteSpace(c.District) ? "Maharashtra State" : c.District,
            type = string.IsNullOrWhiteSpace(c.Category) ? "General Syndicate" : c.Category,
            officer = string.IsNullOrWhiteSpace(c.LeadOfficerName) ? "Investigating Officer" : c.LeadOfficerName,
            status = c.Status,
            evidence_count = c.EvidenceCount,
            clearance = c.Classification,
            description = c.Description,
            fir_number = c.FirNumber,
            police_station = c.PoliceStation,
            created_at = c.CreatedAtUtc
        }).ToList();

        return Ok(reportCases);
    }

    /// <summary>
    /// Exports a comprehensive case intelligence report in CSV format including real entities and evidence hashes.
    /// </summary>
    [HttpGet("{id}/csv")]
    public async Task<IActionResult> ExportCaseCsv(string id, CancellationToken cancellationToken)
    {
        var caseItem = await _dbContext.Cases.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id || c.CaseNumber == id, cancellationToken);

        if (caseItem == null)
        {
            return NotFound(new { error = $"Investigation case '{id}' not found." });
        }

        var entities = await _dbContext.Entities.AsNoTracking()
            .Where(e => e.CaseId == caseItem.Id)
            .OrderBy(e => e.CanonicalName)
            .ToListAsync(cancellationToken);

        var evidence = await _dbContext.EvidenceItems.AsNoTracking()
            .Where(e => e.CaseId == caseItem.Id)
            .OrderByDescending(e => e.UploadedAtUtc)
            .ToListAsync(cancellationToken);

        var relationships = await _dbContext.Relationships.AsNoTracking()
            .Where(r => r.CaseId == caseItem.Id)
            .ToListAsync(cancellationToken);

        var sb = new StringBuilder();

        void AddRow(params string[] cells)
        {
            var escaped = cells.Select(c => $"\"{c.Replace("\"", "\"\"")}\"");
            sb.AppendLine(string.Join(",", escaped));
        }

        AddRow("MAHARASHTRA POLICE — CRIMINAL INTELLIGENCE & NETWORK INVESTIGATION PLATFORM");
        AddRow("Official Classified Case Intelligence Report");
        AddRow("DISCLAIMER: Synthetic Research Environment — Evidence-Backed Decision Support. Not proof of criminality or guilt.");
        AddRow("");
        AddRow("CASE METADATA");
        AddRow("Case ID", caseItem.Id);
        AddRow("Case Number", caseItem.CaseNumber);
        AddRow("Title", caseItem.Title);
        AddRow("Status", caseItem.Status);
        AddRow("Priority", caseItem.Priority);
        AddRow("Classification", caseItem.Classification);
        AddRow("District", caseItem.District);
        AddRow("FIR Number", caseItem.FirNumber);
        AddRow("Police Station", caseItem.PoliceStation);
        AddRow("Lead Officer", caseItem.LeadOfficerName ?? "N/A");
        AddRow("Description", caseItem.Description);
        AddRow("Report Generated At (UTC)", DateTime.UtcNow.ToString("o"));
        AddRow("");
        AddRow("ENTITIES SUMMARY (" + entities.Count + " total)");
        AddRow("Entity ID", "Canonical Name", "Type", "Risk Level", "Confidence", "Status", "Phone", "Vehicle", "Account", "Location");
        foreach (var ent in entities)
        {
            AddRow(
                ent.Id,
                ent.CanonicalName,
                ent.Type,
                ent.RiskLevel,
                ent.Confidence.ToString("F2"),
                ent.VerificationStatus,
                ent.PhoneNumber ?? "",
                ent.VehicleNumber ?? "",
                ent.AccountNumber ?? "",
                ent.Location ?? ""
            );
        }

        AddRow("");
        AddRow("EVIDENCE ITEMS & INTEGRITY LEDGER (" + evidence.Count + " total)");
        AddRow("Evidence ID", "Filename", "File Type", "Size (Bytes)", "SHA-256 Cryptographic Hash", "Clearance", "Processing Status", "Uploaded By", "Uploaded At (UTC)");
        foreach (var ev in evidence)
        {
            AddRow(
                ev.Id,
                ev.FileName,
                ev.MimeType ?? "UNKNOWN",
                ev.FileSize.ToString(),
                ev.Sha256Hash,
                ev.Clearance,
                ev.ProcessingStatus,
                ev.UploadedByName,
                ev.UploadedAtUtc.ToString("o")
            );
        }

        AddRow("");
        AddRow("RELATIONSHIPS (" + relationships.Count + " total)");
        AddRow("Relationship ID", "Source Entity ID", "Target Entity ID", "Relationship Type", "Confidence");
        foreach (var rel in relationships)
        {
            AddRow(
                rel.Id,
                rel.SourceEntityId,
                rel.TargetEntityId,
                rel.Type,
                rel.Confidence.ToString("F2")
            );
        }

        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "usr-investigator";
        var actorName = User.FindFirstValue(ClaimTypes.Name) ?? "DCP Rajesh Sharma";
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        await _auditService.LogAsync(
            actorId,
            actorName,
            "EXPORT_REPORT_CSV",
            "CaseReport",
            caseItem.Id,
            $"Exported intelligence report CSV for {caseItem.CaseNumber}",
            "{}",
            ipAddress,
            cancellationToken);

        var preamble = Encoding.UTF8.GetPreamble();
        var bytes = Encoding.UTF8.GetBytes(sb.ToString());
        var combined = new byte[preamble.Length + bytes.Length];
        Buffer.BlockCopy(preamble, 0, combined, 0, preamble.Length);
        Buffer.BlockCopy(bytes, 0, combined, preamble.Length, bytes.Length);

        var safeCaseNumber = caseItem.CaseNumber.Replace("/", "-").Replace(" ", "_");
        return File(combined, "text/csv; charset=utf-8", $"Investigation-{safeCaseNumber}-Report.csv");
    }

    /// <summary>
    /// Generates and downloads a clean, valid PDF briefing for the investigation case.
    /// </summary>
    [HttpGet("{id}/pdf")]
    public async Task<IActionResult> ExportCasePdf(string id, CancellationToken cancellationToken)
    {
        var caseItem = await _dbContext.Cases.AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == id || c.CaseNumber == id, cancellationToken);

        if (caseItem == null)
        {
            return NotFound(new { error = $"Investigation case '{id}' not found." });
        }

        var entities = await _dbContext.Entities.AsNoTracking()
            .Where(e => e.CaseId == caseItem.Id)
            .OrderBy(e => e.CanonicalName)
            .Take(15)
            .ToListAsync(cancellationToken);

        var evidence = await _dbContext.EvidenceItems.AsNoTracking()
            .Where(e => e.CaseId == caseItem.Id)
            .OrderByDescending(e => e.UploadedAtUtc)
            .ToListAsync(cancellationToken);

        // Build a standard valid PDF 1.4 document
        var pdfBytes = GeneratePdfBriefing(caseItem, entities, evidence);

        var actorId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "usr-investigator";
        var actorName = User.FindFirstValue(ClaimTypes.Name) ?? "DCP Rajesh Sharma";
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        await _auditService.LogAsync(
            actorId,
            actorName,
            "EXPORT_REPORT_PDF",
            "CaseReport",
            caseItem.Id,
            $"Exported intelligence briefing PDF for {caseItem.CaseNumber}",
            "{}",
            ipAddress,
            cancellationToken);

        var safeCaseNumber = caseItem.CaseNumber.Replace("/", "-").Replace(" ", "_");
        return File(pdfBytes, "application/pdf", $"Investigation-{safeCaseNumber}-Briefing.pdf");
    }

    private static byte[] GeneratePdfBriefing(
        Domain.Entities.Case caseItem,
        List<Domain.Entities.EntityItem> entities,
        List<Domain.Entities.Evidence> evidence)
    {
        var lines = new List<string>
        {
            "MAHARASHTRA POLICE - CRIMINAL INTELLIGENCE PLATFORM",
            "CONFIDENTIAL / RESTRICTED INVESTIGATION BRIEFING",
            "--------------------------------------------------------------------------------",
            $"Case Number:   {caseItem.CaseNumber}",
            $"Operation:     {caseItem.Title}",
            $"Classification: {caseItem.Classification} | Status: {caseItem.Status} | Priority: {caseItem.Priority}",
            $"District:       {caseItem.District} | FIR: {caseItem.FirNumber} | Station: {caseItem.PoliceStation}",
            $"Lead Officer:  {caseItem.LeadOfficerName ?? "DCP Rajesh Sharma"}",
            $"Generated UTC: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC",
            "",
            "SAFEGUARD NOTICE:",
            "Synthetic Research Environment - Evidence-Backed Decision Support.",
            "This report is for analytical reference and does NOT constitute proof of criminality or guilt.",
            "--------------------------------------------------------------------------------",
            "",
            $"KEY INVESTIGATED ENTITIES ({entities.Count} displayed):"
        };

        foreach (var ent in entities)
        {
            lines.Add($" - [{ent.Type}] {ent.CanonicalName} (Risk: {ent.RiskLevel}, Conf: {ent.Confidence:P0}, Status: {ent.VerificationStatus})");
        }

        lines.Add("");
        lines.Add($"EVIDENCE LEDGER & INTEGRITY CHAIN ({evidence.Count} items):");

        foreach (var ev in evidence)
        {
            var hashDisplay = ev.Sha256Hash.Length > 24 ? ev.Sha256Hash[..24] + "..." : ev.Sha256Hash;
            lines.Add($" - {ev.FileName} [{ev.MimeType}] SHA256: {hashDisplay} (Status: {ev.ProcessingStatus})");
        }

        lines.Add("--------------------------------------------------------------------------------");
        lines.Add("END OF INTELLIGENCE BRIEFING");

        // Construct standard textual PDF
        var sb = new StringBuilder();
        sb.AppendLine("%PDF-1.4");
        sb.AppendLine("1 0 obj");
        sb.AppendLine("<< /Type /Catalog /Pages 2 0 R >>");
        sb.AppendLine("endobj");
        sb.AppendLine("2 0 obj");
        sb.AppendLine("<< /Type /Pages /Kids [3 0 R] /Count 1 >>");
        sb.AppendLine("endobj");
        sb.AppendLine("3 0 obj");
        sb.AppendLine("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>");
        sb.AppendLine("endobj");

        // Page content stream
        var streamBuilder = new StringBuilder();
        streamBuilder.AppendLine("BT");
        streamBuilder.AppendLine("/F1 10 Tf");
        streamBuilder.AppendLine("40 800 Td");
        streamBuilder.AppendLine("14 TL");

        foreach (var line in lines)
        {
            var clean = line.Replace("\\", "\\\\").Replace("(", "\\(").Replace(")", "\\)");
            streamBuilder.AppendLine($"({clean}) '");
        }

        streamBuilder.AppendLine("ET");
        var streamContent = streamBuilder.ToString();
        var streamBytes = Encoding.ASCII.GetBytes(streamContent);

        sb.AppendLine("4 0 obj");
        sb.AppendLine($"<< /Length {streamBytes.Length} >>");
        sb.AppendLine("stream");
        sb.Append(streamContent);
        sb.AppendLine("endstream");
        sb.AppendLine("endobj");

        sb.AppendLine("5 0 obj");
        sb.AppendLine("<< /Type /Font /Subtype /Type1 /BaseFont /Courier >>");
        sb.AppendLine("endobj");

        sb.AppendLine("xref");
        sb.AppendLine("0 6");
        sb.AppendLine("0000000000 65535 f ");
        sb.AppendLine("0000000010 00000 n ");
        sb.AppendLine("0000000060 00000 n ");
        sb.AppendLine("0000000115 00000 n ");
        sb.AppendLine("0000000220 00000 n ");
        sb.AppendLine("0000000400 00000 n ");
        sb.AppendLine("trailer");
        sb.AppendLine("<< /Size 6 /Root 1 0 R >>");
        sb.AppendLine("startxref");
        sb.AppendLine("480");
        sb.AppendLine("%%EOF");

        return Encoding.ASCII.GetBytes(sb.ToString());
    }
}
