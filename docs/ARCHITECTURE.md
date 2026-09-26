# Criminal Intelligence & Network Investigation Platform — Architecture Specification

## 1. Executive Summary

This document defines the architectural transition of the Criminal Intelligence & Network Investigation Platform from a fragmented prototype to an authoritative, scalable, evidence-backed decision support system.

**Target Scope:** Phase 1 establishes the unified backend architecture across ASP.NET Core 8, PostgreSQL, Neo4j, Python FastAPI AI analytics, real byte-level SHA-256 evidence hashing, and audit logging.

---

## 2. Current vs. Target Architecture

### 2.1 Current Architecture (Legacy Prototype)

```
[ React 19 Frontend (Vite, Port 3000) ]
        |
        +---> Node.js / Express (Port 4000) [Legacy Backend]
        |        |
        |        +---> SQLite (cortex_mhp.db)
        |        +---> Local uploads/ folder
        |        +---> Direct Gemini API (demo wrapper)
        |
        +---> ASP.NET Core 8 Web API (Port 5000) [Scaffold Only]
                 |
                 +---> SystemStatus endpoint (hardcoded simulations)
                 +---> Disconnected EF Core DbContext
                 +---> Unused Neo4j driver factory
```

**Identified Architectural Deficiencies:**
1. **Split Backend**: Frontend calls partially routed to Node.js/Express (Port 4000) and partially to ASP.NET Core (Port 5000).
2. **Relational Graph Simulation**: The Knowledge Graph was querying relational SQLite tables (`graph_nodes`, `graph_edges`). Real graph traversals and Cypher queries on Neo4j were not executed.
3. **Simulated Diagnostics**: `SystemHealthService.cs` contained hardcoded `simulationMode == "ready"` branches returning fake green statuses for PostgreSQL, Neo4j, Redis, and RabbitMQ.
4. **Mocked Case Creation**: The case creation UI in `NewInvestigationPage.tsx` generated client-side IDs without calling persistence endpoints.
5. **Security Vulnerabilities**: Hardcoded JWT secrets (`mhp-cortex-secret-2024-secure`) and API keys in committed configuration files.
6. **Incomplete Evidence Integrity**: SQLite stored SHA-256 strings in a mutable table without an immutable audit trail or provenance records.

---

### 2.2 Target Architecture (Phase 1 Frozen Standard)

```
                 +-------------------------------------------------+
                 |       React 19 + TypeScript (Port 3000)         |
                 |      (Gov/Police UI Design Preserved)           |
                 +-------------------------------------------------+
                                          |
                                          | REST API (JWT Bearer)
                                          v
                 +-------------------------------------------------+
                 |         ASP.NET Core 8 Web API (Port 5000)       |
                 |                                                 |
                 |  Controllers -> Services -> Repositories/Context|
                 +-------------------------------------------------+
                         |                |               |
         +---------------+                |               +---------------+
         |                                |                               |
         v                                v                               v
+------------------+             +------------------+            +------------------+
| PostgreSQL 16    |             | Neo4j 5 Graph DB |            | Local Storage    |
| (Authoritative   |             | (Topology &      |            | (SHA-256 Byte    |
| Relational Store)|             | Traversals)      |            | Hashed Evidence) |
+------------------+             +------------------+            +------------------+
         |                                                                |
         +----------------------------------------------------------------+
                                          |
                                          v
                         +---------------------------------+
                         |  Python FastAPI AI Analytics    |
                         |  (Port 8000 Skeleton Contract)  |
                         +---------------------------------+
```

---

## 3. Layered Design Principles (ASP.NET Core 8)

The backend strictly enforces clean separation of concerns:

```
Controllers (API layer)
    ↓ [DTOs / Commands / Queries]
Services (Application business logic)
    ↓ [Domain Entities / Interfaces]
Repositories / Context (Infrastructure data access)
    ↓ [SQL / Cypher / File I/O]
Database / Storage
```

### Constraints:
- Controllers contain **zero** business logic; they handle validation, routing, HTTP status codes, and authorization.
- Application layer has no reference to ASP.NET Core HTTP abstractions or Entity Framework directly (relies on `IAppDbContext`).
- Domain layer has zero third-party framework dependencies.
- Every external IO (PostgreSQL, Neo4j, File System, HTTP to AI Service) is asynchronous (`async`/`await`) with `CancellationToken` support.

---

## 4. Component Inventory & Transition Status

