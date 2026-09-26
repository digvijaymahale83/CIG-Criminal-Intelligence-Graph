import React, { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  GitMerge,
  CheckCircle2,
  XCircle,
  AlertTriangle,
  Search,
  Filter,
  ArrowRight,
  ExternalLink,
  Clock,
  Sparkles,
  Layers,
  FileText,
  ShieldAlert,
  HelpCircle,
  RotateCw,
  Eye,
  Check,
  X,
  Scale
} from 'lucide-react';
import {
  entityResolutionService,
  EntityMatchCandidate,
  CandidateComparison
} from '../../services/entity-resolution/entityResolution.service';
import { useActiveInvestigation } from '../../hooks/useActiveInvestigation';

export const EntityResolutionPage: React.FC = () => {
  const navigate = useNavigate();
  const { activeCaseId, activeCaseNumber } = useActiveInvestigation();

  const [candidates, setCandidates] = useState<EntityMatchCandidate[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  // Filters
  const [statusFilter, setStatusFilter] = useState<string>('PENDING');
  const [typeFilter, setTypeFilter] = useState<string>('ALL');
  const [minScore, setMinScore] = useState<number>(0.6);
  const [searchQuery, setSearchQuery] = useState('');

  // Batch runner state
  const [analyzing, setAnalyzing] = useState(false);
  const [analysisResult, setAnalysisResult] = useState<string | null>(null);

  // Comparison Modal state
  const [selectedCandidateId, setSelectedCandidateId] = useState<string | null>(null);
  const [comparisonData, setComparisonData] = useState<CandidateComparison | null>(null);
  const [loadingComparison, setLoadingComparison] = useState(false);
  const [reviewNotes, setReviewNotes] = useState('');
  const [submittingReview, setSubmittingReview] = useState(false);

  const fetchCandidates = async () => {
    try {
      setLoading(true);
      setError(null);
      const data = await entityResolutionService.getCandidates({
        caseId: activeCaseId || undefined,
        status: statusFilter === 'ALL' ? undefined : statusFilter,
        entityType: typeFilter === 'ALL' ? undefined : typeFilter,
        minimumScore: minScore,
      });
      setCandidates(data);
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'Failed to load resolution candidates.');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchCandidates();
  }, [activeCaseId, statusFilter, typeFilter, minScore]);

  const handleRunAnalysis = async () => {
    try {
      setAnalyzing(true);
      setAnalysisResult(null);
      const res = await entityResolutionService.runBatchResolution({
        caseId: activeCaseId || undefined,
        minimumScore: minScore,
      });
      setAnalysisResult(
        `Analysis completed: ${res.entitiesCompared} entities evaluated, ${res.potentialMatchesFound} match(es) identified, ${res.candidatesCreated} candidate(s) created.`
      );
      await fetchCandidates();
    } catch (err: any) {
      setError(err.response?.data?.message || err.message || 'Analysis failed.');
    } finally {
      setAnalyzing(false);
    }
  };

  const handleOpenComparison = async (candidateId: string) => {
    setSelectedCandidateId(candidateId);
    setLoadingComparison(true);
    setReviewNotes('');
    try {
      const comp = await entityResolutionService.getCandidateComparison(candidateId);
      setComparisonData(comp);
      if (comp.reviewNotes) {
        setReviewNotes(comp.reviewNotes);
      }
    } catch (err: any) {
      alert('Failed to load candidate comparison: ' + (err.message || 'Unknown error'));
    } finally {
      setLoadingComparison(false);
    }
  };

  const handleDecision = async (decision: 'APPROVED' | 'REJECTED') => {
    if (!selectedCandidateId) return;
    try {
      setSubmittingReview(true);
      if (decision === 'APPROVED') {
        await entityResolutionService.approveCandidate(selectedCandidateId, reviewNotes);
      } else {
        await entityResolutionService.rejectCandidate(selectedCandidateId, reviewNotes);
      }
      setSelectedCandidateId(null);
      setComparisonData(null);
      await fetchCandidates();
    } catch (err: any) {
      alert(`Decision failed: ${err.message || 'Unknown error'}`);
    } finally {
      setSubmittingReview(false);
    }
  };

  // KPI Calculations
  const pendingCount = candidates.filter((c) => c.matchStatus === 'PENDING').length;
  const approvedCount = candidates.filter((c) => c.matchStatus === 'APPROVED').length;
  const highConfidenceCount = candidates.filter((c) => c.matchScore >= 0.85).length;

  const filteredCandidates = candidates.filter((c) => {
    if (!searchQuery) return true;
    const q = searchQuery.toLowerCase();
    return (
      c.sourceEntity?.canonicalName?.toLowerCase().includes(q) ||
      c.targetEntity?.canonicalName?.toLowerCase().includes(q) ||
      c.sourceCaseNumber?.toLowerCase().includes(q) ||
      c.targetCaseNumber?.toLowerCase().includes(q)
    );
  });

  return (
    <div className="space-y-6">
      {/* Header Banner */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 border-b border-[var(--color-border-subtle)] pb-5">
        <div>
          <div className="flex items-center gap-2">
            <span className="px-2 py-0.5 text-[10px] font-mono tracking-wider uppercase bg-[#1f6feb]/20 text-[#58a6ff] border border-[#1f6feb]/30 rounded">
              Phase 4 Intelligence
            </span>
            <span className="text-xs text-[var(--color-text-secondary)]">Synthetic Research Environment</span>
          </div>
          <h1 className="text-2xl font-bold text-[var(--color-text-primary)] tracking-tight mt-1 flex items-center gap-2">
            <GitMerge className="w-6 h-6 text-[#1f6feb]" />
            Entity Resolution & Cross-Case Intelligence
          </h1>
          <p className="text-xs text-[var(--color-text-secondary)] mt-1 max-w-3xl">
            Detects when records across disparate cases refer to the same real-world entity via deterministic
            multi-signal analysis (shared cellular numbers, vehicle plates, banking accounts, and aliases). All potential matches require human investigator review.
          </p>
        </div>

        <div className="flex items-center gap-2">
          <button
            onClick={fetchCandidates}
            className="flex items-center gap-1.5 px-3 py-1.5 text-xs bg-[var(--color-surface-subtle)] hover:bg-[var(--color-surface-subtle)] text-[var(--color-text-primary)] border border-[var(--color-border)] rounded transition-colors"
          >
            <RotateCw className={`w-3.5 h-3.5 ${loading ? 'animate-spin' : ''}`} />
            Refresh
          </button>
          <button
            onClick={handleRunAnalysis}
            disabled={analyzing}
            className="flex items-center gap-1.5 px-4 py-1.5 text-xs font-medium bg-[#1f6feb] hover:bg-[#388bfd] text-white rounded transition-colors shadow-sm disabled:opacity-50"
          >
            <Sparkles className={`w-3.5 h-3.5 ${analyzing ? 'animate-spin' : ''}`} />
            {analyzing ? 'Running Analysis...' : 'Run Cross-Case Analysis'}
          </button>
        </div>
      </div>

      {/* Analysis Output Alert */}
      {analysisResult && (
        <div className="p-3 bg-[#1f6feb]/10 border border-[#1f6feb]/30 rounded text-xs text-[#58a6ff] flex items-center justify-between">
          <span>{analysisResult}</span>
          <button onClick={() => setAnalysisResult(null)} className="text-[var(--color-text-secondary)] hover:text-white">
            <X className="w-4 h-4" />
          </button>
        </div>
      )}

      {/* Metrics Row */}
      <div className="grid grid-cols-2 sm:grid-cols-4 gap-4">
        <div className="bg-[var(--color-surface)] border border-[var(--color-border)] p-4 rounded-lg">
          <div className="flex items-center justify-between">
            <span className="text-xs text-[var(--color-text-secondary)]">Pending Review</span>
            <Clock className="w-4 h-4 text-[#d29922]" />
          </div>
          <div className="text-2xl font-bold text-[var(--color-text-primary)] mt-1">{pendingCount}</div>
          <div className="text-[11px] text-[var(--color-text-secondary)] mt-0.5">Awaiting investigator validation</div>
        </div>

        <div className="bg-[var(--color-surface)] border border-[var(--color-border)] p-4 rounded-lg">
          <div className="flex items-center justify-between">
            <span className="text-xs text-[var(--color-text-secondary)]">Verified Connections</span>
            <CheckCircle2 className="w-4 h-4 text-[#3fb950]" />
          </div>
          <div className="text-2xl font-bold text-[var(--color-text-primary)] mt-1">{approvedCount}</div>
          <div className="text-[11px] text-[var(--color-text-secondary)] mt-0.5">Promoted to cross-case graph</div>
        </div>

        <div className="bg-[var(--color-surface)] border border-[var(--color-border)] p-4 rounded-lg">
          <div className="flex items-center justify-between">
            <span className="text-xs text-[var(--color-text-secondary)]">High Confidence (&gt;=85%)</span>
            <Sparkles className="w-4 h-4 text-[#58a6ff]" />
          </div>
          <div className="text-2xl font-bold text-[var(--color-text-primary)] mt-1">{highConfidenceCount}</div>
          <div className="text-[11px] text-[var(--color-text-secondary)] mt-0.5">Multi-signal corroboration</div>
        </div>

        <div className="bg-[var(--color-surface)] border border-[var(--color-border)] p-4 rounded-lg">
          <div className="flex items-center justify-between">
            <span className="text-xs text-[var(--color-text-secondary)]">Total Candidates</span>
            <Layers className="w-4 h-4 text-[#bc8cff]" />
          </div>
          <div className="text-2xl font-bold text-[var(--color-text-primary)] mt-1">{candidates.length}</div>
          <div className="text-[11px] text-[var(--color-text-secondary)] mt-0.5">Evaluated across cases</div>
        </div>
      </div>

      {/* Filter and Search Bar */}
      <div className="bg-[var(--color-surface)] border border-[var(--color-border)] p-3 rounded-lg flex flex-wrap items-center justify-between gap-3">
        {/* Status Tabs */}
        <div className="flex items-center gap-1 bg-[var(--color-bg-canvas)] p-1 rounded border border-[var(--color-border-subtle)]">
          {(['PENDING', 'APPROVED', 'REJECTED', 'ALL'] as const).map((st) => (
            <button
              key={st}
              onClick={() => setStatusFilter(st)}
              className={`px-3 py-1 text-xs rounded transition-colors ${
                statusFilter === st
                  ? 'bg-[#1f6feb] text-white font-medium'
                  : 'text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-surface)]'
              }`}
            >
              {st === 'ALL' ? 'All' : st.charAt(0) + st.slice(1).toLowerCase()}
            </button>
          ))}
        </div>

        {/* Type Filter */}
        <div className="flex items-center gap-2">
          <span className="text-xs text-[var(--color-text-secondary)]">Entity Type:</span>
          <select
            value={typeFilter}
            onChange={(e) => setTypeFilter(e.target.value)}
            className="bg-[var(--color-bg-canvas)] border border-[var(--color-border)] text-[var(--color-text-primary)] text-xs px-2.5 py-1 rounded focus:outline-none focus:border-[#1f6feb]"
          >
            <option value="ALL">All Types</option>
            <option value="PERSON">PERSON</option>
            <option value="PHONE">PHONE</option>
            <option value="VEHICLE">VEHICLE</option>
            <option value="ACCOUNT">ACCOUNT</option>
            <option value="ORGANIZATION">ORGANIZATION</option>
          </select>
        </div>

        {/* Min Score Slider */}
        <div className="flex items-center gap-2">
          <span className="text-xs text-[var(--color-text-secondary)]">Min Score: {Math.round(minScore * 100)}%</span>
          <input
            type="range"
            min="0.4"
            max="0.95"
            step="0.05"
            value={minScore}
            onChange={(e) => setMinScore(parseFloat(e.target.value))}
            className="w-24 accent-[#1f6feb] cursor-pointer"
          />
        </div>

        {/* Search Input */}
        <div className="relative min-w-[200px] flex-1 max-w-xs">
          <Search className="w-3.5 h-3.5 absolute left-2.5 top-2.5 text-[var(--color-text-secondary)]" />
          <input
            type="text"
            placeholder="Search entity or case..."
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            className="w-full bg-[var(--color-bg-canvas)] border border-[var(--color-border)] text-[var(--color-text-primary)] text-xs pl-8 pr-3 py-1 rounded focus:outline-none focus:border-[#1f6feb]"
          />
        </div>
      </div>

      {/* Candidates List / Table */}
      <div className="bg-[var(--color-surface)] border border-[var(--color-border)] rounded-lg overflow-hidden">
        <div className="p-3 border-b border-[var(--color-border)] bg-[var(--color-surface)]/50 flex items-center justify-between">
          <span className="text-xs font-semibold text-[var(--color-text-primary)]">Candidate Entity Matches ({filteredCandidates.length})</span>
          <span className="text-[11px] text-[var(--color-text-secondary)]">Explainable Multi-Signal Resolution</span>
        </div>

        {loading ? (
          <div className="p-12 text-center text-xs text-[var(--color-text-secondary)] flex flex-col items-center gap-2">
            <RotateCw className="w-5 h-5 animate-spin text-[#1f6feb]" />
            Loading resolution candidates...
          </div>
        ) : filteredCandidates.length === 0 ? (
          <div className="p-12 text-center text-xs text-[var(--color-text-secondary)]">
            No entity resolution candidates matching the selected criteria.
          </div>
        ) : (
          <div className="divide-y divide-[#21262d]">
            {filteredCandidates.map((cand) => {
              const scorePercent = Math.round(cand.matchScore * 100);
              const scoreColor =
                cand.matchScore >= 0.85
                  ? 'bg-emerald-500/20 text-emerald-400 border-emerald-500/30'
                  : cand.matchScore >= 0.70
                  ? 'bg-blue-500/20 text-blue-400 border-blue-500/30'
                  : 'bg-amber-500/20 text-amber-400 border-amber-500/30';

              return (
                <div key={cand.id} className="p-4 hover:bg-[var(--color-bg-elevated)] transition-colors flex flex-col md:flex-row md:items-center justify-between gap-4">
                  {/* Entities & Cases */}
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center gap-2 mb-1.5">
                      <span className={`px-2 py-0.5 text-[10px] font-mono border rounded ${scoreColor}`}>
                        {scorePercent}% Match
                      </span>
                      <span className="text-[10px] font-mono uppercase px-1.5 py-0.2 text-[var(--color-text-secondary)] bg-[var(--color-surface-subtle)] rounded">
                        {cand.entityType}
                      </span>
                      <span
                        className={`text-[10px] font-medium px-2 py-0.5 rounded ${
                          cand.matchStatus === 'APPROVED'
                            ? 'bg-green-900/30 text-green-400 border border-green-800/40'
                            : cand.matchStatus === 'REJECTED'
                            ? 'bg-red-900/30 text-red-400 border border-red-800/40'
                            : 'bg-yellow-900/30 text-yellow-400 border border-yellow-800/40'
                        }`}
                      >
                        {cand.matchStatus}
                      </span>
                      <span className="text-[10px] text-[var(--color-text-secondary)] font-mono">ID: {cand.id}</span>
                    </div>

                    {/* Comparison Cards inline */}
                    <div className="grid grid-cols-1 sm:grid-cols-2 gap-3 mt-2 bg-[var(--color-bg-canvas)] p-2.5 rounded border border-[var(--color-border-subtle)]">
                      {/* Side A */}
                      <div className="border-r border-[var(--color-border-subtle)] pr-2">
                        <div className="text-[10px] text-[#58a6ff] font-mono flex items-center gap-1">
                          <span>Case A:</span>
                          <span className="font-semibold">{cand.sourceCaseNumber}</span>
                        </div>
                        <div className="text-sm font-semibold text-[var(--color-text-primary)] truncate mt-0.5">
                          {cand.sourceEntity?.canonicalName || cand.sourceEntityId}
                        </div>
                        <div className="text-[11px] text-[var(--color-text-secondary)] flex flex-wrap gap-2 mt-1">
                          {cand.sourceEntity?.phoneNumber && <span>📞 {cand.sourceEntity.phoneNumber}</span>}
                          {cand.sourceEntity?.vehicleNumber && <span>🚗 {cand.sourceEntity.vehicleNumber}</span>}
                          {cand.sourceEntity?.location && <span>📍 {cand.sourceEntity.location}</span>}
                        </div>
                      </div>

                      {/* Side B */}
                      <div className="pl-1">
                        <div className="text-[10px] text-[#58a6ff] font-mono flex items-center gap-1">
                          <span>Case B:</span>
                          <span className="font-semibold">{cand.targetCaseNumber}</span>
                        </div>
                        <div className="text-sm font-semibold text-[var(--color-text-primary)] truncate mt-0.5">
                          {cand.targetEntity?.canonicalName || cand.targetEntityId}
                        </div>
                        <div className="text-[11px] text-[var(--color-text-secondary)] flex flex-wrap gap-2 mt-1">
                          {cand.targetEntity?.phoneNumber && <span>📞 {cand.targetEntity.phoneNumber}</span>}
                          {cand.targetEntity?.vehicleNumber && <span>🚗 {cand.targetEntity.vehicleNumber}</span>}
                          {cand.targetEntity?.location && <span>📍 {cand.targetEntity.location}</span>}
                        </div>
                      </div>
                    </div>

                    {/* Signals checklist preview */}
                    <div className="flex flex-wrap items-center gap-1.5 mt-2">
                      <span className="text-[11px] text-[var(--color-text-secondary)]">Signals:</span>
                      {cand.factors.map((f, i) => (
                        <span
                          key={i}
                          className="text-[10px] px-2 py-0.5 bg-[var(--color-surface-subtle)] text-[var(--color-text-primary)] rounded border border-[var(--color-border)] flex items-center gap-1"
                        >
                          <Check className="w-2.5 h-2.5 text-emerald-400" />
                          {f.type.replace('_', ' ')}
                        </span>
                      ))}
                    </div>
                  </div>

                  {/* Actions Column */}
                  <div className="flex flex-row md:flex-col items-end justify-center gap-2 shrink-0">
                    <button
                      onClick={() => handleOpenComparison(cand.id)}
                      className="px-3 py-1.5 text-xs bg-[var(--color-surface-subtle)] hover:bg-[var(--color-surface-subtle)] text-[var(--color-text-primary)] border border-[var(--color-border)] rounded flex items-center gap-1.5 transition-colors"
                    >
                      <Scale className="w-3.5 h-3.5 text-[#58a6ff]" />
                      Compare Entities
                    </button>
                    {cand.matchStatus === 'PENDING' && (
                      <div className="flex items-center gap-1.5">
                        <button
                          onClick={() => {
                            setSelectedCandidateId(cand.id);
                            handleDecision('APPROVED');
                          }}
                          className="px-2.5 py-1 text-xs bg-emerald-700/80 hover:bg-emerald-600 text-white rounded flex items-center gap-1 transition-colors"
                        >
                          <Check className="w-3 h-3" /> Approve
                        </button>
                        <button
                          onClick={() => {
                            setSelectedCandidateId(cand.id);
                            handleDecision('REJECTED');
                          }}
                          className="px-2.5 py-1 text-xs bg-red-800/80 hover:bg-red-700 text-white rounded flex items-center gap-1 transition-colors"
                        >
                          <X className="w-3 h-3" /> Reject
                        </button>
                      </div>
                    )}
                  </div>
                </div>
              );
            })}
          </div>
        )}
      </div>

      {/* Side-by-Side Comparison Modal */}
      {selectedCandidateId && comparisonData && (
        <div className="fixed inset-0 z-50 bg-black/80 backdrop-blur-sm flex items-center justify-center p-4 overflow-y-auto">
          <div className="bg-[var(--color-surface)] border border-[var(--color-border)] rounded-xl max-w-4xl w-full max-h-[90vh] flex flex-col shadow-2xl overflow-hidden">
            {/* Modal Header */}
            <div className="p-4 border-b border-[var(--color-border)] flex items-center justify-between bg-[var(--color-bg-elevated)]">
              <div>
                <div className="flex items-center gap-2">
                  <span className="text-xs font-mono text-[#58a6ff]">Candidate #{comparisonData.candidateId}</span>
                  <span className="text-[10px] font-mono px-2 py-0.5 bg-emerald-500/20 text-emerald-400 border border-emerald-500/30 rounded">
                    {Math.round(comparisonData.matchScore * 100)}% Confidence
                  </span>
                  <span className="text-[10px] uppercase font-mono px-1.5 py-0.5 bg-[var(--color-surface-subtle)] text-[var(--color-text-secondary)] rounded">
                    {comparisonData.entityType}
                  </span>
                </div>
                <h3 className="text-lg font-bold text-[var(--color-text-primary)] mt-1">Cross-Case Entity Comparison</h3>
              </div>
              <button
                onClick={() => {
                  setSelectedCandidateId(null);
                  setComparisonData(null);
                }}
                className="text-[var(--color-text-secondary)] hover:text-white p-1 rounded"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            {/* Modal Body */}
            <div className="p-5 overflow-y-auto space-y-5 flex-1">
              {/* Dual Column Side-by-Side View */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {/* Side A */}
                <div className="bg-[var(--color-bg-canvas)] border border-[var(--color-border)] rounded-lg p-4 space-y-3">
                  <div className="border-b border-[var(--color-border-subtle)] pb-2">
                    <span className="text-[10px] font-mono text-[#58a6ff] uppercase tracking-wide">Case A Origin</span>
                    <h4 className="text-sm font-bold text-[var(--color-text-primary)] mt-0.5">{comparisonData.sideA.caseNumber}</h4>
                    <p className="text-[11px] text-[var(--color-text-secondary)] truncate">{comparisonData.sideA.caseTitle}</p>
                  </div>

                  <div>
                    <span className="text-[10px] text-[var(--color-text-secondary)] uppercase">Canonical Name</span>
                    <div className="text-sm font-semibold text-[#58a6ff]">{comparisonData.sideA.canonicalName}</div>
                    <div className="text-[11px] font-mono text-[var(--color-text-secondary)]">Normalized: {comparisonData.sideA.normalizedValue}</div>
                  </div>

                  {comparisonData.sideA.aliases.length > 0 && (
                    <div>
                      <span className="text-[10px] text-[var(--color-text-secondary)] uppercase">Documented Aliases</span>
                      <div className="flex flex-wrap gap-1 mt-1">
                        {comparisonData.sideA.aliases.map((al, i) => (
                          <span key={i} className="text-[10px] bg-[var(--color-surface-subtle)] text-[var(--color-text-primary)] px-2 py-0.5 rounded">
                            {al}
                          </span>
                        ))}
                      </div>
                    </div>
                  )}

                  <div className="grid grid-cols-2 gap-2 text-xs">
                    <div>
                      <span className="text-[10px] text-[var(--color-text-secondary)]">Phone Number</span>
                      <div className="font-mono text-[var(--color-text-primary)]">{comparisonData.sideA.phoneNumber || '—'}</div>
                    </div>
                    <div>
                      <span className="text-[10px] text-[var(--color-text-secondary)]">Vehicle Reg</span>
                      <div className="font-mono text-[var(--color-text-primary)]">{comparisonData.sideA.vehicleNumber || '—'}</div>
                    </div>
                    <div>
                      <span className="text-[10px] text-[var(--color-text-secondary)]">Bank Account</span>
                      <div className="font-mono text-[var(--color-text-primary)]">{comparisonData.sideA.accountNumber || '—'}</div>
                    </div>
                    <div>
                      <span className="text-[10px] text-[var(--color-text-secondary)]">Location / District</span>
                      <div className="text-[var(--color-text-primary)]">{comparisonData.sideA.district || comparisonData.sideA.location || '—'}</div>
                    </div>
                  </div>

                  {/* Supporting Evidence Citations */}
                  <div className="border-t border-[var(--color-border-subtle)] pt-2">
                    <span className="text-[10px] text-[var(--color-text-secondary)] uppercase">Supporting Evidence ({comparisonData.sideA.supportingEvidence.length})</span>
                    <div className="space-y-1.5 mt-1">
                      {comparisonData.sideA.supportingEvidence.map((ev, i) => (
                        <div key={i} className="bg-[var(--color-surface)] p-2 rounded border border-[var(--color-border-subtle)] text-[11px]">
                          <div className="flex items-center justify-between text-[#58a6ff]">
                            <span className="font-mono truncate">{ev.fileName}</span>
                            <button
                              onClick={() => navigate(`/evidence/${ev.evidenceId}`)}
                              className="text-[10px] text-[var(--color-text-secondary)] hover:text-white flex items-center gap-0.5"
                            >
                              Open <ExternalLink className="w-2.5 h-2.5" />
                            </button>
                          </div>
                          <div className="text-[10px] text-[var(--color-text-secondary)] mt-0.5">Location: {ev.sourceLocation || 'Field record'}</div>
                          {ev.extractedQuote && <div className="italic text-[var(--color-text-primary)] mt-1 bg-[var(--color-bg-canvas)] p-1 rounded">"{ev.extractedQuote}"</div>}
                        </div>
                      ))}
                    </div>
                  </div>
                </div>

                {/* Side B */}
                <div className="bg-[var(--color-bg-canvas)] border border-[var(--color-border)] rounded-lg p-4 space-y-3">
                  <div className="border-b border-[var(--color-border-subtle)] pb-2">
                    <span className="text-[10px] font-mono text-[#58a6ff] uppercase tracking-wide">Case B Target</span>
                    <h4 className="text-sm font-bold text-[var(--color-text-primary)] mt-0.5">{comparisonData.sideB.caseNumber}</h4>
                    <p className="text-[11px] text-[var(--color-text-secondary)] truncate">{comparisonData.sideB.caseTitle}</p>
                  </div>

                  <div>
                    <span className="text-[10px] text-[var(--color-text-secondary)] uppercase">Canonical Name</span>
                    <div className="text-sm font-semibold text-[#58a6ff]">{comparisonData.sideB.canonicalName}</div>
                    <div className="text-[11px] font-mono text-[var(--color-text-secondary)]">Normalized: {comparisonData.sideB.normalizedValue}</div>
                  </div>

                  {comparisonData.sideB.aliases.length > 0 && (
                    <div>
                      <span className="text-[10px] text-[var(--color-text-secondary)] uppercase">Documented Aliases</span>
                      <div className="flex flex-wrap gap-1 mt-1">
                        {comparisonData.sideB.aliases.map((al, i) => (
                          <span key={i} className="text-[10px] bg-[var(--color-surface-subtle)] text-[var(--color-text-primary)] px-2 py-0.5 rounded">
                            {al}
                          </span>
                        ))}
                      </div>
                    </div>
                  )}

                  <div className="grid grid-cols-2 gap-2 text-xs">
                    <div>
                      <span className="text-[10px] text-[var(--color-text-secondary)]">Phone Number</span>
                      <div className="font-mono text-[var(--color-text-primary)]">{comparisonData.sideB.phoneNumber || '—'}</div>
                    </div>
                    <div>
                      <span className="text-[10px] text-[var(--color-text-secondary)]">Vehicle Reg</span>
                      <div className="font-mono text-[var(--color-text-primary)]">{comparisonData.sideB.vehicleNumber || '—'}</div>
                    </div>
                    <div>
                      <span className="text-[10px] text-[var(--color-text-secondary)]">Bank Account</span>
                      <div className="font-mono text-[var(--color-text-primary)]">{comparisonData.sideB.accountNumber || '—'}</div>
                    </div>
                    <div>
                      <span className="text-[10px] text-[var(--color-text-secondary)]">Location / District</span>
                      <div className="text-[var(--color-text-primary)]">{comparisonData.sideB.district || comparisonData.sideB.location || '—'}</div>
                    </div>
                  </div>

                  {/* Supporting Evidence Citations */}
                  <div className="border-t border-[var(--color-border-subtle)] pt-2">
                    <span className="text-[10px] text-[var(--color-text-secondary)] uppercase">Supporting Evidence ({comparisonData.sideB.supportingEvidence.length})</span>
                    <div className="space-y-1.5 mt-1">
                      {comparisonData.sideB.supportingEvidence.map((ev, i) => (
                        <div key={i} className="bg-[var(--color-surface)] p-2 rounded border border-[var(--color-border-subtle)] text-[11px]">
                          <div className="flex items-center justify-between text-[#58a6ff]">
                            <span className="font-mono truncate">{ev.fileName}</span>
                            <button
                              onClick={() => navigate(`/evidence/${ev.evidenceId}`)}
                              className="text-[10px] text-[var(--color-text-secondary)] hover:text-white flex items-center gap-0.5"
                            >
                              Open <ExternalLink className="w-2.5 h-2.5" />
                            </button>
                          </div>
                          <div className="text-[10px] text-[var(--color-text-secondary)] mt-0.5">Location: {ev.sourceLocation || 'Field record'}</div>
                          {ev.extractedQuote && <div className="italic text-[var(--color-text-primary)] mt-1 bg-[var(--color-bg-canvas)] p-1 rounded">"{ev.extractedQuote}"</div>}
                        </div>
                      ))}
                    </div>
                  </div>
                </div>
              </div>

              {/* Match Factors Explanation */}
              <div className="bg-[var(--color-bg-canvas)] border border-[var(--color-border)] rounded-lg p-4 space-y-2">
                <span className="text-xs font-semibold text-[var(--color-text-primary)]">Explainable Matching Signals</span>
                <div className="space-y-2 mt-2">
                  {comparisonData.factors.map((f, i) => (
                    <div key={i} className="flex items-center justify-between p-2 bg-[var(--color-surface)] rounded border border-[var(--color-border-subtle)] text-xs">
                      <div>
                        <div className="font-semibold text-[var(--color-text-primary)]">{f.type.replace('_', ' ')}</div>
                        <div className="text-[11px] text-[var(--color-text-secondary)]">{f.description}</div>
                      </div>
                      <div className="text-right">
                        <span className="text-[11px] font-mono text-[#58a6ff]">Weight: {Math.round(f.weight * 100)}%</span>
                        <div className="text-[10px] text-emerald-400">Score: {Math.round(f.score * 100)}%</div>
                      </div>
                    </div>
                  ))}
                </div>
              </div>

              {/* Decision Section */}
              <div className="border-t border-[var(--color-border)] pt-4 space-y-3">
                <label className="block text-xs font-semibold text-[var(--color-text-primary)]">Investigator Decision Notes</label>
                <textarea
                  rows={2}
                  value={reviewNotes}
                  onChange={(e) => setReviewNotes(e.target.value)}
                  placeholder="Record rationale for decision (e.g., verified identical vehicle and burner SIM logged in both investigation files)..."
                  className="w-full bg-[var(--color-bg-canvas)] border border-[var(--color-border)] text-[var(--color-text-primary)] text-xs p-2.5 rounded focus:outline-none focus:border-[#1f6feb]"
                />

                <div className="flex items-center justify-end gap-2 pt-2">
                  <button
                    onClick={() => {
                      setSelectedCandidateId(null);
                      setComparisonData(null);
                    }}
                    className="px-4 py-2 text-xs bg-[var(--color-surface-subtle)] hover:bg-[var(--color-surface-subtle)] text-[var(--color-text-primary)] rounded transition-colors"
                  >
                    Cancel
                  </button>
                  <button
                    onClick={() => handleDecision('REJECTED')}
                    disabled={submittingReview}
                    className="px-4 py-2 text-xs font-medium bg-red-800 hover:bg-red-700 text-white rounded transition-colors disabled:opacity-50 flex items-center gap-1"
                  >
                    <X className="w-3.5 h-3.5" /> Reject Match
                  </button>
                  <button
                    onClick={() => handleDecision('APPROVED')}
                    disabled={submittingReview}
                    className="px-5 py-2 text-xs font-medium bg-emerald-700 hover:bg-emerald-600 text-white rounded transition-colors disabled:opacity-50 flex items-center gap-1"
                  >
                    <Check className="w-3.5 h-3.5" /> Approve &amp; Link Graph
                  </button>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
