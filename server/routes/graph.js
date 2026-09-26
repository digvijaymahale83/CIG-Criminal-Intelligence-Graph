const express = require('express');
const db = require('../db');
const { authenticate, logAudit } = require('../audit');

const router = express.Router();

// GET /api/graph?investigation_id=xxx
router.get('/', authenticate, (req, res) => {
  const { investigation_id } = req.query;
  
  let nodesQuery = 'SELECT * FROM graph_nodes';
  let edgesQuery = 'SELECT * FROM graph_edges';
  const params = [];

  if (investigation_id) {
    nodesQuery += ' WHERE investigation_id = ?';
    edgesQuery += ' WHERE investigation_id = ?';
    params.push(investigation_id);
  }

  const nodes = db.prepare(nodesQuery).all(...params);
  const edges = db.prepare(edgesQuery).all(...params);

  logAudit(req.user.id, req.user.name, 'VIEW_GRAPH', 'graph', investigation_id || 'all', 'Viewed network graph', req.ip);

  res.json({ nodes, edges });
});

module.exports = router;
