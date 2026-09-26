# Criminal Intelligence & Network Investigation Platform — API Specification

## Base URL
- **Production / Local ASP.NET Core 8 Web API:** `http://localhost:5000`
- **Swagger Documentation:** `http://localhost:5000/swagger`

---

## Authentication & Headers
All requests (except `/api/v1/auth/login` and health probes) require a standard HTTP Authorization header:
```http
Authorization: Bearer <jwt_access_token>
```

Roles supported:
- `ADMIN`: Full investigative system administration and audits.
- `INVESTIGATOR`: Case management, evidence upload, entity linkage, graph queries.
- `ANALYST`: Read-only access to cases, analytics, and dossiers.

---

## Endpoints

### 1. Authentication (`/api/v1/auth`)

#### `POST /api/v1/auth/login`
Authenticates an investigator using badge credentials.
- **Request Body:**
  ```json
  {
    "email": "dcp.sharma@mahapolice.gov.in",
    "password": "Maharashtra@2024"
  }
  ```
- **Response (200 OK):**
  ```json
  {
    "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    "expiresAtUtc": "2026-09-08T08:00:00Z",
    "user": {
      "id": "usr-dcp-sharma",
      "email": "dcp.sharma@mahapolice.gov.in",
      "fullName": "DCP Rajesh Sharma",
      "role": "INVESTIGATOR",
      "badgeNumber": "MH-POL-04821",
      "agency": "Maharashtra Police",
      "rank": "Deputy Commissioner of Police",
      "unit": "Crime Branch CID"
    }
  }
  ```

#### `GET /api/v1/auth/me`
Validates the current session token and returns the authenticated user context.
- **Response (200 OK):**
  ```json
  {
    "user": {
      "id": "usr-dcp-sharma",
      "email": "dcp.sharma@mahapolice.gov.in",
      "fullName": "DCP Rajesh Sharma",
      "role": "INVESTIGATOR",
      "badgeNumber": "MH-POL-04821",
      "agency": "Maharashtra Police",
      "rank": "Deputy Commissioner of Police",
      "unit": "Crime Branch CID"
    }
  }
  ```

---

### 2. Cases (`/api/v1/cases`)

#### `GET /api/v1/cases`
Returns a list of all active investigations scoped to the officer's clearance.
- **Query Parameters:**
  - `status` (optional): `Active` | `Suspended` | `Closed`
  - `search` (optional): text query matching case title or number
- **Response (200 OK):**
  ```json
  [
    {
      "id": "inv-2026-0841",
      "caseNumber": "CASE-2026-0841-ORG",
      "title": "Operation Iron Vault Syndicate",
      "description": "Hawala routing and illicit corporate layering across Mumbai and Pune.",
      "status": "Active",
      "priority": "Critical",
      "classification": "TOP_SECRET",
      "category": "Organized Crime",
      "jurisdiction": "Mumbai Cyber & Financial Intelligence",
      "district": "Mumbai City",
      "leadOfficerName": "DCP Rajesh Sharma",
      "evidenceCount": 4,
      "entityCount": 14,
      "createdAtUtc": "2026-08-15T10:00:00Z",
      "updatedAtUtc": "2026-09-01T14:30:00Z"
    }
  ]
  ```

#### `GET /api/v1/cases/{id}`
Returns the detailed dossier for a specific investigation.
- **Response (200 OK):** Single case object.
- **Response (404 Not Found):** Case does not exist.

#### `POST /api/v1/cases`
Initializes and persists a new investigation workspace.
- **Request Body:**
  ```json
  {
    "caseNumber": "CASE-2026-1102-CYB",
    "title": "Operation Silent Beacon",
    "description": "Cross-border phishing syndicate targeting financial accounts.",
    "priority": "High",
    "classification": "RESTRICTED",
    "category": "Cybercrime",
    "jurisdiction": "State Cyber Cell",
    "district": "Pune"
  }
  ```
- **Response (201 Created):** Newly created case with system-assigned ID and audit trail entry.

#### `PUT /api/v1/cases/{id}`
Updates details, priority, or status of an existing investigation.
- **Response (200 OK):** Updated case object.

#### `DELETE /api/v1/cases/{id}`
Archives or removes an investigation (Admin/Lead Investigator only).
- **Response (204 No Content)**.

#### `GET /api/v1/cases/stats/summary`
Returns aggregate workspace statistics for the investigator dashboard.
- **Response (200 OK):**
  ```json
  {
    "activeInvestigations": 8,
    "totalEntities": 142,
    "totalInvestigations": 15,
    "highRiskAlerts": 19,
    "totalEvidence": 64,
    "connectedNetworks": 5
  }
  ```

---

### 3. Evidence Management (`/api/v1/evidence`)

#### `GET /api/v1/evidence`
Lists evidence files, optionally filtered by `caseId`.
- **Query Parameters:**
  - `caseId` (optional): Filter to evidence linked to a specific investigation.
- **Response (200 OK):** List of evidence records.

#### `POST /api/v1/evidence/upload`
Uploads a new evidence file, calculates raw byte-level SHA-256 hash, persists file to sandboxed storage, inserts database record, and writes an audit log.
- **Content-Type:** `multipart/form-data`
- **Form Fields:**
  - `file`: Raw binary file (Max: 50MB. Types: PDF, CSV, XLSX, TXT, JSON, JPG, PNG)
  - `caseId`: UUID / Case identifier
  - `description`: Narrative of evidence source and custody
  - `clearance`: `CONFIDENTIAL` | `RESTRICTED` | `SECRET`
- **Response (201 Created):**
  ```json
  {
    "id": "ev-2026-91823",
    "caseId": "inv-2026-0841",
    "fileName": "bank_transactions_aug2026.csv",
    "mimeType": "text/csv",
    "fileSize": 145920,
    "sha256Hash": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
    "uploadedByName": "DCP Rajesh Sharma",
    "uploadedAtUtc": "2026-09-08T00:15:00Z",
    "processingStatus": "UPLOADED",
    "clearance": "RESTRICTED",
    "description": "Financial ledger intercepted from server logs"
  }
  ```

---

### 4. Knowledge Graph & Graph Explorer (`/api/v1/graph`, `/api/v1/cases/{caseId}/graph`, `/api/v1/entities/{id}/*`)

