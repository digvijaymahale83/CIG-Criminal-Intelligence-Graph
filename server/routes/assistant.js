const express = require('express');
const { authenticate, logAudit } = require('../audit');
const db = require('../db');

const router = express.Router();

// POST /api/assistant/query
router.post('/query', authenticate, async (req, res) => {
  const { query, investigation_id, history = [] } = req.body;
  if (!query) return res.status(400).json({ error: 'Query is required.' });

  const apiKey = process.env.GEMINI_API_KEY;

  // Build context from DB
  let context = 'You are an intelligence assistant for Maharashtra Police CORTEX system.\n';
  context += 'You help investigators analyze criminal cases with evidence-grounded responses.\n';
  context += 'Always cite specific evidence or entities from the case data when answering.\n';
  context += 'NEVER declare guilt, predict future crimes, or make criminal classifications.\n';
  context += 'Respond concisely and professionally in the context of law enforcement intelligence.\n\n';

  if (investigation_id) {
    const inv = db.prepare('SELECT * FROM investigations WHERE id = ?').get(investigation_id);
    if (inv) {
      context += `ACTIVE CASE: ${inv.case_number} — ${inv.title}\n`;
      context += `District: ${inv.district} | Category: ${inv.category} | Status: ${inv.status}\n`;
      context += `Description: ${inv.description}\n\n`;
    }

    const entities = db.prepare('SELECT * FROM entities WHERE linked_investigation_id = ? LIMIT 20').all(investigation_id);
    if (entities.length > 0) {
      context += 'ENTITIES IN THIS CASE:\n';
      entities.forEach(e => {
        context += `- ${e.name} (${e.type}, Risk: ${e.risk}) — ${e.description || ''} Location: ${e.location || 'N/A'}\n`;
      });
      context += '\n';
    }

    const evidence = db.prepare('SELECT * FROM evidence WHERE investigation_id = ? LIMIT 10').all(investigation_id);
    if (evidence.length > 0) {
      context += 'EVIDENCE FILES:\n';
      evidence.forEach(e => {
        context += `- ${e.filename} (${e.file_type}) — ${e.description || ''}\n`;
      });
      context += '\n';
    }
  }

  if (!apiKey || apiKey === 'MY_GEMINI_API_KEY') {
    // Demo mode — return a structured mock response
    const mockResponse = {
      response: `**Evidence-Grounded Analysis:**\n\nBased on the case data for ${investigation_id || 'this investigation'}, I can provide analysis based on the entities and evidence files in the system.\n\n> **Note:** AI Assistant is running in demo mode. Set a valid GEMINI_API_KEY in your .env file to enable real Gemini AI responses.\n\n**Query received:** "${query}"\n\nThe case contains entities flagged at CRITICAL and HIGH risk levels. Cross-referencing the evidence files with the network graph shows multiple inter-connected nodes. For a full AI-powered analysis, please configure the Gemini API key.`,
      citations: [],
      model: 'demo-mode (set GEMINI_API_KEY to enable real AI)',
    };
    return res.json(mockResponse);
  }

  try {
    // Call Gemini API
    const { GoogleGenAI } = require('@google/genai');
    const genai = new GoogleGenAI({
      apiKey,
      httpOptions: {
        baseUrl: 'https://generativelanguage.googleapis.com', // Force standard Gemini API endpoint
      },
    });

    const contents = [
      ...history.map(h => ({ role: h.role, parts: [{ text: h.content }] })),
      { role: 'user', parts: [{ text: query }] },
    ];

    const result = await genai.models.generateContent({
      model: 'gemini-2.0-flash',
      contents,
      config: { systemInstruction: context },
    });

    const text = (typeof result.text === 'string' ? result.text : null)
      || result.candidates?.[0]?.content?.parts?.[0]?.text
      || 'No response generated.';

    logAudit(req.user.id, req.user.name, 'AI_QUERY', 'assistant', investigation_id || null, `Queried: ${query.slice(0, 80)}`, req.ip);

    res.json({ response: text, model: 'gemini-2.0-flash', citations: [] });
  } catch (err) {
    console.error('[AI] Gemini error:', err.message);
    const isKeyError = err.message?.includes('API_KEY') || err.message?.includes('401') || err.message?.includes('403');
    const isNetworkError = err.message?.includes('fetch failed') || err.message?.includes('no such host');
    let hint = '';
    if (isNetworkError) hint = ' — Network error: the API key may be an Antigravity internal key. Use a standard AI Studio key from aistudio.google.com/apikey (format: AIzaSy...)';
    if (isKeyError) hint = ' — Invalid API key. Get a key from aistudio.google.com/apikey';
    res.status(500).json({ error: 'AI service error: ' + err.message + hint });
  }
});

module.exports = router;
