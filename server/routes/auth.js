const express = require('express');
const bcrypt = require('bcryptjs');
const jwt = require('jsonwebtoken');
const db = require('../db');
const { logAudit } = require('../audit');

const router = express.Router();
const JWT_SECRET = process.env.JWT_SECRET || 'mhp-cortex-secret-2024-secure';

// POST /api/auth/login
router.post('/login', (req, res) => {
  const { email, password } = req.body;
  if (!email || !password) {
    return res.status(400).json({ error: 'Email and password are required.' });
  }

  const user = db.prepare('SELECT * FROM users WHERE email = ?').get(email.toLowerCase().trim());
  if (!user) {
    return res.status(401).json({ error: 'Invalid credentials.' });
  }

  const valid = bcrypt.compareSync(password, user.password_hash);
  if (!valid) {
    return res.status(401).json({ error: 'Invalid credentials.' });
  }

  const payload = {
    id: user.id,
    email: user.email,
    name: user.name,
    role: user.role,
    badge: user.badge_number,
    agency: user.agency,
    rank: user.rank,
    unit: user.unit,
  };

  const token = jwt.sign(payload, JWT_SECRET, { expiresIn: '8h' });

  logAudit(user.id, user.name, 'LOGIN', 'session', null, 'Authenticated via MH Police CORTEX', req.ip);

  res.json({ token, user: payload });
});

// GET /api/auth/me — verify current token
router.get('/me', (req, res) => {
  const auth = req.headers.authorization;
  if (!auth || !auth.startsWith('Bearer ')) {
    return res.status(401).json({ error: 'No token provided.' });
  }
  try {
    const payload = jwt.verify(auth.slice(7), JWT_SECRET);
    res.json({ user: payload });
  } catch {
    res.status(401).json({ error: 'Token invalid or expired.' });
  }
});

module.exports = router;
