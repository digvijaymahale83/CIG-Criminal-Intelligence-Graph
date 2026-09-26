import React from 'react';
import { NavLink, useNavigate } from 'react-router-dom';
import {
  LayoutDashboard,
  Map,
  FolderOpen,
  GitBranch,
  GitMerge,
  Bell,
  FolderSearch,
  FileText,
  ShieldCheck,
  LogOut,
  Clock,
  BarChart3,
  Users,
} from 'lucide-react';
import { useAuth } from '../../hooks/useAuth';
import { useTranslation } from '../../i18n';

interface SidebarProps {
  isOpen: boolean;
  onClose: () => void;
}

export const Sidebar: React.FC<SidebarProps> = ({ isOpen, onClose }) => {
  const { user, logout } = useAuth();
  const { t } = useTranslation();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const navSections = [
    {
      label: 'INTELLIGENCE',
      items: [
        { id: 'overview', label: t('common.nav.overview'), path: '/dashboard', icon: LayoutDashboard },
        { id: 'investigations', label: t('common.nav.investigations'), path: '/investigations', icon: FolderOpen },
        { id: 'cases-evidence', label: t('common.nav.evidence'), path: '/evidence', icon: FolderSearch },
        { id: 'entity-intel', label: t('common.nav.entities'), path: '/entities', icon: Users },
        { id: 'knowledge-graph', label: t('common.nav.graph'), path: '/network', icon: GitBranch },
        { id: 'graph-analytics', label: t('common.nav.analytics'), path: '/analytics', icon: BarChart3 },
        { id: 'timeline-events', label: t('common.nav.timeline'), path: '/timeline', icon: Clock },
        { id: 'cross-case-resolution', label: t('common.nav.resolution'), path: '/entity-resolution', icon: GitMerge },
        { id: 'network-map', label: t('common.nav.map'), path: '/map', icon: Map },
      ],
    },
    {
      label: 'OPERATIONS',
      items: [
        { id: 'alerts-anomalies', label: t('common.nav.alerts'), path: '/alerts', icon: Bell },
        { id: 'intel-reports', label: t('common.nav.reports'), path: '/reports', icon: FileText },
      ],
    },
    {
      label: 'ADMINISTRATION',
      items: [
        { id: 'compliance-audit', label: t('common.nav.audit'), path: '/audit', icon: ShieldCheck },
      ],
    },
  ];

  return (
    <>
      {/* Mobile backdrop */}
      {isOpen && (
        <div
          className="fixed inset-0 bg-black/60 z-30 lg:hidden backdrop-blur-sm"
          onClick={onClose}
          aria-hidden="true"
        />
      )}

      <aside
        id="app-navigation-sidebar"
        className={`fixed top-0 bottom-0 left-0 z-30 w-48 bg-[var(--color-surface)] border-r border-[var(--color-border)] flex flex-col transition-transform duration-200 ease-in-out lg:translate-x-0 ${
          isOpen ? 'translate-x-0' : '-translate-x-full'
        }`}
      >
        {/* Brand Header */}
        <div className="h-14 flex items-center px-4 border-b border-[var(--color-border)] gap-2.5 shrink-0">
          <div className="w-7 h-7 bg-[var(--color-accent)] rounded flex items-center justify-center font-bold text-white shadow-sm text-xs flex-shrink-0">
            <span className="font-black tracking-tighter">CIG</span>
          </div>
          <div className="leading-tight min-w-0">
            <div className="font-bold text-xs text-[var(--color-text-primary)] tracking-wide truncate">CIG</div>
            <div className="text-[8px] text-[var(--color-text-muted)] uppercase tracking-wider truncate">Criminal Intelligence Graph</div>
          </div>
        </div>

        {/* Scrollable Navigation Area */}
        <div className="flex-1 overflow-y-auto py-3 px-2 space-y-4">
          {navSections.map((section) => (
            <div key={section.label}>
              <p className="text-[9px] font-bold text-[var(--color-text-muted)] uppercase tracking-widest px-2 mb-1.5">
                {section.label}
              </p>
              <ul className="space-y-0.5">
                {section.items.map((item) => {
                  const Icon = item.icon;
                  return (
                    <li key={item.id}>
                      <NavLink
                        to={item.path}
                        onClick={() => onClose()}
                        className={({ isActive }) =>
                          `flex items-center gap-2 px-2.5 py-1.5 rounded-md transition-colors text-xs font-medium ${
                            isActive
                              ? 'bg-[var(--color-accent)]/15 text-[var(--color-accent)] border border-[var(--color-accent)]/30 font-semibold'
                              : 'text-[var(--color-text-secondary)] hover:bg-[var(--color-surface-subtle)] hover:text-[var(--color-text-primary)]'
                          }`
                        }
                      >
                        <Icon className="w-3.5 h-3.5 shrink-0" />
                        <span className="truncate">{item.label}</span>
                      </NavLink>
                    </li>
                  );
                })}
              </ul>
            </div>
          ))}
        </div>

        {/* User Footer */}
        <div className="p-3 border-t border-[var(--color-border)] shrink-0">
          <div className="flex items-center gap-2 p-2 bg-[var(--color-surface-subtle)] border border-[var(--color-border-subtle)] rounded-lg">
            <div className="w-7 h-7 rounded-full bg-[var(--color-accent)]/20 border border-[var(--color-accent)]/40 flex items-center justify-center text-[10px] font-bold text-[var(--color-accent)] shrink-0">
              {user ? user.name.slice(0, 2).toUpperCase() : 'AU'}
            </div>
            <div className="flex-1 overflow-hidden min-w-0">
              <p className="text-[11px] font-semibold text-[var(--color-text-primary)] truncate">
                {user?.name || 'Admin User'}
              </p>
              <p className="text-[9px] text-[var(--color-text-muted)] truncate font-mono">
                {user?.role || 'Administrator'}
              </p>
            </div>
            <button
              onClick={handleLogout}
              className="text-[var(--color-text-muted)] hover:text-red-500 cursor-pointer shrink-0"
              title="Sign out"
            >
              <LogOut className="w-3 h-3" />
            </button>
          </div>
          <div className="mt-1.5 flex items-center justify-center">
            <span className="text-[9px] font-mono text-[var(--color-text-muted)] bg-[var(--color-surface-subtle)] px-2 py-0.5 rounded border border-[var(--color-border-subtle)]">
              OP-0001
            </span>
          </div>
        </div>
      </aside>
    </>
  );
};
