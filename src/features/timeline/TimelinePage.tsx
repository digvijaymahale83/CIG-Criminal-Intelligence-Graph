import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Clock,
  Calendar,
  Filter,
  Play,
  CheckCircle2,
  XCircle,
  AlertTriangle,
  FileText,
  MapPin,
  Users,
  Layers,
  ArrowRight,
  TrendingUp,
  RefreshCw,
  ShieldCheck,
  Search,
  SlidersHorizontal,
  ChevronRight,
  X
} from 'lucide-react';
import { useActiveInvestigation } from '../../hooks/useActiveInvestigation';
import { Card } from '../../components/common/Card';
import { StatusBadge } from '../../components/common/StatusBadge';
import { ResponsibleAiNotice } from '../../components/common/ResponsibleAiNotice';
import {
  timelineService,
  TimelineEventDto,
  TimelineResponseDto,
  TemporalOverlapDto,
  TemporalClusterDto,
  TemporalSequenceItemDto,
  TemporalSummaryDto
} from '../../services/timeline/timeline.service';

const EVENT_TYPES = [
  'ALL',
  'CALL',
  'VISIT',
  'TRAVEL',
  'TRANSACTION',
  'COMMUNICATION',
  'VEHICLE_SIGHTING',
  'LOCATION_ACTIVITY',
  'DOCUMENT_EVENT',
  'EVIDENCE_EVENT',
  'CASE_EVENT',
  'OTHER'
];

