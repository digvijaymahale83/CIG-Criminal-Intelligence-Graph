# Phase 7: Geospatial Intelligence & Investigation Map Subsystem

## 1. Overview & Objectives

Phase 7 introduces a comprehensive Geospatial Intelligence (GEOINT) subsystem into the Criminal Intelligence & Network Investigation Platform (synthetic SIH research prototype).

The objective is to ground investigations in physical reality by anchoring extracted events and entities to canonical, geocoded locations, computing geodesic distances using the exact Haversine formula, reconstructing chronological travel trajectories, detecting spatial-temporal co-presence overlaps, and identifying regional activity clusters.

### Core Principles & Ethical Guarantees

1. **Investigative Signal, Not Proof of Criminal Activity:**
   - Spatial proximity and spatial-temporal overlaps are investigative leads indicating potential physical co-presence or geographic association, **never** proof of culpability, guilt, or illicit conspiracy. Two individuals present at the same terminal or dock may represent coincidental commute, public transit, or lawful commercial activity.
2. **Zero Automated Knowledge Graph Mutation:**
   - Surfaced spatial signals (`SPATIAL_TEMPORAL_OVERLAP`, `CROSS_CASE_SPATIAL_OVERLAP`, `PROXIMITY_CLUSTER`, `IMPLAUSIBLE_TRAVEL_SPEED`) are staged in a `PENDING` state. Confirming a signal records an immutable audit log entry and updates the review status to `CONFIRMED` for investigator dossiers, but **never** automatically creates or mutates graph edges or alters case topology.
3. **Strict Neutral Investigative Terminology:**
   - No individual or entity is ever labeled as "criminal", "guilty", "kingpin", or "offender" based on geospatial or travel analytics.
   - Neutral terminology is strictly enforced across the system:
     - *Location Activity*
     - *Geographic Signal*
     - *Spatial Overlap*
     - *Travel Pattern / Sequence*
     - *Investigative Lead*
     - *Association Cluster*
4. **Complete Forensic Evidence Provenance:**
   - Every location record, event, and spatial signal links directly to its source evidence item with SHA-256 integrity verification, storage path citations, and geocode precision. Synthetic coordinates are confined strictly to explicitly marked SIH demonstration data.

---

## 2. Geospatial Data Model Architecture

Phase 7 integrates canonical location management and geographic signal staging into the PostgreSQL / EF Core schema:

### 2.1 Canonical Location Item (`backend/Domain/Entities/LocationItem.cs`)

The `LocationItem` entity represents verified geographic nodes within an investigation:

| Property | Type | Description |
| :--- | :--- | :--- |
| `Id` | `string` (UUID) | Unique location identifier |
| `CaseId` | `string?` | Scoped case identifier (nullable for global landmarks) |
| `EntityId` | `string?` | Optional foreign key link to an `EntityItem` of Type = `LOCATION` |
| `Name` | `string` | Human-readable location name (e.g., "Pune Shivajinagar Hub") |
| `NormalizedName` | `string` | Uppercase normalized name for case-insensitive matching |
| `Address` | `string?` | Postal or street address |
| `City` | `string?` | City / Municipal area |
| `District` | `string?` | District administration |
| `State` | `string?` | State / Province (e.g., "Maharashtra") |
| `Country` | `string` | Default: "India" |
| `Latitude` | `double` | Geodesic latitude in $[-90.0, 90.0]$ |
| `Longitude` | `double` | Geodesic longitude in $[-180.0, 180.0]$ |
| `GeocodePrecision` | `string` | Precision indicator: `EXACT`, `BUILDING`, `STREET`, `AREA`, `CITY`, `DISTRICT`, `STATE`, `COUNTRY`, `APPROXIMATE`, `UNKNOWN` |
| `Source` | `string` | Origin indicator: `SOURCE_DATA`, `SYNTHETIC_DEMO`, `GEOCODING_SERVICE`, `MANUAL_ENTRY` |
| `CreatedAtUtc` | `DateTime` | Creation timestamp |
| `UpdatedAtUtc` | `DateTime` | Last update timestamp |

