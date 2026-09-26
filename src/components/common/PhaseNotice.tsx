import React from 'react';
import { Layers, ArrowRight } from 'lucide-react';
import { Link } from 'react-router-dom';

interface PhaseNoticeProps {
  phaseNumber?: number;
  moduleName?: string;
  className?: string;
}

export const PhaseNotice: React.FC<PhaseNoticeProps> = ({
  phaseNumber,
  moduleName,
  className = '',
}) => {
  return (
    <div
      id="phase-foundation-banner"
      className={`rounded-xl bg-[var(--color-surface)] border border-[var(--color-border)] p-4 text-xs text-[var(--color-text-secondary)] flex flex-col sm:flex-row items-start sm:items-center justify-between gap-3 shadow-xs transition-colors ${className}`}
    >
      <div className="flex items-start sm:items-center gap-3">
        <div className="p-2 rounded-lg bg-[var(--color-surface-subtle)] text-[var(--color-accent)] shrink-0">
          <Layers className="w-4 h-4" />
        </div>
        <div>
          <div className="flex items-center gap-2">
            <span className="font-semibold text-[var(--color-text-primary)]">Phase 1 Development Foundation</span>
            {phaseNumber && (
              <span className="text-[10px] uppercase font-mono px-1.5 py-0.5 rounded bg-[var(--color-surface-subtle)] text-[var(--color-accent)] border border-[var(--color-border-subtle)]">
                {moduleName || 'Module'}: Target Phase {phaseNumber}
              </span>
            )}
          </div>
          <p className="text-[var(--color-text-secondary)] text-xs mt-0.5 leading-relaxed">
            System diagnostics and infrastructure health are live. Authentication, investigations, evidence processing,
            NLP, entity resolution, graph analytics, assistant, reports, and audit persistence are integration-ready for later phases.
          </p>
        </div>
      </div>
      <Link
        to="/system-status"
        className="inline-flex items-center gap-1.5 text-xs font-semibold text-[var(--color-accent)] hover:underline transition-colors whitespace-nowrap self-end sm:self-center"
      >
        <span>Inspect System Status</span>
        <ArrowRight className="w-3.5 h-3.5" />
      </Link>
    </div>
  );
};
