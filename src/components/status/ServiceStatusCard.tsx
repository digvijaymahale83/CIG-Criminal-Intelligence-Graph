import React from 'react';
import { Database, Network, Cpu, HardDrive, CheckCircle2, XCircle } from 'lucide-react';
import { ServiceHealth } from '../../types/system';
import { StatusBadge } from '../common/StatusBadge';

interface ServiceStatusCardProps {
  service: ServiceHealth;
  id?: string;
}

export const ServiceStatusCard: React.FC<ServiceStatusCardProps> = ({ service, id }) => {
  const getServiceIcon = (name: string) => {
    switch (name.toLowerCase()) {
      case 'postgres':
        return <Database className="w-5 h-5 text-indigo-400" />;
      case 'neo4j':
        return <Network className="w-5 h-5 text-emerald-400" />;
      case 'redis':
        return <HardDrive className="w-5 h-5 text-rose-400" />;
      case 'ai-service':
        return <Cpu className="w-5 h-5 text-sky-400" />;
      default:
        return <Database className="w-5 h-5 text-[var(--color-text-muted)]" />;
    }
  };

  const getServiceDisplayName = (name: string) => {
    switch (name.toLowerCase()) {
      case 'postgres':
        return 'PostgreSQL Primary Database';
      case 'neo4j':
        return 'Neo4j Graph Analytics Engine';
      case 'redis':
        return 'Redis Cache & Event Bus';
      case 'ai-service':
        return 'AI Service / NLP Extraction Worker';
      default:
        return name.toUpperCase();
    }
  };

  const getServiceRole = (name: string) => {
    switch (name.toLowerCase()) {
      case 'postgres':
        return 'Relational storage, evidence metadata, provenance records, and immutable audit logging.';
      case 'neo4j':
        return 'Graph projections, entity relationships, centrality calculation, and pathfinding queries.';
      case 'redis':
        return 'Distributed session storage, background job queues, and analytical result caching.';
      case 'ai-service':
        return 'Evidence document parsing, entity recognition, relationship extraction, and decision support.';
      default:
        return 'Platform auxiliary infrastructure component.';
    }
  };

  return (
    <div
      id={id || `service-card-${service.name.toLowerCase()}`}
      className={`rounded-xl border p-4.5 flex flex-col justify-between gap-3 transition-colors shadow-xs ${
        service.healthy
          ? 'bg-[var(--color-surface)] border-[var(--color-border)] hover:border-[var(--color-accent)]/40'
          : 'bg-red-500/10 border-red-500/30'
      }`}
    >
      <div className="flex items-start justify-between gap-3">
        <div className="flex items-center gap-3">
          <div className="p-2 rounded-lg bg-[var(--color-surface-subtle)] border border-[var(--color-border-subtle)]">
            {getServiceIcon(service.name)}
          </div>
          <div>
            <h4 className="text-xs font-semibold text-[var(--color-text-primary)] tracking-wide">
              {getServiceDisplayName(service.name)}
            </h4>
            <span className="text-[10px] font-mono text-[var(--color-text-muted)] uppercase">{service.name}</span>
          </div>
        </div>
        <StatusBadge status={service.healthy ? 'Available' : 'Unavailable'} size="sm" />
      </div>

      <p className="text-xs text-[var(--color-text-secondary)] leading-relaxed">{getServiceRole(service.name)}</p>

      <div className="pt-3 border-t border-[var(--color-border-subtle)] flex items-center justify-between text-[11px] text-[var(--color-text-muted)]">
        <div className="flex items-center gap-1.5">
          {service.healthy ? (
            <>
              <CheckCircle2 className="w-3.5 h-3.5 text-emerald-500" />
              <span className="text-emerald-500 font-medium">Operational</span>
            </>
          ) : (
            <>
              <XCircle className="w-3.5 h-3.5 text-red-500" />
              <span className="text-red-500 font-medium">Service Disrupted</span>
            </>
          )}
        </div>
        {service.latencyMs !== undefined && (
          <span className="font-mono text-[var(--color-text-secondary)]">{service.latencyMs}ms latency</span>
        )}
      </div>
    </div>
  );
};
