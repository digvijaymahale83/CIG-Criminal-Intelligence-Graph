# Phase 8: Alerts & Anomaly Detection Subsystem

## 1. Overview & Objectives

Phase 8 implements the centralized Alerts and Anomaly Detection subsystem for The Criminal Intelligence & Network Investigation Platform (synthetic SIH research prototype).

The objective is to synthesize intelligence signals across network structure, temporal timelines, geospatial trajectories, dyadic relationships, event activity aggregations, data consistency bounds, cross-case linkages, and machine learning models into prioritized, explainable, and deduplicated investigative alerts.

### Core Principles & Ethical Guarantees

1. **Investigative Signal, Not Proof of Criminal Activity:**
   - Surfaced anomalies represent statistical, structural, or spatial outliers that warrant investigator review. They **never** constitute proof of criminal guilt, liability, or illicit conspiracy.
   - An activity spike or travel velocity discrepancy may stem from logging delays, proxy devices, approximate geocoding, or simultaneous observations.
2. **Zero Automated Knowledge Graph Mutation:**
   - Acknowledging, reviewing, resolving, or dismissing an alert never mutates or adds edges or nodes to the primary investigation knowledge graph. All graph mutations remain strictly human-driven through investigator review workflows.
3. **Strict Neutral Investigative Terminology:**
   - Prejudicial labels such as "criminal", "guilty", "kingpin", "perpetrator", or "culprit" are strictly forbidden in detector algorithms, alert titles, descriptions, and explanations.
   - Standardized neutral terminology:
     - *Investigative Alert*
     - *Anomalous Pattern*
     - *Unusual Activity*
     - *High Connectivity Nexus Entity*
     - *Temporal Activity Burst*
     - *Geographic Outlier Activity*
     - *Relationship Interaction Surge*
     - *Case Activity Spike*
     - *Implausible Travel Velocity*
     - *Temporal Inversion Anomaly*
     - *Cross-Case Entity Nexus*
     - *Model Investigative Lead*
4. **Transparent, Explainable Priority Scoring:**
   - Every alert includes a bounded priority score calculated from a transparent, reproducible formula:
     $$\text{PriorityScore} = (0.40 \cdot \text{Score}) + (0.25 \cdot \text{HasEvidence}) + (0.20 \cdot \text{IsCrossCase}) + 0.15$$
   - Alerts are assigned clear review severities: `LOW`, `MEDIUM`, `HIGH`, `CRITICAL`.
5. **Deterministic SHA-256 Deduplication:**
   - Repeated detection runs generate deterministic SHA-256 fingerprints:
     $$\text{Fingerprint} = \text{SHA256}(\text{CaseId} \parallel \text{AlertType} \parallel \text{SourceEntity} \parallel \text{TargetEntity} \parallel \text{Location} \parallel \text{TimeWindowKey})$$
   - Repeated runs refresh existing alerts and increment `AlertsDeduplicated` without creating duplicate records or altering existing alert identifiers.

---

## 2. Detection Engine Architecture

The anomaly detection subsystem employs a modular plugin pattern implementing `IAnomalyDetector`:

```
+---------------------------------------------------------------------------------+
|                                 IAlertService                                   |
|                        (RunAlertDetectionAsync)                                 |
+---------------------------------------------------------------------------------+
           |                           |                           |
           v                           v                           v
+-----------------------+   +-----------------------+   +-----------------------+
| NetworkAnomaly        |   | TemporalAnomaly       |   | GeographicAnomaly     |
| (Degree & Betweenness)|   | (Bursts & Windows)    |   | (Centroid Disparities)|
+-----------------------+   +-----------------------+   +-----------------------+
           |                           |                           |
           v                           v                           v
+-----------------------+   +-----------------------+   +-----------------------+
| RelationshipSurge     |   | ActivitySpike         |   | UnusualTravel         |
| (Dyadic 48h clusters) |   | (Gaussian Z-Scores)   |   | (Geodesic Velocity)   |
+-----------------------+   +-----------------------+   +-----------------------+
           |                           |                           |
           v                           v                           v
+-----------------------+   +-----------------------+   +-----------------------+
| DataConsistency       |   | CrossCasePattern      |   | ModelSignal           |
| (Bounds & Inversions) |   | (Shared Entities/Sites|   | (GAT Attention Leads) |
+-----------------------+   +-----------------------+   +-----------------------+
           |                           |                           |
           +---------------------------+---------------------------+
                                       |
                                       v
                    +-------------------------------------+
                    |     Deduplication & Fingerprint     |
                    |    SHA-256 Collision Prevention     |
                    +-------------------------------------+
                                       |
                                       v
                    +-------------------------------------+
                    |       Explainable Priority &        |
                    |       Severity Classification       |
                    +-------------------------------------+
                                       |
                                       v
                    +-------------------------------------+
                    |   Persisted Alert & AlertRun Record |
                    |      Audit Trail Stream Logged      |
                    +-------------------------------------+
```

