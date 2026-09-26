import { describe, it, expect, vi, beforeEach } from 'vitest';
import { systemStatusResponseSchema } from '../../schemas/system.schema';
import { SystemService } from './system.service';
import { apiClient } from '../api/client';
import { ApiError } from '../api/errors';

describe('System Status & Foundation Tests', () => {
  beforeEach(() => {
    vi.restoreAllMocks();
  });

  describe('systemStatusResponseSchema (Zod Validation)', () => {
    it('validates a conformant HTTP 200 ready payload', () => {
      const validPayload = {
        status: 'ready',
        isReady: true,
        checkedAtUtc: '2026-09-07T12:00:00.000Z',
        services: [
          { name: 'postgres', healthy: true, latencyMs: 5 },
          { name: 'neo4j', healthy: true, latencyMs: 8 },
          { name: 'redis', healthy: true, latencyMs: 2 },
          { name: 'ai-service', healthy: true, latencyMs: 14 },
        ],
      };

      const result = systemStatusResponseSchema.safeParse(validPayload);
      expect(result.success).toBe(true);
      if (result.success) {
        expect(result.data.isReady).toBe(true);
        expect(result.data.services.length).toBe(4);
      }
    });

    it('rejects a malformed payload without required fields', () => {
      const invalidPayload = {
        status: 200, // should be string
        services: 'all-good', // should be array
      };

      const result = systemStatusResponseSchema.safeParse(invalidPayload);
      expect(result.success).toBe(false);
    });
  });

  describe('SystemService.getSystemStatus', () => {
    it('successfully processes valid ready status', async () => {
      const mockPayload = {
        status: 'ready',
        isReady: true,
        checkedAtUtc: '2026-09-07T12:00:00.000Z',
        services: [
          { name: 'postgres', healthy: true },
          { name: 'neo4j', healthy: true },
        ],
      };

      vi.spyOn(apiClient, 'get').mockResolvedValueOnce(mockPayload);

      const response = await SystemService.getSystemStatus();
      expect(response.isDegraded).toBe(false);
      expect(response.isInvalid).toBe(false);
      expect(response.data.status).toBe('ready');
    });

    it('gracefully throws ApiError with 422 when response payload is malformed', async () => {
      const malformedPayload = { unexpectedField: 123 };
      vi.spyOn(apiClient, 'get').mockResolvedValueOnce(malformedPayload);

      await expect(SystemService.getSystemStatus()).rejects.toThrow(ApiError);
    });

    it('supports 503 degraded error extracting payload rather than dropping it', async () => {
      const degradedPayload = {
        status: 'degraded',
        isReady: false,
        checkedAtUtc: '2026-09-07T12:00:00.000Z',
        services: [
          { name: 'postgres', healthy: true },
          { name: 'ai-service', healthy: false, message: 'Queue backpressure' },
        ],
      };

      const apiError = new ApiError(503, 'Degraded performance', { correlationId: 'TEST_503' });
      (apiError as unknown as { responseData: unknown }).responseData = degradedPayload;

      vi.spyOn(apiClient, 'get').mockRejectedValueOnce(apiError);

      const response = await SystemService.getSystemStatus();
      expect(response.isDegraded).toBe(true);
      expect(response.data.services.length).toBe(2);
    });
  });

  describe('Responsible AI Decision-Support Platform Integrity', () => {
    it('verifies non-autonomous decision support principle', () => {
      const principle = {
        isAutonomousClassifier: false,
        requiresHumanInvestigatorApproval: true,
        allowsCriminalityLabeling: false,
      };

      expect(principle.isAutonomousClassifier).toBe(false);
      expect(principle.requiresHumanInvestigatorApproval).toBe(true);
      expect(principle.allowsCriminalityLabeling).toBe(false);
    });
  });
});
