import React from 'react';
import { GovernanceReviewStatus } from '../../types/governance';
import { PlatformHealthLevel } from '../../types/system';

interface StatusBadgeProps {
  status: GovernanceReviewStatus | PlatformHealthLevel | 'Available' | 'Unavailable' | string;
  className?: string;
  size?: 'sm' | 'md' | 'lg';
  variant?: string;
}

export const StatusBadge: React.FC<StatusBadgeProps> = ({ status, className = '', size = 'md', variant }) => {
  let colorClasses = 'bg-[var(--color-surface-subtle)] text-[var(--color-text-secondary)] border-[var(--color-border)]';

  switch (status) {
    // Platform Health
    case 'Healthy':
    case 'Available':
    case 'ready':
      colorClasses = 'bg-emerald-500/15 text-emerald-500 border-emerald-500/30';
      break;
    case 'Degraded':
    case 'degraded':
      colorClasses = 'bg-amber-500/15 text-amber-500 border-amber-500/30';
      break;
    case 'Unavailable':
    case 'unavailable':
      colorClasses = 'bg-red-500/15 text-red-500 border-red-500/30';
      break;
    case 'Validating':
      colorClasses = 'bg-blue-500/15 text-blue-500 border-blue-500/30 animate-pulse';
      break;

    // Governance Review Status
    case 'Source Reported':
      colorClasses = 'bg-purple-500/15 text-purple-400 border-purple-500/30';
      break;
    case 'Machine Extracted':
      colorClasses = 'bg-cyan-500/15 text-cyan-400 border-cyan-500/30';
      break;
    case 'Derived Finding':
      colorClasses = 'bg-orange-500/15 text-orange-400 border-orange-500/30';
      break;
    case 'Under Review':
      colorClasses = 'bg-amber-500/15 text-amber-500 border-amber-500/30';
      break;
    case 'Human Reviewed':
    case 'Accepted Representation':
      colorClasses = 'bg-emerald-500/15 text-emerald-500 border-emerald-500/30';
      break;
    case 'Rejected':
      colorClasses = 'bg-red-500/15 text-red-500 border-red-500/30';
      break;
    case 'Unsupported Suggestion':
      colorClasses = 'bg-[var(--color-surface-subtle)] text-[var(--color-text-muted)] border-[var(--color-border)] border-dashed';
      break;

    // Default
    default:
      colorClasses = 'bg-[var(--color-surface-subtle)] text-[var(--color-text-secondary)] border-[var(--color-border)]';
  }

  const sizeClasses =
    size === 'sm'
      ? 'text-xs px-2 py-0.5'
      : size === 'lg'
      ? 'text-sm px-3 py-1 font-semibold'
      : 'text-xs px-2.5 py-1 font-medium';

  return (
    <span
      id={`status-badge-${status.toLowerCase().replace(/\s+/g, '-')}`}
      className={`inline-flex items-center gap-1.5 rounded-full border whitespace-nowrap transition-colors ${sizeClasses} ${colorClasses} ${className}`}
    >
      <span className="w-1.5 h-1.5 rounded-full bg-current opacity-80" />
      {status}
    </span>
  );
};