### 2.1 The 9 Analytical Detectors

| Detector | Category | Trigger Mechanism | Output Terminology |
| :--- | :--- | :--- | :--- |
| **NetworkAnomalyDetector** | Network Topology | Entity degree $\ge 5$ and $\ge 2.0\times$ case average, or degree $\ge 10$, or betweenness centrality $\ge 0.35$ | *High Connectivity Nexus Entity*, *Network Bridge Entity* |
| **TemporalAnomalyDetector** | Temporal Dynamics | $\ge 4$ events for an entity within a rolling 48-hour window, or staged `TemporalSignal` co-presence $\ge 0.70$ | *Temporal Activity Burst*, *Coincident Activity Window* |
| **GeographicAnomalyDetector** | Spatial Topology | Observation location $\ge 100\text{ km}$ from entity's historical centroid hub, or staged `SpatialSignal` co-presence $\ge 0.70$ | *Geographic Outlier Activity*, *Spatial Co-Presence Signal* |
| **RelationshipSurgeDetector** | Dyadic Interactions | $\ge 4$ joint events between two specific entities within a 48-hour rolling window | *Relationship Interaction Surge* |
| **ActivitySpikeDetector** | Case Aggregates | Daily event count $\ge 5$ with Gaussian Z-score $\ge 2.0$ or spike ratio $\ge 2.5\times$ baseline mean | *Case Activity Spike* |
| **UnusualTravelDetector** | Geodesic Kinematics | Sequential observations with implied velocity $> 800\text{ km/h}$ over distance $> 80\text{ km}$ | *Implausible Travel Velocity* |
| **DataConsistencyDetector** | Forensic Integrity | Inverted event timestamps (`EndTimeUtc < StartTimeUtc`) or geodetic coordinates exceeding WGS-84 bounds | *Temporal Inversion Anomaly*, *Geodetic Coordinate Out of Bounds* |
| **CrossCasePatternDetector** | Cross-Jurisdiction | Authorized cross-case entity matches (`Confidence >= 0.85`) or identical canonical locations across distinct cases | *Cross-Case Entity Nexus*, *Cross-Case Geographic Overlap* |
| **ModelSignalDetector** | Deep Graph Learning | Pending GAT (Graph Attention Network) relationship predictions with score $\ge 0.70$ | *GAT Model Investigative Lead* |

---

## 3. Data Model

### 3.1 Alert (`backend/Domain/Entities/Alert.cs`)

Persisted investigative alerts with complete evidentiary citations and reviewer audit fields:

```csharp
public class Alert
{
    public string Id { get; set; }
    public string CaseId { get; set; }
    public string? AlertRunId { get; set; }

    public string AlertType { get; set; } // NETWORK_ANOMALY, TEMPORAL_ANOMALY, ...
    public string Severity { get; set; }  // LOW, MEDIUM, HIGH, CRITICAL
    public string Status { get; set; }    // NEW, ACKNOWLEDGED, UNDER_REVIEW, RESOLVED, DISMISSED

    public string Title { get; set; }
    public string Description { get; set; }

    // Associated Entities / Location
    public string? SourceEntityId { get; set; }
    public string? SourceEntityName { get; set; }
    public string? TargetEntityId { get; set; }
    public string? TargetEntityName { get; set; }
    public string? LocationId { get; set; }
    public string? LocationName { get; set; }

    // Evidentiary Grounding
    public string? RelatedEventId { get; set; }
    public string? RelatedEvidenceId { get; set; }
    public string? RelatedEvidenceFileName { get; set; }
    public string? RelatedEvidenceSha256 { get; set; }

    public double Score { get; set; }
    public string DetectionMethod { get; set; }
    public string DetectionVersion { get; set; }
    public string Explanation { get; set; }
    public string DeduplicationFingerprint { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    // Reviewer Audit Fields
    public DateTime? ReviewedAtUtc { get; set; }
    public string? ReviewedBy { get; set; }
    public string? ReviewNotes { get; set; }
}
```

### 3.2 AlertRun (`backend/Domain/Entities/AlertRun.cs`)

Tracks operational batch execution metrics and execution performance:

```csharp
public class AlertRun
{
    public string Id { get; set; }
    public string CaseId { get; set; }
    public string Status { get; set; } // RUNNING, COMPLETED, FAILED
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public int DetectorsExecuted { get; set; }
    public int SignalsGenerated { get; set; }
    public int AlertsCreated { get; set; }
    public int AlertsDeduplicated { get; set; }
    public string ExecutedBy { get; set; }
    public long ExecutionDurationMs { get; set; }
    public string? ErrorMessage { get; set; }
}
```

---

## 4. Alert Review Lifecycle

The alert review lifecycle follows a strict investigator-driven state machine:

```
              +---------------+
              |      NEW      |
              +---------------+
                      |
        +-------------+-------------+
        |                           |
        v                           v
+---------------+           +---------------+
| ACKNOWLEDGED  |           | UNDER_REVIEW  |
+---------------+           +---------------+
        |                           |
        +-------------+-------------+
                      |
        +-------------+-------------+
        |                           |
        v                           v
+---------------+           +---------------+
|   RESOLVED    |           |   DISMISSED   |
+---------------+           +---------------+
```

- **NEW:** Freshly surfaced anomaly signal. Requires investigator triage.
- **ACKNOWLEDGED:** Initial officer receipt acknowledged.
- **UNDER_REVIEW:** Active investigation underway into temporal/spatial context.
- **RESOLVED:** Investigator confirmed finding, recorded resolution rationale, and archived finding.
- **DISMISSED:** Investigator reviewed finding, confirmed benign false alarm or irrelevant coincidence, and recorded rationale.

---

## 5. API Endpoints

All endpoints are registered under `/api/cases/{caseId}/alerts` and `/api/alerts`:

| Method | Route | Description |
| :--- | :--- | :--- |
| `GET` | `/api/cases/{caseId}/alerts` | Query case alerts with status, severity, type, and text filters |
| `GET` | `/api/cases/{caseId}/alerts/summary` | Return case alert counts, status distributions, and type breakdowns |
| `GET` | `/api/cases/{caseId}/alerts/{alertId}` | Fetch single alert details with full explanation and evidence references |
| `POST` | `/api/cases/{caseId}/alerts/run` | Trigger on-demand anomaly detection across configured analytical detectors |
| `POST` | `/api/alerts/{alertId}/acknowledge` | Mark alert status as `ACKNOWLEDGED` and record reviewer audit log |
| `POST` | `/api/alerts/{alertId}/start-review` | Mark alert status as `UNDER_REVIEW` and record reviewer audit log |
| `POST` | `/api/alerts/{alertId}/resolve` | Mark alert status as `RESOLVED` with investigator notes |
| `POST` | `/api/alerts/{alertId}/dismiss` | Mark alert status as `DISMISSED` with investigator notes |

---

## 6. Verification & Test Coverage

### 6.1 .NET Unit Test Suite (`backend/Tests/Unit/Phase8AlertAndAnomalyTests.cs`)
16 automated unit tests validating:
1. `Test01`: High-degree network nexus detection.
2. `Test02`: Rolling 48-hour temporal burst detection.
3. `Test03`: Centroid distance disparity geographic anomaly detection.
4. `Test04`: Dyadic relationship interaction surges.
5. `Test05`: Gaussian Z-score case activity spikes.
6. `Test06`: Implausible velocity / supersonic travel detection.
7. `Test07`: Inverted timestamp and out-of-bounds coordinates forensic consistency checks.
8. `Test08`: Authorized cross-case pattern and entity nexus resolution.
9. `Test09`: GAT model investigative leads.
10. `Test10`: Execution run persistence and `AlertRun` tracking.
11. `Test11`: Deterministic SHA-256 fingerprint deduplication across repeated runs.
12. `Test12`: Explainable priority scoring formula bounds and weights.
13. `Test13`: Complete review lifecycle (`ACKNOWLEDGED` $\to$ `UNDER_REVIEW` $\to$ `RESOLVED`) and audit logging.
14. `Test14`: Zero knowledge graph mutation guarantee upon resolution or dismissal.
15. `Test15`: Strict case scoping and isolation.
16. `Test16`: Strict neutral terminology enforcement across all titles, descriptions, and explanations.

**Result:** All 131 .NET unit tests passing (100% green).

### 6.2 Python Analytics & Algorithmic Tests (`ai-service/tests/test_anomalies.py`)
7 unit tests validating:
- Haversine geodetic distance calculations.
- Degree disparity detection.
- Rolling window temporal burst analysis.
- Supersonic velocity calculation.
- Gaussian Z-score daily count spikes.
- Normalized priority formula bounds.
- Prohibition of biased/prejudicial words.

**Result:** All 50 Python tests passing (100% green).

### 6.3 Frontend Production Build
Vite bundling verified with 0 TypeScript, bundling, or JSX errors:
`✓ built in 15.16s`.
