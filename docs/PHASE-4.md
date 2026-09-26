# Phase 4 — Entity Resolution & Cross-Case Intelligence Subsystem

> **System Notice:** This documentation describes a synthetic investigation demonstration platform developed for research and prototype validation (SIH environment). The platform uses neutral investigative terminology and operates solely on synthetic test records.

---

## 1. Architectural Overview

Phase 4 extends the Criminal Intelligence & Network Investigation Platform with a deterministic, multi-signal **Entity Resolution and Cross-Case Intelligence** engine. It identifies potential correlations between entities recorded in disparate cases, provides explainable mathematical scoring breakdowns with evidence citations, enforces human investigator review gates, and synchronizes approved connections with the Neo4j cross-case graph.

```
+-----------------------------------------------------------------------------------+
|                              Investigator Workspace                               |
|                  (Entity Resolution Page / Comparison Drawer)                     |
+-----------------------------------------------------------------------------------+
                                         │
                 REST API (HTTP / JWT)   │  Investigator Approvals / Queries
                                         ▼
+-----------------------------------------------------------------------------------+
|                            ASP.NET Core 8 Web API                                 |
|                                                                                   |
|  [EntityResolutionController]        [EntitiesController]      [CasesController]  |
|          │                                    │                         │         |
|          ▼                                    ▼                         ▼         |
|  [EntityResolutionService] <──────────────────────────────────────────────────────+
|    ├── Blocking (Type Matching, Jurisdiction Filter)                              |
|    ├── Deterministic Signals (Exact Phone/Vehicle/Account, Jaro-Winkler, Levensh.)|
|    ├── False-Positive Safeguards (PERSON Name-Only Capped at <= 0.55)             |
|    ├── Explainable Signal Decomposition (Factor Weights & Citations)              |
|    └── Audit Trail Logging (MATCH_APPROVED, MATCH_REJECTED)                       |
+-----------------------------------------------------------------------------------+
       │                                     │                              │
       ▼                                     ▼                              ▼
+──────────────+                    +──────────────────+          +──────────────────+
|  PostgreSQL  |                    |      Neo4j       |          | Python AI Engine |
| (Relational  |                    |  (Graph Database |          | (FastAPI Helper  |
|  Persistence)|                    |   Cross-Case)    |          |  Async Matching) |
+──────────────+                    +──────────────────+          +──────────────────+
```

---

## 2. Multi-Signal Scoring Engine

The entity resolution pipeline uses a multi-factor scoring model. No match candidate is ever merged or auto-confirmed automatically; all candidates enter the `PENDING` state and require human review.

### Signal Factors & Weights

| Signal Type | Weight | Criteria | Evidence Citation Requirement |
|---|---|---|---|
| `SHARED_PHONE` | 0.45 | Identical normalized E.164 phone or 10-digit suffix | CSV call detail records, telecom intercepts |
| `SHARED_VEHICLE` | 0.35 | Identical normalized alphanumeric license plate | Surveillance log, transport manifest |
| `SHARED_ACCOUNT` | 0.35 | Identical bank account number and branch context | Financial ledger, bank statement |
| `NAME_SIMILARITY` | 0.30 | Token-set similarity + Jaro-Winkler prefix analysis | Case witness statements, FIR registries |
| `ALIAS_MATCH` | 0.25 | Documented alias matches target canonical name | Intelligence dossiers, informant notes |
| `CONTEXT_SIMILARITY`| 0.20 | Shared graph neighbors (common phone, address, org)| Subgraph traversal intersection |
| `CONTRADICTORY_ATTR`| -0.15 | Conflicting non-overlapping jurisdictions without shared ID | Jurisdiction logs |

### False-Positive Protection (Safeguard Rule)
When comparing two `PERSON` entities:
- If the **only** signal present is name similarity (no shared phone number, vehicle plate, financial account, alias match, or network context), the match confidence score is **strictly capped at $\le 0.55$**.
- This guarantees that two individuals who happen to share a common name (e.g., "Rahul Sharma") cannot be surfaced as high-confidence matches and cannot bypass human review.

---

## 3. Human Investigator Review Workflow

1. **Discovery:** Batch resolution runs automatically or on demand (`POST /api/v1/entity-resolution/run`).
2. **Candidate Queue:** Candidates are placed into the `EntityMatchCandidates` table with `MatchStatus = 'PENDING'`.
3. **Side-by-Side Review:** The investigator views full comparative intelligence via `GET /api/v1/entity-resolution/candidates/{id}/compare`:
   - Left side: Case A entity, phone, vehicle, account, and evidence excerpts.
   - Right side: Case B entity, phone, vehicle, account, and evidence excerpts.
   - Middle: Exact mathematical factor decomposition and individual signal weights.
4. **Decision:**
   - **Approve (`POST /candidates/{id}/approve`):** Sets status to `APPROVED`, creates a verified `CrossCaseConnection` with linked supporting evidence provenance, writes an immutable audit record (`MATCH_APPROVED`), and creates a cross-case link in Neo4j (`CreateCrossCaseLinkAsync`).
   - **Reject (`POST /candidates/{id}/reject`):** Sets status to `REJECTED`, records reviewer notes and reason, writes an audit record (`MATCH_REJECTED`), and leaves the graph unlinked.

---

## 4. Verification & Quality Gates

The test suite validates:
- Direct identifier matches (Phone, Vehicle, Account).
- Name similarity calculations (Jaro-Winkler, Levenshtein, Token-set).
- False-positive safeguard enforcement.
- Entity type blocking (PERSON vs VEHICLE blocked).
- Idempotent candidate generation.
- Human review state transitions (APPROVED, REJECTED).
- Audit event logging with IP address and reviewer identity.
- Preservation of chain of custody and multi-case evidence provenance.

**Test Results:**
- **.NET Test Suite:** 61/61 tests passed (`dotnet test backend/Tests/Tests.csproj`).
- **Python AI Service Suite:** 15/15 tests passed (`ai-service/tests` and `backend/AI/tests`).
- **Frontend Typecheck:** 0 errors (`npm run typecheck`).
- **Frontend Production Build:** Successful bundle generation (`npm run build`).
