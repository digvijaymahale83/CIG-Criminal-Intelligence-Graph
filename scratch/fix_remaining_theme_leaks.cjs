const fs = require('fs');
const path = require('path');

function replaceInFile(filePath, replacements) {
  let content = fs.readFileSync(filePath, 'utf8');
  for (const [target, replacement] of replacements) {
    if (typeof target === 'string') {
      content = content.split(target).join(replacement);
    } else if (target instanceof RegExp) {
      content = content.replace(target, replacement);
    }
  }
  fs.writeFileSync(filePath, content, 'utf8');
  console.log(`Updated ${filePath}`);
}

// 1. TimelinePage.tsx: replace bg-white on inputs, selects, cards, and timeline dots
replaceInFile('src/features/timeline/TimelinePage.tsx', [
  ['bg-white text-[var(--color-text-primary)]', 'bg-[var(--color-surface)] text-[var(--color-text-primary)]'],
  ['bg-white text-xs', 'bg-[var(--color-surface)] text-[var(--color-text-primary)] text-xs'],
  ['bg-white border border-[var(--color-border)]', 'bg-[var(--color-surface)] border border-[var(--color-border)]'],
  ['bg-white hover:border-[var(--color-accent)]', 'bg-[var(--color-surface)] hover:border-[var(--color-accent)]'],
  ['bg-white space-y', 'bg-[var(--color-surface)] space-y'],
  ['bg-white text-xs', 'bg-[var(--color-surface)] text-[var(--color-text-primary)] text-xs'],
  ['rounded-full border-2 bg-white', 'rounded-full border-2 bg-[var(--color-surface)]'],
  ['border-[#7A6959]', 'border-[var(--color-border)]']
]);

// 2. CopilotPage.tsx: replace bg-white, border-[#E0D2C0], and bg-[#28201A]
replaceInFile('src/features/copilot/CopilotPage.tsx', [
  ['border-[#E0D2C0]', 'border-[var(--color-border)]'],
  ['bg-white px-6 py-4', 'bg-[var(--color-surface)] px-6 py-4'],
  ['bg-white border border-[var(--color-border-subtle)]', 'bg-[var(--color-surface)] border border-[var(--color-border-subtle)]'],
  ['bg-white border border-[var(--color-border)]', 'bg-[var(--color-surface)] border border-[var(--color-border)]'],
  ['bg-white hover:bg-[var(--color-surface-subtle)]', 'bg-[var(--color-surface)] hover:bg-[var(--color-surface-subtle)]'],
  ['bg-white p-4', 'bg-[var(--color-surface)] p-4'],
  ['bg-white rounded-2xl', 'bg-[var(--color-surface)] rounded-2xl'],
  ['bg-white p-2', 'bg-[var(--color-surface)] p-2'],
  ['bg-white p-3', 'bg-[var(--color-surface)] p-3'],
  ['bg-[#28201A] text-white', 'bg-[var(--color-accent)] text-white']
]);

// 3. AnalyticsPage.tsx: replace bg-white and #E2D5C3
replaceInFile('src/features/analytics/AnalyticsPage.tsx', [
  ['bg-white border border-[var(--color-border)]', 'bg-[var(--color-surface)] border border-[var(--color-border)]'],
  ['bg-white rounded-xl', 'bg-[var(--color-surface)] rounded-xl'],
  ['divide-[#E2D5C3]', 'divide-[var(--color-border)]'],
  ['bg-[#E2D5C3]', 'bg-[var(--color-border)]']
]);

// 4. AlertsPage.tsx: replace active filter pill and dismiss button
replaceInFile('src/features/alerts/AlertsPage.tsx', [
  ["? 'bg-[#28201A] text-white'", "? 'bg-[var(--color-text-primary)] text-[var(--color-bg-canvas)]'"],
  ["'bg-[#7A6959] hover:bg-[#605245]'", "'bg-[var(--color-surface-subtle)] hover:bg-[var(--color-surface-secondary)] text-[var(--color-text-primary)] border border-[var(--color-border)]'"]
]);

