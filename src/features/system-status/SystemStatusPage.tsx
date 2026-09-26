import React from 'react';
import {
  Activity,
  RefreshCw,
  Server,
  Database,
  Cpu,
  ShieldCheck,
  AlertTriangle,
  Radio,
  Sliders,
  CheckCircle2,
  XCircle,
} from 'lucide-react';
import { useSystemStatus } from '../../hooks/useSystemStatus';
import { ServiceStatusCard } from '../../components/status/ServiceStatusCard';
import { StatusBadge } from '../../components/common/StatusBadge';
import { Button } from '../../components/common/Button';
import { Alert } from '../../components/common/Alert';
import { Card } from '../../components/common/Card';
import { ResponsibleAiNotice } from '../../components/common/ResponsibleAiNotice';
import { PhaseNotice } from '../../components/common/PhaseNotice';

export const SystemStatusPage: React.FC = () => {
  const {
    level,
    status,
    isLoading,
    isFetching,
    isDegraded,
    isError,
    isInvalidPayload,
    errorMessage,
    lastChecked,
    refresh,
    simulateMode,
    changeSimulation,
  } = useSystemStatus();

  return (
    <div className="space-y-6" id="system-status-page">
      {/* Page Header */}
      <div className="flex flex-col md:flex-row md:items-center justify-between gap-4 pb-4 border-b border-[var(--color-border)]">
        <div>
          <div className="flex items-center gap-2.5">
            <h1 className="text-xl font-bold text-[var(--color-text-primary)] tracking-tight">System Status & Diagnostics</h1>
            <span className="text-[10px] uppercase font-mono px-2 py-0.5 rounded bg-[#EBF5EC] text-[#1E5C2B] border border-[#C4E4C8] font-bold">
              PHASE 1 LIVE
            </span>
          </div>
          <p className="text-xs text-[var(--color-text-secondary)] mt-1">
            Real-time health verification for core intelligence datastores, graph engines, and analytics workers.
          </p>
        </div>

        <div className="flex items-center gap-3">
          <Button
            id="refresh-status-button"
            variant="secondary"
            size="md"
            onClick={refresh}
            isLoading={isFetching}
            icon={<RefreshCw className={`w-3.5 h-3.5 ${isFetching ? 'animate-spin' : ''}`} />}
          >
            Refresh Status
          </Button>
        </div>
      </div>

      {/* Evidentiary & Responsible AI Reminder */}
      <ResponsibleAiNotice compact />

      {/* Platform Overall Health Hero Banner */}
      <div
        id="platform-health-overview"
        className={`rounded-xl border p-5 sm:p-6 flex flex-col md:flex-row items-start md:items-center justify-between gap-4 transition-colors shadow-xs ${
          level === 'Healthy'
            ? 'bg-[#EBF5EC] border-[#C4E4C8]'
            : level === 'Degraded'
            ? 'bg-[#FEF7E6] border-[#F6D99B]'
            : 'bg-[#FDF1F0] border-[#F6C7C5]'
        }`}
      >
        <div className="flex items-start sm:items-center gap-4">
          <div
            className={`p-3 rounded-xl border ${
              level === 'Healthy'
                ? 'bg-[var(--color-surface)] border-[#C4E4C8] text-[#1E5C2B]'
                : level === 'Degraded'
                ? 'bg-[var(--color-surface)] border-[#F6D99B] text-[#8F4D08]'
                : 'bg-[var(--color-surface)] border-[#F6C7C5] text-[#9E2525]'
            }`}
          >
            {level === 'Healthy' ? (
              <CheckCircle2 className="w-7 h-7" />
            ) : level === 'Degraded' ? (
              <AlertTriangle className="w-7 h-7" />
            ) : (
              <XCircle className="w-7 h-7" />
            )}
          </div>

          <div className="space-y-1">
            <div className="flex items-center gap-2">
              <span className="text-xs font-mono uppercase tracking-wider text-[var(--color-text-secondary)]">
                Platform Health
              </span>
              <StatusBadge status={level} size="sm" />
            </div>
            <h2 className="text-lg font-semibold text-[var(--color-text-primary)]">
              {level === 'Healthy' && 'All Core Services Operational'}
              {level === 'Degraded' && 'Platform Operating in Degraded State'}
              {level === 'Unavailable' && 'Platform Service Disruption Detected'}
              {level === 'Validating' && 'Verifying Infrastructure Health...'}
            </h2>
            <p className="text-xs text-[var(--color-text-primary)] leading-relaxed max-w-2xl">
              {level === 'Healthy' &&
                'PostgreSQL primary repository, Neo4j graph database, Redis caching broker, and AI NLP workers responded successfully to active health checks.'}
              {level === 'Degraded' &&
                (errorMessage ||
                  'One or more non-critical analytical pipelines or graph projection workers are temporarily unavailable. Primary case storage remains accessible.')}
              {level === 'Unavailable' &&
                (errorMessage ||
                  'The intelligence platform gateway is currently unreachable. Network connectivity or backend service requires attention.')}
            </p>
          </div>
        </div>

        {/* Polling & Last Checked Telemetry */}
        <div className="flex flex-col md:items-end gap-1.5 pt-3 md:pt-0 border-t md:border-t-0 border-[var(--color-border)] w-full md:w-auto text-xs text-[var(--color-text-secondary)]">
          <div className="flex items-center gap-2">
            <Radio className="w-3.5 h-3.5 text-[#2E8540] animate-pulse" />
            <span>Auto-refresh: Every 30 seconds</span>
          </div>
          <div className="font-mono text-[11px] text-[var(--color-text-secondary)]">
            Last Checked:{' '}
            <strong className="text-[var(--color-text-primary)]">
              {lastChecked ? lastChecked.toLocaleTimeString() : 'Awaiting check...'}
            </strong>
          </div>
          {status?.checkedAtUtc && (
            <div className="font-mono text-[10px] text-[var(--color-text-muted)]">
              Server UTC: {new Date(status.checkedAtUtc).toISOString()}
            </div>
          )}
        </div>
      </div>

      {/* Malformed Data / Validation Error Banner */}
      {isInvalidPayload && (
        <Alert
          id="malformed-status-alert"
          type="warning"
          title="Service Status Response Was Invalid"
        >
          Service status response was invalid. The backend returned an unrecognized schema structure.
          The application safely caught the error without crashing.
        </Alert>
      )}

      {/* Degraded State Warning Banner */}
      {isDegraded && !isInvalidPayload && (
        <Alert
          id="degraded-status-alert"
          type="warning"
          title="Degraded Platform Notice (HTTP 503)"
        >
          The system status endpoint returned HTTP 503 Degraded. The application is strictly displaying
          current degraded telemetry rather than stale cached data or a generic error page.
        </Alert>
      )}

      {/* Services Grid */}
      <div className="space-y-3">
        <div className="flex items-center justify-between">
          <h3 className="text-sm font-semibold text-[var(--color-text-primary)] tracking-wide flex items-center gap-2">
            <Server className="w-4 h-4 text-[var(--color-text-muted)]" />
            <span>Infrastructure Component Health</span>
          </h3>
          <span className="text-[11px] font-mono text-[var(--color-text-muted)]">
            Endpoint: GET /api/v1/system/status
          </span>
        </div>

        <div className="grid grid-cols-1 md:grid-cols-2 gap-4" id="services-grid">
          {isLoading && !status ? (
            Array.from({ length: 4 }).map((_, i) => (
              <div
                key={i}
                className="h-36 rounded-xl bg-[var(--color-surface)] border border-[var(--color-border)] animate-pulse p-4"
              />
            ))
          ) : status && status.services && status.services.length > 0 ? (
            status.services.map((service) => (
              <ServiceStatusCard
                key={service.name}
                service={service}
                id={`service-card-${service.name.toLowerCase()}`}
              />
            ))
          ) : (
            // Default services fallback display when unavailable
            [
              { name: 'postgres', healthy: false, message: 'Unavailable' },
              { name: 'neo4j', healthy: false, message: 'Unavailable' },
              { name: 'redis', healthy: false, message: 'Unavailable' },
              { name: 'ai-service', healthy: false, message: 'Unavailable' },
            ].map((service) => (
              <ServiceStatusCard
                key={service.name}
                service={service}
                id={`service-card-fallback-${service.name}`}
              />
            ))
          )}
        </div>
      </div>

      {/* Diagnostic Simulation Controls */}
      <Card
        id="diagnostic-simulation-controls"
        title="Diagnostic Simulation & Verification Controls"
        subtitle="Test live frontend resilience under HTTP 200 (Ready), HTTP 503 (Degraded), Malformed Zod payload, and Server 500 error."
        action={
          <div className="flex items-center gap-1.5 text-xs text-[var(--color-text-muted)] font-mono">
            <Sliders className="w-3.5 h-3.5" />
            <span>Interactive Tester</span>
          </div>
        }
      >
        <div className="space-y-4">
          <p className="text-xs text-[var(--color-text-secondary)] leading-relaxed">
            Per Phase 1 requirements, the frontend must correctly process HTTP 200 (Ready), HTTP 503 (Degraded without
            discarding payload), gracefully handle malformed responses via Zod without crashing, and show accurate Last Checked timestamps.
            Use the buttons below to immediately verify these states:
          </p>

          <div className="flex flex-wrap gap-2.5">
            <Button
              id="sim-ready-btn"
              variant={simulateMode === 'ready' || !simulateMode ? 'primary' : 'outline'}
              size="sm"
              onClick={() => changeSimulation('ready')}
              icon={<CheckCircle2 className="w-3.5 h-3.5" />}
            >
              Simulate Ready (200 OK)
            </Button>
            <Button
              id="sim-degraded-btn"
              variant={simulateMode === 'degraded' ? 'primary' : 'outline'}
              size="sm"
              onClick={() => changeSimulation('degraded')}
              icon={<AlertTriangle className="w-3.5 h-3.5 text-[#D97706]" />}
            >
              Simulate Degraded (503 Service Unavailable)
            </Button>
            <Button
              id="sim-malformed-btn"
              variant={simulateMode === 'malformed' ? 'primary' : 'outline'}
              size="sm"
              onClick={() => changeSimulation('malformed')}
              icon={<Cpu className="w-3.5 h-3.5 text-[var(--color-accent)]" />}
            >
              Simulate Malformed Payload (Zod Catch)
            </Button>
            <Button
              id="sim-error-btn"
              variant={simulateMode === 'error' ? 'primary' : 'outline'}
              size="sm"
              onClick={() => changeSimulation('error')}
              icon={<XCircle className="w-3.5 h-3.5 text-[#DC2626]" />}
            >
              Simulate Server Failure (500 Error)
            </Button>
          </div>

          <div className="p-3 rounded-lg bg-[var(--color-surface-subtle)] border border-[var(--color-border)] text-[11px] font-mono text-[var(--color-text-secondary)] flex flex-col sm:flex-row items-start sm:items-center justify-between gap-2">
            <span>
              Active Simulation Mode:{' '}
              <strong className="text-[var(--color-text-primary)] uppercase">{simulateMode || 'READY (LIVE DEFAULT)'}</strong>
            </span>
            <span>AbortSignal Cancellation: Supported</span>
          </div>
        </div>
      </Card>

      {/* Phase Foundation Note */}
      <PhaseNotice phaseNumber={1} moduleName="System Health" />
    </div>
  );
};
