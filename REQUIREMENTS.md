# System & Software Requirements Specification

## Criminal Intelligence & Network Investigation Platform (CINIP)
**Operational Security Level:** Restricted Law Enforcement / Criminal Investigation Use  
**Target Environment:** Local Workstation / Air-gapped Command Center / On-Premise Staging Server

---

## 1. System Architecture Overview

The platform operates as a multi-tier distributed intelligence system:

```
┌─────────────────────────────────────────────────────────────┐
│               Vite + React 18 Web UI                        │
│          (Port 3000 | TypeScript | Cytoscape | Leaflet)     │
└──────────────────────────────┬──────────────────────────────┘
                               │ HTTP / JSON API
                               ▼
┌─────────────────────────────────────────────────────────────┐
│             C# ASP.NET Core 8 Web API                       │
│    (Port 5000 | CQRS | EF Core | JWT Auth | Audit Ledger)   │
└──────────────┬───────────────────────────────┬──────────────┘
               │                               │
        gRPC / HTTP                      Bolt Protocol
               │                               │
               ▼                               ▼
┌─────────────────────────────┐ ┌─────────────────────────────┐
│  Python FastAPI AI Service  │ │    Neo4j Graph Database     │
│   (Port 8000 | NLP | OCR)   │ │  (Port 7687 Bolt, 7474 HTTP)│
└──────────────┬──────────────┘ └─────────────────────────────┘
               │
               ▼
┌─────────────────────────────┐
│ PostgreSQL 16 + pgvector    │
│ (Port 5432 | Entities/Cases)│
└─────────────────────────────┘
```

---

## 2. Hardware Requirements

| Specification | Minimum Requirement | Recommended (Production / Investigation) |
| :--- | :--- | :--- |
| **Processor (CPU)** | 4-Core x64 (Intel Core i5 / AMD Ryzen 5) | 8-Core+ x64 (Intel Core i7/i9, AMD Ryzen 7/9) |
| **System Memory (RAM)** | 16 GB | 32 GB – 64 GB (for large graph topologies) |
| **Disk Space** | 20 GB free SSD storage | 100 GB+ NVMe SSD (for evidentiary files & graph stores) |
| **Network** | Loopback / 1 Gbps Local LAN | 10 Gbps LAN (for multi-investigator deployments) |
| **Display Resolution** | 1366 × 768 | 1920 × 1080 or higher (dual display recommended) |

---

## 3. Operating System Support

- **Windows:** Windows 10 (21H2+) or Windows 11 (64-bit)
- **Linux:** Ubuntu 22.04 LTS or Debian 12 (x86_64)
- **macOS:** macOS Sonoma (14.x) or higher (Apple Silicon / Intel)

---

## 4. Software & Runtime Prerequisites

Ensure the following runtimes and compilers are installed and registered in your system `PATH`:

### 4.1. Node.js & npm (Frontend Toolchain)
- **Version:** Node.js `v18.18.0+` or `v20.x LTS` (Active LTS recommended)
- **Package Manager:** `npm v9.0+` (or `bun v1.1+`)
- **Verification:**
  ```powershell
  node --version
  npm --version
  ```

### 4.2. .NET 8.0 SDK (Backend Web API)
- **Version:** Microsoft .NET 8.0 SDK (`net8.0`)
- **Runtime:** ASP.NET Core 8.0 Runtime
- **Verification:**
  ```powershell
  dotnet --version
  # Should return 8.0.xxx
  ```

### 4.3. Python 3.10 / 3.11 (AI & Extraction Engine)
- **Version:** Python `3.10.x` or `3.11.x` (64-bit)
- **Package Installer:** `pip 23.0+`
- **Verification:**
  ```powershell
  python --version
  pip --version
  ```

### 4.4. PostgreSQL Relational & Vector Database
- **Version:** PostgreSQL `15.x` or `16.x`
- **Extensions Required:** `pgvector`, `uuid-ossp`
- **Default Port:** `5432`
- **Verification:**
  ```powershell
  psql -U postgres -c "SELECT version();"
  ```

### 4.5. Neo4j Graph Database
- **Version:** Neo4j Community or Enterprise `5.18+` or `5.20.0+`
- **Plugins:** APOC (Awesome Procedures on Cypher) Core `5.x`
- **Ports:** `7687` (Bolt), `7474` (HTTP Management Console)
- **Java Runtime Requirement:** OpenJDK 17 LTS or Eclipse Temurin 17 (`JAVA_HOME` must point to JDK 17)
- **Verification:**
  ```powershell
  java -version
  # OpenJDK 64-Bit Server VM (build 17.x)
  ```

