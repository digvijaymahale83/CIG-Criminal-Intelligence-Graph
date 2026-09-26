# Phase 2 — Evidence Processing & AI Extraction Engine Documentation

**Platform:** Criminal Intelligence & Network Investigation Platform (Maharashtra Police - SIH)  
**Status:** IMPLEMENTED & VERIFIED  
**Phase:** 2

---

## 1. Executive Summary

Phase 2 establishes an end-to-end, forensic-grade intelligence extraction engine. The objective is to turn uploaded investigation evidence (PDF, CSV, Excel, Images, JSON, Plain Text) into structured, reviewable intelligence while strictly adhering to the core governance rule:
> **The graph database (Neo4j) does NOT blindly consume unverified AI output.**

All raw AI extractions remain staged in PostgreSQL as candidate entities, relationships, and events. They are promoted to the permanent Neo4j graph database **only** after explicit human-in-the-loop review and approval by an investigator.

---

## 2. Evidence Processing Pipeline

### 2.1 Complete Lifecycle

```
[Evidence Upload] (PDF / CSV / XLSX / Image / JSON / TXT)
       │
       ▼
[Store Byte Stream] ──> Compute Cryptographic SHA-256 (Raw bytes)
       │
       ▼
[PostgreSQL Evidence Record] ──> Status: "UPLOADED" ──> "QUEUED"
       │
       ▼
[ExtractionQueueHostedService] (Background Queue Worker)
       │ (Polling batch every 5 seconds)
       ▼
[Python FastAPI Extraction Engine] (POST /api/v1/process/evidence)
       │
       ├─ File Parsers (PyMuPDF, pandas, openpyxl, PIL/pytesseract, text)
       ├─ Hybrid Entity Extraction (RegEx, Gazetteers, Rule Patterns)
       ├─ Deterministic Normalization (E.164, RTO standard, Title Case, Lowercase)
       └─ Relationship Extraction (Proximity co-occurrence, syntactic patterns)
       │
       ▼
[Stage Extraction Candidates in PostgreSQL]
       │
       ├─ ExtractedEntities (Status: "PENDING_REVIEW")
       ├─ ExtractedRelationships (Status: "PENDING_REVIEW")
       └─ ExtractedEvents
       │
       ▼
[Evidence Status Updated] ──> "REVIEW_REQUIRED"
       │
       ▼
[Investigator Review Interface] (/evidence/:evidenceId/extraction)
       │
       ├── [Approve Entity / Relationship] ──> Status: "APPROVED"
       │         │
       │         ▼
       │   [Neo4j Graph Promotion]
       │   (MERGE Entity Node, MERGE Relationship Edge with Provenance)
       │
       ├── [Edit Entity] ──> Correct Normalized Value / Type / Canonical Name
       │         │
       │         ▼
       │   [Neo4j Graph Promotion with Edited Values]
       │
       └── [Reject Entity / Relationship] ──> Status: "REJECTED" (Kept for Audit)
```

### 2.2 Lifecycle Status States

| Status | Description |
|---|---|
| `UPLOADED` | File received, written to storage, SHA-256 computed. |
| `QUEUED` | Placed in extraction processing queue. |
| `PROCESSING` | Extraction job active in background worker / Python AI service. |
| `EXTRACTED` | AI extraction finished successfully, candidate staging completed. |
| `REVIEW_REQUIRED` | Awaiting investigator review of extracted candidates. |
| `APPROVED` | All or critical items reviewed and approved by investigator. |
| `REJECTED` | Extraction or evidence rejected by investigator. |
| `FAILED` | Extraction job encountered an error (eligible for retry). |

---

## 3. Cryptographic Integrity & Immutability

### 3.1 Real Byte-Level SHA-256
Every uploaded file's SHA-256 hash is computed directly from its byte stream using `System.Security.Cryptography.SHA256` (C#) and `hashlib.sha256` (Python):
- No simulated hashes or mock values are permitted.
- Re-verification dynamically reads the physical file bytes from disk, computes a fresh SHA-256 hash, and compares it with the stored database hash.
- Result: `VALID` if byte hashes match exactly, or `TAMPERED` if any byte has been modified or corrupted.

### 3.2 Evidence Versioning & Append-Only Revisions
- Evidence records are never overwritten on disk or in the database.
- Uploading a revised document creates a new `Evidence` entity incrementing the `Version` number (e.g., `v2`, `v3`) with `ParentEvidenceId` referencing the original evidence.
- This maintains an immutable forensic chain of custody and full audit provenance.

---

## 4. Multi-Format Ingestion & Extraction Engine

