import { apiClient } from '../api/client';

export interface MatchFactor {
  type: string;
  weight: number;
  score: number;
  description: string;
  evidenceCitations?: {
    evidenceId: string;
    caseId: string;
    fileName: string;
    evidenceType: string;
    sourceLocation: string;
    extractedQuote: string;
  }[];
}

export interface EntitySummary {
  id: string;
  caseId: string;
  caseNumber: string;
  canonicalName: string;
  normalizedValue: string;
  entityType: string;
  aliases: string[];
  phoneNumber?: string;
  vehicleNumber?: string;
  accountNumber?: string;
  location: string;
}

export interface EntityMatchCandidate {
  id: string;
  sourceEntityId: string;
  targetEntityId: string;
  sourceCaseId: string;
  targetCaseId: string;
  sourceCaseNumber: string;
  targetCaseNumber: string;
  entityType: string;
  matchStatus: 'PENDING' | 'APPROVED' | 'REJECTED' | 'SUPERSEDED';
  matchScore: number;
  matchMethod: string;
  factors: MatchFactor[];
  createdAtUtc: string;
  reviewedAtUtc?: string;
  reviewedBy?: string;
  reviewNotes?: string;
  sourceEntity: EntitySummary;
  targetEntity: EntitySummary;
}

export interface SupportingEvidenceCitation {
  evidenceId: string;
  caseId: string;
  fileName: string;
  evidenceType: string;
  sourceLocation: string;
  extractedQuote: string;
}

export interface EntityComparisonSide {
  entityId: string;
  caseId: string;
  caseNumber: string;
  caseTitle: string;
  canonicalName: string;
  normalizedValue: string;
  entityType: string;
  aliases: string[];
  phoneNumber?: string;
  vehicleNumber?: string;
  accountNumber?: string;
  bankName?: string;
  location: string;
  district: string;
  supportingEvidence: SupportingEvidenceCitation[];
  connectedRelationships: string[];
}

export interface CandidateComparison {
  candidateId: string;
  matchScore: number;
  matchStatus: string;
  entityType: string;
  factors: MatchFactor[];
  sideA: EntityComparisonSide;
  sideB: EntityComparisonSide;
  createdAtUtc: string;
  reviewedBy?: string;
  reviewedAtUtc?: string;
  reviewNotes?: string;
}

export interface CrossCaseConnection {
  id: string;
  candidateId?: string;
  sourceCaseId: string;
  targetCaseId: string;
  sourceCaseNumber: string;
  targetCaseNumber: string;
  sourceEntityId: string;
  targetEntityId: string;
  sourceEntityName: string;
  targetEntityName: string;
  entityType: string;
  connectionType: string;
  confidence: number;
  status: string;
  explanation: string;
  createdAtUtc: string;
  reviewedAtUtc?: string;
  reviewedBy?: string;
}

export interface BatchResolutionResult {
  entitiesCompared: number;
  potentialMatchesFound: number;
  highConfidenceCandidates: number;
  candidatesCreated: number;
  duration: string;
}

export const entityResolutionService = {
  async getCandidates(params?: {
    caseId?: string;
    status?: string;
    entityType?: string;
    minimumScore?: number;
  }): Promise<EntityMatchCandidate[]> {
    return apiClient.get<EntityMatchCandidate[]>('/entity-resolution/candidates', {
      params: params as Record<string, string | number | boolean | undefined>,
    });
  },

  async getCandidateById(candidateId: string): Promise<EntityMatchCandidate> {
    return apiClient.get<EntityMatchCandidate>(`/entity-resolution/candidates/${candidateId}`);
  },

  async getCandidateComparison(candidateId: string): Promise<CandidateComparison> {
    return apiClient.get<CandidateComparison>(`/entity-resolution/candidates/${candidateId}/compare`);
  },

  async approveCandidate(candidateId: string, reviewNotes?: string): Promise<void> {
    await apiClient.post<void>(`/entity-resolution/candidates/${candidateId}/approve`, {
      status: 'APPROVED',
      reviewNotes,
    });
  },

  async rejectCandidate(candidateId: string, reviewNotes?: string): Promise<void> {
    await apiClient.post<void>(`/entity-resolution/candidates/${candidateId}/reject`, {
      status: 'REJECTED',
      reviewNotes,
    });
  },

  async runBatchResolution(params?: {
    caseId?: string;
    entityTypes?: string[];
    minimumScore?: number;
  }): Promise<BatchResolutionResult> {
    return apiClient.post<BatchResolutionResult>('/entity-resolution/run', {
      caseId: params?.caseId,
      entityTypes: params?.entityTypes,
      minimumScore: params?.minimumScore ?? 0.6,
    });
  },

  async getEntityCrossCaseMatches(entityId: string): Promise<EntityMatchCandidate[]> {
    return apiClient.get<EntityMatchCandidate[]>(`/entities/${entityId}/cross-case-matches`);
  },

  async getCaseCrossCaseConnections(caseId: string, minConfidence?: number): Promise<CrossCaseConnection[]> {
    return apiClient.get<CrossCaseConnection[]>(`/cases/${caseId}/cross-case-connections`, {
      params: minConfidence !== undefined ? { minConfidence } : undefined,
    });
  },
};
