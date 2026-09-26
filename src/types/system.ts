/**
 * System Health & Infrastructure Types
 * Strictly aligns with Phase 1 backend endpoint contract:
 * GET /api/v1/system/status
 */

export type ServiceName = 'postgres' | 'neo4j' | 'redis' | 'ai-service' | string;

export type SystemStatusCode = 'ready' | 'degraded' | 'unavailable' | 'unknown';

export interface ServiceHealth {
  name: ServiceName;
  healthy: boolean;
  message?: string;
  latencyMs?: number;
  lastCheckedUtc?: string;
}

export interface SystemStatusResponse {
  status: 'ready' | 'degraded' | string;
  checkedAtUtc: string;
  services: ServiceHealth[];
  isReady: boolean;
}

export type PlatformHealthLevel = 'Healthy' | 'Degraded' | 'Unavailable' | 'Validating';

export interface SystemHealthState {
  level: PlatformHealthLevel;
  status: SystemStatusResponse | null;
  isLoading: boolean;
  isError: boolean;
  isDegraded: boolean;
  errorMessage: string | null;
  isInvalidPayload: boolean;
  lastChecked: Date | null;
}
