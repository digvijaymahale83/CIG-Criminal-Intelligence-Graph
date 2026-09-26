import React, { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  AlertTriangle,
  ShieldAlert,
  Search,
  Filter,
  RefreshCw,
  Play,
  CheckCircle2,
  XCircle,
  Eye,
  Clock,
  MapPin,
  Network,
  Calendar,
  FileText,
  Sliders,
  ExternalLink,
  ChevronDown,
  ChevronUp,
  SlidersHorizontal,
  Info,
  Check,
  X
} from 'lucide-react';
import { useActiveInvestigation } from '../../hooks/useActiveInvestigation';
import { Card } from '../../components/common/Card';
import { StatusBadge } from '../../components/common/StatusBadge';
import { ResponsibleAiNotice } from '../../components/common/ResponsibleAiNotice';
import {
  alertService,
  AlertDto,
  AlertSummaryDto,
  AlertRunResultDto,
  AlertType,
  AlertSeverity,
  AlertStatus
} from '../../services/alerts/alert.service';

const ALERT_TYPES: { type: AlertType | 'ALL'; label: string }[] = [
  { type: 'ALL', label: 'All Signal Categories' },
  { type: 'NETWORK_ANOMALY', label: 'Network Nexus Anomaly' },
  { type: 'TEMPORAL_ANOMALY', label: 'Temporal Activity Burst' },
  { type: 'GEOGRAPHIC_ANOMALY', label: 'Geographic Outlier' },
  { type: 'RELATIONSHIP_SURGE', label: 'Interaction Surge' },
  { type: 'ACTIVITY_SPIKE', label: 'Case Activity Spike' },
  { type: 'UNUSUAL_TRAVEL', label: 'Implausible Velocity' },
  { type: 'DATA_CONSISTENCY', label: 'Data Consistency Issue' },
  { type: 'CROSS_CASE_PATTERN', label: 'Cross-Case Pattern' },
  { type: 'MODEL_SIGNAL', label: 'GAT Model Lead' },
];

