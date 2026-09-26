import React, { useState, useEffect } from 'react';
import {
  ShieldCheck,
  ShieldAlert,
  CheckCircle,
  AlertTriangle,
  RefreshCw,
  Copy,
  Check,
  Clock,
  User,
  Hash,
  Link as LinkIcon,
  X,
  FileText,
  ChevronDown,
  ChevronUp,
  Activity
} from 'lucide-react';
import {
  IntegrityService,
  EvidenceIntegrityStatus,
  EvidenceLedgerBlock,
  ChainValidationResult
} from '../../../services/integrity/integrity.service';
import { Button } from '../../../components/common/Button';

interface EvidenceIntegrityModalProps {
  evidenceId: string;
  fileName?: string;
  isOpen: boolean;
  onClose: () => void;
}

export const EvidenceIntegrityModal: React.FC<EvidenceIntegrityModalProps> = ({
  evidenceId,
  fileName,
  isOpen,
  onClose
}) => {
  const [loading, setLoading] = useState(true);
  const [verifying, setVerifying] = useState(false);
  const [status, setStatus] = useState<EvidenceIntegrityStatus | null>(null);
  const [history, setHistory] = useState<EvidenceLedgerBlock[]>([]);
  const [showHistory, setShowHistory] = useState(false);
  const [chainResult, setChainResult] = useState<ChainValidationResult | null>(null);
  const [verifyingChain, setVerifyingChain] = useState(false);
  const [reconciling, setReconciling] = useState(false);
  const [copiedKey, setCopiedKey] = useState<string | null>(null);

  const loadIntegrityData = async () => {
    if (!evidenceId) return;
    setLoading(true);
    try {
      const [statusRes, historyRes] = await Promise.all([
        IntegrityService.getEvidenceIntegrity(evidenceId),
        IntegrityService.getEvidenceHistory(evidenceId)
      ]);
      setStatus(statusRes);
      setHistory(historyRes);
    } catch (err) {
      console.error('Failed to load evidence integrity data:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (isOpen && evidenceId) {
      loadIntegrityData();
    }
  }, [isOpen, evidenceId]);

  const handleVerifyNow = async () => {
    if (!evidenceId) return;
    setVerifying(true);
    try {
      const result = await IntegrityService.verifyEvidenceNow(evidenceId);
      setStatus(result);
      const historyRes = await IntegrityService.getEvidenceHistory(evidenceId);
      setHistory(historyRes);
    } catch (err) {
      console.error('Live verification failed:', err);
    } finally {
      setVerifying(false);
    }
  };

  const handleVerifyChain = async () => {
    setVerifyingChain(true);
    try {
      const res = await IntegrityService.verifyChain();
      setChainResult(res);
    } catch (err) {
      console.error('Chain verification failed:', err);
    } finally {
      setVerifyingChain(false);
    }
  };

  const handleReconcile = async () => {
    if (!evidenceId) return;
    setReconciling(true);
    try {
      await IntegrityService.reconcileEvidence(evidenceId);
      await loadIntegrityData();
    } catch (err) {
      console.error('Reconciliation failed:', err);
    } finally {
      setReconciling(false);
    }
  };

  const copyToClipboard = (text: string, key: string) => {
    navigator.clipboard.writeText(text);
    setCopiedKey(key);
    setTimeout(() => setCopiedKey(null), 2000);
  };

  if (!isOpen) return null;

  const isVerified = status?.status === 'VERIFIED';
  const isTampered = status?.status === 'EVIDENCE_MODIFIED' || status?.status === 'HASH_MISMATCH';
  const isLedgerMismatch = status?.status === 'LEDGER_MISMATCH';
  const isMissing = status?.status === 'MISSING_FILE';
  const isPending = status?.status === 'MISSING_LEDGER_RECORD';

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-sm animate-in fade-in duration-200">
      <div className="relative w-full max-w-3xl max-h-[90vh] overflow-y-auto bg-[var(--color-surface)] border border-[var(--color-border)] rounded-xl shadow-2xl text-[var(--color-text-primary)] flex flex-col">
        {/* Modal Header */}
        <div className="sticky top-0 z-10 flex items-center justify-between px-6 py-4 bg-[var(--color-surface)]/95 backdrop-blur border-b border-[var(--color-border)]">
          <div className="flex items-center space-x-3">
            <div className={`p-2 rounded-lg ${isVerified ? 'bg-emerald-950/60 text-emerald-400 border border-emerald-700/50' : 'bg-rose-950/60 text-rose-400 border border-rose-700/50'}`}>
              <ShieldCheck className="w-6 h-6" />
            </div>
            <div>
              <h2 className="text-lg font-bold text-[var(--color-text-primary)] flex items-center gap-2">
                Evidence Integrity Ledger
                <span className="text-[10px] uppercase font-mono px-2 py-0.5 rounded bg-blue-950 text-blue-300 border border-blue-800">
                  Append-Only Blockchain
                </span>
              </h2>
              <p className="text-xs text-[var(--color-text-muted)] font-mono">
                {fileName || status?.fileName || evidenceId} &bull; ID: {evidenceId}
              </p>
            </div>
          </div>
          <button
            onClick={onClose}
            className="p-1.5 text-[var(--color-text-muted)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-surface-subtle)] rounded-md transition-colors"
          >
            <X className="w-5 h-5" />
          </button>
        </div>

        {/* Modal Body */}
        <div className="p-6 space-y-6 flex-1">
          {loading ? (
            <div className="flex flex-col items-center justify-center py-12 space-y-3">
              <RefreshCw className="w-8 h-8 text-blue-500 animate-spin" />
              <span className="text-sm text-[var(--color-text-muted)]">Loading cryptographic ledger state...</span>
            </div>
          ) : !status ? (
            <div className="p-4 bg-[var(--color-surface-subtle)] rounded-lg text-center text-[var(--color-text-muted)] text-sm">
              Unable to load integrity records for this evidence item.
            </div>
          ) : (
            <>
              {/* Primary Status Banner */}
              <div
                className={`p-4 rounded-xl border flex flex-col sm:flex-row sm:items-center justify-between gap-4 ${
                  isVerified
                    ? 'bg-emerald-950/30 border-emerald-800/80 text-emerald-300'
                    : isTampered
                    ? 'bg-rose-950/40 border-rose-800 text-rose-300'
                    : isLedgerMismatch
                    ? 'bg-amber-950/40 border-amber-800 text-amber-300'
                    : isPending
                    ? 'bg-blue-950/40 border-blue-800 text-blue-300'
                    : 'bg-rose-950/40 border-rose-800 text-rose-300'
                }`}
              >
                <div className="space-y-1">
                  <div className="flex items-center gap-2">
                    {isVerified ? (
                      <>
                        <CheckCircle className="w-6 h-6 text-emerald-400 flex-shrink-0" />
                        <span className="text-base font-bold tracking-wide">
                          ✓ CRYPTOGRAPHICALLY VERIFIED
                        </span>
                      </>
                    ) : isTampered ? (
                      <>
                        <ShieldAlert className="w-6 h-6 text-rose-400 flex-shrink-0" />
                        <span className="text-base font-bold tracking-wide">
                          ⚠ TAMPER DETECTED — FILE MODIFIED
                        </span>
                      </>
                    ) : isLedgerMismatch ? (
                      <>
                        <AlertTriangle className="w-6 h-6 text-amber-400 flex-shrink-0" />
                        <span className="text-base font-bold tracking-wide">
                          ⚠ LEDGER MISMATCH DETECTED
                        </span>
                      </>
                    ) : isPending ? (
                      <>
                        <Activity className="w-6 h-6 text-blue-400 flex-shrink-0" />
                        <span className="text-base font-bold tracking-wide">
                          PENDING LEDGER REGISTRATION
                        </span>
                      </>
                    ) : (
                      <>
                        <AlertTriangle className="w-6 h-6 text-rose-400 flex-shrink-0" />
                        <span className="text-base font-bold tracking-wide">
                          ⚠ {status.status.replace(/_/g, ' ')}
                        </span>
                      </>
                    )}
                  </div>
                  <p className="text-xs opacity-90 leading-relaxed max-w-xl">
                    {status.explanation}
                  </p>
                </div>

                <div className="flex items-center gap-2 flex-shrink-0">
                  <Button
                    variant="primary"
                    size="sm"
                    onClick={handleVerifyNow}
                    disabled={verifying}
                    className="bg-blue-600 hover:bg-blue-500 text-white font-medium text-xs flex items-center gap-1.5 shadow-lg shadow-blue-900/30"
                  >
                    <RefreshCw className={`w-3.5 h-3.5 ${verifying ? 'animate-spin' : ''}`} />
                    {verifying ? 'Recalculating File Bytes...' : 'Verify Now'}
                  </Button>

                  {isPending && (
                    <Button
                      variant="secondary"
                      size="sm"
                      onClick={handleReconcile}
                      disabled={reconciling}
                      className="text-xs bg-[var(--color-surface-subtle)] hover:bg-[var(--color-surface-subtle)] text-[var(--color-text-primary)] border-[var(--color-border)]"
                    >
                      {reconciling ? 'Registering...' : 'Register Ledger Block'}
                    </Button>
                  )}
                </div>
              </div>

              {/* SHA-256 Fingerprint Comparison Grid */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                {/* Physical File Hash */}
                <div className="p-4 bg-[var(--color-surface-subtle)] rounded-lg border border-[var(--color-border)] space-y-2">
                  <div className="flex items-center justify-between text-xs text-[var(--color-text-muted)]">
                    <span className="font-semibold flex items-center gap-1.5 text-[var(--color-text-secondary)]">
                      <FileText className="w-3.5 h-3.5 text-blue-400" />
                      Physical File SHA-256 (Disk Stream)
                    </span>
                    <button
                      onClick={() => copyToClipboard(status.actualFileSha256 || status.registeredSha256, 'actual')}
                      className="text-[var(--color-text-muted)] hover:text-[var(--color-text-primary)] p-1"
                      title="Copy SHA-256"
                    >
                      {copiedKey === 'actual' ? <Check className="w-3.5 h-3.5 text-emerald-400" /> : <Copy className="w-3.5 h-3.5" />}
                    </button>
                  </div>
                  <div className="font-mono text-xs text-[var(--color-text-primary)] bg-[var(--color-surface-subtle)] p-2.5 rounded border border-[var(--color-border)] break-all select-all">
                    {status.actualFileSha256 || status.registeredSha256 || 'Pending byte recalculation'}
                  </div>
                  <div className="text-[11px] text-[var(--color-text-muted)]">
                    Calculated at runtime from raw bytes in storage server.
                  </div>
                </div>

                {/* Ledger Record Hash */}
                <div className="p-4 bg-[var(--color-surface-subtle)] rounded-lg border border-[var(--color-border)] space-y-2">
                  <div className="flex items-center justify-between text-xs text-[var(--color-text-muted)]">
                    <span className="font-semibold flex items-center gap-1.5 text-[var(--color-text-secondary)]">
                      <LinkIcon className="w-3.5 h-3.5 text-emerald-400" />
                      Ledger Block EvidenceHash
                    </span>
                    <button
                      onClick={() => copyToClipboard(status.ledgerSha256, 'ledger')}
                      className="text-[var(--color-text-muted)] hover:text-[var(--color-text-primary)] p-1"
                      title="Copy Hash"
                    >
                      {copiedKey === 'ledger' ? <Check className="w-3.5 h-3.5 text-emerald-400" /> : <Copy className="w-3.5 h-3.5" />}
                    </button>
                  </div>
                  <div className="font-mono text-xs text-[var(--color-text-primary)] bg-[var(--color-surface-subtle)] p-2.5 rounded border border-[var(--color-border)] break-all select-all">
                    {status.ledgerSha256 || 'No ledger entry recorded'}
                  </div>
                  <div className="text-[11px] text-[var(--color-text-muted)]">
                    Immutable fingerprint registered in Block #{status.blockIndex ?? '—'}.
                  </div>
                </div>
              </div>

              {/* Custody Metadata Card */}
              <div className="p-4 bg-[var(--color-surface-subtle)]/60 rounded-lg border border-[var(--color-border)] grid grid-cols-2 sm:grid-cols-4 gap-4 text-xs">
                <div>
                  <span className="text-[var(--color-text-muted)] block mb-1">Block Index</span>
                  <span className="font-mono font-bold text-blue-400 text-sm">
                    {status.blockIndex !== null && status.blockIndex !== undefined ? `#${status.blockIndex}` : 'None'}
                  </span>
                </div>

                <div>
                  <span className="text-[var(--color-text-muted)] block mb-1">Recorded Action</span>
                  <span className="px-2 py-0.5 rounded text-[11px] font-bold bg-[var(--color-surface-subtle)] text-[var(--color-text-secondary)] border border-[var(--color-border)]">
                    {status.action || 'UPLOAD'}
                  </span>
                </div>

                <div>
                  <span className="text-[var(--color-text-muted)] block mb-1">Recorded By</span>
                  <span className="font-medium text-[var(--color-text-primary)] flex items-center gap-1">
                    <User className="w-3 h-3 text-[var(--color-text-muted)]" />
                    {status.actorName || 'System'}
                  </span>
                </div>

                <div>
                  <span className="text-[var(--color-text-muted)] block mb-1">Chain Status</span>
                  <span className={`font-semibold flex items-center gap-1 ${status.chainStatus === 'VALID' ? 'text-emerald-400' : 'text-rose-400'}`}>
                    <CheckCircle className="w-3 h-3" />
                    {status.chainStatus === 'VALID' ? '✓ VALID' : '⚠ BROKEN'}
                  </span>
                </div>
              </div>

              {/* Chain Health Summary Bar */}
              <div className="flex flex-wrap items-center justify-between gap-3 p-3 bg-[var(--color-surface-subtle)] rounded-lg border border-[var(--color-border)] text-xs">
                <div className="flex items-center gap-3">
                  <div className="flex items-center gap-1.5 text-emerald-400 font-semibold">
                    <span className="w-2 h-2 rounded-full bg-emerald-500 animate-pulse"></span>
                    Evidence Ledger: {chainResult?.isValid ?? true ? '✓ Chain Valid' : '⚠ Integrity Failure'}
                  </div>
                  {chainResult && (
                    <span className="text-[var(--color-text-muted)] font-mono">
                      ({chainResult.totalBlocks} total blocks verified)
                    </span>
                  )}
                </div>

                <Button
                  variant="outline"
                  size="sm"
                  onClick={handleVerifyChain}
                  disabled={verifyingChain}
                  className="text-xs border-[var(--color-border)] text-[var(--color-text-secondary)] hover:bg-[var(--color-surface-subtle)]"
                >
                  <Activity className={`w-3.5 h-3.5 mr-1.5 text-blue-400 ${verifyingChain ? 'animate-spin' : ''}`} />
                  {verifyingChain ? 'Auditing Complete Ledger...' : 'Audit Whole Blockchain'}
                </Button>
              </div>

              {/* Toggle Timeline History */}
              <div className="border-t border-[var(--color-border)] pt-4">
                <button
                  onClick={() => setShowHistory(!showHistory)}
                  className="flex items-center justify-between w-full p-2.5 rounded-lg bg-[var(--color-surface-subtle)] hover:bg-[var(--color-surface-subtle)] text-xs font-semibold text-[var(--color-text-secondary)] border border-[var(--color-border)] transition-colors"
                >
                  <span className="flex items-center gap-2">
                    <Clock className="w-4 h-4 text-blue-400" />
                    Evidence Custody Timeline ({history.length} {history.length === 1 ? 'block' : 'blocks'})
                  </span>
                  {showHistory ? <ChevronUp className="w-4 h-4 text-[var(--color-text-muted)]" /> : <ChevronDown className="w-4 h-4 text-[var(--color-text-muted)]" />}
                </button>

                {showHistory && (
                  <div className="mt-4 space-y-3">
                    {history.length === 0 ? (
                      <div className="p-4 text-center text-xs text-[var(--color-text-muted)] bg-[var(--color-surface-subtle)] rounded border border-[var(--color-border)]">
                        No ledger history recorded for this evidence item.
                      </div>
                    ) : (
                      history.map((block, idx) => (
                        <div
                          key={block.id}
                          className="relative p-3.5 bg-[var(--color-surface-subtle)] rounded-lg border border-[var(--color-border)] space-y-2 text-xs"
                        >
                          <div className="flex items-center justify-between">
                            <div className="flex items-center gap-2">
                              <span className="font-mono font-bold text-blue-400 text-xs bg-blue-950/60 px-2 py-0.5 rounded border border-blue-900">
                                BLOCK #{block.blockIndex}
                              </span>
                              <span className="px-2 py-0.5 rounded text-[10px] font-semibold bg-[var(--color-surface-subtle)] text-[var(--color-text-secondary)] border border-[var(--color-border)]">
                                {block.action}
                              </span>
                              <span className="text-[var(--color-text-muted)] text-[11px]">
                                v{block.evidenceVersion}
                              </span>
                            </div>

                            <span className="text-[11px] text-[var(--color-text-muted)] font-mono">
                              {new Date(block.timestampUtc).toLocaleString()}
                            </span>
                          </div>

                          <div className="grid grid-cols-1 sm:grid-cols-2 gap-2 font-mono text-[11px] pt-1">
                            <div>
                              <span className="text-[var(--color-text-muted)] block text-[10px]">Evidence Hash</span>
                              <span className="text-[var(--color-text-secondary)] break-all select-all">{block.evidenceHash}</span>
                            </div>
                            <div>
                              <span className="text-[var(--color-text-muted)] block text-[10px]">Block Hash</span>
                              <span className="text-emerald-400/90 break-all select-all">{block.blockHash}</span>
                            </div>
                          </div>

                          <div className="flex items-center justify-between text-[11px] text-[var(--color-text-muted)] pt-1 border-t border-slate-850">
                            <span className="flex items-center gap-1">
                              <User className="w-3 h-3 text-[var(--color-text-muted)]" />
                              Recorded by: <strong className="text-[var(--color-text-secondary)]">{block.actorName}</strong>
                            </span>
                            <span className="text-[var(--color-text-muted)] font-mono text-[10px]">
                              Prev: {block.previousBlockHash.slice(0, 16)}...
                            </span>
                          </div>
                        </div>
                      ))
                    )}
                  </div>
                )}
              </div>

              {/* Informational Disclaimer (SIH / Neutral Terminology) */}
              <div className="p-3 bg-[var(--color-surface-subtle)]/50 rounded-lg border border-[var(--color-border)] text-[11px] text-[var(--color-text-muted)] space-y-1">
                <span className="font-semibold text-[var(--color-text-secondary)] block">Investigative Principles Notice:</span>
                <p>
                  Evidence integrity establishes whether the stored forensic artifact matches its cryptographically registered fingerprint. It verifies non-repudiation and chain-of-custody, but does not infer guilt, intent, or culpability.
                </p>
              </div>
            </>
          )}
        </div>

        {/* Modal Footer */}
        <div className="px-6 py-3 bg-[var(--color-surface)]/95 border-t border-[var(--color-border)] flex justify-end">
          <Button variant="outline" size="sm" onClick={onClose} className="border-[var(--color-border)] text-[var(--color-text-secondary)] hover:bg-[var(--color-surface-subtle)]">
            Close Panel
          </Button>
        </div>
      </div>
    </div>
  );
};
