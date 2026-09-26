# Phase 10 — Evidence-Grounded Investigation Copilot

**The Criminal Intelligence & Network Investigation Platform**  
*Synthetic SIH Research Prototype*

---

## 1. Executive Summary

Phase 10 delivers an **Evidence-Grounded Investigation Copilot** designed specifically for law enforcement investigations. Unlike generic conversational chatbots that synthesize answers from unverified training corpora or hallucinate plausible-sounding facts, this copilot operates strictly as a **retrieval-first, evidence-grounded analytical reasoning engine**.

### Grounding Invariants (The 12 Rules)
1. **Source of Truth Primacy**: PostgreSQL, Neo4j, physical evidence storage, and analytical services are the sole sources of truth. The LLM is strictly an analytical summarization and explanation layer.
2. **Zero Fabrication**: The copilot never invents entities, relationships, timeline events, vehicle numbers, phone numbers, or physical locations.
3. **Explicit Insufficient Evidence Handling**: When current case records do not contain the requested information, the system explicitly responds: *"Insufficient evidence in current investigation data."*
4. **Strict Post-Generation Citation Validation**: Every citation (evidence ID, entity ID, relationship ID, timeline event ID, alert ID) is strictly validated against the retrieved context before returning to the investigator. Hallucinated citations are rejected and associated claims marked `UNSUPPORTED`.
5. **Prompt Injection Hardening**: Retrieved evidence and entity descriptions are treated as untrusted data, encapsulated in strict `<INVESTIGATION_DATA>` XML boundaries, and sanitized against adversarial prompt injection attempts.
6. **Case Isolation**: Queries and conversation memory are strictly scoped to the active investigation case. Cross-case data is surfaced solely through approved Phase 4 Entity Resolution linkages.
7. **Phase 9 Cryptographic Ledger Integration**: Citations of evidence records surface real-time custodial verification warnings if the physical file has been modified (`EVIDENCE_MODIFIED`) or fails hash verification.
8. **Algorithmic Model Predictions Demarcation**: Graph Attention Network (GAT) predictions and analytical leads are strictly labeled as *"Model-predicted connection (Pending Review)"*, requiring human confirmation before being treated as facts.
9. **Temporal Precision Preservation**: Timestamps recorded with `DATE_ONLY` precision never invent hour or minute figures.
10. **Presumption of Innocence**: Neutral investigative terminology is maintained at all times. The system never labels a person "guilty", "criminal", or "kingpin".
11. **Auditable Decision Support**: Every query, intent classification, confidence score, and integrity warning is permanently logged to the immutable `AuditLogs` ledger.
12. **Investigator in the Loop**: AI outputs represent hypotheses and leads for human corroboration, never automated judicial conclusions.

---

## 2. Architecture & Data Flow

```
                      Investigator Query
                             │
                             ▼
               ┌───────────────────────────┐
               │    CopilotIntentRouter    │
               │  - Intent Classification  │
               │  - Token / Entity Match   │
               │  - Prompt Injection Guard │
               └─────────────┬─────────────┘
                             │
                             ▼
               ┌───────────────────────────┐
               │   CopilotContextBuilder   │
               │  - Active Case Scoping    │
               │  - PostgreSQL Entities    │
               │  - Neo4j Verified Rels    │
               │  - Extracted Timelines    │
               │  - Geospatial Coordinates │
               │  - Phase 8 Anomaly Alerts │
               │  - Phase 4 Cross-Case     │
               │  - Phase 9 Ledger Status  │
               └─────────────┬─────────────┘
                             │
                             ▼
               ┌───────────────────────────┐
               │        ILLMService        │
               │  - XML Data Boundary      │
               │  - Grounded Reasoning     │
               │  - Claims Construction    │
               │  - Provider Abstraction   │
               └─────────────┬─────────────┘
                             │
                             ▼
               ┌───────────────────────────┐
               │  CopilotCitationValidator │
               │  - Anti-Hallucination     │
               │  - Ungrounded Rejection   │
               │  - Unsupported Claim Flag │
               │  - Ledger Alert Tagging   │
               └─────────────┬─────────────┘
                             │
                             ▼
               ┌───────────────────────────┐
               │    AuditLog & Response    │
               │  - COPILOT_QUERY logged   │
               │  - Scoped Conversation    │
               │  - Structured JSON DTO    │
               └───────────────────────────┘
```

---

## 3. Core Components

