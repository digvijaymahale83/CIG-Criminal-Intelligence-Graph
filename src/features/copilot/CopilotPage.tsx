import React, { useState, useRef, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Bot,
  Send,
  ShieldCheck,
  ShieldAlert,
  User,
  RefreshCw,
  Trash2,
  FileText,
  Users2,
  Network,
  Clock,
  MapPin,
  AlertTriangle,
  GitFork,
  ExternalLink,
  ChevronDown,
  ChevronUp,
  Sparkles,
  Sliders,
  CheckCircle2,
  XCircle,
  Hash,
  ArrowRight
} from 'lucide-react';
import { useActiveInvestigation } from '../../hooks/useActiveInvestigation';
import { useAuth } from '../../hooks/useAuth';
import {
  copilotService,
  CopilotQueryRequest,
  CopilotResponse,
  EvidenceCitation,
  EntityCitation,
  RelationshipCitation,
  TimelineCitation,
  LocationCitation,
  AlertCitation,
  ModelSignal
} from '../../services/copilot/copilot.service';
import { ResponsibleAiNotice } from '../../components/common/ResponsibleAiNotice';

interface ChatTurn {
  id: string;
  query: string;
  response?: CopilotResponse;
  isLoading?: boolean;
  error?: string;
  timestamp: Date;
}

