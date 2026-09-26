import React from 'react';
import { Database, Plus } from 'lucide-react';
import { Button } from './Button';

interface EmptyStateProps {
  title: string;
  description: string;
  icon?: React.ReactNode;
  actionLabel?: string;
  onAction?: () => void;
  id?: string;
  className?: string;
}

export const EmptyState: React.FC<EmptyStateProps> = ({
  title,
  description,
  icon,
  actionLabel,
  onAction,
  id,
  className = '',
}) => {
  return (
    <div
      id={id}
      className={`rounded-xl border border-dashed border-[var(--color-border)] bg-[var(--color-surface-subtle)] p-8 text-center flex flex-col items-center justify-center max-w-lg mx-auto transition-colors ${className}`}
    >
      <div className="w-12 h-12 rounded-full bg-[var(--color-surface)] border border-[var(--color-border)] flex items-center justify-center text-[var(--color-text-muted)] mb-3 shadow-xs">
        {icon || <Database className="w-5 h-5 text-[var(--color-accent)]" />}
      </div>
      <h4 className="text-sm font-semibold text-[var(--color-text-primary)] mb-1">{title}</h4>
      <p className="text-xs text-[var(--color-text-secondary)] max-w-md leading-relaxed mb-4">{description}</p>
      {actionLabel && onAction && (
        <Button variant="secondary" size="sm" onClick={onAction} icon={<Plus className="w-3.5 h-3.5" />}>
          {actionLabel}
        </Button>
      )}
    </div>
  );
};
