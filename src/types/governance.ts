/**
 * Data Governance & Responsible AI Types
 * Enforces strict distinction between:
 * 1. Source-reported observation
 * 2. Machine-extracted assertion
 * 3. Derived analytical finding
 * 4. Human-reviewed interpretation
 * 5. Human investigator conclusion
 *
 * PROHIBITED: "Guilty Probability", "Criminal Probability", "Criminal Score", "Arrest Recommendation", etc.
 */

export type AnalyticalProvenanceCategory =
  | 'source_reported'
  | 'machine_extracted'
  | 'derived_finding'
  | 'human_reviewed'
  | 'investigator_conclusion';

export type GovernanceReviewStatus =
  | 'Source Reported'
  | 'Machine Extracted'
  | 'Derived Finding'
  | 'Under Review'
  | 'Human Reviewed'
  | 'Rejected'
  | 'Accepted Representation'
  | 'Unsupported Suggestion';

export type InvestigationPriorityLevel = 'Low' | 'Medium' | 'High' | 'Critical';

export type EvidenceLocatorType =
  | 'pdf_text_span'
  | 'csv_row'
  | 'excel_cell_range'
  | 'json_pointer'
  | 'text_span'
  | 'image_region';

export interface EvidenceLocator {
  type: EvidenceLocatorType;
  page?: number;
  row?: number;
  cellRange?: string;
  charOffsetStart?: number;
  charOffsetEnd?: number;
  boundingBox?: {
    x: number;
    y: number;
    width: number;
    height: number;
  };
}

export interface SupportingEvidenceReference {
  evidenceId: string;
  sourceDocumentName: string;
  sourceType: string;
  locator: EvidenceLocator;
  summaryQuote?: string;
  verifiedAtUtc?: string;
}

export interface AnalyticalFinding {
  id: string;
  investigationId: string;
  title: string;
  category:
    | 'Potential Relationship'
    | 'Potential Anomaly'
    | 'Network Pattern'
    | 'Temporal Pattern'
    | 'Financial Pattern'
    | 'Location Pattern';
  status: GovernanceReviewStatus;
  confidenceScore: number; // 0.0 - 1.0 calibrated confidence, NEVER "probability of guilt"
  confidenceExplanation: string;
  algorithm: string;
  analysisRunId: string;
  createdUtc: string;
  affectedEntities: Array<{
    entityId: string;
    displayName: string;
    entityType: string;
  }>;
  reason: string;
  limitations: string;
  supportingEvidence: SupportingEvidenceReference[];
  reviewedBy?: string;
  reviewedAtUtc?: string;
  reviewNotes?: string;
}

export interface GraphFreshnessInfo {
  status: 'Up to Date' | 'Updating' | 'Behind' | 'Unavailable';
  lastSynchronizedUtc: string;
  projectionVersion: string;
  pendingTransactionsCount: number;
  message?: string;
}
