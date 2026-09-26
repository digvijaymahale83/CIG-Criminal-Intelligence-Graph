# Phase 9 — Evidence Integrity Ledger / Permissioned Blockchain Subsystem

**The Criminal Intelligence & Network Investigation Platform**  
*Synthetic SIH Research Prototype*

---

## 1. Executive Summary

Phase 9 establishes a cryptographically verifiable, tamper-evident, append-only **Evidence Integrity Ledger** for digital forensic evidence management. Designed specifically for law enforcement investigations (e.g., Maharashtra Police SIH scenarios), it guarantees immutable chain-of-custody tracking without reliance on public cryptocurrencies, tokenomics, mining, or financial blockchain overhead.

### Key Principles
1. **Off-Chain Physical Storage, On-Chain Cryptographic Fingerprints**: Raw evidence files (telecom CDRs, interception transcripts, surveillance logs) reside securely in dedicated disk/object storage (`uploads/evidence/`). The ledger exclusively stores SHA-256 digests, block hashes, chain linkages, and custody metadata.
2. **Genuine Byte-Level Runtime Recalculation**: No fake or mock hashes exist anywhere in the system. Every verification streams the physical file bytes from disk, recomputes SHA-256 using standard cryptographic libraries, and compares the result against registered metadata and immutable ledger blocks.
3. **Strict Append-Only Immutability**: Historical blocks cannot be updated, edited, or deleted. Corrections, re-verifications, or new evidence versions append subsequent blocks, preserving a permanent audit trail.
4. **Deterministic Canonical Serialization**: Block hashes are derived from an explicit, culture-invariant canonical serialization format, guaranteeing bit-level reproducibility across server restarts.
5. **Neutral Investigative Terminology**: Evidence integrity verifies whether stored bytes match their registered cryptographic fingerprint; it establishes custodial integrity, but never infers guilt, intent, or culpability.

---

## 2. Architecture & Data Flow

```
+-----------------------------------------------------------------------------------+
|                            EVIDENCE UPLOAD & REGISTRATION                         |
+-----------------------------------------------------------------------------------+
      Investigator uploads evidence file
                     │
                     ▼
      Stream physical bytes through SHA-256 Engine
                     │
                     ▼
      Sha256Hex (64 lowercase hex characters)
                     │
                     ▼
      Persist physical file in Storage (`uploads/evidence/YYYY-MM/...`)
                     │
                     ▼
      Insert `Evidence` database record
                     │
                     ▼
      `IIntegrityLedgerService.AppendBlockAsync(...)`
                     │
            ┌────────┴──────────────────────────┐
            │ Concurrency Semaphore (1, 1)      │
            │ PostgreSQL Transaction            │
            └────────┬──────────────────────────┘
                     │
                     ▼
      Query latest block (Index N, BlockHash H_N)
                     │
                     ▼
      Build Canonical String:
      (N+1)|EvidenceId|Version|EvidenceHash|H_N|UPLOAD|Actor|TimestampIso8601|Metadata
                     │
                     ▼
      Compute H_{N+1} = SHA256(UTF-8 canonical bytes)
                     │
                     ▼
      Insert `EvidenceLedgerBlock` (Block #N+1)
                     │
                     ▼
      Log `LEDGER_BLOCK_APPENDED` in Audit Trail
```

```
+-----------------------------------------------------------------------------------+
|                        LIVE RUNTIME BYTE-LEVEL VERIFICATION                       |
+-----------------------------------------------------------------------------------+
      Investigator clicks [Verify Now]
                     │
                     ▼
      1. Load Evidence record & latest `EvidenceLedgerBlock`
                     │
                     ▼
      2. Stream raw bytes from disk (`IFileStorageService.GetFileAsync`)
         If file missing ──> Return `MISSING_FILE`
                     │
                     ▼
      3. Compute live SHA-256 from byte stream
                     │
                     ▼
      4. Run complete blockchain validation (`VerifyChainAsync`)
         If chain broken ──> Return `CHAIN_INVALID`
                     │
                     ▼
      5. Cross-Check Fingerprints:
         - If LiveFileSha256 != Evidence.Sha256Hash ──> `EVIDENCE_MODIFIED`
         - If LiveFileSha256 != LedgerBlock.EvidenceHash ──> `LEDGER_MISMATCH`
         - If All Match and Chain Valid ──> `VERIFIED`
                     │
                     ▼
      Log Audit Entry (`INTEGRITY_VERIFICATION_SUCCESS` or `INTEGRITY_VERIFICATION_FAILED`)
```

