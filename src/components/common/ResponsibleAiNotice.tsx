import React from 'react';
import { ShieldCheck } from 'lucide-react';

interface ResponsibleAiNoticeProps {
  compact?: boolean;
  className?: string;
  title?: string;
  message?: string;
}

export const ResponsibleAiNotice: React.FC<ResponsibleAiNoticeProps> = ({
  compact = false,
  className = '',
  title,
  message,
}) => {
  if (compact) {
    return (
      <div
        id="responsible-ai-notice-compact"
        className={`flex items-center gap-2 text-[11px] text-[var(--color-text-secondary)] bg-[var(--color-surface-subtle)] border border-[var(--color-border-subtle)] rounded-md px-3 py-1.5 ${className}`}
      >
        <ShieldCheck className="w-3.5 h-3.5 text-[var(--color-accent)] shrink-0" />
        <span>
          <strong className="text-[var(--color-text-primary)] font-medium">
            {title || 'Investigator Decision Support'}:
          </strong>{' '}
          {message || 'AI-generated analytical findings are non-autonomous hypotheses requiring source evidence corroboration. Not proof of criminality or guilt.'}
        </span>
      </div>
    );
  }

  return (
    <div
      id="responsible-ai-notice"
      className={`rounded-xl bg-[var(--color-surface)] border border-[var(--color-border)] shadow-2xs p-3.5 flex items-start gap-3 text-xs text-[var(--color-text-primary)] ${className}`}
    >
      <div className="p-1.5 rounded-lg bg-[var(--color-surface-subtle)] border border-[var(--color-border-subtle)] text-[var(--color-accent)] shrink-0 mt-0.5">
        <ShieldCheck className="w-4 h-4" />
      </div>
      <div className="flex-1 space-y-1">
        <div className="flex items-center gap-2">
          <span className="font-semibold text-[var(--color-text-primary)] uppercase tracking-wider text-[11px]">
            {title || 'Governance & Evidentiary Standard'}
          </span>
          <span className="text-[10px] px-2 py-0.5 rounded bg-[var(--color-surface-subtle)] text-[var(--color-text-muted)] border border-[var(--color-border-subtle)] font-mono">
            EVIDENCE-BACKED DECISION SUPPORT
          </span>
        </div>
        <p className="text-[var(--color-text-secondary)] text-xs leading-relaxed">
          {message || (
            <>
              AI-generated analytical findings, assertions, and graph projections are decision-support outputs. They must be
              independently reviewed against primary source evidence by authorized human investigators and must{' '}
              <strong className="text-[var(--color-text-primary)] font-semibold">never be treated as proof of criminality or guilt</strong>.
            </>
          )}
        </p>
      </div>
    </div>
  );
};
