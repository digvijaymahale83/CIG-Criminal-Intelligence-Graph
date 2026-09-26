const fs = require('fs');
const path = require('path');

const PARCHMENT_REPLACEMENTS = [
  // Backgrounds
  { from: /bg-\[#FAF7F2\]/g, to: 'bg-[var(--color-surface)]' },
  { from: /bg-\[#F4EDE4\]/g, to: 'bg-[var(--color-surface-subtle)]' },
  { from: /bg-\[#EDE8E1\]/g, to: 'bg-[var(--color-surface-subtle)]' },
  { from: /bg-\[#FAF0E6\]/g, to: 'bg-[var(--color-surface-subtle)]' },
  { from: /bg-\[#FBE9E7\]/g, to: 'bg-[var(--color-surface-subtle)]' },
  { from: /bg-\[#FFFFFF\]/g, to: 'bg-[var(--color-surface)]' },
  { from: /bg-\[#EFE2D2\]/g, to: 'bg-[var(--color-surface-subtle)]' },
  { from: /bg-\[#EFE5D8\]/g, to: 'bg-[var(--color-surface-subtle)]' },
  { from: /bg-\[#DFD1BF\]/g, to: 'bg-[var(--color-surface-subtle)]' },
  { from: /bg-\[#F2E8DD\]/g, to: 'bg-[var(--color-surface-subtle)]' },
  { from: /bg-\[#EAE0D3\]/g, to: 'bg-[var(--color-surface-subtle)]' },
  { from: /hover:bg-\[#F4EDE4\]/g, to: 'hover:bg-[var(--color-surface-subtle)]' },
  { from: /hover:bg-\[#F9F6F0\]/g, to: 'hover:bg-[var(--color-surface-subtle)]' },
  { from: /hover:bg-\[#FAF0E6\]/g, to: 'hover:bg-[var(--color-surface-subtle)]' },

  // Borders
  { from: /border-\[#E2D5C3\]/g, to: 'border-[var(--color-border)]' },
  { from: /border-\[#DFCDBB\]/g, to: 'border-[var(--color-border)]' },
  { from: /border-\[#D9C9B6\]/g, to: 'border-[var(--color-border)]' },
  { from: /border-\[#EFE5D8\]/g, to: 'border-[var(--color-border)]' },
  { from: /border-\[#E3CEBC\]/g, to: 'border-[var(--color-border)]' },
  { from: /border-\[#EDE8E1\]/g, to: 'border-[var(--color-border-subtle)]' },
  { from: /border-\[#DFD1BF\]/g, to: 'border-[var(--color-border-subtle)]' },
  { from: /before:bg-\[#E2D5C3\]/g, to: 'before:bg-[var(--color-border)]' },

  // Primary text
  { from: /text-\[#28201A\]/g, to: 'text-[var(--color-text-primary)]' },
  { from: /text-\[#5C4C3E\]/g, to: 'text-[var(--color-text-primary)]' },
  { from: /text-\[#3D3128\]/g, to: 'text-[var(--color-text-primary)]' },
  { from: /text-\[#3D3025\]/g, to: 'text-[var(--color-text-primary)]' },
  { from: /text-\[#4E3F30\]/g, to: 'text-[var(--color-text-primary)]' },
  { from: /text-\[#4A3D31\]/g, to: 'text-[var(--color-text-primary)]' },

  // Secondary text
  { from: /text-\[#6E5D4F\]/g, to: 'text-[var(--color-text-secondary)]' },
  { from: /text-\[#5A4E42\]/g, to: 'text-[var(--color-text-secondary)]' },
  { from: /text-\[#5E4E40\]/g, to: 'text-[var(--color-text-secondary)]' },
  { from: /hover:text-\[#28201A\]/g, to: 'hover:text-[var(--color-text-primary)]' },

  // Muted text
  { from: /text-\[#7A6959\]/g, to: 'text-[var(--color-text-muted)]' },
  { from: /text-\[#8C7A6B\]/g, to: 'text-[var(--color-text-muted)]' },

  // Accent rust brown -> semantic accent blue
  { from: /text-\[#A6522C\]/g, to: 'text-[var(--color-accent)]' },
  { from: /bg-\[#A6522C\]/g, to: 'bg-[var(--color-accent)]' },
  { from: /border-\[#A6522C\]/g, to: 'border-[var(--color-accent)]' },
  { from: /hover:bg-\[#8D4424\]/g, to: 'hover:bg-[var(--color-accent-hover)]' },
  { from: /hover:bg-\[#8F4220\]/g, to: 'hover:bg-[var(--color-accent-hover)]' },
  { from: /hover:bg-\[#8C4323\]/g, to: 'hover:bg-[var(--color-accent-hover)]' },
  { from: /focus:border-\[#A6522C\]/g, to: 'focus:border-[var(--color-accent)]' },
  { from: /hover:border-\[#A6522C\]/g, to: 'hover:border-[var(--color-accent)]' },
  { from: /hover:text-\[#A6522C\]/g, to: 'hover:text-[var(--color-accent)]' },
];

const DARK_HARDCODED_REPLACEMENTS = [
  // Backgrounds
  { from: /bg-\[#161b22\]/g, to: 'bg-[var(--color-surface)]' },
  { from: /bg-\[#0d1117\]/g, to: 'bg-[var(--color-bg-canvas)]' },
  { from: /bg-\[#21262d\]/g, to: 'bg-[var(--color-surface-subtle)]' },
  { from: /bg-\[#1c2128\]/g, to: 'bg-[var(--color-bg-elevated)]' },
  { from: /hover:bg-\[#161b22\]/g, to: 'hover:bg-[var(--color-surface-subtle)]' },
  { from: /hover:bg-\[#1c2128\]/g, to: 'hover:bg-[var(--color-surface-subtle)]' },
  { from: /hover:bg-\[#21262d\]/g, to: 'hover:bg-[var(--color-surface-subtle)]' },
  { from: /hover:bg-\[#30363d\]/g, to: 'hover:bg-[var(--color-surface-subtle)]' },

  // Borders
  { from: /border-\[#30363d\]/g, to: 'border-[var(--color-border)]' },
  { from: /border-\[#21262d\]/g, to: 'border-[var(--color-border-subtle)]' },

  // Text
  { from: /text-\[#e6edf3\]/g, to: 'text-[var(--color-text-primary)]' },
  { from: /text-\[#c9d1d9\]/g, to: 'text-[var(--color-text-primary)]' },
  { from: /hover:text-\[#c9d1d9\]/g, to: 'hover:text-[var(--color-text-primary)]' },
  { from: /text-\[#8b949e\]/g, to: 'text-[var(--color-text-secondary)]' },
  { from: /text-\[#6e7681\]/g, to: 'text-[var(--color-text-muted)]' },
  { from: /text-\[#484f58\]/g, to: 'text-[var(--color-text-muted)]' },
  { from: /placeholder:text-\[#484f58\]/g, to: 'placeholder:text-[var(--color-text-muted)]' },
  { from: /placeholder-\[#484f58\]/g, to: 'placeholder-[var(--color-text-muted)]' },
];

const TARGET_FILES = [
  'src/features/timeline/TimelinePage.tsx',
  'src/features/evidence/EvidenceListPage.tsx',
  'src/features/evidence/EvidenceDetailPage.tsx',
  'src/features/reports/ReportsPage.tsx',
  'src/features/investigations/InvestigationsPage.tsx',
  'src/features/investigations/NewInvestigationPage.tsx',
  'src/features/entity-resolution/EntityResolutionPage.tsx',
  'src/features/audit/AuditLogPage.tsx',
  'src/features/copilot/CopilotPage.tsx',
  'src/features/assistant/AssistantPage.tsx',
  'src/features/settings/SettingsPage.tsx',
  'src/features/system-status/SystemStatusPage.tsx',
  'src/features/entities/EntitiesPage.tsx',
  'src/features/analytics/AnalyticsPage.tsx',
  'src/features/alerts/AlertsPage.tsx',
  'src/features/auth/LoginPage.tsx',
];

const baseDir = path.resolve(__dirname, '..');

for (const relFile of TARGET_FILES) {
  const fullPath = path.join(baseDir, relFile);
  if (!fs.existsSync(fullPath)) {
    console.log(`Skipping missing: ${relFile}`);
    continue;
  }
  let content = fs.readFileSync(fullPath, 'utf8');
  let original = content;

  for (const { from, to } of PARCHMENT_REPLACEMENTS) {
    content = content.replace(from, to);
  }

  for (const { from, to } of DARK_HARDCODED_REPLACEMENTS) {
    content = content.replace(from, to);
  }

  if (content !== original) {
    fs.writeFileSync(fullPath, content, 'utf8');
    console.log(`Updated theme tokens in: ${relFile}`);
  } else {
    console.log(`No changes needed in: ${relFile}`);
  }
}