The Python FastAPI extraction engine (`backend/AI/app/` and `ai-service/app/`) runs locally without external cloud LLM dependencies.

### 4.1 Supported Formats & Parsers

| Format | Library / Parser | Strategy |
|---|---|---|
| **PDF** (`.pdf`) | PyMuPDF (`fitz`) | Page-by-page text extraction with coordinate metadata. |
| **CSV** (`.csv`) | `pandas` | Tabular header mapping (Sender, Receiver, Amount, Phone, Date). |
| **Excel** (`.xlsx`, `.xls`) | `openpyxl` / `pandas` | Multi-sheet inspection and structured column extraction. |
| **Images** (`.png`, `.jpg`, `.jpeg`) | `PIL` + `pytesseract` (OCR) | Grayscale/contrast preprocessing and OCR text stream extraction. |
| **JSON** (`.json`) | Built-in recursive parser | Deep key-value traversing extracting identifiers and payloads. |
| **Plain Text** (`.txt`, `.log`) | Standard I/O with UTF-8 / Latin-1 | Line-by-line raw text parsing with offset tracking. |

### 4.2 Canonical Entity Types (10 Types)

1. `PERSON`: Suspects, aliases, handlers, operatives, callers.
2. `ORGANIZATION`: Syndicates, shell companies, hawala firms, agencies.
3. `PHONE_NUMBER`: Mobile, landline, VOIP numbers.
4. `EMAIL`: Email addresses.
5. `VEHICLE`: Vehicle registration / license plates.
6. `LOCATION`: Cities, ports, border crossings, addresses, jurisdictions.
7. `BANK_ACCOUNT`: Account numbers, IFSC codes, UPI handles.
8. `NATIONAL_ID`: Aadhaar numbers, PAN cards, passport numbers.
9. `WEAPON`: Firearms, ammunition, explosives.
10. `NARCOTIC`: Contraband substances (e.g., Heroin, Mephedrone, Meth).

### 4.3 Deterministic Normalization Rules

- **Phone Numbers:** Cleaned of punctuation and formatted to E.164 (e.g., `+91-9820012345`).
- **Vehicle Plates:** Uppercased, spaces/hyphens removed according to Indian RTO patterns (e.g., `MH12AB1234`).
- **Bank Accounts:** Alphanumeric cleaning, spaces stripped, uppercase.
- **National IDs:** PAN uppercased; Aadhaar normalized into standard 12-digit format.
- **Names:** Normalized to Title Case with extraneous honorifics cleaned.
- **Emails:** Lowercased and trimmed.

### 4.4 Canonical Relationship Types (12 Types)

1. `COMMUNICATED_WITH`: Phone calls, messages, email threads.
2. `TRANSFERRED_MONEY_TO`: Financial transactions, hawala transfers.
3. `ASSOCIATE_OF`: Gang membership, syndicate hierarchy, handler relationship.
4. `OPERATES`: Control of phone number, account, or facility.
5. `TRAVELLED_TO`: Movement across checkpoints, airports, locations.
6. `LOCATED_AT`: Residence, safehouse, dock, intercept point.
7. `OWNED_BY`: Vehicle ownership, asset ownership.
8. `MEMBER_OF`: Organization or cartel affiliation.
9. `TRAFFICKED`: Handling or smuggling narcotics/weapons.
10. `REPORTED_BY`: Informant or officer reporting source.
11. `INVOLVED_IN`: Direct involvement in an incident or case.
12. `PARENT_CASE`: Hierarchical case linking.

---

## 5. Staged Persistence & Investigator Review Interface

### 5.1 Staged Persistence (PostgreSQL)
Extractions are written to relational tables with foreign keys to `Evidence`:
- `ExtractedEntities`: Stores `RawValue`, `NormalizedValue`, `EntityType`, `ConfidenceScore`, `ReviewStatus` (`PENDING_REVIEW`, `APPROVED`, `REJECTED`, `EDITED`), `ReviewedByUserId`, `ReviewNotes`.
- `ExtractedRelationships`: Stores `SourceEntityId`, `TargetEntityId`, `RelationshipType`, `ConfidenceScore`, `ReviewStatus`.
- `ExtractedEvents`: Stores chronological events (`OCCURRED_AT`, timestamps, descriptions).

### 5.2 Review Actions

1. **Approve Entity:**
   - Marks status as `APPROVED`.
   - Promotes node to Neo4j graph with label `Entity` and `SourceEvidenceId`.
2. **Edit Entity:**
   - Updates `NormalizedValue`, `EntityType`, or `CanonicalName`.
   - Marks status as `EDITED` / `APPROVED`.
   - Promotes updated entity to Neo4j.
