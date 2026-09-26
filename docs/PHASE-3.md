# Phase 3 — Knowledge Graph + Investigation Graph UI Documentation

**Platform:** Criminal Intelligence & Network Investigation Platform (Maharashtra Police - SIH)  
**Status:** IMPLEMENTED & VERIFIED  
**Phase:** 3

---

## 1. Executive Summary

Phase 3 establishes an authoritative, interactive Knowledge Graph and Investigation Graph UI powered by Neo4j and PostgreSQL. Building upon Phase 2's human-in-the-loop extraction engine, Phase 3 strictly enforces the foundational governance rule:
> **Only human-verified, approved intelligence appears in the investigation graph.**

Every node and edge in the graph is case-scoped, traceable to forensic evidence via cryptographic SHA-256 digests, and supports multi-evidence corroboration without creating redundant duplicate edges.

```
PostgreSQL (System of Record: Cases, Evidence, Entities, Relationships, Provenance)
      │
      ▼
Neo4j Graph Database (Graph Query / Multi-Hop / Path / Centrality Engine)
      │
      ▼
ASP.NET Core 8 Web API (/api/v1/cases/{id}/graph, /api/v1/graph/*, /api/v1/entities/*)
      │
      ▼
React 18 + Cytoscape.js Frontend (Case-Scoped Interactive Intelligence Graph Workbench)
```

---

## 2. Canonical Graph Model

### 2.1 Canonical Node Labels & Categories
The platform supports 10 distinct, color-coded canonical entity categories designed for law enforcement clarity:

| Category | UI Color | Description / Examples |
|---|---|---|
| `PERSON` | Blue (`#388bfd`) | Suspects, handlers, couriers, operatives (e.g., *Rahul Mehta*, *Vikram Singhania*). |
| `PHONE` | Gold (`#d29922`) | Cellular numbers, burner SIM cards, VOIP terminals (e.g., `+91-9000001001`). |
| `VEHICLE` | Orange (`#db6d28`) | Registration license plates (e.g., `MH12AB4321`). |
| `LOCATION` | Green (`#2ea043`) | Cities, checkpoints, ports, safehouses (e.g., *Pune Central*, *JNPT Port Mumbai*). |
| `ORGANIZATION` | Purple (`#8250df`) | Syndicates, front corporations, hawala firms (e.g., *Apex Trading Syndicate*). |
| `ACCOUNT` | Coral/Red (`#cf222e`) | Bank accounts, UPI IDs, hawala ledger entries (e.g., `ACC-9021-PUNE-01`). |
| `DEVICE` | Teal (`#1f883d`) | IMEI numbers, MAC addresses, hardware tokens. |
| `EVENT` | Amber (`#bb8009`) | Chronological crime incidents, seizures, handovers. |
| `DOCUMENT` | Slate (`#6e7781`) | Formal FIRs, manifests, search warrants. |
| `CASE` | Deep Cyan (`#0550ae`) | Parent investigative case containers (e.g., `CASE-2026-001`). |

### 2.2 Canonical Node Properties
Every node contains:
- `id`: Stable canonical identifier (e.g., `can-ent-pune-001`).
- `name`: Human-readable display label.
- `normalized_name`: Deterministically normalized value for entity resolution.
- `type`: Canonical category.
- `case_id`: Case scoping identifier ensuring data isolation.
- `risk`: Risk indicator (`LOW`, `MEDIUM`, `HIGH`, `CRITICAL`).
- `verified`: Boolean indicating human approval.

---

## 3. Relationship Model & Multi-Evidence Provenance

### 3.1 Canonical Relationship Types
1. `COMMUNICATED_WITH`: Cellular calls, SMS threads, messaging sessions.
2. `OPERATES`: Control of vehicles, mobile units, or accounts.
3. `LOCATED_AT`: Presence at intercepted locations, checkpoints, safehouses.
4. `ASSOCIATE_OF`: Handler-operative hierarchy or co-suspect association.
5. `MEMBER_OF`: Gang or syndicate organizational membership.
6. `TRANSFERRED_MONEY_TO`: Wire transfers, structured cash deposits, hawala routing.
7. `TRAVELLED_TO`: Journey between cities or checkpoints.
8. `OWNED_BY`: Asset or vehicle ownership.
9. `TRAFFICKED`: Narcotics or firearms smuggling.
10. `REPORTED_BY`: Informant or intelligence source attribution.
11. `INVOLVED_IN`: Direct involvement in an incident.
12. `PARENT_CASE`: Cross-case or master investigation link.

### 3.2 Multi-Evidence Accumulation
When multiple pieces of evidence corroborate the same relationship (e.g., CDR call logs and physical surveillance both confirm Rahul Mehta used phone `+91-9000001001`):
- **Graph Edge Immutability:** A single graph edge connects the two nodes.
- **Relational Provenance Table (`RelationshipEvidences`):** Each supporting evidence file is linked via a record tracking:
  - `EvidenceId`: References the immutable evidence file.
  - `SourceLocation`: Exact page, line, or row citation.
  - `Confidence`: Confidence score from that source.
  - `VerifiedBy`: Approving investigator name/ID.
  - `VerifiedAtUtc`: Timestamp of review.
