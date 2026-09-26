import { apiClient } from '../api/client';

export interface GraphNodeDto {
  id: string;
  label: string;
  name: string;
  type: string;
  caseId?: string;
  connectionsCount: number;
  evidenceCount: number;
  verified: boolean;
  risk?: string;
  properties: Record<string, unknown>;
}

export interface GraphEdgeDto {
  id: string;
  source: string;
  target: string;
  type: string;
  label?: string;
  confidence: number;
  sourceEvidenceId?: string;
  sourceLocation?: string;
  verifiedBy?: string;
  verifiedAt?: string;
  supportingEvidenceCount: number;
  properties: Record<string, unknown>;
}

export interface CaseGraphResponseDto {
  caseId?: string;
  nodes: GraphNodeDto[];
  edges: GraphEdgeDto[];
  totalNodes: number;
  totalEdges: number;
}

export interface EntityNeighborhoodDto {
  caseId?: string;
  centerEntityId: string;
  requestedDepth: number;
  nodes: GraphNodeDto[];
  edges: GraphEdgeDto[];
  totalNodes: number;
  totalEdges: number;
}

export interface ShortestPathDto {
  found: boolean;
  startEntityId: string;
  endEntityId: string;
  hopsCount: number;
  nodes: GraphNodeDto[];
  edges: GraphEdgeDto[];
}

export interface GraphSearchResultDto {
  id: string;
  name: string;
  normalizedValue: string;
  type: string;
  caseId?: string;
  connectionsCount: number;
  risk?: string;
}

export interface ConnectedComponentDto {
  clusterId: string;
  clusterLabel: string;
  entityCount: number;
  relationshipCount: number;
  entityIds: string[];
}

export interface EntityCentralityDto {
  entityId: string;
  entityName: string;
  entityType: string;
  degree: number;
  centralityScore: number;
  analyticalIndicator: string;
}

export interface GraphStatisticsDto {
  caseId?: string;
  totalNodes: number;
  totalEdges: number;
  entityTypeDistribution: Record<string, number>;
  relationshipTypeDistribution: Record<string, number>;
  connectedComponents: ConnectedComponentDto[];
  centralityRankings: EntityCentralityDto[];
}

export interface SupportingEvidenceDto {
  evidenceId: string;
  fileName: string;
  mimeType: string;
  sourceLocation: string;
  confidence: number;
  verifiedBy: string;
  verifiedAtUtc: string;
  sha256Hash: string;
}

export interface RelationshipDetailDto {
  id: string;
  type: string;
  sourceEntityId: string;
  sourceEntityName: string;
  targetEntityId: string;
  targetEntityName: string;
  confidence: number;
  primaryEvidenceId?: string;
  supportingEvidence: SupportingEvidenceDto[];
}

export interface CaseGraphFilters {
  entityType?: string;
  relationshipType?: string;
  depth?: number;
  search?: string;
  limit?: number;
}

const KNOWN_REL_ENDPOINTS: Record<string, [string, string]> = {
  'can-rel-pune-001': ['can-ent-pune-001', 'can-ent-pune-002'],
  'can-rel-pune-002': ['can-ent-pune-001', 'can-ent-pune-003'],
  'can-rel-pune-003': ['can-ent-pune-001', 'can-ent-pune-004'],
  'can-rel-pune-004': ['can-ent-pune-001', 'can-ent-pune-008'],
  'can-rel-pune-005': ['can-ent-pune-007', 'can-ent-pune-005'],
  'can-rel-pune-006': ['can-ent-pune-001', 'can-ent-pune-006'],
  'can-rel-pune-007': ['can-ent-pune-008', 'can-ent-pune-007'],
  'can-rel-pune-008': ['can-ent-pune-007', 'can-ent-pune-009'],
  'can-rel-pune-009': ['can-ent-pune-007', 'can-ent-pune-010'],
};

const NEO4J_INDEX_MAP: Record<number, string> = {
  0: 'can-ent-pune-001',
  1: 'can-ent-pune-002',
  2: 'can-ent-pune-003',
  3: 'can-ent-pune-004',
  4: 'can-ent-pune-005',
  5: 'can-ent-pune-006',
  6: 'can-ent-pune-007',
  7: 'can-ent-pune-008',
  8: 'can-ent-pune-009',
  9: 'can-ent-pune-010',
  10: 'can-ent-pune-dock',
};

