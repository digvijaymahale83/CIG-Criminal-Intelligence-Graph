import { apiClient } from '../api/client';

export type AlertSeverity = 'LOW' | 'MEDIUM' | 'HIGH' | 'CRITICAL';
export type AlertStatus = 'NEW' | 'ACKNOWLEDGED' | 'UNDER_REVIEW' | 'RESOLVED' | 'DISMISSED';

export type AlertType =
  | 'NETWORK_ANOMALY'
  | 'TEMPORAL_ANOMALY'
  | 'GEOGRAPHIC_ANOMALY'
  | 'RELATIONSHIP_SURGE'
  | 'ACTIVITY_SPIKE'
  | 'UNUSUAL_TRAVEL'
  | 'DATA_CONSISTENCY'
  | 'CROSS_CASE_PATTERN'
  | 'MODEL_SIGNAL';

export interface AlertDto {
  id: string;
  caseId: string;
  alertRunId?: string;
  alertType: AlertType;
  severity: AlertSeverity;
  status: AlertStatus;
  title: string;
  description: string;
  sourceEntityId?: string;
  sourceEntityName?: string;
  targetEntityId?: string;
  targetEntityName?: string;
  locationId?: string;
  locationName?: string;
  relatedEventId?: string;
  relatedEvidenceId?: string;
  relatedEvidenceFileName?: string;
  relatedEvidenceSha256?: string;
  score: number;
  priorityScore: number;
  detectionMethod: string;
  detectionVersion: string;
  explanation: string;
  deduplicationFingerprint: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  reviewedAtUtc?: string;
  reviewedBy?: string;
  reviewNotes?: string;
}

export interface AlertQueryDto {
  status?: string;
  severity?: string;
  alertType?: string;
  entityId?: string;
  search?: string;
  limit?: number;
  offset?: number;
  [key: string]: string | number | boolean | undefined;
}

export interface AlertSummaryDto {
  caseId: string;
  totalAlerts: number;
  newAlerts: number;
  acknowledgedAlerts: number;
  underReviewAlerts: number;
  resolvedAlerts: number;
  dismissedAlerts: number;
  criticalSeverity: number;
  highSeverity: number;
  mediumSeverity: number;
  lowSeverity: number;
  networkAnomalies: number;
  temporalAnomalies: number;
  geographicAnomalies: number;
  relationshipSurges: number;
  activitySpikes: number;
  unusualTravel: number;
  dataConsistency: number;
  crossCasePatterns: number;
  modelSignals: number;
}

export interface AlertRunResultDto {
  runId: string;
  caseId: string;
  status: string;
  startedAtUtc: string;
  completedAtUtc?: string;
  detectorsExecuted: number;
  signalsGenerated: number;
  alertsCreated: number;
  alertsDeduplicated: number;
  executionDurationMs: number;
  createdAlerts: AlertDto[];
}

export interface RunAlertDetectionRequestDto {
  enabledDetectors?: string[];
  includeCrossCase?: boolean;
  authorizedCaseIds?: string[];
  temporalSpikeThreshold?: number;
  geographicDistanceThresholdKm?: number;
  relationshipSurgeThreshold?: number;
}

export interface ReviewAlertRequestDto {
  notes?: string;
}

export const alertService = {
  async getCaseAlerts(caseId: string, query: AlertQueryDto = {}): Promise<AlertDto[]> {
    return await apiClient.get<AlertDto[]>(`/cases/${caseId}/alerts`, {
      params: query
    });
  },

  async getAlertSummary(caseId: string): Promise<AlertSummaryDto> {
    return await apiClient.get<AlertSummaryDto>(`/cases/${caseId}/alerts/summary`);
  },

  async getAlertDetails(caseId: string, alertId: string): Promise<AlertDto> {
    return await apiClient.get<AlertDto>(`/cases/${caseId}/alerts/${alertId}`);
  },

  async runDetection(caseId: string, request: RunAlertDetectionRequestDto = {}): Promise<AlertRunResultDto> {
    return await apiClient.post<AlertRunResultDto>(`/cases/${caseId}/alerts/run`, request);
  },

  async acknowledgeAlert(alertId: string): Promise<AlertDto> {
    return await apiClient.post<AlertDto>(`/alerts/${alertId}/acknowledge`, {});
  },

  async startReview(alertId: string): Promise<AlertDto> {
    return await apiClient.post<AlertDto>(`/alerts/${alertId}/start-review`, {});
  },

  async resolveAlert(alertId: string, notes: string): Promise<AlertDto> {
    return await apiClient.post<AlertDto>(`/alerts/${alertId}/resolve`, { notes });
  },

  async dismissAlert(alertId: string, notes: string): Promise<AlertDto> {
    return await apiClient.post<AlertDto>(`/alerts/${alertId}/dismiss`, { notes });
  }
};