**Database Indexes:**
- `(CaseId, Name)`: Fast lookup and case-scoped filtering.
- `(Latitude, Longitude)`: Spatial bounding queries.
- `(City, District)`: Administrative boundary filtering.

### 2.2 Spatial Signal Entity (`backend/Domain/Entities/SpatialSignal.cs`)

Represents staged geographic correlation leads:

| Property | Type | Description |
| :--- | :--- | :--- |
| `Id` | `string` (UUID) | Unique signal identifier |
| `CaseId` | `string` | Scoped case identifier |
| `AnalysisRunId` | `string?` | Link to execution run |
| `SignalType` | `string` | `SPATIAL_TEMPORAL_OVERLAP`, `CROSS_CASE_SPATIAL_OVERLAP`, `PROXIMITY_CLUSTER`, `IMPLAUSIBLE_TRAVEL_SPEED` |
| `SourceEntityId` | `string?` | Primary entity reference |
| `TargetEntityId` | `string?` | Secondary entity reference |
| `LocationId` | `string?` | Location reference |
| `StartTimeUtc` | `DateTime?` | Overlap window start |
| `EndTimeUtc` | `DateTime?` | Overlap window end |
| `DistanceKm` | `double` | Separation distance in kilometers |
| `Score` | `double` | Algorithmic signal confidence (0.0 to 1.0) |
| `Explanation` | `string` | Detailed contextual description of the spatial lead |
| `SupportingEvidenceIdsJson` | `string` | JSON array of supporting evidence IDs |
| `Status` | `string` | `PENDING`, `CONFIRMED`, `DISMISSED` (default: `PENDING`) |
| `ReviewedBy` | `string?` | Investigator username who reviewed the lead |
| `ReviewedAtUtc` | `DateTime?` | Review timestamp |
| `ReviewNotes` | `string?` | Mandatory investigator justification |

### 2.3 Spatial Analysis Run Entity (`backend/Domain/Entities/SpatialAnalysisRun.cs`)

Tracks execution metrics and operational audit metadata:
- `TotalLocationsAnalyzed`, `TotalEventsAnalyzed`, `SignalsGenerated`, `OverlapsFound`, `ClustersFound`, `VelocityWarningsFound`, `ExecutedBy`, `ExecutionDurationMs`.

---

## 3. Mathematical & Algorithmic Implementation

### 3.1 Exact Haversine Great-Circle Distance (`GeoMath.cs`)

Great-circle distance $d$ across the spherical Earth ($R = 6371.0\text{ km}$):

$$\Delta\phi = \text{radians}(\text{lat}_2 - \text{lat}_1)$$
$$\Delta\lambda = \text{radians}(\text{lon}_2 - \text{lon}_1)$$
$$a = \sin^2\left(\frac{\Delta\phi}{2}\right) + \cos(\text{radians}(\text{lat}_1))\cos(\text{radians}(\text{lat}_2))\sin^2\left(\frac{\Delta\lambda}{2}\right)$$
$$c = 2 \cdot \text{atan2}\left(\sqrt{a}, \sqrt{1 - a}\right)$$
$$d = R \cdot c$$

Boundary check: If $|\text{lat}_1 - \text{lat}_2| < 10^{-9}$ and $|\text{lon}_1 - \text{lon}_2| < 10^{-9}$, $d = 0.0\text{ km}$.

### 3.2 Implausible Travel Velocity Detector

Investigative warning model flags data quality issues, timestamp inaccuracies, or impossible physical movement:
- Given chronological events $E_1$ at $(L_1, t_1)$ and $E_2$ at $(L_2, t_2)$:
  $$\Delta t = t_2 - t_1 \quad (\text{hours})$$
  $$v = \frac{d(L_1, L_2)}{\Delta t} \quad (\text{km/h})$$
- If $d > 100\text{ km}$ and $v > 900.0\text{ km/h}$ (commercial cruise speed ceiling), the step flags `isImplausibleSpeed = true` and generates an `IMPLAUSIBLE_TRAVEL_SPEED` signal for human review.

### 3.3 Spatial-Temporal Co-Presence Overlap

Detects when distinct entities are recorded at the same physical location within a defined temporal delta $\Delta t \le 2.0\text{ hours}$.