### 4.6. Optical Character Recognition (OCR Engine)
- **Engine:** Tesseract OCR `v5.0+` (optional for scanned image/FIR evidence text parsing)
- **Installation:**
  - *Windows:* [UB-Mannheim Tesseract Installer](https://github.com/UB-Mannheim/tesseract/wiki)
  - *Ubuntu:* `sudo apt-get install tesseract-ocr tesseract-ocr-mar tesseract-ocr-hin`

---

## 5. Network Port Allocations

| Service | Port | Protocol | Purpose |
| :--- | :--- | :--- | :--- |
| **Vite Frontend Dev Server** | `3000` | HTTP | React Web User Interface |
| **ASP.NET Core 8 Backend** | `5000` | HTTP / REST | Primary Business Logic, CQRS, Auth & Audit |
| **FastAPI Python AI Engine** | `8000` | HTTP / REST | NLP, Entity Extraction, GAT Inference |
| **PostgreSQL Database** | `5432` | TCP | Relational Store, Evidence Metadata, Audit Logs |
| **Neo4j Bolt Protocol** | `7687` | Bolt | High-Performance Graph Queries & Cypher Execution |
| **Neo4j Browser Console** | `7474` | HTTP | Neo4j Administrator Web Interface |

---

## 6. Project Dependencies Breakdown

### 6.1. Frontend (`package.json`)
- **Framework:** `react ^18.3.1`, `react-dom ^18.3.1`
- **Build Tool:** `vite ^6.2.0`, `@vitejs/plugin-react ^4.3.4`
- **Language:** `typescript ^5.5.3`
- **Styling:** `tailwindcss ^4.0.0-alpha.32`, `@tailwindcss/vite`
- **Data Visualization & Graph:**
  - `cytoscape ^3.31.2`
  - `leaflet ^1.9.4`, `react-leaflet ^4.2.1`
  - `lucide-react ^1.16.0`
- **Server State & Routing:**
  - `@tanstack/react-query ^5.83.0`
  - `react-router-dom ^7.3.0`
  - `axios ^1.7.9`
- **Testing:** `vitest ^3.0.7`, `jsdom ^26.0.0`

### 6.2. Backend API (.NET 8 C#)
- `Npgsql.EntityFrameworkCore.PostgreSQL ^8.0.0`
- `Neo4j.Driver ^5.18.0`
- `Microsoft.AspNetCore.Authentication.JwtBearer ^8.0.0`
- `FluentValidation.AspNetCore ^11.3.0`
- `MediatR ^12.2.0`
- `BCrypt.Net-Next ^4.0.3`
- `Swashbuckle.AspNetCore ^6.5.0`

### 6.3. AI Microservice (Python `requirements.txt`)
- `fastapi>=0.110.0`
- `uvicorn[standard]>=0.29.0`
- `pydantic>=2.6.4`
- `python-dotenv>=1.0.1`
- `requests>=2.31.0`
- `httpx>=0.27.0`
- `pymupdf>=1.24.0` (fitz for PDF extraction)
- `openpyxl>=3.1.2` (Excel CDR/tower dumps)
- `pillow>=10.2.0` (Image evidence processing)
- `pytesseract>=0.3.10` (OCR extraction)
- `numpy>=1.24.3`

---

## 7. Environment Variables (`.env`)

Create a `.env` file in the project root with the following keys:

```env
# Application Environment
ASPNETCORE_ENVIRONMENT=Development
ASPNETCORE_URLS=http://localhost:5000

# PostgreSQL Connection
POSTGRES_HOST=localhost
POSTGRES_PORT=5432
POSTGRES_DB=criminal_network_db
POSTGRES_USER=postgres
POSTGRES_PASSWORD=postgres
ConnectionStrings__DefaultConnection=Host=localhost;Port=5432;Database=criminal_network_db;Username=postgres;Password=postgres

# Neo4j Graph Database Connection
NEO4J_URI=bolt://localhost:7687
NEO4J_USER=neo4j
NEO4J_PASSWORD=InvestigateNow2026!
NEO4J_DATABASE=neo4j

# Python AI Service
AI_SERVICE_URL=http://localhost:8000
PYTHONPATH=.

# Security & Cryptographic Ledger
JWT_SECRET_KEY=SuperSecretKeyForDevelopmentTestingOnly2026!Min32Chars
JWT_ISSUER=CinipAuthService
JWT_AUDIENCE=CinipInvestigationPlatform
LEDGER_HMAC_KEY=CinipCryptographicIntegrityLedgerSecret2026!

# Evidence Storage Directory
EVIDENCE_STORAGE_PATH=./uploads/evidence
```

---

## 8. Quick Start Verification Commands

To verify that all system components and requirements are met:

```powershell
# 1. Install & Test Frontend
npm install
npm run test
npm run build

# 2. Build & Verify C# Backend
dotnet build backend/CriminalNetworkAnalysis.sln

# 3. Verify Python AI Dependencies
pip install -r requirements.txt
python -m uvicorn backend.AI.app.main:app --port 8000 --host 127.0.0.1
```
