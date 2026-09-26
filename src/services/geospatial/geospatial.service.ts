import { apiClient } from '../api/client';
import { TimelineEventDto } from '../timeline/timeline.service';

export interface LocationDto {
  id: string;
  caseId?: string;
  entityId?: string;
  name: string;
  normalizedName: string;
  address?: string;
  city?: string;
  district?: string;
  state?: string;
  country: string;
  latitude: number;
  longitude: number;
  geocodePrecision: string;
  source: string;
  eventCount: number;
  entityCount: number;
  evidenceCount: number;
  createdAtUtc: string;
}

export interface SpatialClusterDto {
  clusterId: string;
  clusterLabel: string;
  centroidLatitude: number;
  centroidLongitude: number;
  locationCount: number;
  eventCount: number;
  entityCount: number;
  locations: LocationDto[];
  keyEntities: string[];
}

export interface SpatialSignalDto {
  id: string;
  caseId: string;
  analysisRunId?: string;
  signalType: string;
  sourceEntityId?: string;
  sourceEntityName?: string;
  targetEntityId?: string;
  targetEntityName?: string;
  locationId?: string;
  locationName?: string;
  startTimeUtc?: string;
  endTimeUtc?: string;
  distanceKm: number;
  score: number;
  explanation: string;
  supportingEvidenceIds: string[];
  supportingEvidenceFileNames: string[];
  status: 'PENDING' | 'CONFIRMED' | 'DISMISSED';
  createdAtUtc: string;
  reviewedAtUtc?: string;
  reviewedBy?: string;
  reviewNotes?: string;
}

export interface CaseMapDto {
  caseId: string;
  totalLocations: number;
  totalEvents: number;
  totalSignals: number;
  locations: LocationDto[];
  clusters: SpatialClusterDto[];
  activeSignals: SpatialSignalDto[];
}

export interface LocationActivityEntitySummaryDto {
  entityId: string;
  entityName: string;
  entityType: string;
  recordedVisits: number;
  firstObservedUtc?: string;
  lastObservedUtc?: string;
}

export interface LocationEvidenceCitationDto {
  evidenceId: string;
  fileName: string;
  sha256Hash: string;
  sourceLocation?: string;
  page?: number;
  evidenceIntegrityVerified: boolean;
}

export interface LocationActivityDto {
  location: LocationDto;
  events: TimelineEventDto[];
  entities: LocationActivityEntitySummaryDto[];
  evidenceRecords: LocationEvidenceCitationDto[];
  activePeriodStartUtc?: string;
  activePeriodEndUtc?: string;
}

export interface SpatialProximityResultDto {
  locationId: string;
  name: string;
  city?: string;
  latitude: number;
  longitude: number;
  distanceKm: number;
  geocodePrecision: string;
  eventCount: number;
  entityCount: number;
}

export interface TravelSequenceStepDto {
  stepIndex: number;
  eventId: string;
  eventType: string;
  description: string;
  timestampUtc: string;
  locationId: string;
  locationName: string;
  latitude: number;
  longitude: number;
  distanceKmFromPrevious: number;
  elapsedHoursFromPrevious: number;
  elapsedFormatted: string;
  impliedSpeedKmh: number;
  isImplausibleSpeed: boolean;
}

export interface TravelSequenceDto {
  entityId: string;
  entityName: string;
  totalSteps: number;
  totalDistanceKm: number;
  sequenceStartUtc?: string;
  sequenceEndUtc?: string;
  steps: TravelSequenceStepDto[];
}

export interface SpatialAnalysisResultDto {
  runId: string;
  caseId: string;
  status: string;
  startedAtUtc: string;
  completedAtUtc?: string;
  totalLocationsAnalyzed: number;
  totalEventsAnalyzed: number;
  signalsGenerated: number;
  overlapsFound: number;
  clustersFound: number;
  velocityWarningsFound: number;
  signals: SpatialSignalDto[];
  clusters: SpatialClusterDto[];
  executedBy: string;
}

export interface RunSpatialAnalysisRequestDto {
  includeCrossCase?: boolean;
  authorizedCaseIds?: string[];
  clusterRadiusKm?: number;
  velocityWarningThresholdKmh?: number;
}

export interface ReviewSpatialSignalRequestDto {
  status: 'CONFIRMED' | 'DISMISSED';
  reviewNotes?: string;
}

export const geospatialService = {
  async getCaseMapData(
    caseId: string,
    filters?: {
      eventType?: string;
      entityId?: string;
      startDate?: string;
      endDate?: string;
      verifiedOnly?: boolean;
    }
  ): Promise<CaseMapDto> {
    return await apiClient.get<CaseMapDto>(`/cases/${caseId}/map`, {
      params: filters as Record<string, string | number | boolean | undefined>
    });
  },

  async getCaseLocations(caseId: string, search?: string): Promise<LocationDto[]> {
    return await apiClient.get<LocationDto[]>(`/cases/${caseId}/map/locations`, {
      params: { search }
    });
  },

  async getLocationDetails(locationId: string): Promise<LocationDto> {
    return await apiClient.get<LocationDto>(`/locations/${locationId}`);
  },

  async getLocationActivity(locationId: string, caseId?: string): Promise<LocationActivityDto> {
    return await apiClient.get<LocationActivityDto>(`/locations/${locationId}/activity`, {
      params: { caseId }
    });
  },

  async getEntityLocations(entityId: string, caseId?: string): Promise<LocationDto[]> {
    return await apiClient.get<LocationDto[]>(`/entities/${entityId}/locations`, {
      params: { caseId }
    });
  },

  async getEntityTravelSequence(entityId: string, caseId?: string): Promise<TravelSequenceDto> {
    return await apiClient.get<TravelSequenceDto>(`/entities/${entityId}/travel-sequence`, {
      params: { caseId }
    });
  },

  async searchProximity(
    caseId: string,
    latitude: number,
    longitude: number,
    radiusKm: number
  ): Promise<SpatialProximityResultDto[]> {
    return await apiClient.get<SpatialProximityResultDto[]>(`/cases/${caseId}/map/proximity`, {
      params: { latitude, longitude, radiusKm }
    });
  },

  async getLatestAnalysis(caseId: string): Promise<SpatialAnalysisResultDto | null> {
    return await apiClient.get<SpatialAnalysisResultDto | null>(`/cases/${caseId}/map/analysis/latest`);
  },

  async runSpatialAnalysis(
    caseId: string,
    request: RunSpatialAnalysisRequestDto = {}
  ): Promise<SpatialAnalysisResultDto> {
    return await apiClient.post<SpatialAnalysisResultDto>(`/cases/${caseId}/map/analysis`, request);
  },

  async getSpatialSignals(
    caseId: string,
    status?: string,
    signalType?: string
  ): Promise<SpatialSignalDto[]> {
    return await apiClient.get<SpatialSignalDto[]>(`/cases/${caseId}/map/signals`, {
      params: { status, signalType }
    });
  },

  async reviewSpatialSignal(
    caseId: string,
    signalId: string,
    request: ReviewSpatialSignalRequestDto
  ): Promise<SpatialSignalDto> {
    return await apiClient.post<SpatialSignalDto>(`/cases/${caseId}/map/signals/${signalId}/review`, request);
  }
};