export const CopilotPage: React.FC = () => {
  const navigate = useNavigate();
  const { activeCaseId, activeCaseNumber } = useActiveInvestigation();
  const { user } = useAuth();

  const [chatTurns, setChatTurns] = useState<ChatTurn[]>([]);
  const [conversationId, setConversationId] = useState<string | undefined>(undefined);
  const [inputQuery, setInputQuery] = useState('');
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [suggestedQuestions, setSuggestedQuestions] = useState<string[]>([]);
  const [loadingSuggestions, setLoadingSuggestions] = useState(false);

  // Retrieval Filters
  const [showFilters, setShowFilters] = useState(false);
  const [includeCrossCase, setIncludeCrossCase] = useState(true);
  const [includeAlerts, setIncludeAlerts] = useState(true);
  const [includeTimeline, setIncludeTimeline] = useState(true);
  const [includeLocations, setIncludeLocations] = useState(true);
  const [includeGraph, setIncludeGraph] = useState(true);
  const [includeEvidence, setIncludeEvidence] = useState(true);

  // Citation Inspection Modal
  const [selectedCitation, setSelectedCitation] = useState<{
    type: 'evidence' | 'entity' | 'relationship' | 'timeline' | 'location' | 'alert' | 'model';
    data: any;
  } | null>(null);

  const bottomRef = useRef<HTMLDivElement>(null);

  // Auto-scroll on new message
  useEffect(() => {
    bottomRef.current?.scrollIntoView({ behavior: 'smooth' });
  }, [chatTurns]);

  // Load Suggested Questions for Active Case
  const loadSuggestedQuestions = useCallback(async () => {
    if (!activeCaseId) {
      setSuggestedQuestions([
        'Summarize the primary entities and evidence in this case',
        'Are there any high-priority investigative anomaly alerts?',
        'What verified connections exist in the investigation graph?',
        'Does any entity have cross-case shared identifiers?',
        'Verify physical evidence files against the tamper-evident ledger'
      ]);
      return;
    }

    setLoadingSuggestions(true);
    try {
      const suggestions = await copilotService.getSuggestedQuestions(activeCaseId);
      if (suggestions && suggestions.length > 0) {
        setSuggestedQuestions(suggestions);
      } else {
        setSuggestedQuestions([
          'What are the key entities identified in this case?',
          'Are there any pending alerts requiring review?',
          'Show verified relationships between entities',
          'Is all case evidence cryptographically verified?'
        ]);
      }
    } catch {
      setSuggestedQuestions([
        'Summarize the key entities in this case',
        'Show verified relationships in the network graph',
        'Are there any pending investigative alerts?',
        'Check evidence integrity against the blockchain ledger'
      ]);
    } finally {
      setLoadingSuggestions(false);
    }
  }, [activeCaseId]);

  useEffect(() => {
    loadSuggestedQuestions();
  }, [loadSuggestedQuestions]);

  const handleSendQuery = async (queryText: string) => {
    const q = queryText.trim();
    if (!q || isSubmitting) return;

    if (!activeCaseId) {
      alert('Please select an active investigation scope first from the Investigations page.');
      return;
    }

    const turnId = `turn-${Date.now()}`;
    const newTurn: ChatTurn = {
      id: turnId,
      query: q,
      isLoading: true,
      timestamp: new Date()
    };

    setChatTurns(prev => [...prev, newTurn]);
    setInputQuery('');
    setIsSubmitting(true);

    const requestPayload: CopilotQueryRequest = {
      caseId: activeCaseId,
      query: q,
      conversationId: conversationId,
      maxResults: 10,
      includeCrossCase,
      includeAlerts,
      includeTimeline,
      includeLocations,
      includeGraph,
      includeEvidence
    };

    try {
      const response = await copilotService.askCopilot(requestPayload);
      if (response.conversationId) {
        setConversationId(response.conversationId);
      }

      setChatTurns(prev =>
        prev.map(turn =>
          turn.id === turnId ? { ...turn, isLoading: false, response } : turn
        )
      );
    } catch (err: any) {
      const errMsg = err?.response?.data?.detail || err?.message || 'Failed to query Copilot';
      setChatTurns(prev =>
        prev.map(turn =>
          turn.id === turnId ? { ...turn, isLoading: false, error: errMsg } : turn
        )
      );
    } finally {
      setIsSubmitting(false);
    }
  };

  const handleClearChat = async () => {
    if (conversationId) {
      try {
        await copilotService.deleteConversation(conversationId);
      } catch {
        // ignore deletion error on reset
      }
    }
    setConversationId(undefined);
    setChatTurns([]);
  };

  const getConfidenceBadge = (confidence: string, score: number) => {
    switch (confidence) {
      case 'HIGH':
        return (
          <span className="px-2 py-0.5 text-xs font-semibold rounded-full bg-emerald-500/10 text-emerald-600 border border-emerald-500/20 flex items-center gap-1">
            <CheckCircle2 className="w-3 h-3" /> Grounded ({Math.round(score * 100)}%)
          </span>
        );
      case 'MEDIUM':
        return (
          <span className="px-2 py-0.5 text-xs font-semibold rounded-full bg-blue-500/10 text-blue-600 border border-blue-500/20 flex items-center gap-1">
            <CheckCircle2 className="w-3 h-3" /> Moderate ({Math.round(score * 100)}%)
          </span>
        );
      case 'MODEL_SIGNAL':
        return (
          <span className="px-2 py-0.5 text-xs font-semibold rounded-full bg-amber-500/10 text-amber-600 border border-amber-500/20 flex items-center gap-1">
            <Sparkles className="w-3 h-3" /> Model Prediction (Pending Review)
          </span>
        );
      case 'LOW':
        return (
          <span className="px-2 py-0.5 text-xs font-semibold rounded-full bg-orange-500/10 text-orange-600 border border-orange-500/20 flex items-center gap-1">
            <AlertTriangle className="w-3 h-3" /> Low Evidence ({Math.round(score * 100)}%)
          </span>
        );
      default:
        return (
          <span className="px-2 py-0.5 text-xs font-semibold rounded-full bg-zinc-500/10 text-zinc-500 border border-zinc-500/20 flex items-center gap-1">
            <XCircle className="w-3 h-3" /> Insufficient Evidence
          </span>
        );
    }
  };

  return (
    <div className="flex flex-col h-full bg-[var(--color-surface)] text-[var(--color-text-primary)]">
      {/* Top Header Bar */}
      <div className="border-b border-[var(--color-border)] bg-[var(--color-surface)] px-6 py-4 flex flex-col md:flex-row md:items-center md:justify-between gap-4 shadow-2xs">
        <div>
          <div className="flex items-center gap-3">
            <div className="p-2 rounded-xl bg-[var(--color-surface-subtle)] text-[var(--color-accent)] border border-[var(--color-border)]">
              <Bot className="w-5 h-5" />
            </div>
            <div>
              <h1 className="text-xl font-bold text-[var(--color-text-primary)] flex items-center gap-2">
                Investigation Copilot
                <span className="text-xs px-2.5 py-0.5 rounded-full bg-[var(--color-surface-subtle)] text-[var(--color-accent)] border border-[var(--color-border)] font-medium">
                  Phase 10 Grounded
                </span>
              </h1>
              <p className="text-xs text-[var(--color-text-secondary)]">
                Evidence-grounded decision support querying PostgreSQL, Neo4j, and the Cryptographic Ledger.
              </p>
            </div>
          </div>
        </div>

        {/* Case Scope Indicator & Actions */}
        <div className="flex items-center gap-3">
          {activeCaseNumber ? (
            <div className="flex items-center gap-2 px-3 py-1.5 rounded-lg bg-[var(--color-surface-subtle)] border border-[var(--color-border-subtle)] text-xs">
              <span className="w-2 h-2 rounded-full bg-emerald-500 animate-pulse" />
              <span className="text-[var(--color-text-primary)] font-medium">Active Case:</span>
              <strong className="text-[var(--color-text-primary)]">{activeCaseNumber}</strong>
            </div>
          ) : (
            <div className="flex items-center gap-2 px-3 py-1.5 rounded-lg bg-amber-500/10 border border-amber-500/20 text-xs text-amber-700">
              <AlertTriangle className="w-3.5 h-3.5" />
              <span>No Active Case Selected</span>
            </div>
          )}

          <button
            onClick={handleClearChat}
            disabled={chatTurns.length === 0}
            className="flex items-center gap-1.5 px-3 py-1.5 text-xs font-medium text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] bg-[var(--color-surface)] border border-[var(--color-border-subtle)] rounded-lg hover:bg-[var(--color-surface-subtle)] transition disabled:opacity-40"
          >
            <Trash2 className="w-3.5 h-3.5" />
            Clear
          </button>
        </div>
      </div>

      {/* Main Conversation Container */}
      <div className="flex-1 overflow-y-auto px-4 md:px-8 py-6 space-y-6">
        <ResponsibleAiNotice compact />

        {/* Initial Welcome Greeting */}
        {chatTurns.length === 0 && (
          <div className="max-w-3xl mx-auto my-8 bg-[var(--color-surface)] border border-[var(--color-border)] rounded-2xl p-6 shadow-sm space-y-4">
            <div className="flex items-center gap-3">
              <div className="p-2.5 rounded-xl bg-[var(--color-surface-subtle)] text-[var(--color-accent)] border border-[var(--color-border)]">
                <Bot className="w-6 h-6" />
              </div>
              <div>
                <h2 className="text-base font-bold text-[var(--color-text-primary)]">Evidence-Grounded Intelligence Querying</h2>
                <p className="text-xs text-[var(--color-text-secondary)]">
                  Answers are derived strictly from case documents, graph topologies, timelines, and anomaly alerts.
                </p>
              </div>
            </div>

            <div className="p-3.5 rounded-xl bg-[#FBF9F5] border border-[#EAE0D5] text-xs text-[var(--color-text-primary)] space-y-2 leading-relaxed">
              <p>
                <strong>Evidentiary Governance:</strong> The LLM is strictly used as an analytical and explanation layer. It cannot invent entities, fabricate evidence records, or make guilt assertions.
              </p>
              <p>
                <strong>Integrity Aware:</strong> Any cited evidence record is verified against the Phase 9 append-only cryptographic ledger. Modifications or ledger mismatches surface instant warnings.
              </p>
            </div>

            {/* Suggested Starter Questions */}
            <div className="space-y-2 pt-2">
              <span className="text-xs font-semibold text-[var(--color-text-secondary)] uppercase tracking-wider">Suggested Inquiries:</span>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-2">
                {suggestedQuestions.map((q, idx) => (
                  <button
                    key={idx}
                    onClick={() => handleSendQuery(q)}
                    disabled={!activeCaseId || isSubmitting}
                    className="p-3 text-left text-xs bg-[var(--color-surface)] hover:bg-[var(--color-surface-subtle)] border border-[var(--color-border)] hover:border-[var(--color-accent)]/40 rounded-xl transition flex items-center justify-between group disabled:opacity-40"
                  >
                    <span className="text-[var(--color-text-primary)] font-medium group-hover:text-[var(--color-accent)]">{q}</span>
                    <ArrowRight className="w-3.5 h-3.5 text-[var(--color-accent)] shrink-0 opacity-0 group-hover:opacity-100 transition-opacity" />
                  </button>
                ))}
              </div>
            </div>
          </div>
        )}

        {/* Chat Turns List */}
        {chatTurns.map(turn => (
          <div key={turn.id} className="max-w-4xl mx-auto space-y-4">
            {/* User Message */}
            <div className="flex justify-end gap-3">
              <div className="max-w-2xl bg-[var(--color-accent)] text-white rounded-2xl rounded-tr-xs px-4 py-3 shadow-sm text-sm">
                <p className="leading-relaxed">{turn.query}</p>
                <span className="block text-[10px] text-zinc-400 mt-1 text-right">
                  {turn.timestamp.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                </span>
              </div>
              <div className="w-8 h-8 rounded-full bg-[var(--color-surface-subtle)] flex items-center justify-center text-[var(--color-text-primary)] shrink-0 text-xs font-bold">
                {user?.name ? user.name[0].toUpperCase() : 'U'}
              </div>
            </div>

            {/* Assistant Response */}
            <div className="flex justify-start gap-3">
              <div className="w-8 h-8 rounded-full bg-[var(--color-surface-subtle)] border border-[var(--color-border)] flex items-center justify-center text-[var(--color-accent)] shrink-0 mt-1">
                <Bot className="w-4 h-4" />
              </div>

              <div className="flex-1 max-w-3xl bg-[var(--color-surface)] border border-[var(--color-border)] rounded-2xl rounded-tl-xs p-5 shadow-xs space-y-4">
                {turn.isLoading && (
                  <div className="flex items-center gap-3 text-xs text-[var(--color-text-secondary)] py-2">
                    <RefreshCw className="w-4 h-4 animate-spin text-[var(--color-accent)]" />
                    <span>Retrieving verified case facts and constructing grounded explanation...</span>
                  </div>
                )}

                {turn.error && (
                  <div className="p-3.5 rounded-xl bg-red-50 border border-red-200 text-xs text-red-700 flex items-start gap-2.5">
                    <ShieldAlert className="w-4 h-4 shrink-0 mt-0.5" />
                    <div>
                      <strong className="block font-semibold">Query Error</strong>
                      <span>{turn.error}</span>
                    </div>
                  </div>
                )}

                {turn.response && (
                  <>
                    {/* Header Metadata */}
                    <div className="flex flex-wrap items-center justify-between gap-2 border-b border-[#F0E6DA] pb-3">
                      <div className="flex items-center gap-2">
                        {getConfidenceBadge(turn.response.confidence, turn.response.confidenceScore)}
                        <span className="text-[11px] px-2 py-0.5 rounded bg-[var(--color-surface-subtle)] text-[var(--color-text-primary)] border border-[var(--color-border-subtle)] font-mono">
                          INTENT: {turn.response.intent}
                        </span>
                      </div>
                      <span className="text-[10px] text-[var(--color-text-secondary)]">
                        {new Date(turn.response.executedAtUtc).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                      </span>
                    </div>

                    {/* Integrity Warnings Alert (if any) */}
                    {turn.response.warnings && turn.response.warnings.length > 0 && (
                      <div className="p-3.5 rounded-xl bg-amber-500/10 border border-amber-500/30 text-xs text-amber-800 space-y-1.5">
                        <div className="flex items-center gap-2 font-semibold">
                          <AlertTriangle className="w-4 h-4 text-amber-600" />
                          <span>Evidentiary & Ledger Notice</span>
                        </div>
                        <ul className="list-disc list-inside space-y-1 text-[11px]">
                          {turn.response.warnings.map((w, wIdx) => (
                            <li key={wIdx}>{w}</li>
                          ))}
                        </ul>
                      </div>
                    )}

                    {/* Answer Text */}
                    <div className="text-sm text-[var(--color-text-primary)] leading-relaxed whitespace-pre-line font-normal">
                      {turn.response.answer}
                    </div>

                    {/* Interactive Citation Badges */}
                    <div className="space-y-3 pt-2 border-t border-[#F0E6DA]">
                      <span className="text-xs font-semibold text-[var(--color-text-primary)] uppercase tracking-wider block">
                        Grounded Sources & Citations:
                      </span>

                      <div className="flex flex-wrap gap-2">
                        {/* Evidence Citations */}
                        {turn.response.evidenceCitations.map(ev => (
                          <button
                            key={ev.evidenceId}
                            onClick={() => setSelectedCitation({ type: 'evidence', data: ev })}
                            className={`px-2.5 py-1 rounded-lg text-xs border flex items-center gap-1.5 transition ${
                              ev.integrityStatus === 'EVIDENCE_MODIFIED' || ev.integrityStatus === 'HASH_MISMATCH'
                                ? 'bg-red-50 border-red-300 text-red-800 hover:bg-red-100'
                                : 'bg-[var(--color-surface-subtle)] border-[var(--color-border)] text-[#8C3E1B] hover:bg-[#F4E3D3]'
                            }`}
                          >
                            <FileText className="w-3.5 h-3.5" />
                            <span className="font-medium truncate max-w-[140px]">{ev.fileName}</span>
                            {ev.integrityStatus === 'EVIDENCE_MODIFIED' ? (
                              <span className="text-[10px] px-1 bg-red-600 text-white rounded font-bold">TAMPERED</span>
                            ) : (
                              <span className="text-[10px] text-emerald-700 font-semibold">✓ Ledger</span>
                            )}
                          </button>
                        ))}

                        {/* Entity Citations */}
                        {turn.response.entityCitations.map(ent => (
                          <button
                            key={ent.entityId}
                            onClick={() => setSelectedCitation({ type: 'entity', data: ent })}
                            className="px-2.5 py-1 rounded-lg text-xs bg-blue-50 border border-blue-200 text-blue-800 hover:bg-blue-100 transition flex items-center gap-1.5"
                          >
                            <Users2 className="w-3.5 h-3.5" />
                            <span className="font-medium">{ent.canonicalName}</span>
                            <span className="text-[10px] opacity-75">({ent.entityType})</span>
                          </button>
                        ))}

                        {/* Relationship Citations */}
                        {turn.response.relationshipCitations.map(rel => (
                          <button
                            key={rel.relationshipId}
                            onClick={() => setSelectedCitation({ type: 'relationship', data: rel })}
                            className="px-2.5 py-1 rounded-lg text-xs bg-emerald-50 border border-emerald-200 text-emerald-800 hover:bg-emerald-100 transition flex items-center gap-1.5"
                          >
                            <Network className="w-3.5 h-3.5" />
                            <span className="font-medium">
                              {rel.sourceEntityName} → {rel.targetEntityName}
                            </span>
                            <span className="text-[10px] font-mono">[{rel.relationshipType}]</span>
                          </button>
                        ))}

                        {/* Timeline Citations */}
                        {turn.response.timelineCitations.map(tl => (
                          <button
                            key={tl.eventId}
                            onClick={() => setSelectedCitation({ type: 'timeline', data: tl })}
                            className="px-2.5 py-1 rounded-lg text-xs bg-purple-50 border border-purple-200 text-purple-800 hover:bg-purple-100 transition flex items-center gap-1.5"
                          >
                            <Clock className="w-3.5 h-3.5" />
                            <span className="font-medium">{tl.eventType}</span>
                            <span className="text-[10px]">
                              {tl.precision === 'DATE_ONLY'
                                ? new Date(tl.eventTimestampUtc).toLocaleDateString()
                                : new Date(tl.eventTimestampUtc).toLocaleString()}
                            </span>
                          </button>
                        ))}

                        {/* Alert Citations */}
                        {turn.response.alertCitations.map(alt => (
                          <button
                            key={alt.alertId}
                            onClick={() => setSelectedCitation({ type: 'alert', data: alt })}
                            className="px-2.5 py-1 rounded-lg text-xs bg-amber-50 border border-amber-200 text-amber-800 hover:bg-amber-100 transition flex items-center gap-1.5"
                          >
                            <AlertTriangle className="w-3.5 h-3.5 text-amber-600" />
                            <span className="font-medium">Alert: {alt.alertType}</span>
                            <span className="text-[10px] font-bold">[{alt.severity}]</span>
                          </button>
                        ))}

                        {/* Model Signals (GAT) */}
                        {turn.response.modelSignals.map((sig, sIdx) => (
                          <button
                            key={sIdx}
                            onClick={() => setSelectedCitation({ type: 'model', data: sig })}
                            className="px-2.5 py-1 rounded-lg text-xs bg-violet-50 border border-violet-200 text-violet-800 hover:bg-violet-100 transition flex items-center gap-1.5"
                          >
                            <Sparkles className="w-3.5 h-3.5 text-violet-600" />
                            <span className="font-medium">AI Signal (GAT)</span>
                            <span className="text-[10px] bg-violet-200 text-violet-900 px-1 rounded font-semibold">
                              Pending Review
                            </span>
                          </button>
                        ))}

                        {/* Cross-Case Connections */}
                        {turn.response.crossCaseConnections &&
                          turn.response.crossCaseConnections.map(cc => (
                            <button
                              key={cc.id}
                              onClick={() => setSelectedCitation({ type: 'relationship', data: cc })}
                              className="px-2.5 py-1 rounded-lg text-xs bg-indigo-50 border border-indigo-200 text-indigo-800 hover:bg-indigo-100 transition flex items-center gap-1.5"
                            >
                              <GitFork className="w-3.5 h-3.5" />
                              <span className="font-medium">
                                Cross-Case: {cc.sourceCaseNumber} ↔ {cc.targetCaseNumber}
                              </span>
                            </button>
                          ))}
                      </div>
                    </div>

                    {/* Suggested Follow-Ups */}
                    {turn.response.suggestedFollowUps && turn.response.suggestedFollowUps.length > 0 && (
                      <div className="pt-2 border-t border-[#F0E6DA] space-y-1.5">
                        <span className="text-[11px] font-semibold text-[var(--color-text-secondary)] uppercase tracking-wider block">
                          Suggested Investigative Follow-ups:
                        </span>
                        <div className="flex flex-wrap gap-2">
                          {turn.response.suggestedFollowUps.map((fu, fuIdx) => (
                            <button
                              key={fuIdx}
                              onClick={() => handleSendQuery(fu)}
                              className="text-xs px-3 py-1 rounded-full bg-[var(--color-surface-subtle)] text-[var(--color-text-primary)] hover:bg-[var(--color-surface-subtle)] hover:text-[var(--color-accent)] border border-[var(--color-border-subtle)] transition"
                            >
                              {fu}
                            </button>
                          ))}
                        </div>
                      </div>
                    )}
                  </>
                )}
              </div>
            </div>
          </div>
        ))}
        <div ref={bottomRef} />
      </div>

      {/* Query Input Section */}
      <div className="border-t border-[var(--color-border)] bg-[var(--color-surface)] p-4 md:px-8 shadow-md space-y-3">
        {/* Toggle Filters Bar */}
        <div className="flex items-center justify-between text-xs text-[var(--color-text-secondary)]">
          <button
            onClick={() => setShowFilters(!showFilters)}
            className="flex items-center gap-1 hover:text-[var(--color-text-primary)] font-medium"
          >
            <Sliders className="w-3.5 h-3.5" />
            <span>Retrieval Scope Filters</span>
            {showFilters ? <ChevronUp className="w-3.5 h-3.5" /> : <ChevronDown className="w-3.5 h-3.5" />}
          </button>

          <span className="text-[11px] text-[var(--color-text-muted)]">
            Presumption of innocence strictly observed. Decisions require human confirmation.
          </span>
        </div>

        {/* Collapsible Retrieval Filters */}
        {showFilters && (
          <div className="p-3 bg-[#FBF9F5] border border-[#EAE0D5] rounded-xl flex flex-wrap items-center gap-4 text-xs text-[var(--color-text-primary)]">
            <label className="flex items-center gap-1.5 cursor-pointer">
              <input
                type="checkbox"
                checked={includeCrossCase}
                onChange={e => setIncludeCrossCase(e.target.checked)}
                className="rounded border-[var(--color-border-subtle)] text-[var(--color-accent)] focus:ring-[#A6522C]"
              />
              Cross-Case Resolution
            </label>
            <label className="flex items-center gap-1.5 cursor-pointer">
              <input
                type="checkbox"
                checked={includeAlerts}
                onChange={e => setIncludeAlerts(e.target.checked)}
                className="rounded border-[var(--color-border-subtle)] text-[var(--color-accent)] focus:ring-[#A6522C]"
              />
              Anomaly Alerts
            </label>
            <label className="flex items-center gap-1.5 cursor-pointer">
              <input
                type="checkbox"
                checked={includeGraph}
                onChange={e => setIncludeGraph(e.target.checked)}
                className="rounded border-[var(--color-border-subtle)] text-[var(--color-accent)] focus:ring-[#A6522C]"
              />
              Graph Relationships
            </label>
            <label className="flex items-center gap-1.5 cursor-pointer">
              <input
                type="checkbox"
                checked={includeTimeline}
                onChange={e => setIncludeTimeline(e.target.checked)}
                className="rounded border-[var(--color-border-subtle)] text-[var(--color-accent)] focus:ring-[#A6522C]"
              />
              Timeline Events
            </label>
            <label className="flex items-center gap-1.5 cursor-pointer">
              <input
                type="checkbox"
                checked={includeEvidence}
                onChange={e => setIncludeEvidence(e.target.checked)}
                className="rounded border-[var(--color-border-subtle)] text-[var(--color-accent)] focus:ring-[#A6522C]"
              />
              Ledger & Evidence
            </label>
          </div>
        )}

        {/* Input Box and Submit Button */}
        <form
          onSubmit={e => {
            e.preventDefault();
            handleSendQuery(inputQuery);
          }}
          className="flex items-center gap-2"
        >
          <input
            type="text"
            value={inputQuery}
            onChange={e => setInputQuery(e.target.value)}
            placeholder={
              activeCaseNumber
                ? `Ask about Case ${activeCaseNumber} (e.g., 'How is Person A connected to Person B?')...`
                : 'Please select an active case scope to query Copilot...'
            }
            disabled={!activeCaseId || isSubmitting}
            className="flex-1 px-4 py-3 text-sm bg-[var(--color-surface)] border border-[var(--color-border-subtle)] rounded-xl focus:outline-none focus:ring-2 focus:ring-[#A6522C] focus:border-transparent transition placeholder-[#8C7A6B] disabled:opacity-50"
          />

          <button
            type="submit"
            disabled={!activeCaseId || isSubmitting || !inputQuery.trim()}
            className="px-5 py-3 bg-[var(--color-accent)] hover:bg-[#8C3E1B] text-white rounded-xl font-medium text-sm flex items-center gap-2 transition shadow-xs disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {isSubmitting ? (
              <RefreshCw className="w-4 h-4 animate-spin" />
            ) : (
              <>
                <span>Query</span>
                <Send className="w-4 h-4" />
              </>
            )}
          </button>
        </form>
      </div>

      {/* Modal / Slide-Over for Citation Inspection */}
      {selectedCitation && (
        <div className="fixed inset-0 z-50 bg-black/40 flex items-center justify-center p-4 backdrop-blur-xs">
          <div className="bg-[var(--color-surface)] rounded-2xl border border-[var(--color-border)] shadow-xl max-w-lg w-full p-6 space-y-4 max-h-[80vh] overflow-y-auto">
            <div className="flex items-center justify-between border-b border-[#F0E6DA] pb-3">
              <h3 className="font-bold text-[var(--color-text-primary)] flex items-center gap-2 text-sm">
                <ShieldCheck className="w-4 h-4 text-[var(--color-accent)]" />
                Citation Evidentiary Inspection
              </h3>
              <button
                onClick={() => setSelectedCitation(null)}
                className="text-[var(--color-text-muted)] hover:text-[var(--color-text-primary)] text-xs font-medium"
              >
                Close
              </button>
            </div>

            {/* Evidence Citation Detail */}
            {selectedCitation.type === 'evidence' && (
              <div className="space-y-3 text-xs text-[var(--color-text-primary)]">
                <div>
                  <span className="text-[10px] text-[var(--color-text-secondary)] uppercase font-semibold">File Name:</span>
                  <div className="text-sm font-bold text-[var(--color-text-primary)]">{selectedCitation.data.fileName}</div>
                </div>

                <div className="p-3 bg-[var(--color-surface)] rounded-xl border border-[var(--color-border)] space-y-1.5">
                  <span className="text-[10px] text-[var(--color-text-secondary)] uppercase font-semibold">Immutable SHA-256 Hash:</span>
                  <div className="font-mono text-[11px] break-all bg-[var(--color-surface)] p-2 rounded border border-[var(--color-border-subtle)]">
                    {selectedCitation.data.sha256Hash}
                  </div>
                </div>

                <div className="flex items-center justify-between p-3 bg-[var(--color-surface)] border border-[var(--color-border)] rounded-xl">
                  <span>Cryptographic Ledger Status:</span>
                  <span
                    className={`font-bold px-2 py-0.5 rounded text-[11px] ${
                      selectedCitation.data.integrityStatus === 'EVIDENCE_MODIFIED'
                        ? 'bg-red-100 text-red-700'
                        : 'bg-emerald-100 text-emerald-800'
                    }`}
                  >
                    {selectedCitation.data.integrityStatus}
                  </span>
                </div>

                {selectedCitation.data.snippet && (
                  <div className="space-y-1">
                    <span className="text-[10px] text-[var(--color-text-secondary)] uppercase font-semibold">Extracted Quotation:</span>
                    <p className="p-3 bg-[var(--color-surface)] border border-[var(--color-border)] rounded-xl italic text-[var(--color-text-primary)]">
                      "{selectedCitation.data.snippet}"
                    </p>
                  </div>
                )}
              </div>
            )}

            {/* Entity Citation Detail */}
            {selectedCitation.type === 'entity' && (
              <div className="space-y-3 text-xs text-[var(--color-text-primary)]">
                <div>
                  <span className="text-[10px] text-[var(--color-text-secondary)] uppercase font-semibold">Entity Canonical Name:</span>
                  <div className="text-base font-bold text-[var(--color-text-primary)]">{selectedCitation.data.canonicalName}</div>
                </div>
                <div className="flex gap-4">
                  <div>
                    <span className="text-[10px] text-[var(--color-text-secondary)] uppercase font-semibold">Type:</span>
                    <div className="font-medium">{selectedCitation.data.entityType}</div>
                  </div>
                  <div>
                    <span className="text-[10px] text-[var(--color-text-secondary)] uppercase font-semibold">Confidence:</span>
                    <div className="font-medium">{Math.round(selectedCitation.data.confidence * 100)}%</div>
                  </div>
                </div>
              </div>
            )}

            {/* Relationship Citation Detail */}
            {selectedCitation.type === 'relationship' && (
              <div className="space-y-3 text-xs text-[var(--color-text-primary)]">
                <div>
                  <span className="text-[10px] text-[var(--color-text-secondary)] uppercase font-semibold">Verified Connection:</span>
                  <div className="text-sm font-bold text-[var(--color-text-primary)]">
                    {selectedCitation.data.sourceEntityName} → {selectedCitation.data.targetEntityName}
                  </div>
                </div>
                <div className="p-3 bg-[var(--color-surface)] rounded-xl border border-[var(--color-border)] space-y-1">
                  <div>Type: <strong className="font-mono">{selectedCitation.data.relationshipType}</strong></div>
                  <div>Confidence: {Math.round(selectedCitation.data.confidence * 100)}%</div>
                  <div>Supporting Evidence Count: {selectedCitation.data.evidenceIds?.length || 0}</div>
                </div>
              </div>
            )}

            {/* Alert Citation Detail */}
            {selectedCitation.type === 'alert' && (
              <div className="space-y-3 text-xs text-[var(--color-text-primary)]">
                <div>
                  <span className="text-[10px] text-[var(--color-text-secondary)] uppercase font-semibold">Investigative Alert:</span>
                  <div className="text-sm font-bold text-[var(--color-text-primary)]">{selectedCitation.data.alertType}</div>
                </div>
                <div className="p-3 bg-[var(--color-surface)] rounded-xl border border-[var(--color-border)] space-y-1.5">
                  <div>Severity: <strong className="uppercase">{selectedCitation.data.severity}</strong></div>
                  <div>Anomaly Score: {selectedCitation.data.score?.toFixed(2)} (Threshold: {selectedCitation.data.threshold?.toFixed(2)})</div>
                  <p className="text-[var(--color-text-primary)] pt-1">{selectedCitation.data.explanation}</p>
                </div>
              </div>
            )}

            {/* Model Signal Detail (GAT) */}
            {selectedCitation.type === 'model' && (
              <div className="space-y-3 text-xs text-[var(--color-text-primary)]">
                <div className="p-3 bg-violet-50 border border-violet-200 rounded-xl space-y-2">
                  <div className="flex items-center gap-1.5 text-violet-900 font-bold text-sm">
                    <Sparkles className="w-4 h-4 text-violet-600" />
                    <span>Graph Attention Network (GAT) Edge Prediction</span>
                  </div>
                  <p className="text-violet-800 text-xs">
                    This is an algorithmic topological lead indicating a potential link between entities. It requires human investigator validation and is NOT confirmed fact.
                  </p>
                  <div className="text-[11px] text-violet-700">
                    Confidence Score: <strong>{Math.round(selectedCitation.data.score * 100)}%</strong> | Status: <strong>Pending Review</strong>
                  </div>
                </div>
              </div>
            )}

            <div className="pt-3 border-t border-[#F0E6DA] flex justify-end">
              <button
                onClick={() => setSelectedCitation(null)}
                className="px-4 py-2 bg-[var(--color-surface-subtle)] text-[var(--color-accent)] font-semibold text-xs rounded-xl hover:bg-[#F4E3D3] transition"
              >
                Dismiss
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};
