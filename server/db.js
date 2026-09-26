/**
 * db.js — SQLite database setup & seed for Maharashtra Police CORTEX
 */
const Database = require('better-sqlite3');
const bcrypt = require('bcryptjs');
const path = require('path');

const DB_PATH = path.join(__dirname, 'cortex_mhp.db');
const db = new Database(DB_PATH);

// Enable WAL mode for better concurrent read performance
db.pragma('journal_mode = WAL');
db.pragma('foreign_keys = ON');

// ─── Schema ───────────────────────────────────────────────────────────────────

db.exec(`
  CREATE TABLE IF NOT EXISTS users (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    email TEXT UNIQUE NOT NULL,
    password_hash TEXT NOT NULL,
    badge_number TEXT,
    role TEXT NOT NULL DEFAULT 'Investigator',
    agency TEXT,
    rank TEXT,
    unit TEXT,
    created_at TEXT DEFAULT (datetime('now'))
  );

  CREATE TABLE IF NOT EXISTS investigations (
    id TEXT PRIMARY KEY,
    case_number TEXT UNIQUE NOT NULL,
    title TEXT NOT NULL,
    description TEXT,
    classification TEXT DEFAULT 'RESTRICTED',
    priority TEXT DEFAULT 'Medium',
    status TEXT DEFAULT 'Active',
    category TEXT,
    jurisdiction TEXT,
    district TEXT,
    lead_officer_id TEXT,
    evidence_count INTEGER DEFAULT 0,
    entity_count INTEGER DEFAULT 0,
    finding_count INTEGER DEFAULT 0,
    fir_number TEXT,
    police_station TEXT,
    created_at TEXT DEFAULT (datetime('now')),
    updated_at TEXT DEFAULT (datetime('now')),
    FOREIGN KEY (lead_officer_id) REFERENCES users(id)
  );

  CREATE TABLE IF NOT EXISTS entities (
    id TEXT PRIMARY KEY,
    entity_id TEXT UNIQUE NOT NULL,
    name TEXT NOT NULL,
    type TEXT NOT NULL,
    risk TEXT DEFAULT 'LOW',
    description TEXT,
    location TEXT,
    district TEXT,
    linked_investigation_id TEXT,
    phone TEXT,
    aadhaar_masked TEXT,
    vehicle_number TEXT,
    account_number TEXT,
    bank TEXT,
    created_at TEXT DEFAULT (datetime('now')),
    FOREIGN KEY (linked_investigation_id) REFERENCES investigations(id)
  );

  CREATE TABLE IF NOT EXISTS graph_nodes (
    id TEXT PRIMARY KEY,
    label TEXT NOT NULL,
    type TEXT NOT NULL,
    risk TEXT DEFAULT 'LOW',
    description TEXT,
    location TEXT,
    entity_id TEXT,
    investigation_id TEXT
  );

  CREATE TABLE IF NOT EXISTS graph_edges (
    id TEXT PRIMARY KEY,
    source TEXT NOT NULL,
    target TEXT NOT NULL,
    label TEXT,
    investigation_id TEXT,
    FOREIGN KEY (source) REFERENCES graph_nodes(id),
    FOREIGN KEY (target) REFERENCES graph_nodes(id)
  );

  CREATE TABLE IF NOT EXISTS evidence (
    id TEXT PRIMARY KEY,
    investigation_id TEXT NOT NULL,
    filename TEXT NOT NULL,
    file_type TEXT,
    description TEXT,
    hash_sha256 TEXT,
    uploaded_by TEXT,
    clearance TEXT DEFAULT 'RESTRICTED',
    created_at TEXT DEFAULT (datetime('now')),
    FOREIGN KEY (investigation_id) REFERENCES investigations(id)
  );

  CREATE TABLE IF NOT EXISTS audit_log (
    id TEXT PRIMARY KEY,
    user_id TEXT,
    user_name TEXT,
    action TEXT NOT NULL,
    resource_type TEXT,
    resource_id TEXT,
    details TEXT,
    ip_address TEXT,
    timestamp TEXT DEFAULT (datetime('now'))
  );

  CREATE TABLE IF NOT EXISTS chat_sessions (
    id TEXT PRIMARY KEY,
    user_id TEXT,
    investigation_id TEXT,
    messages TEXT DEFAULT '[]',
    created_at TEXT DEFAULT (datetime('now')),
    updated_at TEXT DEFAULT (datetime('now'))
  );
`);

// ─── Seed Data ────────────────────────────────────────────────────────────────