### 3.4 Regional Proximity Clustering

Deterministic spatial clustering groups location nodes separated by $\le 25.0\text{ km}$, computing regional centroid coordinates and aggregate entity/event counts.

---

## 4. REST API Reference

All endpoints are mounted under `/api/v1` and require JWT authentication (`Role: Investigator, LeadInvestigator, Supervisor, Admin`).

| Method | Endpoint | Description | Audit Action |
| :--- | :--- | :--- | :--- |
| `GET` | `/cases/{caseId}/map` | Aggregates case locations, clusters, and active signals | `MAP_VIEWED` |
| `GET` | `/cases/{caseId}/map/locations` | Lists canonical locations with search query | - |
| `GET` | `/locations/{id}` | Canonical location details | `LOCATION_VIEWED` |
| `GET` | `/locations/{id}/activity` | Location events, observed entities, and SHA-256 evidence | `LOCATION_VIEWED` |
| `GET` | `/entities/{id}/locations` | All locations where an entity was observed | - |
| `GET` | `/entities/{id}/travel-sequence` | Chronological travel steps with distances & speeds | `ENTITY_TRAVEL_VIEWED` |
| `GET` | `/cases/{caseId}/map/proximity` | Haversine proximity search around given coordinates | `PROXIMITY_SEARCH` |
| `GET` | `/cases/{caseId}/map/analysis/latest` | Latest spatial analysis run report | - |
| `POST` | `/cases/{caseId}/map/analysis` | Runs spatial-temporal overlap and clustering analysis | `SPATIAL_ANALYSIS_COMPLETED` |
| `GET` | `/cases/{caseId}/map/signals` | Lists staged spatial signals with status filter | - |
| `POST` | `/cases/{caseId}/map/signals/{id}/review` | Confirms or dismisses a spatial signal | `SPATIAL_SIGNAL_CONFIRMED` / `DISMISSED` |

---

## 5. Frontend Leaflet GIS Implementation

The frontend replaces static NCRB mockups with a live Leaflet GIS investigation map in `src/features/map/GeospatialMapPage.tsx`:
- **CartoDB Dark Basemap:** High-contrast dark tiles (`dark_all`) styled for tactical law enforcement / intelligence environments.
- **Precision Color System:**
  - Emerald (`#10b981`): `EXACT` / `BUILDING`
  - Blue (`#3b82f6`): `STREET`
  - Amber (`#f59e0b`): `CITY` / `AREA`
  - Violet (`#8b5cf6`): Regional cluster envelope
- **Interactive Location Drawer:** Real-time event timeline, observed entity rosters, and cryptographic SHA-256 evidence verification badges.
- **Entity Trajectory Mode:** Animated dashed polylines, numbered waypoint badges, step elapsed deltas, and supersonic speed warning alerts.
- **Spatial Signal Review Modal:** Enables investigators to record formal rationale and audit decisions without mutating the graph.
- **Proximity Radius Tool:** Dynamic slider (5–100 km) with translucent canvas radius circles.

---

## 6. Cross-Feature Integrations

- **Knowledge Graph ↔ Map:** In `NetworkGraphPage.tsx`, clicking any node offers "View Location on Map" (for `LOCATION` nodes) or "Trace Trajectory on Map" (for `PERSON` nodes).
- **Timeline ↔ Map:** In `TimelinePage.tsx`, location tags on event cards are clickable, routing directly to the target location on the GIS map. Overlap signal cards provide a "View on Map" shortcut.
- **Dashboard ↔ Map:** In `DashboardPage.tsx`, the header features a direct link to the Investigation Map.

---

## 7. Verification & Validation Summary

- **.NET Unit Tests:** 115 tests passed (100% pass rate in `Tests.Unit.Phase7GeospatialIntelligenceTests` and Phases 1–6).
- **Python AI Service Tests:** 42 unit tests passed (100% pass rate in `test_geospatial.py` and graph/NLP suites).
- **Frontend Production Build:** Vite build succeeded with 0 errors (`✓ built in 14.02s`).
- **Graph Mutation Integrity:** Verified through automated testing that confirming a spatial signal never creates or mutates graph nodes or edges.
