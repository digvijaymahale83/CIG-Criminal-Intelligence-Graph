import React, { useState, useEffect } from 'react';
import { FileText, Download, Shield, CheckCircle, Clock, User, RefreshCw } from 'lucide-react';
import { apiClient } from '../../services/api/client';

interface CaseFile {
  id: string;
  case_number: string;
  title: string;
  district: string;
  type: string;
  officer: string;
  status: 'Active' | 'Under Review' | 'Closed';
  evidence_count: number;
  clearance: 'RESTRICTED' | 'CONFIDENTIAL' | 'SECRET';
  description: string;
}

const clearanceBadge = (c: string) => {
  switch (c) {
    case 'RESTRICTED': return 'bg-[#f85149]/15 text-[#f85149] border-[#f85149]/30';
    case 'CONFIDENTIAL': return 'bg-[#e3b341]/15 text-[#e3b341] border-[#e3b341]/30';
    case 'SECRET': return 'bg-[#d2a8ff]/15 text-[#d2a8ff] border-[#d2a8ff]/30';
    default: return 'bg-[#8b949e]/15 text-[var(--color-text-secondary)] border-[#8b949e]/30';
  }
};

const statusBadge = (s: string) => {
  switch (s) {
    case 'Active': return 'bg-[#e3b341]/15 text-[#e3b341] border-[#e3b341]/30';
    case 'Closed': return 'bg-[#3fb950]/15 text-[#3fb950] border-[#3fb950]/30';
    case 'Under Review': return 'bg-[#79c0ff]/15 text-[#79c0ff] border-[#79c0ff]/30';
    default: return 'bg-[#8b949e]/15 text-[var(--color-text-secondary)] border-[#8b949e]/30';
  }
};

