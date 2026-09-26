# Phase 11 — The Final Investigator Command Center

**The Criminal Intelligence & Network Investigation Platform**  
*Synthetic SIH Research Prototype*

---

## 1. Executive Summary

Phase 11 delivers **The Final Investigator Command Center**, synthesizing all prior platform capabilities (Phases 1–10) into a single, cohesive, high-performance operational cockpit for law enforcement investigators and intelligence analysts.

Instead of navigating fragmented sub-modules, investigators have immediate, case-scoped access to verified entity networks, high-priority anomaly alerts, GAT algorithmic leads, chronological timeline events, geospatial intelligence, cross-case linkages, and cryptographic evidence integrity verification from a single unified surface.

### Core Deliverables
1. **Consolidated Case Intelligence Dashboard**: Accessible at `/dashboard` and `/cases/:caseId/dashboard`, backed by the unified backend endpoint `GET /api/v1/cases/{caseId}/dashboard`.
2. **Zero Mock/Simulation Invariant**: Every metric, degree, timestamp, coordinate, alert count, and hash verification is backed by live PostgreSQL, Neo4j, analytics services, and the append-only cryptographic ledger.
3. **Strict Responsible AI Invariants**: Model-predicted linkages (GAT) and analytical alerts are explicitly demarcated as *“Model-generated signal requiring review”*. Guilt or criminality probability is never calculated or asserted.
4. **Tri-State Theme System**: Light, Dark, and System appearance modes with instant switching and `localStorage` persistence.
5. **Full Internationalization (i18n)**: Native English and Marathi (मराठी) localization with modular JSON resource bundles, expandable to other Indian languages (Hindi, Tamil, Kannada, etc.).
6. **Non-Destructive Evidence Translation**: `ITranslationService` with cloud-ready provider abstraction and offline Marathi/English dictionary fallback. Case IDs, evidence IDs, phone numbers, license plates, and SHA-256 hashes remain strictly immutable.
7. **Multilingual Copilot**: Investigation Copilot processes both English and Marathi inquiries, grounding its reasoning in active case evidence while strictly preserving citations and entities.

---

## 2. Architecture & Data Flow

```
                     Investigator Command Center (/dashboard)
                                       │
                    GET /api/v1/cases/{caseId}/dashboard
                                       │
                                       ▼
                   ┌───────────────────────────────────────┐
                   │           DashboardService            │
                   │  - Authorization & Case Scoping       │
                   │  - Parallel Service Orchestration     │
                   └───────────────────┬───────────────────┘
                                       │
         ┌─────────────────────────────┼─────────────────────────────┐
         │                             │                             │
         ▼                             ▼                             ▼
┌───────────────────┐        ┌───────────────────┐        ┌───────────────────┐
│  PostgreSQL / DB  │        │       Neo4j       │        │  Cryptographic    │
│  - Case Metadata  │        │  - Graph Topology │        │  Ledger Service   │
│  - Entity Records │        │  - Node Degrees   │        │  - Chain Health   │
│  - Evidence Items │        │  - Betweenness    │        │  - SHA-256 Verify │
│  - Audit Logs     │        │  - Components     │        │  - Block Indices  │
└───────────────────┘        └───────────────────┘        └───────────────────┘
         │                             │                             │
         ▼                             ▼                             ▼
┌───────────────────┐        ┌───────────────────┐        ┌───────────────────┐
│   Alert Service   │        │ Timeline / Map    │        │ Entity Resolution │
│  - Severity Sums  │        │  - Event Times    │        │  - Confirmed Links│
│  - Anomaly Types  │        │  - Temporal Prec. │        │  - Potential Links│
│  - Review Status  │        │  - Lat / Lon Coords│        │  - Model Signals  │
└───────────────────┘        └───────────────────┘        └───────────────────┘
                                       │
                                       ▼
                         Consolidated DashboardDto
                                       │
                                       ▼
                   ┌───────────────────────────────────────┐
                   │         Frontend Command Center       │
                   │  - 6 Top-line Clickable KPIs          │
                   │  - Topology & Degree Overview         │
                   │  - Prioritized Signals Stream         │
                   │  - Evidence Chain Health Card         │
                   │  - Chronological Timeline Preview     │
                   │  - Geospatial Location Preview        │
                   │  - Tri-State Theme & Marathi i18n     │
                   └───────────────────────────────────────┘
```

