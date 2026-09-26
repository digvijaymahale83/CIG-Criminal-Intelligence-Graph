using Application.Common.Interfaces;
using Application.DTOs;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace Tests.Unit;

public class Phase6TemporalIntelligenceTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly Mock<IAuditService> _mockAudit;
    private readonly TemporalService _temporalService;

    private readonly Case _case1;
    private readonly Case _case2;
    private readonly Evidence _evidence1;
    private readonly Evidence _evidence2;

    public Phase6TemporalIntelligenceTests()
    {
        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(dbOptions);
        _mockAudit = new Mock<IAuditService>();

        _temporalService = new TemporalService(
            _dbContext,
            _mockAudit.Object,
            NullLogger<TemporalService>.Instance);

        // Seed Cases
        _case1 = new Case
        {
            Id = "case-2026-001",
            CaseNumber = "CASE-2026-001",
            Title = "Operation Hawala Network",
            Status = "Active",
            Category = "Financial Crime"
        };
        _case2 = new Case
        {
            Id = "case-2026-002",
            CaseNumber = "CASE-2026-002",
            Title = "Operation Port Intercept",
            Status = "Active",
            Category = "Smuggling"
        };
        _dbContext.Cases.AddRange(_case1, _case2);

        // Seed Evidence
        _evidence1 = new Evidence
        {
            Id = "evd-001",
            CaseId = _case1.Id,
            FileName = "surveillance_log_docks.txt",
            Sha256Hash = "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
            ProcessingStatus = "PROCESSED"
        };
        _evidence2 = new Evidence
        {
            Id = "evd-002",
            CaseId = _case1.Id,
            FileName = "cellular_intercept.csv",
            Sha256Hash = "ca978112ca1bbdcafac231b39a23dc4da786eff8147c4e72b9807785afee48bb",
            ProcessingStatus = "PROCESSED"
        };
        _dbContext.EvidenceItems.AddRange(_evidence1, _evidence2);

        // Seed Entities
        var ent1 = new EntityItem { Id = "ent-rahul", CaseId = _case1.Id, CanonicalName = "Rahul Sharma", Type = "PERSON" };
        var ent2 = new EntityItem { Id = "ent-vikram", CaseId = _case1.Id, CanonicalName = "Vikram Gaikwad", Type = "PERSON" };
        var entLoc = new EntityItem { Id = "ent-docks", CaseId = _case1.Id, CanonicalName = "Shivajinagar Docks", Type = "LOCATION" };
        _dbContext.Entities.AddRange(ent1, ent2, entLoc);

        _dbContext.SaveChanges();
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    [Fact]
    public async Task Test_1_Timeline_Returns_Actual_Events()
    {
        // Arrange
        var ev = new ExtractedEvent
        {
            Id = "evt-01",
            CaseId = _case1.Id,
            EvidenceId = _evidence1.Id,
            EventType = "LOCATION_ACTIVITY",
            Description = "Rahul Sharma observed arriving at docks perimeter.",
            StartTimeUtc = new DateTime(2026, 3, 10, 10, 0, 0, DateTimeKind.Utc),
            EndTimeUtc = new DateTime(2026, 3, 10, 11, 0, 0, DateTimeKind.Utc),
            TimePrecision = "EXACT",
            Location = "Shivajinagar Docks",
            Confidence = 0.95,
            ReviewStatus = "APPROVED"
        };
        _dbContext.ExtractedEvents.Add(ev);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _temporalService.GetCaseTimelineAsync(_case1.Id, new TimelineQueryDto());

        // Assert
        result.Should().NotBeNull();
        result.TotalEvents.Should().Be(1);
        result.Events[0].Id.Should().Be("evt-01");
        result.Events[0].EventType.Should().Be("LOCATION_ACTIVITY");
        result.Events[0].Description.Should().Contain("Rahul Sharma");
    }

    [Fact]
    public async Task Test_2_Timeline_Is_Case_Scoped()
    {
        // Arrange
        _dbContext.ExtractedEvents.AddRange(
            new ExtractedEvent { Id = "evt-c1", CaseId = _case1.Id, EvidenceId = _evidence1.Id, EventType = "VISIT", StartTimeUtc = DateTime.UtcNow },
            new ExtractedEvent { Id = "evt-c2", CaseId = _case2.Id, EvidenceId = _evidence1.Id, EventType = "CALL", StartTimeUtc = DateTime.UtcNow }
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _temporalService.GetCaseTimelineAsync(_case1.Id, new TimelineQueryDto());

        // Assert
        result.Events.Should().HaveCount(1);
        result.Events[0].Id.Should().Be("evt-c1");
    }

    [Fact]
    public async Task Test_3_Authorization_Enforced_For_NonExistent_Case()
    {
        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            _temporalService.GetCaseTimelineAsync("non-existent-case", new TimelineQueryDto()));
    }

    [Fact]
    public async Task Test_4_Date_Filtering_Works()
    {
        // Arrange
        _dbContext.ExtractedEvents.AddRange(
            new ExtractedEvent { Id = "evt-early", CaseId = _case1.Id, EvidenceId = _evidence1.Id, StartTimeUtc = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc) },
            new ExtractedEvent { Id = "evt-mid", CaseId = _case1.Id, EvidenceId = _evidence1.Id, StartTimeUtc = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc) },
            new ExtractedEvent { Id = "evt-late", CaseId = _case1.Id, EvidenceId = _evidence1.Id, StartTimeUtc = new DateTime(2026, 3, 30, 0, 0, 0, DateTimeKind.Utc) }
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var query = new TimelineQueryDto
        {
            StartDate = new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc),
            EndDate = new DateTime(2026, 3, 20, 0, 0, 0, DateTimeKind.Utc)
        };
        var result = await _temporalService.GetCaseTimelineAsync(_case1.Id, query);

        // Assert
        result.Events.Should().HaveCount(1);
        result.Events[0].Id.Should().Be("evt-mid");
    }

    [Fact]
    public async Task Test_5_Event_Type_Filtering_Works()
    {
        // Arrange
        _dbContext.ExtractedEvents.AddRange(
            new ExtractedEvent { Id = "evt-call", CaseId = _case1.Id, EvidenceId = _evidence1.Id, EventType = "CALL" },
            new ExtractedEvent { Id = "evt-visit", CaseId = _case1.Id, EvidenceId = _evidence1.Id, EventType = "VISIT" }
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _temporalService.GetCaseTimelineAsync(_case1.Id, new TimelineQueryDto { EventType = "CALL" });

        // Assert
        result.Events.Should().HaveCount(1);
        result.Events[0].Id.Should().Be("evt-call");
    }

    [Fact]
    public async Task Test_6_Entity_Filtering_Works()
    {
        // Arrange
        _dbContext.ExtractedEvents.AddRange(
            new ExtractedEvent
            {
                Id = "evt-rahul",
                CaseId = _case1.Id,
                EvidenceId = _evidence1.Id,
                RelatedEntitiesJson = System.Text.Json.JsonSerializer.Serialize(new[] { "Rahul Sharma", "Shivajinagar Docks" })
            },
            new ExtractedEvent
            {
                Id = "evt-other",
                CaseId = _case1.Id,
                EvidenceId = _evidence1.Id,
                RelatedEntitiesJson = System.Text.Json.JsonSerializer.Serialize(new[] { "Other Person" })
            }
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _temporalService.GetCaseTimelineAsync(_case1.Id, new TimelineQueryDto { EntityId = "ent-rahul" });

        // Assert
        result.Events.Should().HaveCount(1);
        result.Events[0].Id.Should().Be("evt-rahul");
    }

    [Fact]
    public async Task Test_7_Exact_Timestamps_Remain_Exact()
    {
        // Arrange
        var exactTime = new DateTime(2026, 3, 10, 14, 30, 45, DateTimeKind.Utc);
        _dbContext.ExtractedEvents.Add(new ExtractedEvent
        {
            Id = "evt-exact",
            CaseId = _case1.Id,
            EvidenceId = _evidence1.Id,
            StartTimeUtc = exactTime,
            EndTimeUtc = exactTime,
            TimePrecision = "EXACT"
        });
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _temporalService.GetCaseTimelineAsync(_case1.Id, new TimelineQueryDto());

        // Assert
        result.Events[0].StartTimeUtc.Should().Be(exactTime);
        result.Events[0].TimePrecision.Should().Be("EXACT");
    }

    [Fact]
    public async Task Test_8_Approximate_Timestamps_Retain_Precision()
    {
        // Arrange
        _dbContext.ExtractedEvents.Add(new ExtractedEvent
        {
            Id = "evt-approx",
            CaseId = _case1.Id,
            EvidenceId = _evidence1.Id,
            StartTimeUtc = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            EndTimeUtc = new DateTime(2026, 3, 31, 23, 59, 59, DateTimeKind.Utc),
            TimePrecision = "MONTH"
        });
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _temporalService.GetCaseTimelineAsync(_case1.Id, new TimelineQueryDto());

        // Assert
        result.Events[0].TimePrecision.Should().Be("MONTH");
    }

    [Fact]
    public async Task Test_9_Event_Provenance_Is_Preserved()
    {
        // Arrange
        _dbContext.ExtractedEvents.Add(new ExtractedEvent
        {
            Id = "evt-prov",
            CaseId = _case1.Id,
            EvidenceId = _evidence1.Id,
            SourceLocation = "Page 3, Line 12",
            SourcePage = 3
        });
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _temporalService.GetCaseTimelineAsync(_case1.Id, new TimelineQueryDto());

        // Assert
        result.Events[0].SourceEvidenceId.Should().Be(_evidence1.Id);
        result.Events[0].SourceEvidenceFileName.Should().Be("surveillance_log_docks.txt");
        result.Events[0].SourceEvidenceSha256.Should().Be(_evidence1.Sha256Hash);
        result.Events[0].EvidenceIntegrityVerified.Should().BeTrue();
        result.Events[0].SourcePage.Should().Be(3);
    }

    [Fact]
    public async Task Test_10_Temporal_Overlap_Calculation_Is_Mathematically_Correct()
    {
        // Arrange: E1 from 10:00 to 11:00, E2 from 10:30 to 11:30 at same location
        var e1 = new ExtractedEvent
        {
            Id = "ev-overlap-1",
            CaseId = _case1.Id,
            EvidenceId = _evidence1.Id,
            Location = "Shivajinagar Docks",
            StartTimeUtc = new DateTime(2026, 3, 10, 10, 0, 0, DateTimeKind.Utc),
            EndTimeUtc = new DateTime(2026, 3, 10, 11, 0, 0, DateTimeKind.Utc),
            TimePrecision = "EXACT",
            RelatedEntitiesJson = "[\"Rahul Sharma\"]",
            ReviewStatus = "APPROVED"
        };
        var e2 = new ExtractedEvent
        {
            Id = "ev-overlap-2",
            CaseId = _case1.Id,
            EvidenceId = _evidence2.Id,
            Location = "Shivajinagar Docks",
            StartTimeUtc = new DateTime(2026, 3, 10, 10, 30, 0, DateTimeKind.Utc),
            EndTimeUtc = new DateTime(2026, 3, 10, 11, 30, 0, DateTimeKind.Utc),
            TimePrecision = "EXACT",
            RelatedEntitiesJson = "[\"Vikram Gaikwad\"]",
            ReviewStatus = "APPROVED"
        };
        _dbContext.ExtractedEvents.AddRange(e1, e2);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _temporalService.RunTemporalAnalysisAsync(_case1.Id, new RunTemporalAnalysisRequestDto(), "TestAnalyst");

        // Assert
        result.OverlapsFound.Should().Be(1);
        result.Overlaps[0].DurationMinutes.Should().Be(30.0);
        result.Overlaps[0].OverlapStartUtc.Should().Be(new DateTime(2026, 3, 10, 10, 30, 0, DateTimeKind.Utc));
        result.Overlaps[0].OverlapEndUtc.Should().Be(new DateTime(2026, 3, 10, 11, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public async Task Test_11_Partial_Overlap_Calculation_Is_Correct()
    {
        // Arrange: E1 from 09:00 to 10:15, E2 from 10:00 to 12:00 -> 15 min overlap
        var e1 = new ExtractedEvent
        {
            Id = "ev-part-1",
            CaseId = _case1.Id,
            EvidenceId = _evidence1.Id,
            Location = "Shivajinagar Docks",
            StartTimeUtc = new DateTime(2026, 3, 10, 9, 0, 0, DateTimeKind.Utc),
            EndTimeUtc = new DateTime(2026, 3, 10, 10, 15, 0, DateTimeKind.Utc),
            RelatedEntitiesJson = "[\"Rahul Sharma\"]",
            ReviewStatus = "APPROVED"
        };
        var e2 = new ExtractedEvent
        {
            Id = "ev-part-2",
            CaseId = _case1.Id,
            EvidenceId = _evidence2.Id,
            Location = "Shivajinagar Docks",
            StartTimeUtc = new DateTime(2026, 3, 10, 10, 0, 0, DateTimeKind.Utc),
            EndTimeUtc = new DateTime(2026, 3, 10, 12, 0, 0, DateTimeKind.Utc),
            RelatedEntitiesJson = "[\"Vikram Gaikwad\"]",
            ReviewStatus = "APPROVED"
        };
        _dbContext.ExtractedEvents.AddRange(e1, e2);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _temporalService.RunTemporalAnalysisAsync(_case1.Id, new RunTemporalAnalysisRequestDto(), "TestAnalyst");

        // Assert
        result.Overlaps.Should().HaveCount(1);
        result.Overlaps[0].DurationMinutes.Should().Be(15.0);
    }

    [Fact]
    public async Task Test_12_Non_Overlapping_Events_Do_Not_Produce_Signals()
    {
        // Arrange: E1 10:00-11:00, E2 12:00-13:00 at same location -> No overlap
        _dbContext.ExtractedEvents.AddRange(
            new ExtractedEvent
            {
                Id = "ev-no-1",
                CaseId = _case1.Id,
                EvidenceId = _evidence1.Id,
                Location = "Shivajinagar Docks",
                StartTimeUtc = new DateTime(2026, 3, 10, 10, 0, 0, DateTimeKind.Utc),
                EndTimeUtc = new DateTime(2026, 3, 10, 11, 0, 0, DateTimeKind.Utc),
                RelatedEntitiesJson = "[\"Entity A\"]",
                ReviewStatus = "APPROVED"
            },
            new ExtractedEvent
            {
                Id = "ev-no-2",
                CaseId = _case1.Id,
                EvidenceId = _evidence2.Id,
                Location = "Shivajinagar Docks",
                StartTimeUtc = new DateTime(2026, 3, 10, 12, 0, 0, DateTimeKind.Utc),
                EndTimeUtc = new DateTime(2026, 3, 10, 13, 0, 0, DateTimeKind.Utc),
                RelatedEntitiesJson = "[\"Entity B\"]",
                ReviewStatus = "APPROVED"
            }
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _temporalService.RunTemporalAnalysisAsync(_case1.Id, new RunTemporalAnalysisRequestDto(), "TestAnalyst");

        // Assert
        result.OverlapsFound.Should().Be(0);
        result.Overlaps.Should().BeEmpty();
    }

    [Fact]
    public async Task Test_13_Same_Event_Does_Not_Compare_With_Itself()
    {
        // Arrange
        _dbContext.ExtractedEvents.Add(new ExtractedEvent
        {
            Id = "ev-single",
            CaseId = _case1.Id,
            EvidenceId = _evidence1.Id,
            Location = "Shivajinagar Docks",
            StartTimeUtc = new DateTime(2026, 3, 10, 10, 0, 0, DateTimeKind.Utc),
            EndTimeUtc = new DateTime(2026, 3, 10, 11, 0, 0, DateTimeKind.Utc),
            RelatedEntitiesJson = "[\"Entity A\"]",
            ReviewStatus = "APPROVED"
        });
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _temporalService.RunTemporalAnalysisAsync(_case1.Id, new RunTemporalAnalysisRequestDto(), "TestAnalyst");

        // Assert
        result.OverlapsFound.Should().Be(0);
    }

    [Fact]
    public async Task Test_14_Temporal_Signals_Are_Persisted()
    {
        // Arrange
        _dbContext.ExtractedEvents.AddRange(
            new ExtractedEvent
            {
                Id = "ev-p1",
                CaseId = _case1.Id,
                EvidenceId = _evidence1.Id,
                Location = "Shivajinagar Docks",
                StartTimeUtc = new DateTime(2026, 3, 10, 10, 0, 0, DateTimeKind.Utc),
                EndTimeUtc = new DateTime(2026, 3, 10, 11, 0, 0, DateTimeKind.Utc),
                RelatedEntitiesJson = "[\"Rahul Sharma\"]",
                ReviewStatus = "APPROVED"
            },
            new ExtractedEvent
            {
                Id = "ev-p2",
                CaseId = _case1.Id,
                EvidenceId = _evidence2.Id,
                Location = "Shivajinagar Docks",
                StartTimeUtc = new DateTime(2026, 3, 10, 10, 30, 0, DateTimeKind.Utc),
                EndTimeUtc = new DateTime(2026, 3, 10, 11, 30, 0, DateTimeKind.Utc),
                RelatedEntitiesJson = "[\"Vikram Gaikwad\"]",
                ReviewStatus = "APPROVED"
            }
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _temporalService.RunTemporalAnalysisAsync(_case1.Id, new RunTemporalAnalysisRequestDto(), "TestAnalyst");

        // Assert
        var savedSignals = await _dbContext.TemporalSignals.Where(s => s.CaseId == _case1.Id).ToListAsync();
        savedSignals.Should().HaveCount(1);
        savedSignals[0].Status.Should().Be("PENDING");
        savedSignals[0].DurationMinutes.Should().Be(30.0);
    }

    [Fact]
    public async Task Test_15_Temporal_Signals_Do_Not_Automatically_Create_Graph_Relationships()
    {
        // Arrange
        var initialRelCount = await _dbContext.Relationships.CountAsync();

        _dbContext.ExtractedEvents.AddRange(
            new ExtractedEvent
            {
                Id = "ev-rel1",
                CaseId = _case1.Id,
                EvidenceId = _evidence1.Id,
                Location = "Shivajinagar Docks",
                StartTimeUtc = new DateTime(2026, 3, 10, 10, 0, 0, DateTimeKind.Utc),
                EndTimeUtc = new DateTime(2026, 3, 10, 11, 0, 0, DateTimeKind.Utc),
                RelatedEntitiesJson = "[\"Rahul Sharma\"]",
                ReviewStatus = "APPROVED"
            },
            new ExtractedEvent
            {
                Id = "ev-rel2",
                CaseId = _case1.Id,
                EvidenceId = _evidence2.Id,
                Location = "Shivajinagar Docks",
                StartTimeUtc = new DateTime(2026, 3, 10, 10, 30, 0, DateTimeKind.Utc),
                EndTimeUtc = new DateTime(2026, 3, 10, 11, 30, 0, DateTimeKind.Utc),
                RelatedEntitiesJson = "[\"Vikram Gaikwad\"]",
                ReviewStatus = "APPROVED"
            }
        );
        await _dbContext.SaveChangesAsync();

        // Act
        await _temporalService.RunTemporalAnalysisAsync(_case1.Id, new RunTemporalAnalysisRequestDto(), "TestAnalyst");

        // Assert: Graph relationships must NOT be mutated automatically
        var finalRelCount = await _dbContext.Relationships.CountAsync();
        finalRelCount.Should().Be(initialRelCount);
    }

    [Fact]
    public async Task Test_16_Confirming_Temporal_Signal_Is_Audited()
    {
        // Arrange
        var signal = new TemporalSignal
        {
            Id = "sig-conf-1",
            CaseId = _case1.Id,
            SignalType = "TEMPORAL_OVERLAP",
            Status = "PENDING",
            DurationMinutes = 30.0
        };
        _dbContext.TemporalSignals.Add(signal);
        await _dbContext.SaveChangesAsync();

        // Act
        var reviewResult = await _temporalService.ReviewTemporalSignalAsync(
            signal.Id,
            new ReviewTemporalSignalRequestDto { Status = "CONFIRMED", ReviewNotes = "Confirmed relevant temporal coincidence." },
            "DCP Sharma");

        // Assert
        reviewResult.Status.Should().Be("CONFIRMED");
        var updated = await _dbContext.TemporalSignals.FindAsync(signal.Id);
        updated!.Status.Should().Be("CONFIRMED");
        updated.ReviewedBy.Should().Be("DCP Sharma");

        _mockAudit.Verify(a => a.LogAsync(
            "DCP Sharma",
            "DCP Sharma",
            "TEMPORAL_SIGNAL_CONFIRMED",
            "TemporalSignal",
            signal.Id,
            It.IsAny<string>(),
            It.IsAny<string>(),
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Test_17_Dismissing_Temporal_Signal_Causes_Zero_Graph_Mutation()
    {
        // Arrange
        var initialRelCount = await _dbContext.Relationships.CountAsync();
        var signal = new TemporalSignal
        {
            Id = "sig-dism-1",
            CaseId = _case1.Id,
            SignalType = "TEMPORAL_OVERLAP",
            Status = "PENDING",
            DurationMinutes = 30.0
        };
        _dbContext.TemporalSignals.Add(signal);
        await _dbContext.SaveChangesAsync();

        // Act
        var reviewResult = await _temporalService.ReviewTemporalSignalAsync(
            signal.Id,
            new ReviewTemporalSignalRequestDto { Status = "DISMISSED", ReviewNotes = "Unrelated cargo activity." },
            "DCP Sharma");

        // Assert
        reviewResult.Status.Should().Be("DISMISSED");
        var updated = await _dbContext.TemporalSignals.FindAsync(signal.Id);
        updated!.Status.Should().Be("DISMISSED");

        var finalRelCount = await _dbContext.Relationships.CountAsync();
        finalRelCount.Should().Be(initialRelCount);
    }

    [Fact]
    public async Task Test_18_Cross_Case_Temporal_Analysis_Respects_Authorization()
    {
        // Arrange: Event in Case 1 and Event in Case 2 overlapping at same location
        _dbContext.ExtractedEvents.AddRange(
            new ExtractedEvent
            {
                Id = "ev-cross-1",
                CaseId = _case1.Id,
                EvidenceId = _evidence1.Id,
                Location = "Shivajinagar Docks",
                StartTimeUtc = new DateTime(2026, 3, 10, 10, 0, 0, DateTimeKind.Utc),
                EndTimeUtc = new DateTime(2026, 3, 10, 11, 0, 0, DateTimeKind.Utc),
                RelatedEntitiesJson = "[\"Rahul Sharma\"]",
                ReviewStatus = "APPROVED"
            },
            new ExtractedEvent
            {
                Id = "ev-cross-2",
                CaseId = _case2.Id,
                EvidenceId = _evidence2.Id,
                Location = "Shivajinagar Docks",
                StartTimeUtc = new DateTime(2026, 3, 10, 10, 30, 0, DateTimeKind.Utc),
                EndTimeUtc = new DateTime(2026, 3, 10, 11, 30, 0, DateTimeKind.Utc),
                RelatedEntitiesJson = "[\"R. Sharma\"]",
                ReviewStatus = "APPROVED"
            }
        );
        await _dbContext.SaveChangesAsync();

        // Act 1: Unauthorized run (only case 1 authorized)
        var unauthResult = await _temporalService.RunTemporalAnalysisAsync(
            _case1.Id,
            new RunTemporalAnalysisRequestDto { IncludeCrossCase = true, AuthorizedCaseIds = new List<string> { _case1.Id } },
            "TestAnalyst");

        unauthResult.Overlaps.Count(o => o.SignalType == "CROSS_CASE_TEMPORAL_OVERLAP").Should().Be(0);

        // Act 2: Authorized run (both case 1 and case 2 authorized)
        var authResult = await _temporalService.RunTemporalAnalysisAsync(
            _case1.Id,
            new RunTemporalAnalysisRequestDto { IncludeCrossCase = true, AuthorizedCaseIds = new List<string> { _case1.Id, _case2.Id } },
            "TestAnalyst");

        authResult.Overlaps.Count(o => o.SignalType == "CROSS_CASE_TEMPORAL_OVERLAP").Should().Be(1);
        authResult.Overlaps.First(o => o.SignalType == "CROSS_CASE_TEMPORAL_OVERLAP").DurationMinutes.Should().Be(30.0);
    }

    [Fact]
    public async Task Test_19_Rejected_Events_Are_Excluded_From_Analysis()
    {
        // Arrange: One approved event, one rejected event overlapping at same location
        _dbContext.ExtractedEvents.AddRange(
            new ExtractedEvent
            {
                Id = "ev-appr",
                CaseId = _case1.Id,
                EvidenceId = _evidence1.Id,
                Location = "Shivajinagar Docks",
                StartTimeUtc = new DateTime(2026, 3, 10, 10, 0, 0, DateTimeKind.Utc),
                EndTimeUtc = new DateTime(2026, 3, 10, 11, 0, 0, DateTimeKind.Utc),
                RelatedEntitiesJson = "[\"Entity A\"]",
                ReviewStatus = "APPROVED"
            },
            new ExtractedEvent
            {
                Id = "ev-rej",
                CaseId = _case1.Id,
                EvidenceId = _evidence2.Id,
                Location = "Shivajinagar Docks",
                StartTimeUtc = new DateTime(2026, 3, 10, 10, 30, 0, DateTimeKind.Utc),
                EndTimeUtc = new DateTime(2026, 3, 10, 11, 30, 0, DateTimeKind.Utc),
                RelatedEntitiesJson = "[\"Entity B\"]",
                ReviewStatus = "REJECTED"
            }
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _temporalService.RunTemporalAnalysisAsync(_case1.Id, new RunTemporalAnalysisRequestDto(), "TestAnalyst");

        // Assert: Rejected event must NOT produce any overlap signal
        result.OverlapsFound.Should().Be(0);
    }
}
