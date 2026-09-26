import { apiClient } from '../api/client';

export interface ExtractedEntity {
  id: string;
  extractionJobId: string;
  evidenceId: string;
  caseId?: string;
  entityType: string;
  rawValue: string;
  normalizedValue: string;
  confidence: number;
  sourceLocation: string;
  reviewStatus: 'PENDING' | 'APPROVED' | 'REJECTED';
  promotedEntityId?: string;
  reviewedBy?: string;
  reviewedAtUtc?: string;
  createdAtUtc: string;
}

export interface ExtractedRelationship {
  id: string;
  extractionJobId: string;
  evidenceId: string;
  caseId?: string;
  sourceExtractedEntityId?: string;
  targetExtractedEntityId?: string;
  sourceNormalizedValue: string;
  targetNormalizedValue: string;
  relationshipType: string;
  confidence: number;
  sourceLocation: string;
  reviewStatus: 'PENDING' | 'APPROVED' | 'REJECTED';
  promotedRelationshipId?: string;
  reviewedBy?: string;
  reviewedAtUtc?: string;
  createdAtUtc: string;
}

export interface ExtractedEvent {
  id: string;
  eventType: string;
  eventTimestampUtc?: string;
  location?: string;
  relatedEntitiesJson?: string;
  confidence: number;
  sourceLocation: string;
  reviewStatus: string;
}

export interface ExtractionResult {
  evidenceId: string;
  caseId?: string;
  status: string;
  errorMessage?: string;
  rawTextSnippet?: string;
  entities: ExtractedEntity[];
  relationships: ExtractedRelationship[];
  events: ExtractedEvent[];
  metadata: Record<string, any>;
}

export interface IntegrityCheckResult {
  evidenceId: string;
  fileName: string;
  status: 'VALID' | 'TAMPERED' | 'FILE_MISSING';
  storedHash: string;
  computedHash: string;
  hashesMatch: boolean;
  fileSizeBytes: number;
  checkedAtUtc: string;
  message: string;
}

export interface EvidenceDetail {
  id: string;
  caseId?: string;
  fileName: string;
  mimeType: string;
  fileSize: number;
  storagePath: string;
  sha256Hash: string;
  uploadedById?: string;
  uploadedByName: string;
  uploadedAtUtc: string;
  processingStatus: string;
  clearance: string;
  description: string;
  version: number;
  parentEvidenceId?: string;
  extractionJobId?: string;
}

export class EvidenceService {
  public static async getEvidenceList(caseId?: string): Promise<EvidenceDetail[]> {
    return await apiClient.get<EvidenceDetail[]>(`/api/v1/evidence?caseId=${caseId || ''}`);
  }

  public static async getEvidenceDetail(evidenceId: string): Promise<EvidenceDetail> {
    return await apiClient.get<EvidenceDetail>(`/api/v1/evidence/${evidenceId}`);
  }

  public static async uploadEvidence(formData: FormData): Promise<EvidenceDetail> {
    return await apiClient.post<EvidenceDetail>('/api/v1/evidence/upload', formData);
  }

  public static async getExtraction(evidenceId: string): Promise<ExtractionResult> {
    return await apiClient.get<ExtractionResult>(`/api/v1/evidence/${evidenceId}/extraction`);
  }

  public static async approveEntity(
    evidenceId: string,
    entityId: string,
    correctedRawValue?: string,
    correctedNormalizedValue?: string
  ): Promise<{ message: string; entityId: string }> {
    return await apiClient.post<{ message: string; entityId: string }>(
      `/api/v1/evidence/${evidenceId}/extraction/approve`,
      { entityId, correctedRawValue, correctedNormalizedValue }
    );
  }

  public static async rejectEntity(
    evidenceId: string,
    entityId: string,
    reason?: string
  ): Promise<{ message: string; entityId: string }> {
    return await apiClient.post<{ message: string; entityId: string }>(
      `/api/v1/evidence/${evidenceId}/extraction/reject`,
      { entityId, reason }
    );
  }

  public static async editEntity(
    evidenceId: string,
    entityId: string,
    rawValue: string,
    normalizedValue?: string
  ): Promise<{ message: string; entityId: string }> {
    return await apiClient.post<{ message: string; entityId: string }>(
      `/api/v1/evidence/${evidenceId}/extraction/entities/${entityId}/edit`,
      { rawValue, normalizedValue }
    );
  }

  public static async approveRelationship(
    evidenceId: string,
    relationshipId: string
  ): Promise<{ message: string; relationshipId: string }> {
    return await apiClient.post<{ message: string; relationshipId: string }>(
      `/api/v1/evidence/${evidenceId}/extraction/relationships/approve`,
      { relationshipId }
    );
  }

  public static async rejectRelationship(
    evidenceId: string,
    relationshipId: string,
    reason?: string
  ): Promise<{ message: string; relationshipId: string }> {
    return await apiClient.post<{ message: string; relationshipId: string }>(
      `/api/v1/evidence/${evidenceId}/extraction/relationships/reject`,
      { relationshipId, reason }
    );
  }

  public static async verifyIntegrity(evidenceId: string): Promise<IntegrityCheckResult> {
    return await apiClient.post<IntegrityCheckResult>(`/api/v1/evidence/${evidenceId}/verify-integrity`, {});
  }

  public static async retryProcessing(evidenceId: string): Promise<{ message: string; evidenceId: string }> {
    return await apiClient.post<{ message: string; evidenceId: string }>(`/api/v1/evidence/${evidenceId}/process`, {});
  }
}
