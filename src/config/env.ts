/**
 * Environment Configuration
 * Only public VITE_* environment variables are read here.
 * Never store secrets or API keys in client-side environment variables.
 */

export const env = {
  apiBaseUrl: import.meta.env.VITE_API_BASE_URL || '',
  appEnv: import.meta.env.MODE || 'development',
  isDev: import.meta.env.DEV,
  isProd: import.meta.env.PROD,
  systemStatusPollingIntervalMs: 30000, // 30 seconds as specified in requirements
};
