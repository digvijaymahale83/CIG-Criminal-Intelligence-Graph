using Application.Common.Interfaces;
using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Case> Cases => Set<Case>();
    public DbSet<Evidence> EvidenceItems => Set<Evidence>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<EntityItem> Entities => Set<EntityItem>();
    public DbSet<Relationship> Relationships => Set<Relationship>();
    public DbSet<ExtractionJob> ExtractionJobs => Set<ExtractionJob>();
    public DbSet<ExtractedEntity> ExtractedEntities => Set<ExtractedEntity>();
    public DbSet<ExtractedRelationship> ExtractedRelationships => Set<ExtractedRelationship>();
    public DbSet<ExtractedEvent> ExtractedEvents => Set<ExtractedEvent>();
    public DbSet<RelationshipEvidence> RelationshipEvidences => Set<RelationshipEvidence>();
    public DbSet<EntityMatchCandidate> EntityMatchCandidates => Set<EntityMatchCandidate>();
    public DbSet<CrossCaseConnection> CrossCaseConnections => Set<CrossCaseConnection>();
    public DbSet<GraphAnalysisRun> GraphAnalysisRuns => Set<GraphAnalysisRun>();
    public DbSet<GraphNodeMetrics> GraphNodeMetrics => Set<GraphNodeMetrics>();
    public DbSet<GraphAnalyticalLead> GraphAnalyticalLeads => Set<GraphAnalyticalLead>();
    public DbSet<TemporalAnalysisRun> TemporalAnalysisRuns => Set<TemporalAnalysisRun>();
    public DbSet<TemporalSignal> TemporalSignals => Set<TemporalSignal>();
    public DbSet<LocationItem> Locations => Set<LocationItem>();
    public DbSet<SpatialSignal> SpatialSignals => Set<SpatialSignal>();
    public DbSet<SpatialAnalysisRun> SpatialAnalysisRuns => Set<SpatialAnalysisRun>();
    public DbSet<Alert> Alerts => Set<Alert>();
    public DbSet<AlertRun> AlertRuns => Set<AlertRun>();
    public DbSet<EvidenceLedgerBlock> EvidenceLedgerBlocks => Set<EvidenceLedgerBlock>();

    public async Task<bool> CanConnectAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await Database.CanConnectAsync(cancellationToken);
        }
        catch
        {
            return false;
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // 1. User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);
            entity.HasIndex(u => u.Email).IsUnique();
            entity.Property(u => u.Email).HasMaxLength(255).IsRequired();
            entity.Property(u => u.FullName).HasMaxLength(255).IsRequired();
            entity.Property(u => u.Role).HasMaxLength(50).IsRequired();
            entity.Property(u => u.BadgeNumber).HasMaxLength(50);
            entity.Property(u => u.Agency).HasMaxLength(100);
        });

        // 2. Case
        modelBuilder.Entity<Case>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.HasIndex(c => c.CaseNumber).IsUnique();
            entity.Property(c => c.CaseNumber).HasMaxLength(100).IsRequired();
            entity.Property(c => c.Title).HasMaxLength(500).IsRequired();
            entity.Property(c => c.Status).HasMaxLength(50).IsRequired();
            entity.Property(c => c.Priority).HasMaxLength(50).IsRequired();
            entity.Property(c => c.Classification).HasMaxLength(50);

            entity.HasMany(c => c.EvidenceItems)
                  .WithOne(e => e.Case)
                  .HasForeignKey(e => e.CaseId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasMany(c => c.Entities)
                  .WithOne(e => e.Case)
                  .HasForeignKey(e => e.CaseId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // 3. Evidence
        modelBuilder.Entity<Evidence>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Sha256Hash);
            entity.HasIndex(e => e.CaseId);
            entity.Property(e => e.FileName).HasMaxLength(500).IsRequired();
            entity.Property(e => e.Sha256Hash).HasMaxLength(64).IsRequired();
            entity.Property(e => e.MimeType).HasMaxLength(150);
            entity.Property(e => e.ProcessingStatus).HasMaxLength(50).IsRequired();
            entity.Property(e => e.Version).HasDefaultValue(1);

            entity.HasMany(e => e.ExtractionJobs)
                  .WithOne(j => j.Evidence)
                  .HasForeignKey(j => j.EvidenceId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.ExtractedEntities)
                  .WithOne(ent => ent.Evidence)
                  .HasForeignKey(ent => ent.EvidenceId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.ExtractedRelationships)
                  .WithOne(r => r.Evidence)
                  .HasForeignKey(r => r.EvidenceId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 4. AuditLog
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.HasIndex(a => a.CreatedAtUtc);
            entity.HasIndex(a => a.Action);
            entity.Property(a => a.Action).HasMaxLength(100).IsRequired();
            entity.Property(a => a.ActorName).HasMaxLength(255);
            entity.Property(a => a.ResourceType).HasMaxLength(100);
        });

        // 5. EntityItem (Canonical / Promoted)
        modelBuilder.Entity<EntityItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CanonicalName);
            entity.HasIndex(e => e.Type);
            entity.Property(e => e.Type).HasMaxLength(50).IsRequired();
            entity.Property(e => e.CanonicalName).HasMaxLength(255).IsRequired();
            entity.Property(e => e.NormalizedValue).HasMaxLength(255);
            entity.Property(e => e.VerificationStatus).HasMaxLength(50).IsRequired();
        });

        // 6. Relationship (Canonical / Promoted)
        modelBuilder.Entity<Relationship>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.HasIndex(r => new { r.SourceEntityId, r.TargetEntityId });
            entity.Property(r => r.Type).HasMaxLength(50).IsRequired();
        });

        // 6b. RelationshipEvidence (Multi-evidence Provenance Mapping)
        modelBuilder.Entity<RelationshipEvidence>(entity =>
        {
            entity.HasKey(re => re.Id);
            entity.HasIndex(re => re.RelationshipId);
            entity.HasIndex(re => re.EvidenceId);
            entity.HasIndex(re => re.CaseId);

            entity.HasOne(re => re.Relationship)
                  .WithMany(r => r.SupportingEvidence)
                  .HasForeignKey(re => re.RelationshipId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(re => re.Evidence)
                  .WithMany()
                  .HasForeignKey(re => re.EvidenceId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 7. ExtractionJob
        modelBuilder.Entity<ExtractionJob>(entity =>
        {
            entity.HasKey(j => j.Id);
            entity.HasIndex(j => j.EvidenceId);
            entity.HasIndex(j => j.Status);
            entity.Property(j => j.Status).HasMaxLength(50).IsRequired();
        });

        // 8. ExtractedEntity (Staged Candidate)
        modelBuilder.Entity<ExtractedEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EvidenceId);
            entity.HasIndex(e => e.ExtractionJobId);
            entity.HasIndex(e => e.EntityType);
            entity.HasIndex(e => e.ReviewStatus);
            entity.Property(e => e.EntityType).HasMaxLength(50).IsRequired();
            entity.Property(e => e.RawValue).HasMaxLength(500).IsRequired();
            entity.Property(e => e.NormalizedValue).HasMaxLength(500).IsRequired();
            entity.Property(e => e.ReviewStatus).HasMaxLength(50).IsRequired();
        });

        // 9. ExtractedRelationship (Staged Candidate)
        modelBuilder.Entity<ExtractedRelationship>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.HasIndex(r => r.EvidenceId);
            entity.HasIndex(r => r.ExtractionJobId);
            entity.HasIndex(r => r.RelationshipType);
            entity.HasIndex(r => r.ReviewStatus);
            entity.Property(r => r.RelationshipType).HasMaxLength(50).IsRequired();
            entity.Property(r => r.ReviewStatus).HasMaxLength(50).IsRequired();
        });

        // 10. ExtractedEvent (Staged Candidate / Temporal Event)
        modelBuilder.Entity<ExtractedEvent>(entity =>
        {
            entity.HasKey(ev => ev.Id);
            entity.HasIndex(ev => ev.CaseId);
            entity.HasIndex(ev => ev.EvidenceId);
            entity.HasIndex(ev => ev.ExtractionJobId);
            entity.HasIndex(ev => ev.EventType);
            entity.HasIndex(ev => ev.StartTimeUtc);
            entity.HasIndex(ev => ev.EndTimeUtc);
            entity.HasIndex(ev => ev.ReviewStatus);
            entity.Property(ev => ev.EventType).HasMaxLength(50).IsRequired();
            entity.Property(ev => ev.ReviewStatus).HasMaxLength(50).IsRequired();
            entity.Property(ev => ev.TimePrecision).HasMaxLength(50).HasDefaultValue("EXACT");

            entity.HasOne(ev => ev.Case)
                  .WithMany()
                  .HasForeignKey(ev => ev.CaseId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 11. EntityMatchCandidate (Cross-Case Staged Candidate)
        modelBuilder.Entity<EntityMatchCandidate>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.HasIndex(c => new { c.SourceEntityId, c.TargetEntityId });
            entity.HasIndex(c => c.SourceCaseId);
            entity.HasIndex(c => c.TargetCaseId);
            entity.HasIndex(c => c.MatchStatus);
            entity.HasIndex(c => c.EntityType);
            entity.HasIndex(c => c.MatchScore);

            entity.Property(c => c.EntityType).HasMaxLength(50).IsRequired();
            entity.Property(c => c.MatchStatus).HasMaxLength(50).IsRequired();
            entity.Property(c => c.MatchMethod).HasMaxLength(50).IsRequired();

            entity.HasOne(c => c.SourceEntity)
                  .WithMany()
                  .HasForeignKey(c => c.SourceEntityId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.TargetEntity)
                  .WithMany()
                  .HasForeignKey(c => c.TargetEntityId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.SourceCase)
                  .WithMany()
                  .HasForeignKey(c => c.SourceCaseId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.TargetCase)
                  .WithMany()
                  .HasForeignKey(c => c.TargetCaseId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 12. CrossCaseConnection (Approved Cross-Case Link)
        modelBuilder.Entity<CrossCaseConnection>(entity =>
        {
            entity.HasKey(c => c.Id);
            entity.HasIndex(c => new { c.SourceCaseId, c.TargetCaseId });
            entity.HasIndex(c => new { c.SourceEntityId, c.TargetEntityId });
            entity.HasIndex(c => c.Status);
            entity.Property(c => c.ConnectionType).HasMaxLength(50).IsRequired();
            entity.Property(c => c.Status).HasMaxLength(50).IsRequired();

            entity.HasOne(c => c.SourceEntity)
                  .WithMany()
                  .HasForeignKey(c => c.SourceEntityId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.TargetEntity)
                  .WithMany()
                  .HasForeignKey(c => c.TargetEntityId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.SourceCase)
                  .WithMany()
                  .HasForeignKey(c => c.SourceCaseId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(c => c.TargetCase)
                  .WithMany()
                  .HasForeignKey(c => c.TargetCaseId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 13. GraphAnalysisRun
        modelBuilder.Entity<GraphAnalysisRun>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.HasIndex(r => r.CaseId);
            entity.HasIndex(r => r.Status);
            entity.HasIndex(r => r.StartedAtUtc);
            entity.Property(r => r.Status).HasMaxLength(50).IsRequired();
            entity.Property(r => r.ModelVersion).HasMaxLength(100).IsRequired();

            entity.HasOne(r => r.Case)
                  .WithMany()
                  .HasForeignKey(r => r.CaseId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(r => r.NodeMetrics)
                  .WithOne(m => m.AnalysisRun)
                  .HasForeignKey(m => m.AnalysisRunId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(r => r.Leads)
                  .WithOne(l => l.AnalysisRun)
                  .HasForeignKey(l => l.AnalysisRunId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 14. GraphNodeMetrics
        modelBuilder.Entity<GraphNodeMetrics>(entity =>
        {
            entity.HasKey(m => m.Id);
            entity.HasIndex(m => m.AnalysisRunId);
            entity.HasIndex(m => m.EntityId);
            entity.HasIndex(m => m.CaseId);
            entity.HasIndex(m => m.CommunityId);
            entity.HasIndex(m => m.ComponentId);
            entity.Property(m => m.AnalyticalIndicator).HasMaxLength(100);

            entity.HasOne(m => m.Entity)
                  .WithMany()
                  .HasForeignKey(m => m.EntityId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 15. GraphAnalyticalLead
        modelBuilder.Entity<GraphAnalyticalLead>(entity =>
        {
            entity.HasKey(l => l.Id);
            entity.HasIndex(l => l.CaseId);
            entity.HasIndex(l => l.AnalysisRunId);
            entity.HasIndex(l => new { l.SourceEntityId, l.TargetEntityId });
            entity.HasIndex(l => l.Status);
            entity.HasIndex(l => l.Score);
            entity.Property(l => l.Status).HasMaxLength(50).IsRequired();
            entity.Property(l => l.LeadType).HasMaxLength(100).IsRequired();
            entity.Property(l => l.ModelVersion).HasMaxLength(100).IsRequired();

            entity.HasOne(l => l.SourceEntity)
                  .WithMany()
                  .HasForeignKey(l => l.SourceEntityId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(l => l.TargetEntity)
                  .WithMany()
                  .HasForeignKey(l => l.TargetEntityId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(l => l.Case)
                  .WithMany()
                  .HasForeignKey(l => l.CaseId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(l => l.ResultingRelationship)
                  .WithMany()
                  .HasForeignKey(l => l.ResultingRelationshipId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // 16. TemporalAnalysisRun
        modelBuilder.Entity<TemporalAnalysisRun>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.HasIndex(r => r.CaseId);
            entity.HasIndex(r => r.Status);
            entity.HasIndex(r => r.StartedAtUtc);
            entity.Property(r => r.Status).HasMaxLength(50).IsRequired();
            entity.Property(r => r.ExecutedBy).HasMaxLength(255);

            entity.HasOne(r => r.Case)
                  .WithMany()
                  .HasForeignKey(r => r.CaseId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 17. TemporalSignal
        modelBuilder.Entity<TemporalSignal>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.HasIndex(s => s.CaseId);
            entity.HasIndex(s => s.AnalysisRunId);
            entity.HasIndex(s => s.SignalType);
            entity.HasIndex(s => s.Status);
            entity.HasIndex(s => s.Score);
            entity.HasIndex(s => new { s.SourceEntityId, s.TargetEntityId });
            entity.HasIndex(s => s.StartTimeUtc);
            entity.Property(s => s.SignalType).HasMaxLength(100).IsRequired();
            entity.Property(s => s.Status).HasMaxLength(50).IsRequired();

            entity.HasOne(s => s.Case)
                  .WithMany()
                  .HasForeignKey(s => s.CaseId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(s => s.AnalysisRun)
                  .WithMany(r => r.Signals)
                  .HasForeignKey(s => s.AnalysisRunId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(s => s.SourceEntity)
                  .WithMany()
                  .HasForeignKey(s => s.SourceEntityId)
                  .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(s => s.TargetEntity)
                  .WithMany()
                  .HasForeignKey(s => s.TargetEntityId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        // 18. LocationItem
        modelBuilder.Entity<LocationItem>(entity =>
        {
            entity.HasKey(l => l.Id);
            entity.HasIndex(l => l.CaseId);
            entity.HasIndex(l => new { l.Latitude, l.Longitude });
            entity.HasIndex(l => l.NormalizedName);
            entity.Property(l => l.Name).HasMaxLength(255).IsRequired();
            entity.Property(l => l.NormalizedName).HasMaxLength(255).IsRequired();
            entity.Property(l => l.City).HasMaxLength(100);
            entity.Property(l => l.District).HasMaxLength(100);
            entity.Property(l => l.State).HasMaxLength(100);
            entity.Property(l => l.Country).HasMaxLength(100);
            entity.Property(l => l.GeocodePrecision).HasMaxLength(50);
            entity.Property(l => l.Source).HasMaxLength(50);

            entity.HasOne(l => l.Case)
                  .WithMany()
                  .HasForeignKey(l => l.CaseId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 19. SpatialSignal
        modelBuilder.Entity<SpatialSignal>(entity =>
        {
            entity.HasKey(s => s.Id);
            entity.HasIndex(s => s.CaseId);
            entity.HasIndex(s => s.Status);
            entity.HasIndex(s => s.SignalType);
            entity.HasIndex(s => s.LocationId);
            entity.HasIndex(s => s.AnalysisRunId);
            entity.Property(s => s.SignalType).HasMaxLength(100).IsRequired();
            entity.Property(s => s.Status).HasMaxLength(50).IsRequired();

            entity.HasOne(s => s.Case)
                  .WithMany()
                  .HasForeignKey(s => s.CaseId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 20. SpatialAnalysisRun
        modelBuilder.Entity<SpatialAnalysisRun>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.HasIndex(r => r.CaseId);
            entity.HasIndex(r => r.Status);
            entity.Property(r => r.Status).HasMaxLength(50).IsRequired();

            entity.HasOne(r => r.Case)
                  .WithMany()
                  .HasForeignKey(r => r.CaseId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 21. Alert
        modelBuilder.Entity<Alert>(entity =>
        {
            entity.HasKey(a => a.Id);
            entity.HasIndex(a => a.CaseId);
            entity.HasIndex(a => a.Status);
            entity.HasIndex(a => a.Severity);
            entity.HasIndex(a => a.AlertType);
            entity.HasIndex(a => a.DeduplicationFingerprint);
            entity.HasIndex(a => a.AlertRunId);
            entity.HasIndex(a => a.CreatedAtUtc);
            entity.Property(a => a.AlertType).HasMaxLength(100).IsRequired();
            entity.Property(a => a.Severity).HasMaxLength(50).IsRequired();
            entity.Property(a => a.Status).HasMaxLength(50).IsRequired();
            entity.Property(a => a.Title).HasMaxLength(255).IsRequired();
            entity.Property(a => a.DeduplicationFingerprint).HasMaxLength(255).IsRequired();

            entity.HasOne(a => a.Case)
                  .WithMany()
                  .HasForeignKey(a => a.CaseId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.AlertRun)
                  .WithMany(r => r.Alerts)
                  .HasForeignKey(a => a.AlertRunId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(a => a.SourceEntity)
                  .WithMany()
                  .HasForeignKey(a => a.SourceEntityId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(a => a.TargetEntity)
                  .WithMany()
                  .HasForeignKey(a => a.TargetEntityId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(a => a.LocationItem)
                  .WithMany()
                  .HasForeignKey(a => a.LocationId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(a => a.RelatedEvent)
                  .WithMany()
                  .HasForeignKey(a => a.RelatedEventId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(a => a.RelatedEvidence)
                  .WithMany()
                  .HasForeignKey(a => a.RelatedEvidenceId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // 22. AlertRun
        modelBuilder.Entity<AlertRun>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.HasIndex(r => r.CaseId);
            entity.HasIndex(r => r.Status);
            entity.Property(r => r.Status).HasMaxLength(50).IsRequired();

            entity.HasOne(r => r.Case)
                  .WithMany()
                  .HasForeignKey(r => r.CaseId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // 23. EvidenceLedgerBlock (Phase 9 - Immutable Append-Only Ledger)
        modelBuilder.Entity<EvidenceLedgerBlock>(entity =>
        {
            entity.HasKey(b => b.Id);
            entity.HasIndex(b => b.BlockIndex).IsUnique();
            entity.HasIndex(b => b.BlockHash).IsUnique();
            entity.HasIndex(b => b.EvidenceItemId);
            entity.HasIndex(b => b.EvidenceHash);
            entity.HasIndex(b => b.TimestampUtc);
            entity.HasIndex(b => b.ActorUserId);

            entity.Property(b => b.Action).HasMaxLength(50).IsRequired();
            entity.Property(b => b.EvidenceHash).HasMaxLength(64).IsRequired();
            entity.Property(b => b.PreviousBlockHash).HasMaxLength(64).IsRequired();
            entity.Property(b => b.BlockHash).HasMaxLength(64).IsRequired();
            entity.Property(b => b.ActorUserId).HasMaxLength(100).IsRequired();
            entity.Property(b => b.ActorName).HasMaxLength(255).IsRequired();

            entity.HasOne(b => b.EvidenceItem)
                  .WithMany()
                  .HasForeignKey(b => b.EvidenceItemId)
                  .IsRequired(false)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}

