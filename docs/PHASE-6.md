# Phase 6: Timeline & Temporal Intelligence Subsystem

## 1. Overview & Objectives

Phase 6 introduces a comprehensive Timeline and Temporal Intelligence subsystem into the Criminal Intelligence & Network Investigation Platform (synthetic SIH research prototype).

The objective is to reconstruct accurate, evidence-grounded chronological event flows from unstructured forensic records, compute exact temporal overlaps between entities and locations, analyze behavioral sequences, identify activity clusters, and surface cross-case temporal synchronizations for investigator review.

### Core Principles & Ethical Guarantees

1. **Investigative Signal, Not Proof of Criminal Activity:**
   - Temporal correlation is an investigative lead, *never* proof of culpability or illicit activity. Two entities present at the same location at the same time may represent coincidence, bystander presence, or routine public activity.
2. **Zero Automated Knowledge Graph Mutation:**
   - Surfaced temporal signals (overlaps, clusters, cross-case synchronizations) are staged in a `PENDING` state. Confirming a signal records an immutable audit log entry but **never** automatically creates or mutates graph edges or alters case topology.
3. **Strict Neutral Investigative Terminology:**
   - No individual or entity is ever labeled as "criminal", "guilty", "kingpin", or "offender" based on temporal analysis.
   - Neutral terminology is strictly enforced across the system:
     - *Temporal Overlap Lead*
     - *Activity Cluster*
     - *Chronological Sequence*
     - *Cross-Case Temporal Synchronization*
     - *Investigative Lead*
4. **Complete Forensic Evidence Provenance:**
   - Every event and temporal signal links directly to its source evidence item with SHA-256 integrity verification, page/line citations, and extraction confidence. No synthetic hallucinations or fabricated dates are permitted.

---

## 2. Temporal Data Model Architecture

Phase 6 builds upon and enriches the existing domain entities in the PostgreSQL / EF Core schema rather than creating duplicate tables.

### 2.1 Enriched ExtractedEvent Entity (`backend/Domain/Entities/ExtractedEvent.cs`)

The core `ExtractedEvent` entity captures all timestamped occurrences extracted from case evidence:

| Property | Type | Description |
| :--- | :--- | :--- |
| `Id` | `string` (UUID) | Unique event identifier |
| `CaseId` | `string` | Scoped investigation identifier |
| `EvidenceId` | `string` | Source forensic evidence document |
| `EventType` | `string` | Categorical classification (e.g., `COMMUNICATION`, `FINANCIAL_TRANSACTION`, `SURVEILLANCE_SIGHTING`, `TRAVEL_MOVEMENT`) |
| `Description` | `string` | Detailed contextual description of the event |
| `StartTimeUtc` | `DateTime?` | Start boundary in normalized UTC |
| `EndTimeUtc` | `DateTime?` | End boundary in normalized UTC (defaults to `StartTimeUtc` for instantaneous events) |
| `TimePrecision` | `string` | Precision classification: `EXACT`, `MINUTE`, `HOUR`, `DAY`, `DATE_ONLY`, `MONTH`, `YEAR`, `UNKNOWN` |
| `Location` | `string?` | Textual description of the location |
| `LocationEntityId` | `string?` | Foreign key reference to canonical location entity |
| `RelatedEntitiesJson` | `string` | JSON array of involved entity names and UUIDs |
| `Confidence` | `double` | Extraction and temporal confidence (0.0 to 1.0) |
| `VerificationStatus` | `string` | Staged verification status: `PENDING`, `APPROVED`, `REJECTED` |
| `SourcePage` | `int?` | Document page number for forensic auditing |

**Database Indexes:**
Composite and single-column B-Tree indexes configured in `AppDbContext`:
- `(CaseId, StartTimeUtc)`: Optimized for chronological stream retrieval and range slicing.
- `(CaseId, EventType)`: Accelerated categorical event filtering.
- `(CaseId, ReviewStatus)`: Fast verification status partitioning.

### 2.2 TemporalAnalysisRun Entity (`backend/Domain/Entities/TemporalAnalysisRun.cs`)

Tracks the execution lifecycle and metrics of automated temporal intelligence runs:

| Property | Type | Description |
| :--- | :--- | :--- |
| `Id` | `string` (UUID) | Analysis run execution identifier |
| `CaseId` | `string` | Target investigation |
| `ExecutedBy` | `string` | User identifier of executing investigator/analyst |
| `Status` | `string` | Lifecycle state: `PENDING`, `RUNNING`, `COMPLETED`, `FAILED` |
| `StartedAtUtc` | `DateTime` | Execution start timestamp |
| `CompletedAtUtc` | `DateTime?` | Execution completion timestamp |
| `TotalEventsAnalyzed` | `int` | Number of verified events processed |
| `SignalsGenerated` | `int` | Total candidate signals created |
| `OverlapsFound` | `int` | Count of detected temporal co-presences |
| `ClustersFound` | `int` | Count of identified high-density activity windows |
| `ConfigurationJson` | `string` | Run parameters (thresholds, cross-case flags) |

