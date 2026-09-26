import React, { useState, useEffect } from 'react';
import { Outlet } from 'react-router-dom';
import { Header } from './Header';
import { Sidebar } from './Sidebar';
import { CommandPalette } from '../common/CommandPalette';
import { GlobalSearchModal } from '../common/GlobalSearchModal';
import { NotificationsDrawer } from '../common/NotificationsDrawer';
import { AppNotification } from '../../types';

export const AppShell: React.FC = () => {
  const [isSidebarOpen, setIsSidebarOpen] = useState(false);
  const [isCommandPaletteOpen, setIsCommandPaletteOpen] = useState(false);
  const [isSearchOpen, setIsSearchOpen] = useState(false);
  const [isNotificationsOpen, setIsNotificationsOpen] = useState(false);

  const [notifications, setNotifications] = useState<AppNotification[]>([
    {
      id: 'notif-1',
      title: 'Entity Cluster Detected',
      message: 'Entity cluster detected in Sector 7 — Meridian Holdings LLC flagged as CRITICAL RISK.',
      type: 'success',
      timestampUtc: new Date().toISOString(),
      read: false,
    },
    {
      id: 'notif-2',
      title: 'High-Risk Relationship Identified',
      message: 'High-risk relationship identified between Target A and Unknown Vehicle.',
      type: 'info',
      timestampUtc: new Date(Date.now() - 3600000).toISOString(),
      read: false,
    },
    {
      id: 'notif-3',
      title: 'New HR Record Ingested',
      message: 'New HR record ingested successfully at 09:15 AM.',
      type: 'success',
      timestampUtc: new Date(Date.now() - 7200000).toISOString(),
      read: true,
    },
  ]);

  useEffect(() => {
    (window as unknown as { toggleCommandPalette?: () => void }).toggleCommandPalette = () => {
      setIsCommandPaletteOpen((prev) => !prev);
    };
  }, []);

  const handleMarkAllNotificationsRead = () => {
    setNotifications((prev) => prev.map((n) => ({ ...n, read: true })));
  };

  return (
    <div className="flex h-screen w-full bg-[var(--color-bg-canvas)] text-[var(--color-text-primary)] font-sans overflow-hidden">
      {/* Navigation Sidebar — 176px wide */}
      <Sidebar isOpen={isSidebarOpen} onClose={() => setIsSidebarOpen(false)} />

      {/* Main Workspace Area (offset by sidebar width on desktop) */}
      <div className="flex-1 flex flex-col min-w-0 lg:pl-44 h-screen overflow-hidden bg-[var(--color-bg-canvas)]">
        {/* Top Header */}
        <Header
          onToggleSidebar={() => setIsSidebarOpen(!isSidebarOpen)}
          isSidebarOpen={isSidebarOpen}
          onOpenCommandPalette={() => setIsCommandPaletteOpen(true)}
          onOpenSearch={() => setIsSearchOpen(true)}
          onOpenNotifications={() => setIsNotificationsOpen(true)}
          notifications={notifications}
        />

        {/* Scrollable Content Pane */}
        <main id="main-investigation-workspace" className="flex-1 overflow-y-auto flex flex-col">
          <div className="flex-1 p-5 sm:p-6 flex flex-col gap-5 max-w-[1400px] w-full mx-auto">
            <Outlet />
          </div>
        </main>
      </div>

      {/* Overlays & Modals */}
      <CommandPalette
        isOpen={isCommandPaletteOpen}
        onClose={() => setIsCommandPaletteOpen(false)}
      />
      <GlobalSearchModal
        isOpen={isSearchOpen}
        onClose={() => setIsSearchOpen(false)}
      />
      <NotificationsDrawer
        isOpen={isNotificationsOpen}
        onClose={() => setIsNotificationsOpen(false)}
        notifications={notifications}
        onMarkAllRead={handleMarkAllNotificationsRead}
      />
    </div>
  );
};