export const AlertsPage: React.FC = () => {
  const navigate = useNavigate();
  const { activeCaseId, activeCaseNumber, activeCaseTitle } = useActiveInvestigation();
  const effectiveCaseId = activeCaseId || 'inv-2026-001';

  // State
  const [alerts, setAlerts] = useState<AlertDto[]>([]);
  const [summary, setSummary] = useState<AlertSummaryDto | null>(null);
  const [loading, setLoading] = useState(true);
  const [runningDetection, setRunningDetection] = useState(false);
  const [detectionResult, setDetectionResult] = useState<AlertRunResultDto | null>(null);
  const [error, setError] = useState<string | null>(null);

  // Filters
  const [statusFilter, setStatusFilter] = useState<string>('ALL');
  const [severityFilter, setSeverityFilter] = useState<string>('ALL');
  const [typeFilter, setTypeFilter] = useState<string>('ALL');
  const [searchQuery, setSearchQuery] = useState('');

  // Expandable cards
  const [expandedAlertIds, setExpandedAlertIds] = useState<Set<string>>(new Set());

  // Run Modal
  const [showRunModal, setShowRunModal] = useState(false);
  const [includeCrossCase, setIncludeCrossCase] = useState(true);
  const [distanceThresholdKm, setDistanceThresholdKm] = useState(100);
  const [surgeThreshold, setSurgeThreshold] = useState(4);

  // Review Modal
  const [reviewModalAlert, setReviewModalAlert] = useState<{ alert: AlertDto; mode: 'RESOLVE' | 'DISMISS' } | null>(null);
  const [reviewNotes, setReviewNotes] = useState('');
  const [reviewingAction, setReviewingAction] = useState(false);

  const fetchAlertsAndSummary = useCallback(async () => {
    try {
      setLoading(true);
      setError(null);
      const [alertsData, summaryData] = await Promise.all([
        alertService.getCaseAlerts(effectiveCaseId, {
          status: statusFilter !== 'ALL' ? statusFilter : undefined,
          severity: severityFilter !== 'ALL' ? severityFilter : undefined,
          alertType: typeFilter !== 'ALL' ? typeFilter : undefined,
          search: searchQuery.trim() || undefined,
        }),
        alertService.getAlertSummary(effectiveCaseId)
      ]);
      setAlerts(alertsData);
      setSummary(summaryData);
    } catch (err: any) {
      console.error('Failed to fetch alerts:', err);
      setError('Unable to load alerts from intelligence backend. Please verify service connectivity.');
    } finally {
      setLoading(false);
    }
  }, [effectiveCaseId, statusFilter, severityFilter, typeFilter, searchQuery]);

  useEffect(() => {
    fetchAlertsAndSummary();
  }, [fetchAlertsAndSummary]);

  const toggleExpand = (id: string) => {
    setExpandedAlertIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id);
      else next.add(id);
      return next;
    });
  };

  const handleRunDetection = async () => {
    try {
      setRunningDetection(true);
      setError(null);
      const result = await alertService.runDetection(effectiveCaseId, {
        includeCrossCase,
        geographicDistanceThresholdKm: distanceThresholdKm,
        relationshipSurgeThreshold: surgeThreshold
      });
      setDetectionResult(result);
      setShowRunModal(false);
      await fetchAlertsAndSummary();
    } catch (err: any) {
      console.error('Failed to run detection:', err);
      setError('Anomaly detection execution failed. Check backend logs.');
    } finally {
      setRunningDetection(false);
    }
  };

  const handleAcknowledge = async (alertId: string) => {
    try {
      await alertService.acknowledgeAlert(alertId);
      await fetchAlertsAndSummary();
    } catch (err) {
      console.error('Failed to acknowledge alert:', err);
    }
  };

  const handleStartReview = async (alertId: string) => {
    try {
      await alertService.startReview(alertId);
      await fetchAlertsAndSummary();
    } catch (err) {
      console.error('Failed to start review:', err);
    }
  };

  const handleConfirmReview = async () => {
    if (!reviewModalAlert) return;
    try {
      setReviewingAction(true);
      if (reviewModalAlert.mode === 'RESOLVE') {
        await alertService.resolveAlert(reviewModalAlert.alert.id, reviewNotes);
      } else {
        await alertService.dismissAlert(reviewModalAlert.alert.id, reviewNotes);
      }
      setReviewModalAlert(null);
      setReviewNotes('');
      await fetchAlertsAndSummary();
    } catch (err) {
      console.error('Failed to complete alert review action:', err);
    } finally {
      setReviewingAction(false);
    }
  };

  const getSeverityBadgeClass = (severity: AlertSeverity) => {
    switch (severity) {
      case 'CRITICAL':
        return 'bg-[#7A1E1E] text-white border-[#5B1616]';
      case 'HIGH':
        return 'bg-[var(--color-accent)] text-white border-[#873F1F]';
      case 'MEDIUM':
        return 'bg-[#EBD9B4] text-[#6A4B10] border-[#DFC593]';
      case 'LOW':
        return 'bg-[#EDE4D8] text-[var(--color-text-secondary)] border-[#D9CEBF]';
    }
  };

  const getStatusBadgeVariant = (status: AlertStatus) => {
    switch (status) {
      case 'NEW':
        return 'info';
      case 'ACKNOWLEDGED':
        return 'warning';
      case 'UNDER_REVIEW':
        return 'warning';
      case 'RESOLVED':
        return 'success';
      case 'DISMISSED':
        return 'neutral';
    }
  };

  return (
    <div className="space-y-6" id="alerts-page">
      {/* Top Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-4 border-b border-[var(--color-border)]">
        <div>
          <div className="flex items-center gap-2.5">
            <h1 className="text-xl font-bold text-[var(--color-text-primary)] tracking-tight">Investigative Alerts & Anomaly Detection</h1>
            <span className="text-[10px] font-mono px-2 py-0.5 rounded bg-[var(--color-surface-subtle)] text-[var(--color-text-muted)] border border-[var(--color-border)]">
              CASE: {activeCaseNumber || effectiveCaseId}
            </span>
          </div>
          <p className="text-xs text-[var(--color-text-secondary)] mt-1">
            Algorithmic detection of structural network disparities, temporal bursts, velocity anomalies, and evidentiary inconsistencies.
          </p>
        </div>

        <div className="flex items-center gap-2">
          <button
            onClick={() => fetchAlertsAndSummary()}
            className="p-2 rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] hover:bg-[var(--color-surface-subtle)] text-[var(--color-text-secondary)] transition-colors"
            title="Refresh Alerts"
          >
            <RefreshCw className={`w-4 h-4 ${loading ? 'animate-spin' : ''}`} />
          </button>
          <button
            onClick={() => setShowRunModal(true)}
            className="flex items-center gap-2 px-3.5 py-2 rounded-lg bg-[#8B263E] hover:bg-[#721F33] text-white text-xs font-medium shadow-2xs transition-colors"
          >
            <Play className="w-3.5 h-3.5 fill-current" />
            <span>Run Anomaly Detection</span>
          </button>
        </div>
      </div>

      {/* Responsible AI & Neutral Terminology Banner */}
      <ResponsibleAiNotice />

      <div className="p-3.5 rounded-lg bg-[#F7F2EA] border border-[#DFCBB5] text-xs text-[#5C4D40] flex items-start gap-2.5">
        <Info className="w-4 h-4 text-[#8B263E] mt-0.5 shrink-0" />
        <div className="space-y-0.5">
          <span className="font-semibold text-[var(--color-text-primary)]">Evidentiary & Operational Guarantee:</span>
          <p>
            Anomalous patterns represent investigative signals requiring human evaluation, NOT proof of criminal activity.
            Reviewing, resolving, or dismissing an alert never automatically alters or creates edges in the primary investigation knowledge graph.
          </p>
        </div>
      </div>

      {/* Detection Result Banner */}
      {detectionResult && (
        <div className="p-3.5 rounded-lg bg-[#EBF5EE] border border-[#BDE0C8] text-xs text-[#1E4D2B] flex items-center justify-between">
          <div className="flex items-center gap-2">
            <CheckCircle2 className="w-4 h-4 text-[#2D6A4F]" />
            <span>
              <strong>Anomaly Run Completed:</strong> {detectionResult.detectorsExecuted} detectors executed in {detectionResult.executionDurationMs}ms.
              Generated {detectionResult.signalsGenerated} signals, created {detectionResult.alertsCreated} new alerts ({detectionResult.alertsDeduplicated} duplicate signals suppressed).
            </span>
          </div>
          <button
            onClick={() => setDetectionResult(null)}
            className="p-1 text-[#2D6A4F] hover:bg-[#D8EFE0] rounded"
          >
            <X className="w-3.5 h-3.5" />
          </button>
        </div>
      )}

      {/* Summary KPI Cards */}
      {summary && (
        <div className="grid grid-cols-2 sm:grid-cols-5 gap-3">
          <div className="p-4 rounded-xl bg-[var(--color-surface)] border border-[var(--color-border)] shadow-2xs">
            <span className="text-[11px] font-medium text-[var(--color-text-muted)] uppercase tracking-wider">Total Alerts</span>
            <div className="text-2xl font-bold text-[var(--color-text-primary)] mt-1">{summary.totalAlerts}</div>
            <span className="text-[10px] text-[var(--color-text-muted)]">Across 9 detection engines</span>
          </div>

          <div className="p-4 rounded-xl bg-[var(--color-surface)] border border-[var(--color-border)] shadow-2xs">
            <span className="text-[11px] font-medium text-[#8B263E] uppercase tracking-wider">Action Required</span>
            <div className="text-2xl font-bold text-[#8B263E] mt-1">{summary.newAlerts}</div>
            <span className="text-[10px] text-[var(--color-text-muted)]">NEW status awaiting review</span>
          </div>

          <div className="p-4 rounded-xl bg-[var(--color-surface)] border border-[var(--color-border)] shadow-2xs">
            <span className="text-[11px] font-medium text-[var(--color-accent)] uppercase tracking-wider">High / Critical</span>
            <div className="text-2xl font-bold text-[var(--color-accent)] mt-1">{summary.criticalSeverity + summary.highSeverity}</div>
            <span className="text-[10px] text-[var(--color-text-muted)]">{summary.criticalSeverity} Critical · {summary.highSeverity} High</span>
          </div>

          <div className="p-4 rounded-xl bg-[var(--color-surface)] border border-[var(--color-border)] shadow-2xs">
            <span className="text-[11px] font-medium text-[#7A522A] uppercase tracking-wider">Under Review</span>
            <div className="text-2xl font-bold text-[#7A522A] mt-1">{summary.underReviewAlerts}</div>
            <span className="text-[10px] text-[var(--color-text-muted)]">Investigator review ongoing</span>
          </div>

          <div className="p-4 rounded-xl bg-[var(--color-surface)] border border-[var(--color-border)] shadow-2xs">
            <span className="text-[11px] font-medium text-[#2D6A4F] uppercase tracking-wider">Resolved</span>
            <div className="text-2xl font-bold text-[#2D6A4F] mt-1">{summary.resolvedAlerts}</div>
            <span className="text-[10px] text-[var(--color-text-muted)]">{summary.dismissedAlerts} dismissed</span>
          </div>
        </div>
      )}

      {/* Filters Bar */}
      <div className="p-4 rounded-xl bg-[var(--color-surface)] border border-[var(--color-border)] shadow-2xs space-y-3">
        <div className="flex flex-col md:flex-row gap-3 items-center justify-between">
          <div className="relative w-full md:w-80">
            <Search className="w-4 h-4 absolute left-3 top-2.5 text-[var(--color-text-muted)]" />
            <input
              type="text"
              placeholder="Search alerts by title, entity, location..."
              value={searchQuery}
              onChange={(e) => setSearchQuery(e.target.value)}
              className="w-full pl-9 pr-3 py-1.5 text-xs rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] text-[var(--color-text-primary)] focus:outline-hidden focus:border-[#8B263E]"
            />
          </div>

          <div className="flex flex-wrap items-center gap-2 w-full md:w-auto">
            {/* Severity filter */}
            <select
              value={severityFilter}
              onChange={(e) => setSeverityFilter(e.target.value)}
              className="px-2.5 py-1.5 text-xs rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] text-[var(--color-text-primary)]"
            >
              <option value="ALL">All Severities</option>
              <option value="CRITICAL">Critical</option>
              <option value="HIGH">High</option>
              <option value="MEDIUM">Medium</option>
              <option value="LOW">Low</option>
            </select>

            {/* Type filter */}
            <select
              value={typeFilter}
              onChange={(e) => setTypeFilter(e.target.value)}
              className="px-2.5 py-1.5 text-xs rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] text-[var(--color-text-primary)]"
            >
              {ALERT_TYPES.map((t) => (
                <option key={t.type} value={t.type}>
                  {t.label}
                </option>
              ))}
            </select>
          </div>
        </div>

        {/* Status Pills */}
        <div className="flex items-center gap-1.5 overflow-x-auto pt-1 border-t border-[#F0E6D8]">
          <span className="text-[11px] font-medium text-[var(--color-text-muted)] mr-1">Status:</span>
          {['ALL', 'NEW', 'ACKNOWLEDGED', 'UNDER_REVIEW', 'RESOLVED', 'DISMISSED'].map((status) => (
            <button
              key={status}
              onClick={() => setStatusFilter(status)}
              className={`px-2.5 py-1 rounded-md text-[11px] font-medium transition-colors ${
                statusFilter === status
                  ? 'bg-[var(--color-text-primary)] text-[var(--color-bg-canvas)]'
                  : 'bg-[var(--color-surface-subtle)] text-[var(--color-text-secondary)] hover:bg-[var(--color-surface-subtle)]'
              }`}
            >
              {status.replace('_', ' ')}
            </button>
          ))}
        </div>
      </div>

      {/* Alerts List */}
      {loading ? (
        <div className="p-12 text-center text-xs text-[var(--color-text-muted)] space-y-2">
          <RefreshCw className="w-5 h-5 animate-spin mx-auto text-[#8B263E]" />
          <p>Evaluating investigative anomaly signals...</p>
        </div>
      ) : alerts.length === 0 ? (
        <div className="p-12 text-center rounded-xl bg-[var(--color-surface)] border border-[var(--color-border)] space-y-3">
          <ShieldAlert className="w-10 h-10 text-[#BFA893] mx-auto" />
          <div className="text-sm font-semibold text-[var(--color-text-primary)]">No Anomalies Surfaced</div>
          <p className="text-xs text-[var(--color-text-secondary)] max-w-md mx-auto">
            No anomalous patterns match the current filter criteria for this investigation workspace. Run anomaly detection to evaluate latest evidence.
          </p>
          <button
            onClick={() => setShowRunModal(true)}
            className="px-4 py-2 rounded-lg bg-[#8B263E] text-white text-xs font-medium hover:bg-[#721F33] transition-colors inline-flex items-center gap-2"
          >
            <Play className="w-3.5 h-3.5 fill-current" />
            <span>Execute Detection Engines</span>
          </button>
        </div>
      ) : (
        <div className="space-y-3">
          {alerts.map((alert) => {
            const isExpanded = expandedAlertIds.has(alert.id);
            return (
              <div
                key={alert.id}
                className="rounded-xl bg-[var(--color-surface)] border border-[var(--color-border)] shadow-2xs hover:border-[#D0BDA8] transition-all overflow-hidden"
              >
                {/* Main Card Summary Bar */}
                <div className="p-4 sm:p-5 space-y-3">
                  <div className="flex flex-col sm:flex-row sm:items-start justify-between gap-3">
                    <div className="space-y-1.5 flex-1">
                      <div className="flex flex-wrap items-center gap-2">
                        <span className={`text-[10px] font-bold px-2 py-0.5 rounded border ${getSeverityBadgeClass(alert.severity)}`}>
                          {alert.severity}
                        </span>
                        <StatusBadge
                          status={alert.status.replace('_', ' ')}
                          variant={getStatusBadgeVariant(alert.status)}
                        />
                        <span className="text-[10px] font-mono px-2 py-0.5 rounded bg-[var(--color-surface-subtle)] text-[var(--color-text-secondary)]">
                          {alert.alertType}
                        </span>
                        <span className="text-[10px] text-[var(--color-text-muted)]">
                          Detected {new Date(alert.createdAtUtc).toLocaleDateString()} at {new Date(alert.createdAtUtc).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                        </span>
                      </div>

                      <h3 className="text-sm font-bold text-[var(--color-text-primary)]">{alert.title}</h3>
                      <p className="text-xs text-[var(--color-text-secondary)] leading-relaxed">{alert.description}</p>
                    </div>

                    {/* Score badge & quick actions */}
                    <div className="flex sm:flex-col items-end gap-2 shrink-0">
                      <div className="text-right">
                        <div className="text-[10px] font-medium text-[var(--color-text-muted)]">PRIORITY SCORE</div>
                        <div className="text-lg font-bold text-[#8B263E]">
                          {(alert.priorityScore * 100).toFixed(0)}%
                        </div>
                        <div className="text-[9px] text-[var(--color-text-muted)]">Raw: {(alert.score * 100).toFixed(0)}%</div>
                      </div>

                      <button
                        onClick={() => toggleExpand(alert.id)}
                        className="flex items-center gap-1 text-xs text-[#8B263E] hover:text-[#721F33] font-medium mt-1"
                      >
                        <span>{isExpanded ? 'Hide Details' : 'View Breakdown'}</span>
                        {isExpanded ? <ChevronUp className="w-3.5 h-3.5" /> : <ChevronDown className="w-3.5 h-3.5" />}
                      </button>
                    </div>
                  </div>

                  {/* Context chips */}
                  <div className="flex flex-wrap items-center gap-2 pt-2 border-t border-[#F5EDE3] text-xs">
                    {alert.sourceEntityName && (
                      <div className="inline-flex items-center gap-1 px-2 py-1 rounded bg-[#FAF5EE] border border-[#EADBCC] text-[var(--color-text-secondary)]">
                        <Network className="w-3 h-3 text-[#8B263E]" />
                        <span className="font-medium">{alert.sourceEntityName}</span>
                        {alert.targetEntityName && <span>↔ {alert.targetEntityName}</span>}
                      </div>
                    )}

                    {alert.locationName && (
                      <div className="inline-flex items-center gap-1 px-2 py-1 rounded bg-[#FAF5EE] border border-[#EADBCC] text-[var(--color-text-secondary)]">
                        <MapPin className="w-3 h-3 text-[var(--color-accent)]" />
                        <span>{alert.locationName}</span>
                      </div>
                    )}

                    {alert.relatedEvidenceFileName && (
                      <div className="inline-flex items-center gap-1 px-2 py-1 rounded bg-[#FAF5EE] border border-[#EADBCC] text-[var(--color-text-secondary)]">
                        <FileText className="w-3 h-3 text-[#2D6A4F]" />
                        <span>{alert.relatedEvidenceFileName}</span>
                      </div>
                    )}
                  </div>

                  {/* Action row */}
                  <div className="flex flex-wrap items-center justify-between gap-2 pt-2 border-t border-[#F5EDE3]">
                    {/* Deep-link investigative buttons */}
                    <div className="flex flex-wrap items-center gap-1.5">
                      {(alert.sourceEntityId || alert.targetEntityId) && (
                        <button
                          onClick={() => navigate(`/network?focusEntityId=${alert.sourceEntityId || alert.targetEntityId}`)}
                          className="inline-flex items-center gap-1 px-2.5 py-1 rounded border border-[var(--color-border)] bg-[var(--color-surface)] hover:bg-[var(--color-surface-subtle)] text-[11px] font-medium text-[var(--color-text-primary)] transition-colors"
                        >
                          <Network className="w-3 h-3 text-[#8B263E]" />
                          <span>Graph Focus</span>
                        </button>
                      )}

                      {alert.relatedEventId && (
                        <button
                          onClick={() => navigate(`/timeline?eventId=${alert.relatedEventId}`)}
                          className="inline-flex items-center gap-1 px-2.5 py-1 rounded border border-[var(--color-border)] bg-[var(--color-surface)] hover:bg-[var(--color-surface-subtle)] text-[11px] font-medium text-[var(--color-text-primary)] transition-colors"
                        >
                          <Calendar className="w-3 h-3 text-[#7A522A]" />
                          <span>Timeline</span>
                        </button>
                      )}

                      {alert.locationId && (
                        <button
                          onClick={() => navigate(`/map?locationId=${alert.locationId}`)}
                          className="inline-flex items-center gap-1 px-2.5 py-1 rounded border border-[var(--color-border)] bg-[var(--color-surface)] hover:bg-[var(--color-surface-subtle)] text-[11px] font-medium text-[var(--color-text-primary)] transition-colors"
                        >
                          <MapPin className="w-3 h-3 text-[var(--color-accent)]" />
                          <span>Map</span>
                        </button>
                      )}

                      {alert.relatedEvidenceId && (
                        <button
                          onClick={() => navigate(`/evidence/${alert.relatedEvidenceId}`)}
                          className="inline-flex items-center gap-1 px-2.5 py-1 rounded border border-[var(--color-border)] bg-[var(--color-surface)] hover:bg-[var(--color-surface-subtle)] text-[11px] font-medium text-[var(--color-text-primary)] transition-colors"
                        >
                          <FileText className="w-3 h-3 text-[#2D6A4F]" />
                          <span>Evidence</span>
                        </button>
                      )}
                    </div>

                    {/* Review lifecycle buttons */}
                    <div className="flex items-center gap-1.5">
                      {alert.status === 'NEW' && (
                        <button
                          onClick={() => handleAcknowledge(alert.id)}
                          className="px-2.5 py-1 rounded border border-[var(--color-border)] bg-[var(--color-surface)] hover:bg-[var(--color-surface-subtle)] text-[11px] font-medium text-[var(--color-text-primary)]"
                        >
                          Acknowledge
                        </button>
                      )}

                      {(alert.status === 'NEW' || alert.status === 'ACKNOWLEDGED') && (
                        <button
                          onClick={() => handleStartReview(alert.id)}
                          className="px-2.5 py-1 rounded bg-[var(--color-surface-subtle)] hover:bg-[#DFCDBB] text-[11px] font-medium text-[var(--color-text-primary)]"
                        >
                          Start Review
                        </button>
                      )}

                      {alert.status !== 'RESOLVED' && (
                        <button
                          onClick={() => {
                            setReviewModalAlert({ alert, mode: 'RESOLVE' });
                            setReviewNotes('');
                          }}
                          className="px-2.5 py-1 rounded bg-[#2D6A4F] hover:bg-[#24543E] text-white text-[11px] font-medium flex items-center gap-1"
                        >
                          <Check className="w-3 h-3" />
                          <span>Resolve</span>
                        </button>
                      )}

                      {alert.status !== 'DISMISSED' && (
                        <button
                          onClick={() => {
                            setReviewModalAlert({ alert, mode: 'DISMISS' });
                            setReviewNotes('');
                          }}
                          className="px-2.5 py-1 rounded border border-[var(--color-border)] text-[var(--color-text-muted)] hover:bg-[var(--color-surface-subtle)] text-[11px] font-medium"
                        >
                          Dismiss
                        </button>
                      )}
                    </div>
                  </div>
                </div>

                {/* Expanded Detailed Breakdown */}
                {isExpanded && (
                  <div className="p-4 sm:p-5 bg-[var(--color-surface)] border-t border-[var(--color-border)] space-y-3 text-xs">
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                      {/* Structured Explanation */}
                      <div className="space-y-2">
                        <div className="text-[11px] font-bold text-[var(--color-text-primary)] uppercase tracking-wider">
                          Structured Lead Explanation
                        </div>
                        <pre className="p-3 rounded-lg bg-[var(--color-surface)] border border-[var(--color-border)] text-[#3D332B] font-mono text-[11px] whitespace-pre-wrap leading-relaxed">
                          {alert.explanation}
                        </pre>
                      </div>

                      {/* Technical & Evidentiary Metadata */}
                      <div className="space-y-2.5">
                        <div className="text-[11px] font-bold text-[var(--color-text-primary)] uppercase tracking-wider">
                          Detection Provenance & Evidentiary Grounding
                        </div>
                        <div className="p-3 rounded-lg bg-[var(--color-surface)] border border-[var(--color-border)] space-y-2 text-[#4A3F35]">
                          <div className="flex justify-between">
                            <span className="text-[var(--color-text-muted)]">Algorithm:</span>
                            <span className="font-mono text-[var(--color-text-primary)]">{alert.detectionMethod} ({alert.detectionVersion})</span>
                          </div>
                          <div className="flex justify-between">
                            <span className="text-[var(--color-text-muted)]">Priority Scoring:</span>
                            <span>
                              0.40(Score) + 0.25(Ev) + 0.20(Cross) + 0.15 = <strong>{(alert.priorityScore * 100).toFixed(1)}%</strong>
                            </span>
                          </div>
                          {alert.relatedEvidenceSha256 && (
                            <div className="flex justify-between items-center">
                              <span className="text-[var(--color-text-muted)]">SHA-256 Hash:</span>
                              <span className="font-mono text-[10px] text-[var(--color-text-primary)] truncate max-w-[180px]" title={alert.relatedEvidenceSha256}>
                                {alert.relatedEvidenceSha256.substring(0, 16)}...
                              </span>
                            </div>
                          )}
                          <div className="flex justify-between items-center">
                            <span className="text-[var(--color-text-muted)]">Fingerprint:</span>
                            <span className="font-mono text-[10px] text-[var(--color-text-primary)] truncate max-w-[180px]" title={alert.deduplicationFingerprint}>
                              {alert.deduplicationFingerprint.substring(0, 20)}...
                            </span>
                          </div>

                          {/* Audit Notes if reviewed */}
                          {alert.reviewedBy && (
                            <div className="pt-2 border-t border-[#F0E6D8] space-y-1">
                              <div className="flex justify-between text-[var(--color-text-muted)]">
                                <span>Reviewed By:</span>
                                <span className="font-medium text-[var(--color-text-primary)]">{alert.reviewedBy}</span>
                              </div>
                              {alert.reviewedAtUtc && (
                                <div className="flex justify-between text-[var(--color-text-muted)]">
                                  <span>Reviewed At:</span>
                                  <span>{new Date(alert.reviewedAtUtc).toLocaleString()}</span>
                                </div>
                              )}
                              {alert.reviewNotes && (
                                <div className="text-[11px] text-[var(--color-text-secondary)] italic mt-1 bg-[#F9F6F0] p-1.5 rounded">
                                  "{alert.reviewNotes}"
                                </div>
                              )}
                            </div>
                          )}
                        </div>
                      </div>
                    </div>
                  </div>
                )}
              </div>
            );
          })}
        </div>
      )}

      {/* Run Detection Configuration Modal */}
      {showRunModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
          <div className="bg-[var(--color-surface)] border border-[var(--color-border)] rounded-xl shadow-xl max-w-md w-full p-5 space-y-4">
            <div className="flex items-center justify-between pb-2 border-b border-[var(--color-border)]">
              <div className="flex items-center gap-2">
                <SlidersHorizontal className="w-4 h-4 text-[#8B263E]" />
                <h3 className="font-bold text-[var(--color-text-primary)] text-sm">Execute Anomaly Detection</h3>
              </div>
              <button onClick={() => setShowRunModal(false)} className="text-[var(--color-text-muted)] hover:text-[var(--color-text-primary)]">
                <X className="w-4 h-4" />
              </button>
            </div>

            <p className="text-xs text-[var(--color-text-secondary)]">
              Executes all 9 analytical anomaly engines across the active case workspace. Signals are deduplicated using SHA-256 fingerprints to preserve existing alert identities.
            </p>

            <div className="space-y-3 text-xs">
              <label className="flex items-center gap-2 cursor-pointer">
                <input
                  type="checkbox"
                  checked={includeCrossCase}
                  onChange={(e) => setIncludeCrossCase(e.target.checked)}
                  className="rounded border-[var(--color-border)] text-[#8B263E]"
                />
                <span className="font-medium text-[var(--color-text-primary)]">Include Cross-Case Nexus Detection</span>
              </label>

              <div>
                <label className="block text-[11px] font-medium text-[var(--color-text-secondary)] mb-1">
                  Geographic Disparity Threshold: {distanceThresholdKm} km
                </label>
                <input
                  type="range"
                  min={30}
                  max={300}
                  step={10}
                  value={distanceThresholdKm}
                  onChange={(e) => setDistanceThresholdKm(Number(e.target.value))}
                  className="w-full"
                />
              </div>

              <div>
                <label className="block text-[11px] font-medium text-[var(--color-text-secondary)] mb-1">
                  Relationship Surge Frequency: {surgeThreshold} joint events in 48h
                </label>
                <input
                  type="range"
                  min={2}
                  max={10}
                  step={1}
                  value={surgeThreshold}
                  onChange={(e) => setSurgeThreshold(Number(e.target.value))}
                  className="w-full"
                />
              </div>
            </div>

            <div className="flex items-center justify-end gap-2 pt-3 border-t border-[var(--color-border)]">
              <button
                onClick={() => setShowRunModal(false)}
                className="px-3 py-1.5 rounded-lg border border-[var(--color-border)] text-xs font-medium text-[var(--color-text-secondary)] hover:bg-[var(--color-surface)]"
              >
                Cancel
              </button>
              <button
                onClick={handleRunDetection}
                disabled={runningDetection}
                className="px-4 py-1.5 rounded-lg bg-[#8B263E] hover:bg-[#721F33] text-white text-xs font-medium flex items-center gap-1.5 disabled:opacity-50"
              >
                {runningDetection ? <RefreshCw className="w-3.5 h-3.5 animate-spin" /> : <Play className="w-3.5 h-3.5 fill-current" />}
                <span>{runningDetection ? 'Evaluating...' : 'Run Detection'}</span>
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Review Confirmation Modal */}
      {reviewModalAlert && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
          <div className="bg-[var(--color-surface)] border border-[var(--color-border)] rounded-xl shadow-xl max-w-md w-full p-5 space-y-4">
            <div className="flex items-center justify-between pb-2 border-b border-[var(--color-border)]">
              <h3 className="font-bold text-[var(--color-text-primary)] text-sm">
                {reviewModalAlert.mode === 'RESOLVE' ? 'Resolve Investigative Alert' : 'Dismiss Alert'}
              </h3>
              <button onClick={() => setReviewModalAlert(null)} className="text-[var(--color-text-muted)] hover:text-[var(--color-text-primary)]">
                <X className="w-4 h-4" />
              </button>
            </div>

            <div className="space-y-1">
              <div className="text-xs font-semibold text-[var(--color-text-primary)]">{reviewModalAlert.alert.title}</div>
              <p className="text-[11px] text-[var(--color-text-secondary)]">{reviewModalAlert.alert.description}</p>
            </div>

            <div className="p-2.5 rounded-md bg-[var(--color-surface)] border border-[var(--color-border)] text-[11px] text-[var(--color-text-secondary)]">
              <strong>Investigative Safeguard:</strong> This action logs an audit trail record. It does NOT modify or create edges in the primary investigation knowledge graph.
            </div>

            <div className="space-y-1">
              <label className="block text-xs font-medium text-[var(--color-text-primary)]">Investigator Review Notes</label>
              <textarea
                rows={3}
                placeholder="Enter justification or context for this decision..."
                value={reviewNotes}
                onChange={(e) => setReviewNotes(e.target.value)}
                className="w-full p-2.5 text-xs rounded-lg border border-[var(--color-border)] bg-[var(--color-surface)] text-[var(--color-text-primary)] focus:outline-hidden focus:border-[#8B263E]"
              />
            </div>

            <div className="flex items-center justify-end gap-2 pt-2 border-t border-[var(--color-border)]">
              <button
                onClick={() => setReviewModalAlert(null)}
                className="px-3 py-1.5 rounded-lg border border-[var(--color-border)] text-xs font-medium text-[var(--color-text-secondary)] hover:bg-[var(--color-surface)]"
              >
                Cancel
              </button>
              <button
                onClick={handleConfirmReview}
                disabled={reviewingAction}
                className={`px-4 py-1.5 rounded-lg text-white text-xs font-medium flex items-center gap-1.5 disabled:opacity-50 ${
                  reviewModalAlert.mode === 'RESOLVE' ? 'bg-[#2D6A4F] hover:bg-[#24543E]' : 'bg-[var(--color-surface-subtle)] hover:bg-[var(--color-surface-secondary)] text-[var(--color-text-primary)] border border-[var(--color-border)]'
                }`}
              >
                {reviewingAction ? <RefreshCw className="w-3.5 h-3.5 animate-spin" /> : <Check className="w-3.5 h-3.5" />}
                <span>{reviewModalAlert.mode === 'RESOLVE' ? 'Confirm Resolution' : 'Confirm Dismissal'}</span>
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
