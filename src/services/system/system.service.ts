import { systemStatusResponseSchema } from '../../schemas/system.schema';
import { SystemStatusResponse } from '../../types/system';
import { apiClient } from '../api/client';
import { ApiError } from '../api/errors';

export interface GetSystemStatusOptions {
  signal?: AbortSignal;
  simulateMode?: 'ready' | 'degraded' | 'malformed' | 'error';
}

export class SystemService {
  /**
   * Fetches live system status from Phase 1 backend:
   * GET /api/v1/system/status
   *
   * Handles:
   * - HTTP 200 (ready)
   * - HTTP 503 (degraded) - extracts degraded payload if returned
   * - Zod runtime validation
   * - AbortSignal cancellation
   */
  public static async getSystemStatus(
    options: GetSystemStatusOptions = {}
  ): Promise<{ data: SystemStatusResponse; isDegraded: boolean; isInvalid: boolean }> {
    const params: Record<string, string> = {};
    if (options.simulateMode) {
      params.simulate = options.simulateMode;
    }

    try {
      const rawData = await apiClient.get<unknown>('/api/v1/system/status', {
        signal: options.signal,
        params,
        timeoutMs: 10000,
      });

      // Zod runtime validation
      const parseResult = systemStatusResponseSchema.safeParse(rawData);
      if (!parseResult.success) {
        throw new ApiError(422, 'Service status response was invalid.', {
          correlationId: 'SCHEMA_VALIDATION_FAILED',
        });
      }

      const isDegraded = parseResult.data.status !== 'ready' || !parseResult.data.isReady;
      return {
        data: parseResult.data,
        isDegraded,
        isInvalid: false,
      };
    } catch (err) {
      // If 503 was returned, inspect if responseData contains valid system status response
      if (err instanceof ApiError && err.statusCode === 503) {
        const responseData = (err as unknown as { responseData?: unknown }).responseData;
        if (responseData) {
          const parseResult = systemStatusResponseSchema.safeParse(responseData);
          if (parseResult.success) {
            return {
              data: parseResult.data,
              isDegraded: true,
              isInvalid: false,
            };
          }
        }
      }

      // If schema validation failed specifically
      if (err instanceof ApiError && err.statusCode === 422) {
        throw err;
      }

      throw err;
    }
  }

  /**
   * Health endpoints for readiness / liveness
   */
  public static async getLiveness(): Promise<{ status: string }> {
    return apiClient.get<{ status: string }>('/health/live');
  }

  public static async getReadiness(): Promise<{ status: string }> {
    return apiClient.get<{ status: string }>('/health/ready');
  }

  /**
   * Diagnostic simulation toggle (used for live verification of 503 and malformed states)
   */
  public static async setSimulationMode(mode: 'ready' | 'degraded' | 'malformed' | 'error'): Promise<void> {
    await apiClient.post('/api/v1/system/status/simulate', { mode });
  }
}