### 2.3 TemporalSignal Entity (`backend/Domain/Entities/TemporalSignal.cs`)

Represents surfaced temporal correlations requiring human review:

| Property | Type | Description |
| :--- | :--- | :--- |
| `Id` | `string` (UUID) | Signal record identifier |
| `CaseId` | `string` | Primary investigation scope |
| `AnalysisRunId` | `string` | Associated analysis execution |
| `SignalType` | `string` | `TEMPORAL_OVERLAP`, `CROSS_CASE_TEMPORAL_OVERLAP`, `ACTIVITY_CLUSTER`, `TEMPORAL_SEQUENCE` |
| `SourceEntityId` | `string?` | First involved entity |
| `TargetEntityId` | `string?` | Second involved entity (or counterpart) |
| `LocationEntityId` | `string?` | Common location entity if applicable |
| `LocationName` | `string?` | Textual location descriptor |
| `OverlapStartUtc` | `DateTime?` | Start boundary of temporal intersection |
| `OverlapEndUtc` | `DateTime?` | End boundary of temporal intersection |
| `DurationMinutes` | `double` | Mathematical duration of the overlap |
| `Score` | `double` | Signal strength / correlation score (0.0 to 1.0) |
| `Explanation` | `string` | Human-readable explainable rationale |
| `SignalDetailsJson` | `string` | Serialized component signals and metrics |
| `SupportingEvidenceJson` | `string` | Array of source evidence identifiers and filenames |
| `Status` | `string` | Review state: `PENDING`, `CONFIRMED`, `DISMISSED` |
| `ReviewedBy` | `string?` | Reviewing investigator identifier |
| `ReviewedAtUtc` | `DateTime?` | Timestamp of investigator action |
| `ReviewNotes` | `string?` | Mandatory investigator justification |

---

## 3. Temporal Normalization Engine (Python AI Service)

Located in `ai-service/app/nlp/temporal_normalization.py` and mounted at `POST /api/v1/temporal/normalize`, the temporal normalization engine standardizes unstructured textual timestamps into structured UTC date-time intervals with explicit precision flags.

### Supported Patterns & Expressions

1. **Standard ISO 8601 & Microsecond Timestamps:**
   - `2026-03-12T10:30:00Z` $\rightarrow$ `time_precision: EXACT`
   - `2026-03-12T10:30:00+05:30` $\rightarrow$ Normalized to UTC `2026-03-12T05:00:00Z`
2. **Indian & Commonwealth Date Formats:**
   - `12/03/2026` or `12-03-2026` $\rightarrow$ `2026-03-12T00:00:00Z`, `time_precision: DATE_ONLY`
   - `12/03/2026 at 14:45` $\rightarrow$ `2026-03-12T14:45:00Z`, `time_precision: MINUTE`
3. **Natural English Month Names:**
   - `12 March 2026` or `March 12, 2026` $\rightarrow$ `2026-03-12T00:00:00Z`, `time_precision: DATE_ONLY`
   - `12 March 2026, 10:15 AM` $\rightarrow$ `2026-03-12T10:15:00Z`, `time_precision: MINUTE`
4. **Approximate Daily Time Windows:**
   - *"morning of 12 March 2026"* $\rightarrow$ `06:00:00` to `12:00:00 UTC`, `time_precision: HOUR`
   - *"afternoon of 12 March 2026"* $\rightarrow$ `12:00:00` to `17:00:00 UTC`, `time_precision: HOUR`
   - *"evening of 12 March 2026"* $\rightarrow$ `17:00:00` to `21:00:00 UTC`, `time_precision: HOUR`
   - *"night of 12 March 2026"* $\rightarrow$ `21:00:00` to `04:00:00 UTC` (next day), `time_precision: HOUR`
5. **Date Ranges:**
   - `Between 10 March 2026 and 15 March 2026` $\rightarrow$ `start_time_utc: 2026-03-10`, `end_time_utc: 2026-03-15`, `time_precision: DAY`
6. **Relative Dates (with Anchor Timestamp):**
   - *"3 days later"* with anchor `2026-03-12T10:00:00Z` $\rightarrow$ `2026-03-15T10:00:00Z`
   - *"the following morning"* with anchor `2026-03-12T18:00:00Z` $\rightarrow$ `2026-03-13T08:00:00Z`
