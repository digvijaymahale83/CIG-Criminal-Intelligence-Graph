import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { Shield, Lock, Mail, Eye, EyeOff, AlertCircle } from 'lucide-react';
import { useAuth } from '../../hooks/useAuth';

export const LoginPage: React.FC = () => {
  const navigate = useNavigate();
  const { login } = useAuth();
  const [email, setEmail] = useState('dcp.sharma@mahapolice.gov.in');
  const [password, setPassword] = useState('Maharashtra@2024');
  const [showPassword, setShowPassword] = useState(false);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!email || !password) {
      setError('Email and password are required.');
      return;
    }
    setError(null);
    setIsLoading(true);
    try {
      await login(email, password);
      navigate('/dashboard');
    } catch (err: unknown) {
      const msg = err instanceof Error ? err.message : 'Authentication failed. Check credentials.';
      setError(msg.includes('fetch') || msg.includes('Network')
        ? 'Cannot reach the server. Ensure the backend is running on port 4000.'
        : 'Invalid credentials. Please try again.');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div
      id="login-page"
      className="min-h-screen flex flex-col justify-center py-12 sm:px-6 lg:px-8"
      style={{ background: 'linear-gradient(135deg, #0d1117 0%, #0f1923 60%, #0d1117 100%)' }}
    >
      {/* Subtle grid background */}
      <div className="absolute inset-0 opacity-[0.03]" style={{
        backgroundImage: 'linear-gradient(#e6edf3 1px, transparent 1px), linear-gradient(90deg, #e6edf3 1px, transparent 1px)',
        backgroundSize: '40px 40px',
      }} />

      <div className="sm:mx-auto sm:w-full sm:max-w-md relative z-10">
        {/* Logo block */}
        <div className="text-center mb-6">
          <div className="relative inline-flex">
            <div className="w-16 h-16 rounded-2xl bg-gradient-to-br from-[#1f6feb] to-[#388bfd] flex items-center justify-center shadow-lg shadow-blue-900/40 mb-4">
              <Shield className="w-8 h-8 text-white" />
            </div>
            <span className="absolute -top-1 -right-1 w-4 h-4 rounded-full bg-[#3fb950] border-2 border-[#0d1117] animate-pulse" />
          </div>
          <h1 className="text-xl font-bold tracking-tight text-[var(--color-text-primary)]">Criminal Intelligence &amp; Network Investigation Platform</h1>
          <div className="flex items-center justify-center gap-2 mt-1">
            <span className="w-6 h-px bg-[#30363d]" />
            <p className="text-xs text-[var(--color-text-secondary)] font-mono tracking-wider uppercase">
              Synthetic Investigation &amp; Research Platform · SIH Prototype
            </p>
            <span className="w-6 h-px bg-[#30363d]" />
          </div>
        </div>

        {/* Card */}
        <div className="bg-[var(--color-surface)] border border-[var(--color-border)] rounded-2xl shadow-2xl shadow-black/50 overflow-hidden">
          {/* Top accent bar */}
          <div className="h-0.5 bg-gradient-to-r from-[#1f6feb] via-[#388bfd] to-[#1f6feb]" />

          <div className="p-8">
            <div className="mb-6">
              <h2 className="text-sm font-semibold text-[var(--color-text-primary)]">Secure Authentication</h2>
              <p className="text-xs text-[var(--color-text-secondary)] mt-0.5">Authorized personnel only. All sessions are logged.</p>
            </div>

            <form onSubmit={handleSubmit} className="space-y-4">
              {/* Email */}
              <div>
                <label className="block text-xs font-semibold text-[var(--color-text-secondary)] mb-1.5 uppercase tracking-wide">
                  Official Agency Email
                </label>
                <div className="relative">
                  <Mail className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-[var(--color-text-muted)]" />
                  <input
                    id="login-email"
                    type="email"
                    value={email}
                    onChange={e => setEmail(e.target.value)}
                    className="w-full bg-[var(--color-bg-canvas)] border border-[var(--color-border)] rounded-lg px-3 py-2.5 text-sm text-[var(--color-text-primary)] focus:border-[#388bfd] focus:ring-1 focus:ring-[#388bfd]/30 outline-none pl-9 transition-all placeholder:text-[var(--color-text-muted)]"
                    placeholder="officer@mahapolice.gov.in"
                    autoComplete="username"
                  />
                </div>
              </div>

              {/* Password */}
              <div>
                <label className="block text-xs font-semibold text-[var(--color-text-secondary)] mb-1.5 uppercase tracking-wide">
                  Security Password
                </label>
                <div className="relative">
                  <Lock className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-[var(--color-text-muted)]" />
                  <input
                    id="login-password"
                    type={showPassword ? 'text' : 'password'}
                    value={password}
                    onChange={e => setPassword(e.target.value)}
                    className="w-full bg-[var(--color-bg-canvas)] border border-[var(--color-border)] rounded-lg px-3 py-2.5 text-sm text-[var(--color-text-primary)] focus:border-[#388bfd] focus:ring-1 focus:ring-[#388bfd]/30 outline-none pl-9 pr-10 transition-all"
                    placeholder="••••••••••••"
                    autoComplete="current-password"
                  />
                  <button
                    type="button"
                    onClick={() => setShowPassword(v => !v)}
                    className="absolute right-3 top-1/2 -translate-y-1/2 text-[var(--color-text-muted)] hover:text-[var(--color-text-secondary)] transition-colors"
                  >
                    {showPassword ? <EyeOff className="w-4 h-4" /> : <Eye className="w-4 h-4" />}
                  </button>
                </div>
              </div>

              {/* Error */}
              {error && (
                <div className="flex items-start gap-2 p-3 rounded-lg bg-[#f85149]/10 border border-[#f85149]/30">
                  <AlertCircle className="w-4 h-4 text-[#f85149] shrink-0 mt-0.5" />
                  <p className="text-xs text-[#f85149]">{error}</p>
                </div>
              )}

              {/* Submit */}
              <button
                id="login-submit"
                type="submit"
                disabled={isLoading}
                className="w-full flex items-center justify-center gap-2 px-4 py-2.5 rounded-lg bg-gradient-to-r from-[#1f6feb] to-[#388bfd] text-white text-sm font-semibold hover:from-[#388bfd] hover:to-[#58a6ff] transition-all shadow-lg shadow-blue-900/30 disabled:opacity-60 disabled:cursor-not-allowed cursor-pointer"
              >
                {isLoading ? (
                  <>
                    <span className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" />
                    Authenticating…
                  </>
                ) : (
                  <>
                    <Shield className="w-4 h-4" />
                    Authenticate Session
                  </>
                )}
              </button>
            </form>

            {/* Demo accounts */}
            <div className="mt-6 pt-5 border-t border-[var(--color-border-subtle)]">
              <p className="text-[10px] text-[var(--color-text-muted)] font-semibold uppercase tracking-widest mb-2">Demo Accounts</p>
              <div className="grid grid-cols-2 gap-1.5">
                {[
                  { label: 'DCP (Admin)', email: 'dcp.sharma@mahapolice.gov.in' },
                  { label: 'PI (Investigator)', email: 'pi.deshmukh@mahapolice.gov.in' },
                  { label: 'ASI (Analyst)', email: 'asi.patil@mahapolice.gov.in' },
                  { label: 'SP (Supervisor)', email: 'sp.kulkarni@mahapolice.gov.in' },
                ].map(acc => (
                  <button
                    key={acc.email}
                    type="button"
                    onClick={() => { setEmail(acc.email); setPassword('Maharashtra@2024'); }}
                    className="text-left px-2 py-1.5 rounded-md bg-[var(--color-bg-elevated)] border border-[var(--color-border)] hover:border-[#388bfd]/40 transition-colors cursor-pointer"
                  >
                    <p className="text-[9px] font-bold text-[#79c0ff]">{acc.label}</p>
                    <p className="text-[8px] text-[var(--color-text-muted)] font-mono truncate">{acc.email.split('@')[0]}</p>
                  </button>
                ))}
              </div>
              <p className="text-[10px] text-[var(--color-text-muted)] mt-2 text-center">All use password: <span className="font-mono text-[var(--color-text-secondary)]">Maharashtra@2024</span></p>
            </div>
          </div>

          <div className="px-8 py-3 bg-[var(--color-bg-canvas)] border-t border-[var(--color-border-subtle)] text-center">
            <p className="text-[10px] text-[var(--color-text-muted)]">
              Immutable Audit Logging Active · Unauthorized access is a criminal offence under IT Act 2000
            </p>
          </div>
        </div>
      </div>
    </div>
  );
};
