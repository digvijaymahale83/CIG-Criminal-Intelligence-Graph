# LOCAL DEPLOYMENT & RUNTIME GUIDE
**System:** The Criminal Intelligence & Network Investigation Platform  
**Target Architecture:** React (Port 3000) -> ASP.NET Core 8 (Port 5000) -> PostgreSQL (Port 5432) + Neo4j (Port 7687) -> Python FastAPI (Port 8000)  

---

## 1. Prerequisites & Environment

Before launching the system, verify that the following runtimes are installed on your host system:
- **Node.js:** v20.x or higher (`node --version`)
- **.NET SDK:** .NET 8.0 SDK (`dotnet --version`)
- **Java Runtime:** Eclipse Temurin OpenJDK 17 or higher (`java -version`, required for Neo4j)
- **PostgreSQL:** Version 16.x running on port `5432` with user `postgres` / password `postgres`
- **Neo4j:** Neo4j Community 5.20+ with Bolt on `7687` and HTTP on `7474` (auth: `neo4j` / `password123`)
- **Python:** Python 3.11+ with `pip` and virtual environment support

---

## 2. Ports Allocation Matrix

| Service | Port | Protocol | Primary Responsibilities |
|---|---|---|---|
| **React Web UI** | `3000` | HTTP | Investigative workspace, interactive Cytoscape knowledge graph, GIS Leaflet map |
| **ASP.NET Core Web API** | `5000` | HTTP | Core business logic, JWT authentication, EF Core data access, Neo4j Bolt driver |
| **PostgreSQL Database** | `5432` | TCP | Relational entity storage, evidence metadata, ledger blocks, immutable audit logs |
| **Neo4j Graph Database** | `7687` / `7474` | Bolt / HTTP | Graph topology, Cypher query execution, multi-hop relationship traversals |
| **Python FastAPI AI** | `8000` | HTTP | NLP/NER extraction, PyTorch GAT link prediction, Copilot contextual reasoning |

---

## 3. Step-by-Step Launch Sequence

### Step 1: Start PostgreSQL (Port 5432)
Ensure the PostgreSQL Windows Service or container is active:
```powershell
Get-Service -Name postgresql* | Start-Service
```
Verify connectivity:
```powershell
Test-NetConnection -ComputerName localhost -Port 5432
```
Database `criminal_network_db` should exist. If initializing fresh:
```powershell
# Create database if not exists
& "psql" -U postgres -c "CREATE DATABASE criminal_network_db;"
```

---

### Step 2: Start Neo4j Graph Database (Port 7687 / 7474)
Set Java 17 environment variables and launch Neo4j in console mode:
```powershell
$env:JAVA_HOME = "C:\Users\digvi\jdk17\jdk-17.0.12+7"
$env:PATH = "$env:JAVA_HOME\bin;" + $env:PATH
& "C:\Users\digvi\neo4j\neo4j-community-5.20.0\bin\neo4j.bat" console
```
Verify Bolt connectivity on port `7687`:
```powershell
Test-NetConnection -ComputerName localhost -Port 7687
```

---

### Step 3: Start Python AI FastAPI Service (Port 8000)
Launch the FastAPI uvicorn daemon:
```powershell
cd backend/AI
python -m uvicorn app.main:app --host 0.0.0.0 --port 8000
```
Verify health endpoint:
```powershell
curl http://localhost:8000/health
# Response: {"status":"healthy","service":"criminal-network-ai"}
```

---

### Step 4: Build & Start ASP.NET Core 8 Backend API (Port 5000)
Build the solution and run the API:
```powershell
cd backend
$env:ASPNETCORE_URLS = "http://localhost:5000"
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project API/API.csproj
```
Verify health endpoint:
```powershell
curl http://localhost:5000/api/v1/health
# Response: healthy
```

---

### Step 5: Start React Frontend (Port 3000)
Install dependencies and run the Vite dev server:
```powershell
npm install
npm run dev
```
Access the application in your browser at:
`http://localhost:3000`

---

## 4. Canonical Demo Credentials & Demo Case

- **Lead Investigator:**
  - **Email / Username:** `rajesh.sharma@mahapolice.gov.in` (`usr-dcp-sharma`)
  - **Password:** `Admin@123` (or any valid seeded mock password in Dev environment)
  - **Role:** `LEAD_INVESTIGATOR`
  - **Clearance:** `TOP_SECRET`
- **Target Demo Case:**
  - **Case Identifier:** `CASE-2026-0841` (`inv-2026-0841`)
  - **Title:** Operation Iron Vault Syndicate
  - **Location:** Pune Central & Mumbai Docks, Maharashtra

---

## 5. Troubleshooting Common Local Issues

1. **Port 5000 Conflict (Legacy Node Server):**
   - *Problem:* A legacy `node server/index.js` process is occupying port 5000.
   - *Fix:* Identify and terminate the Node process:
     ```powershell
     Get-Process -Name node | Stop-Process -Force
     ```
2. **Neo4j Bolt Authentication Failure:**
   - *Problem:* Backend logs report `Neo.ClientError.Security.Unauthorized`.
   - *Fix:* Ensure Neo4j credentials match `backend/API/appsettings.json`:
     `"Neo4j": { "Uri": "bolt://localhost:7687", "User": "neo4j", "Password": "password123" }`.
3. **Evidence Upload 405 Method Not Allowed:**
   - *Problem:* Upload fails because frontend sends `application/json` instead of multipart form data.
   - *Fix:* Ensure requests use `apiClient.post` with raw `FormData` without overriding `Content-Type`.
4. **Browser CDP Debugging Port Conflict (Port 9222):**
   - *Problem:* Windows OEM software (e.g. Lenovo Vantage) occupies port 9222.
   - *Fix:* Run automated browser audits on an alternative port (e.g. `msedge.exe --remote-debugging-port=9360`).