#### `GET /api/v1/cases/{caseId}/graph`
Returns the authorized, case-scoped knowledge graph containing only human-verified entities and relationships.
- **Path Parameters:** `caseId` (UUID or CaseNumber, e.g. `CASE-2026-001`)
- **Query Parameters:**
  - `entityType` (optional): Filter to specific canonical entity type (`PERSON`, `PHONE`, `VEHICLE`, `LOCATION`, `ORGANIZATION`, `ACCOUNT`, `DEVICE`, `EVENT`, `DOCUMENT`)
  - `relationshipType` (optional): Filter to specific canonical relationship type (`COMMUNICATED_WITH`, `OPERATES`, `LOCATED_AT`, `ASSOCIATE_OF`, `MEMBER_OF`, `TRANSFERRED_MONEY_TO`, etc.)
  - `depth` (optional, default: 1, max: 3)
  - `search` (optional): Substring filter on entity names
  - `limit` (optional, default: 200, max: 500)
- **Response (200 OK):**
  ```json
  {
    "caseId": "inv-2026-001",
    "totalNodes": 5,
    "totalEdges": 6,
    "nodes": [
      {
        "id": "can-ent-pune-001",
        "label": "Rahul Mehta",
        "name": "Rahul Mehta",
        "type": "PERSON",
        "caseId": "inv-2026-001",
        "connectionsCount": 5,
        "evidenceCount": 2,
        "verified": true,
        "risk": "HIGH",
        "properties": {
          "normalized_value": "Rahul Mehta",
          "location": "Shivajinagar, Pune"
        }
      }
    ],
    "edges": [
      {
        "id": "can-rel-pune-001",
        "source": "can-ent-pune-001",
        "target": "can-ent-pune-002",
        "type": "COMMUNICATED_WITH",
        "confidence": 0.98,
        "sourceEvidenceId": "ev-pune-surv-001",
        "sourceLocation": "Line 3: Subject observed making calls",
        "supportingEvidenceCount": 2,
        "verifiedBy": "DCP Rajesh Sharma"
      }
    ]
  }
  ```

#### `GET /api/v1/entities/{id}/neighborhood`
Performs bounded multi-hop neighborhood expansion around a central entity.
- **Query Parameters:**
  - `depth` (integer, 1–3, default: 1)
  - `caseId` (optional): Case scope filter
- **Response (200 OK):** `EntityNeighborhoodDto` with center entity, connected nodes, and edges up to specified hop distance.

#### `GET /api/v1/entities/{id}/relationships`
Returns all verified relationships directly connected to the specified entity.
- **Response (200 OK):** List of `GraphEdgeDto`.

#### `GET /api/v1/graph/search`
Queries backend/Neo4j for entities matching names, phone numbers, vehicle license plates, or IDs within case scope.
- **Query Parameters:**
  - `query`: Search term
  - `caseId` (optional): Case scope filter
- **Response (200 OK):** List of `GraphSearchResultDto` sorted by connection count.

#### `GET /api/v1/graph/path`
Finds the shortest investigative path between two entities in the knowledge graph.
- **Query Parameters:**
  - `startEntityId`: Origin entity ID
  - `endEntityId`: Destination entity ID
  - `maxHops` (optional, 1–6, default: 4)
  - `caseId` (optional): Case scope filter
- **Response (200 OK):** `ShortestPathDto` with `found: true|false`, `hopsCount`, node list, and edge list.

#### `GET /api/v1/graph/statistics`
Computes real structural metrics and analytical indicators from the knowledge graph.
- **Query Parameters:** `caseId` (optional)
- **Response (200 OK):**
  - `totalNodes`: Total entities
  - `totalEdges`: Total relationships
  - `entityTypeDistribution`: Breakdown per canonical category
  - `relationshipTypeDistribution`: Breakdown per relationship type
  - `connectedComponents`: Network clusters identified via graph traversal
  - `centralityRankings`: Degree centrality and neutral analytical indicators (`High Connectivity Lead`, `Network Association`, `Connected Entity`).

#### `GET /api/v1/graph/relationship/{relationshipId}`
Returns detailed relationship metadata including all supporting evidence records, citations, and SHA-256 hashes for provenance tracing.
- **Response (200 OK):** `RelationshipDetailDto` with `supportingEvidence` list and primary evidence ID.

#### `GET /api/v1/graph`
Returns basic graph dataset for visualization.

#### `POST /api/v1/graph/probe`
Executes an end-to-end Neo4j lifecycle verification:
1. Connects to Neo4j.
2. Creates a temporary probe node `(:SystemProbe { probeId: ... })`.
3. Queries and verifies node presence.
4. Deletes probe node.
- **Response (200 OK):**
  ```json
  {
    "status": "success",
    "probeId": "probe-8192301",
    "latencyMs": 4.12,
    "verified": true
  }
  ```

---

### 5. Audit Logging (`/api/v1/audit`)

#### `GET /api/v1/audit`
Retrieves chronological audit events for chain of custody and supervisory review.
- **Query Parameters:**
  - `limit`: Default 50, max 500.
- **Response (200 OK):**
  ```json
  [
    {
      "id": "aud-109283",
      "actorId": "usr-dcp-sharma",
      "actorName": "DCP Rajesh Sharma",
      "action": "EVIDENCE_UPLOADED",
      "resourceType": "evidence",
      "resourceId": "ev-2026-91823",
      "metadataJson": "{\"fileName\":\"bank_transactions_aug2026.csv\",\"sha256\":\"e3b0...\"}",
      "ipAddress": "127.0.0.1",
      "createdAtUtc": "2026-09-08T00:15:00Z"
    }
  ]
  ```

---

### 6. System Diagnostics & Health (`/api/v1/system`)

#### `GET /api/v1/system/health`
Performs real live probes against PostgreSQL, Neo4j, and Python FastAPI AI service.
- **Response (200 OK when all healthy):**
  ```json
  {
    "status": "healthy",
    "checkedAtUtc": "2026-09-08T00:15:00Z",
    "services": {
      "postgresql": "healthy",
      "neo4j": "healthy",
      "ai_service": "healthy"
    }
  }
  ```
- **Response (503 Service Unavailable when any dependency is degraded):**
  ```json
  {
    "status": "degraded",
    "checkedAtUtc": "2026-09-08T00:15:00Z",
    "services": {
      "postgresql": "healthy",
      "neo4j": "degraded",
      "ai_service": "healthy"
    }
  }
  ```

#### `GET /api/v1/system/status`
Detailed infrastructure status with per-service latency, versions, and connectivity messages.

---

### 7. Entity Resolution & Cross-Case Intelligence (`/api/v1/entity-resolution`, `/api/v1/entities`, `/api/v1/cases`)

