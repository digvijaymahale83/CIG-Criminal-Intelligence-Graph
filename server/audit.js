const jwt = require('jsonwebtoken');
const db = require('./db');

const JWT_SECRET = process.env.JWT_SECRET || 'mhp-cortex-secret-2024-secure';

function authenticate(req, res, next) {
  const auth = req.headers.authorization;
  if (!auth || !auth.startsWith('Bearer ')) {
    return res.status(401).json({ error: 'Authentication required.' });
  }
  try {
    req.user = jwt.verify(auth.slice(7), JWT_SECRET);
    next();
  } catch {
    return res.status(401).json({ error: 'Token invalid or expired.' });
  }
}

function logAudit(userId, userName, action, resourceType, resourceId, details, ip) {
  const id = `audit-${Date.now()}-${Math.random().toString(36).slice(2, 8)}`;
  db.prepare(`
    INSERT INTO audit_log (id, user_id, user_name, action, resource_type, resource_id, details, ip_address)
    VALUES (?, ?, ?, ?, ?, ?, ?, ?)
  `).run(id, userId || null, userName || null, action, resourceType || null, resourceId || null, details || null, ip || null);
}

module.exports = { authenticate, logAudit };
