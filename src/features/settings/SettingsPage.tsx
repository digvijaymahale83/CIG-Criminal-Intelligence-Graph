import React from 'react';
import { SlidersHorizontal, ShieldCheck, Database, Key, Bell, User } from 'lucide-react';
import { Card } from '../../components/common/Card';
import { Button } from '../../components/common/Button';
import { useAuth } from '../../hooks/useAuth';
import { ResponsibleAiNotice } from '../../components/common/ResponsibleAiNotice';

export const SettingsPage: React.FC = () => {
  const { user } = useAuth();

  return (
    <div className="space-y-6" id="settings-page">
      <div className="flex flex-col sm:flex-row sm:items-center justify-between gap-4 pb-4 border-b border-[var(--color-border)]">
        <div>
          <div className="flex items-center gap-2.5">
            <h1 className="text-xl font-bold text-[var(--color-text-primary)] tracking-tight">Platform Configuration & Clearance</h1>
            <span className="text-[10px] font-mono px-2 py-0.5 rounded bg-[var(--color-surface-subtle)] text-[var(--color-text-muted)] border border-[var(--color-border)]">
              OPERATOR SETTINGS
            </span>
          </div>
          <p className="text-xs text-[var(--color-text-secondary)] mt-1">
            Investigator profile, security credentials, active session parameters, and endpoint diagnostics.
          </p>
        </div>
      </div>

      <ResponsibleAiNotice compact />

      <div className="grid grid-cols-1 md:grid-cols-2 gap-5">
        <Card title="Investigator Profile" subtitle="Active credential & clearance assignment">
          <div className="space-y-3 text-xs">
            <div>
              <span className="text-[10px] font-mono text-[var(--color-text-muted)] uppercase">Operator Name</span>
              <p className="font-semibold text-[var(--color-text-primary)] mt-0.5">{user?.name}</p>
            </div>
            <div>
              <span className="text-[10px] font-mono text-[var(--color-text-muted)] uppercase">Official Email</span>
              <p className="font-mono text-[var(--color-text-primary)] mt-0.5">{user?.email}</p>
            </div>
            <div>
              <span className="text-[10px] font-mono text-[var(--color-text-muted)] uppercase">Role & Clearance</span>
              <div className="flex items-center gap-2 mt-0.5">
                <span className="text-[10px] font-mono px-2 py-0.5 rounded bg-[var(--color-surface-subtle)] text-[var(--color-accent)] border border-[var(--color-border)] font-semibold">
                  {user?.role.toUpperCase()}
                </span>
                {user?.badgeNumber && (
                  <span className="text-[10px] font-mono px-1.5 py-0.5 rounded bg-[var(--color-surface-subtle)] text-[var(--color-text-secondary)] border border-[var(--color-border-subtle)]">
                    Badge: {user.badgeNumber}
                  </span>
                )}
              </div>
            </div>
            <div>
              <span className="text-[10px] font-mono text-[var(--color-text-muted)] uppercase">Agency</span>
              <p className="text-[var(--color-text-primary)] mt-0.5">{user?.agency}</p>
            </div>
          </div>
        </Card>

        <Card title="Platform Connectivity" subtitle="Backend API base URL and health telemetry">
          <div className="space-y-3 text-xs">
            <div>
              <span className="text-[10px] font-mono text-[var(--color-text-muted)] uppercase">API Endpoint Gateway</span>
              <p className="font-mono text-[var(--color-text-primary)] mt-0.5">
                {import.meta.env.VITE_API_BASE_URL || '/api/v1 (Reverse Proxy Route)'}
              </p>
            </div>
            <div>
              <span className="text-[10px] font-mono text-[var(--color-text-muted)] uppercase">Health Endpoint</span>
              <p className="font-mono text-[#1E5C2B] font-semibold mt-0.5">GET /api/v1/system/status</p>
            </div>
            <div>
              <span className="text-[10px] font-mono text-[var(--color-text-muted)] uppercase">Polling Frequency</span>
              <p className="text-[var(--color-text-primary)] mt-0.5">30 seconds (automatic background refresh)</p>
            </div>
            <div>
              <span className="text-[10px] font-mono text-[var(--color-text-muted)] uppercase">Session Expiry</span>
              <p className="font-mono text-[var(--color-text-muted)] mt-0.5">{user?.sessionExpiresAtUtc || 'Active'}</p>
            </div>
          </div>
        </Card>
      </div>
    </div>
  );
};
