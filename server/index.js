/**
 * Maharashtra Police CORTEX — Node.js/Express API Server
 * Runs on port 4000. Frontend (Vite) runs on port 3000.
 */
require('dotenv').config();
const express = require('express');
const cors = require('cors');

const app = express();
const PORT = process.env.API_PORT || 4000;

// ─── Middleware ───────────────────────────────────────────────────────────────
app.use(cors({ origin: ['http://localhost:3000', 'http://localhost:5173', 'http://10.206.221.51:3000'], credentials: true }));
app.use(express.json({ limit: '10mb' }));
app.use(express.urlencoded({ extended: true }));

// ─── Request Logger ───────────────────────────────────────────────────────────
app.use((req, _res, next) => {
  const ts = new Date().toLocaleTimeString('en-IN');
  console.log(`[${ts}] ${req.method} ${req.path}`);
  next();
});

// ─── Routes ───────────────────────────────────────────────────────────────────
app.use('/api/auth', require('./routes/auth'));
app.use('/api/investigations', require('./routes/investigations'));
app.use('/api/entities', require('./routes/entities'));
app.use('/api/graph', require('./routes/graph'));
app.use('/api/reports', require('./routes/reports'));
app.use('/api/assistant', require('./routes/assistant'));
app.use('/api/audit', require('./routes/audit'));
app.use('/api/evidence', require('./routes/evidence'));
app.use('/api/health', require('./routes/health'));

// ─── Root ─────────────────────────────────────────────────────────────────────
app.get('/', (_req, res) => {
  res.json({
    name: 'Maharashtra Police CORTEX API',
    version: '1.0.0',
    status: 'online',
    docs: 'POST /api/auth/login | GET /api/investigations | GET /api/graph | GET /api/health',
  });
});

// ─── 404 Handler ──────────────────────────────────────────────────────────────
app.use((req, res) => {
  res.status(404).json({ error: `Route ${req.method} ${req.path} not found.` });
});

// ─── Error Handler ────────────────────────────────────────────────────────────
app.use((err, _req, res, _next) => {
  console.error('[Error]', err.message);
  res.status(500).json({ error: 'Internal server error.' });
});

// ─── Start ────────────────────────────────────────────────────────────────────
app.listen(PORT, () => {
  console.log(`\n╔════════════════════════════════════════════════╗`);
  console.log(`║   Maharashtra Police CORTEX API                ║`);
  console.log(`║   Running on http://localhost:${PORT}              ║`);
  console.log(`╚════════════════════════════════════════════════╝\n`);
  console.log(`  POST http://localhost:${PORT}/api/auth/login`);
  console.log(`  GET  http://localhost:${PORT}/api/investigations`);
  console.log(`  GET  http://localhost:${PORT}/api/health`);
  console.log(`\n  Default credentials:`);
  console.log(`  Email:    dcp.sharma@mahapolice.gov.in`);
  console.log(`  Password: Maharashtra@2024\n`);
});