---

## 3. Backend Implementation

### 3.1 `IDashboardService` & `DashboardService`
Located in `backend/Infrastructure/Services/DashboardService.cs`.
- **Case Scoping**: Validates case existence and authorization. Rejects cross-case contamination.
- **Parallel Querying**: Utilizes `Task.WhenAll` across PostgreSQL (EF Core), Neo4j (`INeo4jService`), Alert Service (`IAlertService`), and Integrity Ledger (`IIntegrityService`).
- **Bounded Topology Metrics**: Computes top connected nodes (degree centrality) and bridge entities (betweenness centrality) for the case subgraph.
- **Data Quality Warnings**: Automatically inspects case records for timestamp conflicts, unverified relationships, missing coordinates, and missing provenance.
- **Responsible AI Notice**: Injects authoritative governance disclaimer into every response payload.

### 3.2 `ITranslationService` & `TranslationService`
Located in `backend/Infrastructure/Services/TranslationService.cs`.
- **Provider Abstraction**: Detects Google Cloud Translation credentials; seamlessly falls back to offline law-enforcement dictionary for Marathi/English translations.
- **Invariant Preservation**: Regex tokenization protects Case IDs (`CASE-2026-0841-ORG`), Evidence IDs (`EVD-00042`), SHA-256 hashes (`3f7a8b...`), vehicle registration numbers (`MH-02-AB-1234`), and phone numbers from being altered during translation.
- **Non-Destructive Guarantee**: Translation outputs are returned with an explicit machine translation disclaimer and never overwrite original physical evidence files or database records.

### 3.3 Multilingual Copilot Support
Located in `backend/Infrastructure/Services/Copilot/`.
- **Language Detection**: `CopilotContextBuilder` detects Marathi Unicode ranges (`\u0900-\u097F`) and tags requests accordingly.
- **Marathi Intent Router**: `CopilotIntentRouter` matches Marathi investigative queries (e.g. `संबंध`, `पुरावे`, `सतर्कता`, `स्थान`, `अखंडता`, `घटनाक्रम`, `मार्ग`) to exact operational intents.
- **Bilingual Grounded Answers**: `LLMService` synthesizes grounded answers in Marathi while retaining exact entity identifiers, citation tags (`[EVD-...]`), and evidence hashes.

---

## 4. Frontend Command Center

### 4.1 Surface Architecture
- **Route**: Accessible at `/dashboard` (active case) and `/cases/:caseId/dashboard` (direct case link).
- **Case Switcher**: Header dropdown dynamically populates all accessible cases and switches active investigation context without page reloads.
- **Clickable Top-line KPIs**:
  - **Entities**: Total verified entities -> `/entities`
  - **Relationships**: Graph edge count -> `/network`
  - **Evidence**: Items indexed with verified count -> `/evidence`
  - **Alerts**: Active anomaly alerts with high-risk breakdown -> `/alerts`
  - **Cross-Case Links**: Inter-case linkages (Confirmed, Potential, Model) -> `/entity-resolution`
  - **Model Signals**: Algorithmic predictions requiring human review -> `/analytics`
- **Main 2-Column Responsive Layout**:
  - **Left Column**: Network Overview (degree ranking), Prioritized Signals Stream, Chronological Timeline (with temporal precision indicators), Investigation Map Preview (with unmapped location handling).
  - **Right Column**: Evidence Integrity Card (real-time chain status, hash comparison, tamper alerts), Cross-Case Intelligence, Investigator Actions Queue, and Recent Audit Activity Stream.

