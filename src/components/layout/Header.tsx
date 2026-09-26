import React, { useState } from 'react';
import { Bell, Menu, X, LogOut, Sun, Moon, Laptop, Sparkles, ChevronDown, Languages } from 'lucide-react';
import { useAuth } from '../../hooks/useAuth';
import { useTheme } from '../../hooks/useTheme';
import { useTranslation } from '../../i18n';
import { AppNotification } from '../../types';

interface HeaderProps {
  onToggleSidebar: () => void;
  isSidebarOpen: boolean;
  onOpenCommandPalette: () => void;
  onOpenSearch: () => void;
  onOpenNotifications: () => void;
  notifications: AppNotification[];
}

export const Header: React.FC<HeaderProps> = ({
  onToggleSidebar,
  isSidebarOpen,
  onOpenSearch,
  onOpenNotifications,
  notifications,
}) => {
  const { user, logout } = useAuth();
  const { theme, resolvedTheme, setTheme } = useTheme();
  const { language, setLanguage, t, supportedLanguages } = useTranslation();

  const [isUserMenuOpen, setIsUserMenuOpen] = useState(false);
  const [isThemeMenuOpen, setIsThemeMenuOpen] = useState(false);
  const [isLangMenuOpen, setIsLangMenuOpen] = useState(false);

  const unreadCount = notifications.filter((n) => !n.read).length;

  return (
    <header
      id="app-top-header"
      className="h-14 border-b border-[var(--color-border)] flex items-center justify-between px-4 bg-[var(--color-surface)]/95 backdrop-blur-md sticky top-0 z-20 transition-colors duration-200"
    >
      {/* Left: Mobile Toggle + Brand context */}
      <div className="flex items-center gap-3">
        <button
          onClick={onToggleSidebar}
          className="p-1.5 rounded-md text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] hover:bg-[var(--color-surface-subtle)] lg:hidden cursor-pointer"
          aria-label="Toggle navigation sidebar"
        >
          {isSidebarOpen ? <X className="w-4 h-4" /> : <Menu className="w-4 h-4" />}
        </button>

        {/* Role clearance badge */}
        <div className="hidden sm:flex items-center gap-2">
          <span className="text-[11px] text-[var(--color-text-muted)] font-mono">{t('common.roleClearance', { count: 1 }) || 'Role Clearance'}:</span>
          <span className="text-[11px] font-semibold text-[var(--color-text-primary)] px-2 py-0.5 rounded bg-[var(--color-surface-subtle)] border border-[var(--color-border-subtle)] font-mono">
            {user?.role ? user.role.toUpperCase() : 'ADMINISTRATOR'}
          </span>
          <span className="text-[10px] text-[var(--color-text-muted)] font-mono hidden md:inline">
            OP-0001
          </span>
        </div>
      </div>

      {/* Right: AI Copilot, Language, Theme, Notifications, User */}
      <div className="flex items-center gap-1.5 sm:gap-2.5">
        {/* AI Copilot Button */}
        <button
          onClick={onOpenSearch}
          className="flex items-center gap-1.5 px-3 py-1.5 rounded-lg bg-[var(--color-accent)]/15 border border-[var(--color-accent)]/40 hover:bg-[var(--color-accent)]/25 transition-colors cursor-pointer"
          title="Open AI Copilot"
        >
          <Sparkles className="w-3.5 h-3.5 text-[var(--color-accent)]" />
          <span className="text-xs font-semibold text-[var(--color-accent)] hidden sm:inline">
            {t('common.nav.copilot')}
          </span>
          <span className="w-1.5 h-1.5 rounded-full bg-emerald-500 animate-pulse" />
        </button>

        {/* Language Selector Dropdown */}
        <div className="relative">
          <button
            onClick={() => {
              setIsLangMenuOpen(!isLangMenuOpen);
              setIsThemeMenuOpen(false);
              setIsUserMenuOpen(false);
            }}
            className="flex items-center gap-1 px-2.5 py-1.5 rounded-lg border border-[var(--color-border-subtle)] bg-[var(--color-surface-subtle)] hover:border-[var(--color-accent)]/40 text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] transition-colors cursor-pointer text-xs font-medium"
            aria-label="Select Language"
            id="header-language-select"
          >
            <Languages className="w-3.5 h-3.5 text-[var(--color-accent)]" />
            <span className="uppercase font-mono text-[11px]">
              {language === 'mr' ? 'मराठी' : 'EN'}
            </span>
            <ChevronDown className="w-3 h-3 opacity-60" />
          </button>

          {isLangMenuOpen && (
            <>
              <div className="fixed inset-0 z-40" onClick={() => setIsLangMenuOpen(false)} />
              <div className="absolute right-0 mt-2 w-40 rounded-xl bg-[var(--color-surface)] border border-[var(--color-border)] shadow-xl p-1.5 z-50">
                <div className="text-[10px] font-mono uppercase px-2 py-1 text-[var(--color-text-muted)]">
                  Language / भाषा
                </div>
                {supportedLanguages.map((lang) => (
                  <button
                    key={lang.code}
                    onClick={() => {
                      setLanguage(lang.code);
                      setIsLangMenuOpen(false);
                    }}
                    className={`w-full flex items-center justify-between px-2.5 py-1.5 rounded-md text-xs transition-colors cursor-pointer ${
                      language === lang.code
                        ? 'bg-[var(--color-accent)]/15 text-[var(--color-accent)] font-semibold'
                        : 'text-[var(--color-text-secondary)] hover:bg-[var(--color-surface-subtle)] hover:text-[var(--color-text-primary)]'
                    }`}
                  >
                    <span>{lang.nativeLabel}</span>
                    <span className="text-[10px] font-mono opacity-70 uppercase">{lang.code}</span>
                  </button>
                ))}
              </div>
            </>
          )}
        </div>

        {/* Theme Switcher Dropdown */}
        <div className="relative">
          <button
            onClick={() => {
              setIsThemeMenuOpen(!isThemeMenuOpen);
              setIsLangMenuOpen(false);
              setIsUserMenuOpen(false);
            }}
            className="w-8 h-8 flex items-center justify-center rounded-lg border border-[var(--color-border-subtle)] bg-[var(--color-surface-subtle)] hover:border-[var(--color-accent)]/40 text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] transition-colors cursor-pointer"
            aria-label="Select Theme Mode"
            id="header-theme-toggle"
            title={`Current theme: ${theme} (${resolvedTheme})`}
          >
            {theme === 'system' ? (
              <Laptop className="w-4 h-4 text-[var(--color-accent)]" />
            ) : resolvedTheme === 'dark' ? (
              <Moon className="w-4 h-4 text-[var(--color-accent)]" />
            ) : (
              <Sun className="w-4 h-4 text-[var(--color-accent)]" />
            )}
          </button>

          {isThemeMenuOpen && (
            <>
              <div className="fixed inset-0 z-40" onClick={() => setIsThemeMenuOpen(false)} />
              <div className="absolute right-0 mt-2 w-36 rounded-xl bg-[var(--color-surface)] border border-[var(--color-border)] shadow-xl p-1.5 z-50">
                <div className="text-[10px] font-mono uppercase px-2 py-1 text-[var(--color-text-muted)]">
                  Appearance
                </div>
                <button
                  onClick={() => {
                    setTheme('light');
                    setIsThemeMenuOpen(false);
                  }}
                  className={`w-full flex items-center gap-2 px-2.5 py-1.5 rounded-md text-xs transition-colors cursor-pointer ${
                    theme === 'light'
                      ? 'bg-[var(--color-accent)]/15 text-[var(--color-accent)] font-semibold'
                      : 'text-[var(--color-text-secondary)] hover:bg-[var(--color-surface-subtle)] hover:text-[var(--color-text-primary)]'
                  }`}
                >
                  <Sun className="w-3.5 h-3.5" />
                  <span>Light</span>
                </button>
                <button
                  onClick={() => {
                    setTheme('dark');
                    setIsThemeMenuOpen(false);
                  }}
                  className={`w-full flex items-center gap-2 px-2.5 py-1.5 rounded-md text-xs transition-colors cursor-pointer ${
                    theme === 'dark'
                      ? 'bg-[var(--color-accent)]/15 text-[var(--color-accent)] font-semibold'
                      : 'text-[var(--color-text-secondary)] hover:bg-[var(--color-surface-subtle)] hover:text-[var(--color-text-primary)]'
                  }`}
                >
                  <Moon className="w-3.5 h-3.5" />
                  <span>Dark</span>
                </button>
                <button
                  onClick={() => {
                    setTheme('system');
                    setIsThemeMenuOpen(false);
                  }}
                  className={`w-full flex items-center gap-2 px-2.5 py-1.5 rounded-md text-xs transition-colors cursor-pointer ${
                    theme === 'system'
                      ? 'bg-[var(--color-accent)]/15 text-[var(--color-accent)] font-semibold'
                      : 'text-[var(--color-text-secondary)] hover:bg-[var(--color-surface-subtle)] hover:text-[var(--color-text-primary)]'
                  }`}
                >
                  <Laptop className="w-3.5 h-3.5" />
                  <span>System</span>
                </button>
              </div>
            </>
          )}
        </div>

        {/* Notifications Bell */}
        <button
          onClick={onOpenNotifications}
          id="header-notifications-button"
          className="w-8 h-8 flex items-center justify-center rounded-lg border border-[var(--color-border-subtle)] bg-[var(--color-surface-subtle)] text-[var(--color-text-secondary)] hover:text-[var(--color-text-primary)] hover:border-[var(--color-accent)]/40 transition-colors relative cursor-pointer"
          aria-label="View notifications"
        >
          {unreadCount > 0 && (
            <span className="absolute top-1.5 right-1.5 w-2 h-2 bg-red-500 rounded-full" />
          )}
          <Bell className="w-4 h-4" />
        </button>

        {/* User Profile */}
        <div className="relative">
          <button
            onClick={() => {
              setIsUserMenuOpen(!isUserMenuOpen);
              setIsThemeMenuOpen(false);
              setIsLangMenuOpen(false);
            }}
            className="flex items-center gap-2 p-1 rounded-full hover:bg-[var(--color-surface-subtle)] transition-colors cursor-pointer"
            aria-expanded={isUserMenuOpen}
            aria-label="User account menu"
            id="header-user-menu-button"
          >
            <div className="w-7 h-7 rounded-full bg-[var(--color-accent)]/20 border border-[var(--color-accent)]/40 flex items-center justify-center text-[10px] font-bold text-[var(--color-accent)]">
              {user ? user.name.slice(0, 2).toUpperCase() : 'AU'}
            </div>
            <span className="hidden xl:block text-xs font-semibold text-[var(--color-text-primary)]">
              OP-0001
            </span>
            <ChevronDown className="hidden xl:block w-3 h-3 text-[var(--color-text-muted)]" />
          </button>

          {isUserMenuOpen && (
            <>
              <div className="fixed inset-0 z-40" onClick={() => setIsUserMenuOpen(false)} />
              <div className="absolute right-0 mt-2 w-72 rounded-xl bg-[var(--color-surface)] border border-[var(--color-border)] shadow-2xl p-3 z-50">
                <div className="pb-2.5 border-b border-[var(--color-border-subtle)]">
                  <div className="flex items-center gap-2.5 mb-2">
                    <div className="w-9 h-9 rounded-full bg-[var(--color-accent)]/20 border border-[var(--color-accent)]/40 flex items-center justify-center text-sm font-bold text-[var(--color-accent)]">
                      {user ? user.name.slice(0, 2).toUpperCase() : 'AU'}
                    </div>
                    <div>
                      <p className="text-xs font-semibold text-[var(--color-text-primary)]">{user?.name || 'Admin User'}</p>
                      <p className="text-[10px] text-[var(--color-text-muted)] font-mono">{user?.email || 'admin@tnpolice.gov.in'}</p>
                    </div>
                  </div>
                  <div className="flex items-center gap-1.5">
                    <span className="text-[10px] font-mono px-2 py-0.5 rounded bg-[var(--color-accent)]/15 text-[var(--color-accent)] border border-[var(--color-accent)]/30">
                      ROLE: {user?.role ? user.role.toUpperCase() : 'ADMINISTRATOR'}
                    </span>
                    <span className="text-[10px] font-mono px-1.5 py-0.5 rounded bg-[var(--color-surface-subtle)] text-[var(--color-text-muted)] border border-[var(--color-border-subtle)]">
                      OP-0001
                    </span>
                  </div>
                </div>
                <div className="py-2 border-b border-[var(--color-border-subtle)]">
                  <p className="text-[10px] text-[var(--color-text-muted)] uppercase font-mono px-1 mb-1">Platform Environment</p>
                  <p className="text-[11px] text-[var(--color-text-secondary)] px-1 leading-relaxed">
                    CIG — Criminal Intelligence Graph (Synthetic Research Environment)
                  </p>
                </div>
                <div className="pt-2">
                  <button
                    onClick={() => { setIsUserMenuOpen(false); logout(); }}
                    className="w-full flex items-center gap-2 px-2 py-1.5 rounded text-red-500 hover:bg-red-500/10 transition-colors text-left cursor-pointer text-xs font-medium"
                  >
                    <LogOut className="w-3.5 h-3.5" />
                    <span>{t('common.logout')}</span>
                  </button>
                </div>
              </div>
            </>
          )}
        </div>
      </div>
    </header>
  );
};
