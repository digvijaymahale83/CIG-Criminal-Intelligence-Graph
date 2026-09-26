import { z } from 'zod';

export const createInvestigationSchema = z.object({
  caseNumber: z
    .string()
    .min(3, 'Case number must be at least 3 characters')
    .max(50, 'Case number must not exceed 50 characters')
    .regex(/^[A-Za-z0-9\-_/]+$/, 'Case number must contain only alphanumeric characters, dashes, slashes, or underscores'),
  title: z.string().min(5, 'Title must be at least 5 characters').max(200, 'Title cannot exceed 200 characters'),
  description: z.string().min(10, 'Description must be at least 10 characters').max(2000, 'Description cannot exceed 2000 characters'),
  classification: z.string().min(2, 'Classification is required'),
  priority: z.enum(['Low', 'Medium', 'High', 'Critical']),
  jurisdiction: z.string().min(2, 'Jurisdiction is required'),
  startDate: z.string().min(10, 'Start date is required'),
  notes: z.string().max(2000).optional(),
});

export const loginSchema = z.object({
  email: z.string().email('Please enter a valid official email address'),
  password: z.string().min(8, 'Password must be at least 8 characters'),
  rememberMe: z.boolean().optional(),
});
