const express = require('express');
const db = require('../db');
const { authenticate, logAudit } = require('../audit');

const router = express.Router();

// GET /api/investigations
router.get('/', authenticate, (req, res) => {
  const rows = db.prepare(`
    SELECT i.*, u.name as lead_officer_name
    FROM investigations i
    LEFT JOIN users u ON i.lead_officer_id = u.id
    ORDER BY i.updated_at DESC
  `).all();
  res.json(rows);
});

// GET /api/investigations/:id
router.get('/:id', authenticate, (req, res) => {
  const inv = db.prepare(`
    SELECT i.*, u.name as lead_officer_name
    FROM investigations i
    LEFT JOIN users u ON i.lead_officer_id = u.id
    WHERE i.id = ?
  `).get(req.params.id);
  if (!inv) return res.status(404).json({ error: 'Investigation not found.' });
  logAudit(req.user.id, req.user.name, 'VIEW_INVESTIGATION', 'investigation', inv.id, `Opened case ${inv.case_number}`, req.ip);
  res.json(inv);
});

// POST /api/investigations
router.post('/', authenticate, (req, res) => {
  const { title, description, classification, priority, category, jurisdiction, district, fir_number, police_station } = req.body;
  if (!title) return res.status(400).json({ error: 'Title is required.' });

  const now = new Date().toISOString();
  const id = `inv-${Date.now()}`;
  const year = new Date().getFullYear();
  const seq = db.prepare('SELECT COUNT(*) as cnt FROM investigations').get().cnt + 1;
  const catCode = (category || 'CR').slice(0, 3).toUpperCase();
  const case_number = `MH-${catCode}-${year}-${String(seq).padStart(4, '0')}`;

  db.prepare(`
    INSERT INTO investigations (id, case_number, title, description, classification, priority, status, category, jurisdiction, district, lead_officer_id, fir_number, police_station, updated_at)
    VALUES (?, ?, ?, ?, ?, ?, 'Active', ?, ?, ?, ?, ?, ?, ?)
  `).run(id, case_number, title, description || '', classification || 'RESTRICTED', priority || 'Medium', category || 'General', jurisdiction || '', district || '', req.user.id, fir_number || '', police_station || '', now);

  logAudit(req.user.id, req.user.name, 'CREATE_INVESTIGATION', 'investigation', id, `Created case ${case_number}`, req.ip);

  const created = db.prepare('SELECT * FROM investigations WHERE id = ?').get(id);
  res.status(201).json(created);
});

// GET /api/dashboard/stats
router.get('/stats/summary', authenticate, (req, res) => {
  const activeCount = db.prepare("SELECT COUNT(*) as cnt FROM investigations WHERE status = 'Active'").get().cnt;
  const totalEntities = db.prepare('SELECT COUNT(*) as cnt FROM entities').get().cnt;
  const totalInvs = db.prepare('SELECT COUNT(*) as cnt FROM investigations').get().cnt;
  const criticalAlerts = db.prepare("SELECT COUNT(*) as cnt FROM entities WHERE risk = 'CRITICAL'").get().cnt;
  const totalEvidence = db.prepare('SELECT COUNT(*) as cnt FROM evidence').get().cnt;
  const networks = db.prepare('SELECT COUNT(DISTINCT investigation_id) as cnt FROM graph_nodes').get().cnt;

  res.json({
    activeInvestigations: activeCount,
    totalEntities,
    totalInvestigations: totalInvs,
    highRiskAlerts: criticalAlerts,
    totalEvidence,
    connectedNetworks: networks,
  });
});

module.exports = router;
