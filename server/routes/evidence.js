const express = require('express');
const multer = require('multer');
const crypto = require('crypto');
const path = require('path');
const fs = require('fs');
const db = require('../db');
const { authenticate, logAudit } = require('../audit');

const router = express.Router();

// Create uploads directory
const UPLOADS_DIR = path.join(__dirname, '../../uploads');
if (!fs.existsSync(UPLOADS_DIR)) fs.mkdirSync(UPLOADS_DIR, { recursive: true });

// Multer storage — saves with original filename + timestamp
const storage = multer.diskStorage({
  destination: (req, file, cb) => cb(null, UPLOADS_DIR),
  filename: (req, file, cb) => {
    const safe = file.originalname.replace(/[^a-zA-Z0-9._-]/g, '_');
    cb(null, `${Date.now()}-${safe}`);
  },
});

const ALLOWED_TYPES = [
  'application/pdf',
  'text/csv',
  'application/vnd.ms-excel',
  'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet',
  'application/json',
  'text/plain',
  'image/jpeg',
  'image/png',
];

const upload = multer({
  storage,
  limits: { fileSize: 50 * 1024 * 1024 }, // 50 MB
  fileFilter: (req, file, cb) => {
    if (ALLOWED_TYPES.includes(file.mimetype) || file.originalname.match(/\.(pdf|csv|xlsx|xls|json|txt|jpg|jpeg|png)$/i)) {
      cb(null, true);
    } else {
      cb(new Error(`File type not allowed: ${file.mimetype}. Allowed: PDF, CSV, Excel, JSON, TXT, Images`));
    }
  },
});

// GET /api/evidence?investigation_id=xxx
router.get('/', authenticate, (req, res) => {
  const { investigation_id } = req.query;
  let query = 'SELECT * FROM evidence';
  const params = [];
  if (investigation_id) {
    query += ' WHERE investigation_id = ?';
    params.push(investigation_id);
  }
  query += ' ORDER BY created_at DESC';
  res.json(db.prepare(query).all(...params));
});

// POST /api/evidence/upload — real file upload
router.post('/upload', authenticate, upload.single('file'), async (req, res) => {
  if (!req.file) return res.status(400).json({ error: 'No file provided.' });

  const { investigation_id, description, clearance } = req.body;

  try {
    // Compute SHA-256 hash of actual file contents
    const fileBuffer = fs.readFileSync(req.file.path);
    const hash = crypto.createHash('sha256').update(fileBuffer).digest('hex');

    // Determine file type label
    const ext = path.extname(req.file.originalname).toLowerCase().replace('.', '').toUpperCase();
    const fileTypeMap = {
      'application/pdf': 'PDF',
      'text/csv': 'CSV',
      'application/vnd.ms-excel': 'XLS',
      'application/vnd.openxmlformats-officedocument.spreadsheetml.sheet': 'XLSX',
      'application/json': 'JSON',
      'text/plain': 'TXT',
      'image/jpeg': 'JPG',
      'image/png': 'PNG',
    };
    const fileType = fileTypeMap[req.file.mimetype] || ext || 'FILE';

    // Save to DB
    const id = `ev-${Date.now()}-${Math.random().toString(36).slice(2, 6)}`;
    db.prepare(`
      INSERT INTO evidence (id, investigation_id, filename, file_type, description, hash_sha256, uploaded_by, clearance)
      VALUES (?, ?, ?, ?, ?, ?, ?, ?)
    `).run(
      id,
      investigation_id || null,
      req.file.originalname,
      fileType,
      description || '',
      hash,
      req.user.name,
      clearance || 'RESTRICTED'
    );

    // Update evidence count in investigation
    if (investigation_id) {
      db.prepare('UPDATE investigations SET evidence_count = evidence_count + 1 WHERE id = ?').run(investigation_id);
    }

    logAudit(
      req.user.id,
      req.user.name,
      'UPLOAD_EVIDENCE',
      'evidence',
      id,
      `Uploaded: ${req.file.originalname} (${(req.file.size / 1024).toFixed(1)} KB, ${fileType}) SHA256: ${hash.slice(0, 16)}…`,
      req.ip
    );

    res.status(201).json({
      id,
      filename: req.file.originalname,
      file_type: fileType,
      description: description || '',
      hash_sha256: hash,
      uploaded_by: req.user.name,
      clearance: clearance || 'RESTRICTED',
      investigation_id: investigation_id || null,
      created_at: new Date().toISOString(),
      size_bytes: req.file.size,
    });
  } catch (err) {
    // Clean up file if DB insert fails
    try { fs.unlinkSync(req.file.path); } catch {}
    console.error('[Upload] Error:', err.message);
    res.status(500).json({ error: 'Upload failed: ' + err.message });
  }
});

// Error handler for multer
router.use((err, req, res, next) => {
  if (err.code === 'LIMIT_FILE_SIZE') {
    return res.status(413).json({ error: 'File too large. Maximum size is 50 MB.' });
  }
  res.status(400).json({ error: err.message });
});

module.exports = router;