---

## 3. Threat Model & Security Properties

| Threat | System Defense Mechanism |
|---|---|
| **Direct File Tampering on Disk** | Live runtime verification recalculates SHA-256 from raw disk bytes. Any byte modification immediately yields `EVIDENCE_MODIFIED` while the ledger record remains immutable. |
| **Direct Database Mutation of Block Hash** | `VerifyChainAsync` recomputes canonical hashes for every block from Genesis to tip. A modified block hash causes immediate validation failure at that exact block index. |
| **Altering Historical Custody Links** | Modifying `PreviousBlockHash` in an earlier block breaks the hash pointer link with subsequent blocks, triggering `PreviousBlockHash mismatch` detection. |
| **Concurrent Append Race Conditions** | Internal `SemaphoreSlim(1, 1)` and database transactions ensure block indexing and chain linkage remain strictly sequential with no duplicate indices or orphan blocks. |
| **Unauthorized Block Editing/Deletion** | Application API exposes zero `PUT`, `PATCH`, or `DELETE` endpoints for ledger blocks. The EF Core entity configuration sets `DeleteBehavior.Restrict` on foreign keys. |
| **Fake or Placeholder Hashes** | Prohibited repository-wide. All demo and test evidence files physically exist on disk and match their byte hashes. |

---

## 4. Hashing & Canonicalization Design

### Canonical Block Serialization Format
To eliminate culture-specific variations, JSON property reordering, or formatting inconsistencies, each block is serialized into a deterministic pipe-delimited UTF-8 string:

$$\text{CanonicalString} = \text{BlockIndex} \mid \text{EvidenceId} \mid \text{Version} \mid \text{EvidenceHash} \mid \text{PreviousBlockHash} \mid \text{Action} \mid \text{ActorUserId} \mid \text{TimestampIso8601} \mid \text{MetadataJson}$$

- **Timestamp**: Invariant ISO 8601 round-trip string (`yyyy-MM-ddTHH:mm:ss.fffffffZ`).
- **Null Fields**: Coalesced to empty string (`""`).
- **Hashes**: Normalized to 64-character lowercase hexadecimal.
- **BlockHash Computation**: `Convert.ToHexString(SHA256(UTF8(CanonicalString))).ToLowerInvariant()`.

### Deterministic Genesis Block (Block #0)
Block #0 is anchored with constant parameters:
- `BlockIndex = 0`
- `EvidenceItemId = null`
- `EvidenceVersion = 0`
- `EvidenceHash = "genesis"`
- `PreviousBlockHash = "genesis"`
- `Action = "GENESIS"`
- `ActorUserId = "SYSTEM"`
- `ActorName = "System Genesis Authority"`
- `TimestampUtc = 2026-01-01T00:00:00.0000000Z`
- `MetadataJson = "{\"genesis\":\"Maharashtra Police Evidence Integrity Ledger Genesis Block\",\"network\":\"SIH-2026-RESEARCH\"}"`

The Genesis hash is computed using the canonical formula and remains 100% constant across application restarts.

---

## 5. Domain Model & Constraints

### Entity: `EvidenceLedgerBlock` (`backend/Domain/Entities/EvidenceLedgerBlock.cs`)
- `Id`: GUID string (primary key)
- `BlockIndex`: Sequential integer (0-indexed, unique constraint)
- `EvidenceItemId`: Foreign key to `Evidence` (nullable for Genesis, `DeleteBehavior.Restrict`)
- `EvidenceVersion`: Version number of the evidence item (1, 2, ...)
- `EvidenceHash`: 64-char lowercase SHA-256 hexadecimal string
- `PreviousBlockHash`: 64-char lowercase SHA-256 hexadecimal string
- `BlockHash`: 64-char lowercase SHA-256 hexadecimal string (unique constraint)
- `Action`: Enum string (`GENESIS`, `UPLOAD`, `VERSION_CREATED`, `EVIDENCE_VERIFIED`, `INTEGRITY_CHECK`, `RECONCILIATION`)
- `ActorUserId`: User identifier or `"SYSTEM"`
- `ActorName`: Display name of recording officer
- `TimestampUtc`: Event timestamp in UTC
- `MetadataJson`: Structured audit metadata
- `CreatedAtUtc`: Record creation timestamp

