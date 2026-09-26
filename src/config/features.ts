/**
 * Feature Capability Configuration
 * In Phase 1, only system health is connected to real backend endpoints.
 * All subsequent modules are architected and integration-ready, but clearly
 * display their phase status without fabricating fake responses.
 */

export interface FeatureCapability {
  id: string;
  name: string;
  phase: number;
  enabled: boolean;
  connectedToBackend: boolean;
  statusMessage: string;
  endpointPrefix?: string;
}

export const FEATURE_CAPABILITIES: Record<string, FeatureCapability> = {
  phase1Health: {
    id: 'phase1Health',
    name: 'System Health & Diagnostics',
    phase: 1,
    enabled: true,
    connectedToBackend: true,
    statusMessage: 'Connected to live ASP.NET Core system status endpoints.',
    endpointPrefix: '/api/v1/system/status',
  },
  authentication: {
    id: 'authentication',
    name: 'Authentication & Session Service',
    phase: 2,
    enabled: true,
    connectedToBackend: false,
    statusMessage: 'Authentication API integration scheduled for Phase 2.',
    endpointPrefix: '/api/v1/auth',
  },
  investigations: {
    id: 'investigations',
    name: 'Case Isolation & Workspace',
    phase: 3,
    enabled: true,
    connectedToBackend: false,
    statusMessage: 'Case persistence and PostgreSQL repository coming in Phase 3.',
    endpointPrefix: '/api/v1/investigations',
  },
  evidence: {
    id: 'evidence',
    name: 'Evidence & Provenance Management',
    phase: 4,
    enabled: true,
    connectedToBackend: false,
    statusMessage: 'Object storage & evidence quarantine pipeline scheduled for Phase 4.',
    endpointPrefix: '/api/v1/evidence',
  },
  nlp: {
    id: 'nlp',
    name: 'NLP Entity & Assertion Extraction',
    phase: 6,
    enabled: true,
    connectedToBackend: false,
    statusMessage: 'FastAPI Python NLP extraction pipeline scheduled for Phase 6.',
    endpointPrefix: '/api/v1/nlp',
  },
  entityResolution: {
    id: 'entityResolution',
    name: 'Entity Resolution & Review',
    phase: 7,
    enabled: true,
    connectedToBackend: false,
    statusMessage: 'Entity matching & disambiguation engine scheduled for Phase 7.',
    endpointPrefix: '/api/v1/entity-resolution',
  },
  graph: {
    id: 'graph',
    name: 'Neo4j Graph Explorer & Freshness',
    phase: 8,
    enabled: true,
    connectedToBackend: false,
    statusMessage: 'Neo4j case-scoped graph projection scheduled for Phase 8.',
    endpointPrefix: '/api/v1/graph',
  },
  analytics: {
    id: 'analytics',
    name: 'Centrality & Community Analytics',
    phase: 9,
    enabled: true,
    connectedToBackend: false,
    statusMessage: 'Graph centrality & PageRank calculation worker scheduled for Phase 9.',
    endpointPrefix: '/api/v1/analytics',
  },
  anomalies: {
    id: 'anomalies',
    name: 'Pattern & Anomaly Detection',
    phase: 8,
    enabled: true,
    connectedToBackend: true,
    statusMessage: 'Connected to live Phase 8 alert and anomaly detection engine.',
    endpointPrefix: '/api/v1/cases/{caseId}/alerts',
  },
  timeline: {
    id: 'timeline',
    name: 'Temporal Analysis & Timeline',
    phase: 6,
    enabled: true,
    connectedToBackend: true,
    statusMessage: 'Connected to live temporal intelligence and timeline API.',
    endpointPrefix: '/api/v1/cases/{caseId}/timeline',
  },
  map: {
    id: 'map',
    name: 'Geospatial Intelligence Map',
    phase: 7,
    enabled: true,
    connectedToBackend: true,
    statusMessage: 'Connected to live Phase 7 geospatial intelligence and investigation map API.',
    endpointPrefix: '/api/v1/cases/{caseId}/map',
  },
  assistant: {
    id: 'assistant',
    name: 'Evidence-Grounded Investigation Copilot',
    phase: 10,
    enabled: true,
    connectedToBackend: true,
    statusMessage: 'Connected to live Phase 10 Evidence-Grounded Investigation Copilot and citation validation APIs.',
    endpointPrefix: '/api/v1/copilot',
  },
  reports: {
    id: 'reports',
    name: 'Audit-Proof Report Builder',
    phase: 14,
    enabled: true,
    connectedToBackend: false,
    statusMessage: 'PDF & JSON dossier generation pipeline scheduled for Phase 14.',
    endpointPrefix: '/api/v1/reports',
  },
  audit: {
    id: 'audit',
    name: 'Immutable Audit Log Persistence',
    phase: 14,
    enabled: true,
    connectedToBackend: false,
    statusMessage: 'PostgreSQL immutable audit logging stream scheduled for Phase 14.',
    endpointPrefix: '/api/v1/audit',
  },
  evidenceIntegrity: {
    id: 'evidenceIntegrity',
    name: 'Evidence Integrity Ledger / Blockchain',
    phase: 9,
    enabled: true,
    connectedToBackend: true,
    statusMessage: 'Connected to live Phase 9 Evidence Integrity Ledger and blockchain validation APIs.',
    endpointPrefix: '/api/v1/evidence/{evidenceId}/integrity',
  },
};
