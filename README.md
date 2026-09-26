# CIG — Criminal Intelligence Graph

**A synthetic research environment for evidence-grounded investigation, entity resolution, and knowledge-graph-based analysis.**

---

## Core Principle

**AI proposes. Evidence supports. Investigator decides.**

CIG does not determine guilt, assign criminality, or recommend arrests. All analytical outputs—entity correlations, link predictions, pattern analyses—are investigative leads requiring human investigator validation against evidence, legal procedure, and investigative practice. Model confidence scores communicate model output uncertainty; they do not establish factual certainty.

---

## Overview

CIG is a **research and educational prototype** demonstrating:

- **Evidence integrity:** SHA-256 fingerprinting + append-only audit ledger for chain of custody
- **Entity resolution:** Deterministic matching, fuzzy matching (Jaro-Winkler), and deduplication
- **Knowledge graphs:** Cytoscape visualization of entity relationships with confidence indicators
- **Cross-case analysis:** Pattern detection and relationship correlation across investigations
- **Investigator workflows:** Case management, evidence review, temporal reconstruction, geospatial analysis
- **Audit trails:** Comprehensive logging of investigator actions and data access
- **AI-assisted leads:** Grounded natural-language reasoning over evidence, graph relationships, and timeline events

**Synthetic data only.** CIG operates on synthetic/demo investigation data for research and demonstration purposes. No real investigative data, official law-enforcement records, or NCRB databases are included.

---

## Technology Stack

| Layer | Technology |
|-------|-----------|
| **Frontend** | React 18 + TypeScript + Vite (Port 3000) |
| **Backend API** | C# ASP.NET Core 8 (Port 5000) |
| **Relational Storage** | PostgreSQL 16 (Port 5432) |
| **Graph Database** | Neo4j 5.20 Community (Port 7687 Bolt / 7474 HTTP) |
| **AI Analytics** | Python 3.11+ + FastAPI + PyTorch (Port 8000) |

### Key Libraries & Frameworks

- **Graph visualization:** Cytoscape.js 3.30 (interactive network layout and rendering)
- **Geospatial mapping:** Leaflet + React Leaflet (coordinate visualization)
- **State management:** TanStack React Query (frontend server-state synchronization)
- **Internationalization:** i18next (English/Marathi dual-language UI)
- **Data access:** Entity Framework Core + Npgsql (ORM and PostgreSQL driver)
- **Authentication:** JWT Bearer tokens (stateless request signing)
- **Validation:** FluentValidation (server-side input and business-rule validation)
- **NLP/ML:** spaCy, PyTorch (entity extraction and link prediction)

---

## Project Structure

```
src/                        Frontend (React 18 + TypeScript)
├── features/               Dashboard, investigations, evidence, graph, map, 
                            copilot assistant, audit, reports, system status
├── components/             Shared UI components and layouts
├── services/               API client wrappers and service abstractions
├── hooks/                  useAuth, useActiveInvestigation state management
├── i18n/                   English/Marathi localization files
└── types/                  Shared TypeScript interfaces

backend/                    ASP.NET Core 8 + Entity Framework Core
├── API/                    HTTP controllers, middleware, configuration
├── Application/            Business logic services, data transfer objects (DTOs)
├── Domain/                 Domain entities, enums, exceptions, interfaces
├── Infrastructure/         Data persistence (EF Core/PostgreSQL, Neo4j driver),
│                          JWT security, file storage, background services
├── Worker/                 Async background job processing
└── Tests/                  Unit and integration test suites

ai-service/                 Python FastAPI analytics microservice
├── app/
│   ├── api/                HTTP endpoints (/health, /ner/extract, /link-prediction/predict)
│   ├── nlp/                Named entity recognition and extraction modules
│   ├── processors/         Document parsing and text extraction
│   ├── resolution/         Entity matching and deduplication logic
│   ├── graph_ml/           Graph-based link prediction heuristics
│   └── geo/                Geographic-analysis utilities
└── tests/                  Service test suite

docs/                       Architecture and implementation documentation
├── ARCHITECTURE.md         System design and component dependencies
├── RUNNING_LOCALLY.md      Local development environment setup
├── API.md                  REST endpoint reference
├── FINAL_PROJECT_STATUS.md Implementation status and phase verification
└── FINAL_REMAINING_WORK.md Production hardening roadmap
```