| Component | Current State | Phase 1 Target State | Disposition |
| :--- | :--- | :--- | :--- |
| **Frontend UI/UX** | React 19 + Vite | Retained as-is with bug fixes & real API connections | **RETAINED** |
| **Authentication** | Node.js + bcrypt + SQLite | ASP.NET Core JWT + PBKDF2 Hashing + Role authorization | **MIGRATED** |
| **Case Management** | Node.js + SQLite | ASP.NET Core `/api/v1/cases` CRUD + PostgreSQL EF Core | **MIGRATED** |
| **Evidence Storage** | Node.js Multer + disk | ASP.NET Core `/api/v1/evidence` + disk abstraction + byte SHA-256 | **MIGRATED** |
| **Evidence Hashing** | Node `crypto` | ASP.NET Core `IHashService` streaming real byte-level SHA-256 | **MIGRATED** |
| **Audit Logging** | Node.js + SQLite table | ASP.NET Core `/api/v1/audit` + PostgreSQL `audit_logs` | **MIGRATED** |
| **Graph DB** | SQLite relational tables | Neo4j Bolt driver + Parameterized Cypher queries + Lifecycle Probe | **MIGRATED** |
| **Health Checks** | Node.js + Simulated ASP.NET | ASP.NET Core `/api/v1/system/health` live multi-service probe | **MIGRATED** |
| **AI Analytics** | Static script | Python FastAPI (`ai-service/app`) with `/health` and structured contract | **ESTABLISHED** |
| **Node.js Express** | Port 4000 Server | Maintained temporarily as fallback until all flows verified | **DEPRECATED** |
| **SQLite DB** | `server/cortex_mhp.db` | Replaced by PostgreSQL 16 schema | **DEPRECATED** |

---

## 5. Migration Plan

### Step 1: Backend Foundation Unification
1. Establish .NET 8 SDK on the host and resolve project references (`Domain.csproj`).
2. Implement Domain Entities, Enums, Exceptions, and Value Objects.
3. Build Application Services and Infrastructure implementations.
4. Implement REST Controllers for Auth, Cases, Evidence, Entities, Graph, Audit, and System Health.

### Step 2: Relational Data Persistence (PostgreSQL)
1. Configure EF Core with PostgreSQL provider (`Npgsql.EntityFrameworkCore.PostgreSQL`).
2. Map normalized entities (`User`, `Role`, `Case`, `Evidence`, `AuditLog`, `EntityItem`, `Relationship`).
3. Seed authentic synthetic Maharashtra Police investigation data into PostgreSQL.

### Step 3: Graph Integration (Neo4j)
1. Implement `INeo4jService` utilizing the Neo4j Bolt driver.
2. Establish canonical graph schema:
   - **Nodes:** `(:Person)`, `(:Phone)`, `(:Vehicle)`, `(:Location)`, `(:Organization)`, `(:Account)`, `(:Device)`, `(:Document)`, `(:Event)`, `(:Case)`.
   - **Relationships:** `[:CALLED]`, `[:USED]`, `[:VISITED]`, `[:OWNED]`, `[:MET]`, `[:WORKED_FOR]`, `[:INVOLVED_IN]`, `[:LOCATED_AT]`, `[:MENTIONED_IN]`, `[:ASSOCIATED_WITH]`.
   - **Properties:** `source_evidence_id`, `confidence`, `created_at`, `valid_from`, `valid_to`.
3. Provide automated lifecycle probe (`create test node` -> `read test node` -> `delete test node`).

### Step 4: Evidence Storage & Provenance
1. Create `IFileStorageService` for controlled, sandboxed disk storage.
2. Implement `IHashService` generating lowercase hexadecimal SHA-256 strings from raw stream bytes.
3. Hook upload operations into `audit_logs` to maintain chain of custody.

### Step 5: Python AI Service Foundation
1. Build modular FastAPI package in `ai-service/app/`.
2. Provide endpoints: `GET /health`, `GET /ready`, `POST /api/v1/process/text`.
3. Clearly mark future modules (OCR, NER, Resolution, GAT) as unimplemented skeletons without fake mock outputs.

### Step 6: Frontend Cutover & Quality Gate
1. Resolve TypeScript compilation errors (`useActiveInvestigation.tsx`).
2. Switch API client to point to ASP.NET Core (`http://localhost:5000`).
3. Validate end-to-end acceptance flow.
4. Mark Node.js server as deprecated.
