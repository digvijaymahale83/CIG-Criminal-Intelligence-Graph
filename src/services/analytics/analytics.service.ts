import { apiClient } from '../api/client';

export interface GraphAnalysisRunDto {
  id: string;
  caseId: string;
  status: string;
  startedAtUtc: string;
  completedAtUtc?: string;
  nodeCount: number;
  edgeCount: number;
  metricsGenerated: number;
  modelVersion: string;
  networkDensity: number;
  averageDegree: number;
  averagePathLength: number;
  connectedComponentsCount: number;
  communitiesCount: number;
  executedBy: string;
}

export interface EntityCentralityMetricDto {
  entityId: string;
  entityName: string;
  entityType: string;
  degree: number;
  inDegree: number;
  outDegree: number;
  normalizedDegree: number;
  betweennessCentrality: number;
  closenessCentrality: number;
  pageRank: number;
  analyticalIndicator: string;
  communityId: string;
  componentId: string;
}

export interface CentralityResultsDto {
  caseId: string;
  totalNodes: number;
  sortedBy: string;
  metrics: EntityCentralityMetricDto[];
}

export interface CommunityClusterDto {
  communityId: string;
  clusterLabel: string;
  entityCount: number;
  relationshipCount: number;
  internalDensity: number;
  entityTypeDistribution: Record<string, number>;
  entityIds: string[];
  sampleEntities: string[];
}

export interface ConnectedComponentDetailDto {
  componentId: string;
  componentLabel: string;
  nodeCount: number;
  edgeCount: number;
  entityTypeDistribution: Record<string, number>;
  topNexusEntities: string[];
  entityIds: string[];
}

export interface NetworkStatisticsDto {
  caseId: string;
  totalEntities: number;
  totalRelationships: number;
  connectedComponents: number;
  communitiesCount: number;
  averageDegree: number;
  networkDensity: number;
  averagePathLength: number;
  entityTypeDistribution: Record<string, number>;
  relationshipTypeDistribution: Record<string, number>;
}

export interface ContributingSignalDto {
  signalName: string;
  weight: number;
  contribution: number;
  description: string;
}

export interface LeadSignalBreakdownDto {
  cosineSimilarity: number;
  sharedNeighborsCount: number;
  sharedNeighborNames: string[];
  attentionWeight: number;
  contributingSignals: ContributingSignalDto[];
}

export interface GraphAnalyticalLeadDto {
  id: string;
  caseId: string;
  analysisRunId: string;
  sourceEntityId: string;
  sourceEntityName: string;
  sourceEntityType: string;
  targetEntityId: string;
  targetEntityName: string;
  targetEntityType: string;
  leadType: string;
  suggestedRelationshipType: string;
  score: number;
  status: 'PENDING' | 'CONFIRMED' | 'DISMISSED';
  modelVersion: string;
  signals: LeadSignalBreakdownDto;
  createdAtUtc: string;
  reviewedAtUtc?: string;
  reviewedBy?: string;
  reviewNotes?: string;
  resultingRelationshipId?: string;
}

export interface RunAnalyticsRequestDto {
  includeCrossCase?: boolean;
  authorizedCaseIds?: string[];
  gatEmbeddingDim?: number;
  gatHeads?: number;
  candidateThreshold?: number;
  maxCandidates?: number;
}

export interface LeadReviewRequestDto {
  status: 'CONFIRMED' | 'DISMISSED';
  reviewNotes?: string;
  suggestedRelationshipType?: string;
}

export interface EntityAnalyticsProfileDto {
  entityId: string;
  entityName: string;
  entityType: string;
  metrics?: EntityCentralityMetricDto;
  communityCluster?: CommunityClusterDto;
  connectedComponent?: ConnectedComponentDetailDto;
  adjacentLeads: GraphAnalyticalLeadDto[];
}

export const analyticsService = {
  async runAnalytics(caseId: string, params: RunAnalyticsRequestDto = {}): Promise<GraphAnalysisRunDto> {
    return await apiClient.post<GraphAnalysisRunDto>(`/cases/${caseId}/analytics/run`, params);
  },

  async getLatestRun(caseId: string): Promise<GraphAnalysisRunDto | null> {
    try {
      return await apiClient.get<GraphAnalysisRunDto>(`/cases/${caseId}/analytics`);
    } catch {
      return null;
    }
  },

  async getCentrality(caseId: string, sortBy: string = 'Betweenness', limit: number = 50): Promise<CentralityResultsDto> {
    return await apiClient.get<CentralityResultsDto>(`/cases/${caseId}/analytics/centrality`, {
      params: { sortBy, limit }
    });
  },

  async getCommunities(caseId: string): Promise<CommunityClusterDto[]> {
    return await apiClient.get<CommunityClusterDto[]>(`/cases/${caseId}/analytics/communities`);
  },

  async getComponents(caseId: string): Promise<ConnectedComponentDetailDto[]> {
    return await apiClient.get<ConnectedComponentDetailDto[]>(`/cases/${caseId}/analytics/components`);
  },

  async getStatistics(caseId: string): Promise<NetworkStatisticsDto> {
    return await apiClient.get<NetworkStatisticsDto>(`/cases/${caseId}/analytics/statistics`);
  },

  async getLeads(caseId: string, status?: string, minScore?: number, limit: number = 50): Promise<GraphAnalyticalLeadDto[]> {
    return await apiClient.get<GraphAnalyticalLeadDto[]>(`/cases/${caseId}/analytics/leads`, {
      params: { status, minScore, limit }
    });
  },

  async getEntityProfile(entityId: string, caseId?: string): Promise<EntityAnalyticsProfileDto> {
    return await apiClient.get<EntityAnalyticsProfileDto>(`/entities/${entityId}/analytics`, {
      params: { caseId }
    });
  },

  async reviewLead(leadId: string, review: LeadReviewRequestDto): Promise<GraphAnalyticalLeadDto> {
    return await apiClient.post<GraphAnalyticalLeadDto>(`/analytics/leads/${leadId}/review`, review);
  }
};