### Data Flow

```
┌─────────────────────────────────────────────────┐
│  React 18 / Vite Frontend (Port 3000)           │
│  (Investigator workspace, case, graph, map)     │
└──────────────────┬──────────────────────────────┘
                   │ REST API + JWT Bearer Token
                   ▼
┌─────────────────────────────────────────────────┐
│  ASP.NET Core 8 Web API (Port 5000)             │
│  (Controllers → Services → Repositories)        │
└──────┬──────────────────────┬──────────────────┘
       │                      │
       ▼                      ▼
   PostgreSQL 16        Neo4j 5.20 Community
   (relational store:   (graph topology and
    users, roles,       relationship traversal
    cases, evidence,    via Cypher queries)
    audit logs)         
       │                      │
       └──────────┬───────────┘
                  │ gRPC / HTTP for async tasks
                  ▼
         Python FastAPI (Port 8000)
         (Entity extraction, link prediction,
          copilot reasoning, text analysis)
```

---

## Evidence Integrity & Chain of Custody

CIG implements **cryptographic evidence fingerprinting** using SHA-256:

1. **File Upload:** Raw evidence file bytes are hashed using SHA-256
2. **Metadata Recording:** Hash, filename, upload timestamp, uploader identity, and case ID are recorded in PostgreSQL
3. **Integrity Ledger:** An append-only ledger block is created containing:
   - Block index, evidence ID, SHA-256 hash, timestamp, uploader ID, case ID, and hash of previous block
4. **Verification:** Investigators can verify evidence integrity via `/api/v1/audit/verify` at any time
5. **Tamper Detection:** Any modification to file or metadata produces a different hash, breaking the chain

**This is not blockchain.** It is a straightforward append-only ledger implemented using standard PostgreSQL indexes and transaction isolation. No distributed consensus, no mining, no external consensus mechanism—just sequential cryptographic integrity records in a single database. The approach is simple, debuggable, and sufficient for chain-of-custody verification in single-organization investigator workflows.

---

## What This Is NOT

❌ **Not a guilt/not-guilty determination system.** CIG does not assign criminal liability or make conclusions about culpability.

❌ **Not a replacement for investigator judgment.** All recommendations, predictions, and analytical leads require investigator review and decision.

❌ **Not real-time surveillance.** CIG is a case-review and investigation-management tool, not a live monitoring or interception system.

❌ **Not OSINT or social-media intelligence.** No public social-media data ingestion, phone-tower location data, cellular metadata, or external intelligence feeds.

❌ **Not a production law-enforcement deployment.** This is a research prototype intended for education, demonstration, and evaluation, not operational use by police agencies.

❌ **Not blockchain-based.** Evidence integrity uses append-only PostgreSQL records with SHA-256 fingerprinting, not distributed consensus or immutable ledger technologies.

---

## Running Locally

### Prerequisites

- **Docker & Docker Compose** (recommended for full stack)
- **OR:** .NET 8 SDK, Node.js 20+, Python 3.11+, PostgreSQL 16, Neo4j 5.20, Java 17

### Quick Start with Docker

```bash
git clone https://github.com/digvijaymahale83/CIG-Criminal-Intelligence-Graph.git
cd CIG-Criminal-Intelligence-Graph
docker compose up --build
```

Services will be available at:

```
Frontend:           http://localhost:3000
API Documentation:  http://localhost:5000/swagger
Neo4j Browser:      http://localhost:7474
AI Service Docs:    http://localhost:8000/docs
```

### Local Development Setup (No Docker)

**1. PostgreSQL (Port 5432)**

```bash
# Verify PostgreSQL is running
psql -U postgres -c "SELECT version();"

# Create database if needed
psql -U postgres -c "CREATE DATABASE criminal_network_db;"
```

**2. Neo4j (Port 7687 Bolt / 7474 HTTP)**

```bash
# Set Java 17 environment
$env:JAVA_HOME = "C:\path\to\jdk-17.0.12+7"

# Start Neo4j
cd neo4j-community-5.20.0
.\bin\neo4j.bat console
```

**3. Python AI Service (Port 8000)**

```bash
cd backend/AI
python -m venv .venv
.venv\Scripts\activate
pip install -r requirements.txt
python -m uvicorn app.main:app --host 0.0.0.0 --port 8000
```

