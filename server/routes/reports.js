const express = require('express');
const db = require('../db');
const { authenticate, logAudit } = require('../audit');
const crypto = require('crypto');

const router = express.Router();

// GET /api/reports — list of case files
router.get('/', authenticate, (req, res) => {
  const rows = db.prepare(`
    SELECT i.id, i.case_number, i.title, i.district, i.category as type, 
           u.name as officer, i.status, i.evidence_count, i.classification as clearance, i.description
    FROM investigations i
    LEFT JOIN users u ON i.lead_officer_id = u.id
    ORDER BY i.updated_at DESC
  `).all();
  res.json(rows);
});

// GET /api/reports/:id/csv — download real CSV
router.get('/:id/csv', authenticate, (req, res) => {
  const inv = db.prepare('SELECT i.*, u.name as officer FROM investigations i LEFT JOIN users u ON i.lead_officer_id = u.id WHERE i.id = ?').get(req.params.id);
  if (!inv) return res.status(404).json({ error: 'Case not found.' });

  const entities = db.prepare('SELECT * FROM entities WHERE linked_investigation_id = ?').all(req.params.id);
  const evidence = db.prepare('SELECT * FROM evidence WHERE investigation_id = ?').all(req.params.id);

  // Build CSV content
  const rows = [
    ['MAHARASHTRA POLICE — CORTEX INTELLIGENCE PLATFORM'],
    ['Case Intelligence Report'],
    [''],
    ['Case Number', inv.case_number],
    ['Title', inv.title],
    ['District', inv.district],
    ['Category', inv.category],
    ['Status', inv.status],
    ['Classification', inv.classification],
    ['Lead Officer', inv.officer],
    ['FIR Number', inv.fir_number],
    ['Police Station', inv.police_station],
    ['Description', inv.description],
    ['Report Generated', new Date().toISOString()],
    [''],
    ['ENTITIES'],
    ['Entity ID', 'Name', 'Type', 'Risk Level', 'Location', 'District', 'Description'],
    ...entities.map(e => [e.entity_id, e.name, e.type, e.risk, e.location, e.district, e.description]),
    [''],
    ['EVIDENCE FILES'],
    ['File ID', 'Filename', 'Type', 'Description', 'Clearance', 'SHA-256 Hash', 'Uploaded At'],
    ...evidence.map(e => [e.id, e.filename, e.file_type, e.description, e.clearance, e.hash_sha256, e.created_at]),
  ];

  const csvContent = rows.map(row =>
    row.map(cell => `"${String(cell || '').replace(/"/g, '""')}"`).join(',')
  ).join('\r\n');

  logAudit(req.user.id, req.user.name, 'EXPORT_REPORT_CSV', 'report', req.params.id, `Downloaded CSV for ${inv.case_number}`, req.ip);

  res.setHeader('Content-Type', 'text/csv; charset=utf-8');
  res.setHeader('Content-Disposition', `attachment; filename="MHP-CORTEX-${inv.case_number.replace(/\//g, '-')}-Report.csv"`);
  res.send('\uFEFF' + csvContent); // BOM for Excel UTF-8
});