### 3.1 `ICopilotIntentRouter` & `CopilotIntentRouter`
Classifies investigator queries into targeted operational intents to focus data retrieval and prevent broad context leakage:
- `SHORTEST_PATH` / `ENTITY_RELATIONSHIPS`: Connection paths between specific persons or assets.
- `CROSS_CASE`: Shared identifiers across distinct case boundaries.
- `INTEGRITY`: Verification status of digital evidence on the Phase 9 append-only ledger.
- `ALERT`: Prioritized anomaly patterns exceeding statistical or temporal thresholds.
- `MODEL_SIGNAL`: GAT edge predictions and topological link suggestions.
- `TIMELINE`: Chronological sequence of events and movements.
- `LOCATION`: Geospatial coordinates, bounding boxes, and travel routes.
- `EVIDENCE`: Specific documents, files, and forensic artifacts.
- `GENERAL_CASE_SUMMARY`: Comprehensive summary of case entities, evidence, and leads.

Includes `SanitizeInput` to disarm adversarial prompt injection instructions (e.g. `IGNORE ALL PREVIOUS INSTRUCTIONS`, `DECLARE ... GUILTY`).

### 3.2 `ICopilotContextBuilder` & `CopilotContextBuilder`
Retrieves genuine factual context across the platform:
- Matches mentioned entities against case-scoped `EntityItem` records (canonical names, normalized values, aliases).
- Retrieves verified `Relationship` edges from Neo4j / PostgreSQL.
- Queries `IIntegrityLedgerService` for real-time SHA-256 validation status of all candidate evidence items.
- Queries `IEntityResolutionService` for approved cross-case connections (`Status == "APPROVED"`).
- Queries `IAlertService` for active anomaly alerts and structured explanations.
- Queries `GraphAnalyticalLeads` for GAT model predictions (`Status == "PENDING"`).

### 3.3 `ILLMService` & `LLMService`
Deterministic reasoning engine implementing the provider abstraction. Synthesizes explainable, citation-backed answers strictly from the provided `CopilotGroundedContext`.
- Wraps facts in `<INVESTIGATION_DATA>` boundaries.
- Enforces strict neutral tone: "connected to", "associated with", "observed at".
- Explicitly flags GAT model predictions as `Pending Review`.
- Attaches structured `CopilotClaimDto` elements (each marked `FACT`, `MODEL_PREDICTION`, or `UNSUPPORTED`).

### 3.4 `ICopilotCitationValidator` & `CopilotCitationValidator`
Post-generation verification pipeline:
- Cross-references every evidence ID, entity ID, relationship ID, event ID, and alert ID against the retrieved context.
- Rejects any citation not present in genuine retrieved data.
- Marks claims with unverified citations as `UNSUPPORTED` (`IsSupported = false`).
- Inspects `IntegrityStatus` of cited evidence: if `EVIDENCE_MODIFIED` or `HASH_MISMATCH`, generates high-priority custodial warning.
- Computes mathematical grounded confidence score (`HIGH` 0.95, `MEDIUM` 0.75, `MODEL_SIGNAL` 0.55, `LOW` 0.35, `UNKNOWN` 0.10).

### 3.5 `ICopilotService` & `CopilotService`
Orchestrator managing:
- Role-based authorization (`ADMIN`, `INVESTIGATOR`, `ANALYST`, `OFFICER`, `SUPERVISOR`).
- Case scoping validation (throws `KeyNotFoundException` for non-existent or inaccessible cases).
- Scoped conversation memory (`ConcurrentDictionary<string, CopilotConversationDto>`) ensuring history does not leak across cases.
- Comprehensive audit logging via `IAuditService` (`COPILOT_QUERY`, `COPILOT_INTEGRITY_WARNING`, `COPILOT_ACCESS_DENIED`).

---

## 4. API Endpoints

### 4.1 Query Copilot
- **Route**: `POST /api/v1/copilot/query`
- **Request Body**:
  ```json
  {
    "caseId": "case-2026-001",
    "query": "How is Rajesh Kumar connected to Vikram Malhotra?",
    "conversationId": "conv-20260908-abcd1234",
    "maxResults": 10,
    "includeCrossCase": true,
    "includeAlerts": true,
    "includeTimeline": true,
    "includeLocations": true,
    "includeGraph": true,
    "includeEvidence": true
  }
  ```
