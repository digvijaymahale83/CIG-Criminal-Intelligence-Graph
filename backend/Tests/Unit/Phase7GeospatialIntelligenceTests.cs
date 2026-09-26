using System.Text.Json;
using Application.Common.Interfaces;
using Application.Common.Models;
using Application.DTOs;
using Domain.Entities;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Tests.Unit;

public class Phase7GeospatialIntelligenceTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly GeospatialService _geospatialService;

    private readonly Case _case1;
    private readonly Case _case2;
    private readonly LocationItem _locPuneShivaji;
    private readonly LocationItem _locPuneCamp;
    private readonly LocationItem _locMumbaiPort;
    private readonly LocationItem _locNashik;
    private readonly EntityItem _entityArjun;
    private readonly EntityItem _entityVikram;
    private readonly Evidence _evidence1;

    public Phase7GeospatialIntelligenceTests()
    {
        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new AppDbContext(dbOptions);

        _geospatialService = new GeospatialService(
            _dbContext,
            NullLogger<GeospatialService>.Instance);

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
            Sha256Hash = "a3f5e92b8c7d1e4f5a6b7c8d9e0f1a2b3c4d5e6f7a8b9c0d1e2f3a4b5c6d7e8f",
            StoragePath = "/storage/ev-001.pdf",
            ProcessingStatus = "PROCESSED",
            UploadedByName = "inspector_deshmukh",
            UploadedAtUtc = DateTime.UtcNow
        };
        _dbContext.EvidenceItems.Add(_evidence1);

        // Seed Canonical Locations
        _locPuneShivaji = new LocationItem
        {
            Id = "loc-pune-shivaji",
            CaseId = _case1.Id,
            Name = "Pune Shivajinagar Hub",
            NormalizedName = "PUNE SHIVAJINAGAR HUB",
            Address = "Shivajinagar Goods Terminal, Pune, Maharashtra 411005",
            Latitude = 18.5314,
            Longitude = 73.8446,
            GeocodePrecision = "BUILDING",
            Source = "SOURCE_DATA",
            City = "Pune",
            State = "Maharashtra",
            Country = "India",
            CreatedAtUtc = DateTime.UtcNow
        };

        _locPuneCamp = new LocationItem
        {
            Id = "loc-pune-camp",
            CaseId = _case1.Id,
            Name = "Pune Camp Office",
            NormalizedName = "PUNE CAMP OFFICE",
            Address = "MG Road, Pune Camp, Pune, Maharashtra 411001",
            Latitude = 18.5167,
            Longitude = 73.8750,
            GeocodePrecision = "STREET",
            Source = "SOURCE_DATA",
            City = "Pune",
            State = "Maharashtra",
            Country = "India",
            CreatedAtUtc = DateTime.UtcNow
        };

        _locMumbaiPort = new LocationItem
        {
            Id = "loc-mumbai-port",
            CaseId = _case1.Id,
            Name = "Mumbai Port Trust Area",
            NormalizedName = "MUMBAI PORT TRUST AREA",
            Address = "Indira Dock, Mumbai Port Trust, Mumbai 400001",
            Latitude = 18.9438,
            Longitude = 72.8387,
            GeocodePrecision = "AREA",
            Source = "SOURCE_DATA",
            City = "Mumbai",
            State = "Maharashtra",
            Country = "India",
            CreatedAtUtc = DateTime.UtcNow
        };

        _locNashik = new LocationItem
        {
            Id = "loc-nashik-hub",
            CaseId = _case2.Id, // Belonging to Case 2
            Name = "Nashik Warehouse",
            NormalizedName = "NASHIK WAREHOUSE",
            Address = "Ambad Industrial Area, Nashik 422010",
            Latitude = 19.9575,
            Longitude = 73.7380,
            GeocodePrecision = "AREA",
            Source = "SOURCE_DATA",
            City = "Nashik",
            State = "Maharashtra",
            Country = "India",
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Locations.AddRange(_locPuneShivaji, _locPuneCamp, _locMumbaiPort, _locNashik);

        // Seed Entities
        _entityArjun = new EntityItem
        {
            Id = "ent-arjun",
            CaseId = _case1.Id,
            CanonicalName = "Arjun Verma",
            Type = "PERSON",
            VerificationStatus = "VERIFIED",
            Confidence = 0.95,
            Latitude = 18.5314,
            Longitude = 73.8446,
            CreatedAtUtc = DateTime.UtcNow
        };

        _entityVikram = new EntityItem
        {
            Id = "ent-vikram",
            CaseId = _case1.Id,
            CanonicalName = "Vikram Singhania",
            Type = "PERSON",
            VerificationStatus = "VERIFIED",
            Confidence = 0.92,
            Latitude = 18.5314,
            Longitude = 73.8446,
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.Entities.AddRange(_entityArjun, _entityVikram);

        // Seed Extracted Events
        var baseDate = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

        var event1 = new ExtractedEvent
        {
            Id = "evt-1",
            CaseId = _case1.Id,
            EvidenceId = _evidence1.Id,
            Evidence = _evidence1,
            Description = "Terminal Cargo Dispatch",
            EventType = "COMMUNICATION",
            StartTimeUtc = baseDate,
            EndTimeUtc = baseDate.AddHours(1),
            Location = _locPuneShivaji.Name,
            LocationEntityId = _locPuneShivaji.Id,
            RelatedEntitiesJson = JsonSerializer.Serialize(new[] { _entityArjun.CanonicalName }),
            ReviewStatus = "APPROVED",
            CreatedAtUtc = DateTime.UtcNow
        };

        var event2 = new ExtractedEvent
        {
            Id = "evt-2",
            CaseId = _case1.Id,
            EvidenceId = _evidence1.Id,
            Evidence = _evidence1,
            Description = "Port Handover Meeting",
            EventType = "MEETING",
            StartTimeUtc = baseDate.AddHours(4), // 4 hours later in Mumbai (~120km away -> ~30km/h: plausible)
            EndTimeUtc = baseDate.AddHours(5),
            Location = _locMumbaiPort.Name,
            LocationEntityId = _locMumbaiPort.Id,
            RelatedEntitiesJson = JsonSerializer.Serialize(new[] { _entityArjun.CanonicalName }),
            ReviewStatus = "APPROVED",
            CreatedAtUtc = DateTime.UtcNow
        };

        var event3CoPresence = new ExtractedEvent
        {
            Id = "evt-3",
            CaseId = _case1.Id,
            EvidenceId = _evidence1.Id,
            Evidence = _evidence1,
            Description = "Terminal Presence Log",
            EventType = "FINANCIAL_TRANSACTION",
            StartTimeUtc = baseDate.AddMinutes(30), // Co-present at Pune Shivaji within 30 mins!
            EndTimeUtc = baseDate.AddHours(1),
            Location = _locPuneShivaji.Name,
            LocationEntityId = _locPuneShivaji.Id,
            RelatedEntitiesJson = JsonSerializer.Serialize(new[] { _entityVikram.CanonicalName }),
            ReviewStatus = "APPROVED",
            CreatedAtUtc = DateTime.UtcNow
        };

        _dbContext.ExtractedEvents.AddRange(event1, event2, event3CoPresence);
        _dbContext.SaveChanges();
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _dbContext.Dispose();
    }

    // ==========================================
    // 1. GEOMATH TESTS
    // ==========================================

    [Fact]
    public void GeoMath_CoordinateValidation_ShouldValidateCorrectly()
    {
        GeoMath.IsValidCoordinate(18.5204, 73.8567).Should().BeTrue();
        GeoMath.IsValidCoordinate(0.0, 0.0).Should().BeTrue();
        GeoMath.IsValidCoordinate(-90.0, -180.0).Should().BeTrue();
        GeoMath.IsValidCoordinate(90.0, 180.0).Should().BeTrue();

        GeoMath.IsValidCoordinate(91.0, 73.8567).Should().BeFalse();
        GeoMath.IsValidCoordinate(-90.1, 73.8567).Should().BeFalse();
        GeoMath.IsValidCoordinate(18.5204, 180.5).Should().BeFalse();
        GeoMath.IsValidCoordinate(18.5204, -180.5).Should().BeFalse();
        GeoMath.IsValidCoordinate(double.NaN, 0.0).Should().BeFalse();
        GeoMath.IsValidCoordinate(0.0, double.PositiveInfinity).Should().BeFalse();
    }

    [Fact]
    public void GeoMath_HaversineDistance_IdenticalCoordinates_ShouldReturnZero()
    {
        double dist = GeoMath.HaversineDistanceKm(18.5204, 73.8567, 18.5204, 73.8567);
        dist.Should().Be(0.0);
    }

    [Fact]
    public void GeoMath_HaversineDistance_PuneToMumbai_ShouldBeApprox120Km()
    {
        // Pune (18.5204, 73.8567) to Mumbai Port Trust (18.9438, 72.8387)
        double dist = GeoMath.HaversineDistanceKm(18.5204, 73.8567, 18.9438, 72.8387);
        dist.Should().BeInRange(115.0, 125.0);
    }

    [Fact]
    public void GeoMath_VelocityCalculation_ShouldComputePlausibleAndImplausibleSpeeds()
    {
        // Plausible: 120km in 4 hours = 30 km/h
        double speed = GeoMath.CalculateSpeedKmh(120.0, 4.0);
        speed.Should().BeApproximately(30.0, 0.1);
        GeoMath.IsImplausibleVelocity(120.0, 4.0).Should().BeFalse();

        // Implausible: 1500km in 0.5 hours = 3000 km/h (> 900 km/h threshold)
        double supersonicSpeed = GeoMath.CalculateSpeedKmh(1500.0, 0.5);
        supersonicSpeed.Should().BeApproximately(3000.0, 0.1);
        GeoMath.IsImplausibleVelocity(1500.0, 0.5, 900.0).Should().BeTrue();

        // Zero duration edge case
        GeoMath.CalculateSpeedKmh(100.0, 0.0).Should().Be(0.0);
    }

    // ==========================================
    // 2. MAP DATA SCOPING & LAYERING
    // ==========================================

    [Fact]
    public async Task GetCaseMapDataAsync_ShouldReturnScopedCaseLocationsAndClusters()
    {
        var result = await _geospatialService.GetCaseMapDataAsync(_case1.Id);

        result.Should().NotBeNull();
        result.CaseId.Should().Be(_case1.Id);
        result.TotalLocations.Should().Be(3); // Shivaji, Camp, Mumbai Port in Case 1
        result.Locations.Should().Contain(l => l.Name == "Pune Shivajinagar Hub");
        result.Locations.Should().Contain(l => l.Name == "Pune Camp Office");
        result.Locations.Should().Contain(l => l.Name == "Mumbai Port Trust Area");
        // Case 2 location should NOT be present
        result.Locations.Should().NotContain(l => l.Name == "Nashik Warehouse");

        // Clusters: Pune locations are ~4km apart, so they form 1 cluster; Mumbai is ~120km away, forming a 2nd cluster
        result.Clusters.Should().HaveCount(2);
    }

    // ==========================================
    // 3. LOCATION ACTIVITY & EVIDENCE PROVENANCE
    // ==========================================

    [Fact]
    public async Task GetLocationActivityAsync_ShouldReturnAssociatedEventsAndEntitiesWithHash()
    {
        var activity = await _geospatialService.GetLocationActivityAsync(_locPuneShivaji.Id, _case1.Id);

        activity.Should().NotBeNull();
        activity!.Location.Id.Should().Be(_locPuneShivaji.Id);
        activity.Location.Name.Should().Be("Pune Shivajinagar Hub");
        activity.Events.Should().HaveCount(2); // evt-1 and evt-3
        activity.Entities.Should().HaveCount(2); // Arjun and Vikram
        activity.Entities.Should().Contain(e => e.EntityName == "Arjun Verma");
        activity.Entities.Should().Contain(e => e.EntityName == "Vikram Singhania");

        // Check evidence provenance citations
        activity.EvidenceRecords.Should().NotBeEmpty();
        activity.EvidenceRecords.First().Sha256Hash.Should().Be(_evidence1.Sha256Hash);
        activity.EvidenceRecords.First().EvidenceIntegrityVerified.Should().BeTrue();
    }

    [Fact]
    public async Task GetLocationActivityAsync_NonExistentLocation_ShouldReturnNull()
    {
        var result = await _geospatialService.GetLocationActivityAsync("non-existent-id", _case1.Id);
        result.Should().BeNull();
    }

    // ==========================================
    // 4. PROXIMITY SEARCH
    // ==========================================

    [Fact]
    public async Task GetSpatialProximityAsync_ShouldFilterByRadiusCorrectly()
    {
        // Search centered at Pune Shivaji (18.5314, 73.8446) with 10km radius
        var matches = await _geospatialService.GetSpatialProximityAsync(_case1.Id, 18.5314, 73.8446, 10.0);

        matches.Should().NotBeNull();
        // Should find Shivaji (0 km) and Pune Camp (~4.2 km)
        matches.Should().HaveCount(2);
        matches[0].DistanceKm.Should().BeApproximately(0.0, 0.1);
        matches[1].DistanceKm.Should().BeInRange(3.5, 5.0);

        // Mumbai Port (~120km away) must NOT be included
        matches.Should().NotContain(m => m.Name == "Mumbai Port Trust Area");
    }

    [Fact]
    public async Task GetSpatialProximityAsync_InvalidCoordinates_ShouldThrowArgumentException()
    {
        Func<Task> act = async () => await _geospatialService.GetSpatialProximityAsync(_case1.Id, 95.0, 73.8446, 10.0);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Invalid geographic coordinates*");
    }

    [Fact]
    public async Task GetSpatialProximityAsync_InvalidRadius_ShouldThrowArgumentException()
    {
        Func<Task> act = async () => await _geospatialService.GetSpatialProximityAsync(_case1.Id, 18.5314, 73.8446, -5.0);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Radius must be greater than zero*");
    }

    // ==========================================
    // 5. ENTITY TRAVEL SEQUENCE
    // ==========================================

    [Fact]
    public async Task GetEntityTravelSequenceAsync_ShouldCalculateDeltasAndSpeedsCorrectly()
    {
        var sequence = await _geospatialService.GetEntityTravelSequenceAsync(_entityArjun.Id, _case1.Id);

        sequence.Should().NotBeNull();
        sequence.EntityId.Should().Be(_entityArjun.Id);
        sequence.Steps.Should().HaveCount(2);

        // Step 1: Pune Shivaji (Origin)
        sequence.Steps[0].StepIndex.Should().Be(1);
        sequence.Steps[0].LocationName.Should().Be("Pune Shivajinagar Hub");
        sequence.Steps[0].DistanceKmFromPrevious.Should().Be(0.0);

        // Step 2: Mumbai Port (4 hours later, ~120km away)
        sequence.Steps[1].StepIndex.Should().Be(2);
        sequence.Steps[1].LocationName.Should().Be("Mumbai Port Trust Area");
        sequence.Steps[1].DistanceKmFromPrevious.Should().BeInRange(115.0, 125.0);
        sequence.Steps[1].ElapsedHoursFromPrevious.Should().BeApproximately(4.0, 0.1);
        sequence.Steps[1].ImpliedSpeedKmh.Should().BeInRange(25.0, 35.0);
        sequence.Steps[1].IsImplausibleSpeed.Should().BeFalse();
    }

    [Fact]
    public async Task GetEntityTravelSequenceAsync_ImplausibleSpeed_ShouldFlagWarning()
    {
        // Add an implausible event: Arjun jumps from Mumbai to Delhi (1150km away) in 30 minutes
        var delhiLoc = new LocationItem
        {
            Id = "loc-delhi",
            CaseId = _case1.Id,
            Name = "Delhi Cargo Hub",
            NormalizedName = "DELHI CARGO HUB",
            Address = "IGI Cargo Terminal, New Delhi",
            Latitude = 28.5562,
            Longitude = 77.1000,
            GeocodePrecision = "AREA",
            Source = "SOURCE_DATA",
            City = "Delhi",
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.Locations.Add(delhiLoc);

        var supersonicEvt = new ExtractedEvent
        {
            Id = "evt-delhi-supersonic",
            CaseId = _case1.Id,
            EvidenceId = _evidence1.Id,
            Description = "Delhi Terminal Log",
            EventType = "COMMUNICATION",
            StartTimeUtc = new DateTime(2026, 1, 15, 14, 30, 0, DateTimeKind.Utc), // 30 mins after Mumbai event!
            EndTimeUtc = new DateTime(2026, 1, 15, 15, 0, 0, DateTimeKind.Utc),
            Location = delhiLoc.Name,
            LocationEntityId = delhiLoc.Id,
            RelatedEntitiesJson = JsonSerializer.Serialize(new[] { _entityArjun.CanonicalName }),
            ReviewStatus = "APPROVED",
            CreatedAtUtc = DateTime.UtcNow
        };
        _dbContext.ExtractedEvents.Add(supersonicEvt);
        await _dbContext.SaveChangesAsync();

        var sequence = await _geospatialService.GetEntityTravelSequenceAsync(_entityArjun.Id, _case1.Id);

        sequence.Steps.Should().HaveCount(3);
        // Step 3 (Delhi): >1100km in 0.5 hours -> >2200 km/h -> IsImplausibleSpeed = true
        var delhiStep = sequence.Steps.First(s => s.LocationName == "Delhi Cargo Hub");
        delhiStep.IsImplausibleSpeed.Should().BeTrue();
        delhiStep.ImpliedSpeedKmh.Should().BeGreaterThan(2000.0);
    }

    // ==========================================
    // 6. SPATIAL ANALYSIS & OVERLAP DETECTION
    // ==========================================

    [Fact]
    public async Task RunSpatialAnalysisAsync_ShouldDetectSpatialTemporalOverlap()
    {
        // In our seed data, Arjun and Vikram are both logged at Pune Shivaji within 30 minutes of each other
        var req = new RunSpatialAnalysisRequestDto
        {
            ClusterRadiusKm = 25.0,
            VelocityWarningThresholdKmh = 900.0
        };

        var runResult = await _geospatialService.RunSpatialAnalysisAsync(_case1.Id, req, "ACTOR-001");

        runResult.Should().NotBeNull();
        runResult.CaseId.Should().Be(_case1.Id);
        runResult.Status.Should().Be("COMPLETED");
        runResult.SignalsGenerated.Should().BeGreaterThanOrEqualTo(1);

        // Fetch staged signals
        var signals = await _geospatialService.GetSpatialSignalsAsync(_case1.Id);
        signals.Should().NotBeEmpty();

        var overlapSignal = signals.FirstOrDefault(s => s.SignalType == "SPATIAL_TEMPORAL_OVERLAP");
        overlapSignal.Should().NotBeNull();
        overlapSignal!.Status.Should().Be("PENDING");
        overlapSignal.Explanation.Should().Contain("Pune Shivajinagar Hub");
    }

    // ==========================================
    // 7. REVIEW WORKFLOW & ZERO GRAPH MUTATION
    // ==========================================

    [Fact]
    public async Task ReviewSpatialSignalAsync_ConfirmSignal_MustNeverMutateKnowledgeGraph()
    {
        // First run analysis to generate signal
        var runResult = await _geospatialService.RunSpatialAnalysisAsync(_case1.Id, new RunSpatialAnalysisRequestDto(), "ACTOR-001");
        var signals = await _geospatialService.GetSpatialSignalsAsync(_case1.Id, "PENDING");
        var targetSignal = signals.First();

        // Record initial entity count and relationship count
        int initialEntityCount = await _dbContext.Entities.CountAsync();
        int initialRelationshipCount = await _dbContext.Relationships.CountAsync();

        // Review signal to CONFIRMED
        var reviewDto = new ReviewSpatialSignalRequestDto
        {
            Status = "CONFIRMED",
            ReviewNotes = "Verified co-presence against security checkpoint log."
        };

        var reviewed = await _geospatialService.ReviewSpatialSignalAsync(_case1.Id, targetSignal.Id, reviewDto, "INVESTIGATOR-42");

        reviewed.Status.Should().Be("CONFIRMED");
        reviewed.ReviewedBy.Should().Be("INVESTIGATOR-42");
        reviewed.ReviewedAtUtc.Should().NotBeNull();
        reviewed.ReviewNotes.Should().Be("Verified co-presence against security checkpoint log.");

        // CRITICAL GUARANTEE: Zero graph mutation!
        int afterEntityCount = await _dbContext.Entities.CountAsync();
        int afterRelationshipCount = await _dbContext.Relationships.CountAsync();

        afterEntityCount.Should().Be(initialEntityCount, "Confirming a spatial signal must NEVER create new graph nodes automatically");
        afterRelationshipCount.Should().Be(initialRelationshipCount, "Confirming a spatial signal must NEVER synthesize or mutate graph edges automatically");
    }

    [Fact]
    public async Task ReviewSpatialSignalAsync_DismissSignal_ShouldUpdateStatus()
    {
        var runResult = await _geospatialService.RunSpatialAnalysisAsync(_case1.Id, new RunSpatialAnalysisRequestDto(), "ACTOR-001");
        var signals = await _geospatialService.GetSpatialSignalsAsync(_case1.Id, "PENDING");
        var targetSignal = signals.First();

        var reviewDto = new ReviewSpatialSignalRequestDto
        {
            Status = "DISMISSED",
            ReviewNotes = "Coincidental proximity during peak commute hours; excluded."
        };

        var reviewed = await _geospatialService.ReviewSpatialSignalAsync(_case1.Id, targetSignal.Id, reviewDto, "SUPERVISOR-99");

        reviewed.Status.Should().Be("DISMISSED");
        reviewed.ReviewedBy.Should().Be("SUPERVISOR-99");
    }

    // ==========================================
    // 8. CROSS-CASE ANALYSIS ISOLATION & FILTERING
    // ==========================================

    [Fact]
    public async Task RunSpatialAnalysis_WithCrossCaseFalse_ShouldNotFlagOtherCaseLocations()
    {
        var req = new RunSpatialAnalysisRequestDto
        {
            IncludeCrossCase = false
        };

        var runResult = await _geospatialService.RunSpatialAnalysisAsync(_case1.Id, req, "ACTOR-001");

        // Signals must not include Nashik Warehouse (which belongs to Case 2)
        var signals = await _geospatialService.GetSpatialSignalsAsync(_case1.Id);
        signals.Should().NotContain(s => s.LocationName == "Nashik Warehouse");
    }

    [Fact]
    public async Task GetCaseLocationsAsync_WithSearchTerm_ShouldFilterCorrectly()
    {
        var locations = await _geospatialService.GetCaseLocationsAsync(_case1.Id, "Mumbai");
        locations.Should().HaveCount(1);
        locations.First().Name.Should().Be("Mumbai Port Trust Area");
    }
}
