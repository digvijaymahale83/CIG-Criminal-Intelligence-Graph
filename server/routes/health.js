const express = require('express');
const db = require('../db');

const router = express.Router();

// GET /api/health
router.get('/', (req, res) => {
  const startTime = Date.now();

  const checks = [];

  // DB check
  try {
    db.prepare('SELECT 1').get();
    checks.push({ name: 'SQLite Database', status: 'OPERATIONAL', latencyMs: Date.now() - startTime });
  } catch (e) {
    checks.push({ name: 'SQLite Database', status: 'DEGRADED', error: e.message });
  }

  // Gemini check
  const geminiStatus = process.env.GEMINI_API_KEY && process.env.GEMINI_API_KEY !== 'MY_GEMINI_API_KEY'
    ? 'OPERATIONAL'
    : 'NOT_CONFIGURED';
  checks.push({ name: 'Gemini AI Service', status: geminiStatus });

  // Counts
  const invCount = db.prepare('SELECT COUNT(*) as cnt FROM investigations').get().cnt;
  const entityCount = db.prepare('SELECT COUNT(*) as cnt FROM entities').get().cnt;
  const evCount = db.prepare('SELECT COUNT(*) as cnt FROM evidence').get().cnt;

  const allOk = checks.every(c => c.status === 'OPERATIONAL' || c.status === 'NOT_CONFIGURED');

  res.status(allOk ? 200 : 503).json({
    status: allOk ? 'OPERATIONAL' : 'DEGRADED',
    version: '1.0.0',
    platform: 'Maharashtra Police CORTEX Intelligence Platform',
    uptime: process.uptime(),
    timestamp: new Date().toISOString(),
    services: checks,
    database: {
      investigations: invCount,
      entities: entityCount,
      evidence: evCount,
    },
  });
});

module.exports = router;
