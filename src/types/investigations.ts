import { InvestigationPriorityLevel } from './governance';

export type InvestigationStatus =
  | 'Draft'
  | 'Active'
  | 'Under Review'
  | 'Suspended'
  | 'Closed'
  | 'Archived';

export interface InvestigationMember {
  id: string;
  name: string;
  email: string;
  role: 'Administrator' | 'Investigator' | 'Analyst' | 'Reviewer' | 'Supervisor' | 'Auditor';
  permissions: string[];
  addedAtUtc: string;
  lastActiveUtc: string;
}

export interface InvestigationNote {
  id: string;
  investigationId: string;
  authorId: string;
  authorName: string;
  title: string;
  content: string;
  createdAtUtc: string;
  updatedAtUtc?: string;
  evidenceReferences?: string[];
}

export interface Investigation {
  id: string;
  caseNumber: string;
  title: string;
  description: string;
  classification: string;
  priority: InvestigationPriorityLevel;
  status: InvestigationStatus;
  jurisdiction: string;
  ownerId: string;
  ownerName: string;
  createdAtUtc: string;
  updatedAtUtc: string;
  startDateUtc: string;
  evidenceCount: number;
  findingCount: number;
  entityCount: number;
  members: InvestigationMember[];
}

export interface CreateInvestigationInput {
  caseNumber: string;
  title: string;
  description: string;
  classification: string;
  priority: InvestigationPriorityLevel;
  jurisdiction: string;
  startDate: string;
  notes?: string;
}