#### `POST /api/v1/entity-resolution/run`
Executes deterministic multi-signal candidate generation across cases.
- **Request Body:**
  ```json
  {
    "caseId": "inv-2026-001",
    "entityTypes": ["PERSON", "ORGANIZATION"],
    "minimumScore": 0.60
  }
  ```
- **Response (200 OK):**
  ```json
  {
    "entitiesCompared": 18,
    "potentialMatchesFound": 3,
    "highConfidenceCandidates": 2,
    "candidatesCreated": 2,
    "duration": "00:00:00.0851420"
  }
  ```

#### `GET /api/v1/entity-resolution/candidates`
Retrieves generated resolution candidates with filtering by case, status, and score threshold.
- **Query Parameters:**
  - `caseId`: Optional focus case ID.
  - `status`: Filter by `PENDING`, `APPROVED`, or `REJECTED`.
  - `entityType`: Filter by `PERSON`, `VEHICLE`, etc.
  - `minScore`: Minimum match score (0.0 - 1.0).
- **Response (200 OK):**
  ```json
  [
    {
      "id": "cand-9f82ab",
      "sourceCaseId": "case-pune-001",
      "targetCaseId": "case-mum-002",
      "sourceCaseNumber": "CASE-2026-001",
      "targetCaseNumber": "CASE-2026-002",
      "entityType": "PERSON",
      "matchScore": 0.93,
      "matchStatus": "PENDING",
      "matchMethod": "MULTI_SIGNAL",
      "factors": [
        {
          "type": "SHARED_PHONE",
          "weight": 0.45,
          "score": 1.0,
          "description": "Identical normalized phone number: +919000001001"
        },
        {
          "type": "NAME_SIMILARITY",
          "weight": 0.30,
          "score": 0.88,
          "description": "High name similarity (88%): 'Rahul Mehta' vs 'R. Sharma'"
        }
      ]
    }
  ]
  ```

#### `GET /api/v1/entity-resolution/candidates/{id}/compare`
Returns side-by-side evidence and attributes for human review.
- **Response (200 OK):**
  ```json
  {
    "candidateId": "cand-9f82ab",
    "matchScore": 0.93,
    "matchStatus": "PENDING",
    "factors": [...],
    "sideA": {
      "entityId": "ent-src-01",
      "caseId": "case-pune-001",
      "caseNumber": "CASE-2026-001",
      "canonicalName": "Rahul Mehta",
      "phoneNumber": "+91-9000001001",
      "supportingEvidence": [...]
    },
    "sideB": {
      "entityId": "ent-tgt-01",
      "caseId": "case-mum-002",
      "caseNumber": "CASE-2026-002",
      "canonicalName": "R. Sharma",
      "phoneNumber": "+91-9000001001",
      "supportingEvidence": [...]
    }
  }
  ```

#### `POST /api/v1/entity-resolution/candidates/{id}/approve`
Approves a candidate match, creating an active cross-case connection, audit record, and Neo4j edge.
- **Request Body:**
  ```json
  {
    "status": "APPROVED",
    "reviewNotes": "Confirmed via corroborating telecom intercept logs."
  }
  ```

#### `POST /api/v1/entity-resolution/candidates/{id}/reject`
Rejects a candidate match with recorded investigator reasoning.
- **Request Body:**
  ```json
  {
    "status": "REJECTED",
    "reviewNotes": "Confirmed distinct individuals via differing identification documents."
  }
  ```

#### `GET /api/v1/entities/{id}/cross-case-matches`
Retrieves all potential cross-case candidate matches for a specific entity node.

#### `GET /api/v1/cases/{id}/cross-case-connections`
Retrieves verified cross-case links where the specified case is either the source or target.

#### `GET /api/v1/cases/{id}/cross-case-network`
Returns graph-ready nodes and edges representing cross-case connections for interactive network visualization.

---

### 8. Graph Analytics & Graph Attention Network (GAT) (`/api/v1/cases/{caseId}/analytics` & `/api/v1/analytics`)

#### `POST /api/v1/cases/{caseId}/analytics/run`
Executes traditional centrality algorithms (Brandes betweenness, Wasserman-Faust closeness, PageRank, Louvain communities) and PyTorch GAT link prediction inference on the case network.
- **Request Body (Optional):**
  ```json
  {
    "includeCrossCase": false,
    "gatEmbeddingDim": 32,
    "gatHeads": 4,
    "candidateThreshold": 0.5,
    "maxCandidates": 50
  }
  ```
- **Response (200 OK):**
  ```json
  {
    "id": "run-482a-99",
    "caseId": "inv-2026-001",
    "status": "COMPLETED",
    "startedAtUtc": "2026-09-08T02:00:00Z",
    "completedAtUtc": "2026-09-08T02:00:02Z",
    "nodeCount": 18,
    "edgeCount": 24,
    "metricsGenerated": 18,
    "modelVersion": "gat-link-prediction-v1",
    "networkDensity": 0.1569,
    "averageDegree": 2.67,
    "averagePathLength": 2.45,
    "connectedComponentsCount": 1,
    "communitiesCount": 3,
    "executedBy": "DCP Rajesh Sharma"
  }
  ```

#### `GET /api/v1/cases/{caseId}/analytics`
Returns the latest completed graph analysis run and summary statistics for the case.

#### `GET /api/v1/cases/{caseId}/analytics/centrality`
Retrieves sorted entity centrality metrics.
- **Query Parameters:**
  - `sortBy`: `Betweenness` (default), `PageRank`, `Degree`, `Closeness`
  - `limit`: Maximum records to return (default: 50)
- **Response (200 OK):**
  ```json
  {
    "caseId": "inv-2026-001",
    "totalNodes": 18,
    "sortedBy": "Betweenness",
    "metrics": [
      {
        "entityId": "ent-bridge-001",
        "entityName": "Vikram Gaikwad",
        "entityType": "Person",
        "degree": 6,
        "inDegree": 3,
        "outDegree": 3,
        "normalizedDegree": 0.3529,
        "betweennessCentrality": 0.6250,
        "closenessCentrality": 0.7241,
        "pageRank": 0.1420,
        "analyticalIndicator": "KEY_NEXUS",
        "communityId": "comm-1",
        "componentId": "comp-1"
      }
    ]
  }
  ```

#### `GET /api/v1/cases/{caseId}/analytics/communities`
Retrieves detected association clusters (Louvain modularity partitions) with density and entity breakdown.

#### `GET /api/v1/cases/{caseId}/analytics/components`
Retrieves connected components and top nexus entities for disconnected graph analysis.

#### `GET /api/v1/cases/{caseId}/analytics/statistics`
Retrieves comprehensive graph topology summary statistics and entity/relationship type distributions.

