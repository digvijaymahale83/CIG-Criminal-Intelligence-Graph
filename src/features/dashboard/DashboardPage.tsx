import React, { useState, useEffect, useCallback } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import {
  TrendingUp,
  Users,
  AlertTriangle,
  Network,
  Database,
  GitBranch,
  CheckCircle,
  FileText,
  ArrowRight,
  Clock,
  Activity,
  Radio,
  RefreshCw,
  FolderGit2,
  MapPin,
  ShieldCheck,
  Sparkles,
  ChevronRight,
  ShieldAlert,
  GitMerge,
  ExternalLink,
  Layers,
  CheckCircle2,
  AlertCircle,
  Cpu,
} from 'lucide-react';
import { useAuth } from '../../hooks/useAuth';
import { useActiveInvestigation } from '../../hooks/useActiveInvestigation';
import { useTranslation } from '../../i18n';
import { dashboardService } from '../../services/dashboard/dashboard.service';
import { DashboardDto } from '../../types/dashboard';
import { apiClient } from '../../services/api/client';
import { ResponsibleAiNotice } from '../../components/common/ResponsibleAiNotice';

interface CaseOption {
  id: string;
  caseNumber: string;
  title: string;
  status: string;
  priority: string;
  district?: string;
  leadOfficerName?: string;
  firNumber?: string;
}