- **Neo4j Edge Property:** Stores accumulated evidence identifiers (`evidence_ids` array) and maximum confidence score.

---

## 4. Case-Scoped Graph APIs

| HTTP Method | Route | Description |
|---|---|---|
| `GET` | `/api/v1/cases/{caseId}/graph` | Retrieves authorized, case-scoped graph with optional `entityType`, `relationshipType`, `depth`, `search`, and `limit` filters. |
| `GET` | `/api/v1/entities/{id}/neighborhood` | Bounded multi-hop neighborhood expansion (`depth=1..3`) around a central node. |
| `GET` | `/api/v1/entities/{id}/relationships` | Direct relationships connected to the specified entity. |
| `GET` | `/api/v1/graph/search` | Backend search querying Neo4j and PostgreSQL for suspects, phones, plates, or locations. |
| `GET` | `/api/v1/graph/path` | Shortest investigative path finder between two entities (`maxHops=1..6`). |
| `GET` | `/api/v1/graph/statistics` | Real structural metrics, connected components, and degree centrality analysis. |
| `GET` | `/api/v1/graph/relationship/{id}` | Detailed relationship metadata, citations, and list of supporting evidence files. |
| `GET` | `/api/v1/graph` | Global graph view for authorized administrators. |
| `POST` | `/api/v1/graph/probe` | Neo4j lifecycle verification probe (`CREATE` -> `READ` -> `DELETE`). |

---

## 5. Interactive Frontend Architecture

### 5.1 Cytoscape Canvas & Controls
- High-performance, hardware-accelerated force-directed layout (`cose`).
- Color-coded node halos, directional bezier curve arrows, and edge labels.
- Canvas tools: Pan, Zoom In, Zoom Out, Fit Graph, and Reset Layout.

### 5.2 Node Selection Drawer
- Full entity metadata, normalized values, location, and risk level.
- Live counters: Total connections and associated evidence count.
- **Neighborhood Expansion:** One-click `+1 Hop`, `+2 Hop`, and `+3 Hop` buttons dynamically merging new nodes and edges from the backend into the canvas without reloading.
- Interactive list of all connected relationships.

### 5.3 Edge Selection Drawer & Evidence Navigation
- Displays relationship type, source entity, target entity, and confidence meter.
- Displays all supporting evidence records with exact citation quotes.
- **`[OPEN EVIDENCE]` Button:** Navigates directly to `/evidence/{evidenceId}` where the investigator can review the original file bytes, inspect metadata, and execute live SHA-256 verification (`VALID` / `TAMPERED`).

### 5.4 Shortest Path Tool (`[Find Path]`)
- Allows investigators to select Origin (A) and Destination (B) entities and set maximum hops.
- Highlights the shortest path in bright accent color on the canvas, dims unrelated nodes, and displays the step-by-step hops sequence in the drawer.

### 5.5 Graph Analytics & Centrality Modal
- Calculates real metrics from the backend:
  - Total Nodes, Total Edges, Network Clusters count.
  - Entity category distribution.
  - Relationship type distribution.
  - Connected Components (Association Clusters).
  - Degree Centrality ranking table using strictly neutral investigative terminology (*"High Connectivity Lead"*, *"Network Association"*, *"Connected Entity"*).

---

## 6. Security, Authorization & Audit Logging

- **Role-Based Access Control (RBAC):** Access requires `INVESTIGATOR`, `ANALYST`, or `ADMIN` roles.
- **Case Scoping:** Non-admin officers are restricted to cases within their jurisdiction.
- **Parametric Cypher:** Zero string concatenation in Cypher queries prevents Cypher injection vulnerabilities.
- **Tamper-Evident Audit Logging:** All graph views, entity neighborhood expansions, searches, and path queries are recorded in `AuditLogs`.

---

## 7. Verification & Test Evidence

### 7.1 Backend Automated Tests (45 Tests Passed)
- **Phase 3 Suite (`Phase3InvestigationGraphTests.cs`):** 17 automated tests covering all required scenarios:
  1. Case graph retrieval
  2. Empty graph handling
  3. Node property mapping
  4. Relationship mapping
  5. Case authorization enforcement
  6. Cross-case data isolation
  7. Neighborhood depth 1 expansion
  8. Neighborhood depth 2 expansion
  9. Maximum depth clamping (clamped to 3)
  10. Backend search (by name, phone, plate)
  11. Shortest path discovery
  12. Graph statistics computation
  13. Centrality ranking & neutral terminology
  14. Relationship provenance citation
  15. Evidence navigation routing
  16. Multiple evidence accumulation
  17. Zero mock graph dependency
- **Phase 2 Suite (`Phase2ExtractionAndReviewTests.cs`):** 10 tests passed.
- **Phase 1 Suite:** 18 tests passed.
- **Total:** **45 / 45 Passed (100%)**.

### 7.2 Python AI Engine Tests (11 Tests Passed)
- `backend/AI/tests/test_extraction.py`: 8 / 8 Passed.
- `ai-service/tests/test_api.py`: 3 / 3 Passed.

### 7.3 Frontend Validation
- `npm run typecheck`: **0 errors** (`tsc --noEmit`).
- `npm run build`: Production bundle built cleanly with Vite.