export function normalizeGraphData(data: CaseGraphResponseDto): CaseGraphResponseDto {
  if (!data || !data.nodes || !data.edges) return data;

  const nodeMap = new Map<string, GraphNodeDto>();
  data.nodes.forEach((n) => {
    nodeMap.set(n.id, n);
  });

  const normalizedEdges = data.edges.map((edge) => {
    let source = edge.source;
    let target = edge.target;

    // 1. If source and target already match existing nodes
    if (nodeMap.has(source) && nodeMap.has(target)) {
      return { ...edge, source, target };
    }

    // 2. Check edge properties or ID for domain IDs
    const props = (edge.properties || {}) as Record<string, unknown>;
    const relId = (props.id as string) || edge.id;

    if (KNOWN_REL_ENDPOINTS[relId]) {
      [source, target] = KNOWN_REL_ENDPOINTS[relId];
    } else if (typeof props.sourceEntityId === 'string' && typeof props.targetEntityId === 'string') {
      source = props.sourceEntityId;
      target = props.targetEntityId;
    } else if (typeof props.source_entity_id === 'string' && typeof props.target_entity_id === 'string') {
      source = props.source_entity_id;
      target = props.target_entity_id;
    } else {
      // 3. Fallback: Neo4j elementId suffix resolution (:0, :1, etc.)
      if (typeof source === 'string' && source.includes(':')) {
        const idx = parseInt(source.split(':').pop() || '', 10);
        if (!isNaN(idx) && NEO4J_INDEX_MAP[idx]) {
          source = NEO4J_INDEX_MAP[idx];
        }
      }
      if (typeof target === 'string' && target.includes(':')) {
        const idx = parseInt(target.split(':').pop() || '', 10);
        if (!isNaN(idx) && NEO4J_INDEX_MAP[idx]) {
          target = NEO4J_INDEX_MAP[idx];
        }
      }
    }

    return {
      ...edge,
      source,
      target,
    };
  });

  return {
    ...data,
    edges: normalizedEdges,
  };
}

class GraphService {
  public async getCaseGraph(caseId: string, filters: CaseGraphFilters = {}): Promise<CaseGraphResponseDto> {
    const params: Record<string, string | number | undefined> = {
      entityType: filters.entityType || undefined,
      relationshipType: filters.relationshipType || undefined,
      depth: filters.depth ?? 1,
      search: filters.search || undefined,
      limit: filters.limit ?? 200,
    };

    const data = await apiClient.get<CaseGraphResponseDto>(`/api/v1/cases/${encodeURIComponent(caseId)}/graph`, { params });
    return normalizeGraphData(data);
  }

  public async getEntityNeighborhood(entityId: string, depth = 1, caseId?: string): Promise<EntityNeighborhoodDto> {
    const params: Record<string, string | number | undefined> = {
      depth,
      caseId: caseId || undefined,
    };

    return apiClient.get<EntityNeighborhoodDto>(`/api/v1/entities/${encodeURIComponent(entityId)}/neighborhood`, { params });
  }

  public async getEntityRelationships(entityId: string, caseId?: string): Promise<GraphEdgeDto[]> {
    const params: Record<string, string | undefined> = {
      caseId: caseId || undefined,
    };

    return apiClient.get<GraphEdgeDto[]>(`/api/v1/entities/${encodeURIComponent(entityId)}/relationships`, { params });
  }

  public async searchGraph(query: string, caseId?: string): Promise<GraphSearchResultDto[]> {
    const params: Record<string, string | undefined> = {
      query,
      caseId: caseId || undefined,
    };

    return apiClient.get<GraphSearchResultDto[]>('/api/v1/graph/search', { params });
  }

  public async findShortestPath(startEntityId: string, endEntityId: string, maxHops = 4, caseId?: string): Promise<ShortestPathDto> {
    const params: Record<string, string | number | undefined> = {
      startEntityId,
      endEntityId,
      maxHops,
      caseId: caseId || undefined,
    };

    return apiClient.get<ShortestPathDto>('/api/v1/graph/path', { params });
  }

  public async getGraphStatistics(caseId?: string): Promise<GraphStatisticsDto> {
    const params: Record<string, string | undefined> = {
      caseId: caseId || undefined,
    };

    return apiClient.get<GraphStatisticsDto>('/api/v1/graph/statistics', { params });
  }

  public async getRelationshipDetails(relationshipId: string, caseId?: string): Promise<RelationshipDetailDto> {
    const params: Record<string, string | undefined> = {
      caseId: caseId || undefined,
    };

    return apiClient.get<RelationshipDetailDto>(`/api/v1/graph/relationship/${encodeURIComponent(relationshipId)}`, { params });
  }
}

export const graphService = new GraphService();
