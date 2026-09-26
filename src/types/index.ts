export * from './system';
export * from './governance';
export * from './investigations';
export * from './evidence';
export * from './entities';
export * from './graph';
export * from './auth';

export interface AuditEvent {
  id: string;
  actorId: string;
  actorName: string;
  action:
    | 'Login'
    | 'Logout'
    | 'Evidence Upload'
    | 'Evidence View'
    | 'Evidence Download'
    | 'Entity Review'
    | 'Resolution Decision'
    | 'Finding Review'
    | 'Graph Query'
    | 'Report Generation'
    | 'Permission Change'
    | 'Investigation Update';
  result: 'Success' | 'Denied' | 'Failed';
  investigationId?: string;
  resourceType: string;
  resourceId?: string;
  timestampUtc: string;
  requestId: string;
  ipAddress?: string;
}

export interface AppNotification {
  id: string;
  title: string;
  message: string;
  type: 'info' | 'warning' | 'error' | 'success';
  timestampUtc: string;
  read: boolean;
  investigationId?: string;
  actionUrl?: string;
}
