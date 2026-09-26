import React from 'react';
import { Link } from 'react-router-dom';
import { Activity, AlertTriangle, CheckCircle2, XCircle } from 'lucide-react';
import { useSystemStatus } from '../../hooks/useSystemStatus';

export const SystemHealthIndicator: React.FC = () => {
  const { level, isDegraded, isError, isLoading } = useSystemStatus();

  let badgeColor = 'bg-emerald-500/10 text-emerald-600 dark:text-emerald-400 border-emerald-500/30 hover:bg-emerald-500/20';
  let icon = <CheckCircle2 className="w-3.5 h-3.5 text-emerald-500" />;
  let label = 'System Ready';

  if (isLoading) {
    badgeColor = 'bg-sky-500/10 text-sky-600 dark:text-sky-400 border-sky-500/30 animate-pulse';
    icon = <Activity className="w-3.5 h-3.5 text-sky-500 animate-spin" />;
    label = 'Checking...';
  } else if (isDegraded || level === 'Degraded') {
    badgeColor = 'bg-amber-500/10 text-amber-600 dark:text-amber-400 border-amber-500/30 hover:bg-amber-500/20';
    icon = <AlertTriangle className="w-3.5 h-3.5 text-amber-500" />;
    label = 'System Degraded';
  } else if (isError || level === 'Unavailable') {
    badgeColor = 'bg-red-500/10 text-red-600 dark:text-red-400 border-red-500/30 hover:bg-red-500/20';
    icon = <XCircle className="w-3.5 h-3.5 text-red-500" />;
    label = 'System Unavailable';
  }

  return (
    <Link
      to="/system-status"
      id="top-system-health-indicator"
      title="Live Platform Status (GET /api/v1/system/status)"
      className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full border text-xs font-medium transition-colors ${badgeColor}`}
    >
      {icon}
      <span>{label}</span>
      <span className="text-[10px] uppercase font-mono px-1 rounded bg-[var(--color-surface)] text-[var(--color-text-secondary)] border border-[var(--color-border)] ml-1">
        LIVE
      </span>
    </Link>
  );
};
