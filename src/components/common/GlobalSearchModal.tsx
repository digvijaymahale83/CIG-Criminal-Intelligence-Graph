import React, { useState } from 'react';
import { Search, X, Filter } from 'lucide-react';
import { useActiveInvestigation } from '../../hooks/useActiveInvestigation';
import { EntityType } from '../../types/entities';
import { StatusBadge } from './StatusBadge';

interface GlobalSearchModalProps {
  isOpen: boolean;
  onClose: () => void;
}

const ENTITY_TYPES: EntityType[] = [
  'Person',
  'Organization',
  'Phone',
  'Vehicle',
  'Bank Account',
  'Location',
  'Event',
  'Case',
  'Crime',
  'Document',
  'Evidence',
];

export const GlobalSearchModal: React.FC<GlobalSearchModalProps> = ({ isOpen, onClose }) => {
  const [query, setQuery] = useState('');
  const [selectedType, setSelectedType] = useState<string>('all');
  const { activeCaseNumber, activeCaseTitle } = useActiveInvestigation();

  if (!isOpen) return null;

  return (
    <div
      id="global-search-modal"
      className="fixed inset-0 z-50 flex items-start justify-center pt-16 p-4 bg-black/60 backdrop-blur-xs"
      role="dialog"
      aria-modal="true"
    >
      <div className="fixed inset-0" onClick={onClose} />
      <div className="relative w-full max-w-2xl rounded-xl bg-[var(--color-surface)] border border-[var(--color-border)] shadow-2xl overflow-hidden z-10 flex flex-col max-h-[85vh] transition-colors">
        {/* Header & Search Input */}
        <div className="p-4 border-b border-[var(--color-border)] space-y-3 bg-[var(--color-surface-subtle)]">
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <Search className="w-4 h-4 text-[var(--color-accent)]" />
              <h3 className="text-sm font-semibold text-[var(--color-text-primary)]">Global Investigative Search</h3>
            </div>
            <button
              onClick={onClose}
              className="p-1 rounded text-[var(--color-text-muted)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-surface)] transition-colors cursor-pointer"
            >
              <X className="w-4 h-4" />
            </button>
          </div>

          <div className="flex items-center gap-2 bg-[var(--color-surface)] border border-[var(--color-border)] rounded-lg px-3 py-2">
            <Search className="w-4 h-4 text-[var(--color-text-muted)] shrink-0" />
            <input
              type="text"
              placeholder="Search entities, evidence, assertions, case numbers, identifiers..."
              value={query}
              onChange={(e) => setQuery(e.target.value)}
              className="w-full bg-transparent text-xs text-[var(--color-text-primary)] placeholder-[var(--color-text-muted)] outline-none"
              autoFocus
            />
          </div>

          <div className="flex items-center gap-2 overflow-x-auto pb-1 text-xs">
            <span className="text-[var(--color-text-muted)] flex items-center gap-1 text-[11px] shrink-0">
              <Filter className="w-3 h-3" /> Type:
            </span>
            <button
              onClick={() => setSelectedType('all')}
              className={`px-2 py-0.5 rounded text-[11px] font-medium transition-colors cursor-pointer shrink-0 ${
                selectedType === 'all'
                  ? 'bg-[var(--color-accent)] text-white'
                  : 'bg-[var(--color-surface)] text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] border border-[var(--color-border)]'
              }`}
            >
              All Types
            </button>
            {ENTITY_TYPES.slice(0, 6).map((type) => (
              <button
                key={type}
                onClick={() => setSelectedType(type)}
                className={`px-2 py-0.5 rounded text-[11px] font-medium transition-colors cursor-pointer shrink-0 ${
                  selectedType === type
                    ? 'bg-[var(--color-accent)] text-white'
                    : 'bg-[var(--color-surface)] text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] border border-[var(--color-border)]'
                }`}
              >
                {type}
              </button>
            ))}
          </div>
        </div>

        {/* Results Body */}
        <div className="p-6 overflow-y-auto flex-1">
          {query.trim() === '' ? (
            <div className="text-center py-8 text-[var(--color-text-muted)] text-xs">
              <p>Type keywords, entity names, phone numbers, vehicle plates, or evidence IDs.</p>
              <p className="mt-1 text-[11px] text-[var(--color-text-secondary)]">
                Searches are scoped within active investigation: <strong className="text-[var(--color-text-primary)]">{activeCaseNumber}</strong>
              </p>
            </div>
          ) : (
            <div className="space-y-3">
              <div className="text-[11px] font-mono text-[var(--color-text-muted)] uppercase tracking-wider">
                Demonstration Search Index ({activeCaseNumber})
              </div>
              <div className="p-3.5 rounded-lg bg-[var(--color-surface-subtle)] border border-[var(--color-border)] flex items-start justify-between gap-4">
                <div className="space-y-1">
                  <div className="flex items-center gap-2">
                    <span className="text-xs font-semibold text-[var(--color-text-primary)]">{query}</span>
                    <span className="text-[10px] font-mono px-1.5 py-0.5 rounded bg-[var(--color-surface)] text-[var(--color-text-secondary)] border border-[var(--color-border)]">
                      Person
                    </span>
                    <StatusBadge status="Under Review" size="sm" />
                  </div>
                  <p className="text-[11px] text-[var(--color-text-secondary)]">
                    Source Document: FININT-Wire-2026-B.pdf (Page 4, Row 18)
                  </p>
                </div>
                <div className="text-right">
                  <span className="text-[11px] font-mono text-[var(--color-text-muted)]">Confidence: 0.88</span>
                </div>
              </div>
            </div>
          )}
        </div>

        {/* Footer */}
        <div className="px-4 py-2.5 border-t border-[var(--color-border)] bg-[var(--color-surface-subtle)] flex items-center justify-between text-[11px] text-[var(--color-text-muted)]">
          <span>Active Case: {activeCaseTitle}</span>
          <span>Press Esc to close</span>
        </div>
      </div>
    </div>
  );
};
