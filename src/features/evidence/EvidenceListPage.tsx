import React, { useState, useEffect, useRef } from 'react';
import { Link } from 'react-router-dom';
import { Upload, FileCheck2, Loader2, RefreshCw, Eye, ShieldCheck, CheckCircle, ShieldAlert } from 'lucide-react';
import { useActiveInvestigation } from '../../hooks/useActiveInvestigation';
import { apiClient } from '../../services/api/client';
import { EvidenceService } from '../../services/evidence/evidence.service';
import { IntegrityService, EvidenceIntegrityStatus } from '../../services/integrity/integrity.service';
import { EvidenceIntegrityModal } from './components/EvidenceIntegrityModal';
import { Button } from '../../components/common/Button';
import { StatusBadge } from '../../components/common/StatusBadge';
import { Card } from '../../components/common/Card';
import { PhaseNotice } from '../../components/common/PhaseNotice';
import { ResponsibleAiNotice } from '../../components/common/ResponsibleAiNotice';

interface Evidence {
  id: string;
  filename: string;
  file_type: string;
  description: string;
  hash_sha256: string;
  uploaded_by: string;
  clearance: string;
  created_at: string;
  version: number;
  processing_status: string;
}

export const EvidenceListPage: React.FC = () => {
  const { activeCaseId, activeCaseNumber, activeCaseTitle } = useActiveInvestigation();
  const [evidenceList, setEvidenceList] = useState<Evidence[]>([]);
  const [loading, setLoading] = useState(true);
  const [uploading, setUploading] = useState(false);
  const [verifyingId, setVerifyingId] = useState<string | null>(null);
  const [integrityResults, setIntegrityResults] = useState<Record<string, EvidenceIntegrityStatus>>({});
  const [modalEvidence, setModalEvidence] = useState<{ id: string; fileName: string } | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const fetchEvidence = async () => {
    setLoading(true);
    try {
      let rawData: any[];
      try {
        rawData = await apiClient.get<any[]>(`/api/v1/evidence?caseId=${activeCaseId || ''}`);
      } catch {
        rawData = await apiClient.get<any[]>(`/api/evidence?investigation_id=${activeCaseId || ''}`);
      }
      const data: Evidence[] = (rawData || []).map((d: any) => ({
        id: d.id,
        filename: d.filename || d.fileName || 'evidence_file',
        file_type: d.file_type || d.fileType || (d.fileName ? d.fileName.split('.').pop()?.toUpperCase() : 'FILE'),
        description: d.description || '',
        hash_sha256: d.hash_sha256 || d.sha256Hash || '',
        uploaded_by: d.uploaded_by || d.uploadedByName || 'Officer',
        clearance: d.clearance || 'RESTRICTED',
        created_at: d.created_at || d.uploadedAtUtc || new Date().toISOString(),
        version: d.version || 1,
        processing_status: d.processing_status || d.processingStatus || 'UPLOADED'
      }));
      setEvidenceList(data);
    } catch (err) {
      console.error('Evidence fetch error:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchEvidence();
  }, [activeCaseId]);

  const handleFileChange = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;

    if (!activeCaseId) {
      alert('Please set an active scope (investigation) from the Investigations page first.');
      return;
    }

    setUploading(true);
    try {
      const formData = new FormData();
      formData.append('file', file);
      formData.append('caseId', activeCaseId);
      formData.append('description', `Uploaded evidence: ${file.name}`);
      formData.append('clearance', 'RESTRICTED');

      await apiClient.post('/api/v1/evidence/upload', formData);
      await fetchEvidence();
    } catch (err: any) {
      console.error('Evidence upload error:', err);
      alert(`Upload failed: ${err?.message || 'Server error'}`);
    } finally {
      setUploading(false);
      if (fileInputRef.current) fileInputRef.current.value = '';
    }
  };

  const handleVerify = async (evId: string) => {
    setVerifyingId(evId);
    try {
      const res = await IntegrityService.verifyEvidenceNow(evId);
      setIntegrityResults(prev => ({ ...prev, [evId]: res }));
    } catch (err) {
      console.error('Verification error:', err);
    } finally {
      setVerifyingId(null);
    }
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    if (e.dataTransfer.files && e.dataTransfer.files[0]) {
      const droppedFile = e.dataTransfer.files[0];
      const dataTransfer = new DataTransfer();
      dataTransfer.items.add(droppedFile);
      if (fileInputRef.current) {
        fileInputRef.current.files = dataTransfer.files;
        handleFileChange({ target: { files: dataTransfer.files } } as any);
      }
    }
  };

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4">
        <div>
          <h1 className="text-2xl font-bold text-[var(--color-text-primary)] flex items-center gap-2">
            <FileCheck2 className="w-6 h-6 text-[var(--color-accent)]" />
            Evidence Repository & Intake
          </h1>
          <p className="text-xs text-[var(--color-text-secondary)] mt-1 font-mono">
            Active Scope: <span className="font-semibold text-[var(--color-accent)]">{activeCaseNumber || 'Global / Unscoped'}</span>
            {activeCaseTitle ? ` — ${activeCaseTitle}` : ''}
          </p>
        </div>

        <div className="flex items-center gap-2">
          <Button variant="secondary" size="md" onClick={fetchEvidence} disabled={loading} icon={<RefreshCw className={`w-3.5 h-3.5 ${loading ? 'animate-spin' : ''}`} />}>
            Refresh
          </Button>
          <input type="file" ref={fileInputRef} onChange={handleFileChange} className="hidden" />
          <Button variant="primary" size="md" onClick={() => fileInputRef.current?.click()} disabled={uploading} icon={uploading ? <Loader2 className="w-3.5 h-3.5 animate-spin" /> : <Upload className="w-3.5 h-3.5" />}>
            {uploading ? 'Uploading & Hashing...' : 'Upload Evidence File'}
          </Button>
        </div>
      </div>

      <PhaseNotice phaseNumber={2} moduleName="Evidence Processing & AI Extraction Engine" />
      <ResponsibleAiNotice compact />

      {/* Upload Drop Zone */}
      <div 
        className="border-2 border-dashed border-[var(--color-border)] hover:border-[var(--color-accent)] rounded-xl p-6 text-center bg-[var(--color-surface)] transition-colors cursor-pointer"
        onDrop={handleDrop}
        onDragOver={handleDragOver}
        onClick={() => fileInputRef.current?.click()}
      >
        <div className="w-10 h-10 rounded-full bg-[var(--color-surface)] border border-[var(--color-border)] flex items-center justify-center text-[var(--color-accent)] mx-auto mb-2 shadow-xs">
          {uploading ? <Loader2 className="w-5 h-5 animate-spin" /> : <Upload className="w-5 h-5" />}
        </div>
        <h3 className="text-xs font-semibold text-[var(--color-text-primary)]">
          Upload primary documents or data extracts
        </h3>
        <p className="text-[11px] text-[var(--color-text-secondary)] mt-0.5">
          PDF, CSV, XLSX, JSON, TXT, or Image files (automatic SHA-256 byte calculation on intake)
        </p>
      </div>

      {/* Evidence Table */}
      <Card
        title="Ingested Evidence Registry"
        subtitle={activeCaseTitle ? `Current items linked to ${activeCaseTitle}` : 'All uploaded evidence items'}
      >
        <div className="overflow-x-auto">
          <table className="w-full text-left text-xs">
            <thead>
              <tr className="border-b border-[var(--color-border)] text-[var(--color-text-muted)] font-mono text-[11px]">
                <th className="pb-3 font-semibold">Document Filename</th>
                <th className="pb-3 font-semibold">Version</th>
                <th className="pb-3 font-semibold">Type</th>
                <th className="pb-3 font-semibold">Processing State</th>
                <th className="pb-3 font-semibold">Clearance</th>
                <th className="pb-3 font-semibold">SHA-256 Provenance</th>
                <th className="pb-3 font-semibold text-right">Actions</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-[#EFE5D8]">
              {loading && evidenceList.length === 0 ? (
                <tr>
                  <td colSpan={7} className="py-6 text-center text-[var(--color-text-muted)]">Loading evidence...</td>
                </tr>
              ) : evidenceList.length === 0 ? (
                <tr>
                  <td colSpan={7} className="py-6 text-center text-[var(--color-text-muted)]">No evidence items found.</td>
                </tr>
              ) : (
                evidenceList.map((item) => {
                  const check = integrityResults[item.id];
                  const isVerifying = verifyingId === item.id;

                  return (
                    <tr key={item.id} className="hover:bg-[var(--color-surface)] transition-colors">
                      <td className="py-3 pr-3 font-medium text-[var(--color-text-primary)]">
                        <div className="flex items-center gap-2">
                          <FileCheck2 className="w-4 h-4 text-[var(--color-accent)] shrink-0" />
                          <div>
                            <Link to={`/evidence/${item.id}`} className="hover:text-[var(--color-accent)] hover:underline font-semibold">
                              {item.filename}
                            </Link>
                            <p className="text-[10px] text-[var(--color-text-muted)] font-mono mt-0.5">{new Date(item.created_at).toLocaleString()}</p>
                          </div>
                        </div>
                      </td>

                      <td className="py-3 px-2">
                        <span className="font-mono text-[10px] px-1.5 py-0.5 rounded bg-blue-100 text-blue-800 font-bold">
                          v{item.version}
                        </span>
                      </td>

                      <td className="py-3 px-2">
                        <span className="font-mono text-[10px] px-1.5 py-0.5 rounded bg-[var(--color-surface-subtle)] text-[var(--color-text-primary)] border border-[var(--color-border)]">
                          {item.file_type}
                        </span>
                      </td>

                      <td className="py-3 px-2">
                        <span className={`px-2 py-0.5 rounded text-[10px] font-semibold uppercase tracking-wider ${
                          item.processing_status === 'APPROVED' ? 'bg-emerald-100 text-emerald-800 border border-emerald-300' :
                          item.processing_status === 'REVIEW_REQUIRED' ? 'bg-amber-100 text-amber-900 border border-amber-300' :
                          item.processing_status === 'FAILED' ? 'bg-rose-100 text-rose-800 border border-rose-300' :
                          'bg-slate-100 text-slate-700 border border-slate-300'
                        }`}>
                          {item.processing_status}
                        </span>
                      </td>

                      <td className="py-3 px-2">
                        <StatusBadge status={item.clearance} size="sm" />
                      </td>

                      <td className="py-3 px-2 font-mono text-[10px] text-[var(--color-text-muted)]">
                        <div className="flex items-center gap-1.5">
                          {check ? (
                            check.status === 'VERIFIED' ? (
                              <button
                                onClick={() => setModalEvidence({ id: item.id, fileName: item.filename })}
                                className="flex items-center text-emerald-700 font-bold hover:underline"
                                title="Cryptographically verified on ledger"
                              >
                                <CheckCircle className="w-3.5 h-3.5 mr-0.5 text-emerald-600" /> VERIFIED
                              </button>
                            ) : check.status === 'EVIDENCE_MODIFIED' || check.status === 'HASH_MISMATCH' ? (
                              <button
                                onClick={() => setModalEvidence({ id: item.id, fileName: item.filename })}
                                className="flex items-center text-rose-700 font-bold hover:underline"
                                title="Tampering detected"
                              >
                                <ShieldAlert className="w-3.5 h-3.5 mr-0.5 text-rose-600" /> TAMPERED
                              </button>
                            ) : (
                              <button
                                onClick={() => setModalEvidence({ id: item.id, fileName: item.filename })}
                                className="flex items-center text-amber-700 font-bold hover:underline"
                                title={check.explanation}
                              >
                                <ShieldAlert className="w-3.5 h-3.5 mr-0.5 text-amber-600" /> {check.status}
                              </button>
                            )
                          ) : (
                            <button
                              onClick={() => setModalEvidence({ id: item.id, fileName: item.filename })}
                              className="truncate max-w-[100px] hover:text-[var(--color-accent)] hover:underline"
                              title="Click to inspect integrity ledger"
                            >
                              {item.hash_sha256.slice(0, 12)}...
                            </button>
                          )}
                        </div>
                      </td>

                      <td className="py-3 px-2 text-right">
                        <div className="flex items-center justify-end gap-1.5">
                          <Button
                            size="sm"
                            variant="secondary"
                            onClick={() => handleVerify(item.id)}
                            disabled={isVerifying}
                            className="text-[11px] py-0.5 px-2 h-6"
                            title="Verify byte integrity"
                          >
                            <ShieldCheck className={`w-3 h-3 ${isVerifying ? 'animate-spin' : ''}`} />
                            {isVerifying ? 'Verifying...' : 'Verify'}
                          </Button>

                          <Button
                            size="sm"
                            variant="secondary"
                            onClick={() => setModalEvidence({ id: item.id, fileName: item.filename })}
                            className="text-[11px] py-0.5 px-2 h-6 border-[var(--color-border)] text-[var(--color-text-primary)] hover:bg-[var(--color-surface-subtle)]"
                            title="Inspect Blockchain Ledger"
                          >
                            <ShieldCheck className="w-3 h-3 mr-1 text-blue-600" /> Ledger
                          </Button>

                          <Link to={`/evidence/${item.id}/extraction`}>
                            <Button size="sm" variant="primary" className="text-[11px] py-0.5 px-2.5 h-6">
                              <Eye className="w-3 h-3 mr-1" /> Review
                            </Button>
                          </Link>

                          <Link to={`/evidence/${item.id}`}>
                            <Button size="sm" variant="secondary" className="text-[11px] py-0.5 px-2 h-6">
                              Details
                            </Button>
                          </Link>
                        </div>
                      </td>
                    </tr>
                  );
                })
              )}
            </tbody>
          </table>
        </div>
      </Card>

      {/* Phase 9 Evidence Integrity Modal */}
      <EvidenceIntegrityModal
        isOpen={!!modalEvidence}
        onClose={() => setModalEvidence(null)}
        evidenceId={modalEvidence?.id || ''}
        fileName={modalEvidence?.fileName}
      />
    </div>
  );
};
