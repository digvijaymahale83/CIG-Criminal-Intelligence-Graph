export interface NavItemConfig {
  id: string;
  label: string;
  path: string;
  iconName: string;
  requiresInvestigation?: boolean;
  requiredPermission?: string;
  phase: number;
  badge?: string;
}

export const MAIN_NAV_ITEMS: NavItemConfig[] = [
  {
    id: 'dashboard',
    label: 'Dashboard',
    path: '/dashboard',
    iconName: 'LayoutDashboard',
    phase: 1,
  },
  {
    id: 'system-status',
    label: 'System Status',
    path: '/system-status',
    iconName: 'Activity',
    phase: 1,
    badge: 'LIVE',
  },
  {
    id: 'investigations',
    label: 'Investigations',
    path: '/investigations',
    iconName: 'FolderGit2',
    phase: 3,
  },
  {
    id: 'evidence',
    label: 'Evidence',
    path: '/evidence',
    iconName: 'FileCheck2',
    requiresInvestigation: true,
    phase: 4,
  },
  {
    id: 'entities',
    label: 'Entities',
    path: '/entities',
    iconName: 'Users2',
    requiresInvestigation: true,
    phase: 7,
  },
  {
    id: 'network',
    label: 'Network Graph',
    path: '/network',
    iconName: 'Network',
    requiresInvestigation: true,
    phase: 8,
  },
  {
    id: 'analytics',
    label: 'Analytics',
    path: '/analytics',
    iconName: 'BarChart3',
    requiresInvestigation: true,
    phase: 9,
  },
  {
    id: 'anomalies',
    label: 'Anomalies',
    path: '/anomalies',
    iconName: 'AlertTriangle',
    requiresInvestigation: true,
    phase: 10,
  },
  {
    id: 'timeline',
    label: 'Timeline',
    path: '/timeline',
    iconName: 'Clock',
    requiresInvestigation: true,
    phase: 11,
  },
  {
    id: 'map',
    label: 'Map',
    path: '/map',
    iconName: 'MapPin',
    requiresInvestigation: true,
    phase: 11,
  },
  {
    id: 'assistant',
    label: 'AI Assistant',
    path: '/assistant',
    iconName: 'Bot',
    requiresInvestigation: true,
    phase: 13,
  },
  {
    id: 'reports',
    label: 'Reports',
    path: '/reports',
    iconName: 'FileText',
    requiresInvestigation: true,
    phase: 14,
  },
  {
    id: 'audit',
    label: 'Audit Log',
    path: '/audit',
    iconName: 'ShieldAlert',
    requiredPermission: 'view_audit',
    phase: 14,
  },
  {
    id: 'settings',
    label: 'Settings',
    path: '/settings',
    iconName: 'SlidersHorizontal',
    phase: 1,
  },
];