#### `GET /api/v1/cases/{caseId}/analytics/leads`
Retrieves staged model-generated relationship hypotheses (`GraphAnalyticalLead`).
- **Query Parameters:**
  - `status`: `PENDING`, `CONFIRMED`, `DISMISSED`
  - `minScore`: Minimum composite confidence score (e.g., `0.5`)
  - `limit`: Number of leads to return (default: 50)

#### `GET /api/v1/entities/{id}/analytics`
Returns deep network centrality, community affiliation, component details, and adjacent model leads for a specific entity.

#### `POST /api/v1/analytics/leads/{id}/review`
Processes investigator review for a model-generated link prediction lead.
- **Request Body:**
  ```json
  {
    "status": "CONFIRMED",
    "reviewNotes": "Corroborated by call records and financial trail.",
    "suggestedRelationshipType": "ASSOCIATE_OF"
  }
  ```
- **Response (200 OK):**
  ```json
  {
    "id": "lead-88fa-01",
    "caseId": "inv-2026-001",
    "status": "CONFIRMED",
    "reviewedAtUtc": "2026-09-08T02:05:00Z",
    "reviewedBy": "DCP Rajesh Sharma",
    "reviewNotes": "Corroborated by call records and financial trail.",
    "resultingRelationshipId": "rel-99bb-01"
  }
  ```

---

### 9. Timeline & Temporal Intelligence (`/api/v1/cases/{caseId}/timeline` & `/api/v1/timeline`)

All endpoints require JWT bearer authentication. Endpoints executing analysis runs or modifying review status require `ADMIN`, `INVESTIGATOR`, or `ANALYST` role.

#### `GET /api/v1/cases/{caseId}/timeline`
Retrieves paginated timeline response with events, active temporal clusters, and sequence highlights.
- **Query Parameters:**
  - `startDate`: ISO 8601 start bound
  - `endDate`: ISO 8601 end bound
  - `eventType`: Filter by event type (`COMMUNICATION`, `FINANCIAL_TRANSACTION`, `SURVEILLANCE_SIGHTING`, `TRAVEL_MOVEMENT`)
  - `entityId`: Filter by involved entity UUID
  - `location`: Textual location query
  - `minConfidence`: Minimum extraction confidence threshold (0.0 to 1.0)
  - `verificationStatus`: `PENDING`, `APPROVED`, `REJECTED`
  - `page`: 1-based page number (default: 1)
  - `pageSize`: Page size (default: 50)
- **Response (200 OK):**
  ```json
  {
    "caseId": "inv-2026-001",
    "totalEvents": 5,
    "page": 1,
    "pageSize": 50,
    "activePeriodStartUtc": "2026-03-12T10:00:00Z",
    "activePeriodEndUtc": "2026-03-15T00:00:00Z",
    "events": [
      {
        "id": "evt-001",
        "caseId": "inv-2026-001",
        "eventType": "SURVEILLANCE_SIGHTING",
        "description": "Rahul Sharma spotted entering Shivajinagar Docks warehouse.",
        "startTimeUtc": "2026-03-12T10:00:00Z",
        "endTimeUtc": "2026-03-12T11:00:00Z",
        "timePrecision": "MINUTE",
        "location": "Shivajinagar Docks, Berth 4",
        "locationEntityId": "can-ent-pune-dock",
        "relatedEntityNames": ["Rahul Sharma"],
        "relatedEntityIds": ["can-ent-1"],
        "confidence": 0.95,
        "verificationStatus": "APPROVED",
        "sourceEvidenceId": "ev-pune-001",
        "sourceEvidenceFileName": "cctv_log_dock_gate4.pdf",
        "sourceEvidenceSha256": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
        "evidenceIntegrityVerified": true,
        "sourceLocation": "Page 3, Line 14",
        "sourcePage": 3,
        "createdAtUtc": "2026-09-08T00:00:00Z"
      }
    ],
    "clusters": [
      {
        "clusterId": "cluster-1",
        "clusterLabel": "High-Density Activity Window (4 Events)",
        "startTimeUtc": "2026-03-12T10:00:00Z",
        "endTimeUtc": "2026-03-12T18:00:00Z",
        "eventCount": 4,
        "distinctEntitiesCount": 3,
        "distinctLocationsCount": 2,
        "intensity": "HIGH",
        "keyEntities": ["Rahul Sharma", "Vikram Gaikwad"],
        "keyLocations": ["Shivajinagar Docks"],
        "events": [...]
      }
    ],
    "sequenceHighlights": [...]
  }
  ```

#### `GET /api/v1/cases/{caseId}/timeline/events`
Returns raw array of chronological `TimelineEventDto` items matching the query.

#### `GET /api/v1/entities/{entityId}/timeline`
Retrieves chronological activity timeline specifically for a target entity.
- **Query Parameters:** `caseId` (optional scope)

#### `GET /api/v1/relationships/{relationshipId}/timeline`
Retrieves interaction timeline events involving both endpoints of a graph relationship.
- **Query Parameters:** `caseId` (optional scope)

#### `GET /api/v1/cases/{caseId}/timeline/range`
Retrieves overall chronological bounds for the case (`earliestEventUtc`, `latestEventUtc`, `totalEvents`).

#### `GET /api/v1/cases/{caseId}/temporal-analysis`
Retrieves the latest completed temporal analysis execution results (detected overlaps and clusters).

#### `POST /api/v1/cases/{caseId}/temporal-analysis/run`
Triggers an automated temporal intelligence run on the case.
- **Request Body (Optional):**
  ```json
  {
    "includeCrossCase": true,
    "authorizedCaseIds": ["case-mum-002"],
    "minOverlapDurationMinutes": 5.0
  }
  ```
- **Response (200 OK):** `TemporalAnalysisResultDto`

#### `GET /api/v1/cases/{caseId}/temporal-overlaps`
Returns all staged co-presence signals where two entities were active at the same location concurrently.
- **Query Parameters:**
  - `entityId`: Filter overlaps involving this entity
  - `location`: Filter by location
  - `minDurationMinutes`: Minimum overlap duration threshold

