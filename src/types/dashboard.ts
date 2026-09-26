import { AlertDto } from '../services/alerts/alert.service';

export interface DashboardCaseDto {
  id: string;
  caseNumber: string;
  title: string;
  description?: string;
  status: string;
  priority: string;
  jurisdiction?: string;
  district?: string;
  policeStation?: string;
  leadOfficerName?: string;
  firNumber?: string;
  createdAtUtc: string;
  updatedAtUtc: string;
}

export interface DashboardSummaryDto {
  entityCount: number;
  relationshipCount: number;
  evidenceCount: number;
  alertCount: number;
  highRiskAlerts: number;
  crossCaseTotalCount: number;
  crossCaseConfirmedCount: number;
  crossCasePotentialCount: number;
  crossCaseModelCount: number;
  modelSignalCount: number;
  integrityVerifiedCount: number;
  integrityModifiedCount: number;
  integrityUnreconciledCount: number;
}

export interface TopConnectedEntityDto {
  entityId: string;
  canonicalName: string;
  entityType: string;
  degree: number;
}

export interface BridgeEntityDto {
  entityId: string;
  canonicalName: string;
  entityType: string;
  betweennessScore: number;
}

export interface NetworkNodePreviewDto {
  id: string;
  label: string;
  type: string;
  status: string;
  degree: number;
}

export interface NetworkEdgePreviewDto {
  id: string;
  sourceId: string;
  targetId: string;
  type: string;
  confidence: number;
  status: string;
}

export interface DashboardNetworkDto {
  nodeCount: number;
  relationshipCount: number;
  componentCount: number;
  topConnectedEntities: TopConnectedEntityDto[];
  bridgeEntities: BridgeEntityDto[];
  nodes: NetworkNodePreviewDto[];
  edges: NetworkEdgePreviewDto[];
}

export interface DashboardSignalDto {
  id: string;
  title: string;
  type: 'ALERT' | 'CROSS_CASE' | 'MODEL_SIGNAL' | 'SPATIAL_ANOMALY' | 'TEMPORAL_ANOMALY' | string;
  priority: 'CRITICAL' | 'HIGH' | 'MEDIUM' | 'LOW' | string;
  whyItMatters: string;
  evidenceCount: number;
  status: string;
  modelScore?: number;
  modelName?: string;
  createdAtUtc: string;
  actionUrl: string;
}

export interface DashboardTimelineEventDto {
  eventId: string;
  eventType: string;
  eventTimestampUtc: string;
  precision: 'DATETIME' | 'DATE_ONLY' | 'TIME_UNAVAILABLE' | string;
  formattedTime: string;
  description: string;
  location?: string;
  evidenceIds: string[];
}

export interface DashboardLocationDto {
  locationId: string;
  name: string;
  latitude?: number;
  longitude?: number;
  hasCoordinates: boolean;
  coordinateDisplay: string;
  activityCount: number;
  lastActivityUtc?: string;
}

export interface DashboardAlertSeverityCountsDto {
  critical: number;
  high: number;
  medium: number;
  low: number;
}

export interface DashboardAlertStatusCountsDto {
  newCount: number;
  underReviewCount: number;
  resolvedCount: number;
}

export interface DashboardAlertsDto {
  bySeverity: DashboardAlertSeverityCountsDto;
  byStatus: DashboardAlertStatusCountsDto;
  highPriorityAlerts: AlertDto[];
}

export interface CrossCaseItemDto {
  connectionId: string;
  sourceCaseId: string;
  targetCaseId: string;
  targetCaseNumber: string;
  targetCaseTitle: string;
  reason: string;
  supportingEvidenceCount: number;
  status: 'CONFIRMED' | 'POTENTIAL' | 'MODEL_PREDICTED' | string;
  confidence: number;
  connectionType: string;
}

export interface DashboardCrossCaseDto {
  confirmedCount: number;
  potentialCount: number;
  modelPredictedCount: number;
  connections: CrossCaseItemDto[];
}

export interface IntegrityWarningItemDto {
  evidenceId: string;
  fileName: string;
  expectedSha256: string;
  currentSha256: string;
  ledgerBlockIndex?: number;
  status: string;
  explanation: string;
}

export interface DashboardIntegrityDto {
  verifiedCount: number;
  modifiedCount: number;
  unreconciledCount: number;
  totalEvidenceCount: number;
  ledgerHealthStatus: 'VALID' | 'WARNING' | 'COMPROMISED' | string;
  warnings: IntegrityWarningItemDto[];
}

export interface DashboardActionItemDto {
  id: string;
  type: string;
  priority: string;
  description: string;
  caseId: string;
  caseNumber: string;
  status: string;
  actionLabel: string;
  actionUrl: string;
  createdAtUtc: string;
}

export interface DashboardActivityDto {
  id: string;
  action: string;
  details: string;
  actorName: string;
  resourceType: string;
  resourceId?: string;
  timestampUtc: string;
}

export interface DashboardDataQualityWarningDto {
  type: string;
  message: string;
  affectedEntityId?: string;
  affectedEvidenceId?: string;
  actionUrl: string;
}

export interface DashboardDto {
  case: DashboardCaseDto;
  summary: DashboardSummaryDto;
  network: DashboardNetworkDto;
  signals: DashboardSignalDto[];
  timeline: DashboardTimelineEventDto[];
  locations: DashboardLocationDto[];
  alerts: DashboardAlertsDto;
  crossCase: DashboardCrossCaseDto;
  integrity: DashboardIntegrityDto;
  actions: DashboardActionItemDto[];
  recentActivity: DashboardActivityDto[];
  dataQualityWarnings: DashboardDataQualityWarningDto[];
  responsibleAiNotice: string;
  generatedAtUtc: string;
}
