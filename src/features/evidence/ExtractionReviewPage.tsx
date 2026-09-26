import React, { useState, useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { 
  ShieldCheck, 
  ShieldAlert, 
  ArrowLeft, 
  CheckCircle, 
  XCircle, 
  Edit3, 
  RefreshCw, 
  FileText, 
  Database,
  Share2,
  AlertTriangle,
  Clock,
  Layers
} from 'lucide-react';
import { EvidenceService, ExtractionResult, ExtractedEntity, ExtractedRelationship, IntegrityCheckResult } from '../../services/evidence/evidence.service';
import { Button } from '../../components/common/Button';
import { Card } from '../../components/common/Card';
import { StatusBadge } from '../../components/common/StatusBadge';
import { PhaseNotice } from '../../components/common/PhaseNotice';
import { ResponsibleAiNotice } from '../../components/common/ResponsibleAiNotice';

export const ExtractionReviewPage: React.FC = () => {
  const { evidenceId } = useParams<{ evidenceId: string }>();
  const navigate = useNavigate();

  const [loading, setLoading] = useState(true);
  const [extraction, setExtraction] = useState<ExtractionResult | null>(null);
  const [integrity, setIntegrity] = useState<IntegrityCheckResult | null>(null);
  const [verifying, setVerifying] = useState(false);
  const [actionMessage, setActionMessage] = useState<string | null>(null);

  // Edit Modal State
  const [editingEntity, setEditingEntity] = useState<ExtractedEntity | null>(null);
  const [editRawValue, setEditRawValue] = useState('');
  const [editNormalizedValue, setEditNormalizedValue] = useState('');
  const [savingEdit, setSavingEdit] = useState(false);

  const fetchExtractionData = async () => {
    if (!evidenceId) return;
    setLoading(true);
    try {
      const data = await EvidenceService.getExtraction(evidenceId);
      setExtraction(data);
    } catch (err: any) {
      console.error('Failed to load extraction:', err);
    } finally {
      setLoading(false);
    }
  };

  const checkIntegrity = async () => {
    if (!evidenceId) return;
    setVerifying(true);
    try {
      const res = await EvidenceService.verifyIntegrity(evidenceId);
      setIntegrity(res);
    } catch (err) {
      console.error('Integrity check failed:', err);
    } finally {
      setVerifying(false);
    }
  };

  useEffect(() => {
    fetchExtractionData();
    checkIntegrity();
  }, [evidenceId]);

  const handleApproveEntity = async (entityId: string) => {
    if (!evidenceId) return;
    try {
      await EvidenceService.approveEntity(evidenceId, entityId);
      setActionMessage('Entity approved and promoted to investigation graph.');
      await fetchExtractionData();
      setTimeout(() => setActionMessage(null), 4000);
    } catch (err: any) {
      alert(`Approval failed: ${err?.message || 'Unknown error'}`);
    }
  };

  const handleRejectEntity = async (entityId: string) => {
    if (!evidenceId) return;
    const reason = prompt('Optional rejection reason:');
    try {
      await EvidenceService.rejectEntity(evidenceId, entityId, reason || undefined);
      setActionMessage('Entity marked as rejected.');
      await fetchExtractionData();
      setTimeout(() => setActionMessage(null), 4000);
    } catch (err: any) {
      alert(`Rejection failed: ${err?.message || 'Unknown error'}`);
    }
  };

  const handleStartEdit = (ent: ExtractedEntity) => {
    setEditingEntity(ent);
    setEditRawValue(ent.rawValue);
    setEditNormalizedValue(ent.normalizedValue);
  };

  const handleSaveEdit = async () => {
    if (!evidenceId || !editingEntity) return;
    setSavingEdit(true);
    try {
      await EvidenceService.editEntity(evidenceId, editingEntity.id, editRawValue, editNormalizedValue);
      setEditingEntity(null);
      setActionMessage('Entity value successfully corrected in audit ledger.');
      await fetchExtractionData();
      setTimeout(() => setActionMessage(null), 4000);
    } catch (err: any) {
      alert(`Correction failed: ${err?.message || 'Unknown error'}`);
    } finally {
      setSavingEdit(false);
    }
  };

  const handleApproveRelationship = async (relId: string) => {
    if (!evidenceId) return;
    try {
      await EvidenceService.approveRelationship(evidenceId, relId);
      setActionMessage('Relationship approved and ingested into Neo4j graph with source provenance.');
      await fetchExtractionData();
      setTimeout(() => setActionMessage(null), 4000);
    } catch (err: any) {
      alert(`Relationship approval failed: ${err?.message || 'Unknown error'}`);
    }
  };

  const handleRejectRelationship = async (relId: string) => {
    if (!evidenceId) return;
    const reason = prompt('Optional rejection reason:');
    try {
      await EvidenceService.rejectRelationship(evidenceId, relId, reason || undefined);
      setActionMessage('Relationship marked as rejected.');
      await fetchExtractionData();
      setTimeout(() => setActionMessage(null), 4000);
    } catch (err: any) {
      alert(`Rejection failed: ${err?.message || 'Unknown error'}`);
    }
  };

  if (loading) {
    return (
      <div className="flex items-center justify-center p-16">
        <RefreshCw className="w-8 h-8 text-blue-500 animate-spin mr-3" />
        <span className="text-[var(--color-text-secondary)] font-medium">Loading evidence extraction review data...</span>
      </div>
    );
  }

  const entities = extraction?.entities || [];
  const relationships = extraction?.relationships || [];

  return (
    <div className="space-y-6 pb-12">
      {/* Top Navigation & Status */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-[var(--color-border)] pb-4">
        <div className="flex items-center space-x-3">
          <Button
            variant="outline"
            size="sm"
            onClick={() => navigate('/evidence')}
            className="flex items-center text-[var(--color-text-secondary)] border-[var(--color-border)] hover:bg-[var(--color-surface-subtle)]"
          >
            <ArrowLeft className="w-4 h-4 mr-1.5" /> Back to Evidence
          </Button>
          <div>
            <h1 className="text-2xl font-bold text-[var(--color-text-primary)] flex items-center gap-2">
              <FileText className="w-6 h-6 text-blue-400" />
              Evidence Extraction Review
            </h1>
            <p className="text-xs text-[var(--color-text-muted)]">
              Evidence Identifier: <span className="font-mono text-slate-200">{evidenceId}</span>
              {extraction?.caseId && <> &bull; Case: <span className="font-mono text-slate-200">{extraction.caseId}</span></>}
            </p>
          </div>
        </div>

        <div className="flex items-center gap-3">
          <Button
            variant="outline"
            size="sm"
            onClick={checkIntegrity}
            disabled={verifying}
            className="flex items-center border-[var(--color-border)] text-[var(--color-text-secondary)] hover:bg-[var(--color-surface-subtle)]"
          >
            <RefreshCw className={`w-3.5 h-3.5 mr-1.5 ${verifying ? 'animate-spin' : ''}`} />
            {verifying ? 'Verifying...' : 'Verify Integrity'}
          </Button>

          <Link to={`/evidence/${evidenceId}`}>
            <Button variant="outline" size="sm" className="border-[var(--color-border)] text-[var(--color-text-secondary)] hover:bg-[var(--color-surface-subtle)]">
              View Document
            </Button>
          </Link>
        </div>
      </div>

      <PhaseNotice phaseNumber={2} moduleName="Evidence Processing & AI Extraction Engine" />
      <ResponsibleAiNotice />

      {/* Action Notification */}
      {actionMessage && (
        <div className="p-3 bg-emerald-900/40 border border-emerald-500/60 rounded-md flex items-center text-emerald-200 text-sm animate-fade-in">
          <CheckCircle className="w-4 h-4 mr-2 text-emerald-400 flex-shrink-0" />
          {actionMessage}
        </div>
      )}

      {/* Evidence Integrity & Status Header Banner */}
      <Card className="bg-[var(--color-surface)] border-[var(--color-border)] p-5">
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4 divide-y md:divide-y-0 md:divide-x divide-slate-800">
          <div>
            <div className="text-xs font-semibold uppercase tracking-wider text-[var(--color-text-muted)] mb-1">Evidence ID</div>
            <div className="text-base font-mono font-bold text-[var(--color-text-primary)]">{evidenceId}</div>
            <div className="text-xs text-slate-500 mt-1">Staged for human officer verification</div>
          </div>

          <div className="md:pl-4 pt-3 md:pt-0">
            <div className="text-xs font-semibold uppercase tracking-wider text-[var(--color-text-muted)] mb-1">Processing Status</div>
            <div className="flex items-center gap-2">
              <span className={`px-2.5 py-0.5 rounded text-xs font-semibold uppercase tracking-wide ${
                extraction?.status === 'APPROVED' ? 'bg-emerald-950 text-emerald-400 border border-emerald-800' :
                extraction?.status === 'REVIEW_REQUIRED' ? 'bg-amber-950 text-amber-400 border border-amber-800' :
                'bg-blue-950 text-blue-400 border border-blue-800'
              }`}>
                {extraction?.status || 'REVIEW REQUIRED'}
              </span>
              <span className="text-xs text-[var(--color-text-muted)]">
                ({entities.filter(e => e.reviewStatus === 'PENDING').length} pending entities)
              </span>
            </div>
            <div className="text-xs text-slate-500 mt-1">Graph ingestion blocked until approval</div>
          </div>

          <div className="md:pl-4 pt-3 md:pt-0">
            <div className="text-xs font-semibold uppercase tracking-wider text-[var(--color-text-muted)] mb-1">Cryptographic Integrity</div>
            <div className="flex items-center gap-2">
              {integrity?.status === 'VALID' ? (
                <span className="flex items-center gap-1 text-xs font-semibold text-emerald-400 bg-emerald-950/70 border border-emerald-800/80 px-2.5 py-0.5 rounded">
                  <ShieldCheck className="w-3.5 h-3.5 text-emerald-400" />
                  SHA-256 VERIFIED
                </span>
              ) : integrity?.status === 'TAMPERED' ? (
                <span className="flex items-center gap-1 text-xs font-semibold text-red-400 bg-red-950/70 border border-red-800/80 px-2.5 py-0.5 rounded">
                  <ShieldAlert className="w-3.5 h-3.5 text-red-400" />
                  TAMPERED DETECTED
                </span>
              ) : (
                <span className="text-xs font-mono text-[var(--color-text-muted)]">
                  {integrity?.storedHash ? `${integrity.storedHash.substring(0, 16)}...` : 'Computing hash...'}
                </span>
              )}
            </div>
            <div className="text-xs text-slate-500 font-mono mt-1 truncate max-w-xs" title={integrity?.storedHash}>
              Hash: {integrity?.storedHash || 'Calculating...'}
            </div>
          </div>
        </div>
      </Card>

      {/* Raw Text / Snippet Excerpt */}
      {extraction?.rawTextSnippet && (
        <Card className="bg-[var(--color-surface)] border-[var(--color-border)] p-4">
          <div className="text-xs font-semibold text-[var(--color-text-muted)] uppercase tracking-wider mb-2 flex items-center gap-1.5">
            <Layers className="w-3.5 h-3.5 text-[var(--color-text-muted)]" /> Source Text Snippet
          </div>
          <p className="text-xs font-mono text-[var(--color-text-secondary)] bg-[var(--color-surface-subtle)] p-3 rounded border border-[var(--color-border)] whitespace-pre-wrap leading-relaxed">
            {extraction.rawTextSnippet}
          </p>
        </Card>
      )}

      {/* Extracted Entities Section */}
      <div className="space-y-3">
        <div className="flex items-center justify-between">
          <h2 className="text-lg font-bold text-[var(--color-text-primary)] flex items-center gap-2">
            <Database className="w-5 h-5 text-blue-400" />
            Extracted Entities ({entities.length})
          </h2>
          <div className="text-xs text-[var(--color-text-muted)]">
            Approved items will be committed to the Knowledge Graph as canonical nodes
          </div>
        </div>

        {entities.length === 0 ? (
          <Card className="bg-[var(--color-surface)] border-[var(--color-border)] p-8 text-center text-[var(--color-text-muted)]">
            No entities extracted from this evidence item.
          </Card>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {entities.map((ent) => {
              const isApproved = ent.reviewStatus === 'APPROVED';
              const isRejected = ent.reviewStatus === 'REJECTED';

              return (
                <Card 
                  key={ent.id}
                  className={`p-4 border transition-all ${
                    isApproved ? 'bg-emerald-950/20 border-emerald-800/60' :
                    isRejected ? 'bg-[var(--color-surface-subtle)] border-[var(--color-border)] opacity-60' :
                    'bg-[var(--color-surface)] border-[var(--color-border)] hover:border-[var(--color-border)]'
                  }`}
                >
                  <div className="flex items-start justify-between gap-2 mb-2">
                    <span className="px-2 py-0.5 rounded text-[10px] font-bold tracking-wider uppercase bg-blue-950 text-blue-300 border border-blue-800">
                      {ent.entityType}
                    </span>
                    <span className={`text-[11px] font-semibold px-2 py-0.5 rounded ${
                      isApproved ? 'text-emerald-400 bg-emerald-950 border border-emerald-800' :
                      isRejected ? 'text-rose-400 bg-rose-950 border border-rose-800' :
                      'text-amber-300 bg-amber-950/80 border border-amber-800'
                    }`}>
                      {ent.reviewStatus}
                    </span>
                  </div>

                  <div className="text-base font-bold text-[var(--color-text-primary)] truncate mb-1" title={ent.rawValue}>
                    {ent.rawValue}
                  </div>

                  <div className="text-xs font-mono text-[var(--color-text-muted)] mb-3 truncate" title={ent.normalizedValue}>
                    Norm: <span className="text-[var(--color-text-secondary)]">{ent.normalizedValue}</span>
                  </div>

                  <div className="flex items-center justify-between text-[11px] text-[var(--color-text-muted)] mb-3 pt-2 border-t border-[var(--color-border)]/60">
                    <div>
                      Confidence: <span className="font-semibold text-slate-200">{(ent.confidence * 100).toFixed(0)}%</span>
                    </div>
                    <div className="truncate max-w-[120px]" title={ent.sourceLocation}>
                      {ent.sourceLocation}
                    </div>
                  </div>

                  {/* Review Actions */}
                  <div className="flex items-center gap-1.5 pt-2 border-t border-[var(--color-border)]">
                    {!isApproved && (
                      <Button
                        size="sm"
                        variant="primary"
                        onClick={() => handleApproveEntity(ent.id)}
                        className="flex-1 text-xs py-1 h-7 bg-emerald-700 hover:bg-emerald-600 text-white"
                      >
                        Approve
                      </Button>
                    )}
                    <Button
                      size="sm"
                      variant="outline"
                      onClick={() => handleStartEdit(ent)}
                      className="px-2 text-xs py-1 h-7 border-[var(--color-border)] text-[var(--color-text-secondary)] hover:bg-[var(--color-surface-subtle)]"
                    >
                      <Edit3 className="w-3 h-3" />
                    </Button>
                    {!isRejected && (
                      <Button
                        size="sm"
                        variant="outline"
                        onClick={() => handleRejectEntity(ent.id)}
                        className="flex-1 text-xs py-1 h-7 border-[var(--color-border)] text-rose-300 hover:bg-rose-950/40 hover:border-rose-800"
                      >
                        Reject
                      </Button>
                    )}
                  </div>
                </Card>
              );
            })}
          </div>
        )}
      </div>

      {/* Extracted Relationships Section */}
      <div className="space-y-3 pt-4 border-t border-[var(--color-border)]">
        <div className="flex items-center justify-between">
          <h2 className="text-lg font-bold text-[var(--color-text-primary)] flex items-center gap-2">
            <Share2 className="w-5 h-5 text-indigo-400" />
            Extracted Relationships ({relationships.length})
          </h2>
          <div className="text-xs text-[var(--color-text-muted)]">
            Approved relationships will be created as edges in Neo4j with supporting provenance
          </div>
        </div>

        {relationships.length === 0 ? (
          <Card className="bg-[var(--color-surface)] border-[var(--color-border)] p-8 text-center text-[var(--color-text-muted)]">
            No grounded relationships detected in this evidence document.
          </Card>
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            {relationships.map((rel) => {
              const isApproved = rel.reviewStatus === 'APPROVED';
              const isRejected = rel.reviewStatus === 'REJECTED';

              return (
                <Card 
                  key={rel.id}
                  className={`p-4 border transition-all ${
                    isApproved ? 'bg-emerald-950/20 border-emerald-800/60' :
                    isRejected ? 'bg-[var(--color-surface-subtle)] border-[var(--color-border)] opacity-60' :
                    'bg-[var(--color-surface)] border-[var(--color-border)] hover:border-[var(--color-border)]'
                  }`}
                >
                  <div className="flex items-center justify-between mb-3">
                    <span className="text-xs text-[var(--color-text-muted)]">
                      Confidence: <span className="font-semibold text-slate-200">{(rel.confidence * 100).toFixed(0)}%</span>
                    </span>
                    <span className={`text-[11px] font-semibold px-2 py-0.5 rounded ${
                      isApproved ? 'text-emerald-400 bg-emerald-950 border border-emerald-800' :
                      isRejected ? 'text-rose-400 bg-rose-950 border border-rose-800' :
                      'text-amber-300 bg-amber-950/80 border border-amber-800'
                    }`}>
                      {rel.reviewStatus}
                    </span>
                  </div>

                  {/* Relationship Flow Representation */}
                  <div className="bg-[var(--color-surface-subtle)] p-3 rounded border border-[var(--color-border)]/80 mb-3 flex items-center justify-between">
                    <span className="font-semibold text-sm text-[var(--color-text-primary)] truncate max-w-[140px]" title={rel.sourceNormalizedValue}>
                      {rel.sourceNormalizedValue}
                    </span>
                    <span className="px-2 py-0.5 mx-2 rounded text-[11px] font-bold tracking-wider bg-indigo-950 text-indigo-300 border border-indigo-800 flex items-center gap-1">
                      &rarr; {rel.relationshipType} &rarr;
                    </span>
                    <span className="font-semibold text-sm text-[var(--color-text-primary)] truncate max-w-[140px]" title={rel.targetNormalizedValue}>
                      {rel.targetNormalizedValue}
                    </span>
                  </div>

                  <div className="text-xs text-[var(--color-text-muted)] mb-3">
                    <span className="font-semibold text-[var(--color-text-secondary)]">Citation:</span> {rel.sourceLocation}
                  </div>

                  {/* Actions */}
                  <div className="flex items-center gap-2 pt-2 border-t border-[var(--color-border)]">
                    {!isApproved && (
                      <Button
                        size="sm"
                        variant="primary"
                        onClick={() => handleApproveRelationship(rel.id)}
                        className="flex-1 text-xs py-1 h-7 bg-emerald-700 hover:bg-emerald-600 text-white"
                      >
                        Approve & Ingest to Graph
                      </Button>
                    )}
                    {!isRejected && (
                      <Button
                        size="sm"
                        variant="outline"
                        onClick={() => handleRejectRelationship(rel.id)}
                        className="flex-1 text-xs py-1 h-7 border-[var(--color-border)] text-rose-300 hover:bg-rose-950/40 hover:border-rose-800"
                      >
                        Reject
                      </Button>
                    )}
                  </div>
                </Card>
              );
            })}
          </div>
        )}
      </div>

      {/* Edit Entity Modal */}
      {editingEntity && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 backdrop-blur-sm p-4">
          <Card className="bg-[var(--color-surface)] border-[var(--color-border)] max-w-md w-full p-5 space-y-4 shadow-2xl">
            <h3 className="text-lg font-bold text-[var(--color-text-primary)] flex items-center gap-2">
              <Edit3 className="w-5 h-5 text-blue-400" />
              Correct Extracted Entity
            </h3>
            <p className="text-xs text-[var(--color-text-muted)]">
              Manual correction will be logged in the immutable audit trail before promotion.
            </p>

            <div className="space-y-3">
              <div>
                <label className="text-xs font-semibold text-[var(--color-text-secondary)] block mb-1">Raw Value (from document)</label>
                <input
                  type="text"
                  value={editRawValue}
                  onChange={(e) => setEditRawValue(e.target.value)}
                  className="w-full bg-[var(--color-surface)] border border-[var(--color-border)] rounded px-3 py-1.5 text-sm text-[var(--color-text-primary)] focus:outline-none focus:border-blue-500"
                />
              </div>

              <div>
                <label className="text-xs font-semibold text-[var(--color-text-secondary)] block mb-1">Normalized Value (graph index)</label>
                <input
                  type="text"
                  value={editNormalizedValue}
                  onChange={(e) => setEditNormalizedValue(e.target.value)}
                  className="w-full bg-[var(--color-surface)] border border-[var(--color-border)] rounded px-3 py-1.5 text-sm text-[var(--color-text-primary)] focus:outline-none focus:border-blue-500"
                />
              </div>
            </div>

            <div className="flex items-center justify-end gap-2 pt-2 border-t border-[var(--color-border)]">
              <Button
                variant="outline"
                size="sm"
                onClick={() => setEditingEntity(null)}
                className="border-[var(--color-border)] text-[var(--color-text-secondary)]"
              >
                Cancel
              </Button>
              <Button
                variant="primary"
                size="sm"
                onClick={handleSaveEdit}
                disabled={savingEdit || !editRawValue.trim()}
                className="bg-blue-600 hover:bg-blue-500 text-white"
              >
                {savingEdit ? 'Saving...' : 'Save Correction'}
              </Button>
            </div>
          </Card>
        </div>
      )}
    </div>
  );
};
