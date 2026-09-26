import { useQuery, useQueryClient } from '@tanstack/react-query';
import { useState } from 'react';
import { SystemService } from '../services/system/system.service';
import { PlatformHealthLevel, SystemHealthState, SystemStatusResponse } from '../types/system';
import { ApiError } from '../services/api/errors';

export const SYSTEM_STATUS_QUERY_KEY = ['systemStatus'];

export function useSystemStatus() {
  const queryClient = useQueryClient();
  const [simulateMode, setSimulateMode] = useState<'ready' | 'degraded' | 'malformed' | 'error' | undefined>(
    undefined
  );

  const query = useQuery({
    queryKey: [...SYSTEM_STATUS_QUERY_KEY, simulateMode],
    queryFn: async ({ signal }) => {
      return SystemService.getSystemStatus({ signal, simulateMode });
    },
    refetchInterval: 30000, // 30s auto-refresh
    staleTime: 5000,
    retry: false, // Critical: do not retry 503 or fail silently with old data
  });

  let level: PlatformHealthLevel = 'Validating';
  let status: SystemStatusResponse | null = null;
  let isDegraded = false;
  let isInvalidPayload = false;
  let errorMessage: string | null = null;

  if (query.isLoading) {
    level = 'Validating';
  } else if (query.isError) {
    const error = query.error;
    if (error instanceof ApiError && error.statusCode === 422) {
      isInvalidPayload = true;
      level = 'Degraded';
      errorMessage = 'Service status response was invalid.';
    } else if (error instanceof ApiError && error.statusCode === 503) {
      isDegraded = true;
      level = 'Degraded';
      errorMessage = 'System is operating in a degraded state.';
      // Check if degraded payload was preserved
      const payload = (error as unknown as { responseData?: SystemStatusResponse }).responseData;
      if (payload && payload.services) {
        status = payload;
      }
    } else {
      level = 'Unavailable';
      errorMessage = error instanceof Error ? error.message : 'System health verification failed.';
    }
  } else if (query.data) {
    status = query.data.data;
    isDegraded = query.data.isDegraded;
    isInvalidPayload = query.data.isInvalid;

    if (isDegraded) {
      level = 'Degraded';
      errorMessage = 'System is operating in a degraded state.';
    } else {
      level = 'Healthy';
    }
  }

  const healthState: SystemHealthState = {
    level,
    status,
    isLoading: query.isLoading,
    isError: query.isError,
    isDegraded,
    errorMessage,
    isInvalidPayload,
    lastChecked: query.dataUpdatedAt ? new Date(query.dataUpdatedAt) : null,
  };

  const refresh = async () => {
    await queryClient.invalidateQueries({ queryKey: SYSTEM_STATUS_QUERY_KEY });
  };

  const changeSimulation = async (mode: 'ready' | 'degraded' | 'malformed' | 'error') => {
    setSimulateMode(mode);
    try {
      await SystemService.setSimulationMode(mode);
    } catch {
      // Ignored if direct endpoint simulation param is used
    }
    await queryClient.invalidateQueries({ queryKey: SYSTEM_STATUS_QUERY_KEY });
  };

  return {
    ...healthState,
    isFetching: query.isFetching,
    refresh,
    simulateMode,
    changeSimulation,
  };
}
