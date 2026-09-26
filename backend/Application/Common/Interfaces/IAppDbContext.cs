using Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Case> Cases { get; }
    DbSet<Evidence> EvidenceItems { get; }
    DbSet<AuditLog> AuditLogs { get; }
    DbSet<EntityItem> Entities { get; }
    DbSet<Relationship> Relationships { get; }
    DbSet<ExtractionJob> ExtractionJobs { get; }
    DbSet<ExtractedEntity> ExtractedEntities { get; }
    DbSet<ExtractedRelationship> ExtractedRelationships { get; }
    DbSet<ExtractedEvent> ExtractedEvents { get; }
    DbSet<RelationshipEvidence> RelationshipEvidences { get; }
    DbSet<EntityMatchCandidate> EntityMatchCandidates { get; }
    DbSet<CrossCaseConnection> CrossCaseConnections { get; }
    DbSet<GraphAnalysisRun> GraphAnalysisRuns { get; }
    DbSet<GraphNodeMetrics> GraphNodeMetrics { get; }
    DbSet<GraphAnalyticalLead> GraphAnalyticalLeads { get; }
    DbSet<TemporalAnalysisRun> TemporalAnalysisRuns { get; }
    DbSet<TemporalSignal> TemporalSignals { get; }
    DbSet<LocationItem> Locations { get; }
    DbSet<SpatialSignal> SpatialSignals { get; }
    DbSet<SpatialAnalysisRun> SpatialAnalysisRuns { get; }
    DbSet<Alert> Alerts { get; }
    DbSet<AlertRun> AlertRuns { get; }
    DbSet<EvidenceLedgerBlock> EvidenceLedgerBlocks { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<bool> CanConnectAsync(CancellationToken cancellationToken = default);
}
