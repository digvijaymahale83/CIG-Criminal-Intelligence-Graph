import React, { useState } from 'react';
import { ChevronDown, Check, Plus, FolderGit2 } from 'lucide-react';
import { useActiveInvestigation } from '../../hooks/useActiveInvestigation';
import { useNavigate } from 'react-router-dom';

const RECENT_INVESTIGATIONS = [
  {
    id: 'inv-2026-0841',
    caseNumber: 'CASE-2026-0841',
    title: 'Operation Iron Vault Syndicate',
    classification: 'Confidential / Law Enforcement Sensitive',
  },
  {
    id: 'inv-2026-0842',
    caseNumber: 'CASE-2026-0842',
    title: 'Cross-Border Logistics Network',
    classification: 'Restricted',
  },
];

export const InvestigationSelector: React.FC = () => {
  const [isOpen, setIsOpen] = useState(false);
  const { activeInvestigationId, activeCaseNumber, setActiveInvestigation } = useActiveInvestigation();
  const navigate = useNavigate();

  return (
    <div className="relative" id="investigation-case-selector">
      <button
        onClick={() => setIsOpen(!isOpen)}
        className="flex items-center gap-2 bg-[var(--color-surface)] border border-[var(--color-border)] hover:border-[var(--color-accent)]/50 rounded-lg px-3 py-1.5 transition-colors cursor-pointer text-left shadow-2xs"
        aria-expanded={isOpen}
        aria-label="Active investigation selector"
      >
        <span className="text-[10px] font-bold text-[var(--color-text-muted)] uppercase tracking-wider">Case:</span>
        <span className="text-xs sm:text-sm font-mono text-[var(--color-accent)] font-semibold truncate max-w-[130px] sm:max-w-[190px]">
          {activeCaseNumber || 'CASE-2026-0841'}
        </span>
        <ChevronDown className="w-3.5 h-3.5 text-[var(--color-text-muted)] shrink-0 ml-0.5" />
      </button>

      {isOpen && (
        <>
          <div className="fixed inset-0 z-40" onClick={() => setIsOpen(false)} />
          <div className="absolute left-0 mt-2 w-80 rounded-xl bg-[var(--color-surface)] border border-[var(--color-border)] shadow-xl p-2.5 z-50 text-xs text-[var(--color-text-primary)] transition-colors">
            <div className="px-2.5 py-1.5 text-[10px] font-mono text-[var(--color-text-muted)] uppercase tracking-wider flex items-center justify-between border-b border-[var(--color-border)]">
              <span>Active Investigation Scope</span>
              <span className="text-emerald-500 font-bold">ISOLATED</span>
            </div>
            <div className="py-1.5 space-y-1">
              {RECENT_INVESTIGATIONS.map((inv) => {
                const isSelected = activeCaseNumber === inv.caseNumber || activeInvestigationId === inv.id;
                return (
                  <button
                    key={inv.id}
                    onClick={() => {
                      setActiveInvestigation(inv.id, inv.caseNumber, inv.title);
                      setIsOpen(false);
                    }}
                    className={`w-full flex items-start gap-2.5 p-2.5 rounded-lg text-left transition-colors cursor-pointer ${
                      isSelected
                        ? 'bg-[var(--color-surface-subtle)] text-[var(--color-text-primary)] border border-[var(--color-border)]'
                        : 'text-[var(--color-text-secondary)] hover:bg-[var(--color-surface-subtle)]'
                    }`}
                  >
                    <FolderGit2 className="w-4 h-4 text-[var(--color-accent)] shrink-0 mt-0.5" />
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center justify-between">
                        <span className="font-mono text-xs font-semibold text-[var(--color-accent)]">
                          {inv.caseNumber}
                        </span>
                        {isSelected && <Check className="w-3.5 h-3.5 text-emerald-500 shrink-0" />}
                      </div>
                      <p className="text-xs text-[var(--color-text-primary)] truncate font-medium mt-0.5">{inv.title}</p>
                      <p className="text-[10px] text-[var(--color-text-muted)] truncate mt-0.5">{inv.classification}</p>
                    </div>
                  </button>
                );
              })}
            </div>
            <div className="pt-2 border-t border-[var(--color-border)] flex items-center justify-between px-1">
              <button
                onClick={() => {
                  setIsOpen(false);
                  navigate('/investigations');
                }}
                className="text-xs text-[var(--color-accent)] hover:underline transition-colors font-medium cursor-pointer"
              >
                All Investigations
              </button>
              <button
                onClick={() => {
                  setIsOpen(false);
                  navigate('/investigations/new');
                }}
                className="inline-flex items-center gap-1 text-[11px] text-[var(--color-text-primary)] hover:bg-[var(--color-surface-subtle)] px-2.5 py-1 rounded bg-[var(--color-surface)] border border-[var(--color-border)] transition-colors cursor-pointer"
              >
                <Plus className="w-3 h-3 text-[var(--color-accent)]" /> New Case
              </button>
            </div>
          </div>
        </>
      )}
    </div>
  );
};
