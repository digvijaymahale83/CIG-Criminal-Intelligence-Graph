import { apiClient } from '../api/client';

export interface CopilotQueryRequest {
  caseId: string;
  query: string;
  conversationId?: string;
  maxResults?: number;
  includeCrossCase?: boolean;
  includeAlerts?: boolean;
  includeTimeline?: boolean;
  includeLocations?: boolean;
  includeGraph?: boolean;
  includeEvidence?: boolean;
}

export interface EvidenceCitation {
  evidenceId: string;
  evidenceVersion: number;
  fileName: string;
  sourceType: string;
  snippet: string;
  page?: number;
  row?: number;
  recordId?: string;
  sha256Hash: string;
  integrityStatus: string;
}

export interface EntityCitation {
  entityId: string;
  canonicalName: string;
  entityType: string;
  caseId?: string;
  confidence: number;
}

export interface RelationshipCitation {
  relationshipId: string;
  sourceEntityId: string;
  sourceEntityName: string;
  targetEntityId: string;
  targetEntityName: string;
  relationshipType: string;
  confidence: number;
  status: string;
  evidenceIds: string[];
}

export interface TimelineCitation {
  eventId: string;
  eventType: string;
  eventTimestampUtc: string;
  precision: 'DATETIME' | 'DATE_ONLY';
  description: string;
  location?: string;
  evidenceIds: string[];
}

export interface LocationCitation {
  locationId: string;
  locationName: string;
  latitude: number;
  longitude: number;
  description?: string;
}

export interface AlertCitation {
  alertId: string;
  alertType: string;
  severity: 'LOW' | 'MEDIUM' | 'HIGH' | 'CRITICAL';
  score: number;
  threshold: number;
  explanation: string;
  status: string;
}

export interface ModelSignal {
  signalType: string;
  sourceEntityId: string;
  sourceEntityName: string;
  targetEntityId: string;
  targetEntityName: string;
  predictedType: string;
  score: number;
  status: 'PENDING_REVIEW' | 'CONFIRMED' | 'DISMISSED';
  note: string;
}

export interface CrossCaseConnection {
  id: string;
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
}

export interface CopilotClaim {
  text: string;
  claimType: 'FACT' | 'MODEL_PREDICTION' | 'UNSUPPORTED';
  sourceIds: string[];
  isSupported: boolean;
}

export interface CopilotResponse {
  answer: string;
  confidence: 'HIGH' | 'MEDIUM' | 'LOW' | 'MODEL_SIGNAL' | 'UNKNOWN';
  confidenceScore: number;
  intent: string;
  claims: CopilotClaim[];
  evidenceCitations: EvidenceCitation[];
  entityCitations: EntityCitation[];
  relationshipCitations: RelationshipCitation[];
  timelineCitations: TimelineCitation[];
  locationCitations: LocationCitation[];
  alertCitations: AlertCitation[];
  modelSignals: ModelSignal[];
  crossCaseConnections?: CrossCaseConnection[];
  warnings: string[];
  suggestedFollowUps: string[];
  conversationId: string;
  executedAtUtc: string;
}

export interface CopilotConversationMessage {
  role: 'user' | 'assistant';
  content: string;
  timestampUtc: string;
}

export interface CopilotConversation {
  conversationId: string;
  userId: string;
  caseId: string;
  messages: CopilotConversationMessage[];
  createdAtUtc: string;
  updatedAtUtc: string;
}

export class CopilotService {
  async askCopilot(request: CopilotQueryRequest): Promise<CopilotResponse> {
    return apiClient.post<CopilotResponse>('/api/v1/copilot/query', request);
  }

  async getConversation(conversationId: string): Promise<CopilotConversation> {
    return apiClient.get<CopilotConversation>(`/api/v1/copilot/conversations/${conversationId}`);
  }

  async deleteConversation(conversationId: string): Promise<boolean> {
    return apiClient.delete<boolean>(`/api/v1/copilot/conversations/${conversationId}`);
  }

  async getSuggestedQuestions(caseId: string): Promise<string[]> {
    return apiClient.get<string[]>(`/api/v1/copilot/suggested-questions?caseId=${encodeURIComponent(caseId)}`);
  }
}

export const copilotService = new CopilotService();
