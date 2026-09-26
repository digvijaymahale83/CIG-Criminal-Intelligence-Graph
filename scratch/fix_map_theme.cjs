const fs = require('fs');
const path = require('path');

const mapFilePath = path.resolve(__dirname, '../src/features/map/GeospatialMapPage.tsx');
let code = fs.readFileSync(mapFilePath, 'utf8');

// 1. Add useTheme import if missing
if (!code.includes("useTheme")) {
  code = code.replace(
    "import { ResponsibleAiNotice } from '../../components/common/ResponsibleAiNotice';",
    "import { ResponsibleAiNotice } from '../../components/common/ResponsibleAiNotice';\nimport { useTheme } from '../../hooks/useTheme';"
  );
}

// 2. Add resolvedTheme and tileLayerRef
if (!code.includes("const { resolvedTheme } = useTheme();")) {
  code = code.replace(
    "const effectiveCaseId = searchParams.get('caseId') || activeCaseId || 'case-2026-001';",
    "const effectiveCaseId = searchParams.get('caseId') || activeCaseId || 'case-2026-001';\n  const { resolvedTheme } = useTheme();\n  const tileLayerRef = useRef<L.TileLayer | null>(null);"
  );
}

// 3. Update Leaflet initialization
const oldInitPattern = `    // Dark Basemap CartoDB
    L.tileLayer('https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png', {
      attribution: '© CartoDB',
      subdomains: 'abcd',
      maxZoom: 19
    }).addTo(map);`;

const newInitReplacement = `    // Dynamic Basemap CartoDB (follows resolvedTheme)
    const tileUrl = resolvedTheme === 'dark'
      ? 'https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png'
      : 'https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png';

    tileLayerRef.current = L.tileLayer(tileUrl, {
      attribution: '© CartoDB',
      subdomains: 'abcd',
      maxZoom: 19
    }).addTo(map);`;

if (code.includes(oldInitPattern)) {
  code = code.replace(oldInitPattern, newInitReplacement);
}

// 4. Add dynamic tile layer update effect after map init effect
if (!code.includes("tileLayerRef.current = L.tileLayer(tileUrl")) {
  const mapInitReturn = `    return () => {
      map.remove();
      mapInstanceRef.current = null;
    };
  }, []);`;

  const mapInitWithThemeEffect = `    return () => {
      map.remove();
      mapInstanceRef.current = null;
    };
  }, []);

  // Dynamically update tile layer when resolvedTheme changes
  useEffect(() => {
    if (!mapInstanceRef.current) return;
    if (tileLayerRef.current) {
      mapInstanceRef.current.removeLayer(tileLayerRef.current);
    }
    const tileUrl = resolvedTheme === 'dark'
      ? 'https://{s}.basemaps.cartocdn.com/dark_all/{z}/{x}/{y}{r}.png'
      : 'https://{s}.basemaps.cartocdn.com/light_all/{z}/{x}/{y}{r}.png';

    tileLayerRef.current = L.tileLayer(tileUrl, {
      attribution: '© CartoDB',
      subdomains: 'abcd',
      maxZoom: 19
    }).addTo(mapInstanceRef.current);
  }, [resolvedTheme]);`;

  code = code.replace(mapInitReturn, mapInitWithThemeEffect);
}

// 5. Replace hardcoded slate colors with semantic theme variables
code = code.replace(/bg-\[#0f172a\]/g, 'bg-[var(--color-surface-subtle)]');
code = code.replace(/border-\[#1e293b\]/g, 'border-[var(--color-border)]');
code = code.replace(/border-\[#334155\]/g, 'border-[var(--color-border)]');
code = code.replace(/bg-\[#1e293b\]/g, 'bg-[var(--color-surface)]');
code = code.replace(/hover:bg-\[#1e293b\]/g, 'hover:bg-[var(--color-surface-subtle)]');
code = code.replace(/hover:bg-\[#334155\]/g, 'hover:bg-[var(--color-surface-subtle)]');
code = code.replace(/bg-\[#020617\]/g, 'bg-[var(--color-surface)]');
code = code.replace(/text-\[#94a3b8\]/g, 'text-[var(--color-text-secondary)]');
code = code.replace(/text-\[#f8fafc\]/g, 'text-[var(--color-text-primary)]');
code = code.replace(/text-\[#cbd5e1\]/g, 'text-[var(--color-text-secondary)]');
code = code.replace(/text-\[#64748b\]/g, 'text-[var(--color-text-muted)]');

fs.writeFileSync(mapFilePath, code, 'utf8');
console.log('Successfully updated GeospatialMapPage.tsx for theme consistency!');
