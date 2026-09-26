import { EntityType } from './entities';
import { GovernanceReviewStatus, SupportingEvidenceReference } from './governance';

export type RelationshipType =
  | 'CALLED'
  | 'MESSAGED'
  | 'TRANSFERRED_MONEY'
  | 'MET'
  | 'TRAVELLED_TO'
  | 'VISITED'
  | 'OWNS'
  | 'REGISTERED_TO'
  | 'WORKS_FOR'
  | 'ASSOCIATED_WITH'
  | 'LINKED_TO_CASE'
  | 'OBSERVED_WITH';

export interface GraphNode {
  id: string;
  label: string;
  type: EntityType;
  investigationId: string;
  reviewStatus: GovernanceReviewStatus;
  degreeCentrality?: number;
  betweennessCentrality?: number;
  pageRank?: number;
  communityId?: string;
  isUnsupportedSuggestion?: boolean;
}

export interface GraphEdge {
  id: string;
  sourceId: string;
  targetId: string;
  type: RelationshipType;
  investigationId: string;
  isInferred: boolean;
  isUnsupportedSuggestion?: boolean;
  confidence: number;
  extractionMethod: 'Source-Document Extraction' | 'Graph Analytics Inference' | 'Investigator Asserted';
  algorithmOrRun?: string;
  timestamp?: string;
  reviewStatus: GovernanceReviewStatus;
  supportingEvidence: SupportingEvidenceReference[];
}

export interface CaseNetworkGraphData {
  investigationId: string;
  projectionStatus: 'Up to Date' | 'Updating' | 'Behind' | 'Unavailable';
  projectionVersion: string;
  nodes: GraphNode[];
  edges: GraphEdge[];
  totalNodes: number;
  totalEdges: number;
  truncated: boolean;
  maxVisualizationLimit: number;
}
