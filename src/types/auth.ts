export type UserRole =
  | 'Administrator'
  | 'Investigator'
  | 'Analyst'
  | 'Reviewer'
  | 'Supervisor'
  | 'Auditor';

export type UserPermission =
  | 'view_investigation'
  | 'view_evidence'
  | 'upload_evidence'
  | 'review_entities'
  | 'review_findings'
  | 'run_analytics'
  | 'generate_reports'
  | 'export_data'
  | 'manage_members';

export interface AuthUser {
  id: string;
  name: string;
  email: string;
  badgeNumber?: string;
  role: UserRole;
  permissions: UserPermission[];
  agency?: string;
  sessionExpiresAtUtc: string;
}

export interface AuthSessionState {
  user: AuthUser | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  token: string | null;
}