#### `GET /api/v1/entities/{entityId}/sequence`
Reconstructs an entity's step-by-step movement trajectory with elapsed delta calculations between steps.
- **Response (200 OK):**
  ```json
  {
    "entityId": "can-ent-1",
    "entityName": "Rahul Sharma",
    "totalSteps": 3,
    "sequenceStartUtc": "2026-03-12T10:00:00Z",
    "sequenceEndUtc": "2026-03-12T15:30:00Z",
    "steps": [
      {
        "stepIndex": 1,
        "eventId": "evt-001",
        "eventType": "SURVEILLANCE_SIGHTING",
        "description": "Spotted at Shivajinagar Docks warehouse.",
        "timestampUtc": "2026-03-12T10:00:00Z",
        "location": "Shivajinagar Docks",
        "involvedEntities": ["Rahul Sharma"],
        "elapsedFromPrevious": "Origin",
        "elapsedMinutesFromPrevious": 0.0
      },
      {
        "stepIndex": 2,
        "eventId": "evt-003",
        "eventType": "FINANCIAL_TRANSACTION",
        "description": "Cash withdrawal at Pune Central.",
        "timestampUtc": "2026-03-12T14:00:00Z",
        "location": "Pune Central",
        "involvedEntities": ["Rahul Sharma"],
        "elapsedFromPrevious": "+4h",
        "elapsedMinutesFromPrevious": 240.0
      }
    ]
  }
  ```

#### `GET /api/v1/cases/{caseId}/temporal-summary`
Returns high-level temporal dashboard indicators (`totalEvents`, `activePeriodStartUtc`, `activePeriodEndUtc`, `activityPeaks`, `pendingSignals`, `confirmedSignals`).

#### `POST /api/v1/cases/{caseId}/temporal-signals/{signalId}/review`
Processes investigator confirmation or dismissal of a staged temporal signal.
- **Request Body:**
  ```json
  {
    "status": "CONFIRMED",
    "reviewNotes": "Confirmed simultaneous presence via dock security log."
  }
  ```
- **Response (200 OK):** Updated `TemporalOverlapDto`

---

### 10. Geospatial Intelligence & Investigation Map (`/api/v1/cases/{caseId}/map`, `/api/v1/locations`, `/api/v1/entities/{id}/travel-sequence`)

#### `GET /api/v1/cases/{caseId}/map`
Returns complete case-scoped GIS data: canonical locations, regional cluster envelopes, and active staged signals.
- **Query Parameters:** `eventType` (optional), `entityId` (optional), `startDate` (optional), `endDate` (optional), `verifiedOnly` (boolean, default: false).
- **Response (200 OK):**
  ```json
  {
    "caseId": "case-2026-001",
    "totalLocations": 3,
    "totalEvents": 14,
    "totalSignals": 2,
    "locations": [
      {
        "id": "loc-pune-shivaji",
        "name": "Shivajinagar Docks Terminal",
        "latitude": 18.5314,
        "longitude": 73.8446,
        "geocodePrecision": "BUILDING",
        "city": "Pune",
        "state": "Maharashtra",
        "eventCount": 6,
        "entityCount": 4,
        "evidenceCount": 3
      }
    ],
    "clusters": [
      {
        "clusterId": "cluster-geo-1",
        "clusterLabel": "Pune Region (2 Locations)",
        "centroidLatitude": 18.5240,
        "centroidLongitude": 73.8598,
        "locationCount": 2,
        "eventCount": 9
      }
    ],
    "activeSignals": []
  }
  ```

#### `GET /api/v1/cases/{caseId}/map/locations`
Lists canonical locations in the case with optional `search` query parameter.

#### `GET /api/v1/locations/{id}`
Returns full details for a canonical location.

#### `GET /api/v1/locations/{id}/activity`
Returns the complete activity dossier for a location: associated chronological events, observed entities with visit counts, and supporting evidence citations with SHA-256 verification.

#### `GET /api/v1/entities/{id}/travel-sequence`
Reconstructs an entity's chronological movement trajectory across canonical locations, computing geodesic Haversine distance, elapsed hours, implied velocity (km/h), and supersonic speed warning flags (`isImplausibleSpeed`).

#### `GET /api/v1/cases/{caseId}/map/proximity`
Executes an exact Haversine proximity query around specified coordinates.
- **Query Parameters:** `latitude`, `longitude`, `radiusKm`.

#### `POST /api/v1/cases/{caseId}/map/analysis`
Executes backend spatial-temporal overlap and clustering analysis.
- **Request Body:**
  ```json
  {
    "clusterRadiusKm": 25.0,
    "velocityWarningThresholdKmh": 900.0,
    "includeCrossCase": false
  }
  ```

#### `GET /api/v1/cases/{caseId}/map/signals`
Retrieves staged spatial signals with optional `status` (`PENDING`, `CONFIRMED`, `DISMISSED`) and `signalType` filtering.

#### `POST /api/v1/cases/{caseId}/map/signals/{id}/review`
Processes investigator confirmation or dismissal of a staged spatial signal with required review notes. Note: Confirming updates the review status for dossiers and logs an immutable audit entry, but **never** mutates knowledge graph edges.
- **Request Body:**
  ```json
  {
    "status": "CONFIRMED",
    "reviewNotes": "Confirmed co-presence via cargo delivery manifest."
  }
  ```

---

### 11. Alerts & Anomaly Detection Subsystem (Phase 8)

Provides analytical anomaly detection, deduplication, explainable priority scoring, and review lifecycle management.

#### `GET /api/cases/{caseId}/alerts`
Queries investigative alerts with multi-dimensional filtering.
- **Query Parameters:**
  - `status`: Optional filter (`NEW`, `ACKNOWLEDGED`, `UNDER_REVIEW`, `RESOLVED`, `DISMISSED`).
  - `severity`: Optional filter (`CRITICAL`, `HIGH`, `MEDIUM`, `LOW`).
  - `alertType`: Optional filter (`NETWORK_ANOMALY`, `TEMPORAL_ANOMALY`, `GEOGRAPHIC_ANOMALY`, `RELATIONSHIP_SURGE`, `ACTIVITY_SPIKE`, `UNUSUAL_TRAVEL`, `DATA_CONSISTENCY`, `CROSS_CASE_PATTERN`, `MODEL_SIGNAL`).
  - `entityId`: Optional entity filter (matches source or target entity ID or name).
  - `search`: Case-insensitive text filter across title, description, and explanation.
  - `limit`: Page limit (default: 50).
  - `offset`: Page offset (default: 0).