export const TimelinePage: React.FC = () => {
  const navigate = useNavigate();
  const { activeCaseId, activeCaseNumber, activeCaseTitle } = useActiveInvestigation();
  const effectiveCaseId = activeCaseId || 'inv-2026-001';

  // Active View Tab
  const [activeTab, setActiveTab] = useState<'STREAM' | 'OVERLAPS' | 'CLUSTERS' | 'SEQUENCE'>('STREAM');

  // Data state
  const [timelineData, setTimelineData] = useState<TimelineResponseDto | null>(null);
  const [overlaps, setOverlaps] = useState<TemporalOverlapDto[]>([]);
  const [summary, setSummary] = useState<TemporalSummaryDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [analyzing, setAnalyzing] = useState(false);

  // Filters state
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedEventType, setSelectedEventType] = useState('ALL');
  const [selectedStatus, setSelectedStatus] = useState('ALL');
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');

  // Selected Detail Drawers
  const [selectedEvent, setSelectedEvent] = useState<TimelineEventDto | null>(null);
  const [selectedSignal, setSelectedSignal] = useState<TemporalOverlapDto | null>(null);
  const [reviewNotes, setReviewNotes] = useState('');
  const [reviewing, setReviewing] = useState(false);

  // Load timeline data
  const loadData = useCallback(async () => {
    setLoading(true);
    try {
      const [timeline, overlapList, summaryStats] = await Promise.all([
        timelineService.getCaseTimeline(effectiveCaseId, {
          eventType: selectedEventType !== 'ALL' ? selectedEventType : undefined,
          verificationStatus: selectedStatus !== 'ALL' ? selectedStatus : undefined,
          startDate: startDate ? new Date(startDate).toISOString() : undefined,
          endDate: endDate ? new Date(endDate).toISOString() : undefined,
          page: 1,
          pageSize: 100
        }),
        timelineService.getTemporalOverlaps(effectiveCaseId),
        timelineService.getTemporalSummary(effectiveCaseId)
      ]);

      setTimelineData(timeline);
      setOverlaps(overlapList);
      setSummary(summaryStats);
    } catch (err) {
      console.error('Failed to load timeline intelligence data:', err);
    } finally {
      setLoading(false);
    }
  }, [effectiveCaseId, selectedEventType, selectedStatus, startDate, endDate]);

  useEffect(() => {
    loadData();
  }, [loadData]);

  // Trigger temporal analysis run
  const handleRunAnalysis = async () => {
    setAnalyzing(true);
    try {
      await timelineService.runTemporalAnalysis(effectiveCaseId, {
        includeCrossCase: true,
        authorizedCaseIds: [effectiveCaseId, 'inv-2026-002', 'inv-2026-003'],
        minOverlapDurationMinutes: 1.0
      });
      await loadData();
    } catch (err) {
      console.error('Failed to execute temporal analysis:', err);
    } finally {
      setAnalyzing(false);
    }
  };

  // Handle Signal Review
  const handleReviewSignal = async (status: 'CONFIRMED' | 'DISMISSED') => {
    if (!selectedSignal) return;
    setReviewing(true);
    try {
      await timelineService.reviewTemporalSignal(effectiveCaseId, selectedSignal.id, {
        status,
        reviewNotes
      });
      setSelectedSignal(null);
      setReviewNotes('');
      await loadData();
    } catch (err) {
      console.error('Failed to review temporal signal:', err);
    } finally {
      setReviewing(false);
    }
  };

  // Clear filters
  const handleClearFilters = () => {
    setSearchQuery('');
    setSelectedEventType('ALL');
    setSelectedStatus('ALL');
    setStartDate('');
    setEndDate('');
  };

  // Filter events by text search client-side
  const filteredEvents = (timelineData?.events || []).filter((e) => {
    if (!searchQuery.trim()) return true;
    const query = searchQuery.toLowerCase();
    return (
      e.description.toLowerCase().includes(query) ||
      e.eventType.toLowerCase().includes(query) ||
      (e.location && e.location.toLowerCase().includes(query)) ||
      e.relatedEntityNames.some((name) => name.toLowerCase().includes(query))
    );
  });

  return (
    <div className="space-y-6" id="timeline-page">
      {/* Header */}
      <div className="flex flex-col lg:flex-row lg:items-center justify-between gap-4 pb-4 border-b border-[var(--color-border)]">
        <div>
          <div className="flex items-center gap-2.5">
            <Clock className="w-6 h-6 text-[var(--color-accent)]" />
            <h1 className="text-xl font-bold text-[var(--color-text-primary)] tracking-tight">Timeline & Temporal Intelligence</h1>
            <span className="text-[10px] font-mono px-2 py-0.5 rounded bg-[var(--color-surface-subtle)] text-[var(--color-text-muted)] border border-[var(--color-border)]">
              CASE: {activeCaseNumber}
            </span>
          </div>
          <p className="text-xs text-[var(--color-text-secondary)] mt-1">
            Chronological reconstruction of extracted evidence events, activity windows, and mathematical temporal overlap analysis.
          </p>
        </div>

        <div className="flex items-center gap-3">
          <button
            onClick={loadData}
            disabled={loading}
            className="flex items-center gap-1.5 text-xs font-semibold px-3 py-2 rounded border border-[var(--color-border)] bg-[var(--color-surface)] hover:bg-[var(--color-surface-subtle)] text-[var(--color-text-primary)] transition-colors"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${loading ? 'animate-spin' : ''}`} />
            Refresh
          </button>
          <button
            onClick={handleRunAnalysis}
            disabled={analyzing}
            className="flex items-center gap-2 text-xs font-semibold px-4 py-2 rounded bg-[var(--color-accent)] hover:bg-[var(--color-accent-hover)] text-white shadow-sm transition-colors disabled:opacity-50"
          >
            <Play className={`w-3.5 h-3.5 ${analyzing ? 'animate-spin' : ''}`} />
            {analyzing ? 'Analyzing Temporal Patterns...' : 'Run Temporal Analysis'}
          </button>
        </div>
      </div>

      <ResponsibleAiNotice compact />

      {/* KPI Overview Summary */}
      {summary && (
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <Card className="p-3.5 bg-[var(--color-surface)] border-[var(--color-border)]">
            <div className="flex items-center justify-between">
              <span className="text-[11px] font-medium text-[var(--color-text-muted)]">Chronological Events</span>
              <Calendar className="w-4 h-4 text-[var(--color-accent)]" />
            </div>
            <div className="mt-2 flex items-baseline gap-2">
              <span className="text-2xl font-bold text-[var(--color-text-primary)]">{summary.totalEvents}</span>
              <span className="text-[10px] text-[var(--color-text-secondary)]">recorded</span>
            </div>
          </Card>

          <Card className="p-3.5 bg-[var(--color-surface)] border-[var(--color-border)]">
            <div className="flex items-center justify-between">
              <span className="text-[11px] font-medium text-[var(--color-text-muted)]">Active Period</span>
              <Clock className="w-4 h-4 text-[#2E5A88]" />
            </div>
            <div className="mt-2">
              <span className="text-sm font-semibold text-[var(--color-text-primary)]">
                {summary.activePeriodStartUtc
                  ? new Date(summary.activePeriodStartUtc).toLocaleDateString(undefined, { month: 'short', day: 'numeric' })
                  : 'N/A'}{' '}
                –{' '}
                {summary.activePeriodEndUtc
                  ? new Date(summary.activePeriodEndUtc).toLocaleDateString(undefined, { month: 'short', day: 'numeric', year: 'numeric' })
                  : 'N/A'}
              </span>
              <p className="text-[10px] text-[var(--color-text-secondary)] mt-0.5">span of activity</p>
            </div>
          </Card>

          <Card className="p-3.5 bg-[var(--color-surface)] border-[var(--color-border)]">
            <div className="flex items-center justify-between">
              <span className="text-[11px] font-medium text-[var(--color-text-muted)]">Activity Peaks</span>
              <TrendingUp className="w-4 h-4 text-[#A86F38]" />
            </div>
            <div className="mt-2 flex items-baseline gap-2">
              <span className="text-2xl font-bold text-[var(--color-text-primary)]">{summary.activityPeaks}</span>
              <span className="text-[10px] text-[var(--color-text-secondary)]">clusters</span>
            </div>
          </Card>

          <Card className="p-3.5 bg-[var(--color-surface)] border-[var(--color-border)]">
            <div className="flex items-center justify-between">
              <span className="text-[11px] font-medium text-[var(--color-text-muted)]">Temporal Signals</span>
              <Layers className="w-4 h-4 text-[#8C2D19]" />
            </div>
            <div className="mt-2 flex items-baseline gap-2">
              <span className="text-2xl font-bold text-[#8C2D19]">{summary.temporalSignals}</span>
              <span className="text-[10px] text-[var(--color-text-secondary)]">({summary.pendingSignals} pending)</span>
            </div>
          </Card>
        </div>
      )}

      {/* Filter Toolbar */}
      <Card className="p-4 bg-[var(--color-surface)] border-[var(--color-border)]">
        <div className="flex flex-col md:flex-row gap-3 items-center justify-between">
          <div className="flex-1 flex flex-wrap items-center gap-3 w-full">
            {/* Search */}
            <div className="relative flex-1 min-w-[200px]">
              <Search className="w-3.5 h-3.5 absolute left-2.5 top-1/2 -translate-y-1/2 text-[var(--color-text-muted)]" />
              <input
                type="text"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                placeholder="Search events, locations, or entities..."
                className="w-full text-xs pl-8 pr-3 py-1.5 rounded border border-[var(--color-border)] bg-[var(--color-surface)] text-[var(--color-text-primary)] focus:outline-none focus:border-[var(--color-accent)]"
              />
            </div>

            {/* Event Type Filter */}
            <select
              value={selectedEventType}
              onChange={(e) => setSelectedEventType(e.target.value)}
              className="text-xs px-2.5 py-1.5 rounded border border-[var(--color-border)] bg-[var(--color-surface)] text-[var(--color-text-primary)] focus:outline-none focus:border-[var(--color-accent)]"
            >
              {EVENT_TYPES.map((type) => (
                <option key={type} value={type}>
                  {type === 'ALL' ? 'All Event Types' : type}
                </option>
              ))}
            </select>

            {/* Verification Status */}
            <select
              value={selectedStatus}
              onChange={(e) => setSelectedStatus(e.target.value)}
              className="text-xs px-2.5 py-1.5 rounded border border-[var(--color-border)] bg-[var(--color-surface)] text-[var(--color-text-primary)] focus:outline-none focus:border-[var(--color-accent)]"
            >
              <option value="ALL">All Statuses</option>
              <option value="APPROVED">Verified Only</option>
              <option value="PENDING">Pending Review</option>
            </select>

            {/* Date Range */}
            <div className="flex items-center gap-1.5 text-xs text-[var(--color-text-secondary)]">
              <span>From:</span>
              <input
                type="date"
                value={startDate}
                onChange={(e) => setStartDate(e.target.value)}
                className="px-2 py-1 rounded border border-[var(--color-border)] bg-[var(--color-surface)] text-[var(--color-text-primary)] text-xs"
              />
              <span>To:</span>
              <input
                type="date"
                value={endDate}
                onChange={(e) => setEndDate(e.target.value)}
                className="px-2 py-1 rounded border border-[var(--color-border)] bg-[var(--color-surface)] text-[var(--color-text-primary)] text-xs"
              />
            </div>
          </div>

          <button
            onClick={handleClearFilters}
            className="text-xs text-[var(--color-text-muted)] hover:text-[var(--color-accent)] font-medium whitespace-nowrap transition-colors"
          >
            Clear Filters
          </button>
        </div>
      </Card>

      {/* Tabs Navigation */}
      <div className="flex border-b border-[var(--color-border)] gap-2">
        <button
          onClick={() => setActiveTab('STREAM')}
          className={`px-4 py-2.5 text-xs font-semibold border-b-2 transition-colors ${
            activeTab === 'STREAM'
              ? 'border-[var(--color-accent)] text-[var(--color-accent)]'
              : 'border-transparent text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]'
          }`}
        >
          Chronological Event Stream ({filteredEvents.length})
        </button>

        <button
          onClick={() => setActiveTab('OVERLAPS')}
          className={`px-4 py-2.5 text-xs font-semibold border-b-2 transition-colors flex items-center gap-1.5 ${
            activeTab === 'OVERLAPS'
              ? 'border-[var(--color-accent)] text-[var(--color-accent)]'
              : 'border-transparent text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]'
          }`}
        >
          <span>Temporal Overlaps ({overlaps.length})</span>
          {overlaps.some((o) => o.status === 'PENDING') && (
            <span className="w-2 h-2 rounded-full bg-[var(--color-accent)]" />
          )}
        </button>

        <button
          onClick={() => setActiveTab('CLUSTERS')}
          className={`px-4 py-2.5 text-xs font-semibold border-b-2 transition-colors ${
            activeTab === 'CLUSTERS'
              ? 'border-[var(--color-accent)] text-[var(--color-accent)]'
              : 'border-transparent text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]'
          }`}
        >
          Activity Clusters ({timelineData?.clusters.length || 0})
        </button>

        <button
          onClick={() => setActiveTab('SEQUENCE')}
          className={`px-4 py-2.5 text-xs font-semibold border-b-2 transition-colors ${
            activeTab === 'SEQUENCE'
              ? 'border-[var(--color-accent)] text-[var(--color-accent)]'
              : 'border-transparent text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]'
          }`}
        >
          Sequence Flow ({timelineData?.sequenceHighlights.length || 0})
        </button>
      </div>

      {/* Tab 1: Chronological Stream */}
      {activeTab === 'STREAM' && (
        <Card title="Chronological Event Stream" subtitle="Time-indexed observations from primary source evidence">
          {loading ? (
            <div className="py-12 text-center text-xs text-[var(--color-text-muted)]">Loading chronological events...</div>
          ) : filteredEvents.length === 0 ? (
            <div className="py-12 text-center text-xs text-[var(--color-text-muted)]">
              No events match the selected criteria. Run temporal analysis or adjust filters.
            </div>
          ) : (
            <div className="relative pl-6 space-y-6 before:absolute before:left-2 before:top-2 before:bottom-2 before:w-0.5 before:bg-[var(--color-border)]">
              {filteredEvents.map((evt) => (
                <div
                  key={evt.id}
                  onClick={() => setSelectedEvent(evt)}
                  className="relative p-3.5 rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] hover:border-[var(--color-accent)] hover:shadow-sm cursor-pointer transition-all space-y-2 text-xs"
                >
                  {/* Timeline dot */}
                  <div
                    className={`absolute -left-[27px] top-4 w-3.5 h-3.5 rounded-full border-2 bg-[var(--color-surface)] ${
                      evt.verificationStatus === 'APPROVED'
                        ? 'border-[#2E7D32]'
                        : evt.timePrecision === 'EXACT'
                        ? 'border-[var(--color-accent)]'
                        : 'border-[var(--color-border)]'
                    }`}
                  />

                  {/* Header Row */}
                  <div className="flex flex-wrap items-center justify-between gap-2">
                    <div className="flex items-center gap-2">
                      <span className="font-mono text-xs font-bold text-[var(--color-accent)]">
                        {evt.startTimeUtc
                          ? new Date(evt.startTimeUtc).toLocaleString(undefined, {
                              year: 'numeric',
                              month: 'short',
                              day: 'numeric',
                              hour: evt.timePrecision === 'DATE_ONLY' ? undefined : '2-digit',
                              minute: evt.timePrecision === 'DATE_ONLY' ? undefined : '2-digit'
                            })
                          : 'Date Unknown'}
                      </span>
                      <span className="text-[10px] font-mono px-1.5 py-0.5 rounded bg-[var(--color-surface-subtle)] text-[var(--color-text-primary)] border border-[var(--color-border)]">
                        {evt.eventType}
                      </span>
                      <span className="text-[10px] font-mono px-1.5 py-0.5 rounded bg-[var(--color-surface-subtle)] text-[var(--color-text-muted)]">
                        PRECISION: {evt.timePrecision}
                      </span>
                    </div>

                    <div className="flex items-center gap-2">
                      {evt.evidenceIntegrityVerified && (
                        <span className="inline-flex items-center gap-1 text-[10px] font-medium text-[#2E7D32]">
                          <ShieldCheck className="w-3 h-3" />
                          Evidence Verified
                        </span>
                      )}
                      <StatusBadge status={evt.verificationStatus} />
                    </div>
                  </div>

                  {/* Description */}
                  <p className="text-xs text-[var(--color-text-primary)] font-medium leading-relaxed">{evt.description}</p>

                  {/* Location & Entities */}
                  <div className="flex flex-wrap items-center gap-4 pt-1 text-[11px] text-[var(--color-text-secondary)]">
                    {evt.location && (
                      <button
                        onClick={() => navigate(`/map?location=${encodeURIComponent(evt.location!)}&caseId=${encodeURIComponent(effectiveCaseId)}`)}
                        className="flex items-center gap-1 text-[var(--color-accent)] hover:underline cursor-pointer"
                        title="View Location on Geospatial Map"
                      >
                        <MapPin className="w-3 h-3 text-[var(--color-accent)]" />
                        <span>{evt.location}</span>
                      </button>
                    )}
                    {evt.relatedEntityNames.length > 0 && (
                      <div className="flex items-center gap-1">
                        <Users className="w-3 h-3 text-[#2E5A88]" />
                        <span>{evt.relatedEntityNames.join(', ')}</span>
                      </div>
                    )}
                    {evt.sourceEvidenceFileName && (
                      <div className="flex items-center gap-1 font-mono text-[10px]">
                        <FileText className="w-3 h-3 text-[var(--color-text-muted)]" />
                        <span>{evt.sourceEvidenceFileName}</span>
                      </div>
                    )}
                  </div>
                </div>
              ))}
            </div>
          )}
        </Card>
      )}

      {/* Tab 2: Temporal Overlaps */}
      {activeTab === 'OVERLAPS' && (
        <Card
          title="Temporal Overlap & Coincident Activity Signals"
          subtitle="Mathematical detection of overlapping activity windows across entities and locations"
        >
          {overlaps.length === 0 ? (
            <div className="py-12 text-center text-xs text-[var(--color-text-muted)]">
              No temporal overlaps detected for this case. Click &quot;Run Temporal Analysis&quot; to calculate concurrent activity.
            </div>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {overlaps.map((sig) => (
                <div
                  key={sig.id}
                  className="p-4 rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] hover:border-[var(--color-accent)] space-y-3 text-xs transition-all shadow-sm"
                >
                  <div className="flex items-center justify-between">
                    <span className="text-[10px] font-mono px-2 py-0.5 rounded bg-[var(--color-surface-subtle)] text-[var(--color-accent)] font-semibold border border-[#FFCCBC]">
                      {sig.signalType === 'CROSS_CASE_TEMPORAL_OVERLAP' ? 'CROSS-CASE OVERLAP' : 'COINCIDENT WINDOW'}
                    </span>
                    <StatusBadge status={sig.status} />
                  </div>

                  <div>
                    <h4 className="font-semibold text-[var(--color-text-primary)] text-sm">
                      {sig.sourceEntityName || 'Entity 1'} ↔ {sig.targetEntityName || 'Entity 2'}
                    </h4>
                    <p className="text-xs text-[var(--color-text-primary)] mt-1 leading-relaxed">{sig.explanation}</p>
                  </div>

                  <div className="grid grid-cols-2 gap-2 p-2 rounded bg-[var(--color-surface)] border border-[var(--color-border-subtle)] text-[11px]">
                    <div>
                      <span className="text-[var(--color-text-muted)]">Duration:</span>
                      <p className="font-semibold text-[var(--color-accent)]">{sig.durationMinutes} minutes</p>
                    </div>
                    <div>
                      <span className="text-[var(--color-text-muted)]">Location:</span>
                      <p className="font-medium text-[var(--color-text-primary)] truncate">{sig.locationName || 'Specified Site'}</p>
                    </div>
                  </div>

                  <div className="flex items-center justify-between pt-2 border-t border-[#F0EAE1]">
                    <div className="flex items-center gap-1 text-[11px]">
                      <span className="text-[var(--color-text-muted)]">Score:</span>
                      <span className="font-bold text-[var(--color-accent)] font-mono">{sig.score.toFixed(2)}</span>
                    </div>

                    <div className="flex items-center gap-2">
                      <button
                        onClick={() => setSelectedSignal(sig)}
                        className="text-xs font-semibold px-2.5 py-1 rounded border border-[var(--color-border)] bg-[var(--color-surface)] hover:bg-[var(--color-surface-subtle)] text-[var(--color-text-primary)]"
                      >
                        Review Lead
                      </button>
                      <button
                        onClick={() => navigate(`/cases/${effectiveCaseId}/graph`)}
                        className="text-xs font-semibold px-2.5 py-1 rounded bg-[var(--color-surface-subtle)] hover:bg-[var(--color-surface-subtle)] text-[var(--color-accent)]"
                      >
                        Highlight on Graph
                      </button>
                      <button
                        onClick={() => navigate(`/map?caseId=${encodeURIComponent(effectiveCaseId)}`)}
                        className="text-xs font-semibold px-2.5 py-1 rounded bg-[#E8F5E9] hover:bg-[#C8E6C9] text-[#2E7D32]"
                      >
                        View on Map
                      </button>
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </Card>
      )}

      {/* Tab 3: Activity Clusters */}
      {activeTab === 'CLUSTERS' && (
        <Card title="Activity Concentration Clusters" subtitle="High-density temporal windows identified by time proximity">
          {!timelineData?.clusters || timelineData.clusters.length === 0 ? (
            <div className="py-12 text-center text-xs text-[var(--color-text-muted)]">
              No activity clusters detected. Activity events are either sparse or unrecorded.
            </div>
          ) : (
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {timelineData.clusters.map((cluster) => (
                <div key={cluster.clusterId} className="p-4 rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] space-y-3 text-xs">
                  <div className="flex items-center justify-between">
                    <span className="font-semibold text-[var(--color-text-primary)] text-sm">{cluster.clusterLabel}</span>
                    <span
                      className={`text-[10px] font-mono font-bold px-2 py-0.5 rounded ${
                        cluster.intensity === 'HIGH'
                          ? 'bg-[#FFEBEE] text-[#C62828] border border-[#FFCDD2]'
                          : cluster.intensity === 'MEDIUM'
                          ? 'bg-[#FFF3E0] text-[#E65100] border border-[#FFE0B2]'
                          : 'bg-[#E8F5E9] text-[#2E7D32] border border-[#C8E6C9]'
                      }`}
                    >
                      {cluster.intensity} DENSITY
                    </span>
                  </div>

                  <div className="grid grid-cols-3 gap-2 p-2.5 rounded bg-[var(--color-surface)] text-center border border-[var(--color-border-subtle)]">
                    <div>
                      <span className="text-[10px] text-[var(--color-text-muted)]">Events</span>
                      <p className="text-base font-bold text-[var(--color-accent)]">{cluster.eventCount}</p>
                    </div>
                    <div>
                      <span className="text-[10px] text-[var(--color-text-muted)]">Entities</span>
                      <p className="text-base font-bold text-[#2E5A88]">{cluster.distinctEntitiesCount}</p>
                    </div>
                    <div>
                      <span className="text-[10px] text-[var(--color-text-muted)]">Locations</span>
                      <p className="text-base font-bold text-[var(--color-text-primary)]">{cluster.distinctLocationsCount}</p>
                    </div>
                  </div>

                  <div>
                    <span className="text-[10px] text-[var(--color-text-muted)] uppercase font-semibold">Key Involved Entities:</span>
                    <div className="flex flex-wrap gap-1 mt-1">
                      {cluster.keyEntities.map((ent, idx) => (
                        <span key={idx} className="text-[10px] px-1.5 py-0.5 rounded bg-[var(--color-surface-subtle)] text-[var(--color-text-primary)] border border-[var(--color-border)]">
                          {ent}
                        </span>
                      ))}
                    </div>
                  </div>
                </div>
              ))}
            </div>
          )}
        </Card>
      )}

      {/* Tab 4: Sequence Flow */}
      {activeTab === 'SEQUENCE' && (
        <Card title="Chronological Sequence Analysis" subtitle="Observed progression of sequential investigative milestones">
          {!timelineData?.sequenceHighlights || timelineData.sequenceHighlights.length === 0 ? (
            <div className="py-12 text-center text-xs text-[var(--color-text-muted)]">No sequential progression steps recorded.</div>
          ) : (
            <div className="space-y-3">
              {timelineData.sequenceHighlights.map((step) => (
                <div key={step.stepIndex} className="flex items-center gap-4 p-3 rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] text-[var(--color-text-primary)] text-xs">
                  <div className="w-8 h-8 rounded-full bg-[var(--color-surface-subtle)] border border-[var(--color-border)] text-[var(--color-accent)] font-bold flex items-center justify-center font-mono">
                    {step.stepIndex}
                  </div>

                  <div className="flex-1 space-y-1">
                    <div className="flex items-center gap-2">
                      <span className="font-semibold text-[var(--color-text-primary)]">{step.eventType}</span>
                      <span className="text-[10px] font-mono text-[var(--color-text-muted)]">
                        {new Date(step.timestampUtc).toLocaleString()}
                      </span>
                    </div>
                    <p className="text-[var(--color-text-primary)]">{step.description}</p>
                  </div>

                  <div className="text-right">
                    <span className="text-[10px] font-mono font-semibold px-2 py-1 rounded bg-[var(--color-surface)] text-[var(--color-accent)] border border-[var(--color-border)]">
                      + {step.elapsedFromPrevious}
                    </span>
                  </div>
                </div>
              ))}
            </div>
          )}
        </Card>
      )}

      {/* Event Detail Modal Drawer */}
      {selectedEvent && (
        <div className="fixed inset-0 bg-black/40 backdrop-blur-sm z-50 flex justify-end">
          <div className="w-full max-w-lg bg-[var(--color-surface)] h-full shadow-2xl p-6 overflow-y-auto space-y-6 border-l border-[var(--color-border)]">
            <div className="flex items-center justify-between pb-4 border-b border-[var(--color-border)]">
              <div className="flex items-center gap-2">
                <Calendar className="w-5 h-5 text-[var(--color-accent)]" />
                <h3 className="font-bold text-sm text-[var(--color-text-primary)]">Event Provenance & Details</h3>
              </div>
              <button onClick={() => setSelectedEvent(null)} className="p-1 rounded hover:bg-[var(--color-surface-subtle)] text-[var(--color-text-muted)]">
                <X className="w-4 h-4" />
              </button>
            </div>

            <div className="space-y-4 text-xs">
              <div className="p-3 rounded bg-[var(--color-surface)] border border-[var(--color-border)] space-y-2">
                <span className="text-[10px] text-[var(--color-text-muted)] uppercase font-bold">Event Type & Description</span>
                <div className="flex items-center gap-2">
                  <span className="text-xs font-mono font-bold text-[var(--color-accent)] px-2 py-0.5 bg-[var(--color-surface-subtle)] rounded border border-[var(--color-border)]">
                    {selectedEvent.eventType}
                  </span>
                  <StatusBadge status={selectedEvent.verificationStatus} />
                </div>
                <p className="text-xs text-[var(--color-text-primary)] font-medium leading-relaxed">{selectedEvent.description}</p>
              </div>

              <div className="p-3 rounded bg-[var(--color-surface)] border border-[var(--color-border)] space-y-2">
                <span className="text-[10px] text-[var(--color-text-muted)] uppercase font-bold">Temporal Precision & Bounds</span>
                <div className="grid grid-cols-2 gap-2">
                  <div>
                    <span className="text-[var(--color-text-muted)]">Start UTC:</span>
                    <p className="font-mono text-[var(--color-text-primary)]">
                      {selectedEvent.startTimeUtc ? new Date(selectedEvent.startTimeUtc).toISOString() : 'N/A'}
                    </p>
                  </div>
                  <div>
                    <span className="text-[var(--color-text-muted)]">Precision:</span>
                    <p className="font-semibold text-[var(--color-accent)]">{selectedEvent.timePrecision}</p>
                  </div>
                </div>
              </div>

              <div className="p-3 rounded bg-[var(--color-surface)] border border-[var(--color-border)] space-y-2">
                <span className="text-[10px] text-[var(--color-text-muted)] uppercase font-bold">Location & Entities</span>
                <div>
                  <span className="text-[var(--color-text-muted)]">Location:</span>
                  <p className="font-medium text-[var(--color-text-primary)]">{selectedEvent.location || 'Unspecified'}</p>
                </div>
                <div>
                  <span className="text-[var(--color-text-muted)]">Related Entities:</span>
                  <div className="flex flex-wrap gap-1 mt-1">
                    {selectedEvent.relatedEntityNames.map((n, i) => (
                      <span key={i} className="px-2 py-0.5 rounded bg-[var(--color-surface-subtle)] text-[var(--color-text-primary)] border border-[var(--color-border)] text-[10px]">
                        {n}
                      </span>
                    ))}
                  </div>
                </div>
              </div>

              <div className="p-3 rounded bg-[var(--color-surface)] border border-[var(--color-border)] space-y-2">
                <span className="text-[10px] text-[var(--color-text-muted)] uppercase font-bold">Evidentiary Provenance</span>
                <div className="space-y-1">
                  <div className="flex justify-between">
                    <span className="text-[var(--color-text-muted)]">Source File:</span>
                    <span className="font-mono text-[var(--color-text-primary)]">{selectedEvent.sourceEvidenceFileName || 'Document'}</span>
                  </div>
                  <div className="flex justify-between">
                    <span className="text-[var(--color-text-muted)]">Source Page / Line:</span>
                    <span className="font-mono text-[var(--color-text-primary)]">{selectedEvent.sourceLocation || 'N/A'}</span>
                  </div>
                  {selectedEvent.sourceEvidenceSha256 && (
                    <div className="pt-1">
                      <span className="text-[var(--color-text-muted)]">SHA-256 Hash:</span>
                      <p className="font-mono text-[9px] text-[var(--color-text-primary)] truncate">{selectedEvent.sourceEvidenceSha256}</p>
                    </div>
                  )}
                </div>
              </div>
            </div>

            <div className="flex items-center gap-3 pt-4 border-t border-[var(--color-border)]">
              <button
                onClick={() => {
                  setSelectedEvent(null);
                  navigate(`/cases/${effectiveCaseId}/graph`);
                }}
                className="flex-1 text-xs font-semibold py-2 px-3 rounded bg-[var(--color-accent)] text-white hover:bg-[var(--color-accent-hover)] text-center"
              >
                Highlight on Graph
              </button>
              <button
                onClick={() => {
                  setSelectedEvent(null);
                  navigate('/evidence');
                }}
                className="flex-1 text-xs font-semibold py-2 px-3 rounded border border-[var(--color-border)] bg-[var(--color-surface)] text-[var(--color-text-primary)] hover:bg-[var(--color-surface-subtle)] text-center"
              >
                View Evidence
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Signal Review Modal Drawer */}
      {selectedSignal && (
        <div className="fixed inset-0 bg-black/40 backdrop-blur-sm z-50 flex justify-end">
          <div className="w-full max-w-md bg-[var(--color-surface)] h-full shadow-2xl p-6 overflow-y-auto space-y-6 border-l border-[var(--color-border)]">
            <div className="flex items-center justify-between pb-4 border-b border-[var(--color-border)]">
              <div className="flex items-center gap-2">
                <AlertTriangle className="w-5 h-5 text-[var(--color-accent)]" />
                <h3 className="font-bold text-sm text-[var(--color-text-primary)]">Review Temporal Signal</h3>
              </div>
              <button onClick={() => setSelectedSignal(null)} className="p-1 rounded hover:bg-[var(--color-surface-subtle)] text-[var(--color-text-muted)]">
                <X className="w-4 h-4" />
              </button>
            </div>

            <div className="space-y-3 text-xs">
              <div className="p-3 rounded bg-[var(--color-surface)] border border-[var(--color-border)] space-y-1">
                <span className="text-[10px] text-[var(--color-text-muted)] uppercase font-bold">Signal Type</span>
                <p className="font-semibold text-sm text-[var(--color-accent)]">{selectedSignal.signalType}</p>
                <p className="text-[var(--color-text-primary)] mt-1">{selectedSignal.explanation}</p>
              </div>

              <div className="p-3 rounded bg-[var(--color-surface)] border border-[var(--color-border)] space-y-1">
                <span className="text-[10px] text-[var(--color-text-muted)] uppercase font-bold">Overlap Window</span>
                <p className="font-mono text-[var(--color-text-primary)]">
                  {new Date(selectedSignal.overlapStartUtc).toLocaleString()} –{' '}
                  {new Date(selectedSignal.overlapEndUtc).toLocaleString()}
                </p>
                <span className="text-xs font-semibold text-[var(--color-accent)]">{selectedSignal.durationMinutes} minutes overlap</span>
              </div>

              <div>
                <label className="text-[11px] font-medium text-[var(--color-text-primary)] block mb-1">
                  Investigator Rationale & Review Notes:
                </label>
                <textarea
                  value={reviewNotes}
                  onChange={(e) => setReviewNotes(e.target.value)}
                  placeholder="Record justification for confirming or dismissing this temporal lead..."
                  rows={3}
                  className="w-full text-xs p-2.5 rounded border border-[var(--color-border)] bg-[var(--color-surface)] text-[var(--color-text-primary)] focus:outline-none focus:border-[var(--color-accent)]"
                />
              </div>
            </div>

            <div className="flex items-center gap-3 pt-4 border-t border-[var(--color-border)]">
              <button
                onClick={() => handleReviewSignal('CONFIRMED')}
                disabled={reviewing}
                className="flex-1 flex items-center justify-center gap-1.5 text-xs font-semibold py-2 px-3 rounded bg-[#2E7D32] text-white hover:bg-[#256628] disabled:opacity-50"
              >
                <CheckCircle2 className="w-4 h-4" />
                Confirm Signal
              </button>
              <button
                onClick={() => handleReviewSignal('DISMISSED')}
                disabled={reviewing}
                className="flex-1 flex items-center justify-center gap-1.5 text-xs font-semibold py-2 px-3 rounded bg-[#C62828] text-white hover:bg-[#A32020] disabled:opacity-50"
              >
                <XCircle className="w-4 h-4" />
                Dismiss
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
