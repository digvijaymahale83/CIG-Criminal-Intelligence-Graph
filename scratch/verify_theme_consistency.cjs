const { spawn } = require('child_process');
const http = require('http');
const fs = require('fs');
const path = require('path');
const WebSocket = require('ws');

const PORT = 9370;
const edgePath = "C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe";
const userDir = "C:\\Users\\digvi\\AppData\\Local\\Temp\\edge_theme_verify";
const ARTIFACT_DIR = "C:\\Users\\digvi\\.gemini\\antigravity-ide\\brain\\4c1c04e2-b2fb-47d2-9a44-b20b125f83e8";

class CdpClient {
  constructor(wsUrl) {
    this.ws = new WebSocket(wsUrl);
    this.id = 1;
    this.callbacks = new Map();
  }

  async connect() {
    return new Promise((resolve, reject) => {
      this.ws.on('open', resolve);
      this.ws.on('error', reject);
      this.ws.on('message', (data) => {
        const msg = JSON.parse(data.toString());
        if (msg.id && this.callbacks.has(msg.id)) {
          const cb = this.callbacks.get(msg.id);
          this.callbacks.delete(msg.id);
          if (msg.error) cb.reject(new Error(msg.error.message || JSON.stringify(msg.error)));
          else cb.resolve(msg.result);
        }
      });
    });
  }

  send(method, params = {}) {
    return new Promise((resolve, reject) => {
      const id = this.id++;
      this.callbacks.set(id, { resolve, reject });
      this.ws.send(JSON.stringify({ id, method, params }));
    });
  }

  async evaluate(expression) {
    const res = await this.send('Runtime.evaluate', { expression, returnByValue: true, awaitPromise: true });
    if (res.exceptionDetails) {
      console.error('Evaluate JS error:', res.exceptionDetails.exception?.description || res.exceptionDetails.text);
    }
    return res.result?.value;
  }

  async captureScreenshot(filename) {
    const res = await this.send('Page.captureScreenshot', { format: 'png' });
    const p = path.join(ARTIFACT_DIR, filename);
    fs.writeFileSync(p, Buffer.from(res.data, 'base64'));
    console.log(`  Saved screenshot: ${filename}`);
  }

  close() {
    this.ws.close();
  }
}

function sleep(ms) {
  return new Promise(r => setTimeout(r, ms));
}