- **Response (200 OK):**
  ```json
  [
    {
      "id": "alt-demo-001",
      "caseId": "inv-2026-001",
      "alertRunId": "run-alert-demo-001",
      "alertType": "NETWORK_ANOMALY",
      "severity": "HIGH",
      "status": "NEW",
      "title": "High Connectivity Nexus Entity: Rahul Sharma",
      "description": "Entity recorded 3 direct relationships and betweenness centrality of 0.42, serving as a structural hub.",
      "sourceEntityId": "ent-rahul-sharma",
      "sourceEntityName": "Rahul Sharma",
      "score": 0.88,
      "priorityScore": 0.81,
      "detectionMethod": "DegreeCentralityDisparity",
      "detectionVersion": "v1.0",
      "explanation": "WHAT: High relationship density observed.\nWHO: Rahul Sharma (ID: ent-rahul-sharma)\nMETRICS: Degree = 3 (Case Avg = 1.6)\nWHY: Marked structural hub position in the investigation network.",
      "deduplicationFingerprint": "SHA256:DEMO-NET-RAHUL-SHARMA",
      "createdAtUtc": "2026-03-12T10:00:00Z",
      "updatedAtUtc": "2026-03-12T10:00:00Z"
    }
  ]
  ```

#### `GET /api/cases/{caseId}/alerts/summary`
Returns aggregate statistics, severity distributions, and category breakdowns for a case workspace.
- **Response (200 OK):**
  ```json
  {
    "caseId": "inv-2026-001",
    "totalAlerts": 3,
    "newAlerts": 2,
    "acknowledgedAlerts": 1,
    "underReviewAlerts": 0,
    "resolvedAlerts": 0,
    "dismissedAlerts": 0,
    "criticalSeverity": 0,
    "highSeverity": 2,
    "mediumSeverity": 1,
    "lowSeverity": 0,
    "networkAnomalies": 1,
    "temporalAnomalies": 0,
    "geographicAnomalies": 1,
    "relationshipSurges": 0,
    "activitySpikes": 0,
    "unusualTravel": 0,
    "dataConsistency": 0,
    "crossCasePatterns": 1,
    "modelSignals": 0
  }
  ```

#### `GET /api/cases/{caseId}/alerts/{alertId}`
Returns complete details and structured explanation for a single alert.

#### `POST /api/cases/{caseId}/alerts/run`
Executes backend anomaly detection across all registered analytical detectors.
- **Request Body:**
  ```json
  {
    "enabledDetectors": ["NETWORK_ANOMALY", "TEMPORAL_ANOMALY", "GEOGRAPHIC_ANOMALY"],
    "includeCrossCase": true,
    "geographicDistanceThresholdKm": 100.0,
    "relationshipSurgeThreshold": 4
  }
  ```
- **Response (200 OK):**
  ```json
  {
    "runId": "run-alert-20260312-001",
    "caseId": "inv-2026-001",
    "status": "COMPLETED",
    "startedAtUtc": "2026-03-12T10:30:00Z",
    "completedAtUtc": "2026-03-12T10:30:00.045Z",
    "detectorsExecuted": 9,
    "signalsGenerated": 3,
    "alertsCreated": 3,
    "alertsDeduplicated": 0,
    "executionDurationMs": 45,
    "createdAlerts": [...]
  }
  ```

#### `POST /api/alerts/{alertId}/acknowledge`
Transitions alert status to `ACKNOWLEDGED` and logs an audit entry.

#### `POST /api/alerts/{alertId}/start-review`
Transitions alert status to `UNDER_REVIEW` and logs an audit entry.

#### `POST /api/alerts/{alertId}/resolve`
Resolves an alert with required investigator review notes. Note: Does **never** mutate knowledge graph edges.
- **Request Body:**
  ```json
  {
    "notes": "Confirmed benign logistics transition with Pune depot officer."
  }
  ```

#### `POST /api/alerts/{alertId}/dismiss`
Transitions alert status to `DISMISSED` with required investigator notes.
- **Request Body:**
  ```json
  {
    "notes": "Verified coincidental transit overlap."
  }
  ```

---

## 12. Evidence Integrity Ledger Endpoints (Phase 9)

### Overview
Cryptographically verifiable, append-only, tamper-evident ledger for digital evidence. Off-chain physical storage with on-chain SHA-256 fingerprints.

---

### Endpoints

#### `GET /api/v1/evidence/{evidenceId}/integrity`
Returns the current integrity status based on registered metadata and the latest ledger block.
- **Response (200 OK):**
  ```json
  {
    "evidenceId": "ev-2026-001-cdr",
    "fileName": "call_records_pune.csv",
    "version": 1,
    "actualFileSha256": "0a886a80a5de27a26b198c35e6d93683083bec60501f84efdba6f682d3998aae",
    "registeredSha256": "0a886a80a5de27a26b198c35e6d93683083bec60501f84efdba6f682d3998aae",
    "ledgerSha256": "0a886a80a5de27a26b198c35e6d93683083bec60501f84efdba6f682d3998aae",
    "status": "VERIFIED",
    "chainStatus": "VALID",
    "blockIndex": 1,
    "action": "UPLOAD",
    "actorName": "DCP Rajesh Sharma",
    "verifiedAtUtc": "2026-09-08T12:00:00Z",
    "explanation": "Registered evidence hash matches ledger record."
  }
  ```

#### `GET /api/v1/evidence/{evidenceId}/integrity/history`
Retrieves all chronological ledger blocks recorded for this evidence item.
- **Response (200 OK):** Array of `EvidenceLedgerBlockDto`.

#### `POST /api/v1/evidence/{evidenceId}/integrity/verify`
Streams raw bytes directly from physical disk storage, computes runtime SHA-256, cross-references with evidence record and ledger block, and checks full blockchain integrity.
- **Response (200 OK):**
  ```json
  {
    "evidenceId": "ev-2026-001-cdr",
    "fileName": "call_records_pune.csv",
    "version": 1,
    "actualFileSha256": "0a886a80a5de27a26b198c35e6d93683083bec60501f84efdba6f682d3998aae",
    "registeredSha256": "0a886a80a5de27a26b198c35e6d93683083bec60501f84efdba6f682d3998aae",
    "ledgerSha256": "0a886a80a5de27a26b198c35e6d93683083bec60501f84efdba6f682d3998aae",
    "status": "VERIFIED",
    "chainStatus": "VALID",
    "blockIndex": 1,
    "action": "UPLOAD",
    "actorName": "DCP Rajesh Sharma",
    "verifiedAtUtc": "2026-09-08T12:00:05Z",
    "explanation": "Evidence file bytes match registered SHA-256 and immutable blockchain ledger record. Cryptographic integrity confirmed."
  }
  ```
- **Tamper Response Example (200 OK):**
  ```json
  {
    "evidenceId": "ev-2026-001-cdr",
    "status": "EVIDENCE_MODIFIED",
    "chainStatus": "VALID",
    "actualFileSha256": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855",
    "registeredSha256": "0a886a80a5de27a26b198c35e6d93683083bec60501f84efdba6f682d3998aae",
    "ledgerSha256": "0a886a80a5de27a26b198c35e6d93683083bec60501f84efdba6f682d3998aae",
    "explanation": "Evidence content does not match registered hash. Physical file modifications detected."
  }
  ```

