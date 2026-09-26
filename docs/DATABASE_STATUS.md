# DATABASE INTEGRITY & SCHEMA AUDIT REPORT
**Database Engine:** PostgreSQL 16 on `localhost:5432` (`criminal_network_db`)  
**Graph Engine:** Neo4j Community 5.20 on `bolt://localhost:7687`  
**Audit Date:** September 9, 2026  
**Canonical Investigation:** `inv-2026-0841` (`CASE-2026-0841`, "Operation Iron Vault Syndicate")  

---

## PostgreSQL Relational Tables Audit (24 Tables)

| # | Table Name | System Purpose | Record Count | Sample Primary Keys / IDs | Connection to `CASE-2026-0841` |
|---|---|---|---|---|---|
| 1 | `Cases` | Master investigation cases registry | 2 | `inv-2026-0841`, `inv-2026-0842` | Primary row for target demo investigation `CASE-2026-0841` |
| 2 | `Entities` | Criminal intelligence entities (Persons, Gangs, Vehicles, Locations) | 11 | `ent-rahul-01`, `ent-vikram-02`, `ent-gang-01` | Direct suspects, organizations, and assets belonging to Case `inv-2026-0841` |
| 3 | `EntityAliases` | Aliases, street names, and monikers for entities | 8 | `alias-001`, `alias-002` | Aliases for Rahul Mehta ("Rocky", "Bhai") and Vikram Rao |
| 4 | `EntityRelationships` | Relational connections, hierarchies, and roles | 9 | `rel-001`, `rel-002` | Relational edges mapping the hierarchy of the Iron Vault Syndicate |
| 5 | `EvidenceItems` | Physical & digital evidence custody records | 4 | `ev-2026-001-cdr`, `ev-20260908-0af77a` | Contains CDR logs, surveillance reports, and live uploaded test files |
| 6 | `EvidenceLedgerBlocks` | Immutable cryptographic blockchain custody blocks | 5 | `block-genesis-000000`, `block-000001-ev1` | Cryptographic SHA-256 blocks linking all evidence items to actors |
| 7 | `ExtractionJobs` | AI text and multimodal entity extraction job queues | 2 | `job-20260814-001`, `job-mum-001` | Processes raw surveillance and CDR files into structured entities |
| 8 | `ExtractedEntities` | Raw entities identified by NLP/NER extraction jobs | 6 | `ext-ent-001`, `ext-ent-002` | Intermediate NER extractions pending investigator review |
| 9 | `ExtractedRelationships` | NLP-identified relationships awaiting approval | 3 | `ext-rel-001`, `ext-rel-002` | Candidate relationships identified in textual evidence |
| 10 | `TimelineEvents` | Chronological event logs with timestamp and source | 5 | `evt-001`, `evt-002`, `evt-005` | Key operational milestones (meetings, money transfers, arrests) |
| 11 | `Locations` | Geographic crime scene and surveillance coordinates | 2 | `loc-shivaji-01`, `loc-mumbai-02` | Physical coordinates for Shivajinagar hideout and Mumbai port dock |
| 12 | `Alerts` | Real-time threat detection and anomaly alerts | 3 | `alt-001`, `alt-002`, `alt-003` | Syndicate activity alerts (border movement, burner phone spikes) |
| 13 | `AuditLogs` | Tamper-resistant investigator action tracking | 18+ | `aud-001`, `aud-002`, live records | Comprehensive audit trail of logins, evidence views, and queries |
| 14 | `Users` | Law enforcement personnel, roles, and credentials | 4 | `usr-dcp-sharma`, `usr-analyst-patil` | Assigned Lead Investigator (DCP Sharma) and Intelligence Analysts |
| 15 | `Roles` | Role-based authorization privileges | 4 | `role-lead`, `role-analyst` | Defines Investigator, Analyst, Forensics, and Admin permissions |
| 16 | `UserRoles` | User-to-role assignment junction table | 4 | Composite PK | Assigns DCP Sharma to Lead Investigator, Neha Patil to Analyst |
| 17 | `EntityResolutions` | Deduplication merges, matches, and resolutions | 3 | `res-cand-001`, `res-cand-002` | Resolved potential duplicate entries for Rahul Mehta and phone numbers |
| 18 | `InvestigationNotes` | Investigator field notes and hypothesis logs | 4 | `note-001`, `note-002` | Tactical case notes logged by DCP Rajesh Sharma |
| 19 | `CaseAssignments` | Officer case assignments and authorization scopes | 3 | Composite PK | Confirms lead investigator assignment for `inv-2026-0841` |
| 20 | `PhoneNumbers` | Tracked telecommunication identifiers (IMSI/IMEI) | 6 | `phone-001`, `phone-002` | CDR target burner numbers tied to syndicate communications |
| 21 | `BankAccounts` | Tracked financial nodes, accounts, and hawala conduits | 4 | `bank-001`, `bank-002` | Accounts used for extortion deposits and illicit funds routing |
| 22 | `Vehicles` | Monitored getaway and transport vehicle registrations | 3 | `veh-001`, `veh-002` | Target logistics vehicles (e.g. MH12AB4321) identified in Pune |
| 23 | `Warrants` | Judicial search warrants and court wiretap orders | 2 | `war-001`, `war-002` | Active judicial warrants for Shivajinagar commercial complex raid |
| 24 | `ReportArtifacts` | Generated intelligence reports, CSVs, and PDF dossiers | 3 | `rep-001`, `rep-002`, live records | Historical and on-demand generated investigation briefing files |

---

## Neo4j Knowledge Graph Schema & Node Count

- **Total Graph Nodes:** 11 nodes
  - **Person Nodes (6):** Rahul Mehta (Kingpin), Vikram Rao (Enforcer), Priya Shah (Financier), Amit Desai (Associate), Sunil Kamble (Associate), Suresh Patil (Cross-Case Link)
  - **Organization Nodes (1):** Iron Vault Syndicate
  - **Vehicle Nodes (1):** MH12AB4321
  - **Location Nodes (2):** Shivajinagar Commercial Complex, Pune Central Hideout
  - **Bank Account Nodes (1):** HDFC Hawala Conduit 4099-2810
- **Total Graph Relationships (Edges):** 9 directed edges
  - `COMMANDS`: Rahul Mehta -> Vikram Rao
  - `CONTROLS_FINANCES`: Priya Shah -> Iron Vault Syndicate
  - `OPERATES_VEHICLE`: Vikram Rao -> MH12AB4321
  - `ASSOCIATED_WITH`: Amit Desai -> Rahul Mehta
  - `MEETS_AT`: Rahul Mehta -> Shivajinagar Commercial Complex
  - `COMMUNICATES_WITH`: Rahul Mehta -> Suresh Patil
  - `FUNDS_TRANSFER`: Priya Shah -> HDFC Hawala Conduit 4099-2810
  - `MONITORS`: Sunil Kamble -> Pune Central Hideout
  - `COORDINATES_LOGISTICS`: Vikram Rao -> Sunil Kamble

All 11 nodes and 9 edges are verified active in Neo4j and synchronized with the frontend Cytoscape canvas via `GET /api/v1/cases/inv-2026-0841/graph`.