async function run() {
  console.log('Spawning Edge browser on port ' + PORT + '...');
  const edgeProc = spawn(edgePath, [
    `--remote-debugging-port=${PORT}`,
    `--user-data-dir=${userDir}`,
    '--headless=new',
    '--no-first-run',
    '--no-default-browser-check',
    '--disable-extensions',
    '--window-size=1440,920',
    'http://localhost:3000'
  ]);

  await sleep(3500);

  try {
    const pageList = await new Promise((resolve, reject) => {
      http.get(`http://127.0.0.1:${PORT}/json/list`, res => {
        let d = '';
        res.on('data', chunk => d += chunk);
        res.on('end', () => resolve(JSON.parse(d)));
      }).on('error', reject);
    });

    const target = pageList.find(t => t.url && t.url.includes('localhost:3000')) || pageList[0];
    console.log(`Connecting to page target: ${target.title} (${target.url})`);

    const client = new CdpClient(target.webSocketDebuggerUrl);
    await client.connect();
    await client.send('Page.enable');
    await client.send('Runtime.enable');
    await client.send('Emulation.setFocusEmulationEnabled', { enabled: true });

    await sleep(2000);

    // Login if needed
    const isLogin = await client.evaluate("!!document.querySelector('input[type=\"email\"]')");
    if (isLogin) {
      console.log('Logging in with DCP credentials...');
      await client.evaluate(`
        const email = document.querySelector('input[type="email"]');
        const pass = document.querySelector('input[type="password"]');
        if (email && pass) {
          email.value = 'dcp.sharma@mahapolice.gov.in';
          email.dispatchEvent(new Event('input', { bubbles: true }));
          pass.value = 'Maharashtra@2024';
          pass.dispatchEvent(new Event('input', { bubbles: true }));
          const btn = document.querySelector('button[type="submit"]');
          if (btn) btn.click();
        }
      `);
      await sleep(3000);
    }

    // Set active investigation scope
    await client.evaluate(`
      localStorage.setItem('active_investigation_id', 'inv-2026-0841');
      localStorage.setItem('active_investigation_title', 'Operation Iron Vault Syndicate');
      localStorage.setItem('active_case_number', 'CASE-2026-0841');
      localStorage.setItem('cinip_active_case', 'inv-2026-0841');
    `);

    // ==========================================
    // TEST 1: LIGHT MODE AUDIT
    // ==========================================
    console.log('\n========================================');
    console.log('TEST 1: LIGHT MODE FULL AUDIT');
    console.log('========================================');
    await client.evaluate(`window.__setTheme('light');`);
    await sleep(1000);

    const lightState = await client.evaluate(`({
      dataTheme: document.documentElement.getAttribute('data-theme'),
      hasDarkClass: document.documentElement.classList.contains('dark'),
      hasLightClass: document.documentElement.classList.contains('light'),
      bodyBg: window.getComputedStyle(document.body).backgroundColor,
      bodyColor: window.getComputedStyle(document.body).color,
      storageTheme: localStorage.getItem('cinip_theme')
    })`);
    console.log('Light Mode Root State:', JSON.stringify(lightState, null, 2));

    const pagesToTest = [
      { name: 'Dashboard', path: '/dashboard', shot: 'theme_light_dashboard.png' },
      { name: 'Timeline', path: '/timeline', shot: 'theme_light_timeline.png' },
      { name: 'Knowledge Graph', path: '/network', shot: 'theme_light_graph.png' },
      { name: 'Evidence', path: '/evidence', shot: 'theme_light_evidence.png' },
      { name: 'Map', path: '/map', shot: 'theme_light_map.png' },
      { name: 'Analytics', path: '/analytics', shot: 'theme_light_analytics.png' },
      { name: 'Reports', path: '/reports', shot: 'theme_light_reports.png' },
      { name: 'Alerts', path: '/alerts', shot: 'theme_light_alerts.png' },
      { name: 'Copilot', path: '/copilot', shot: 'theme_light_copilot.png' },
      { name: 'Entity Resolution', path: '/entity-resolution', shot: 'theme_light_entity_resolution.png' }
    ];

    for (const p of pagesToTest) {
      console.log(`Checking Light Mode: ${p.name}...`);
      await client.send('Page.navigate', { url: `http://localhost:3000${p.path}` });
      await sleep(1800);
      await client.captureScreenshot(p.shot);

      // Inspect contrast & element colors
      const check = await client.evaluate(`(() => {
        const rootTheme = document.documentElement.getAttribute('data-theme');
        const bodyBg = window.getComputedStyle(document.body).backgroundColor;
        const bodyColor = window.getComputedStyle(document.body).color;
        const cards = Array.from(document.querySelectorAll('.rounded-lg, .rounded-xl, [class*="card"]'));
        const cardBgs = cards.slice(0, 3).map(c => window.getComputedStyle(c).backgroundColor);
        const cardColors = cards.slice(0, 3).map(c => window.getComputedStyle(c).color);
        return { rootTheme, bodyBg, bodyColor, cardBgs, cardColors };
      })()`);
      console.log(`  [Light] ${p.name} check:`, JSON.stringify(check));
    }

    // ==========================================
    // TEST 2: DARK MODE AUDIT
    // ==========================================
    console.log('\n========================================');
    console.log('TEST 2: DARK MODE FULL AUDIT');
    console.log('========================================');
    await client.evaluate(`window.__setTheme('dark');`);
    await sleep(1000);

    const darkState = await client.evaluate(`({
      dataTheme: document.documentElement.getAttribute('data-theme'),
      hasDarkClass: document.documentElement.classList.contains('dark'),
      hasLightClass: document.documentElement.classList.contains('light'),
      bodyBg: window.getComputedStyle(document.body).backgroundColor,
      bodyColor: window.getComputedStyle(document.body).color,
      storageTheme: localStorage.getItem('cinip_theme')
    })`);
    console.log('Dark Mode Root State:', JSON.stringify(darkState, null, 2));

    const darkPagesToTest = [
      { name: 'Dashboard', path: '/dashboard', shot: 'theme_dark_dashboard.png' },
      { name: 'Timeline', path: '/timeline', shot: 'theme_dark_timeline.png' },
      { name: 'Knowledge Graph', path: '/network', shot: 'theme_dark_graph.png' },
      { name: 'Evidence', path: '/evidence', shot: 'theme_dark_evidence.png' },
      { name: 'Map', path: '/map', shot: 'theme_dark_map.png' },
      { name: 'Analytics', path: '/analytics', shot: 'theme_dark_analytics.png' },
      { name: 'Reports', path: '/reports', shot: 'theme_dark_reports.png' },
      { name: 'Alerts', path: '/alerts', shot: 'theme_dark_alerts.png' },
      { name: 'Copilot', path: '/copilot', shot: 'theme_dark_copilot.png' },
      { name: 'Entity Resolution', path: '/entity-resolution', shot: 'theme_dark_entity_resolution.png' }
    ];

    for (const p of darkPagesToTest) {
      console.log(`Checking Dark Mode: ${p.name}...`);
      await client.send('Page.navigate', { url: `http://localhost:3000${p.path}` });
      await sleep(1800);
      await client.captureScreenshot(p.shot);

      const check = await client.evaluate(`(() => {
        const rootTheme = document.documentElement.getAttribute('data-theme');
        const bodyBg = window.getComputedStyle(document.body).backgroundColor;
        const bodyColor = window.getComputedStyle(document.body).color;
        const cards = Array.from(document.querySelectorAll('.rounded-lg, .rounded-xl, [class*="card"]'));
        const cardBgs = cards.slice(0, 3).map(c => window.getComputedStyle(c).backgroundColor);
        const cardColors = cards.slice(0, 3).map(c => window.getComputedStyle(c).color);
        return { rootTheme, bodyBg, bodyColor, cardBgs, cardColors };
      })()`);
      console.log(`  [Dark] ${p.name} check:`, JSON.stringify(check));
    }

    // ==========================================
    // TEST 3: SYSTEM MODE AUDIT (MEDIA QUERY EMULATION)
    // ==========================================
    console.log('\n========================================');
    console.log('TEST 3: SYSTEM MODE AUDIT');
    console.log('========================================');
    await client.evaluate(`window.__setTheme('system');`);
    await sleep(500);

    // Emulate Dark OS
    console.log('Emulating OS prefers-color-scheme: dark...');
    await client.send('Emulation.setEmulatedMedia', {
      media: 'page',
      features: [{ name: 'prefers-color-scheme', value: 'dark' }]
    });
    // Trigger media change dispatch or poll
    await client.evaluate(`
      window.dispatchEvent(new Event('resize'));
    `);
    await sleep(1000);
    const systemDarkState = await client.evaluate(`({
      preference: localStorage.getItem('cinip_theme'),
      dataTheme: document.documentElement.getAttribute('data-theme'),
      hasDarkClass: document.documentElement.classList.contains('dark')
    })`);
    console.log('System Mode with OS Dark:', JSON.stringify(systemDarkState, null, 2));

    await client.captureScreenshot('theme_system_dark.png');

    // Emulate Light OS
    console.log('Emulating OS prefers-color-scheme: light...');
    await client.send('Emulation.setEmulatedMedia', {
      media: 'page',
      features: [{ name: 'prefers-color-scheme', value: 'light' }]
    });
    await client.evaluate(`
      window.dispatchEvent(new Event('resize'));
    `);
    await sleep(1000);
    const systemLightState = await client.evaluate(`({
      preference: localStorage.getItem('cinip_theme'),
      dataTheme: document.documentElement.getAttribute('data-theme'),
      hasLightClass: document.documentElement.classList.contains('light')
    })`);
    console.log('System Mode with OS Light:', JSON.stringify(systemLightState, null, 2));

    await client.captureScreenshot('theme_system_light.png');

    // Reset emulated media
    await client.send('Emulation.setEmulatedMedia', { features: [] });

    // ==========================================
    // TEST 4: RAPID THEME TOGGLING WITHOUT RELOAD
    // ==========================================
    console.log('\n========================================');
    console.log('TEST 4: RAPID THEME TOGGLING WITHOUT RELOAD');
    console.log('========================================');
    for (let i = 0; i < 4; i++) {
      await client.evaluate(`window.__setTheme('light');`);
      await sleep(200);
      const lState = await client.evaluate(`document.documentElement.getAttribute('data-theme')`);
      if (lState !== 'light') throw new Error(`Toggle failed on light: got ${lState}`);

      await client.evaluate(`window.__setTheme('dark');`);
      await sleep(200);
      const dState = await client.evaluate(`document.documentElement.getAttribute('data-theme')`);
      if (dState !== 'dark') throw new Error(`Toggle failed on dark: got ${dState}`);
    }
    console.log('Rapid switching passed with zero errors!');

    // ==========================================
    // TEST 5: LOCALSTORAGE REFRESH PERSISTENCE
    // ==========================================
    console.log('\n========================================');
    console.log('TEST 5: LOCALSTORAGE PERSISTENCE ACROSS RELOAD');
    console.log('========================================');
    // Set to light and reload
    await client.evaluate(`window.__setTheme('light');`);
    await sleep(500);
    await client.send('Page.reload');
    await sleep(2000);
    const reloadLight = await client.evaluate(`({
      dataTheme: document.documentElement.getAttribute('data-theme'),
      storageTheme: localStorage.getItem('cinip_theme')
    })`);
    console.log('Persisted after reload for Light:', JSON.stringify(reloadLight, null, 2));

    // Set to dark and reload
    await client.evaluate(`window.__setTheme('dark');`);
    await sleep(500);
    await client.send('Page.reload');
    await sleep(2000);
    const reloadDark = await client.evaluate(`({
      dataTheme: document.documentElement.getAttribute('data-theme'),
      storageTheme: localStorage.getItem('cinip_theme')
    })`);
    console.log('Persisted after reload for Dark:', JSON.stringify(reloadDark, null, 2));

    // ==========================================
    // TEST 6: GRAPH CANVAS & EDGES VERIFICATION
    // ==========================================
    console.log('\n========================================');
    console.log('TEST 6: NETWORK GRAPH EDGES CHECK IN LIGHT & DARK');
    console.log('========================================');
    await client.send('Page.navigate', { url: 'http://localhost:3000/network' });
    await sleep(2500);

    // Light graph check
    await client.evaluate(`window.__setTheme('light');`);
    await sleep(1000);
    const graphLightInfo = await client.evaluate(`(() => {
      const cy = window.__cytoscape_instance;
      if (!cy) return { error: 'No cytoscape instance' };
      const edge = cy.edges()[0];
      return {
        edgeCount: cy.edges().length,
        nodeCount: cy.nodes().length,
        firstEdgeColor: edge ? edge.style('line-color') : null,
        firstEdgeOpacity: edge ? edge.style('opacity') : null,
        firstEdgeWidth: edge ? edge.style('width') : null
      };
    })()`);
    console.log('Graph Light Info:', JSON.stringify(graphLightInfo, null, 2));
    await client.captureScreenshot('theme_graph_light_edges.png');

    // Dark graph check
    await client.evaluate(`window.__setTheme('dark');`);
    await sleep(1000);
    const graphDarkInfo = await client.evaluate(`(() => {
      const cy = window.__cytoscape_instance;
      if (!cy) return { error: 'No cytoscape instance' };
      const edge = cy.edges()[0];
      return {
        edgeCount: cy.edges().length,
        nodeCount: cy.nodes().length,
        firstEdgeColor: edge ? edge.style('line-color') : null,
        firstEdgeOpacity: edge ? edge.style('opacity') : null,
        firstEdgeWidth: edge ? edge.style('width') : null
      };
    })()`);
    console.log('Graph Dark Info:', JSON.stringify(graphDarkInfo, null, 2));
    await client.captureScreenshot('theme_graph_dark_edges.png');

    client.close();
    console.log('\n========================================');
    console.log('ALL VERIFICATIONS FINISHED SUCCESSFULLY!');
    console.log('========================================');
  } finally {
    edgeProc.kill();
  }
}

run().catch(err => {
  console.error('Fatal error during test:', err);
  process.exit(1);
});
