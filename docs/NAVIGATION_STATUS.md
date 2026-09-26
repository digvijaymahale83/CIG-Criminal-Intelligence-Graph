# APPLICATION NAVIGATION & UI ROUTES AUDIT REPORT
**Target Platform:** The Criminal Intelligence & Network Investigation Platform  
**Frontend Host:** React 18 + Vite on `http://localhost:3000`  
**Backend API Host:** C# ASP.NET Core 8 on `http://localhost:5000`  
**Audit Date:** September 9, 2026  
**Verification Method:** Real Browser CDP Session (Edge Chromium headless, port 9360), 12/12 routes crawled and screenshot-verified  

---

## Complete Route Audit Matrix (12 Routes)

| # | Route | React Page Component | Page Loads? | Backend API Invoked | Real Live Data Displayed | Verified Screenshot Artifact | Status |
|---|---|---|---|---|---|---|---|
| 1 | `/dashboard` | `src/features/dashboard/DashboardPage.tsx` | YES (HTTP 200) | `GET /api/v1/cases/inv-2026-0841/summary` | Real active case KPIs, threat score, recent alerts, and entity counts | `verified_dashboard.png` | **WORK** |
| 2 | `/investigations` | `src/features/investigations/InvestigationsPage.tsx` | YES (HTTP 200) | `GET /api/v1/cases` | 2 live cases (`inv-2026-0841`, `inv-2026-0842`) with status badges and officer metadata | `verified_investigations.png` | **WORK** |
| 3 | `/evidence` | `src/features/evidence/EvidenceListPage.tsx` | YES (HTTP 200) | `GET /api/v1/evidence?caseId=inv-2026-0841`<br/>`POST /api/v1/evidence/upload` | CDR CSV, surveillance text, and live-uploaded `test_evidence.txt` with SHA-256 hashes | `verified_evidence.png` | **WORK** |
| 4 | `/entities` | `src/features/entities/EntityListPage.tsx` | YES (HTTP 200) | `GET /api/v1/entities?caseId=inv-2026-0841` | 11 live criminal suspects, aliases, roles, risk levels, and phone conduits | `verified_entities.png` | **WORK** |
| 5 | `/network` | `src/features/graph/NetworkGraphPage.tsx` | YES (HTTP 200) | `GET /api/v1/cases/inv-2026-0841/graph` | Cytoscape canvas: 11 nodes, 9 visible relationship edges, arrowheads, confidence badges | `verified_knowledge_graph.png` | **WORK** |
| 6 | `/analytics` | `src/features/analytics/AnalyticsPage.tsx` | YES (HTTP 200) | `GET /api/v1/analytics/case/inv-2026-0841` | Real centrality metrics (degree, betweenness, PageRank) for Rahul Mehta and syndicate | `verified_analytics.png` | **WORK** |
| 7 | `/timeline` | `src/features/timeline/TimelinePage.tsx` | YES (HTTP 200) | `GET /api/v1/timeline/case/inv-2026-0841` | 5 chronologically sequenced operational events with suspect filters and event badges | `verified_timeline.png` | **WORK** |
| 8 | `/entity-resolution` | `src/features/entity-resolution/EntityResolutionPage.tsx` | YES (HTTP 200) | `GET /api/v1/entity-resolution/candidates` | Live deduplication candidates with match confidence scores (Jaro-Winkler + graph proximity) | `verified_entity_resolution.png` | **WORK** |
| 9 | `/map` | `src/features/map/MapPage.tsx` | YES (HTTP 200) | `GET /api/v1/locations/case/inv-2026-0841` | Interactive Leaflet map with Pune Central, Shivajinagar hideout, and Mumbai port pins | `verified_investigation_map.png` | **WORK** |
| 10 | `/alerts` | `src/features/alerts/AlertsPage.tsx` | YES (HTTP 200) | `GET /api/v1/alerts` | 3 threat alerts (High/Critical priority) with acknowledge and escalate workflows | `verified_alerts.png` | **WORK** |
| 11 | `/reports` | `src/features/reports/ReportsPage.tsx` | YES (HTTP 200) | `GET /api/v1/reports`<br/>`GET /api/v1/reports/{id}/csv`<br/>`GET /api/v1/reports/{id}/pdf` | Live case listing with one-click export of verified CSV and official binary PDF dossier | `verified_reports.png` | **WORK** |
| 12 | `/audit` | `src/features/audit/AuditPage.tsx` | YES (HTTP 200) | `GET /api/v1/audit/logs` | Immutable audit log of all logins, evidence uploads, integrity verifications, and user actions | `verified_compliance___audit.png` | **WORK** |

---

## Localization (i18n) & Accessibility Audit

- **Dual-Language Toggle (English / Marathi मराठी):**
  - Verified across navigation sidebar, page titles, statistics cards, and button labels.
  - Missing keys discovered during Part 17 audit (`analytics`, `evidence`, `entities`, `graph`, `timeline`, `resolution`, `map`, `alerts`, `reports`) were populated into both `src/i18n/locales/en/common.json` and `src/i18n/locales/mr/common.json`.
  - Switching to Marathi verified via `verified_marathi_dashboard.png`: displays `डॅशबोर्ड`, `तपास`, `पुरावा`, `घटक`, `नेटवर्क आलेख`, `अनालिटिक्स`, `टाइमलाइन`.
- **Theme Adaptability (Light / Dark / System):**
  - Cytoscape graph canvas dynamically swaps background colors and edge contrasts:
    - Dark Mode (`#0f172a` canvas, `#38bdf8` high-contrast lines, verified via `graph_dark_verified.png`).
    - Light Mode (`#f8fafc` canvas, `#0284c7` dark blue lines, verified via `graph_light_verified.png`).
    - System Mode (automatically follows OS theme preference, verified via `graph_system_verified.png`).
