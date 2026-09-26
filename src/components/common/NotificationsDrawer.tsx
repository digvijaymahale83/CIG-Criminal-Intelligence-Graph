import React from 'react';
import { Drawer } from './Drawer';
import { AppNotification } from '../../types';
import { CheckCircle2, AlertTriangle, Info } from 'lucide-react';
import { Button } from './Button';

interface NotificationsDrawerProps {
  isOpen: boolean;
  onClose: () => void;
  notifications: AppNotification[];
  onMarkAllRead: () => void;
}

export const NotificationsDrawer: React.FC<NotificationsDrawerProps> = ({
  isOpen,
  onClose,
  notifications,
  onMarkAllRead,
}) => {
  return (
    <Drawer
      isOpen={isOpen}
      onClose={onClose}
      title="Investigative Notifications"
      subtitle="System alerts, pipeline events, and audit flags"
      width="md"
    >
      <div className="space-y-3">
        <div className="flex items-center justify-between pb-2 border-b border-[var(--color-border)]">
          <span className="text-xs text-[var(--color-text-secondary)]">
            {notifications.filter((n) => !n.read).length} unread updates
          </span>
          <Button variant="ghost" size="sm" onClick={onMarkAllRead}>
            Mark all read
          </Button>
        </div>

        {notifications.length === 0 ? (
          <div className="text-center py-8 text-[var(--color-text-muted)] text-xs">
            No current alerts or notifications.
          </div>
        ) : (
          <div className="space-y-2.5">
            {notifications.map((item) => (
              <div
                key={item.id}
                className={`p-3 rounded-lg border text-xs transition-colors ${
                  item.read
                    ? 'bg-[var(--color-surface-subtle)] border-[var(--color-border)] text-[var(--color-text-secondary)]'
                    : 'bg-[var(--color-surface)] border-[var(--color-accent)]/40 text-[var(--color-text-primary)] shadow-xs'
                }`}
              >
                <div className="flex items-start gap-2.5">
                  {item.type === 'success' ? (
                    <CheckCircle2 className="w-4 h-4 text-emerald-500 shrink-0 mt-0.5" />
                  ) : item.type === 'warning' ? (
                    <AlertTriangle className="w-4 h-4 text-amber-500 shrink-0 mt-0.5" />
                  ) : (
                    <Info className="w-4 h-4 text-[var(--color-accent)] shrink-0 mt-0.5" />
                  )}
                  <div className="flex-1 space-y-1">
                    <div className="flex items-center justify-between">
                      <span className="font-semibold text-[var(--color-text-primary)]">{item.title}</span>
                      <span className="text-[10px] text-[var(--color-text-muted)] font-mono">
                        {new Date(item.timestampUtc).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' })}
                      </span>
                    </div>
                    <p className="text-[11px] text-[var(--color-text-secondary)] leading-relaxed">{item.message}</p>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}
      </div>
    </Drawer>
  );
};