function seedIfEmpty() {
  const userCount = db.prepare('SELECT COUNT(*) as cnt FROM users').get().cnt;
  if (userCount > 0) return; // Already seeded

  console.log('[DB] Seeding Maharashtra Police CORTEX database...');

  const hash = bcrypt.hashSync('Maharashtra@2024', 10);

  // ── Officers ──────────────────────────────────────────────────────────────
  const insertUser = db.prepare(`
    INSERT INTO users (id, name, email, password_hash, badge_number, role, agency, rank, unit)
    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
  `);

  const users = [
    ['usr-001', 'DCP Rajesh Sharma', 'dcp.sharma@mahapolice.gov.in', hash, 'DCP-MH-4821', 'Administrator', 'Maharashtra Police — Crime Branch, Mumbai', 'Deputy Commissioner of Police', 'Organized Crime Unit'],
    ['usr-002', 'PI Priya Deshmukh', 'pi.deshmukh@mahapolice.gov.in', hash, 'PI-MH-3312', 'Investigator', 'Maharashtra Police — CID', 'Police Inspector', 'Financial Crimes Division'],
    ['usr-003', 'ASI Vikram Patil', 'asi.patil@mahapolice.gov.in', hash, 'ASI-MH-7741', 'Analyst', 'Maharashtra Police — Cyber Cell', 'Assistant Sub-Inspector', 'Cyber Crime Unit'],
    ['usr-004', 'SP Anjali Kulkarni', 'sp.kulkarni@mahapolice.gov.in', hash, 'SP-MH-2290', 'Supervisor', 'Maharashtra Police — ATS', 'Superintendent of Police', 'Anti-Terrorism Squad'],
  ];

  for (const u of users) insertUser.run(...u);

  // ── Investigations ────────────────────────────────────────────────────────
  const insertInv = db.prepare(`
    INSERT INTO investigations (id, case_number, title, description, classification, priority, status, category, jurisdiction, district, lead_officer_id, evidence_count, entity_count, finding_count, fir_number, police_station, updated_at)
    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
  `);

  const investigations = [
    ['inv-001', 'MH-CR-2024-0841', 'Operation Hawala Nexus — Mumbai Port Trust Fraud', 'Multi-layered hawala network operating through Mumbai Port Trust contractors. Suspected ₹42 Cr unaccounted transfers via 6 shell entities in Dharavi and Andheri East.', 'CONFIDENTIAL', 'Critical', 'Active', 'Financial Crime', 'Mumbai Crime Branch + ED Liaison', 'Mumbai City', 'usr-002', 28, 87, 11, 'CR/0841/2024', 'Dharavi Police Station', '2026-09-06T18:30:00Z'],
    ['inv-002', 'MH-CYB-2024-0912', 'Operation Dark Net — Pune Cyber Fraud Ring', 'Organised cyber fraud network operating from Hadapsar, Pune. SIM swapping, OTP fraud, and digital payment mule accounts targeting elderly citizens across Maharashtra.', 'RESTRICTED', 'High', 'Active', 'Cyber Crime', 'Pune Cyber Cell + CERT-In', 'Pune', 'usr-003', 42, 134, 18, 'CYB/0912/2024', 'Hadapsar Police Station', '2026-09-05T14:15:00Z'],
    ['inv-003', 'MH-NDPS-2024-1044', 'Operation Blue Tide — Coastal Narcotics Network', 'Narcotics smuggling network via Maharashtra coastline (Raigad to Sindhudurg). Methamphetamine and brown sugar consignments intercepted. Cross-border links to Gujarat and Goa.', 'SECRET', 'Critical', 'Active', 'NDPS', 'ATS Maharashtra + NCB Mumbai Zonal Unit', 'Raigad', 'usr-004', 63, 212, 27, 'NDPS/1044/2024', 'Alibag Police Station', '2026-09-07T06:00:00Z'],
    ['inv-004', 'MH-ORG-2024-0523', 'Operation Iron Syndicate — Nagpur Extortion Network', 'Organised crime syndicate operating under MCOCA. Extortion network targeting construction industry in Nagpur and Vidarbha. 14 accused identified; 6 absconding.', 'RESTRICTED', 'High', 'Under Review', 'Organised Crime (MCOCA)', 'Nagpur Crime Branch', 'Nagpur', 'usr-001', 19, 58, 9, 'OR/0523/2024', 'Sitabuldi Police Station', '2026-09-04T10:00:00Z'],
    ['inv-005', 'MH-CR-2024-0317', 'Operation Counterfeit Shield — Nashik Currency Syndicate', 'Multi-district counterfeit Indian currency note (FICN) operation. Printing press located in Nashik Road industrial zone. Distribution network covering Nashik, Aurangabad, and Jalgaon.', 'CONFIDENTIAL', 'Medium', 'Under Review', 'Financial Crime', 'Nashik CID + NIA Liaison', 'Nashik', 'usr-002', 31, 44, 6, 'CR/0317/2024', 'Nashik Road Police Station', '2026-09-02T09:00:00Z'],
  ];

  for (const inv of investigations) insertInv.run(...inv);

  // ── Entities ──────────────────────────────────────────────────────────────
  const insertEntity = db.prepare(`
    INSERT INTO entities (id, entity_id, name, type, risk, description, location, district, linked_investigation_id, phone, vehicle_number, account_number, bank)
    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
  `);

  const entities = [
    ['ent-001', 'P-MH-00182', 'Dawood Mirza Shaikh', 'Person', 'CRITICAL', 'Primary hawala operator. Connected to 4 shell companies. Active warrant under PMLA.', 'Dharavi, Mumbai', 'Mumbai City', 'inv-001', '+91 98205 44110', null, null, null],
    ['ent-002', 'ORG-MH-77213', 'Radiant Traders Pvt Ltd', 'Organization', 'CRITICAL', 'Shell company. Registered in Dharavi. Director linked to hawala network. ₹18 Cr suspicious inflows.', 'Dharavi, Mumbai', 'Mumbai City', 'inv-001', null, null, 'HDFC-0029184733', 'HDFC Bank'],
    ['ent-003', 'L-MH-00391', 'Andheri East Warehouse Complex', 'Location', 'HIGH', 'Known storage facility. Linked to hawala cash bundling and document forgery.', 'Andheri East, Mumbai', 'Mumbai City', 'inv-001', null, null, null, null],
    ['ent-004', 'V-MH-02290', 'MH 01 CQ 4499', 'Vehicle', 'HIGH', 'Toyota Innova. Registered to shell company. Spotted at 3 hawala collection points.', 'Mumbai', 'Mumbai City', 'inv-001', null, 'MH 01 CQ 4499', null, null],
    ['ent-005', 'PH-MH-22910', '+91 99205 11820', 'Phone Number', 'MEDIUM', 'Burner number. 6 contacts linked to hawala network. Call records subpoenaed.', 'Mumbai', 'Mumbai City', 'inv-001', '+91 99205 11820', null, null, null],
    ['ent-006', 'AC-MH-98213', 'AC-PUNB-0044421', 'Account', 'CRITICAL', 'Punjab National Bank. Flagged for hawala transactions. ₹4.2 Cr unaccounted flows.', 'Mumbai', 'Mumbai City', 'inv-001', null, null, 'PUNB-0044421', 'Punjab National Bank'],
    ['ent-007', 'P-MH-00445', 'Rakesh Bhaskar Rane', 'Person', 'HIGH', 'Cyber fraud ring coordinator. Pune Hadapsar. Operates via 12 mule accounts.', 'Hadapsar, Pune', 'Pune', 'inv-002', '+91 97668 33210', null, null, null],
    ['ent-008', 'DEV-MH-4471', 'IMEI: 357291108432100', 'Device', 'HIGH', 'Android device. SIM swapped 4 times. Used for OTP interception during fraud.', 'Pune', 'Pune', 'inv-002', null, null, null, null],
    ['ent-009', 'VES-MH-009', 'Fishing Vessel #MH-MRD-0091', 'Vehicle', 'CRITICAL', 'Coastal vessel. Raigad coast. Intercepted carrying 12 kg methamphetamine. Owner absconding.', 'Raigad Coast', 'Raigad', 'inv-003', null, 'MH-MRD-0091', null, null],
    ['ent-010', 'P-MH-00731', 'Suresh Govind Naik', 'Person', 'CRITICAL', 'Narcotics supply chain handler. ATS wanted. Three prior NDPS arrests.', 'Alibag, Raigad', 'Raigad', 'inv-003', '+91 94048 77100', null, null, null],
  ];

  for (const e of entities) insertEntity.run(...e);

  // ── Graph Nodes ───────────────────────────────────────────────────────────
  const insertNode = db.prepare(`
    INSERT INTO graph_nodes (id, label, type, risk, description, location, entity_id, investigation_id)
    VALUES (?, ?, ?, ?, ?, ?, ?, ?)
  `);

  const nodes = [
    ['gn-1', 'Radiant Traders Pvt Ltd', 'Organization', 'CRITICAL', 'Shell company. ₹18 Cr suspicious inflows. Hawala hub.', 'Dharavi, Mumbai', 'ORG-MH-77213', 'inv-001'],
    ['gn-2', 'Dawood Mirza Shaikh', 'Person', 'CRITICAL', 'Primary hawala operator. Active PMLA warrant.', 'Dharavi, Mumbai', 'P-MH-00182', 'inv-001'],
    ['gn-3', 'Andheri East Warehouse', 'Location', 'HIGH', 'Cash bundling & document forgery location.', 'Andheri East, Mumbai', 'L-MH-00391', 'inv-001'],
    ['gn-4', 'MH 01 CQ 4499 (Toyota Innova)', 'Vehicle', 'HIGH', 'Shell company registered. Spotted at 3 hawala points.', 'Mumbai City', 'V-MH-02290', 'inv-001'],
    ['gn-5', '+91 99205 11820', 'Phone Number', 'MEDIUM', 'Burner. 6 contacts in hawala network.', 'Mumbai', 'PH-MH-22910', 'inv-001'],
    ['gn-6', 'PNB Account AC-0044421', 'Account', 'CRITICAL', '₹4.2 Cr unaccounted hawala flows.', 'Mumbai', 'AC-MH-98213', 'inv-001'],
    ['gn-7', 'Rakesh Bhaskar Rane', 'Person', 'HIGH', 'Cyber fraud ring coordinator, Pune.', 'Hadapsar, Pune', 'P-MH-00445', 'inv-002'],
    ['gn-8', 'IMEI: 3572911 (Device)', 'Device', 'HIGH', 'SIM swapped 4 times. OTP interception.', 'Pune', 'DEV-MH-4471', 'inv-002'],
    ['gn-9', 'Vessel MH-MRD-0091', 'Vehicle', 'CRITICAL', 'Intercepted: 12 kg methamphetamine.', 'Raigad Coast', 'VES-MH-009', 'inv-003'],
    ['gn-10', 'Suresh Govind Naik', 'Person', 'CRITICAL', 'Narcotics handler. ATS wanted.', 'Alibag, Raigad', 'P-MH-00731', 'inv-003'],
    ['gn-11', 'Horizon Exports Pvt Ltd', 'Organization', 'HIGH', 'Front company for narcotics financing.', 'Navi Mumbai', 'ORG-MH-88301', 'inv-003'],
  ];

  for (const n of nodes) insertNode.run(...n);

  // ── Graph Edges ───────────────────────────────────────────────────────────
  const insertEdge = db.prepare(`
    INSERT INTO graph_edges (id, source, target, label, investigation_id)
    VALUES (?, ?, ?, ?, ?)
  `);

  const edges = [
    ['ge-1', 'gn-1', 'gn-2', 'CONTROLLED_BY', 'inv-001'],
    ['ge-2', 'gn-1', 'gn-6', 'OWNS_ACCOUNT', 'inv-001'],
    ['ge-3', 'gn-2', 'gn-3', 'OPERATES_FROM', 'inv-001'],
    ['ge-4', 'gn-2', 'gn-5', 'USES_PHONE', 'inv-001'],
    ['ge-5', 'gn-3', 'gn-4', 'ASSOCIATED_VEHICLE', 'inv-001'],
    ['ge-6', 'gn-5', 'gn-8', 'DEVICE_LINKED', 'inv-002'],
    ['ge-7', 'gn-7', 'gn-8', 'CARRIES_DEVICE', 'inv-002'],
    ['ge-8', 'gn-10', 'gn-9', 'OPERATES_VESSEL', 'inv-003'],
    ['ge-9', 'gn-11', 'gn-9', 'FUNDS_OPERATION', 'inv-003'],
    ['ge-10', 'gn-10', 'gn-11', 'AFFILIATED_WITH', 'inv-003'],
  ];

  for (const e of edges) insertEdge.run(...e);

  // ── Evidence ──────────────────────────────────────────────────────────────
  const insertEv = db.prepare(`
    INSERT INTO evidence (id, investigation_id, filename, file_type, description, hash_sha256, uploaded_by, clearance)
    VALUES (?, ?, ?, ?, ?, ?, ?, ?)
  `);

  const evidenceItems = [
    ['ev-001', 'inv-001', 'FIR-CR-0841-2024.pdf', 'PDF', 'Original FIR filed at Dharavi Police Station', 'a3f8c1d2e9b047a6f3128c9d4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9b0c1d2e3f4', 'usr-002', 'RESTRICTED'],
    ['ev-002', 'inv-001', 'Bank-Statement-HDFC-0029184733.xlsx', 'XLSX', 'HDFC Bank statement for Radiant Traders — 18 months transaction history', 'b4c9e2f1a0b7d8e9f0a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9b0c1d2e3', 'usr-002', 'CONFIDENTIAL'],
    ['ev-003', 'inv-001', 'CDR-Analysis-+9199205.xlsx', 'XLSX', 'Call Detail Records for burner number. 6 contacts identified.', 'c5d0f3g2b1c8e9f0a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9b0c1d2e3f4', 'usr-002', 'RESTRICTED'],
    ['ev-004', 'inv-002', 'FIR-CYB-0912-2024.pdf', 'PDF', 'Cyber FIR — SIM swap fraud complaint', 'aa3b4c5d6e7f8a9b0c1d2e3f4a5b6c7d8e9f0a1b2c3d4e5f6a7b8c9d0e1f2a3b4', 'usr-003', 'RESTRICTED'],
    ['ev-005', 'inv-002', 'Device-Forensics-IMEI-357291.pdf', 'PDF', 'Forensic report on seized Android device. SIM swap logs extracted.', 'bb5c6d7e8f9a0b1c2d3e4f5a6b7c8d9e0f1a2b3c4d5e6f7a8b9c0d1e2f3a4b5c', 'usr-003', 'CONFIDENTIAL'],
    ['ev-006', 'inv-003', 'NCB-Seizure-Report-MH-MRD-0091.pdf', 'PDF', 'NCB official seizure report. 12 kg methamphetamine. Lab analysis certified.', 'cc7d8e9f0a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9b0c1d2e3f4a5b6c7d', 'usr-004', 'SECRET'],
  ];

  for (const e of evidenceItems) insertEv.run(...e);

  // ── Audit Log ─────────────────────────────────────────────────────────────
  const insertAudit = db.prepare(`
    INSERT INTO audit_log (id, user_id, user_name, action, resource_type, resource_id, details, ip_address, timestamp)
    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
  `);

  const auditEntries = [
    ['audit-001', 'usr-002', 'PI Priya Deshmukh', 'LOGIN', 'session', null, 'Authenticated successfully via MH Police CORTEX', '10.1.0.42', '2026-09-07T09:00:00Z'],
    ['audit-002', 'usr-002', 'PI Priya Deshmukh', 'VIEW_INVESTIGATION', 'investigation', 'inv-001', 'Opened case MH-CR-2024-0841', '10.1.0.42', '2026-09-07T09:05:00Z'],
    ['audit-003', 'usr-002', 'PI Priya Deshmukh', 'UPLOAD_EVIDENCE', 'evidence', 'ev-003', 'Uploaded CDR analysis for inv-001', '10.1.0.42', '2026-09-07T10:15:00Z'],
    ['audit-004', 'usr-003', 'ASI Vikram Patil', 'LOGIN', 'session', null, 'Authenticated successfully via MH Police CORTEX', '10.1.0.55', '2026-09-07T11:30:00Z'],
    ['audit-005', 'usr-003', 'ASI Vikram Patil', 'VIEW_GRAPH', 'graph', 'inv-002', 'Opened network graph for Operation Dark Net', '10.1.0.55', '2026-09-07T11:32:00Z'],
    ['audit-006', 'usr-004', 'SP Anjali Kulkarni', 'EXPORT_REPORT', 'report', 'inv-003', 'Downloaded PDF briefing for Operation Blue Tide', '10.1.0.21', '2026-09-07T14:00:00Z'],
    ['audit-007', 'usr-001', 'DCP Rajesh Sharma', 'VIEW_AUDIT', 'audit_log', null, 'Accessed full audit log', '10.1.0.10', '2026-09-07T15:00:00Z'],
    ['audit-008', 'usr-002', 'PI Priya Deshmukh', 'AI_QUERY', 'assistant', 'inv-001', 'Queried AI assistant: entity connection analysis', '10.1.0.42', '2026-09-07T16:00:00Z'],
  ];

  for (const a of auditEntries) insertAudit.run(...a);

  console.log('[DB] Seed complete. Maharashtra Police CORTEX ready.');
}

seedIfEmpty();

module.exports = db;
