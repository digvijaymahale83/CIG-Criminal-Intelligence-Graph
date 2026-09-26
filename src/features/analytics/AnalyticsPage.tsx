import React, { useState, useEffect, useCallback } from 'react';
import {
  BarChart3, TrendingUp, Network, Award, ShieldCheck, Play, RefreshCw,
  GitBranch, CheckCircle2, XCircle, Eye, AlertTriangle, Layers, ArrowRight,
  Filter, Sparkles, SlidersHorizontal, Info, ExternalLink
} from 'lucide-react';
import { useNavigate } from 'react-router-dom';
import { useActiveInvestigation } from '../../hooks/useActiveInvestigation';
import { Card } from '../../components/common/Card';
import { ResponsibleAiNotice } from '../../components/common/ResponsibleAiNotice';
import {
  analyticsService,
  GraphAnalysisRunDto,
  CentralityResultsDto,
  CommunityClusterDto,
  ConnectedComponentDetailDto,
  NetworkStatisticsDto,
  GraphAnalyticalLeadDto
} from '../../services/analytics/analytics.service';

export const AnalyticsPage: React.FC = () => {
  const navigate = useNavigate();
  const { activeCaseId, activeCaseNumber, activeCaseTitle } = useActiveInvestigation();
  const effectiveCaseId = activeCaseId || 'inv-2026-001';

  // Loading & Action states
  const [runningAnalytics, setRunningAnalytics] = useState(false);
  const [loadingData, setLoadingData] = useState(true);
  const [activeTab, setActiveTab] = useState<'OVERVIEW' | 'CENTRALITY' | 'COMMUNITIES' | 'LEADS'>('OVERVIEW');

  // Analytics data states
  const [latestRun, setLatestRun] = useState<GraphAnalysisRunDto | null>(null);
  const [statistics, setStatistics] = useState<NetworkStatisticsDto | null>(null);
  const [centrality, setCentrality] = useState<CentralityResultsDto | null>(null);
  const [communities, setCommunities] = useState<CommunityClusterDto[]>([]);
  const [components, setComponents] = useState<ConnectedComponentDetailDto[]>([]);
  const [leads, setLeads] = useState<GraphAnalyticalLeadDto[]>([]);

  // Filters & sorting
  const [centralitySort, setCentralitySort] = useState<'Betweenness' | 'Degree' | 'Closeness' | 'PageRank'>('Betweenness');
  const [leadStatusFilter, setLeadStatusFilter] = useState<'ALL' | 'PENDING' | 'CONFIRMED' | 'DISMISSED'>('ALL');
  const [leadMinScore, setLeadMinScore] = useState<number>(0.50);

  // Modals
  const [selectedLeadForExplanation, setSelectedLeadForExplanation] = useState<GraphAnalyticalLeadDto | null>(null);
  const [selectedLeadForReview, setSelectedLeadForReview] = useState<GraphAnalyticalLeadDto | null>(null);
  const [reviewAction, setReviewAction] = useState<'CONFIRMED' | 'DISMISSED'>('CONFIRMED');
  const [reviewNotes, setReviewNotes] = useState('');
  const [reviewRelType, setReviewRelType] = useState('ASSOCIATE_OF');
  const [submittingReview, setSubmittingReview] = useState(false);

  // Fetch all analytics data
  const loadAnalytics = useCallback(async () => {
    setLoadingData(true);
    try {
      const [runRes, statsRes, centRes, commRes, compRes, leadsRes] = await Promise.allSettled([
        analyticsService.getLatestRun(effectiveCaseId),
        analyticsService.getStatistics(effectiveCaseId),
        analyticsService.getCentrality(effectiveCaseId, centralitySort),
        analyticsService.getCommunities(effectiveCaseId),
        analyticsService.getComponents(effectiveCaseId),
        analyticsService.getLeads(effectiveCaseId, leadStatusFilter === 'ALL' ? undefined : leadStatusFilter, leadMinScore)
      ]);

      if (runRes.status === 'fulfilled') setLatestRun(runRes.value);
      if (statsRes.status === 'fulfilled') setStatistics(statsRes.value);
      if (centRes.status === 'fulfilled') setCentrality(centRes.value);
      if (commRes.status === 'fulfilled') setCommunities(commRes.value);
      if (compRes.status === 'fulfilled') setComponents(compRes.value);
      if (leadsRes.status === 'fulfilled') setLeads(leadsRes.value);
    } catch (err) {
      console.error('Failed to load graph analytics:', err);
    } finally {
      setLoadingData(false);
    }
  }, [effectiveCaseId, centralitySort, leadStatusFilter, leadMinScore]);

  useEffect(() => {
    loadAnalytics();
  }, [loadAnalytics]);

  // Trigger real analysis run
  const handleRunAnalytics = async () => {
    setRunningAnalytics(true);
    try {
      await analyticsService.runAnalytics(effectiveCaseId, {
        candidateThreshold: 0.50,
        maxCandidates: 25,
        gatEmbeddingDim: 64,
        gatHeads: 4
      });
      await loadAnalytics();
    } catch (err) {
      console.error('Failed to execute analytics run:', err);
    } finally {
      setRunningAnalytics(false);
    }
  };

  // Submit investigator review decision
  const handleSubmitReview = async () => {
    if (!selectedLeadForReview) return;
    setSubmittingReview(true);
    try {
      await analyticsService.reviewLead(selectedLeadForReview.id, {
        status: reviewAction,
        reviewNotes: reviewNotes.trim() || undefined,
        suggestedRelationshipType: reviewAction === 'CONFIRMED' ? reviewRelType : undefined
      });
      setSelectedLeadForReview(null);
      setReviewNotes('');
      await loadAnalytics();
    } catch (err) {
      console.error('Failed to review model lead:', err);
    } finally {
      setSubmittingReview(false);
    }
  };

  return (
    <div className="space-y-6" id="analytics-page">
      {/* Header & Controls */}
      <div className="flex flex-col lg:flex-row lg:items-center justify-between gap-4 pb-4 border-b border-[var(--color-border)]">
        <div>
          <div className="flex items-center gap-2.5">
            <h1 className="text-xl font-bold text-[var(--color-text-primary)] tracking-tight">Graph Intelligence & GAT Analytics</h1>
            <span className="text-[10px] font-mono px-2 py-0.5 rounded bg-[var(--color-surface-subtle)] text-[var(--color-text-muted)] border border-[var(--color-border)]">
              CASE: {activeCaseNumber}
            </span>
            {latestRun && (
              <span className="text-[10px] font-mono px-2 py-0.5 rounded bg-[#EBF5EC] text-[#1E5C2B] border border-[#C4E4C8]">
                {latestRun.modelVersion}
              </span>
            )}
          </div>
          <p className="text-xs text-[var(--color-text-secondary)] mt-1">
            Deterministic centrality metrics, Louvain association clusters, and Graph Attention Network (GAT) representation learning.
          </p>
        </div>

        <div className="flex items-center gap-3">
          <button
            onClick={loadAnalytics}
            disabled={loadingData || runningAnalytics}
            className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg border border-[var(--color-border)] text-xs font-medium text-[var(--color-text-primary)] hover:bg-[var(--color-surface-subtle)] transition-colors disabled:opacity-50"
            title="Refresh analytics data"
          >
            <RefreshCw className={`w-3.5 h-3.5 ${loadingData ? 'animate-spin' : ''}`} />
            Refresh
          </button>

          <button
            onClick={handleRunAnalytics}
            disabled={runningAnalytics}
            className="flex items-center gap-2 px-4 py-1.5 rounded-lg bg-[var(--color-accent)] text-white text-xs font-medium hover:bg-[var(--color-accent-hover)] transition-colors shadow-xs disabled:opacity-50"
          >
            {runningAnalytics ? (
              <>
                <RefreshCw className="w-3.5 h-3.5 animate-spin" />
                Executing Pipeline...
              </>
            ) : (
              <>
                <Play className="w-3.5 h-3.5 fill-current" />
                Run Graph Analytics
              </>
            )}
          </button>
        </div>
      </div>

      <ResponsibleAiNotice />

      {/* KPI Overview Strip */}
      <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-6 gap-3">
        <div className="p-3.5 rounded-xl bg-[var(--color-surface)] border border-[var(--color-border)] shadow-2xs">
          <p className="text-[11px] font-medium text-[var(--color-text-muted)] uppercase tracking-wider">Total Entities</p>
          <p className="text-xl font-bold text-[var(--color-text-primary)] mt-1">
            {statistics ? statistics.totalEntities : latestRun?.nodeCount ?? '—'}
          </p>
          <p className="text-[10px] text-[var(--color-text-secondary)] mt-0.5">Verified nodes</p>
        </div>

        <div className="p-3.5 rounded-xl bg-[var(--color-surface)] border border-[var(--color-border)] shadow-2xs">
          <p className="text-[11px] font-medium text-[var(--color-text-muted)] uppercase tracking-wider">Relationships</p>
          <p className="text-xl font-bold text-[var(--color-text-primary)] mt-1">
            {statistics ? statistics.totalRelationships : latestRun?.edgeCount ?? '—'}
          </p>
          <p className="text-[10px] text-[var(--color-text-secondary)] mt-0.5">Verified edges</p>
        </div>

        <div className="p-3.5 rounded-xl bg-[var(--color-surface)] border border-[var(--color-border)] shadow-2xs">
          <p className="text-[11px] font-medium text-[var(--color-text-muted)] uppercase tracking-wider">Network Density</p>
          <p className="text-xl font-bold text-[var(--color-text-primary)] mt-1">
            {statistics ? (statistics.networkDensity).toFixed(3) : latestRun?.networkDensity.toFixed(3) ?? '—'}
          </p>
          <p className="text-[10px] text-[var(--color-text-secondary)] mt-0.5">Edge connectivity</p>
        </div>

        <div className="p-3.5 rounded-xl bg-[var(--color-surface)] border border-[var(--color-border)] shadow-2xs">
          <p className="text-[11px] font-medium text-[var(--color-text-muted)] uppercase tracking-wider">Average Degree</p>
          <p className="text-xl font-bold text-[var(--color-text-primary)] mt-1">
            {statistics ? statistics.averageDegree.toFixed(2) : latestRun?.averageDegree.toFixed(2) ?? '—'}
          </p>
          <p className="text-[10px] text-[var(--color-text-secondary)] mt-0.5">Connections/node</p>
        </div>

        <div className="p-3.5 rounded-xl bg-[var(--color-surface)] border border-[var(--color-border)] shadow-2xs">
          <p className="text-[11px] font-medium text-[var(--color-text-muted)] uppercase tracking-wider">Components</p>
          <p className="text-xl font-bold text-[var(--color-text-primary)] mt-1">
            {components.length > 0 ? components.length : latestRun?.connectedComponentsCount ?? '—'}
          </p>
          <p className="text-[10px] text-[var(--color-text-secondary)] mt-0.5">Disjoint subgraphs</p>
        </div>

        <div className="p-3.5 rounded-xl bg-[var(--color-surface)] border border-[var(--color-border)] shadow-2xs">
          <p className="text-[11px] font-medium text-[var(--color-text-muted)] uppercase tracking-wider">Clusters</p>
          <p className="text-xl font-bold text-[var(--color-text-primary)] mt-1">
            {communities.length > 0 ? communities.length : latestRun?.communitiesCount ?? '—'}
          </p>
          <p className="text-[10px] text-[var(--color-text-secondary)] mt-0.5">Association groups</p>
        </div>
      </div>

      {/* Tabs Bar */}
      <div className="flex border-b border-[var(--color-border)] gap-6 text-xs font-semibold">
        <button
          onClick={() => setActiveTab('OVERVIEW')}
          className={`pb-2.5 transition-colors border-b-2 ${
            activeTab === 'OVERVIEW'
              ? 'border-[var(--color-accent)] text-[var(--color-accent)]'
              : 'border-transparent text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]'
          }`}
        >
          Analytics Overview
        </button>
        <button
          onClick={() => setActiveTab('CENTRALITY')}
          className={`pb-2.5 transition-colors border-b-2 flex items-center gap-1.5 ${
            activeTab === 'CENTRALITY'
              ? 'border-[var(--color-accent)] text-[var(--color-accent)]'
              : 'border-transparent text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]'
          }`}
        >
          <span>Centrality Rankings</span>
          {centrality && (
            <span className="text-[10px] px-1.5 py-0.2 rounded-full bg-[var(--color-surface-subtle)] text-[var(--color-text-muted)]">
              {centrality.metrics.length}
            </span>
          )}
        </button>
        <button
          onClick={() => setActiveTab('COMMUNITIES')}
          className={`pb-2.5 transition-colors border-b-2 flex items-center gap-1.5 ${
            activeTab === 'COMMUNITIES'
              ? 'border-[var(--color-accent)] text-[var(--color-accent)]'
              : 'border-transparent text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]'
          }`}
        >
          <span>Association Clusters</span>
          <span className="text-[10px] px-1.5 py-0.2 rounded-full bg-[var(--color-surface-subtle)] text-[var(--color-text-muted)]">
            {communities.length}
          </span>
        </button>
        <button
          onClick={() => setActiveTab('LEADS')}
          className={`pb-2.5 transition-colors border-b-2 flex items-center gap-1.5 ${
            activeTab === 'LEADS'
              ? 'border-[var(--color-accent)] text-[var(--color-accent)]'
              : 'border-transparent text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)]'
          }`}
        >
          <span>Model-Generated Leads</span>
          {leads.length > 0 && (
            <span className="text-[10px] px-1.5 py-0.2 rounded-full bg-[var(--color-surface-subtle)] text-[var(--color-accent)] font-mono font-bold">
              {leads.length}
            </span>
          )}
        </button>
      </div>

      {/* Tab 1: Overview */}
      {activeTab === 'OVERVIEW' && (
        <div className="space-y-6">
          <div className="grid grid-cols-1 lg:grid-cols-3 gap-5">
            {/* Top Nexus Entities */}
            <Card
              title="Key Nexus Entities"
              subtitle="Entities with high shortest-path betweenness or topological degree"
            >
              <div className="space-y-2.5">
                {centrality?.metrics.slice(0, 4).map((m, idx) => (
                  <div
                    key={m.entityId}
                    className="p-3 rounded-lg bg-[var(--color-surface)] border border-[var(--color-border)] flex items-center justify-between"
                  >
                    <div>
                      <div className="flex items-center gap-2">
                        <span className="text-[10px] font-mono text-[var(--color-text-muted)]">#{idx + 1}</span>
                        <p className="text-xs font-semibold text-[var(--color-text-primary)]">{m.entityName}</p>
                      </div>
                      <p className="text-[10px] text-[var(--color-text-secondary)] mt-0.5">
                        Degree: {m.degree} • Betweenness: {m.betweennessCentrality.toFixed(3)}
                      </p>
                    </div>
                    <span
                      className={`text-[9px] font-mono px-2 py-0.5 rounded font-semibold ${
                        m.analyticalIndicator === 'Key Nexus Entity'
                          ? 'bg-[var(--color-surface-subtle)] text-[var(--color-accent)] border border-[var(--color-border)]'
                          : 'bg-[var(--color-surface-subtle)] text-[var(--color-text-primary)] border border-[var(--color-border)]'
                      }`}
                    >
                      {m.analyticalIndicator.toUpperCase()}
                    </span>
                  </div>
                ))}
              </div>
            </Card>

            {/* Association Clusters Snapshot */}
            <Card
              title="Association Clusters (Louvain)"
              subtitle="Dense communities detected from structural network density"
            >
              <div className="space-y-2.5">
                {communities.slice(0, 3).map((c) => (
                  <div
                    key={c.communityId}
                    className="p-3 rounded-lg bg-[var(--color-surface)] border border-[var(--color-border)]"
                  >
                    <div className="flex items-center justify-between">
                      <p className="text-xs font-semibold text-[var(--color-text-primary)]">{c.clusterLabel}</p>
                      <span className="text-[10px] font-mono px-1.5 py-0.5 rounded bg-[var(--color-surface-subtle)] text-[var(--color-text-primary)]">
                        Density: {(c.internalDensity * 100).toFixed(0)}%
                      </span>
                    </div>
                    <p className="text-[11px] text-[var(--color-text-secondary)] mt-1">
                      {c.entityCount} entities • {c.relationshipCount} connections
                    </p>
                    {c.sampleEntities.length > 0 && (
                      <p className="text-[10px] text-[var(--color-text-muted)] mt-1 italic truncate">
                        Key leads: {c.sampleEntities.join(', ')}
                      </p>
                    )}
                  </div>
                ))}
              </div>
            </Card>

            {/* GAT ML Insights Snapshot */}
            <Card
              title="GAT Link Predictions"
              subtitle="Neural representation learning candidates for investigator review"
            >
              <div className="space-y-2.5">
                {leads.slice(0, 3).map((l) => (
                  <div
                    key={l.id}
                    className="p-3 rounded-lg bg-[var(--color-surface)] border border-[var(--color-border)] space-y-1.5"
                  >
                    <div className="flex items-center justify-between">
                      <span className="text-[9px] font-mono px-1.5 py-0.5 rounded bg-[var(--color-surface-subtle)] text-[var(--color-accent)] font-semibold">
                        MODEL-GENERATED LEAD
                      </span>
                      <span className="text-xs font-bold text-[var(--color-accent)] font-mono">
                        {(l.score * 100).toFixed(0)}% Match
                      </span>
                    </div>
                    <p className="text-xs font-semibold text-[var(--color-text-primary)]">
                      {l.sourceEntityName} ↔ {l.targetEntityName}
                    </p>
                    <p className="text-[10px] text-[var(--color-text-secondary)]">
                      Signals: {l.signals.sharedNeighborsCount} shared neighbors, Latent Cosine: {l.signals.cosineSimilarity.toFixed(2)}
                    </p>
                  </div>
                ))}
                {leads.length === 0 && (
                  <p className="text-xs text-[var(--color-text-muted)] py-4 text-center">
                    No pending model leads surfaced. Run analysis to trigger GAT inference.
                  </p>
                )}
              </div>
            </Card>
          </div>
        </div>
      )}

      {/* Tab 2: Centrality Rankings Table */}
      {activeTab === 'CENTRALITY' && (
        <Card
          title="Network Centrality Rankings"
          subtitle="Measures network prominence, shortest-path brokerage, and link influence"
        >
          <div className="flex items-center justify-between pb-3 mb-3 border-b border-[var(--color-border)]">
            <div className="flex items-center gap-2">
              <span className="text-xs font-medium text-[var(--color-text-secondary)]">Sort By:</span>
              {(['Betweenness', 'Degree', 'Closeness', 'PageRank'] as const).map((mode) => (
                <button
                  key={mode}
                  onClick={() => setCentralitySort(mode)}
                  className={`text-xs px-2.5 py-1 rounded-md transition-colors ${
                    centralitySort === mode
                      ? 'bg-[var(--color-accent)] text-white font-medium'
                      : 'bg-[var(--color-surface-subtle)] text-[var(--color-text-primary)] hover:bg-[#EADDCF]'
                  }`}
                >
                  {mode} Centrality
                </button>
              ))}
            </div>
            <span className="text-[11px] text-[var(--color-text-muted)]">
              Showing {centrality?.metrics.length ?? 0} analyzed entities
            </span>
          </div>

          <div className="overflow-x-auto">
            <table className="w-full text-left text-xs">
              <thead className="bg-[var(--color-surface)] text-[var(--color-text-muted)] text-[10px] font-mono uppercase tracking-wider border-b border-[var(--color-border)]">
                <tr>
                  <th className="py-2.5 px-3">Rank</th>
                  <th className="py-2.5 px-3">Entity Name</th>
                  <th className="py-2.5 px-3">Type</th>
                  <th className="py-2.5 px-3">Degree</th>
                  <th className="py-2.5 px-3">Betweenness</th>
                  <th className="py-2.5 px-3">Closeness</th>
                  <th className="py-2.5 px-3">PageRank</th>
                  <th className="py-2.5 px-3">Analytical Indicator</th>
                  <th className="py-2.5 px-3 text-right">Actions</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-[var(--color-border)]">
                {centrality?.metrics.map((m, idx) => (
                  <tr key={m.entityId} className="hover:bg-[var(--color-surface)] transition-colors">
                    <td className="py-2.5 px-3 font-mono text-[var(--color-text-muted)]">#{idx + 1}</td>
                    <td className="py-2.5 px-3 font-semibold text-[var(--color-text-primary)]">{m.entityName}</td>
                    <td className="py-2.5 px-3">
                      <span className="text-[9px] font-mono px-2 py-0.5 rounded bg-[var(--color-surface-subtle)] text-[var(--color-text-primary)]">
                        {m.entityType}
                      </span>
                    </td>
                    <td className="py-2.5 px-3 font-mono">{m.degree}</td>
                    <td className="py-2.5 px-3 font-mono font-medium text-[var(--color-accent)]">
                      {m.betweennessCentrality.toFixed(4)}
                    </td>
                    <td className="py-2.5 px-3 font-mono">{m.closenessCentrality.toFixed(4)}</td>
                    <td className="py-2.5 px-3 font-mono">{m.pageRank.toFixed(5)}</td>
                    <td className="py-2.5 px-3">
                      <span
                        className={`text-[9px] font-mono px-2 py-0.5 rounded font-semibold ${
                          m.analyticalIndicator === 'Key Nexus Entity'
                            ? 'bg-[var(--color-surface-subtle)] text-[var(--color-accent)] border border-[var(--color-border)]'
                            : m.analyticalIndicator === 'High Connectivity Lead'
                            ? 'bg-[#EBF5EC] text-[#1E5C2B] border border-[#C4E4C8]'
                            : 'bg-[var(--color-surface-subtle)] text-[var(--color-text-primary)]'
                        }`}
                      >
                        {m.analyticalIndicator}
                      </span>
                    </td>
                    <td className="py-2.5 px-3 text-right">
                      <button
                        onClick={() => navigate(`/network?select=${m.entityId}`)}
                        className="text-[var(--color-accent)] hover:underline flex items-center gap-1 ml-auto font-medium"
                      >
                        Graph <ArrowRight className="w-3 h-3" />
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </Card>
      )}

      {/* Tab 3: Association Clusters (Communities) */}
      {activeTab === 'COMMUNITIES' && (
        <div className="space-y-4">
          <div className="p-3.5 rounded-lg bg-[var(--color-surface)] border border-[var(--color-border)] flex items-center justify-between text-xs">
            <div className="flex items-center gap-2">
              <Layers className="w-4 h-4 text-[var(--color-accent)]" />
              <span className="text-[var(--color-text-primary)] font-semibold">
                Louvain Modularity Group Detection: {communities.length} clusters identified
              </span>
            </div>
            <span className="text-[11px] text-[var(--color-text-secondary)]">
              Groups entities by internal relationship density
            </span>
          </div>

          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {communities.map((cluster) => (
              <div
                key={cluster.communityId}
                className="p-4 rounded-xl bg-[var(--color-surface)] border border-[var(--color-border)] shadow-2xs space-y-3"
              >
                <div className="flex items-center justify-between">
                  <h3 className="text-sm font-bold text-[var(--color-text-primary)]">{cluster.clusterLabel}</h3>
                  <span className="text-[10px] font-mono px-2 py-0.5 rounded bg-[var(--color-surface-subtle)] text-[var(--color-accent)] font-semibold">
                    {(cluster.internalDensity * 100).toFixed(0)}% Density
                  </span>
                </div>

                <div className="grid grid-cols-2 gap-2 text-xs py-2 border-y border-[var(--color-border)]">
                  <div>
                    <span className="text-[var(--color-text-muted)] text-[10px]">Entities:</span>
                    <p className="font-semibold text-[var(--color-text-primary)]">{cluster.entityCount}</p>
                  </div>
                  <div>
                    <span className="text-[var(--color-text-muted)] text-[10px]">Internal Edges:</span>
                    <p className="font-semibold text-[var(--color-text-primary)]">{cluster.relationshipCount}</p>
                  </div>
                </div>

                <div>
                  <p className="text-[10px] font-medium text-[var(--color-text-muted)] uppercase tracking-wider mb-1">
                    Composition:
                  </p>
                  <div className="flex flex-wrap gap-1">
                    {Object.entries(cluster.entityTypeDistribution).map(([type, count]) => (
                      <span
                        key={type}
                        className="text-[9px] font-mono px-1.5 py-0.5 rounded bg-[var(--color-surface-subtle)] text-[var(--color-text-primary)]"
                      >
                        {type}: {count}
                      </span>
                    ))}
                  </div>
                </div>

                {cluster.sampleEntities.length > 0 && (
                  <div>
                    <p className="text-[10px] font-medium text-[var(--color-text-muted)] uppercase tracking-wider mb-1">
                      Prominent Leads:
                    </p>
                    <p className="text-xs text-[var(--color-text-primary)]">{cluster.sampleEntities.join(', ')}</p>
                  </div>
                )}
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Tab 4: Model-Generated Leads (GAT Predictions) */}
      {activeTab === 'LEADS' && (
        <div className="space-y-4">
          {/* Filters Bar */}
          <div className="p-3.5 rounded-lg bg-[var(--color-surface)] border border-[var(--color-border)] flex flex-wrap items-center justify-between gap-3 text-xs shadow-2xs">
            <div className="flex items-center gap-2">
              <span className="text-[var(--color-text-secondary)] font-medium">Status Filter:</span>
              {(['ALL', 'PENDING', 'CONFIRMED', 'DISMISSED'] as const).map((st) => (
                <button
                  key={st}
                  onClick={() => setLeadStatusFilter(st)}
                  className={`px-2.5 py-1 rounded-md transition-colors ${
                    leadStatusFilter === st
                      ? 'bg-[var(--color-accent)] text-white font-medium'
                      : 'bg-[var(--color-surface-subtle)] text-[var(--color-text-primary)] hover:bg-[#EADDCF]'
                  }`}
                >
                  {st}
                </button>
              ))}
            </div>

            <div className="flex items-center gap-3">
              <span className="text-[var(--color-text-secondary)]">Min Score:</span>
              <input
                type="range"
                min="0.30"
                max="0.95"
                step="0.05"
                value={leadMinScore}
                onChange={(e) => setLeadMinScore(parseFloat(e.target.value))}
                className="w-24 accent-[#A6522C]"
              />
              <span className="font-mono text-xs font-bold text-[var(--color-accent)]">
                {(leadMinScore * 100).toFixed(0)}%
              </span>
            </div>
          </div>

          {/* Leads Grid */}
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {leads.map((lead) => (
              <div
                key={lead.id}
                className="p-4 rounded-xl bg-[var(--color-surface)] border border-[var(--color-border)] shadow-2xs space-y-3"
              >
                <div className="flex items-center justify-between">
                  <span className="text-[9px] font-mono px-2 py-0.5 rounded bg-[var(--color-surface-subtle)] text-[var(--color-accent)] border border-[var(--color-border)] font-semibold">
                    MODEL-GENERATED LEAD
                  </span>
                  <div className="flex items-center gap-1.5">
                    <span
                      className={`text-[9px] font-mono px-2 py-0.5 rounded font-semibold ${
                        lead.status === 'CONFIRMED'
                          ? 'bg-[#EBF5EC] text-[#1E5C2B] border border-[#C4E4C8]'
                          : lead.status === 'DISMISSED'
                          ? 'bg-[#FEECEC] text-[#B91C1C] border border-[#FCA5A5]'
                          : 'bg-[var(--color-surface-subtle)] text-[var(--color-accent)] border border-[var(--color-border)]'
                      }`}
                    >
                      {lead.status}
                    </span>
                    <span className="text-sm font-bold text-[var(--color-accent)] font-mono">
                      {(lead.score * 100).toFixed(0)}%
                    </span>
                  </div>
                </div>

                <div className="flex items-center justify-between text-xs py-2 bg-[var(--color-surface)] p-2.5 rounded-lg border border-[var(--color-border)]">
                  <div>
                    <span className="text-[10px] text-[var(--color-text-muted)] uppercase">{lead.sourceEntityType}</span>
                    <p className="font-bold text-[var(--color-text-primary)]">{lead.sourceEntityName}</p>
                  </div>
                  <div className="text-center px-2">
                    <span className="text-[9px] font-mono text-[var(--color-text-muted)]">GAT LINK</span>
                    <ArrowRight className="w-4 h-4 text-[var(--color-accent)] mx-auto my-0.5" />
                    <span className="text-[8px] font-mono text-[var(--color-text-secondary)]">{lead.suggestedRelationshipType}</span>
                  </div>
                  <div className="text-right">
                    <span className="text-[10px] text-[var(--color-text-muted)] uppercase">{lead.targetEntityType}</span>
                    <p className="font-bold text-[var(--color-text-primary)]">{lead.targetEntityName}</p>
                  </div>
                </div>

                <p className="text-[11px] text-[var(--color-text-secondary)]">
                  Signals: Latent Cosine {(lead.signals.cosineSimilarity).toFixed(2)} • {lead.signals.sharedNeighborsCount} shared neighbors
                </p>

                <div className="flex items-center justify-between pt-2 border-t border-[var(--color-border)] text-xs">
                  <button
                    onClick={() => setSelectedLeadForExplanation(lead)}
                    className="text-[var(--color-accent)] hover:underline font-medium flex items-center gap-1"
                  >
                    <Info className="w-3.5 h-3.5" /> View Explanation
                  </button>

                  {lead.status === 'PENDING' && (
                    <div className="flex items-center gap-2">
                      <button
                        onClick={() => {
                          setSelectedLeadForReview(lead);
                          setReviewAction('CONFIRMED');
                          setReviewRelType(lead.suggestedRelationshipType || 'ASSOCIATE_OF');
                        }}
                        className="px-2.5 py-1 rounded bg-[#EBF5EC] text-[#1E5C2B] border border-[#C4E4C8] hover:bg-[#DDF0DE] font-semibold text-[11px] transition-colors"
                      >
                        Confirm Lead
                      </button>
                      <button
                        onClick={() => {
                          setSelectedLeadForReview(lead);
                          setReviewAction('DISMISSED');
                        }}
                        className="px-2.5 py-1 rounded bg-[#FEECEC] text-[#B91C1C] border border-[#FCA5A5] hover:bg-[#FCD8D8] font-semibold text-[11px] transition-colors"
                      >
                        Dismiss
                      </button>
                    </div>
                  )}
                </div>
              </div>
            ))}

            {leads.length === 0 && (
              <div className="col-span-2 py-12 text-center text-xs text-[var(--color-text-muted)] bg-[var(--color-surface)] rounded-xl border border-[var(--color-border)]">
                <p>No model leads found matching current filter criteria.</p>
              </div>
            )}
          </div>
        </div>
      )}

      {/* Explanation Modal */}
      {selectedLeadForExplanation && (
        <div className="fixed inset-0 bg-black/40 backdrop-blur-xs flex items-center justify-center p-4 z-50">
          <div className="bg-[var(--color-surface)] rounded-xl border border-[var(--color-border)] max-w-lg w-full p-5 space-y-4 shadow-xl">
            <div className="flex items-center justify-between pb-3 border-b border-[var(--color-border)]">
              <div>
                <h3 className="text-sm font-bold text-[var(--color-text-primary)]">GAT Model Explainability Signal Breakdown</h3>
                <p className="text-[10px] font-mono text-[var(--color-text-muted)]">MODEL: {selectedLeadForExplanation.modelVersion}</p>
              </div>
              <button
                onClick={() => setSelectedLeadForExplanation(null)}
                className="text-[var(--color-text-muted)] hover:text-[var(--color-text-primary)] text-sm font-bold"
              >
                ✕
              </button>
            </div>

            <div className="p-3 rounded-lg bg-[var(--color-surface)] border border-[var(--color-border)] space-y-1">
              <p className="text-xs font-semibold text-[var(--color-text-primary)]">
                {selectedLeadForExplanation.sourceEntityName} ↔ {selectedLeadForExplanation.targetEntityName}
              </p>
              <p className="text-[11px] text-[var(--color-accent)] font-mono font-bold">
                Overall GAT Link Prediction Score: {(selectedLeadForExplanation.score * 100).toFixed(0)}%
              </p>
            </div>

            <div className="space-y-3">
              <p className="text-xs font-semibold text-[var(--color-text-primary)]">Contributing Structural Signals:</p>
              {selectedLeadForExplanation.signals.contributingSignals.map((sig, idx) => (
                <div key={idx} className="space-y-1">
                  <div className="flex items-center justify-between text-[11px]">
                    <span className="font-medium text-[var(--color-text-primary)]">{sig.signalName}</span>
                    <span className="font-mono text-[var(--color-accent)] font-bold">
                      +{sig.contribution.toFixed(3)} (Weight: {(sig.weight * 100).toFixed(0)}%)
                    </span>
                  </div>
                  <div className="w-full bg-[var(--color-border)] rounded-full h-1.5 overflow-hidden">
                    <div
                      className="bg-[var(--color-accent)] h-1.5 rounded-full"
                      style={{ width: `${Math.min(100, (sig.contribution / sig.weight) * 100)}%` }}
                    />
                  </div>
                  <p className="text-[10px] text-[var(--color-text-secondary)]">{sig.description}</p>
                </div>
              ))}
            </div>

            {selectedLeadForExplanation.signals.sharedNeighborNames.length > 0 && (
              <div className="p-2.5 rounded-lg bg-[var(--color-surface)] border border-[var(--color-border)] text-[11px]">
                <span className="text-[var(--color-text-muted)] font-medium">Common Verified Neighbors: </span>
                <span className="text-[var(--color-text-primary)]">
                  {selectedLeadForExplanation.signals.sharedNeighborNames.join(', ')}
                </span>
              </div>
            )}

            <div className="pt-2 border-t border-[var(--color-border)] flex justify-end">
              <button
                onClick={() => setSelectedLeadForExplanation(null)}
                className="px-3 py-1.5 rounded bg-[var(--color-surface-subtle)] text-[var(--color-text-primary)] text-xs font-medium hover:bg-[#EADDCF]"
              >
                Close Explanation
              </button>
            </div>
          </div>
        </div>
      )}

      {/* Review Modal */}
      {selectedLeadForReview && (
        <div className="fixed inset-0 bg-black/40 backdrop-blur-xs flex items-center justify-center p-4 z-50">
          <div className="bg-[var(--color-surface)] rounded-xl border border-[var(--color-border)] max-w-md w-full p-5 space-y-4 shadow-xl">
            <div className="flex items-center justify-between pb-3 border-b border-[var(--color-border)]">
              <h3 className="text-sm font-bold text-[var(--color-text-primary)]">Investigator Lead Review Decision</h3>
              <button
                onClick={() => setSelectedLeadForReview(null)}
                className="text-[var(--color-text-muted)] hover:text-[var(--color-text-primary)] text-sm font-bold"
              >
                ✕
              </button>
            </div>

            <div className="space-y-2 text-xs">
              <p className="text-[var(--color-text-secondary)]">
                Decision for candidate link between <strong className="text-[var(--color-text-primary)]">{selectedLeadForReview.sourceEntityName}</strong> and <strong className="text-[var(--color-text-primary)]">{selectedLeadForReview.targetEntityName}</strong>.
              </p>

              {reviewAction === 'CONFIRMED' && (
                <div>
                  <label className="block text-[11px] font-medium text-[var(--color-text-muted)] mb-1">
                    Confirmed Relationship Type:
                  </label>
                  <select
                    value={reviewRelType}
                    onChange={(e) => setReviewRelType(e.target.value)}
                    className="w-full p-2 text-xs rounded border border-[var(--color-border)] bg-[var(--color-surface)] text-[var(--color-text-primary)]"
                  >
                    <option value="ASSOCIATE_OF">ASSOCIATE_OF</option>
                    <option value="COMMUNICATED_WITH">COMMUNICATED_WITH</option>
                    <option value="OPERATES">OPERATES</option>
                    <option value="MEMBER_OF">MEMBER_OF</option>
                    <option value="LOCATED_AT">LOCATED_AT</option>
                    <option value="TRANSFERRED_MONEY_TO">TRANSFERRED_MONEY_TO</option>
                  </select>
                </div>
              )}

              <div>
                <label className="block text-[11px] font-medium text-[var(--color-text-muted)] mb-1">
                  Investigator Review Notes:
                </label>
                <textarea
                  value={reviewNotes}
                  onChange={(e) => setReviewNotes(e.target.value)}
                  placeholder="State evidence provenance, corroborating call records, or rationale..."
                  className="w-full p-2.5 text-xs rounded border border-[var(--color-border)] bg-[var(--color-surface)] text-[var(--color-text-primary)] h-20 resize-none"
                />
              </div>

              <div className="p-2.5 rounded bg-[var(--color-surface)] border border-[var(--color-border)] text-[10px] text-[var(--color-text-muted)]">
                {reviewAction === 'CONFIRMED' ? (
                  <span>
                    Confirming promotes this candidate into a documented verified relationship with provenance citation.
                  </span>
                ) : (
                  <span>
                    Dismissing marks the lead as discarded. Zero knowledge graph mutations will occur.
                  </span>
                )}
              </div>
            </div>

            <div className="pt-2 border-t border-[var(--color-border)] flex justify-end gap-2 text-xs">
              <button
                onClick={() => setSelectedLeadForReview(null)}
                className="px-3 py-1.5 rounded bg-[var(--color-surface-subtle)] text-[var(--color-text-primary)] font-medium hover:bg-[#EADDCF]"
              >
                Cancel
              </button>
              <button
                onClick={handleSubmitReview}
                disabled={submittingReview}
                className={`px-4 py-1.5 rounded font-semibold text-white transition-colors ${
                  reviewAction === 'CONFIRMED'
                    ? 'bg-[#1E5C2B] hover:bg-[#164620]'
                    : 'bg-[#B91C1C] hover:bg-[#991B1B]'
                }`}
              >
                {submittingReview ? 'Submitting...' : `Confirm ${reviewAction}`}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
