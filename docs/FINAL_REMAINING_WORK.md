# REMAINING WORK & PRODUCTION READINESS ROADMAP
**Platform:** The Criminal Intelligence & Network Investigation Platform  
**Target Scope:** Post-Phase 11 Production Hardening  
**Audit Date:** September 9, 2026  

---

## 1. Overview & Scope Clarification

All features within the planned Phases 1–11 development scope have been fully implemented, integrated, and verified against live backend and database services. Per explicit instructions, **Phase 12 has not been created or implemented**. 

The items below represent production-grade enhancements, enterprise clustering, and operational hardening recommendations required before nationwide deployment across state police forces.

---

## 2. Categorized Remaining Work

### Priority: CRITICAL (Pre-Deployment Hardening)
1. **Ledger Block Microsecond Normalization in Seeders:**
   - *Issue:* In .NET, `DateTime.UtcNow` generates 7 fractional digits (`.fffffff`), whereas PostgreSQL `timestamptz` persists 6 microsecond digits (`.ffffff`). Seed data created in-memory before database round-tripping exhibits subtle sub-microsecond tick differences during verification.
   - *Action:* Update `IntegrityLedgerService.AppendBlockAsync` and `DbInitializer` to truncate all in-memory timestamps to microsecond precision (`(ticks / 10) * 10`) prior to canonical string hashing.
2. **Production JWT Key Rotation & Refresh Token Store:**
   - *Issue:* The current implementation uses symmetric secret signing in `appsettings.json` with in-memory token expiry.
   - *Action:* Integrate asymmetric RSA / ECDSA signing keys managed via Azure Key Vault or HashiCorp Vault, coupled with a Redis-backed refresh token revocation blacklist.

---

### Priority: HIGH (Security & Operational Resilience)
1. **Distributed Concurrency Lock for Ledger Appends:**
   - *Issue:* Currently, `IntegrityLedgerService` uses an in-process `SemaphoreSlim` to serialize block creation.
   - *Action:* In a multi-instance load-balanced ASP.NET Core cluster, replace the in-process semaphore with a distributed database lock (`pg_advisory_xact_lock`) or Redis distributed redlock to prevent concurrent ledger branching.
2. **Secure Object Storage Integration for Evidence:**
   - *Issue:* Uploaded evidence files are currently stored on the local file system under `uploads/`.
   - *Action:* Implement an `S3CompatibleFileStorageService` with AES-256 server-side encryption (SSE-KMS) and WORM (Write-Once-Read-Many) bucket immutability policies for legal compliance.

---

### Priority: MEDIUM (Performance & User Experience)
1. **Offline Map Raster Tile Caching:**
   - *Issue:* The Leaflet map currently requests OpenStreetMap tiles over the public internet. In restricted police intranets (CCTNS / secure enclaves), internet connectivity may be disabled.
   - *Action:* Package pre-rendered vector/raster MBTiles covering Maharashtra and national territory, served directly from the ASP.NET Core API or local tile server.
2. **GPU-Accelerated Model Serving for Graph Neural Networks:**
   - *Issue:* PyTorch GAT link prediction and SpaCy NER currently run on CPU in the FastAPI process.
   - *Action:* Package PyTorch models using ONNX Runtime or Triton Inference Server with CUDA acceleration for sub-50ms inference on multi-thousand-node graphs.

---

### Priority: LOW (Administrative Convenience)
1. **Custom Marathi Translation Overrides in UI:**
   - *Issue:* Marathi strings are stored in static JSON bundles (`mr/common.json`).
   - *Action:* Provide a police department terminology management console allowing intelligence officers to customize local police dialect translations on the fly.
2. **Automated End-to-End Cypress / Playwright Pipeline:**
   - *Issue:* Automated end-to-end tests are currently executed via custom Chrome DevTools Protocol (CDP) test scripts.
   - *Action:* Formalize these scripts into a standard Playwright test harness running in GitHub Actions / GitLab CI.