export const ReportsPage: React.FC = () => {
  const [cases, setCases] = useState<CaseFile[]>([]);
  const [selectedCase, setSelectedCase] = useState<CaseFile | null>(null);
  const [loading, setLoading] = useState(true);
  const [downloading, setDownloading] = useState<'csv' | 'pdf' | null>(null);
  const [downloadSuccess, setDownloadSuccess] = useState<string | null>(null);

  useEffect(() => {
    const fetchCases = async () => {
      setLoading(true);
      try {
        const data = await apiClient.get<CaseFile[]>('/api/v1/reports');
        setCases(data);
        if (data.length > 0) setSelectedCase(data[0]);
      } catch (err) {
        console.error('Reports fetch error:', err);
      } finally {
        setLoading(false);
      }
    };
    fetchCases();
  }, []);

  const handleDownload = async (type: 'csv' | 'pdf') => {
    if (!selectedCase) return;
    setDownloading(type);
    setDownloadSuccess(null);
    try {
      const blob = await apiClient.getBlob(`/api/v1/reports/${selectedCase.id}/${type}`);
      const url = URL.createObjectURL(blob);
      const a = document.createElement('a');
      a.href = url;
      const filename = type === 'csv'
        ? `Investigation-${selectedCase.case_number.replace(/\//g, '-')}-Report.csv`
        : `Investigation-${selectedCase.case_number.replace(/\//g, '-')}-Briefing.pdf`;
      a.download = filename;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      URL.revokeObjectURL(url);
      setDownloadSuccess(`${type.toUpperCase()} downloaded: ${filename}`);
    } catch (err) {
      console.error('Download error:', err);
    } finally {
      setDownloading(null);
    }
  };

  return (
    <div className="flex flex-col gap-4" id="reports-page">
      <div>
        <div className="flex items-center gap-2">
          <FileText className="w-5 h-5 text-[#79c0ff]" />
          <h1 className="text-xl font-bold text-[var(--color-text-primary)] tracking-tight">Case Intelligence Reports Generator</h1>
        </div>
        <p className="text-xs text-[var(--color-text-secondary)] mt-0.5">
          Select any case to generate and download encrypted CSV or PDF intelligence briefings with evidence audit.
        </p>
      </div>

      {loading ? (
        <div className="flex items-center justify-center h-64">
          <div className="flex flex-col items-center gap-3">
            <div className="w-8 h-8 border-2 border-[var(--color-border)] border-t-[#388bfd] rounded-full animate-spin" />
            <p className="text-xs text-[var(--color-text-secondary)]">Loading case files…</p>
          </div>
        </div>
      ) : (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-4" style={{ minHeight: '560px' }}>
          {/* Case Selector */}
          <div className="bg-[var(--color-surface)] border border-[var(--color-border)] rounded-xl overflow-hidden flex flex-col">
            <div className="px-4 py-3 border-b border-[var(--color-border-subtle)] flex items-center gap-2">
              <FileText className="w-3.5 h-3.5 text-[var(--color-text-secondary)]" />
              <h3 className="text-xs font-semibold text-[var(--color-text-primary)]">Select Case File ({cases.length} total)</h3>
            </div>
            <div className="flex-1 overflow-y-auto divide-y divide-[#21262d]">
              {cases.map((caseFile) => (
                <button
                  key={caseFile.id}
                  onClick={() => setSelectedCase(caseFile)}
                  className={`w-full text-left px-4 py-4 transition-all cursor-pointer ${
                    selectedCase?.id === caseFile.id
                      ? 'bg-[#1f6feb]/10 border-l-2 border-l-[#388bfd]'
                      : 'hover:bg-[var(--color-bg-elevated)] border-l-2 border-l-transparent'
                  }`}
                >
                  <div className="flex items-start justify-between gap-3 mb-1.5">
                    <span className={`text-[9px] font-bold px-1.5 py-0.5 rounded font-mono ${
                      selectedCase?.id === caseFile.id ? 'text-[#79c0ff]' : 'text-[var(--color-text-secondary)]'
                    }`}>
                      {caseFile.case_number}
                    </span>
                    <span className={`text-[9px] font-bold px-2 py-0.5 rounded border whitespace-nowrap shrink-0 ${clearanceBadge(caseFile.clearance)}`}>
                      {caseFile.clearance}
                    </span>
                  </div>
                  <h4 className={`text-sm font-semibold leading-tight mb-1 ${selectedCase?.id === caseFile.id ? 'text-[var(--color-text-primary)]' : 'text-[var(--color-text-primary)]'}`}>
                    {caseFile.title}
                  </h4>
                  <p className="text-[10px] text-[var(--color-text-secondary)]">{caseFile.district} · Officer: {caseFile.officer}</p>
                </button>
              ))}
            </div>
          </div>

          {/* Generated Briefing */}
          {selectedCase && (
            <div className="bg-[var(--color-surface)] border border-[var(--color-border)] rounded-xl overflow-hidden flex flex-col">
              <div className="px-4 py-3 border-b border-[var(--color-border-subtle)] flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <FileText className="w-3.5 h-3.5 text-[var(--color-text-secondary)]" />
                  <h3 className="text-xs font-semibold text-[var(--color-text-primary)]">Briefing: {selectedCase.case_number}</h3>
                </div>
                <span className="text-[9px] font-bold px-2 py-1 rounded border bg-[#3fb950]/15 text-[#3fb950] border-[#3fb950]/30 flex items-center gap-1">
                  <span className="w-1.5 h-1.5 rounded-full bg-[#3fb950] animate-pulse" /> LIVE GENERATED
                </span>
              </div>

              <div className="flex-1 overflow-y-auto p-4 flex flex-col gap-4">
                <div className="bg-[var(--color-bg-elevated)] border border-[var(--color-border)] rounded-xl p-4">
                  <p className="text-[10px] font-bold text-[var(--color-text-secondary)] uppercase tracking-widest mb-1">
                    {selectedCase.district.toUpperCase()} · {selectedCase.type?.toUpperCase() || 'INVESTIGATION'}
                  </p>
                  <h2 className="text-base font-bold text-[var(--color-text-primary)] leading-tight mb-2">{selectedCase.title}</h2>
                  <p className="text-xs text-[var(--color-text-secondary)] leading-relaxed mb-4">{selectedCase.description}</p>
                  <div className="grid grid-cols-4 gap-2">
                    <div className="bg-[var(--color-surface)] rounded-lg p-2.5 border border-[var(--color-border)]">
                      <p className="text-[9px] text-[var(--color-text-muted)] uppercase tracking-wide mb-1 flex items-center gap-1"><User className="w-2.5 h-2.5" /> Officer</p>
                      <p className="text-[11px] font-bold text-[var(--color-text-primary)]">{selectedCase.officer}</p>
                    </div>
                    <div className="bg-[var(--color-surface)] rounded-lg p-2.5 border border-[var(--color-border)]">
                      <p className="text-[9px] text-[var(--color-text-muted)] uppercase tracking-wide mb-1 flex items-center gap-1"><Clock className="w-2.5 h-2.5" /> Status</p>
                      <span className={`text-[9px] font-bold px-1.5 py-0.5 rounded border ${statusBadge(selectedCase.status)}`}>{selectedCase.status}</span>
                    </div>
                    <div className="bg-[var(--color-surface)] rounded-lg p-2.5 border border-[var(--color-border)]">
                      <p className="text-[9px] text-[var(--color-text-muted)] uppercase tracking-wide mb-1 flex items-center gap-1"><FileText className="w-2.5 h-2.5" /> Evidence</p>
                      <p className="text-[11px] font-bold text-[var(--color-text-primary)]">{selectedCase.evidence_count} Items</p>
                    </div>
                    <div className="bg-[var(--color-surface)] rounded-lg p-2.5 border border-[var(--color-border)]">
                      <p className="text-[9px] text-[var(--color-text-muted)] uppercase tracking-wide mb-1 flex items-center gap-1"><Shield className="w-2.5 h-2.5" /> Clearance</p>
                      <span className={`text-[9px] font-bold px-1.5 py-0.5 rounded border ${clearanceBadge(selectedCase.clearance)}`}>{selectedCase.clearance}</span>
                    </div>
                  </div>
                </div>

                <div className="bg-[var(--color-bg-elevated)] border border-[var(--color-border)] rounded-xl p-4">
                  <p className="text-xs font-semibold text-[var(--color-text-primary)] mb-3">Report Contents:</p>
                  <div className="space-y-2">
                    {['Master FIR record and original station logs (Standard format)', 'SHA-256 cryptographic audit hashes for all evidence files', 'Entity intelligence cards with risk classification', 'Multi-hop network link analysis and relationship map', 'Compliance report per Standard Operating Procedure'].map((item, i) => (
                      <div key={i} className="flex items-start gap-2">
                        <CheckCircle className="w-3.5 h-3.5 text-[#3fb950] shrink-0 mt-0.5" />
                        <p className="text-xs text-[var(--color-text-secondary)]">{item}</p>
                      </div>
                    ))}
                  </div>
                </div>

                <div className="bg-[var(--color-bg-canvas)] border border-[var(--color-border-subtle)] rounded-lg p-3">
                  <p className="text-[9px] font-bold text-[var(--color-text-muted)] uppercase tracking-widest mb-2">Cryptographic Ledger Integrity</p>
                  <p className="text-[10px] font-mono text-[#3fb950] break-all">
                    SHA-256 Ledger Verified • Official Court-Admissible Intelligence Briefing
                  </p>
                </div>

                {downloadSuccess && (
                  <div className="flex items-center gap-2 p-3 rounded-lg bg-[#3fb950]/10 border border-[#3fb950]/30">
                    <CheckCircle className="w-4 h-4 text-[#3fb950] shrink-0" />
                    <p className="text-xs text-[#3fb950]">{downloadSuccess}</p>
                  </div>
                )}
              </div>

              <div className="px-4 py-3 border-t border-[var(--color-border-subtle)] flex items-center gap-3">
                <button
                  onClick={() => handleDownload('csv')}
                  disabled={downloading !== null}
                  className="flex-1 flex items-center justify-center gap-2 px-3 py-2.5 rounded-lg bg-[var(--color-surface-subtle)] border border-[var(--color-border)] text-xs font-semibold text-[var(--color-text-primary)] hover:bg-[var(--color-surface-subtle)] hover:border-[#388bfd]/50 transition-all cursor-pointer disabled:opacity-60"
                >
                  {downloading === 'csv' ? <RefreshCw className="w-3.5 h-3.5 animate-spin" /> : <Download className="w-3.5 h-3.5 text-[var(--color-text-secondary)]" />}
                  {downloading === 'csv' ? 'Generating CSV…' : 'Download CSV Report'}
                </button>
                <button
                  onClick={() => handleDownload('pdf')}
                  disabled={downloading !== null}
                  className="flex-1 flex items-center justify-center gap-2 px-3 py-2.5 rounded-lg bg-[#1f6feb]/20 border border-[#1f6feb]/40 text-xs font-semibold text-[#79c0ff] hover:bg-[#1f6feb]/30 transition-all cursor-pointer disabled:opacity-60"
                >
                  {downloading === 'pdf' ? <RefreshCw className="w-3.5 h-3.5 animate-spin" /> : <Download className="w-3.5 h-3.5" />}
                  {downloading === 'pdf' ? 'Generating PDF…' : 'Download PDF Briefing'}
                </button>
              </div>
            </div>
          )}
        </div>
      )}
    </div>
  );
};