7. **Year-Only & Month-Only:**
   - `2026` $\rightarrow$ `2026-01-01T00:00:00Z` to `2026-12-31T23:59:59Z`, `time_precision: YEAR`
   - `March 2026` $\rightarrow$ `2026-03-01T00:00:00Z` to `2026-03-31T23:59:59Z`, `time_precision: MONTH`

---

## 4. Temporal Intelligence Algorithms

### 4.1 Exact Mathematical Overlap Detection

To identify co-presence or simultaneous activity between two entities or events at the same location, the engine evaluates interval intersections:

$$\text{Overlap}(E_A, E_B) \iff \max(E_{A.\text{start}}, E_{B.\text{start}}) < \min(E_{A.\text{end}}, E_{B.\text{end}})$$

$$\text{Duration} = \min(E_{A.\text{end}}, E_{B.\text{end}}) - \max(E_{A.\text{start}}, E_{B.\text{start}})$$

**Scoring Model:**
The composite overlap score $S_{\text{overlap}} \in [0, 1]$ is computed reproducibly:

$$S_{\text{overlap}} = \min\left(1.0, 0.4 + 0.3 \cdot \min\left(1.0, \frac{\text{DurationMinutes}}{60.0}\right) + 0.3 \cdot \min(C_A, C_B)\right)$$

Where $C_A$ and $C_B$ are extraction confidences of the underlying evidence records.
- If duration is zero or negative, the events are non-overlapping and immediately rejected.
- Self-comparisons (comparing an entity to itself) are strictly prohibited.
- Events marked `REJECTED` by an investigator are excluded from analysis.

### 4.2 Temporal Sequence Analysis

Reconstructs the step-by-step movement and interactions of an entity across chronological time:
1. Filters all events referencing the target entity ID or canonical name within authorized case boundaries.
2. Orders chronologically by `StartTimeUtc`.
3. Computes the elapsed time delta between step $i$ and step $i-1$:
   $$\Delta t_i = T_{i.\text{start}} - T_{(i-1).\text{start}}$$
4. Formats human-readable elapsed indicators (e.g., `"+30m"`, `"+3h 15m"`, `"+2d 4h"`).

### 4.3 Activity Clustering

Groups dense bursts of investigative occurrences:
- Iterates across chronological events and links them into a single cluster if the temporal gap between consecutive events is $\le 36$ hours.
- Computes cluster statistics: total events, distinct involved entities, distinct locations, start/end bounds.
- Classifies cluster intensity:
  - **HIGH:** $\ge 5$ events or $\ge 4$ distinct entities.
  - **MEDIUM:** $3 - 4$ events.
  - **LOW:** $< 3$ events.

### 4.4 Cross-Case Temporal Correlation

Investigates whether entities from different cases were active in the same temporal window and physical location:
- Strictly scoped by investigator case access permissions.
- Compares events in Case A against authorized events in Case B.
- Surfaces `CROSS_CASE_TEMPORAL_OVERLAP` signals indicating potential shared coordination without prematurely linking cases.

---

## 5. REST API Specifications

All endpoints are hosted under `backend/API/Controllers/TimelineController.cs` with JWT authentication and RBAC authorization:

### 1. `GET /api/v1/cases/{caseId}/timeline`
Retrieves paginated chronological timeline events, active clusters, and sequence highlights.
- **Parameters:** `startDate`, `endDate`, `eventType`, `entityId`, `location`, `minConfidence`, `verificationStatus`, `page`, `pageSize`.

### 2. `GET /api/v1/cases/{caseId}/timeline/events`
Returns raw array of chronological `TimelineEventDto` records with full evidence provenance and SHA-256 integrity flags.

### 3. `GET /api/v1/entities/{entityId}/timeline`
Returns all chronological events referencing a specific entity across the case.

### 4. `GET /api/v1/relationships/{relationshipId}/timeline`
Returns interaction events involving both endpoints of a graph relationship.

### 5. `GET /api/v1/cases/{caseId}/timeline/range`
Returns earliest event, latest event, and total event count for the investigation date bounds.

### 6. `GET /api/v1/cases/{caseId}/temporal-analysis`
Retrieves the latest completed `TemporalAnalysisResultDto` (overlaps, clusters, metrics).

### 7. `POST /api/v1/cases/{caseId}/temporal-analysis/run`
Executes temporal overlap detection, clustering, and sequence analysis.
- **Roles Required:** `ADMIN`, `INVESTIGATOR`, `ANALYST`.
- **Payload:** `{ "includeCrossCase": true, "authorizedCaseIds": ["..."], "minOverlapDurationMinutes": 5.0 }`.

### 8. `GET /api/v1/cases/{caseId}/temporal-overlaps`
Returns list of detected temporal co-presences with duration, location, score, and explanation.

### 9. `GET /api/v1/entities/{entityId}/sequence`
Returns reconstructed step-by-step sequence of events with elapsed deltas for a specific entity.

