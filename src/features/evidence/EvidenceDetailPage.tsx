import React, { useState, useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { 
  FileText, 
  ArrowLeft, 
  ShieldCheck, 
  ShieldAlert, 
  Download, 
  RefreshCw, 
  CheckCircle, 
  Layers,
  Clock,
  User,
  Hash,
  Database,
  Eye
} from 'lucide-react';
import { EvidenceService, EvidenceDetail } from '../../services/evidence/evidence.service';
import { IntegrityService, EvidenceIntegrityStatus } from '../../services/integrity/integrity.service';
import { EvidenceIntegrityModal } from './components/EvidenceIntegrityModal';
import { apiClient } from '../../services/api/client';
import { Button } from '../../components/common/Button';
import { Card } from '../../components/common/Card';
import { PhaseNotice } from '../../components/common/PhaseNotice';
import { ResponsibleAiNotice } from '../../components/common/ResponsibleAiNotice';

import { translationService, TranslationResponseDto } from '../../services/translation/translation.service';
import { useTranslation } from '../../i18n';
import { Languages, AlertCircle as AlertCircleIcon } from 'lucide-react';

export const EvidenceDetailPage: React.FC = () => {
  const { evidenceId } = useParams<{ evidenceId: string }>();
  const navigate = useNavigate();
  const { t } = useTranslation();

  const [evidence, setEvidence] = useState<EvidenceDetail | null>(null);
  const [loading, setLoading] = useState(true);
  const [integrity, setIntegrity] = useState<EvidenceIntegrityStatus | null>(null);
  const [verifying, setVerifying] = useState(false);
  const [previewContent, setPreviewContent] = useState<string | null>(null);
  const [previewLoading, setPreviewLoading] = useState(false);
  const [isIntegrityModalOpen, setIsIntegrityModalOpen] = useState(false);

  // Phase 11 Non-destructive Translation State
  const [viewMode, setViewMode] = useState<'original' | 'translated'>('original');
  const [translatedData, setTranslatedData] = useState<TranslationResponseDto | null>(null);
  const [translating, setTranslating] = useState(false);
  const [targetLang, setTargetLang] = useState<'mr' | 'en'>('mr');

  const handleTranslate = async (lang: 'mr' | 'en') => {
    if (!previewContent) return;
    setTranslating(true);
    setTargetLang(lang);
    try {
      const res = await translationService.translate({
        text: previewContent,
        sourceLanguage: 'auto',
        targetLanguage: lang,
        evidenceId: evidenceId,
      });
      setTranslatedData(res);
      setViewMode('translated');
    } catch (e) {
      console.error('Translation error:', e);
    } finally {
      setTranslating(false);
    }
  };

  const fetchDetail = async () => {
    if (!evidenceId) return;
    setLoading(true);
    try {
      const data = await EvidenceService.getEvidenceDetail(evidenceId);
      setEvidence(data);

      // Load fast integrity status
      try {
        const integrityData = await IntegrityService.getEvidenceIntegrity(evidenceId);
        setIntegrity(integrityData);
      } catch (e) {
        console.error('Integrity fetch error:', e);
      }

      // Attempt to load preview if text/json/csv
      if (data.mimeType.includes('text') || data.mimeType.includes('json') || data.mimeType.includes('csv') || data.fileName.endsWith('.txt') || data.fileName.endsWith('.csv') || data.fileName.endsWith('.json')) {
        setPreviewLoading(true);
        try {
          const text = await apiClient.get<string>(`/api/v1/evidence/${evidenceId}/download`);
          setPreviewContent(typeof text === 'string' ? text : JSON.stringify(text, null, 2));
        } catch (e) {
          console.error('Preview load error:', e);
        } finally {
          setPreviewLoading(false);
        }
      }
    } catch (err) {
      console.error('Failed to load evidence detail:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleVerifyIntegrity = async () => {
    if (!evidenceId) return;
    setVerifying(true);
    try {
      const result = await IntegrityService.verifyEvidenceNow(evidenceId);
      setIntegrity(result);
    } catch (err) {
      console.error('Verification failed:', err);
    } finally {
      setVerifying(false);
    }
  };

  useEffect(() => {
    fetchDetail();
  }, [evidenceId]);

  if (loading) {
    return (
      <div className="flex items-center justify-center p-16">
        <RefreshCw className="w-8 h-8 text-blue-500 animate-spin mr-3" />
        <span className="text-[var(--color-text-secondary)] font-medium">Loading evidence record...</span>
      </div>
    );
  }

  if (!evidence) {
    return (
      <div className="p-8 text-center">
        <h2 className="text-xl font-bold text-[var(--color-text-primary)] mb-2">Evidence Not Found</h2>
        <p className="text-sm text-[var(--color-text-muted)] mb-4">Evidence item {evidenceId} could not be located in secure storage.</p>
        <Button onClick={() => navigate('/evidence')}>Return to Evidence List</Button>
      </div>
    );
  }

  const isImage = evidence.mimeType.startsWith('image/') || /\.(jpg|jpeg|png)$/i.test(evidence.fileName);
  const isPdf = evidence.mimeType.includes('pdf') || /\.pdf$/i.test(evidence.fileName);

  return (
    <div className="space-y-6 pb-12">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 border-b border-[var(--color-border)] pb-4">
        <div className="flex items-center space-x-3">
          <Button
            variant="outline"
            size="sm"
            onClick={() => navigate('/evidence')}
            className="flex items-center text-[var(--color-text-secondary)] border-[var(--color-border)] hover:bg-[var(--color-surface-subtle)]"
          >
            <ArrowLeft className="w-4 h-4 mr-1.5" /> Back
          </Button>
          <div>
            <h1 className="text-2xl font-bold text-[var(--color-text-primary)] flex items-center gap-2">
              <FileText className="w-6 h-6 text-blue-400" />
              {evidence.fileName}
            </h1>
            <p className="text-xs text-[var(--color-text-muted)]">
              Identifier: <span className="font-mono text-[var(--color-text-primary)]">{evidence.id}</span>
              {evidence.caseId && <> &bull; Case: <span className="font-mono text-[var(--color-text-primary)]">{evidence.caseId}</span></>}
              &bull; Version: <span className="font-bold text-blue-400">v{evidence.version}</span>
            </p>
          </div>
        </div>

        <div className="flex items-center gap-2">
          <Button
            variant="outline"
            size="sm"
            onClick={handleVerifyIntegrity}
            disabled={verifying}
            className="flex items-center border-[var(--color-border)] text-[var(--color-text-secondary)] hover:bg-[var(--color-surface-subtle)]"
          >
            <ShieldCheck className={`w-4 h-4 mr-1.5 text-emerald-400 ${verifying ? 'animate-spin' : ''}`} />
            {verifying ? 'Computing SHA-256...' : 'Verify Integrity'}
          </Button>

          <Link to={`/evidence/${evidence.id}/extraction`}>
            <Button variant="primary" size="sm" className="bg-blue-600 hover:bg-blue-500 text-white flex items-center gap-1.5">
              <Eye className="w-4 h-4" /> Review Extraction
            </Button>
          </Link>

          <a 
            href={`http://localhost:5000/api/v1/evidence/${evidence.id}/download`} 
            target="_blank" 
            rel="noreferrer"
            download={evidence.fileName}
          >
            <Button variant="outline" size="sm" className="border-[var(--color-border)] text-[var(--color-text-secondary)] hover:bg-[var(--color-surface-subtle)] flex items-center">
              <Download className="w-4 h-4 mr-1.5" /> Download
            </Button>
          </a>
        </div>
      </div>

      <PhaseNotice phaseNumber={2} moduleName="Evidence Processing & AI Extraction Engine" />
      <ResponsibleAiNotice />

      {/* Grid: Details & Integrity Card */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Metadata Details */}
        <Card className="lg:col-span-2 bg-[var(--color-surface)] border-[var(--color-border)] p-5 space-y-4">
          <h2 className="text-base font-bold text-[var(--color-text-primary)] flex items-center gap-2 border-b border-[var(--color-border)] pb-3">
            <Layers className="w-4 h-4 text-blue-400" />
            Evidence Provenance & Metadata
          </h2>

          <div className="grid grid-cols-2 sm:grid-cols-3 gap-4 text-xs">
            <div>
              <span className="text-slate-500 block mb-0.5">File Name</span>
              <span className="font-semibold text-[var(--color-text-primary)]">{evidence.fileName}</span>
            </div>

            <div>
              <span className="text-slate-500 block mb-0.5">Case Reference</span>
              <span className="font-mono text-[var(--color-text-primary)]">{evidence.caseId || 'Unassigned'}</span>
            </div>

            <div>
              <span className="text-slate-500 block mb-0.5">Processing Status</span>
              <span className="px-2 py-0.5 rounded text-[11px] font-bold bg-amber-950 text-amber-300 border border-amber-800">
                {evidence.processingStatus}
              </span>
            </div>

            <div>
              <span className="text-slate-500 block mb-0.5">Uploaded By</span>
              <span className="text-[var(--color-text-primary)]">{evidence.uploadedByName}</span>
            </div>

            <div>
              <span className="text-slate-500 block mb-0.5">Uploaded Timestamp</span>
              <span className="font-mono text-[var(--color-text-secondary)]">{new Date(evidence.uploadedAtUtc).toLocaleString()}</span>
            </div>

            <div>
              <span className="text-slate-500 block mb-0.5">File Size</span>
              <span className="text-[var(--color-text-primary)]">{(evidence.fileSize / 1024).toFixed(1)} KB ({evidence.fileSize} bytes)</span>
            </div>

            <div>
              <span className="text-slate-500 block mb-0.5">MIME Type</span>
              <span className="font-mono text-[var(--color-text-secondary)]">{evidence.mimeType}</span>
            </div>

            <div>
              <span className="text-slate-500 block mb-0.5">Clearance Classification</span>
              <span className="font-semibold text-amber-400">{evidence.clearance}</span>
            </div>

            <div>
              <span className="text-slate-500 block mb-0.5">Evidence Version</span>
              <span className="text-blue-400 font-bold">Version {evidence.version}</span>
            </div>
          </div>

          {evidence.description && (
            <div className="pt-3 border-t border-[var(--color-border)]">
              <span className="text-xs text-slate-500 block mb-1">Investigator Description / Chain of Custody</span>
              <p className="text-xs text-[var(--color-text-secondary)] leading-relaxed bg-[var(--color-surface-subtle)] p-3 rounded border border-[var(--color-border)]">
                {evidence.description}
              </p>
            </div>
          )}
        </Card>

        {/* Real Cryptographic Integrity Verification Card (Section 19 & 20) */}
        <Card className="bg-[var(--color-surface)] border-[var(--color-border)] p-5 space-y-4">
          <div className="flex items-center justify-between border-b border-[var(--color-border)] pb-3">
            <h2 className="text-base font-bold text-[var(--color-text-primary)] flex items-center gap-2">
              <ShieldCheck className="w-4 h-4 text-emerald-400" />
              Evidence Integrity
            </h2>
            <span className="text-[10px] uppercase font-mono px-2 py-0.5 rounded bg-blue-950 text-blue-300 border border-blue-800">
              Blockchain Ledger
            </span>
          </div>

          <div className="space-y-3">
            <div className={`p-3 rounded border text-center ${
              integrity?.status === 'VERIFIED'
                ? 'bg-emerald-950/40 border-emerald-800/80 text-emerald-300'
                : integrity?.status === 'EVIDENCE_MODIFIED' || integrity?.status === 'HASH_MISMATCH'
                ? 'bg-rose-950/40 border-rose-800 text-rose-300'
                : integrity?.status === 'LEDGER_MISMATCH'
                ? 'bg-amber-950/40 border-amber-800 text-amber-300'
                : 'bg-[var(--color-surface-subtle)] border-[var(--color-border)] text-[var(--color-text-muted)]'
            }`}>
              {integrity?.status === 'VERIFIED' ? (
                <div className="space-y-1">
                  <div className="flex items-center justify-center text-emerald-400 font-bold text-sm gap-1.5">
                    <CheckCircle className="w-5 h-5 text-emerald-400" />
                    &#x2713; VERIFIED
                  </div>
                  <p className="text-[11px] text-emerald-300/80">Stored bytes match registered cryptographic fingerprint</p>
                </div>
              ) : integrity?.status === 'EVIDENCE_MODIFIED' || integrity?.status === 'HASH_MISMATCH' ? (
                <div className="space-y-1">
                  <div className="flex items-center justify-center text-rose-400 font-bold text-sm gap-1.5">
                    <ShieldAlert className="w-5 h-5 text-rose-400" />
                    ⚠ TAMPER DETECTED
                  </div>
                  <p className="text-[11px] text-rose-300/80">File bytes on disk differ from registered hash!</p>
                </div>
              ) : integrity?.status === 'LEDGER_MISMATCH' ? (
                <div className="space-y-1">
                  <div className="flex items-center justify-center text-amber-400 font-bold text-sm gap-1.5">
                    <ShieldAlert className="w-5 h-5 text-amber-400" />
                    ⚠ LEDGER MISMATCH
                  </div>
                  <p className="text-[11px] text-amber-300/80">Physical bytes do not match ledger block hash</p>
                </div>
              ) : (
                <div className="text-xs text-[var(--color-text-muted)]">
                  {integrity?.explanation || 'Click "Verify Now" to compute live SHA-256.'}
                </div>
              )}
            </div>

            <div>
              <span className="text-[11px] font-semibold text-[var(--color-text-muted)] block mb-1">Evidence SHA-256</span>
              <div className="font-mono text-[11px] text-[var(--color-text-primary)] bg-[var(--color-surface-subtle)] p-2 rounded border border-[var(--color-border)] break-all select-all">
                {integrity?.actualFileSha256 || evidence.sha256Hash}
              </div>
            </div>

            <div>
              <span className="text-[11px] font-semibold text-[var(--color-text-muted)] block mb-1">Ledger SHA-256</span>
              <div className="font-mono text-[11px] text-[var(--color-text-primary)] bg-[var(--color-surface-subtle)] p-2 rounded border border-[var(--color-border)] break-all select-all">
                {integrity?.ledgerSha256 || 'Pending ledger registration'}
              </div>
            </div>

            <div className="grid grid-cols-2 gap-2 text-[11px] p-2.5 bg-[var(--color-surface-subtle)] rounded border border-[var(--color-border)]">
              <div>
                <span className="text-slate-500 block">Chain Status</span>
                <span className={`font-semibold ${integrity?.chainStatus === 'VALID' ? 'text-emerald-400' : 'text-[var(--color-text-muted)]'}`}>
                  {integrity?.chainStatus === 'VALID' ? '✓ VALID' : 'UNKNOWN'}
                </span>
              </div>
              <div>
                <span className="text-slate-500 block">Block</span>
                <span className="font-mono font-bold text-blue-400">
                  {integrity?.blockIndex !== null && integrity?.blockIndex !== undefined ? `#${integrity.blockIndex}` : 'None'}
                </span>
              </div>
              <div>
                <span className="text-slate-500 block">Recorded Action</span>
                <span className="font-medium text-[var(--color-text-secondary)]">{integrity?.action || 'UPLOAD'}</span>
              </div>
              <div>
                <span className="text-slate-500 block">Recorded By</span>
                <span className="font-medium text-[var(--color-text-secondary)]">{integrity?.actorName || evidence.uploadedByName}</span>
              </div>
            </div>

            <div className="pt-2 flex flex-col gap-2">
              <Button
                variant="outline"
                size="sm"
                onClick={handleVerifyIntegrity}
                disabled={verifying}
                className="w-full text-xs border-[var(--color-border)] hover:bg-[var(--color-surface-subtle)] text-[var(--color-text-primary)] flex items-center justify-center gap-1.5"
              >
                <RefreshCw className={`w-3.5 h-3.5 ${verifying ? 'animate-spin text-blue-400' : 'text-emerald-400'}`} />
                {verifying ? 'Recalculating Digest...' : 'Verify Now'}
              </Button>

              <Button
                variant="outline"
                size="sm"
                onClick={() => setIsIntegrityModalOpen(true)}
                className="w-full text-xs bg-[var(--color-surface-subtle)] border-[var(--color-border)] hover:bg-[var(--color-surface-subtle)] text-blue-400 flex items-center justify-center gap-1.5"
              >
                <ShieldCheck className="w-3.5 h-3.5" />
                View Integrity History
              </Button>
            </div>
          </div>
        </Card>
      </div>

      {/* Document / Content Viewer (Section 18 & Phase 11 Multilingual Translation) */}
      <Card className="bg-[var(--color-surface)] border-[var(--color-border)] p-5 space-y-4">
        <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-3 border-b border-[var(--color-border)] pb-3">
          <h2 className="text-base font-bold text-[var(--color-text-primary)] flex items-center gap-2">
            <Eye className="w-4 h-4 text-blue-400" />
            <span>{t('evidence.detail')}</span>
          </h2>

          {/* Translation controls */}
          {previewContent && (
            <div className="flex items-center gap-2">
              <div className="flex items-center rounded-lg bg-[var(--color-surface-subtle)] p-1 border border-[var(--color-border)] text-xs">
                <button
                  onClick={() => setViewMode('original')}
                  className={`px-3 py-1 rounded-md transition-colors font-medium cursor-pointer ${
                    viewMode === 'original'
                      ? 'bg-blue-600 text-white'
                      : 'text-[var(--color-text-muted)] hover:text-[var(--color-text-primary)]'
                  }`}
                >
                  {t('evidence.originalView')}
                </button>
                <button
                  onClick={() => {
                    if (!translatedData) {
                      handleTranslate('mr');
                    } else {
                      setViewMode('translated');
                    }
                  }}
                  disabled={translating}
                  className={`px-3 py-1 rounded-md transition-colors font-medium flex items-center gap-1.5 cursor-pointer ${
                    viewMode === 'translated'
                      ? 'bg-blue-600 text-white'
                      : 'text-[var(--color-text-muted)] hover:text-[var(--color-text-primary)]'
                  }`}
                >
                  <Languages className="w-3.5 h-3.5 text-blue-400" />
                  <span>{translating ? 'Translating...' : `${t('evidence.translatedView')} (मराठी)`}</span>
                </button>
              </div>

              {viewMode === 'translated' && (
                <button
                  onClick={() => handleTranslate(targetLang === 'mr' ? 'en' : 'mr')}
                  disabled={translating}
                  className="px-2.5 py-1 text-xs rounded bg-[var(--color-surface-subtle)] text-[var(--color-text-secondary)] border border-[var(--color-border)] hover:bg-[var(--color-surface-subtle)] flex items-center gap-1 cursor-pointer"
                  title="Switch target language"
                >
                  <RefreshCw className={`w-3 h-3 ${translating ? 'animate-spin' : ''}`} />
                  <span>{targetLang === 'mr' ? 'To English' : 'To मराठी'}</span>
                </button>
              )}
            </div>
          )}
        </div>

        {/* Machine Translation Disclaimer Alert */}
        {viewMode === 'translated' && (
          <div className="p-3 rounded-lg bg-amber-500/10 border border-amber-500/30 text-xs text-amber-300 space-y-1">
            <div className="flex items-center justify-between">
              <div className="flex items-center gap-2 font-bold uppercase tracking-wider text-[11px]">
                <AlertCircleIcon className="w-4 h-4 text-amber-400 shrink-0" />
                <span>{t('evidence.machineTranslation')}</span>
              </div>
              <span className="text-[10px] font-mono px-2 py-0.5 rounded bg-amber-950 text-amber-300 border border-amber-800">
                {translatedData?.provider || 'SystemDictionary'}
              </span>
            </div>
            <p className="text-[11px] text-amber-200/90 leading-relaxed">
              {translatedData?.disclaimer || t('evidence.machineTranslationDisclaimer')}
            </p>
          </div>
        )}

        {previewLoading ? (
          <div className="p-8 text-center text-[var(--color-text-muted)] text-xs">Loading document preview stream...</div>
        ) : isImage ? (
          <div className="flex justify-center p-4 bg-[var(--color-surface-subtle)] rounded border border-[var(--color-border)]">
            <img 
              src={`http://localhost:5000/api/v1/evidence/${evidence.id}/download`} 
              alt={evidence.fileName}
              className="max-h-[500px] object-contain rounded"
            />
          </div>
        ) : isPdf ? (
          <div className="w-full h-[600px] bg-[var(--color-surface-subtle)] rounded border border-[var(--color-border)] overflow-hidden">
            <iframe
              src={`http://localhost:5000/api/v1/evidence/${evidence.id}/download`}
              title={evidence.fileName}
              className="w-full h-full border-0"
            />
          </div>
        ) : viewMode === 'translated' && translatedData ? (
          <div className="space-y-2">
            <div className="flex items-center justify-between text-[11px] font-mono text-[var(--color-text-muted)] px-1">
              <span>Translated Content ({translatedData.targetLanguage.toUpperCase()})</span>
              <span>SHA-256 remains immutable: {evidence.sha256Hash.slice(0, 16)}...</span>
            </div>
            <pre className="p-4 bg-[var(--color-surface-subtle)] text-[var(--color-text-primary)] text-xs font-mono rounded border border-[var(--color-border)] overflow-x-auto max-h-[500px] leading-relaxed whitespace-pre-wrap">
              {translatedData.translatedText}
            </pre>
          </div>
        ) : previewContent ? (
          <div className="space-y-2">
            <div className="flex items-center justify-between text-[11px] font-mono text-[var(--color-text-muted)] px-1">
              <span>Original Content</span>
              <span>SHA-256: {evidence.sha256Hash.slice(0, 16)}...</span>
            </div>
            <pre className="p-4 bg-[var(--color-surface-subtle)] text-[var(--color-text-primary)] text-xs font-mono rounded border border-[var(--color-border)] overflow-x-auto max-h-[500px] leading-relaxed whitespace-pre-wrap">
              {previewContent}
            </pre>
          </div>
        ) : (
          <div className="p-8 text-center text-[var(--color-text-muted)] text-xs">
            Direct browser preview not supported for {evidence.mimeType}. Use the Download button above to inspect file.
          </div>
        )}
      </Card>

      {/* Phase 9 Evidence Integrity Modal (Full Chain Inspector & Timeline) */}
      <EvidenceIntegrityModal
        isOpen={isIntegrityModalOpen}
        onClose={() => setIsIntegrityModalOpen(false)}
        evidenceId={evidence.id}
        fileName={evidence.fileName}
      />
    </div>
  );
};
