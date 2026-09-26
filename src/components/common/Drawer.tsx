import React, { useEffect } from 'react';
import { X } from 'lucide-react';

interface DrawerProps {
  isOpen: boolean;
  onClose: () => void;
  title: string;
  subtitle?: string;
  children: React.ReactNode;
  footer?: React.ReactNode;
  width?: 'md' | 'lg' | 'xl';
  id?: string;
}

export const Drawer: React.FC<DrawerProps> = ({
  isOpen,
  onClose,
  title,
  subtitle,
  children,
  footer,
  width = 'md',
  id,
}) => {
  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if (e.key === 'Escape' && isOpen) {
        onClose();
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [isOpen, onClose]);

  if (!isOpen) return null;

  let widthClass = 'max-w-md';
  if (width === 'lg') widthClass = 'max-w-lg';
  if (width === 'xl') widthClass = 'max-w-2xl';

  return (
    <div id={id} className="fixed inset-0 z-50 overflow-hidden" role="dialog" aria-modal="true">
      <div className="fixed inset-0 bg-black/60 backdrop-blur-xs transition-opacity" onClick={onClose} />
      <div className="fixed inset-y-0 right-0 pl-10 max-w-full flex">
        <div
          className={`w-screen ${widthClass} bg-[var(--color-surface)] border-l border-[var(--color-border)] shadow-2xl flex flex-col transition-colors`}
        >
          <div className="px-5 py-4 border-b border-[var(--color-border)] flex items-center justify-between gap-4 bg-[var(--color-surface-subtle)]">
            <div>
              <h3 className="text-sm font-semibold text-[var(--color-text-primary)] tracking-wide">{title}</h3>
              {subtitle && <p className="text-xs text-[var(--color-text-secondary)] mt-0.5">{subtitle}</p>}
            </div>
            <button
              onClick={onClose}
              className="p-1 rounded text-[var(--color-text-muted)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-surface)] transition-colors cursor-pointer"
              aria-label="Close panel"
            >
              <X className="w-4 h-4" />
            </button>
          </div>
          <div className="p-5 overflow-y-auto flex-1 text-xs text-[var(--color-text-primary)]">{children}</div>
          {footer && (
            <div className="px-5 py-3 border-t border-[var(--color-border)] bg-[var(--color-surface-subtle)] flex items-center justify-end gap-2">
              {footer}
            </div>
          )}
        </div>
      </div>
    </div>
  );
};