---

## 6. REST API Reference

| Method | Route | Access | Purpose |
|---|---|---|---|
| `GET` | `/api/v1/evidence/{evidenceId}/integrity` | Investigator / Analyst / Admin | Fast check using registered metadata and latest ledger block |
| `GET` | `/api/v1/evidence/{evidenceId}/integrity/history` | Investigator / Analyst / Admin | Retrieves complete chronological block custody timeline for evidence |
| `POST` | `/api/v1/evidence/{evidenceId}/integrity/verify` | Investigator / Analyst / Admin | Live runtime byte verification reading physical disk file |
| `GET` | `/api/v1/integrity/ledger` | Investigator / Admin | Paginated ledger blocks with custody metadata |
| `GET` | `/api/v1/integrity/ledger/{blockIndex}` | Investigator / Admin | Retrieves single block details by index |
| `POST` | `/api/v1/integrity/ledger/verify-chain` | Supervisor / Admin | Audits full chain integrity from Block #0 to tip |
| `GET` | `/api/v1/integrity/reconciliation` | Supervisor / Admin | Lists unledgered evidence items pending registration |
| `POST` | `/api/v1/integrity/reconciliation/{evidenceId}` | Supervisor / Admin | Safely reconciles and registers unledgered evidence |

---

## 7. Automated Test Verification Results

All 15 automated test specifications in `backend/Tests/Unit/Phase9IntegrityLedgerTests.cs` passed with zero failures:

1. `Test01_GenesisBlock_CreatedDeterministically`: Confirms stable Genesis block creation and idempotent initialization.
2. `Test02_CanonicalHashCalculation_DeterministicAcrossOrder`: Asserts deterministic canonical block hashing.
3. `Test03_BlockAppend_SequentialChaining`: Validates sequential block indexing and previous-hash pointer linking.
4. `Test04_ChainValidation_SucceedsOnValidChain`: Validates unbroken chains from Genesis to latest block.
5. `Test05_TamperDetection_ModifiedStoredBlockHash_FailsChainValidation`: Confirms modifying a database block hash immediately triggers chain failure at that index.
6. `Test06_TamperDetection_ModifiedPreviousHash_FailsChainValidation`: Confirms modifying previous-hash pointer triggers broken-chain failure.
7. `Test07_EvidenceVerification_AuthenticFile_ReturnsVerified`: Verifies real physical file matches registered and ledger hashes.
8. `Test08_TamperDetection_ModifiedFileBytes_DetectedAndLedgerIntact`: Demonstrates authentic file -> tamper file bytes -> `EVIDENCE_MODIFIED` detected -> ledger intact -> restore bytes -> `VERIFIED`.
9. `Test09_EvidenceVerification_MissingFile_ReturnsMissingFile`: Confirms `MISSING_FILE` status when physical disk file is absent.
10. `Test10_EvidenceVerification_MissingLedgerBlock_ReturnsMissingLedgerRecord`: Confirms `MISSING_LEDGER_RECORD` for unledgered evidence.
11. `Test11_VersionAwareIntegrity_MultipleVersionsHaveDistinctBlocks`: Validates versioned evidence (v1 and v2) has independent, immutable ledger blocks.
12. `Test12_Reconciliation_IdentifiesAndRegistersMissingEvidence`: Identifies unregistered evidence and registers ledger block under audit.
13. `Test13_AppendConcurrency_20ConcurrentRequests_ProducesValidLinearChain`: 20 concurrent threads produce 20 sequential blocks with zero duplicates or corrupted links.
14. `Test14_AuditLogging_LogsVerificationAndTamperAlerts`: Asserts audit logs are recorded for appends, successful verifications, and tamper alerts.
15. Backward compatibility: All 131 prior Phase 1–8 .NET tests and 50 Python AI service tests continue to pass with 0 failures.

---

## 8. Limitations & Scope Boundaries

1. **Permissioned Ledger Model**: This is an internal, cryptographically chained permissioned audit ledger stored in PostgreSQL, not a public consensus network (Ethereum, Bitcoin, Hyperledger Fabric).
2. **Local Storage Bound**: Evidence files reside on the application server or configured object store. Physical storage disk failure must be mitigated with RAID/cloud replication.
3. **No Automatic Legal Inference**: The ledger guarantees cryptographic non-repudiation and tamper detection; it does not substantiate guilt or determine judicial outcomes.
