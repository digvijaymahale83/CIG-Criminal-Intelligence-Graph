import { CreateInvestigationInput, Investigation } from '../../types/investigations';
import { ApiError } from '../api/errors';

export class InvestigationService {
  /**
   * Phase 3 Integration-Ready Service
   * Throws typed ApiError indicating backend capability is not connected yet.
   */
  public static async getInvestigations(): Promise<Investigation[]> {
    throw new ApiError(501, 'Backend capability not connected. Investigations module is introduced in Phase 3.');
  }

  public static async getInvestigationById(investigationId: string): Promise<Investigation> {
    throw new ApiError(
      501,
      `Backend capability not connected. Case retrieval (${investigationId}) is introduced in Phase 3.`
    );
  }

  public static async createInvestigation(input: CreateInvestigationInput): Promise<Investigation> {
    throw new ApiError(
      501,
      `Backend capability not connected. Case creation for ${input.caseNumber} is scheduled for Phase 3.`
    );
  }
}