export const DashboardPage: React.FC = () => {
  const { caseId: routeCaseId } = useParams<{ caseId?: string }>();
  const navigate = useNavigate();
  const { user } = useAuth();
  const { t, language } = useTranslation();
  const {
    activeCaseId,
    activeCaseNumber,
    activeCaseTitle,
    setActiveInvestigation,
  } = useActiveInvestigation();

  const currentCaseId = routeCaseId || activeCaseId || 'inv-2026-0841';

  const [dashboard, setDashboard] = useState<DashboardDto | null>(null);
  const [availableCases, setAvailableCases] = useState<CaseOption[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [lastRefresh, setLastRefresh] = useState(new Date());

  // Load available cases for case selector
  const loadCases = useCallback(async () => {
    try {
      const casesData = await apiClient.get<any[]>('/cases');
      const normalized: CaseOption[] = (casesData || []).map((c: any) => ({
        id: c.id || c.caseId,
        caseNumber: c.caseNumber || c.case_number || 'UNKNOWN',
        title: c.title || 'Untitled Case',
        status: c.status || 'OPEN',
        priority: c.priority || 'HIGH',
        district: c.district,
        leadOfficerName: c.leadOfficerName || c.lead_officer_name,
        firNumber: c.firNumber || c.fir_number,
      }));
      setAvailableCases(normalized);
    } catch {
      // Fallback if list fails
    }
  }, []);

  // Load dashboard for current case
  const loadDashboard = useCallback(async (targetCaseId: string) => {
    setLoading(true);
    setError(null);
    try {
      const data = await dashboardService.getDashboard(targetCaseId);
      setDashboard(data);
      setLastRefresh(new Date());

      // Sync active investigation context if needed
      if (data.case) {
        setActiveInvestigation(
          data.case.id,
          data.case.caseNumber,
          data.case.title
        );
      }
    } catch (err: any) {
      console.error('Failed to load dashboard:', err);
      setError(err?.message || 'Failed to load case intelligence dashboard');
    } finally {
      setLoading(false);
    }
  }, [setActiveInvestigation]);

  useEffect(() => {
    loadCases();
  }, [loadCases]);

  useEffect(() => {
    if (currentCaseId) {
      loadDashboard(currentCaseId);
    }
  }, [currentCaseId, loadDashboard]);

  const handleCaseChange = (newCaseId: string) => {
    const selected = availableCases.find((c) => c.id === newCaseId);
    if (selected) {
      setActiveInvestigation(selected.id, selected.caseNumber, selected.title);
    }
    navigate(`/cases/${newCaseId}/dashboard`);
  };

  const getSeverityBadgeClass = (severity: string) => {
    switch (severity?.toUpperCase()) {
      case 'CRITICAL':
        return 'bg-red-500/15 text-red-500 border-red-500/30';
      case 'HIGH':
        return 'bg-amber-500/15 text-amber-500 border-amber-500/30';
      case 'MEDIUM':
        return 'bg-emerald-500/15 text-emerald-500 border-emerald-500/30';
      default:
        return 'bg-blue-500/15 text-blue-500 border-blue-500/30';
    }
  };

  const getLedgerHealthClass = (status: string) => {
    switch (status?.toUpperCase()) {
      case 'VALID':
        return 'bg-emerald-500/15 text-emerald-500 border-emerald-500/30';
      case 'WARNING':
        return 'bg-amber-500/15 text-amber-500 border-amber-500/30';
      case 'COMPROMISED':
        return 'bg-red-500/15 text-red-500 border-red-500/30';
      default:
        return 'bg-zinc-500/15 text-zinc-400 border-zinc-500/30';
    }
  };

  return (
    <div className="flex flex-col gap-5" id="investigator-command-center">
      {/* Top Header & Case Selector Bar */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 bg-[var(--color-surface)] border border-[var(--color-border)] rounded-xl p-4 shadow-sm">
        <div className="space-y-1.5 flex-1 min-w-0">
          <div className="flex flex-wrap items-center gap-2">
            <span className="text-[10px] font-mono uppercase px-2 py-0.5 rounded bg-[var(--color-accent)]/15 text-[var(--color-accent)] border border-[var(--color-accent)]/30 font-semibold">
              {t('dashboard.activeInvestigation')}
            </span>
            <span className="text-xs font-mono font-bold text-[var(--color-text-primary)]">
              {dashboard?.case.caseNumber || activeCaseNumber || currentCaseId}
            </span>
            {dashboard?.case.status && (
              <span className="text-[10px] font-mono px-2 py-0.5 rounded bg-[var(--color-surface-subtle)] text-[var(--color-text-secondary)] border border-[var(--color-border-subtle)] uppercase">
                {dashboard.case.status}
              </span>
            )}
            {dashboard?.case.district && (
              <span className="text-[10px] font-mono px-2 py-0.5 rounded bg-[var(--color-surface-subtle)] text-[var(--color-text-muted)] border border-[var(--color-border-subtle)]">
                {dashboard.case.district}
              </span>
            )}
            {dashboard?.case.firNumber && (
              <span className="text-[10px] font-mono text-[var(--color-text-muted)]">
                FIR: {dashboard.case.firNumber}
              </span>
            )}
          </div>
          <h1 className="text-xl sm:text-2xl font-bold text-[var(--color-text-primary)] tracking-tight truncate">
            {dashboard?.case.title || activeCaseTitle || t('dashboard.title')}
          </h1>
          <p className="text-xs text-[var(--color-text-secondary)] flex items-center gap-2">
            <span>{t('dashboard.subtitle')}</span>
            <span>•</span>
            <span>{t('dashboard.lastUpdated')}: {lastRefresh.toLocaleTimeString(language === 'mr' ? 'mr-IN' : 'en-IN')}</span>
            {dashboard?.case.leadOfficerName && (
              <>
                <span>•</span>
                <span>Lead: {dashboard.case.leadOfficerName}</span>
              </>
            )}
          </p>
        </div>

        {/* Action Controls & Case Switcher */}
        <div className="flex flex-wrap items-center gap-2 shrink-0">
          {availableCases.length > 0 && (
            <select
              value={currentCaseId}
              onChange={(e) => handleCaseChange(e.target.value)}
              className="px-3 py-1.5 text-xs rounded-lg bg-[var(--color-surface-subtle)] border border-[var(--color-border-subtle)] text-[var(--color-text-primary)] focus:outline-none focus:border-[var(--color-accent)] font-medium cursor-pointer"
              aria-label="Switch Active Case"
              id="case-switcher-dropdown"
            >
              {availableCases.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.caseNumber} - {c.title.length > 32 ? `${c.title.slice(0, 32)}...` : c.title}
                </option>
              ))}
            </select>
          )}

          <button
            onClick={() => loadDashboard(currentCaseId)}
            disabled={loading}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-[var(--color-surface-subtle)] border border-[var(--color-border-subtle)] text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] hover:border-[var(--color-accent)]/40 transition-colors text-xs font-medium cursor-pointer disabled:opacity-50"
            title="Refresh dashboard data"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${loading ? 'animate-spin' : ''}`} />
            <span className="hidden sm:inline">Refresh</span>
          </button>

          <Link
            to="/copilot"
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-[var(--color-accent)]/15 border border-[var(--color-accent)]/30 text-[var(--color-accent)] hover:bg-[var(--color-accent)]/25 transition-colors text-xs font-semibold"
          >
            <Sparkles className="w-3.5 h-3.5" />
            <span>{t('dashboard.copilot.openCopilot')}</span>
          </Link>
        </div>
      </div>

      {/* Responsible AI Invariant Notice */}
      <ResponsibleAiNotice
        title="INVESTIGATOR DECISION SUPPORT"
        message={
          language === 'mr'
            ? 'विश्लेषणात्मक आणि मॉडेल-व्युत्पन्न संकेत हे तपासणीस संदर्भ आहेत आणि मानवी पडताळणी आवश्यक आहे. ते कोणत्याही गुन्ह्याची किंवा दोषाची खात्री देत नाहीत.'
            : dashboard?.responsibleAiNotice ||
              'Analytical and model-generated signals are investigative leads and require human verification. They do not establish guilt or wrongdoing.'
        }
      />

      {/* Data Quality Warnings Banner if any */}
      {dashboard?.dataQualityWarnings && dashboard.dataQualityWarnings.length > 0 && (
        <div
          id="data-quality-warnings-banner"
          className="rounded-xl bg-amber-500/10 border border-amber-500/30 p-3.5 text-xs text-amber-500 space-y-2"
        >
          <div className="flex items-center gap-2 font-bold uppercase tracking-wider text-[11px]">
            <AlertTriangle className="w-4 h-4 text-amber-500 shrink-0" />
            <span>{t('dashboard.dataQuality.title')} ({dashboard.dataQualityWarnings.length})</span>
          </div>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
            {dashboard.dataQualityWarnings.map((warning, idx) => (
              <div
                key={idx}
                className="flex items-start justify-between gap-2 p-2 rounded-lg bg-[var(--color-surface)]/80 border border-amber-500/20 text-[11px]"
              >
                <div className="space-y-0.5">
                  <span className="font-mono font-semibold uppercase text-[10px] text-amber-600 dark:text-amber-400 block">
                    {warning.type.replace('_', ' ')}
                  </span>
                  <p className="text-[var(--color-text-secondary)]">{warning.message}</p>
                </div>
                {warning.actionUrl && (
                  <Link
                    to={warning.actionUrl}
                    className="shrink-0 text-amber-500 hover:underline flex items-center gap-1 font-medium mt-0.5"
                  >
                    <span>Inspect</span>
                    <ChevronRight className="w-3 h-3" />
                  </Link>
                )}
              </div>
            ))}
          </div>
        </div>
      )}

      {/* 6 Top-line KPI Cards */}
      <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-3">
        {/* Entities */}
        <Link
          to="/entities"
          className="p-3.5 rounded-xl bg-[var(--color-surface)] border border-[var(--color-border)] hover:border-[var(--color-accent)]/50 transition-all group flex flex-col justify-between shadow-2xs cursor-pointer"
        >
          <div className="flex items-center justify-between text-[var(--color-text-muted)] mb-1">
            <span className="text-[10px] font-bold font-mono tracking-wider">
              {t('dashboard.kpi.entities')}
            </span>
            <Users className="w-4 h-4 text-purple-400 group-hover:scale-110 transition-transform" />
          </div>
          <div className="text-2xl font-extrabold text-[var(--color-text-primary)]">
            {dashboard?.summary.entityCount ?? '—'}
          </div>
          <div className="text-[10px] text-[var(--color-text-muted)] flex items-center gap-1 mt-1">
            <span>Explore directory</span>
            <ArrowRight className="w-2.5 h-2.5 group-hover:translate-x-0.5 transition-transform" />
          </div>
        </Link>

        {/* Relationships */}
        <Link
          to="/network"
          className="p-3.5 rounded-xl bg-[var(--color-surface)] border border-[var(--color-border)] hover:border-[var(--color-accent)]/50 transition-all group flex flex-col justify-between shadow-2xs cursor-pointer"
        >
          <div className="flex items-center justify-between text-[var(--color-text-muted)] mb-1">
            <span className="text-[10px] font-bold font-mono tracking-wider">
              {t('dashboard.kpi.relationships')}
            </span>
            <GitBranch className="w-4 h-4 text-blue-400 group-hover:scale-110 transition-transform" />
          </div>
          <div className="text-2xl font-extrabold text-[var(--color-text-primary)]">
            {dashboard?.summary.relationshipCount ?? '—'}
          </div>
          <div className="text-[10px] text-[var(--color-text-muted)] flex items-center gap-1 mt-1">
            <span>Graph topology</span>
            <ArrowRight className="w-2.5 h-2.5 group-hover:translate-x-0.5 transition-transform" />
          </div>
        </Link>

        {/* Evidence */}
        <Link
          to="/evidence"
          className="p-3.5 rounded-xl bg-[var(--color-surface)] border border-[var(--color-border)] hover:border-[var(--color-accent)]/50 transition-all group flex flex-col justify-between shadow-2xs cursor-pointer"
        >
          <div className="flex items-center justify-between text-[var(--color-text-muted)] mb-1">
            <span className="text-[10px] font-bold font-mono tracking-wider">
              {t('dashboard.kpi.evidence')}
            </span>
            <Database className="w-4 h-4 text-emerald-400 group-hover:scale-110 transition-transform" />
          </div>
          <div className="text-2xl font-extrabold text-[var(--color-text-primary)]">
            {dashboard?.summary.evidenceCount ?? '—'}
          </div>
          <div className="text-[10px] text-emerald-500 font-medium flex items-center gap-1 mt-1">
            <CheckCircle className="w-2.5 h-2.5" />
            <span>{dashboard?.summary.integrityVerifiedCount ?? 0} verified</span>
          </div>
        </Link>

        {/* Alerts */}
        <Link
          to="/alerts"
          className="p-3.5 rounded-xl bg-[var(--color-surface)] border border-[var(--color-border)] hover:border-red-500/50 transition-all group flex flex-col justify-between shadow-2xs cursor-pointer"
        >
          <div className="flex items-center justify-between text-[var(--color-text-muted)] mb-1">
            <span className="text-[10px] font-bold font-mono tracking-wider">
              {t('dashboard.kpi.alerts')}
            </span>
            <AlertTriangle className="w-4 h-4 text-red-500 group-hover:scale-110 transition-transform" />
          </div>
          <div className="text-2xl font-extrabold text-[var(--color-text-primary)] flex items-baseline gap-1.5">
            <span>{dashboard?.summary.alertCount ?? '—'}</span>
            {(dashboard?.summary.highRiskAlerts ?? 0) > 0 && (
              <span className="text-xs font-bold text-red-500 font-mono">
                ({dashboard?.summary.highRiskAlerts} high)
              </span>
            )}
          </div>
          <div className="text-[10px] text-[var(--color-text-muted)] flex items-center gap-1 mt-1">
            <span>Anomaly signals</span>
            <ArrowRight className="w-2.5 h-2.5 group-hover:translate-x-0.5 transition-transform" />
          </div>
        </Link>

        {/* Cross-Case Links */}
        <Link
          to="/entity-resolution"
          className="p-3.5 rounded-xl bg-[var(--color-surface)] border border-[var(--color-border)] hover:border-amber-500/50 transition-all group flex flex-col justify-between shadow-2xs cursor-pointer"
        >
          <div className="flex items-center justify-between text-[var(--color-text-muted)] mb-1">
            <span className="text-[10px] font-bold font-mono tracking-wider">
              {t('dashboard.kpi.crossCase')}
            </span>
            <GitMerge className="w-4 h-4 text-amber-500 group-hover:scale-110 transition-transform" />
          </div>
          <div className="text-2xl font-extrabold text-[var(--color-text-primary)]">
            {dashboard?.summary.crossCaseTotalCount ?? '—'}
          </div>
          <div className="text-[10px] text-amber-500 font-medium flex items-center gap-1 mt-1">
            <span>{dashboard?.summary.crossCaseConfirmedCount ?? 0} confirmed</span>
          </div>
        </Link>

        {/* Model Signals */}
        <Link
          to="/analytics"
          className="p-3.5 rounded-xl bg-[var(--color-surface)] border border-[var(--color-border)] hover:border-[var(--color-accent)]/50 transition-all group flex flex-col justify-between shadow-2xs cursor-pointer"
        >
          <div className="flex items-center justify-between text-[var(--color-text-muted)] mb-1">
            <span className="text-[10px] font-bold font-mono tracking-wider">
              {t('dashboard.kpi.modelSignals')}
            </span>
            <Cpu className="w-4 h-4 text-[var(--color-accent)] group-hover:scale-110 transition-transform" />
          </div>
          <div className="text-2xl font-extrabold text-[var(--color-text-primary)]">
            {dashboard?.summary.modelSignalCount ?? '—'}
          </div>
          <div className="text-[10px] text-[var(--color-accent)] font-medium flex items-center gap-1 mt-1">
            <span>Requires review</span>
          </div>
        </Link>
      </div>

      {/* Main Two-Column Intelligence Grid */}
      <div className="grid grid-cols-1 lg:grid-cols-12 gap-5">
        {/* Left Column: Network Overview, Prioritized Signals, Timeline, Map (8 cols) */}
        <div className="lg:col-span-8 space-y-5">
          {/* Network Overview Card */}
          <div className="rounded-xl bg-[var(--color-surface)] border border-[var(--color-border)] p-4 sm:p-5 shadow-sm space-y-4">
            <div className="flex items-center justify-between border-b border-[var(--color-border-subtle)] pb-3">
              <div className="flex items-center gap-2">
                <GitBranch className="w-4 h-4 text-[var(--color-accent)]" />
                <h2 className="text-sm font-bold text-[var(--color-text-primary)] uppercase tracking-wider">
                  {t('dashboard.network.title')}
                </h2>
              </div>
              <Link
                to="/network"
                className="text-xs text-[var(--color-accent)] hover:underline flex items-center gap-1 font-semibold"
              >
                <span>{t('dashboard.network.openFullGraph')}</span>
                <ChevronRight className="w-3.5 h-3.5" />
              </Link>
            </div>

            {/* Topology Summary Badges */}
            <div className="grid grid-cols-3 gap-2.5">
              <div className="p-2.5 rounded-lg bg-[var(--color-surface-subtle)] border border-[var(--color-border-subtle)] text-center">
                <span className="text-[10px] font-mono uppercase text-[var(--color-text-muted)] block">
                  {t('dashboard.network.nodes')}
                </span>
                <span className="text-base font-extrabold text-[var(--color-text-primary)]">
                  {dashboard?.network.nodeCount ?? 0}
                </span>
              </div>
              <div className="p-2.5 rounded-lg bg-[var(--color-surface-subtle)] border border-[var(--color-border-subtle)] text-center">
                <span className="text-[10px] font-mono uppercase text-[var(--color-text-muted)] block">
                  {t('dashboard.network.relationships')}
                </span>
                <span className="text-base font-extrabold text-[var(--color-text-primary)]">
                  {dashboard?.network.relationshipCount ?? 0}
                </span>
              </div>
              <div className="p-2.5 rounded-lg bg-[var(--color-surface-subtle)] border border-[var(--color-border-subtle)] text-center">
                <span className="text-[10px] font-mono uppercase text-[var(--color-text-muted)] block">
                  {t('dashboard.network.components')}
                </span>
                <span className="text-base font-extrabold text-[var(--color-text-primary)]">
                  {dashboard?.network.componentCount ?? 0}
                </span>
              </div>
            </div>

            {/* Top Connected Entities */}
            {dashboard?.network.topConnectedEntities && dashboard.network.topConnectedEntities.length > 0 && (
              <div className="space-y-2 pt-1">
                <p className="text-xs font-semibold text-[var(--color-text-secondary)]">
                  {t('dashboard.network.topConnected')}
                </p>
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
                  {dashboard.network.topConnectedEntities.slice(0, 4).map((ent) => (
                    <div
                      key={ent.entityId}
                      className="flex items-center justify-between p-2 rounded-lg bg-[var(--color-surface-subtle)] border border-[var(--color-border-subtle)] text-xs"
                    >
                      <div className="flex items-center gap-2 min-w-0">
                        <div className="w-2 h-2 rounded-full bg-[var(--color-accent)] shrink-0" />
                        <span className="font-semibold text-[var(--color-text-primary)] truncate">
                          {ent.canonicalName}
                        </span>
                        <span className="text-[10px] font-mono px-1.5 py-0.2 rounded bg-[var(--color-surface)] text-[var(--color-text-muted)] border border-[var(--color-border-subtle)]">
                          {ent.entityType}
                        </span>
                      </div>
                      <span className="text-[11px] font-mono font-bold text-[var(--color-accent)] shrink-0">
                        {t('dashboard.network.degree')}: {ent.degree}
                      </span>
                    </div>
                  ))}
                </div>
              </div>
            )}
          </div>

          {/* Prioritized Signals Stream */}
          <div className="rounded-xl bg-[var(--color-surface)] border border-[var(--color-border)] p-4 sm:p-5 shadow-sm space-y-4">
            <div className="flex items-center justify-between border-b border-[var(--color-border-subtle)] pb-3">
              <div className="flex items-center gap-2">
                <AlertTriangle className="w-4 h-4 text-amber-500" />
                <h2 className="text-sm font-bold text-[var(--color-text-primary)] uppercase tracking-wider">
                  {t('dashboard.signals.title')}
                </h2>
              </div>
              <span className="text-xs font-mono text-[var(--color-text-muted)]">
                {dashboard?.signals?.length ?? 0} active signals
              </span>
            </div>

            <div className="space-y-2.5">
              {dashboard?.signals && dashboard.signals.length > 0 ? (
                dashboard.signals.map((sig) => (
                  <div
                    key={sig.id}
                    className="p-3.5 rounded-lg bg-[var(--color-surface-subtle)] border border-[var(--color-border-subtle)] hover:border-[var(--color-border)] transition-colors space-y-2"
                  >
                    <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-1.5">
                      <div className="flex flex-wrap items-center gap-2">
                        <span className={`text-[10px] font-bold px-2 py-0.5 rounded border ${getSeverityBadgeClass(sig.priority)} font-mono`}>
                          {sig.priority}
                        </span>
                        <span className="text-[10px] font-mono px-2 py-0.5 rounded bg-[var(--color-surface)] text-[var(--color-text-muted)] border border-[var(--color-border-subtle)] uppercase">
                          {sig.type.replace('_', ' ')}
                        </span>
                        {sig.type === 'MODEL_SIGNAL' && (
                          <span className="text-[10px] font-semibold text-[var(--color-accent)] bg-[var(--color-accent)]/10 px-2 py-0.5 rounded border border-[var(--color-accent)]/20">
                            Model-generated signal requiring review
                          </span>
                        )}
                        <h3 className="text-xs font-bold text-[var(--color-text-primary)]">
                          {sig.title}
                        </h3>
                      </div>

                      <div className="flex items-center gap-2 shrink-0">
                        {sig.actionUrl ? (
                          <Link
                            to={sig.actionUrl}
                            className="text-xs text-[var(--color-accent)] hover:underline font-semibold flex items-center gap-1"
                          >
                            <span>Inspect</span>
                            <ChevronRight className="w-3 h-3" />
                          </Link>
                        ) : null}
                      </div>
                    </div>

                    <p className="text-xs text-[var(--color-text-secondary)] leading-relaxed">
                      <strong className="text-[var(--color-text-primary)]">{t('dashboard.signals.whyItMatters')}:</strong> {sig.whyItMatters}
                    </p>

                    <div className="flex flex-wrap items-center gap-3 text-[11px] text-[var(--color-text-muted)] pt-1 border-t border-[var(--color-border-subtle)]">
                      <span>{t('dashboard.signals.evidenceCount')}: <strong className="text-[var(--color-text-primary)]">{sig.evidenceCount}</strong></span>
                      {sig.modelScore !== undefined && sig.modelScore !== null && (
                        <span>{t('dashboard.signals.modelScore')}: <strong className="text-[var(--color-accent)] font-mono">{(sig.modelScore * 100).toFixed(1)}%</strong> ({sig.modelName || 'GAT'})</span>
                      )}
                      <span>{t('dashboard.signals.status')}: <strong className="text-[var(--color-text-primary)] font-mono">{sig.status}</strong></span>
                    </div>
                  </div>
                ))
              ) : (
                <div className="py-6 text-center text-xs text-[var(--color-text-muted)]">
                  No critical signals pending review for this investigation.
                </div>
              )}
            </div>
          </div>

          {/* Timeline Preview Card */}
          <div className="rounded-xl bg-[var(--color-surface)] border border-[var(--color-border)] p-4 sm:p-5 shadow-sm space-y-4">
            <div className="flex items-center justify-between border-b border-[var(--color-border-subtle)] pb-3">
              <div className="flex items-center gap-2">
                <Clock className="w-4 h-4 text-[var(--color-accent)]" />
                <h2 className="text-sm font-bold text-[var(--color-text-primary)] uppercase tracking-wider">
                  {t('dashboard.timeline.title')}
                </h2>
              </div>
              <Link
                to="/timeline"
                className="text-xs text-[var(--color-accent)] hover:underline flex items-center gap-1 font-semibold"
              >
                <span>{t('dashboard.timeline.openTimeline')}</span>
                <ChevronRight className="w-3.5 h-3.5" />
              </Link>
            </div>

            <div className="space-y-2">
              {dashboard?.timeline && dashboard.timeline.length > 0 ? (
                dashboard.timeline.slice(0, 4).map((evt) => (
                  <div
                    key={evt.eventId}
                    className="p-2.5 rounded-lg bg-[var(--color-surface-subtle)] border border-[var(--color-border-subtle)] flex items-start gap-3 text-xs"
                  >
                    <div className="p-1 rounded bg-[var(--color-surface)] border border-[var(--color-border-subtle)] text-[var(--color-accent)] mt-0.5 shrink-0">
                      <Clock className="w-3.5 h-3.5" />
                    </div>
                    <div className="flex-1 min-w-0 space-y-0.5">
                      <div className="flex items-center justify-between gap-2">
                        <span className="font-bold text-[var(--color-text-primary)] truncate">
                          {evt.eventType.replace('_', ' ')}
                        </span>
                        <span className="font-mono text-[10px] text-[var(--color-text-muted)] shrink-0">
                          {evt.precision === 'TIME_UNAVAILABLE'
                            ? t('dashboard.timeline.timeUnavailable')
                            : evt.precision === 'DATE_ONLY'
                            ? `${evt.formattedTime} (Date only)`
                            : evt.formattedTime}
                        </span>
                      </div>
                      <p className="text-[var(--color-text-secondary)] text-[11px] leading-relaxed">
                        {evt.description}
                      </p>
                      {evt.location && (
                        <span className="text-[10px] text-[var(--color-text-muted)] flex items-center gap-1">
                          <MapPin className="w-2.5 h-2.5" />
                          <span>{evt.location}</span>
                        </span>
                      )}
                    </div>
                  </div>
                ))
              ) : (
                <div className="py-4 text-center text-xs text-[var(--color-text-muted)]">
                  No chronological events recorded.
                </div>
              )}
            </div>
          </div>

          {/* Map Preview Card */}
          <div className="rounded-xl bg-[var(--color-surface)] border border-[var(--color-border)] p-4 sm:p-5 shadow-sm space-y-4">
            <div className="flex items-center justify-between border-b border-[var(--color-border-subtle)] pb-3">
              <div className="flex items-center gap-2">
                <MapPin className="w-4 h-4 text-emerald-500" />
                <h2 className="text-sm font-bold text-[var(--color-text-primary)] uppercase tracking-wider">
                  {t('dashboard.map.title')}
                </h2>
              </div>
              <Link
                to="/map"
                className="text-xs text-[var(--color-accent)] hover:underline flex items-center gap-1 font-semibold"
              >
                <span>{t('dashboard.map.openMap')}</span>
                <ChevronRight className="w-3.5 h-3.5" />
              </Link>
            </div>

            <div className="grid grid-cols-1 sm:grid-cols-2 gap-2.5">
              {dashboard?.locations && dashboard.locations.length > 0 ? (
                dashboard.locations.slice(0, 4).map((loc) => (
                  <div
                    key={loc.locationId}
                    className="p-2.5 rounded-lg bg-[var(--color-surface-subtle)] border border-[var(--color-border-subtle)] space-y-1 text-xs"
                  >
                    <div className="flex items-center justify-between gap-1">
                      <span className="font-semibold text-[var(--color-text-primary)] truncate">
                        {loc.name}
                      </span>
                      <span className="text-[10px] font-mono px-1.5 py-0.5 rounded bg-[var(--color-surface)] text-[var(--color-text-muted)] border border-[var(--color-border-subtle)] shrink-0">
                        {loc.activityCount} visits
                      </span>
                    </div>
                    <div className="text-[10px] font-mono text-[var(--color-text-muted)]">
                      {loc.hasCoordinates ? (
                        <span className="text-emerald-500 font-semibold">{loc.coordinateDisplay}</span>
                      ) : (
                        <span className="text-amber-500">{t('dashboard.map.withoutCoordinates')}</span>
                      )}
                    </div>
                  </div>
                ))
              ) : (
                <div className="py-4 col-span-2 text-center text-xs text-[var(--color-text-muted)]">
                  No geographic coordinates linked to active case entities.
                </div>
              )}
            </div>
          </div>
        </div>

        {/* Right Column: Evidence Integrity, Cross-Case, Actions Queue, Activity (4 cols) */}
        <div className="lg:col-span-4 space-y-5">
          {/* Evidence Integrity Card */}
          <div className="rounded-xl bg-[var(--color-surface)] border border-[var(--color-border)] p-4 sm:p-5 shadow-sm space-y-4">
            <div className="flex items-center justify-between border-b border-[var(--color-border-subtle)] pb-3">
              <div className="flex items-center gap-2">
                <ShieldCheck className="w-4 h-4 text-emerald-500" />
                <h2 className="text-sm font-bold text-[var(--color-text-primary)] uppercase tracking-wider">
                  {t('dashboard.integrity.title')}
                </h2>
              </div>
              <span className={`text-[10px] font-mono font-bold px-2 py-0.5 rounded border ${getLedgerHealthClass(dashboard?.integrity.ledgerHealthStatus || 'VALID')}`}>
                {dashboard?.integrity.ledgerHealthStatus || 'VALID'}
              </span>
            </div>

            {/* Metrics Breakdown */}
            <div className="grid grid-cols-3 gap-2 text-center">
              <div className="p-2 rounded-lg bg-[var(--color-surface-subtle)] border border-[var(--color-border-subtle)]">
                <span className="text-[9px] font-mono uppercase text-emerald-500 block">
                  {t('dashboard.integrity.verified')}
                </span>
                <span className="text-sm font-extrabold text-[var(--color-text-primary)]">
                  {dashboard?.integrity.verifiedCount ?? 0}
                </span>
              </div>
              <div className="p-2 rounded-lg bg-[var(--color-surface-subtle)] border border-[var(--color-border-subtle)]">
                <span className="text-[9px] font-mono uppercase text-amber-500 block">
                  {t('dashboard.integrity.modified')}
                </span>
                <span className="text-sm font-extrabold text-[var(--color-text-primary)]">
                  {dashboard?.integrity.modifiedCount ?? 0}
                </span>
              </div>
              <div className="p-2 rounded-lg bg-[var(--color-surface-subtle)] border border-[var(--color-border-subtle)]">
                <span className="text-[9px] font-mono uppercase text-[var(--color-text-muted)] block">
                  {t('dashboard.integrity.unreconciled')}
                </span>
                <span className="text-sm font-extrabold text-[var(--color-text-primary)]">
                  {dashboard?.integrity.unreconciledCount ?? 0}
                </span>
              </div>
            </div>

            {/* Warning Box if any modified items */}
            {dashboard?.integrity.warnings && dashboard.integrity.warnings.length > 0 && (
              <div className="p-3 rounded-lg bg-red-500/10 border border-red-500/30 text-xs text-red-500 space-y-1.5">
                <div className="flex items-center gap-1.5 font-bold uppercase tracking-wider text-[10px]">
                  <ShieldAlert className="w-3.5 h-3.5 shrink-0" />
                  <span>{t('dashboard.integrity.warning')}</span>
                </div>
                {dashboard.integrity.warnings.map((warn, i) => (
                  <div key={i} className="text-[11px] space-y-0.5 pt-1 border-t border-red-500/20">
                    <p className="font-semibold text-[var(--color-text-primary)]">{warn.fileName}</p>
                    <p className="text-[10px] font-mono break-all opacity-80">
                      Exp: {warn.expectedSha256.slice(0, 16)}...
                    </p>
                    <p className="text-[10px] font-mono break-all text-red-400">
                      Cur: {warn.currentSha256.slice(0, 16)}...
                    </p>
                  </div>
                ))}
              </div>
            )}

            <div className="flex items-center justify-between pt-1">
              <Link
                to="/evidence"
                className="text-xs text-[var(--color-accent)] hover:underline flex items-center gap-1 font-semibold"
              >
                <span>{t('dashboard.integrity.viewEvidenceIntegrity')}</span>
                <ChevronRight className="w-3 h-3" />
              </Link>
            </div>
          </div>

          {/* Cross-Case Intelligence Card */}
          <div className="rounded-xl bg-[var(--color-surface)] border border-[var(--color-border)] p-4 sm:p-5 shadow-sm space-y-4">
            <div className="flex items-center justify-between border-b border-[var(--color-border-subtle)] pb-3">
              <div className="flex items-center gap-2">
                <GitMerge className="w-4 h-4 text-amber-500" />
                <h2 className="text-sm font-bold text-[var(--color-text-primary)] uppercase tracking-wider">
                  {t('dashboard.crossCase.title')}
                </h2>
              </div>
              <Link
                to="/entity-resolution"
                className="text-xs text-[var(--color-accent)] hover:underline flex items-center gap-1 font-semibold"
              >
                <span>Review</span>
                <ChevronRight className="w-3 h-3" />
              </Link>
            </div>

            {/* Counts breakdown */}
            <div className="flex items-center justify-between text-xs px-2 py-1.5 rounded-lg bg-[var(--color-surface-subtle)] border border-[var(--color-border-subtle)] font-mono">
              <span>{t('dashboard.crossCase.confirmed')}: <strong className="text-emerald-500">{dashboard?.crossCase.confirmedCount ?? 0}</strong></span>
              <span>•</span>
              <span>{t('dashboard.crossCase.potential')}: <strong className="text-amber-500">{dashboard?.crossCase.potentialCount ?? 0}</strong></span>
              <span>•</span>
              <span>{t('dashboard.crossCase.modelPredicted')}: <strong className="text-[var(--color-accent)]">{dashboard?.crossCase.modelPredictedCount ?? 0}</strong></span>
            </div>

            <div className="space-y-2">
              {dashboard?.crossCase.connections && dashboard.crossCase.connections.length > 0 ? (
                dashboard.crossCase.connections.slice(0, 3).map((conn) => (
                  <div
                    key={conn.connectionId}
                    className="p-2.5 rounded-lg bg-[var(--color-surface-subtle)] border border-[var(--color-border-subtle)] space-y-1 text-xs"
                  >
                    <div className="flex items-center justify-between gap-2">
                      <span className="font-mono font-bold text-[var(--color-accent)] text-[11px]">
                        {conn.targetCaseNumber}
                      </span>
                      <span className="text-[9px] font-mono px-1.5 py-0.2 rounded bg-[var(--color-surface)] border border-[var(--color-border-subtle)]">
                        {conn.status}
                      </span>
                    </div>
                    <p className="text-[11px] text-[var(--color-text-secondary)] truncate">
                      {conn.targetCaseTitle}
                    </p>
                    <div className="flex items-center justify-between text-[10px] text-[var(--color-text-muted)] pt-0.5">
                      <span>{conn.reason}</span>
                      <span>{conn.supportingEvidenceCount} records</span>
                    </div>
                  </div>
                ))
              ) : (
                <div className="py-3 text-center text-xs text-[var(--color-text-muted)]">
                  No cross-case links detected.
                </div>
              )}
            </div>
          </div>

          {/* Investigator Actions Queue */}
          <div className="rounded-xl bg-[var(--color-surface)] border border-[var(--color-border)] p-4 sm:p-5 shadow-sm space-y-4">
            <div className="flex items-center justify-between border-b border-[var(--color-border-subtle)] pb-3">
              <div className="flex items-center gap-2">
                <CheckCircle2 className="w-4 h-4 text-[var(--color-accent)]" />
                <h2 className="text-sm font-bold text-[var(--color-text-primary)] uppercase tracking-wider">
                  {t('dashboard.actions.title')}
                </h2>
              </div>
              <span className="text-xs font-mono text-[var(--color-text-muted)]">
                {dashboard?.actions?.length ?? 0} pending
              </span>
            </div>

            <div className="space-y-2">
              {dashboard?.actions && dashboard.actions.length > 0 ? (
                dashboard.actions.map((act) => (
                  <div
                    key={act.id}
                    className="p-2.5 rounded-lg bg-[var(--color-surface-subtle)] border border-[var(--color-border-subtle)] flex items-center justify-between gap-2 text-xs"
                  >
                    <div className="space-y-0.5 min-w-0">
                      <div className="flex items-center gap-1.5">
                        <span className={`text-[9px] font-bold px-1.5 py-0.2 rounded border font-mono ${getSeverityBadgeClass(act.priority)}`}>
                          {act.priority}
                        </span>
                        <span className="text-[9px] font-mono text-[var(--color-text-muted)]">
                          {act.type.replace('_', ' ')}
                        </span>
                      </div>
                      <p className="text-[11px] text-[var(--color-text-primary)] font-medium truncate">
                        {act.description}
                      </p>
                    </div>

                    <Link
                      to={act.actionUrl || '/alerts'}
                      className="px-2.5 py-1 rounded bg-[var(--color-accent)]/15 border border-[var(--color-accent)]/30 text-[var(--color-accent)] text-[11px] font-semibold hover:bg-[var(--color-accent)]/25 transition-colors shrink-0"
                    >
                      {act.actionLabel || 'Review'}
                    </Link>
                  </div>
                ))
              ) : (
                <div className="py-3 text-center text-xs text-[var(--color-text-muted)]">
                  All action items reviewed.
                </div>
              )}
            </div>
          </div>

          {/* Recent Investigation Activity */}
          <div className="rounded-xl bg-[var(--color-surface)] border border-[var(--color-border)] p-4 sm:p-5 shadow-sm space-y-4">
            <div className="flex items-center justify-between border-b border-[var(--color-border-subtle)] pb-3">
              <div className="flex items-center gap-2">
                <Activity className="w-4 h-4 text-blue-400" />
                <h2 className="text-sm font-bold text-[var(--color-text-primary)] uppercase tracking-wider">
                  {t('dashboard.activity.title')}
                </h2>
              </div>
              <Link
                to="/audit"
                className="text-xs text-[var(--color-accent)] hover:underline flex items-center gap-1 font-semibold"
              >
                <span>{t('dashboard.activity.viewAudit')}</span>
                <ChevronRight className="w-3 h-3" />
              </Link>
            </div>

            <div className="space-y-2">
              {dashboard?.recentActivity && dashboard.recentActivity.length > 0 ? (
                dashboard.recentActivity.slice(0, 4).map((act) => (
                  <div
                    key={act.id}
                    className="p-2 rounded-lg bg-[var(--color-surface-subtle)] border border-[var(--color-border-subtle)] text-xs space-y-0.5"
                  >
                    <div className="flex items-center justify-between gap-1 text-[10px] font-mono text-[var(--color-text-muted)]">
                      <span className="font-semibold text-[var(--color-text-primary)] truncate">
                        {act.actorName}
                      </span>
                      <span>
                        {new Date(act.timestampUtc).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                      </span>
                    </div>
                    <p className="text-[11px] text-[var(--color-text-secondary)] truncate">
                      {act.action.replace('_', ' ')}: {act.details}
                    </p>
                  </div>
                ))
              ) : (
                <div className="py-3 text-center text-xs text-[var(--color-text-muted)]">
                  No recent audit records.
                </div>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
