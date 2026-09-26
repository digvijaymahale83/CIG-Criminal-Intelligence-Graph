import React, { useState, useEffect } from 'react';
import { FolderGit2, Plus, Search, Lock, ArrowRight, RefreshCw, X, Clock } from 'lucide-react';
import { Link } from 'react-router-dom';
import { useActiveInvestigation } from '../../hooks/useActiveInvestigation';
import { apiClient } from '../../services/api/client';

interface Investigation {
  id: string;
  case_number: string;
  title: string;
  description: string;
  classification: string;
  priority: string;
  status: string;
  category: string;
  jurisdiction: string;
  district: string;
  lead_officer_name: string;
  evidence_count: number;
  entity_count: number;
  finding_count: number;
  fir_number: string;
  police_station: string;
  updated_at: string;
}

const priorityColor: Record<string, string> = {
  Critical: 'text-[#f85149] bg-[#f85149]/15 border-[#f85149]/30',
  High: 'text-[#e3b341] bg-[#e3b341]/15 border-[#e3b341]/30',
  Medium: 'text-[#3fb950] bg-[#3fb950]/15 border-[#3fb950]/30',
  Low: 'text-[#79c0ff] bg-[#79c0ff]/15 border-[#79c0ff]/30',
};

export const InvestigationsPage: React.FC = () => {
  const { activeInvestigationId, setActiveInvestigation } = useActiveInvestigation();
  const [investigations, setInvestigations] = useState<Investigation[]>([]);
  const [loading, setLoading] = useState(true);
  const [searchTerm, setSearchTerm] = useState('');

  const fetchInvestigations = async () => {
    setLoading(true);
    try {
      let rawData: any[];
      try {
        rawData = await apiClient.get<any[]>('/api/v1/cases');
      } catch {
        rawData = await apiClient.get<any[]>('/api/investigations');
      }
      const data: Investigation[] = (rawData || []).map((d: any) => ({
        ...d,
        case_number: d.case_number || d.caseNumber || '',
        lead_officer_name: d.lead_officer_name || d.leadOfficerName || '',
        fir_number: d.fir_number || d.firNumber || '',
        police_station: d.police_station || d.policeStation || '',
        evidence_count: d.evidence_count ?? d.evidenceCount ?? 0,
        entity_count: d.entity_count ?? d.entityCount ?? 0,
        updated_at: d.updated_at || d.updatedAtUtc || '',
      }));
      setInvestigations(data);
    } catch (err) {
      console.error('Investigations fetch error:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { fetchInvestigations(); }, []);

  const filtered = investigations.filter((inv) =>
    inv.case_number.toLowerCase().includes(searchTerm.toLowerCase()) ||
    inv.title.toLowerCase().includes(searchTerm.toLowerCase()) ||
    inv.district?.toLowerCase().includes(searchTerm.toLowerCase()) ||
    inv.category?.toLowerCase().includes(searchTerm.toLowerCase())
  );

  return (
    <div className="flex flex-col gap-5" id="investigations-page">
      <div className="flex items-center justify-between">
        <div>
          <div className="flex items-center gap-2">
            <FolderGit2 className="w-5 h-5 text-[#79c0ff]" />
            <h1 className="text-xl font-bold text-[var(--color-text-primary)] tracking-tight">Investigations Repository</h1>
          </div>
          <p className="text-xs text-[var(--color-text-secondary)] mt-0.5">
            The Platform · Case files, synthetic FIR records, and operational scope management
          </p>
        </div>
        <div className="flex items-center gap-2">
          <button onClick={fetchInvestigations} disabled={loading} className="flex items-center gap-1.5 px-3 py-2 rounded-lg bg-[var(--color-surface)] border border-[var(--color-border)] text-[var(--color-text-secondary)] text-xs hover:border-[#388bfd]/40 transition-all cursor-pointer disabled:opacity-50">
            <RefreshCw className={`w-3.5 h-3.5 ${loading ? 'animate-spin' : ''}`} />
          </button>
          <Link to="/investigations/new" className="flex items-center gap-2 px-3 py-2 rounded-lg bg-gradient-to-r from-[#1f6feb] to-[#388bfd] text-white text-xs font-semibold hover:from-[#388bfd] hover:to-[#58a6ff] transition-all shadow-lg shadow-blue-900/20">
            <Plus className="w-3.5 h-3.5" /> New Investigation
          </Link>
        </div>
      </div>

      {/* Search */}
      <div className="relative">
        <Search className="absolute left-3 top-1/2 -translate-y-1/2 w-3.5 h-3.5 text-[var(--color-text-muted)]" />
        <input
          type="text"
          placeholder="Filter by case number, title, district, or category..."
          value={searchTerm}
          onChange={(e) => setSearchTerm(e.target.value)}
          className="w-full pl-9 pr-9 py-2.5 bg-[var(--color-surface)] border border-[var(--color-border)] rounded-lg text-sm text-[var(--color-text-primary)] placeholder-[var(--color-text-muted)] focus:border-[#388bfd] outline-none transition-colors"
        />
        {searchTerm && (
          <button onClick={() => setSearchTerm('')} className="absolute right-3 top-1/2 -translate-y-1/2 text-[var(--color-text-muted)] hover:text-[var(--color-text-secondary)]">
            <X className="w-3.5 h-3.5" />
          </button>
        )}
      </div>

      {loading ? (
        <div className="flex items-center justify-center h-48">
          <div className="flex flex-col items-center gap-3">
            <div className="w-8 h-8 border-2 border-[var(--color-border)] border-t-[#388bfd] rounded-full animate-spin" />
            <p className="text-xs text-[var(--color-text-secondary)]">Loading investigations…</p>
          </div>
        </div>
      ) : (
        <div className="space-y-3">
          {filtered.map((inv) => {
            const isActive = activeInvestigationId === inv.id;
            return (
              <div
                key={inv.id}
                className={`rounded-xl border p-5 transition-all ${isActive
                    ? 'bg-[var(--color-surface)] border-[#388bfd] ring-1 ring-[#388bfd]/30'
                    : 'bg-[var(--color-surface)] border-[var(--color-border)] hover:border-[#388bfd]/40'
                  }`}
              >
                <div className="flex flex-col md:flex-row md:items-center justify-between gap-4">
                  <div className="space-y-2 flex-1">
                    <div className="flex flex-wrap items-center gap-2">
                      <span className="text-xs font-mono font-bold text-[#79c0ff]">{inv.case_number}</span>
                      <span className={`text-[9px] font-bold px-1.5 py-0.5 rounded border ${priorityColor[inv.priority] || 'text-[var(--color-text-secondary)]'}`}>
                        {inv.priority} Priority
                      </span>
                      <span className="text-[9px] px-1.5 py-0.5 rounded bg-[#30363d] text-[var(--color-text-secondary)] border border-[var(--color-border)]">
                        {inv.classification}
                      </span>
                      {inv.fir_number && (
                        <span className="text-[9px] font-mono px-1.5 py-0.5 rounded bg-[var(--color-bg-elevated)] text-[var(--color-text-secondary)] border border-[var(--color-border)]">
                          FIR: {inv.fir_number}
                        </span>
                      )}
                      {isActive && (
                        <span className="text-[9px] font-mono px-2 py-0.5 rounded bg-[#3fb950]/15 text-[#3fb950] border border-[#3fb950]/30 font-semibold animate-pulse">
                          ACTIVE SCOPE
                        </span>
                      )}
                    </div>
                    <h3 className="text-sm font-bold text-[var(--color-text-primary)]">{inv.title}</h3>
                    <p className="text-xs text-[var(--color-text-secondary)] leading-relaxed max-w-3xl">{inv.description}</p>
                    <div className="flex flex-wrap items-center gap-4 text-[11px] font-mono text-[var(--color-text-muted)] pt-1">
                      <span className="text-[var(--color-text-secondary)]">📍 {inv.district}</span>
                      {inv.police_station && <span>🏢 {inv.police_station}</span>}
                      <span>Evidence: {inv.evidence_count}</span>
                      <span>Entities: {inv.entity_count}</span>
                      <span>Findings: {inv.finding_count}</span>
                      {inv.lead_officer_name && <span>👤 {inv.lead_officer_name}</span>}
                    </div>
                  </div>
                  <div className="flex items-center gap-2 self-end md:self-center shrink-0">
                    {!isActive ? (
                      <button
                        onClick={() => setActiveInvestigation(inv.id, inv.case_number, inv.title)}
                        className="flex items-center gap-2 px-3 py-2 rounded-lg bg-[var(--color-bg-elevated)] border border-[var(--color-border)] text-[var(--color-text-secondary)] text-xs font-semibold hover:border-[#3fb950]/50 hover:text-[#3fb950] transition-all cursor-pointer"
                      >
                        <Lock className="w-3.5 h-3.5" /> Set Active Scope
                      </button>
                    ) : (
                      <div className="flex items-center gap-2">
                        <Link
                          to="/timeline"
                          className="flex items-center gap-1.5 px-3 py-2 rounded-lg bg-[#d29922]/15 border border-[#d29922]/30 text-[#e3b341] hover:bg-[#d29922]/25 text-xs font-semibold transition-all"
                          title="View Case Temporal Intelligence Timeline"
                        >
                          <Clock className="w-3.5 h-3.5" /> Timeline
                        </Link>
                        <Link
                          to="/evidence"
                          className="flex items-center gap-2 px-3 py-2 rounded-lg bg-[#388bfd]/20 border border-[#388bfd]/40 text-[#79c0ff] text-xs font-semibold hover:bg-[#388bfd]/30 transition-all"
                        >
                          <ArrowRight className="w-3.5 h-3.5" /> Open Workspace
                        </Link>
                      </div>
                    )}
                  </div>
                </div>
              </div>
            );
          })}
          {filtered.length === 0 && (
            <div className="text-center py-12 text-[var(--color-text-muted)]">
              <FolderGit2 className="w-12 h-12 mx-auto mb-3 opacity-30" />
              <p className="text-sm">No investigations found matching your search.</p>
            </div>
          )}
        </div>
      )}
    </div>
  );
};
