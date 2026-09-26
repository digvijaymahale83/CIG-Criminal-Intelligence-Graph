import React from 'react';
import { AlertCircle, AlertTriangle, CheckCircle2, Info } from 'lucide-react';

interface AlertProps {
  type?: 'info' | 'warning' | 'error' | 'success';
  title?: string;
  children: React.ReactNode;
  action?: React.ReactNode;
  id?: string;
  className?: string;
}

export const Alert: React.FC<AlertProps> = ({
  type = 'info',
  title,
  children,
  action,
  id,
  className = '',
}) => {
  let containerStyles = 'bg-[var(--color-accent)]/10 border-[var(--color-accent)]/30 text-[var(--color-text-primary)]';
  let icon = <Info className="w-4 h-4 text-[var(--color-accent)] shrink-0 mt-0.5" />;

  switch (type) {
    case 'warning':
      containerStyles = 'bg-amber-500/15 border-amber-500/30 text-[var(--color-text-primary)]';
      icon = <AlertTriangle className="w-4 h-4 text-amber-500 shrink-0 mt-0.5" />;
      break;
    case 'error':
      containerStyles = 'bg-red-500/15 border-red-500/30 text-[var(--color-text-primary)]';
      icon = <AlertCircle className="w-4 h-4 text-red-500 shrink-0 mt-0.5" />;
      break;
    case 'success':
      containerStyles = 'bg-emerald-500/15 border-emerald-500/30 text-[var(--color-text-primary)]';
      icon = <CheckCircle2 className="w-4 h-4 text-emerald-500 shrink-0 mt-0.5" />;
      break;
    case 'info':
    default:
      containerStyles = 'bg-[var(--color-accent)]/15 border-[var(--color-accent)]/30 text-[var(--color-text-primary)]';
      icon = <Info className="w-4 h-4 text-[var(--color-accent)] shrink-0 mt-0.5" />;
      break;
  }

  return (
    <div
      id={id}
      role="alert"
      className={`rounded-xl border p-4 flex gap-3 text-xs leading-relaxed transition-colors ${containerStyles} ${className}`}
    >
      {icon}
      <div className="flex-1">
        {title && <h4 className="font-semibold tracking-wide mb-1 text-[var(--color-text-primary)]">{title}</h4>}
        <div className="text-[var(--color-text-secondary)]">{children}</div>
      </div>
      {action && <div className="shrink-0 self-center">{action}</div>}
    </div>
  );
};
