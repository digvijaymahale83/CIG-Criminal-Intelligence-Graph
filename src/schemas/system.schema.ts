import { z } from 'zod';

export const serviceHealthSchema = z.object({
  name: z.string(),
  healthy: z.boolean(),
  message: z.string().optional(),
  latencyMs: z.number().optional(),
  lastCheckedUtc: z.string().optional(),
});

export const systemStatusResponseSchema = z.object({
  status: z.string(),
  checkedAtUtc: z.string(),
  services: z.array(serviceHealthSchema),
  isReady: z.boolean(),
});

export type ValidatedSystemStatusResponse = z.infer<typeof systemStatusResponseSchema>;
export type ValidatedServiceHealth = z.infer<typeof serviceHealthSchema>;