// GET /api/reports/:id/pdf — generate PDF briefing
router.get('/:id/pdf', authenticate, (req, res) => {
  const inv = db.prepare('SELECT i.*, u.name as officer, u.rank, u.unit FROM investigations i LEFT JOIN users u ON i.lead_officer_id = u.id WHERE i.id = ?').get(req.params.id);
  if (!inv) return res.status(404).json({ error: 'Case not found.' });

  const entities = db.prepare('SELECT * FROM entities WHERE linked_investigation_id = ?').all(req.params.id);
  const evidence = db.prepare('SELECT * FROM evidence WHERE investigation_id = ?').all(req.params.id);

  const PDFDocument = require('pdfkit');
  const doc = new PDFDocument({ margin: 50, size: 'A4' });

  res.setHeader('Content-Type', 'application/pdf');
  res.setHeader('Content-Disposition', `attachment; filename="MHP-CORTEX-${inv.case_number.replace(/\//g, '-')}-Briefing.pdf"`);
  doc.pipe(res);

  // Header
  doc.rect(0, 0, doc.page.width, 80).fill('#0d1117');
  doc.fillColor('#e6edf3').fontSize(16).font('Helvetica-Bold')
    .text('MAHARASHTRA POLICE', 50, 20, { align: 'center' });
  doc.fontSize(10).text('CORTEX Intelligence Platform — Classified Case Briefing', 50, 42, { align: 'center' });
  doc.fillColor('#388bfd').fontSize(8).text(`CLASSIFICATION: ${inv.classification}`, 50, 58, { align: 'center' });

  doc.moveDown(3);

  // Case Meta
  doc.fillColor('#161b22').rect(50, 90, doc.page.width - 100, 2).fill();
  doc.fillColor('#1f6feb').fontSize(14).font('Helvetica-Bold').text(inv.title, 50, 100);
  doc.fillColor('#484f58').fontSize(9).font('Helvetica')
    .text(`Case No: ${inv.case_number}  |  FIR: ${inv.fir_number || 'N/A'}  |  District: ${inv.district}`, 50, 118);
  doc.text(`Category: ${inv.category}  |  Status: ${inv.status}  |  Priority: ${inv.priority}`, 50, 130);
  doc.text(`Lead Officer: ${inv.officer || 'N/A'}  |  Rank: ${inv.rank || ''}  |  Unit: ${inv.unit || ''}`, 50, 142);
  doc.text(`Police Station: ${inv.police_station || 'N/A'}  |  Jurisdiction: ${inv.jurisdiction || 'N/A'}`, 50, 154);
  doc.text(`Generated: ${new Date().toLocaleString('en-IN', { timeZone: 'Asia/Kolkata' })} IST`, 50, 166);

  doc.moveDown(2);
  doc.fillColor('#0d1117').rect(50, 178, doc.page.width - 100, 1).fill();

  // Description
  doc.fillColor('#28201A').fontSize(11).font('Helvetica-Bold').text('Case Summary', 50, 188);
  doc.fillColor('#3d3128').fontSize(9).font('Helvetica').text(inv.description || '', 50, 202, { width: doc.page.width - 100 });

  doc.moveDown(1.5);

  // Entities
  const entY = doc.y + 10;
  doc.fillColor('#28201A').fontSize(11).font('Helvetica-Bold').text(`Entities Identified (${entities.length})`, 50, entY);
  doc.fillColor('#0d1117').rect(50, entY + 16, doc.page.width - 100, 1).fill();

  let y = entY + 22;
  for (const ent of entities.slice(0, 8)) {
    const riskColors = { CRITICAL: '#f85149', HIGH: '#e3b341', MEDIUM: '#3fb950', LOW: '#79c0ff' };
    doc.fillColor(riskColors[ent.risk] || '#8b949e').font('Helvetica-Bold').fontSize(8).text(`[${ent.risk}] `, 50, y, { continued: true });
    doc.fillColor('#28201A').font('Helvetica-Bold').text(`${ent.name}`, { continued: true });
    doc.font('Helvetica').fillColor('#5C4C3E').text(`  —  ${ent.type}  |  ${ent.location || 'Location N/A'}`);
    doc.fillColor('#8C7A6B').fontSize(7.5).text(`   ID: ${ent.entity_id}  |  ${ent.description || ''}`, 50, y + 10, { width: doc.page.width - 100 });
    y += 24;
    if (y > 700) break;
  }

  // Evidence
  y += 10;
  doc.fillColor('#28201A').fontSize(11).font('Helvetica-Bold').text(`Evidence Files (${evidence.length})`, 50, y);
  doc.fillColor('#0d1117').rect(50, y + 16, doc.page.width - 100, 1).fill();
  y += 22;

  for (const ev of evidence.slice(0, 6)) {
    doc.fillColor('#28201A').font('Helvetica-Bold').fontSize(8).text(`${ev.filename}`, 50, y, { continued: true });
    doc.font('Helvetica').fillColor('#5C4C3E').text(`  [${ev.clearance}]  ${ev.file_type}`);
    doc.fillColor('#8C7A6B').fontSize(7.5).text(`   SHA-256: ${ev.hash_sha256 || 'N/A'}`, 50, y + 10);
    y += 22;
    if (y > 720) break;
  }

  // Footer
  const footerY = doc.page.height - 40;
  doc.fillColor('#0d1117').rect(0, footerY - 5, doc.page.width, 45).fill();
  const docHash = crypto.createHash('sha256').update(`${inv.case_number}-${Date.now()}`).digest('hex');
  doc.fillColor('#484f58').fontSize(7).font('Helvetica')
    .text(`Document Integrity Hash: ${docHash}`, 50, footerY + 2, { align: 'center' });
  doc.text(`Maharashtra Police CORTEX — RESTRICTED — Authorized Personnel Only`, 50, footerY + 12, { align: 'center' });

  doc.end();

  logAudit(req.user.id, req.user.name, 'EXPORT_REPORT_PDF', 'report', req.params.id, `Downloaded PDF for ${inv.case_number}`, req.ip);
});

module.exports = router;