**4. ASP.NET Core API (Port 5000)**

```bash
cd backend
dotnet build
$env:ASPNETCORE_URLS = "http://localhost:5000"
dotnet run --project API/API.csproj
```

**5. React Frontend (Port 3000)**

```bash
npm install
npm run dev
```

---

## Key Features

### Case & Investigation Management

- Create and manage investigations with scope, case type, and jurisdiction
- Real-time entity and evidence counts
- Role-based access control (LEAD_INVESTIGATOR, ANALYST, ADMIN)
- Multi-case navigation and filtering

### Evidence Processing & Chain of Custody

- Drag-and-drop or file-select evidence upload
- SHA-256 byte-level fingerprinting with PostgreSQL metadata storage
- Append-only integrity ledger for tamper detection
- Source-to-entity traceability and provenance tracking

### Entity Resolution & Relationship Discovery

- **Exact matching:** Deterministic matching on name, phone, vehicle ID
- **Fuzzy matching:** Jaro-Winkler string similarity for approximate name matches
- **Deduplication:** Candidate merging and alias management
- **Graph clustering:** Community-detection-based relationship inference

### Knowledge Graph Visualization

- **Cytoscape canvas** with force-directed and hierarchical layouts
- **Node types:** Person, Phone, Vehicle, Location, Organization, Account, Device, Document
- **Relationship types:** CALLED, USED, VISITED, OWNED, MET, WORKED_FOR, INVOLVED_IN, LOCATED_AT, MENTIONED_IN, ASSOCIATED_WITH
- **Confidence indicators:** Visual badges showing model-predicted relationship strength
- **Interaction:** Drag-to-pan, scroll-to-zoom, node/edge filtering

### Cross-Case Analysis

- Query entities across multiple investigations
- Pattern detection: modus operandi, suspect networks, syndicate identification
- Timeline correlation and temporal reconstruction
- Relationship traversal and shortest-path discovery

### Geospatial Mapping

- Leaflet-based crime mapping with coordinate visualization
- Suspect movement paths and heat maps
- District boundaries and jurisdiction pins
- Coordinate-based clustering and drill-down

### Temporal Timeline & Event Reconstruction

- Chronological event sequencing across entities
- Interactive filtering by date range, entity type, and event category
- Multi-suspect correlation and gap analysis
- Time-based evidence association

### AI-Assisted Copilot

- Natural-language query interface over:
  - PostgreSQL case facts and entity metadata
  - Neo4j graph relationships and multi-hop paths
  - Timeline events and temporal sequences
  - Link-prediction results and confidence scores
- **Evidence-grounded responses:** All responses cite sources from case data; no hallucinated facts
- **Dual-language support:** English and Marathi
- **Investigator verification required:** All suggestions presented as investigative leads

### Audit & Compliance

- Structured event logging: login, evidence upload, case access, graph queries, copilot interactions
- Capture: timestamp, investigator identity, IP, action type, affected entities, JSON payload diffs
- Tamper-detection verification and integrity chain validation
- CSV and PDF report export with audit trail

### Dual-Language UI

- Full English and Marathi localization via i18next
- Police terminology aligned to investigative context
- Consistent language across all 12 navigation modules

---

## Architecture & Design Decisions

### PostgreSQL + Neo4j (Why Both?)

- **PostgreSQL:** Authoritative relational store for users, roles, cases, evidence metadata, audit logs, and investigative facts
- **Neo4j:** Optimized for multi-hop graph traversals, pattern detection, and relationship analysis
- **Together:** Investigations require both structured data (relational) and network patterns (graph). Neither database alone is sufficient.

### ASP.NET Core + Python FastAPI (Why Two Backends?)

- **ASP.NET Core:** Unified, type-safe REST API with Entity Framework Core for data consistency, JWT security, and RBAC middleware
- **Python FastAPI:** Rapid NLP/ML prototyping (spaCy, PyTorch) without re-implementation in C#
- **Integration:** gRPC or HTTP coupling with async job processing via background services

### Cytoscape.js (Why Not D3.js or GraphQL?)

- Cytoscape is specifically designed for network and graph visualization
- Built-in layout algorithms (force-directed, hierarchical, circular) scale to thousands of nodes
- Interactive performance and node/edge filtering without manual optimization

