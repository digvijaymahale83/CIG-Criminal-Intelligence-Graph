import React, { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  Activity,
  AlertTriangle,
  BarChart3,
  Bot,
  FileCheck2,
  FileText,
  FolderGit2,
  LayoutDashboard,
  MapPin,
  Network,
  Search,
  ShieldAlert,
  SlidersHorizontal,
  Users2,
  X,
} from 'lucide-react';
import { MAIN_NAV_ITEMS } from '../../config/navigation';

interface CommandPaletteProps {
  isOpen: boolean;
  onClose: () => void;
}

export const CommandPalette: React.FC<CommandPaletteProps> = ({ isOpen, onClose }) => {
  const [query, setQuery] = useState('');
  const navigate = useNavigate();

  useEffect(() => {
    const handleKeyDown = (e: KeyboardEvent) => {
      if ((e.metaKey || e.ctrlKey) && e.key === 'k') {
        e.preventDefault();
        if (isOpen) {
          onClose();
        } else {
          // Open
          (window as unknown as { toggleCommandPalette?: () => void }).toggleCommandPalette?.();
        }
      }
      if (e.key === 'Escape' && isOpen) {
        onClose();
      }
    };
    window.addEventListener('keydown', handleKeyDown);
    return () => window.removeEventListener('keydown', handleKeyDown);
  }, [isOpen, onClose]);

  if (!isOpen) return null;

  const filteredItems = MAIN_NAV_ITEMS.filter((item) =>
    item.label.toLowerCase().includes(query.toLowerCase())
  );

  const getIcon = (iconName: string) => {
    switch (iconName) {
      case 'LayoutDashboard':
        return <LayoutDashboard className="w-4 h-4 text-[var(--color-text-muted)]" />;
      case 'Activity':
        return <Activity className="w-4 h-4 text-[var(--color-text-muted)]" />;
      case 'FolderGit2':
        return <FolderGit2 className="w-4 h-4 text-[var(--color-text-muted)]" />;
      case 'FileCheck2':
        return <FileCheck2 className="w-4 h-4 text-[var(--color-text-muted)]" />;
      case 'Users2':
        return <Users2 className="w-4 h-4 text-[var(--color-text-muted)]" />;
      case 'Network':
        return <Network className="w-4 h-4 text-[var(--color-text-muted)]" />;
      case 'BarChart3':
        return <BarChart3 className="w-4 h-4 text-[var(--color-text-muted)]" />;
      case 'AlertTriangle':
        return <AlertTriangle className="w-4 h-4 text-[var(--color-text-muted)]" />;
      case 'MapPin':
        return <MapPin className="w-4 h-4 text-[var(--color-text-muted)]" />;
      case 'Bot':
        return <Bot className="w-4 h-4 text-[var(--color-text-muted)]" />;
      case 'FileText':
        return <FileText className="w-4 h-4 text-[var(--color-text-muted)]" />;
      case 'ShieldAlert':
        return <ShieldAlert className="w-4 h-4 text-[var(--color-text-muted)]" />;
      case 'SlidersHorizontal':
        return <SlidersHorizontal className="w-4 h-4 text-[var(--color-text-muted)]" />;
      default:
        return <Activity className="w-4 h-4 text-[var(--color-text-muted)]" />;
    }
  };

  const handleSelect = (path: string) => {
    navigate(path);
    onClose();
  };

  return (
    <div
      id="command-palette-modal"
      className="fixed inset-0 z-50 flex items-start justify-center pt-20 p-4 bg-black/60 backdrop-blur-xs"
      role="dialog"
      aria-modal="true"
    >
      <div className="fixed inset-0" onClick={onClose} />
      <div className="relative w-full max-w-lg rounded-xl bg-[var(--color-surface)] border border-[var(--color-border)] shadow-2xl overflow-hidden z-10 transition-colors">
        <div className="flex items-center px-4 py-3 border-b border-[var(--color-border)] gap-3 bg-[var(--color-surface-subtle)]">
          <Search className="w-4 h-4 text-[var(--color-text-muted)] shrink-0" />
          <input
            type="text"
            placeholder="Type a command or jump to workspace section..."
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            className="w-full bg-transparent text-sm text-[var(--color-text-primary)] placeholder-[var(--color-text-muted)] outline-none"
            autoFocus
          />
          <button
            onClick={onClose}
            className="p-1 rounded text-[var(--color-text-muted)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-surface)] transition-colors cursor-pointer"
          >
            <X className="w-4 h-4" />
          </button>
        </div>

        <div className="max-h-80 overflow-y-auto p-2">
          <div className="text-[10px] uppercase font-mono text-[var(--color-text-muted)] px-3 py-1 font-semibold">
            Navigation & Capabilities
          </div>
          {filteredItems.map((item) => (
            <button
              key={item.id}
              onClick={() => handleSelect(item.path)}
              className="w-full flex items-center justify-between px-3 py-2.5 rounded-lg hover:bg-[var(--color-surface-subtle)] text-left transition-colors group cursor-pointer"
            >
              <div className="flex items-center gap-3">
                {getIcon(item.iconName)}
                <span className="text-xs font-medium text-[var(--color-text-primary)] group-hover:text-[var(--color-accent)]">
                  {item.label}
                </span>
              </div>
              <div className="flex items-center gap-2">
                {item.badge && (
                  <span className="text-[10px] font-mono px-1.5 py-0.5 rounded bg-emerald-500/15 text-emerald-500 border border-emerald-500/30">
                    {item.badge}
                  </span>
                )}
                <span className="text-[10px] font-mono text-[var(--color-text-muted)]">
                  {item.phase === 1 ? 'Phase 1 (Active)' : `Phase ${item.phase}`}
                </span>
              </div>
            </button>
          ))}
        </div>

        <div className="px-4 py-2 border-t border-[var(--color-border)] bg-[var(--color-surface-subtle)] flex items-center justify-between text-[11px] text-[var(--color-text-muted)]">
          <div className="flex items-center gap-2">
            <span>Navigation:</span>
            <kbd className="px-1.5 py-0.5 rounded bg-[var(--color-surface)] border border-[var(--color-border)] text-[var(--color-text-primary)] font-mono text-[10px]">
              Ctrl+K
            </kbd>
          </div>
          <span>Case Isolation Enforced</span>
        </div>
      </div>
    </div>
  );
};
