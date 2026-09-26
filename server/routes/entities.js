const express = require('express');
const db = require('../db');
const { authenticate, logAudit } = require('../audit');

const router = express.Router();

// GET /api/entities
router.get('/', authenticate, (req, res) => {
  const { investigation_id, type, risk, search } = req.query;
  let query = 'SELECT * FROM entities WHERE 1=1';
  const params = [];

  if (investigation_id) { query += ' AND linked_investigation_id = ?'; params.push(investigation_id); }
  if (type) { query += ' AND type = ?'; params.push(type); }
  if (risk) { query += ' AND risk = ?'; params.push(risk); }
  if (search) { query += ' AND (name LIKE ? OR location LIKE ? OR description LIKE ?)'; params.push(`%${search}%`, `%${search}%`, `%${search}%`); }

  query += ' ORDER BY risk DESC, name ASC';
  const rows = db.prepare(query).all(...params);
  res.json(rows);
});

// POST /api/entities
router.post('/', authenticate, (req, res) => {
  const { name, type, risk, description, location, district, linked_investigation_id, phone, vehicle_number, account_number, bank } = req.body;
  if (!name || !type) return res.status(400).json({ error: 'Name and type are required.' });

  const id = `ent-${Date.now()}`;
  const entity_id = `${type.slice(0, 1).toUpperCase()}-MH-${String(Date.now()).slice(-5)}`;

  db.prepare(`
    INSERT INTO entities (id, entity_id, name, type, risk, description, location, district, linked_investigation_id, phone, vehicle_number, account_number, bank)
    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
  `).run(id, entity_id, name, type, risk || 'LOW', description || '', location || '', district || '', linked_investigation_id || null, phone || null, vehicle_number || null, account_number || null, bank || null);

  // Update entity count in investigation
  if (linked_investigation_id) {
    db.prepare('UPDATE investigations SET entity_count = entity_count + 1 WHERE id = ?').run(linked_investigation_id);
  }

  logAudit(req.user.id, req.user.name, 'CREATE_ENTITY', 'entity', id, `Added entity: ${name} (${type})`, req.ip);

  const created = db.prepare('SELECT * FROM entities WHERE id = ?').get(id);
  res.status(201).json(created);
});

module.exports = router;