3. **Reject Entity:**
   - Marks status as `REJECTED`.
   - Node is **not** written to Neo4j. Staged record kept for audit compliance.
4. **Approve / Reject Relationship:**
   - When approved, queries or creates the source and target nodes in Neo4j and connects them with the typed edge, setting `sourceEvidenceId` and `confidence`.

---

## 6. Neo4j Graph Promotion & Provenance

When an item is approved:
- Node Cypher Query:
  ```cypher
  MERGE (e:Entity {id: $id})
  ON CREATE SET e.name = $name, e.type = $type, e.normalizedValue = $value, 
                e.sourceEvidenceId = $evidenceId, e.confidence = $confidence, 
                e.createdAt = datetime()
  ON MATCH SET e.name = $name, e.lastSeenAt = datetime()
  ```
- Relationship Cypher Query:
  ```cypher
  MATCH (a:Entity {id: $sourceId}), (b:Entity {id: $targetId})
  MERGE (a)-[r:RELATIONSHIP {type: $relType, sourceEvidenceId: $evidenceId}]->(b)
  ON CREATE SET r.confidence = $confidence, r.createdAt = datetime()
  ```
- **Provenance Integrity:** Every node and edge retains `sourceEvidenceId`, allowing investigators to click any graph element and instantly view the exact evidence file and cryptographic hash that established it.

---

## 7. REST API Endpoints

### 7.1 Evidence & Extraction Endpoints

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/v1/evidence` | Upload evidence file (multipart/form-data), compute SHA-256, queue for extraction. |
| `GET` | `/api/v1/evidence/{id}` | Get evidence details including version, status, and metadata. |
| `GET` | `/api/v1/evidence/{id}/verify-integrity` | Perform byte-level SHA-256 check (`VALID` or `TAMPERED`). |
| `GET` | `/api/v1/evidence/{id}/download` | Stream original evidence file bytes. |
| `POST` | `/api/v1/evidence/{id}/process` | Trigger or retry background extraction processing. |
| `GET` | `/api/v1/evidence/{id}/extraction` | Get extraction results, candidates, and review statuses. |
| `POST` | `/api/v1/evidence/{id}/extraction/approve` | Approve staged entity and promote to Neo4j. |
| `POST` | `/api/v1/evidence/{id}/extraction/reject` | Reject staged entity (prevent Neo4j promotion). |
| `PUT` | `/api/v1/evidence/{id}/extraction/entities/{entityId}/edit` | Edit entity attributes and promote to Neo4j. |
| `POST` | `/api/v1/evidence/{id}/extraction/relationships/approve` | Approve staged relationship and create edge in Neo4j. |
| `POST` | `/api/v1/evidence/{id}/extraction/relationships/reject` | Reject staged relationship. |

---

## 8. Frontend Interface

- **Route `/evidence/:evidenceId/extraction` (`ExtractionReviewPage.tsx`):**
  - Displays extraction summary metrics: Total Entities, High/Medium/Low Confidence, Approved/Pending/Rejected counts.
  - Interactive Entity Review Table: Shows Raw Text, Normalized Value, Type Tag, Confidence Meter, and Status Badge.
  - Quick action buttons: **Approve**, **Reject**, and **Edit** (with modal for correcting values).
  - Relationship Review Section: Inspect connected entities, relationship type, confidence, and approve/reject controls.
- **Route `/evidence/:evidenceId` (`EvidenceDetailPage.tsx`):**
  - Live SHA-256 Integrity Verification Card: Trigger real-time cryptographic audit with immediate visual status indicator (`VALID` in green or `TAMPERED` in red).
  - Evidence Metadata & Chain of Custody: File size, MIME type, version badge (`v1`, `v2`), uploader, timestamp.
  - Inline document viewer/preview and direct download.
- **Route `/evidence` (`EvidenceListPage.tsx`):**
  - Status badges (`UPLOADED`, `PROCESSING`, `REVIEW_REQUIRED`, `APPROVED`, etc.).
  - Direct links to "Review Extraction" and "Verify Hash".

---

## 9. Verification & Test Summary

- **C# .NET Backend Tests:** Unit tests in `backend/Tests/Unit/Phase2ExtractionAndReviewTests.cs` and `backend/Tests/Unit/CaseAndEvidenceServiceTests.cs`.
- **Python AI Engine Tests:** Unit tests in `backend/AI/tests/test_extraction.py` and `ai-service/tests/test_api.py`.
- **Frontend Validation:** `npm run typecheck` (0 errors), `npm run build` (successful production build).
