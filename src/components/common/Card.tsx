import React from 'react';

export interface CardProps extends React.HTMLAttributes<HTMLDivElement> {
  children: React.ReactNode;
  header?: React.ReactNode;
  footer?: React.ReactNode;
  title?: string;
  subtitle?: string;
  action?: React.ReactNode;
  id?: string;
}

export const Card: React.FC<CardProps> = ({
  children,
  header,
  footer,
  title,
  subtitle,
  action,
  className = '',
  id,
  ...props
}) => {
  return (
    <div
      id={id}
      className={`rounded-xl bg-[var(--color-surface)] border border-[var(--color-border)] shadow-xs overflow-hidden flex flex-col text-[var(--color-text-primary)] transition-colors ${className}`}
      {...props}
    >
      {(header || title) && (
        <div className="px-5 py-4 border-b border-[var(--color-border)] flex items-center justify-between gap-4">
          {header ? (
            header
          ) : (
            <div>
              {title && <h3 className="text-sm font-semibold text-[var(--color-text-primary)] tracking-wide">{title}</h3>}
              {subtitle && <p className="text-xs text-[var(--color-text-secondary)] mt-0.5">{subtitle}</p>}
            </div>
          )}
          {action && <div className="flex items-center gap-2">{action}</div>}
        </div>
      )}
      <div className="p-5 flex-1">{children}</div>
      {footer && <div className="px-5 py-3 border-t border-[var(--color-border)] bg-[var(--color-surface-subtle)] text-xs text-[var(--color-text-secondary)]">{footer}</div>}
    </div>
  );
};
