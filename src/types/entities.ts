import { GovernanceReviewStatus, SupportingEvidenceReference } from './governance';

export type EntityType =
  | 'Person'
  | 'Organization'
  | 'Phone'
  | 'Vehicle'
  | 'Bank Account'
  | 'Location'
  | 'Event'
  | 'Case'
  | 'Crime' // Source-referenced incident category/classification, NEVER proof of guilt
  | 'Document'
  | 'Evidence';

export interface Entity {
  id: string;
  investigationId: string;
  type: EntityType;
  displayName: string;
  primaryIdentifier?: string;
  reviewStatus: GovernanceReviewStatus;
  confidence: number;
  sourceMentionsCount: number;
  relationshipsCount: number;
  firstObservedUtc?: string;
  lastObservedUtc?: string;
  attributes: Record<string, string | number | boolean>;
  supportingEvidence: SupportingEvidenceReference[];
}

export interface EntityResolutionCandidate {
  id: string;
  investigationId: string;
  status: 'Under Review' | 'Accepted Representation' | 'Rejected' | 'Deferred';
  mentionA: {
    entityId: string;
    displayName: string;
    type: EntityType;
    sourceDocument: string;
    attributes: Record<string, string>;
  };
  mentionB: {
    entityId: string;
    displayName: string;
    type: EntityType;
    sourceDocument: string;
    attributes: Record<string, string>;
  };
  similaritySignals: Array<{
    signalName: string;
    score: number;
    explanation: string;
  }>;
  conflicts: string[];
  reviewedBy?: string;
  reviewedAtUtc?: string;
  resolutionNotes?: string;
}
