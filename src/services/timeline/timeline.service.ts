import { apiClient } from '../api/client';

export interface TimelineEventDto {
  id: string;
  caseId: string;
  eventType: string;
  description: string;
  startTimeUtc?: string;
  endTimeUtc?: string;
  timePrecision: 'EXACT' | 'MINUTE' | 'HOUR' | 'DAY' | 'DATE_ONLY' | 'MONTH' | 'YEAR' | 'UNKNOWN';
  location?: string;
  locationEntityId?: string;
  relatedEntityNames: string[];
  relatedEntityIds: string[];
  confidence: number;
  verificationStatus: 'PENDING' | 'APPROVED' | 'REJECTED';
  sourceEvidenceId?: string;
  sourceEvidenceFileName?: string;
  sourceEvidenceSha256?: string;
  evidenceIntegrityVerified: boolean;
  sourceLocation?: string;
  sourcePage?: number;
  createdAtUtc: string;
}

export interface TimelineQueryDto {
  startDate?: string;
  endDate?: string;
  eventType?: string;
  entityId?: string;
  location?: string;
  evidenceId?: string;
  minConfidence?: number;
  verificationStatus?: string;
  page?: number;
  pageSize?: number;
}

export interface TimelineDateRangeDto {
  caseId: string;
  earliestEventUtc?: string;
  latestEventUtc?: string;
  totalEvents: number;
}

export interface TemporalOverlapDto {
  id: string;
  caseId: string;
  sourceEntityId?: string;
  sourceEntityName?: string;
  sourceEntityType?: string;
  targetEntityId?: string;
  targetEntityName?: string;
  targetEntityType?: string;
  locationEntityId?: string;
  locationName?: string;
  overlapStartUtc: string;
  overlapEndUtc: string;
  durationMinutes: number;
  score: number;
  explanation: string;
  signalType: 'TEMPORAL_OVERLAP' | 'CROSS_CASE_TEMPORAL_OVERLAP';
  status: 'PENDING' | 'CONFIRMED' | 'DISMISSED';
  supportingEvidenceIds: string[];
  supportingEvidenceFileNames: string[];
  reviewedBy?: string;
  reviewedAtUtc?: string;
  reviewNotes?: string;
}

export interface TemporalClusterDto {
  clusterId: string;
  clusterLabel: string;
  startTimeUtc: string;
  endTimeUtc: string;
  eventCount: number;
  distinctEntitiesCount: number;
  distinctLocationsCount: number;
  intensity: 'LOW' | 'MEDIUM' | 'HIGH';
  keyEntities: string[];
  keyLocations: string[];
  events: TimelineEventDto[];
}

export interface TemporalSequenceItemDto {
  stepIndex: number;
  eventId: string;
  eventType: string;
  description: string;
  timestampUtc: string;
  location?: string;
  involvedEntities: string[];
  elapsedFromPrevious: string;
  elapsedMinutesFromPrevious: number;
}

export interface TemporalSequenceDto {
  entityId: string;
  entityName: string;
  totalSteps: number;
  sequenceStartUtc?: string;
  sequenceEndUtc?: string;
  steps: TemporalSequenceItemDto[];
}

export interface TimelineResponseDto {
  caseId: string;
  totalEvents: number;
  page: number;
  pageSize: number;
  activePeriodStartUtc?: string;
  activePeriodEndUtc?: string;
  events: TimelineEventDto[];
  clusters: TemporalClusterDto[];
  sequenceHighlights: TemporalSequenceItemDto[];
}

export interface TemporalAnalysisResultDto {
  runId: string;
  caseId: string;
  status: string;
  startedAtUtc: string;
  completedAtUtc?: string;
  totalEventsAnalyzed: number;
  signalsGenerated: number;
  overlapsFound: number;
  clustersFound: number;
  overlaps: TemporalOverlapDto[];
  clusters: TemporalClusterDto[];
  executedBy: string;
}

export interface ReviewTemporalSignalRequestDto {
  status: 'CONFIRMED' | 'DISMISSED';
  reviewNotes?: string;
}

export interface TemporalSummaryDto {
  caseId: string;
  totalEvents: number;
  activePeriodStartUtc?: string;
  activePeriodEndUtc?: string;
  activityPeaks: number;
  temporalSignals: number;
  pendingSignals: number;
  confirmedSignals: number;
}

export interface RunTemporalAnalysisRequestDto {
  includeCrossCase?: boolean;
  authorizedCaseIds?: string[];
  minOverlapDurationMinutes?: number;
}

export const timelineService = {
  async getCaseTimeline(caseId: string, query: TimelineQueryDto = {}): Promise<TimelineResponseDto> {
    return await apiClient.get<TimelineResponseDto>(`/cases/${caseId}/timeline`, {
      params: query as Record<string, string | number | boolean | undefined>
    });
  },

  async getCaseTimelineEvents(caseId: string, query: TimelineQueryDto = {}): Promise<TimelineEventDto[]> {
    return await apiClient.get<TimelineEventDto[]>(`/cases/${caseId}/timeline/events`, {
      params: query as Record<string, string | number | boolean | undefined>
    });
  },

  async getEntityTimeline(entityId: string, caseId?: string): Promise<TimelineEventDto[]> {
    return await apiClient.get<TimelineEventDto[]>(`/entities/${entityId}/timeline`, {
      params: { caseId }
    });
  },

  async getRelationshipTimeline(relationshipId: string, caseId?: string): Promise<TimelineEventDto[]> {
    return await apiClient.get<TimelineEventDto[]>(`/relationships/${relationshipId}/timeline`, {
      params: { caseId }
    });
  },

  async getTimelineRange(caseId: string): Promise<TimelineDateRangeDto> {
    return await apiClient.get<TimelineDateRangeDto>(`/cases/${caseId}/timeline/range`);
  },

  async getLatestTemporalAnalysis(caseId: string): Promise<TemporalAnalysisResultDto | null> {
    try {
      return await apiClient.get<TemporalAnalysisResultDto>(`/cases/${caseId}/temporal-analysis`);
    } catch {
      return null;
    }
  },

  async runTemporalAnalysis(caseId: string, request: RunTemporalAnalysisRequestDto = {}): Promise<TemporalAnalysisResultDto> {
    return await apiClient.post<TemporalAnalysisResultDto>(`/cases/${caseId}/temporal-analysis/run`, request);
  },

  async getTemporalOverlaps(caseId: string, params: { entityId?: string; location?: string; minDurationMinutes?: number } = {}): Promise<TemporalOverlapDto[]> {
    return await apiClient.get<TemporalOverlapDto[]>(`/cases/${caseId}/temporal-overlaps`, {
      params
    });
  },

  async getEntitySequence(entityId: string, caseId?: string): Promise<TemporalSequenceDto> {
    return await apiClient.get<TemporalSequenceDto>(`/entities/${entityId}/sequence`, {
      params: { caseId }
    });
  },

  async getTemporalSummary(caseId: string): Promise<TemporalSummaryDto> {
    return await apiClient.get<TemporalSummaryDto>(`/cases/${caseId}/temporal-summary`);
  },

  async reviewTemporalSignal(caseId: string, signalId: string, review: ReviewTemporalSignalRequestDto): Promise<TemporalOverlapDto> {
    return await apiClient.post<TemporalOverlapDto>(`/cases/${caseId}/temporal-signals/${signalId}/review`, review);
  }
};