### 4.2 Tri-State Theme System
- Implementation in `src/hooks/useTheme.tsx`.
- Supports `light`, `dark`, and `system` (OS-level `prefers-color-scheme`).
- Complete color tokenization in `src/index.css` with CSS variables (`--color-surface`, `--color-border`, `--color-text-primary`, `--color-accent`, etc.).
- Persisted in `localStorage` under `cinip_theme`.

### 4.3 Multilingual Localization (i18n)
- Implementation in `src/i18n/index.tsx`.
- Structured locale directories: `src/i18n/locales/en/` and `src/i18n/locales/mr/`.
- Coverage: `common.json`, `dashboard.json`, `evidence.json`, `graph.json`, `alerts.json`, `timeline.json`, `copilot.json`, `integrity.json`.
- Automatic fallback to English when keys are missing.
- Persisted in `localStorage` under `cinip_language`.

### 4.4 Non-Destructive Evidence Translation
- Implementation in `src/features/evidence/EvidenceDetailPage.tsx`.
- Toggle between `Original Evidence` and `Translated View (मराठी)`.
- Highlights mandatory warning: *“Machine translation is provided for investigative reference only and is not authoritative evidence. Original evidence remains unchanged.”*
- Displays original immutable SHA-256 fingerprint alongside translated text.

---

## 5. Verification & Test Evidence

| Test Suite | Commands Executed | Result | Duration |
| :--- | :--- | :--- | :--- |
| **.NET Backend Unit Tests** | `dotnet test backend/Tests/Tests.csproj` | **173 / 173 PASSED** (0 failed) | 6.0s |
| **Python AI Service Tests** | `python -m unittest discover -s ai-service/tests` | **50 / 50 PASSED** (0 failed) | 0.24s |
| **Frontend TypeScript Check** | `npm run typecheck` (`tsc --noEmit`) | **0 ERRORS** | ~8s |
| **Frontend Production Build** | `npm run build` (`vite build`) | **SUCCESS** (1907 modules) | 15.03s |

### Phase 11 Specific Test Coverage (`Phase11DashboardTests.cs`)
1. `GetDashboard_ValidCase_ReturnsAggregatedData_WithScoping`: Verifies case-scoping and DB counts.
2. `GetDashboard_UnauthorizedCase_ThrowsKeyNotFoundException`: Verifies 404/rejection on unassigned cases.
3. `GetDashboard_NetworkPreview_RespectsGraphIsolation`: Verifies Neo4j relationship scoping.
4. `GetDashboard_AlertCounts_MatchesAlertServiceSummary`: Verifies cross-service alert aggregation.
5. `GetDashboard_Signals_SeparatesAlertsFromModelPredictions`: Verifies GAT demarcation from factual alerts.
6. `GetDashboard_EvidenceIntegrity_FlagsModifiedEvidence`: Verifies cryptographic warning on hash mismatch.
7. `GetDashboard_Timeline_PreservesTemporalPrecision`: Verifies `DATE_ONLY` and `TIME_UNAVAILABLE` handling.
8. `GetDashboard_Locations_HandlesMissingCoordinates`: Verifies unmapped location fallback.
9. `GetDashboard_CrossCase_DemarcatesConfirmedFromPredicted`: Verifies confirmed vs potential vs model separation.
10. `Translate_EnglishToMarathi_PreservesIdentifiers`: Verifies case IDs, phone numbers, and hashes are untouched.
11. `Translate_ProviderStatus_ReturnsCorrectCapabilities`: Verifies fallback detection.
12. `Copilot_MarathiQuery_DetectsLanguageAndReturnsGroundedResponse`: Verifies Marathi intent classification.
13. `Copilot_MarathiQuery_PreservesEntityCitations`: Verifies citations are never translated.
14. `DashboardDto_ResponsibleAiNotice_IsPopulated`: Verifies disclaimer presence.
15. `DashboardDto_DataQualityWarnings_DetectsIncompleteData`: Verifies coordinate and timestamp warnings.