### 10. `GET /api/v1/cases/{caseId}/temporal-summary`
Returns high-level temporal health metrics (total events, active period, activity peaks, pending/confirmed signals).

### 11. `POST /api/v1/cases/{caseId}/temporal-signals/{signalId}/review`
Reviews a candidate temporal signal (`CONFIRMED` or `DISMISSED`) with audit logging.
- **Roles Required:** `ADMIN`, `INVESTIGATOR`.
- **Payload:** `{ "status": "CONFIRMED", "reviewNotes": "Confirmed co-presence via CCTV evidence." }`.

---

## 6. Frontend User Interface

Implemented in `src/features/timeline/TimelinePage.tsx` and integrated across the platform:

### 1. Chronological Event Stream
- Displays timeline events with distinct visual nodes color-coded by category (`COMMUNICATION`, `FINANCIAL_TRANSACTION`, `SURVEILLANCE_SIGHTING`, `TRAVEL_MOVEMENT`).
- Filter toolbar supports text search, event type dropdown, verification status, and date range picking.
- Evidence Integrity Badge: Shows SHA-256 fingerprint verification and file origin.

### 2. Temporal Overlaps Tab
- Cards summarize overlapping entities, duration in minutes, common location, and composite score.
- Signal status badge: `PENDING`, `CONFIRMED`, `DISMISSED`.
- Direct action buttons: Confirm / Dismiss with review drawer.

### 3. Activity Clusters Tab
- Displays high-density activity windows with intensity badges (`HIGH`, `MEDIUM`, `LOW`).
- Entity and location distribution tags.

### 4. Sequence Flow Tab
- Step-by-step entity trajectory visualizer showing chronological progression and elapsed time intervals (`+30m`, `+4h`).

### 5. Knowledge Graph Integration
- **Selected Node Drawer:** Displays the entity's activity timeline, recent events, and a direct "Full Timeline" link.
- **Selected Edge Drawer:** Displays temporal interactions between relationship endpoints.
- **Cross-Navigation:** Clicking "View in Graph" from any timeline event highlights and centers the entity node in cytoscape.

---

## 7. Verification & Automated Test Matrix

### 7.1 Backend Automated Tests (`backend/Tests/Unit/Phase6TemporalIntelligenceTests.cs`)
19 comprehensive unit and integration tests covering:
1. `GetCaseTimelineAsync_ReturnsActualEventsFromDatabase`
2. `GetCaseTimelineAsync_FiltersByCaseId_EnforcingScoping`
3. `GetCaseTimelineAsync_FiltersByDateRange`
4. `GetCaseTimelineAsync_FiltersByEventType`
5. `GetCaseTimelineAsync_FiltersByEntityId`
6. `GetCaseTimelineAsync_SupportsExactAndApproximatePrecision`
7. `GetCaseTimelineAsync_IncludesEvidenceProvenance`
8. `DetectOverlaps_CalculatesExactMathematicalOverlap`
9. `DetectOverlaps_CalculatesPartialOverlap`
10. `DetectOverlaps_RejectsNonOverlappingEvents`
11. `DetectOverlaps_RejectsSelfComparison`
12. `RunTemporalAnalysisAsync_PersistsRunAndSignals`
13. `ConfirmSignal_DoesNotMutateKnowledgeGraph`
14. `ConfirmSignal_CreatesAuditLog`
15. `DismissSignal_DoesNotMutateKnowledgeGraph`
16. `DetectCrossCaseOverlaps_RespectsAuthorization`
17. `ReconstructEntitySequence_OrdersChronologicallyWithElapsed`
18. `ClusterEvents_GroupsWithinProximityWindow`
19. `RejectedEvents_AreExcludedFromTimeline`

### 7.2 Python Automated Tests (`ai-service/tests/test_temporal.py`)
12 unit tests covering all normalization cases:
1. `test_normalize_iso_timestamp`
2. `test_normalize_minute_precision`
3. `test_normalize_indian_date_format`
4. `test_normalize_natural_language_date`
5. `test_normalize_approximate_period_morning`
6. `test_normalize_approximate_period_evening`
7. `test_normalize_approximate_period_night`
8. `test_normalize_date_range`
9. `test_normalize_relative_date_with_anchor`
10. `test_normalize_relative_next_morning`
11. `test_normalize_year_only`
12. `test_normalize_unknown_expression`

### 7.3 Regression Test Results
- **.NET Unit Tests:** 98 passed, 0 failed (18 P1, 10 P2, 17 P3, 16 P4, 18 P5, 19 P6).
- **Python Unit Tests:** 35 passed, 0 failed (23 P4/P5, 12 P6).
- **Frontend Build:** `vite build` completed cleanly in 35.43s with 1880 modules transformed and 0 TypeScript errors.