// 5. EvidenceDetailPage.tsx: replace bg-slate-900, bg-slate-950, text-slate-*, border-slate-*
replaceInFile('src/features/evidence/EvidenceDetailPage.tsx', [
  ['bg-slate-900 border-slate-800', 'bg-[var(--color-surface)] border-[var(--color-border)]'],
  ['bg-slate-950 border-slate-800', 'bg-[var(--color-surface-subtle)] border-[var(--color-border)]'],
  ['bg-slate-950 text-slate-200', 'bg-[var(--color-surface-subtle)] text-[var(--color-text-primary)]'],
  ['bg-slate-950 p-3', 'bg-[var(--color-surface-subtle)] p-3'],
  ['bg-slate-950 p-2', 'bg-[var(--color-surface-subtle)] p-2'],
  ['bg-slate-950 p-1', 'bg-[var(--color-surface-subtle)] p-1'],
  ['bg-slate-950 rounded', 'bg-[var(--color-surface-subtle)] rounded'],
  ['text-slate-300', 'text-[var(--color-text-secondary)]'],
  ['text-slate-400', 'text-[var(--color-text-muted)]'],
  ['text-slate-200', 'text-[var(--color-text-primary)]'],
  ['text-slate-100', 'text-[var(--color-text-primary)]'],
  ['border-slate-700', 'border-[var(--color-border)]'],
  ['border-slate-800', 'border-[var(--color-border)]'],
  ['hover:bg-slate-800', 'hover:bg-[var(--color-surface-subtle)]'],
  ['hover:bg-slate-850', 'hover:bg-[var(--color-surface-subtle)]'],
  ['hover:bg-slate-750', 'hover:bg-[var(--color-surface-subtle)]'],
  ['bg-slate-800', 'bg-[var(--color-surface-subtle)]'],
  ['bg-slate-950', 'bg-[var(--color-surface-subtle)]']
]);

// 6. ExtractionReviewPage.tsx: replace slate colors
replaceInFile('src/features/evidence/ExtractionReviewPage.tsx', [
  ['bg-slate-900/90 border-slate-800', 'bg-[var(--color-surface)] border-[var(--color-border)]'],
  ['bg-slate-900/60 border-slate-800', 'bg-[var(--color-surface)] border-[var(--color-border)]'],
  ['bg-slate-900/40 border-slate-800', 'bg-[var(--color-surface)] border-[var(--color-border)]'],
  ['bg-slate-900 border-slate-800', 'bg-[var(--color-surface)] border-[var(--color-border)]'],
  ['bg-slate-900 border-slate-700', 'bg-[var(--color-surface)] border-[var(--color-border)]'],
  ['bg-slate-950/60 border-slate-800/80', 'bg-[var(--color-surface-subtle)] border-[var(--color-border)]'],
  ['bg-slate-950 border border-slate-700 rounded px-3 py-1.5 text-sm text-slate-100', 'bg-[var(--color-surface)] border border-[var(--color-border)] rounded px-3 py-1.5 text-sm text-[var(--color-text-primary)]'],
  ['bg-slate-950 p-3', 'bg-[var(--color-surface-subtle)] p-3'],
  ['text-slate-300', 'text-[var(--color-text-secondary)]'],
  ['text-slate-400', 'text-[var(--color-text-muted)]'],
  ['text-slate-100', 'text-[var(--color-text-primary)]'],
  ['border-slate-700', 'border-[var(--color-border)]'],
  ['border-slate-800', 'border-[var(--color-border)]'],
  ['hover:bg-slate-800', 'hover:bg-[var(--color-surface-subtle)]'],
  ['hover:border-slate-700', 'hover:border-[var(--color-accent)]']
]);

// 7. EvidenceIntegrityModal.tsx: replace slate colors
replaceInFile('src/features/evidence/components/EvidenceIntegrityModal.tsx', [
  ['bg-slate-900/95 backdrop-blur border-b border-slate-800', 'bg-[var(--color-surface)]/95 backdrop-blur border-b border-[var(--color-border)]'],
  ['bg-slate-900/95 border-t border-slate-800', 'bg-[var(--color-surface)]/95 border-t border-[var(--color-border)]'],
  ['bg-slate-900 border border-slate-700 rounded-xl shadow-2xl text-slate-100', 'bg-[var(--color-surface)] border border-[var(--color-border)] rounded-xl shadow-2xl text-[var(--color-text-primary)]'],
  ['bg-slate-900/90', 'bg-[var(--color-surface-subtle)]'],
  ['bg-slate-950', 'bg-[var(--color-surface-subtle)]'],
  ['bg-slate-800/50', 'bg-[var(--color-surface-subtle)]'],
  ['bg-slate-800', 'bg-[var(--color-surface-subtle)]'],
  ['border-slate-800/80', 'border-[var(--color-border)]'],
  ['border-slate-800', 'border-[var(--color-border)]'],
  ['border-slate-700', 'border-[var(--color-border)]'],
  ['text-slate-100', 'text-[var(--color-text-primary)]'],
  ['text-slate-200', 'text-[var(--color-text-primary)]'],
  ['text-slate-300', 'text-[var(--color-text-secondary)]'],
  ['text-slate-400', 'text-[var(--color-text-muted)]'],
  ['text-slate-500', 'text-[var(--color-text-muted)]'],
  ['hover:bg-slate-800', 'hover:bg-[var(--color-surface-subtle)]'],
  ['hover:bg-slate-700', 'hover:bg-[var(--color-surface-subtle)]'],
  ['hover:bg-slate-850', 'hover:bg-[var(--color-surface-subtle)]'],
  ['hover:text-slate-200', 'hover:text-[var(--color-text-primary)]']
]);

console.log('All remaining theme leaks replaced with semantic tokens successfully!');
