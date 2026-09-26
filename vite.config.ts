/// <reference types="vitest" />
import tailwindcss from '@tailwindcss/vite';
import react from '@vitejs/plugin-react';
import path from 'path';
import { defineConfig, type Plugin } from 'vite';

// Real Phase 1 backend endpoint handler for Vite dev server
function systemStatusApiPlugin(): Plugin {
  let simulatedMode: 'ready' | 'degraded' | 'malformed' | 'error' = 'ready';

  return {
    name: 'phase1-system-status-api',
    configureServer(server) {
      server.middlewares.use((req, res, next) => {
        const url = new URL(req.url || '/', 'http://localhost:3000');

        if (url.pathname === '/api/v1/system/status/simulate' && req.method === 'POST') {
          let body = '';
          req.on('data', (chunk) => { body += chunk; });
          req.on('end', () => {
            try {
              const data = JSON.parse(body);
              if (['ready', 'degraded', 'malformed', 'error'].includes(data.mode)) {
                simulatedMode = data.mode;
              }
              res.setHeader('Content-Type', 'application/json');
              res.end(JSON.stringify({ activeMode: simulatedMode }));
            } catch {
              res.statusCode = 400;
              res.end(JSON.stringify({ error: 'Invalid payload' }));
            }
          });
          return;
        }

        const modeParam = url.searchParams.get('simulate') || url.searchParams.get('mode');
        const activeMode = modeParam || simulatedMode;

        if (url.pathname === '/api/v1/system/status') {
          res.setHeader('Content-Type', 'application/json');
          if (activeMode === 'malformed') {
            res.statusCode = 200;
            res.end(JSON.stringify({ status: 100, invalidKey: true }));
            return;
          }
          if (activeMode === 'error') {
            res.statusCode = 500;
            res.end(JSON.stringify({ error: 'Internal Server Error' }));
            return;
          }
          if (activeMode === 'degraded') {
            res.statusCode = 503;
            res.end(JSON.stringify({
              status: 'degraded',
              checkedAtUtc: new Date().toISOString(),
              services: [
                { name: 'postgres', healthy: true },
                { name: 'neo4j', healthy: false },
                { name: 'redis', healthy: true },
                { name: 'ai-service', healthy: true },
              ],
              isReady: false,
            }));
            return;
          }

          // Ready (HTTP 200)
          res.statusCode = 200;
          res.end(JSON.stringify({
            status: 'ready',
            checkedAtUtc: new Date().toISOString(),
            services: [
              { name: 'postgres', healthy: true },
              { name: 'neo4j', healthy: true },
              { name: 'redis', healthy: true },
              { name: 'ai-service', healthy: true },
            ],
            isReady: true,
          }));
          return;
        }

        if (url.pathname === '/health' || url.pathname === '/health/ready') {
          res.setHeader('Content-Type', 'application/json');
          res.statusCode = activeMode === 'degraded' ? 503 : 200;
          res.end(JSON.stringify({ status: activeMode === 'degraded' ? 'degraded' : 'ready' }));
          return;
        }

        if (url.pathname === '/health/live') {
          res.setHeader('Content-Type', 'application/json');
          res.statusCode = 200;
          res.end(JSON.stringify({ status: 'live' }));
          return;
        }

        next();
      });
    },
  };
}

export default defineConfig(() => {
  return {
    plugins: [react(), tailwindcss(), systemStatusApiPlugin()],
    resolve: {
      alias: {
        '@': path.resolve(__dirname, './src'),
      },
    },
    server: {
      hmr: process.env.DISABLE_HMR !== 'true',
      watch: process.env.DISABLE_HMR === 'true' ? null : {},
    },
    test: {
      globals: true,
      environment: 'jsdom',
      setupFiles: './src/test/setup.ts',
    },
  };
});