#### `GET /api/v1/integrity/ledger`
Returns paginated blocks from the immutable ledger.
- **Query Parameters:**
  - `limit`: number of blocks (default 50, max 200)
  - `offset`: pagination offset (default 0)
- **Response (200 OK):** Array of `EvidenceLedgerBlockDto`.

#### `GET /api/v1/integrity/ledger/{blockIndex}`
Retrieves full details of a single block.
- **Response (200 OK):** `EvidenceLedgerBlockDto`.

#### `POST /api/v1/integrity/ledger/verify-chain`
Performs end-to-end cryptographic chain validation from Block #0 (Genesis) to tip.
- **Response (200 OK):**
  ```json
  {
    "isValid": true,
    "totalBlocks": 4,
    "checkedBlocks": 4,
    "firstInvalidBlockIndex": null,
    "failureReason": null,
    "validatedAtUtc": "2026-09-08T12:00:10Z"
  }
  ```

#### `GET /api/v1/integrity/reconciliation`
Returns list of persisted evidence items lacking a cryptographic ledger registration.
- **Response (200 OK):** Array of `ReconciliationItemDto`.

#### `POST /api/v1/integrity/reconciliation/{evidenceId}`
Verifies physical file on disk and appends an authorized ledger block with `Action = "RECONCILIATION"`.
- **Response (200 OK):** `EvidenceLedgerBlockDto`.

---

## 13. Evidence-Grounded Investigation Copilot Endpoints (Phase 10)

### Overview
Retrieval-first, evidence-grounded AI copilot for natural-language case investigations. Operates strictly over authoritative case records (PostgreSQL, Neo4j, Evidence Store, Ledger, and Anomaly Models). Enforces zero-fabrication, strict citation validation, prompt injection defense, presumption of innocence, and cross-case authorization boundaries.

---

### Endpoints

#### `POST /api/v1/copilot/query`
Executes an evidence-grounded query within the specified case context.
- **Request Body:**
  ```json
  {
    "caseId": "inv-2026-0841",
    "query": "How is Rajesh Sharma connected to Hawala Node Alpha?",
    "conversationId": "conv-case-841-01",
    "includePredictions": true,
    "confidenceThreshold": 0.65,
    "retrievalLimit": 25
  }
  ```
- **Response (200 OK):**
  ```json
  {
    "answer": "Rajesh Sharma is directly connected to Hawala Node Alpha via verified transaction records.",
    "caseId": "inv-2026-0841",
    "conversationId": "conv-case-841-01",
    "detectedIntent": "SHORTEST_PATH",
    "confidenceScore": 0.94,
    "claims": [
      {
        "statement": "Rajesh Sharma is connected to Hawala Node Alpha via DIRECT_TRANSACTION.",
        "supportStatus": "FULLY_SUPPORTED",
        "evidenceCitationIds": ["ev-2026-001-cdr"],
        "entityCitationIds": ["ent-841-01", "ent-841-05"],
        "relationshipCitationIds": ["rel-841-01"],
        "timelineCitationIds": [],
        "alertCitationIds": [],
        "modelSignalCitationIds": []
      }
    ],
    "evidenceCitations": [
      {
        "evidenceId": "ev-2026-001-cdr",
        "title": "Call Detail Records - Syndicate Cell A",
        "evidenceType": "CDR",
        "fileName": "call_records_pune.csv",
        "integrityStatus": "VERIFIED",
        "sha256": "0a886a80a5de27a26b198c35e6d93683083bec60501f84efdba6f682d3998aae",
        "ledgerBlockIndex": 1,
        "isTampered": false
      }
    ],
    "entityCitations": [
      {
        "entityId": "ent-841-01",
        "canonicalName": "Rajesh Sharma",
        "entityType": "PERSON",
        "role": "Suspect",
        "riskScore": 0.85
      }
    ],
    "relationshipCitations": [
      {
        "relationshipId": "rel-841-01",
        "sourceEntityId": "ent-841-01",
        "sourceEntityName": "Rajesh Sharma",
        "targetEntityId": "ent-841-05",
        "targetEntityName": "Hawala Node Alpha",
        "relationshipType": "DIRECT_TRANSACTION",
        "confidence": 0.98,
        "isVerified": true
      }
    ],
    "timelineCitations": [],
    "locationCitations": [],
    "alertCitations": [],
    "modelSignals": [
      {
        "signalId": "gat-841-01",
        "modelName": "GAT-LinkPrediction-v2",
        "predictionType": "POTENTIAL_LINK",
        "sourceEntityId": "ent-841-01",
        "targetEntityId": "ent-841-09",
        "predictedRelationship": "CO_CONSPIRATOR",
        "score": 0.78,
        "modelVersion": "2.1.0",
        "demarcation": "Model-predicted connection (Pending Review)"
      }
    ],
    "crossCaseCitations": [],
    "integrityWarnings": [],
    "insufficientEvidence": false,
    "suggestedFollowUps": [
      "What evidence confirms transactions between Rajesh Sharma and Hawala Node Alpha?",
      "Show timeline of activity for Rajesh Sharma"
    ],
    "disclaimer": "This answer is generated strictly from active case evidence. Presumption of innocence applies.",
    "generatedAtUtc": "2026-09-08T16:30:00Z"
  }
  ```
- **Error Responses:**
  - `400 Bad Request`: Validation error or invalid parameters.
  - `403 Forbidden`: Investigator not authorized to access requested `caseId`.
  - `404 Not Found`: Case does not exist.

#### `GET /api/v1/copilot/conversations/{conversationId}`
Retrieves active conversation history for the authenticated officer.
- **Response (200 OK):**
  ```json
  {
    "conversationId": "conv-case-841-01",
    "caseId": "inv-2026-0841",
    "userId": "usr-dcp-sharma",
    "messages": [
      {
        "role": "user",
        "content": "How is Rajesh Sharma connected to Hawala Node Alpha?",
        "timestampUtc": "2026-09-08T16:30:00Z"
      },
      {
        "role": "assistant",
        "content": "Rajesh Sharma is directly connected to Hawala Node Alpha...",
        "timestampUtc": "2026-09-08T16:30:02Z"
      }
    ],
    "createdAtUtc": "2026-09-08T16:30:00Z",
    "updatedAtUtc": "2026-09-08T16:30:02Z"
  }
  ```

