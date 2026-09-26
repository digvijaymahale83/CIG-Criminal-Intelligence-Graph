const express = require('express');
const db = require('../db');
const { authenticate, logAudit } = require('../audit');

const router = express.Router();

// GET /api/audit
router.get('/', authenticate, (req, res) => {
  const { limit = 50 } = req.query;
  const rows = db.prepare('SELECT * FROM audit_log ORDER BY timestamp DESC LIMIT ?').all(Number(limit));
  logAudit(req.user.id, req.user.name, 'VIEW_AUDIT', 'audit_log', null, 'Accessed audit log', req.ip);
  res.json(rows);
});

module.exports = router;
