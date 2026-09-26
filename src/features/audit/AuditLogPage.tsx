import React, { useState, useEffect } from 'react';
import { ShieldAlert, RefreshCw, Clock } from 'lucide-react';
import { apiClient } from '../../services/api/client';

interface AuditEntry {
  id: string;
  user_name: string;
  action: string;
  resource_type: string;
  resource_id: string;
  details: string;
  ip_address: string;
  timestamp: string;
}

const actionBadge = (action: string) => {
  if (action.includes('LOGIN')) return 'bg-[#3fb950]/15 text-[#3fb950] border-[#3fb950]/30';
  if (action.includes('EXPORT') || action.includes('DOWNLOAD')) return 'bg-[#e3b341]/15 text-[#e3b341] border-[#e3b341]/30';
  if (action.includes('CREATE')) return 'bg-[#1f6feb]/15 text-[#79c0ff] border-[#1f6feb]/30';
  if (action.includes('DELETE')) return 'bg-[#f85149]/15 text-[#f85149] border-[#f85149]/30';
  return 'bg-[#30363d] text-[var(--color-text-secondary)] border-[var(--color-border)]';
};

export const AuditLogPage: React.FC = () => {
  const [entries, setEntries] = useState<AuditEntry[]>([]);
  const [loading, setLoading] = useState(true);

  const fetchAudit = async () => {
    setLoading(true);
    try {
      let rawData: any[];
      try {
        rawData = await apiClient.get<any[]>('/api/v1/audit?limit=100');
      } catch {
        rawData = await apiClient.get<any[]>('/api/audit?limit=100');
      }
      const data: AuditEntry[] = (rawData || []).map((d: any) => ({
        id: d.id,
        user_id: d.user_id || d.actorId || '',
        user_name: d.user_name || d.actorName || 'System',
        action: d.action,
        resource_type: d.resource_type || d.resourceType || '',
        resource_id: d.resource_id || d.resourceId || '',
        details: d.details || d.metadataJson || '',
        ip_address: d.ip_address || d.ipAddress || '',
        timestamp: d.timestamp || d.createdAtUtc || new Date().toISOString(),
      }));
      setEntries(data);
    } catch (err) {
      console.error('Audit fetch error:', err);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => { fetchAudit(); }, []);

  return (
    <div className="flex flex-col gap-5" id="audit-log-page">
      <div className="flex items-center justify-between">
        <div>
          <div className="flex items-center gap-2">
            <ShieldAlert className="w-5 h-5 text-[#f85149]" />
            <h1 className="text-xl font-bold text-[var(--color-text-primary)] tracking-tight">Immutable Audit Log</h1>
            <span className="text-[9px] font-mono px-2 py-0.5 rounded bg-[#3fb950]/15 text-[#3fb950] border border-[#3fb950]/30 flex items-center gap-1">
              <span className="w-1.5 h-1.5 rounded-full bg-[#3fb950] animate-pulse" /> LIVE
            </span>
          </div>
          <p className="text-xs text-[var(--color-text-secondary)] mt-0.5">
            Complete record of all investigator actions — The Platform · Tamper-evident log
          </p>
        </div>
        <button onClick={fetchAudit} disabled={loading} className="flex items-center gap-1.5 px-3 py-2 rounded-lg bg-[var(--color-surface)] border border-[var(--color-border)] text-[var(--color-text-secondary)] text-xs hover:border-[#388bfd]/40 transition-all cursor-pointer disabled:opacity-50">
          <RefreshCw className={`w-3.5 h-3.5 ${loading ? 'animate-spin' : ''}`} /> Refresh
        </button>
      </div>

      <div className="bg-[var(--color-surface)] border border-[var(--color-border)] rounded-xl overflow-hidden">
        <div className="overflow-x-auto">
          <table className="w-full text-left">
            <thead>
              <tr className="border-b border-[var(--color-border-subtle)] text-[var(--color-text-muted)] text-[10px] font-mono uppercase tracking-widest">
                <th className="px-4 py-3 font-semibold">Timestamp (IST)</th>
                <th className="px-4 py-3 font-semibold">Officer / Actor</th>
                <th className="px-4 py-3 font-semibold">Action</th>
                <th className="px-4 py-3 font-semibold">Details</th>
                <th className="px-4 py-3 font-semibold">IP Address</th>
              </tr>
            </thead>
            <tbody className="divide-y divide-[#21262d]">
              {loading ? (
                [...Array(6)].map((_, i) => (
                  <tr key={i} className="animate-pulse">
                    {[...Array(5)].map((__, j) => (
                      <td key={j} className="px-4 py-3"><div className="h-3 bg-[var(--color-surface-subtle)] rounded w-3/4" /></td>
                    ))}
                  </tr>
                ))
              ) : entries.map((entry) => (
                <tr key={entry.id} className="hover:bg-[var(--color-bg-elevated)] transition-colors">
                  <td className="px-4 py-3 text-[11px] font-mono text-[var(--color-text-secondary)] whitespace-nowrap">
                    <div className="flex items-center gap-1.5">
                      <Clock className="w-3 h-3 text-[var(--color-text-muted)]" />
                      {new Date(entry.timestamp).toLocaleString('en-IN', { timeZone: 'Asia/Kolkata', hour12: false })}
                    </div>
                  </td>
                  <td className="px-4 py-3 text-xs font-semibold text-[var(--color-text-primary)]">{entry.user_name || '—'}</td>
                  <td className="px-4 py-3">
                    <span className={`text-[9px] font-bold px-2 py-0.5 rounded border font-mono ${actionBadge(entry.action)}`}>
                      {entry.action}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-[11px] text-[var(--color-text-secondary)] max-w-xs truncate">{entry.details || entry.resource_type || '—'}</td>
                  <td className="px-4 py-3 text-[10px] font-mono text-[var(--color-text-muted)]">{entry.ip_address || '—'}</td>
                </tr>
              ))}
            </tbody>
          </table>
          {!loading && entries.length === 0 && (
            <div className="text-center py-12 text-[var(--color-text-muted)]">
              <ShieldAlert className="w-10 h-10 mx-auto mb-3 opacity-30" />
              <p className="text-sm">No audit entries yet. Actions will appear here as officers use the system.</p>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};