#### `DELETE /api/v1/copilot/conversations/{conversationId}`
Purges the conversation session and associated context memory.
- **Response (204 No Content)**

#### `GET /api/v1/copilot/suggested-questions?caseId={caseId}`
Generates dynamic suggested questions tailored to the active case's verified entities, timeline events, and GAT model predictions.
- **Response (200 OK):**
  ```json
  [
    "Summarize all verified entities and high-risk nodes in this case",
    "What verified relationships exist for Rajesh Sharma?",
    "Show recent timeline events and CDR spikes",
    "Are there any cross-case linkages with other active investigations?",
    "Review model-predicted connections pending investigator verification"
  ]
  ```

---

### 11. Investigator Dashboard (`/api/v1/cases/{caseId}/dashboard`)

Consolidated operational view of an active investigation, synthesizing real data across PostgreSQL, Neo4j, timeline, maps, anomaly alerts, GAT model signals, and cryptographic integrity ledger.

#### `GET /api/v1/cases/{caseId}/dashboard`
Retrieves the consolidated operational command center intelligence payload.
- **Authorization**: Required (`Bearer <token>`). Case access authorization enforced.
- **Response (200 OK)**:
  ```json
  {
    "case": {
      "id": "inv-2026-0841",
      "caseNumber": "CASE-2026-0841-ORG",
      "title": "Operation Iron Vault Syndicate",
      "status": "Active",
      "priority": "Critical",
      "district": "Mumbai City",
      "leadOfficerName": "DCP Rajesh Sharma",
      "firNumber": "FIR-2026-MUM-0841",
      "createdAtUtc": "2026-08-15T10:00:00Z",
      "updatedAtUtc": "2026-09-08T14:30:00Z"
    },
    "summary": {
      "entityCount": 14,
      "relationshipCount": 18,
      "evidenceCount": 4,
      "alertCount": 3,
      "highRiskAlerts": 1,
      "crossCaseTotalCount": 2,
      "crossCaseConfirmedCount": 1,
      "crossCasePotentialCount": 1,
      "crossCaseModelCount": 0,
      "modelSignalCount": 2,
      "integrityVerifiedCount": 4,
      "integrityModifiedCount": 0,
      "integrityUnreconciledCount": 0
    },
    "network": {
      "nodeCount": 14,
      "relationshipCount": 18,
      "componentCount": 1,
      "topConnectedEntities": [
        {
          "entityId": "ent-001",
          "canonicalName": "Rajesh Sharma",
          "entityType": "Person",
          "degree": 8
        }
      ],
      "bridgeEntities": [
        {
          "entityId": "ent-002",
          "canonicalName": "Hawala Node Alpha",
          "entityType": "Organization",
          "betweennessScore": 0.42
        }
      ],
      "nodes": [],
      "edges": []
    },
    "signals": [
      {
        "id": "sig-001",
        "title": "Frequent Foreign SIM Card Handover",
        "type": "ALERT",
        "priority": "CRITICAL",
        "whyItMatters": "Statistical spike in burner device swaps across border tower sectors.",
        "evidenceCount": 3,
        "status": "PENDING_REVIEW",
        "modelScore": null,
        "modelName": null,
        "actionUrl": "/alerts"
      }
    ],
    "timeline": [
      {
        "eventId": "evt-001",
        "eventType": "CDR_SPIKE",
        "eventTimestampUtc": "2026-09-07T14:22:00Z",
        "precision": "DATETIME",
        "formattedTime": "2026-09-07 14:22 UTC",
        "description": "24 encrypted calls logged within 45 minutes",
        "location": "Bandra Kurla Complex",
        "evidenceIds": ["evd-001"]
      }
    ],
    "locations": [
      {
        "locationId": "loc-001",
        "name": "Bandra Kurla Complex, Mumbai",
        "latitude": 19.0657,
        "longitude": 72.8687,
        "hasCoordinates": true,
        "coordinateDisplay": "19.0657, 72.8687",
        "activityCount": 12
      }
    ],
    "alerts": {
      "bySeverity": { "critical": 1, "high": 2, "medium": 0, "low": 0 },
      "byStatus": { "newCount": 2, "underReviewCount": 1, "resolvedCount": 0 },
      "highPriorityAlerts": []
    },
    "crossCase": {
      "confirmedCount": 1,
      "potentialCount": 1,
      "modelPredictedCount": 0,
      "connections": []
    },
    "integrity": {
      "verifiedCount": 4,
      "modifiedCount": 0,
      "unreconciledCount": 0,
      "totalEvidenceCount": 4,
      "ledgerHealthStatus": "VALID",
      "warnings": []
    },
    "actions": [],
    "recentActivity": [],
    "dataQualityWarnings": [],
    "responsibleAiNotice": "Analytical and model-generated signals are investigative leads and require human verification. They do not establish guilt or wrongdoing.",
    "generatedAtUtc": "2026-09-08T18:00:00Z"
  }
  ```

---

### 12. Translation (`/api/v1/translation`)

Non-destructive translation service for evidence records and investigative text. Original evidence files and SHA-256 fingerprints remain immutable.

#### `POST /api/v1/translation/translate`
Translates investigative text between English and Marathi (मराठी), preserving invariant identifiers (case IDs, evidence hashes, phone numbers, registration numbers).
- **Request Body**:
  ```json
  {
    "text": "The suspect met near Gateway of India to hand over bag containing SIM cards.",
    "sourceLanguage": "en",
    "targetLanguage": "mr",
    "evidenceId": "evd-00042"
  }
  ```
- **Response (200 OK)**:
  ```json
  {
    "originalText": "The suspect met near Gateway of India to hand over bag containing SIM cards.",
    "translatedText": "संशयित व्यक्ती Gateway of India जवळ भेटली आणि सिम कार्ड असलेली बॅग हस्तांतरित केली.",
    "detectedSourceLanguage": "en",
    "targetLanguage": "mr",
    "provider": "SystemDictionary",
    "isMachineTranslation": true,
    "disclaimer": "Machine translation is provided for investigative reference only and is not authoritative evidence. Original evidence remains unchanged.",
    "translatedAtUtc": "2026-09-08T18:05:00Z"
  }
  ```

#### `GET /api/v1/translation/provider-status`
Returns configured translation provider status and availability.
- **Response (200 OK)**:
  ```json
  {
    "configuredProvider": "SystemDictionary",
    "isGoogleCloudAvailable": false,
    "fallbackAvailable": true,
    "supportedLanguages": ["en", "mr"]
  }
  ```