### Append-Only Ledger (Why Not Blockchain?)

- **Simplicity:** Standard SQL queries, indexes, and transaction isolation
- **Debuggability:** No consensus overhead, deterministic behavior
- **Sufficient:** Chain-of-custody verification in single-organization investigator workflows does not require distributed consensus
- **Speed:** No mining or consensus delay; immediate verification

---

## Current Development Status

CIG is an actively developed research prototype. The core investigation workflow, evidence processing, knowledge graph, graph analysis, temporal/geospatial analysis, AI-assisted analytical signals, evidence integrity, access control, and investigator review workflows are implemented in the current prototype.

**Implemented:**
- Evidence upload and SHA-256 fingerprinting
- Entity resolution and deduplication
- Knowledge graph visualization and relationship discovery
- Cross-case entity correlation
- Temporal timeline and event reconstruction
- Geospatial mapping with coordinate analysis
- Investigator copilot with evidence-grounded reasoning
- Complete audit logging and investigator accountability
- Role-based access control
- Dual-language (English/Marathi) UI

**Remaining work** for production deployments includes:
- Production JWT key rotation and refresh-token management
- Distributed ledger locking for multi-instance ASP.NET Core clusters
- Encrypted evidence storage with WORM (Write-Once-Read-Many) immutability
- Offline geospatial tile caching for air-gapped environments
- GPU-accelerated model serving (ONNX Runtime / Triton)
- Formal end-to-end test automation (Playwright)

Refer to [docs/FINAL_PROJECT_STATUS.md](docs/FINAL_PROJECT_STATUS.md) for detailed implementation status and [docs/FINAL_REMAINING_WORK.md](docs/FINAL_REMAINING_WORK.md) for the production hardening roadmap.

---

## Ethical Use Principles

CIG is designed with investigator-in-the-loop accountability:

- ✅ **AI outputs are investigative leads, not conclusions.** All model predictions require investigator review and evidence validation.
- ✅ **Confidence scores communicate model uncertainty, not factual certainty.** High model confidence does not establish proof.
- ✅ **Predicted relationships require source evidence.** Relationship suggestions must be verified against underlying case evidence.
- ✅ **No automated investigation decisions.** All case actions (merging entities, creating relationships, accessing evidence) require explicit investigator authorization.
- ✅ **Complete audit trail.** All investigator actions are logged with timestamp, actor, action type, and affected data.
- ✅ **Role-based access control.** Data access is governed by investigator role and case assignment.
- ✅ **Transparent model limitations.** UI communicates model confidence, data recency, and reasoning chain.

**Investigators remain accountable for all decisions.**

---

## Testing

### Frontend

```bash
npm run test           # Vitest + React Testing Library
npm run typecheck      # TypeScript strict checking
npm run build          # Vite production build
```

### Backend

```bash
cd backend
dotnet test            # Unit and integration tests
```

### End-to-End

Integrated tests verify data flows from PostgreSQL/Neo4j through ASP.NET Core API to React frontend without mock data.

---

## Documentation

- **[Architecture](docs/ARCHITECTURE.md)** — System design, component dependencies, and data flow
- **[Running Locally](docs/RUNNING_LOCALLY.md)** — Step-by-step setup for all services
- **[API Reference](docs/API.md)** — REST endpoint documentation
- **[Implementation Status](docs/FINAL_PROJECT_STATUS.md)** — Detailed phase-by-phase verification
- **[Remaining Work](docs/FINAL_REMAINING_WORK.md)** — Production hardening roadmap

---

## Contributing

This is a research project. Contributions are welcome via pull requests or GitHub Issues. Please ensure all contributions:

- Maintain the "AI proposes, investigator decides" investigator-in-the-loop principle
- Include appropriate evidence/source citations in UI and logs
- Do not add OSINT, social-media intelligence, phone-tower data, surveillance, or real-world data sources
- Preserve audit logging and chain-of-custody semantics
- Include clear documentation of model confidence and limitations

---

## License

See LICENSE file for details.

---

## Support & Questions

For architecture, design, or implementation questions, please open a GitHub Issue in this repository. Detailed documentation is available in the `docs/` directory.

---

**Last Updated:** September 2026  
**Status:** Research prototype in active development
