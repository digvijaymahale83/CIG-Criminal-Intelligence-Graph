using System.Text;
using Application.Common.Interfaces;
using Application.DTOs;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Security;
using Infrastructure.Services;
using Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Tests.Unit;

public class CaseAndEvidenceServiceTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly AuditService _auditService;
    private readonly CaseService _caseService;
    private readonly Sha256HashService _hashService;
    private readonly LocalFileStorageService _storageService;
    private readonly EvidenceService _evidenceService;
    private readonly AuthService _authService;
    private readonly string _testTempDir;

    public CaseAndEvidenceServiceTests()
    {
        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(dbOptions);
        _auditService = new AuditService(_dbContext, NullLogger<AuditService>.Instance);
        _caseService = new CaseService(_dbContext, _auditService, NullLogger<CaseService>.Instance);

        _testTempDir = Path.Combine(Path.GetTempPath(), "cortex_unit_test_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_testTempDir);

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Storage:LocalPath"] = _testTempDir,
                ["Jwt:SecretKey"] = "cortex-mhp-test-secret-key-32-chars-long-2026!!"
            })
            .Build();

        _storageService = new LocalFileStorageService(config, NullLogger<LocalFileStorageService>.Instance);
        _hashService = new Sha256HashService();
        var mockExtractionService = new Mock<IExtractionService>();
        var ledgerService = new IntegrityLedgerService(_dbContext, _storageService, _hashService, _auditService, NullLogger<IntegrityLedgerService>.Instance);

        _evidenceService = new EvidenceService(
            _dbContext,
            _storageService,
            _hashService,
            _auditService,
            mockExtractionService.Object,
            ledgerService,
            NullLogger<EvidenceService>.Instance);

        var jwtTokenService = new JwtTokenService(config);
        _authService = new AuthService(_dbContext, jwtTokenService, _auditService, NullLogger<AuthService>.Instance);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        if (Directory.Exists(_testTempDir))
        {
            try { Directory.Delete(_testTempDir, true); } catch { }
        }
    }

    [Fact]
    public async Task CreateCaseAsync_ShouldPersistCaseAndEmitAuditLog()
    {
        var request = new CreateCaseDto
        {
            Title = "Cyber Fraud Syndicate - Pune Unit",
            Description = "Investigation into illegal mule accounts across Pune.",
            Priority = "High",
            District = "Pune City",
            Jurisdiction = "Pune Police Commissionerate",
            FirNumber = "FIR-2026-0891",
            PoliceStation = "Shivajinagar Police Station"
        };

        var caseDto = await _caseService.CreateCaseAsync(request, "OFFICER-001", "Insp. R. Sharma", "127.0.0.1");

        caseDto.Should().NotBeNull();
        caseDto.Id.Should().NotBeNullOrWhiteSpace();
        caseDto.CaseNumber.Should().StartWith("MH-");
        caseDto.Title.Should().Be("Cyber Fraud Syndicate - Pune Unit");
        caseDto.Status.Should().Be("Active");

        // Verify in DB
        var savedCase = await _dbContext.Cases.FindAsync(caseDto.Id);
        savedCase.Should().NotBeNull();
        savedCase!.Title.Should().Be(request.Title);

        // Verify audit log
        var logs = await _dbContext.AuditLogs.ToListAsync();
        logs.Should().ContainSingle(l => l.Action == "CASE_CREATED" && l.ResourceId == caseDto.Id);
    }

    [Fact]
    public async Task UploadEvidenceAsync_ShouldComputeAuthenticSha256_AndSaveEvidence()
    {
        // 1. Create a parent case first
        var caseItem = new Case
        {
            Id = Guid.NewGuid().ToString(),
            CaseNumber = "MHP-2026-TEST-001",
            Title = "Test Syndicate Case",
            Status = "Active",
            Priority = "Critical",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Cases.Add(caseItem);
        await _dbContext.SaveChangesAsync();

        // 2. Upload evidence payload
        var fileContent = "CDR Records: Caller +919820012345 to +919820054321, Duration: 420s";
        var fileBytes = Encoding.UTF8.GetBytes(fileContent);
        using var stream = new MemoryStream(fileBytes);

        var expectedHash = _hashService.ComputeSha256Hex(fileBytes);

        var uploadResult = await _evidenceService.UploadEvidenceAsync(
            stream,
            "cdr_dump_jan2026.csv",
            "text/csv",
            fileBytes.Length,
            caseItem.Id,
            "Call detail records for suspect mobile unit",
            "CONFIDENTIAL",
            "OFFICER-002",
            "Sub-Insp. S. Patil",
            "127.0.0.1");

        uploadResult.Should().NotBeNull();
        uploadResult.Sha256Hash.Should().Be(expectedHash);
        uploadResult.CaseId.Should().Be(caseItem.Id);

        // Verify file was actually saved to DB and disk
        var evidenceEntity = await _dbContext.EvidenceItems.FindAsync(uploadResult.Id);
        evidenceEntity.Should().NotBeNull();
        evidenceEntity!.Sha256Hash.Should().Be(expectedHash);
        evidenceEntity.StoragePath.Should().NotBeNullOrWhiteSpace();

        var retrievedStream = await _storageService.GetFileAsync(evidenceEntity.StoragePath);
        retrievedStream.Should().NotBeNull();
        using var ms = new MemoryStream();
        await retrievedStream!.CopyToAsync(ms);
        _hashService.ComputeSha256Hex(ms.ToArray()).Should().Be(expectedHash);

        // Verify audit log
        var logs = await _dbContext.AuditLogs.ToListAsync();
        logs.Should().ContainSingle(l => l.Action == "EVIDENCE_UPLOADED" && l.ResourceId == uploadResult.Id);
    }

    [Fact]
    public async Task AuditLog_ShouldRecordAndRetrieveChronologically()
    {
        await _auditService.LogAsync(
            "OFFICER-1",
            "Insp. Deshmukh",
            "SEARCH_PERFORMED",
            "Entity",
            "ENT-999",
            "Searched suspect aliases",
            "{\"query\":\"Bhai\"}",
            "127.0.0.1");

        await _auditService.LogAsync(
            "OFFICER-1",
            "Insp. Deshmukh",
            "GRAPH_EXPORT",
            "Case",
            "CASE-111",
            "Exported syndicate subgraph",
            null,
            "127.0.0.1");

        var logs = await _auditService.GetLogsAsync(50);
        logs.Should().HaveCount(2);
        logs[0].Action.Should().Be("GRAPH_EXPORT"); // Most recent first
        logs[1].Action.Should().Be("SEARCH_PERFORMED");
    }

    [Fact]
    public async Task AuthService_Login_ShouldAuthenticateValidCredentials_AndRejectInvalid()
    {
        var password = "StrongOfficerPassword123#";
        var user = new User
        {
            Id = Guid.NewGuid().ToString(),
            Email = "lead.investigator@mahapolice.gov.in",
            PasswordHash = PasswordHasher.HashPassword(password),
            FullName = "Inspector Anil Kadam",
            Role = "INVESTIGATOR",
            Agency = "Maharashtra Police",
            BadgeNumber = "MH-CB-9021",
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        // 1. Valid login
        var loginResponse = await _authService.LoginAsync(new LoginRequestDto
        {
            Email = "lead.investigator@mahapolice.gov.in",
            Password = password
        }, "127.0.0.1");

        loginResponse.Should().NotBeNull();
        loginResponse!.Token.Should().NotBeNullOrWhiteSpace();
        loginResponse.User.Email.Should().Be("lead.investigator@mahapolice.gov.in");
        loginResponse.User.FullName.Should().Be("Inspector Anil Kadam");

        // 2. Invalid password
        var invalidResponse = await _authService.LoginAsync(new LoginRequestDto
        {
            Email = "lead.investigator@mahapolice.gov.in",
            Password = "WrongPassword!"
        }, "127.0.0.1");

        invalidResponse.Should().BeNull();
    }
}