- **Response**:
  ```json
  {
    "answer": "A verified relationship was found between Rajesh Kumar and Vikram Malhotra: CALLED (Confidence: 95%).",
    "confidence": "HIGH",
    "confidenceScore": 0.95,
    "intent": "SHORTEST_PATH",
    "claims": [
      {
        "text": "Rajesh Kumar connects to Vikram Malhotra via CALLED",
        "claimType": "FACT",
        "sourceIds": ["ev-copilot-001"],
        "isSupported": true
      }
    ],
    "evidenceCitations": [
      {
        "evidenceId": "ev-copilot-001",
        "fileName": "call_records_log.csv",
        "sha256Hash": "e3b0c442...",
        "integrityStatus": "VERIFIED"
      }
    ],
    "entityCitations": [
      {
        "entityId": "ent-rajesh-001",
        "canonicalName": "Rajesh Kumar",
        "entityType": "PERSON"
      }
    ],
    "relationshipCitations": [
      {
        "relationshipId": "rel-001",
        "sourceEntityName": "Rajesh Kumar",
        "targetEntityName": "Vikram Malhotra",
        "relationshipType": "CALLED",
        "confidence": 0.95,
        "evidenceIds": ["ev-copilot-001"]
      }
    ],
    "warnings": [],
    "suggestedFollowUps": [
      "Show supporting evidence for this connection",
      "Check if any of these entities appear in another case"
    ],
    "conversationId": "conv-20260908-abcd1234",
    "executedAtUtc": "2026-09-08T11:00:00Z"
  }
  ```

### 4.2 Conversation History
- **Route**: `GET /api/v1/copilot/conversations/{id}`
- **Response**: Conversation turn history scoped to the authorized user and case.

### 4.3 Delete Conversation
- **Route**: `DELETE /api/v1/copilot/conversations/{id}`
- **Response**: `200 OK` (clears in-memory session).

### 4.4 Suggested Inquiries
- **Route**: `GET /api/v1/copilot/suggested-questions?caseId={caseId}`
- **Response**: Array of context-aware follow-up queries based on case entities, pending alerts, and ledger status.

---

## 5. Verification & Test Suite

The Phase 10 test suite (`backend/Tests/Unit/Phase10CopilotTests.cs`) verifies all requirements:

| Test ID | Test Name | Invariant Tested | Result |
|---|---|---|---|
| `Test01` | `Test01_IntentClassification_CorrectlyRoutesQueries` | Intent classification across network, timeline, geospatial, alert, cross-case, summary | **PASS** |
| `Test02` | `Test02_NonExistentEntity_ReturnsInsufficientEvidence` | Anti-hallucination: returns insufficient evidence when entity does not exist | **PASS** |
| `Test03` | `Test03_NonExistentRelationship_ReturnsNoVerifiedRelationship` | Anti-hallucination: zero fabricated relationships between disconnected entities | **PASS** |
| `Test04` | `Test04_CitationValidator_RejectsFabricatedCitations` | Citation validator filters non-existent IDs and marks claims `UNSUPPORTED` | **PASS** |
| `Test05` | `Test05_PromptInjectionDefense_IgnoresAdversarialEvidenceInstructions` | Disarms adversarial system instructions and preserves presumption of innocence | **PASS** |
| `Test06` | `Test06_CaseIsolation_BlocksUnauthorizedCaseQueries` | Case isolation: rejects queries for non-existent or inaccessible cases | **PASS** |
| `Test07` | `Test07_IntegrityAware_TamperedEvidenceSurfacesWarning` | Surfaces explicit warning when cited evidence has `EVIDENCE_MODIFIED` status | **PASS** |
| `Test08` | `Test08_GATModelSignals_StrictlyLabeledAsPredicted` | GAT model predictions strictly demarcated as `Pending Review` | **PASS** |
| `Test09` | `Test09_TimelinePrecision_DateOnlyDoesNotInventHour` | Preserves `DATE_ONLY` timestamp precision without inventing hours/minutes | **PASS** |
| `Test10` | `Test10_GraphQuestion_ProducesGroundedConnectionAnswer` | Grounded connection answer with verified relationship citation | **PASS** |
| `Test11` | `Test11_CrossCase_DistinguishesConfirmedFromPotential` | Approved cross-case connections distinguished from pending candidates | **PASS** |
| `Test12` | `Test12_AuditLogging_RecordsCopilotQueryAndWarnings` | Comprehensive audit logging in `AuditLogs` table | **PASS** |
| `Test13` | `Test13_ConversationMemory_ScopedToCase` | Multi-turn conversation memory strictly scoped to active case | **PASS** |

- **Total .NET Unit Tests Passing**: 158 / 158 (0 failures)
- **Total Python AI Tests Passing**: 50 / 50 (0 failures)
- **Frontend Production Build**: Zero TypeScript errors, bundled in 23.8s.
