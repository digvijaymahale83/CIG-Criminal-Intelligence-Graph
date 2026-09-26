import { GovernanceReviewStatus } from './governance';

export type EvidenceSourceType = 'PDF' | 'CSV' | 'Excel' | 'JSON' | 'Text' | 'Images' | 'Other';

export type EvidenceProcessingStatus =
  | 'Uploaded'
  | 'Quarantined'
  | 'Processing'
  | 'Processed'
  | 'Failed'
  | 'Reviewed';

export interface EvidenceItem {
  id: string;
  investigationId: string;
  filename: string;
  sourceType: EvidenceSourceType;
  sourceOrigin: string;
  uploadedBy: string;
  uploadedAtUtc: string;
  fileSizeBytes: number;
  sha256Hash: string;
  processingStatus: EvidenceProcessingStatus;
  reviewStatus: GovernanceReviewStatus;
  extractedEntitiesCount: number;
  extractedAssertionsCount: number;
  quarantineReason?: string;
  errorMessage?: string;
}

export interface EvidenceDetail extends EvidenceItem {
  mimeType: string;
  metadata: Record<string, string | number | boolean>;
  extractedSummary?: string;
  extractedInformation: Array<{
    id: string;
    type: string;
    value: string;
    confidence: number;
    locationInfo: string;
  }>;
}
