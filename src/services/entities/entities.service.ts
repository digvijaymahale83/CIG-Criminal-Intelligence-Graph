import { Entity, EntityResolutionCandidate } from '../../types/entities';
import { ApiError } from '../api/errors';

export class EntityService {
  public static async getEntities(investigationId: string): Promise<Entity[]> {
    throw new ApiError(501, `Backend capability not connected. Entity extraction for case ${investigationId} is scheduled for Phase 7.`);
  }

  public static async getEntityById(investigationId: string, entityId: string): Promise<Entity> {
    throw new ApiError(501, `Backend capability not connected. Entity profile for ${entityId} is scheduled for Phase 7.`);
  }

  public static async getResolutionCandidates(investigationId: string): Promise<EntityResolutionCandidate[]> {
    throw new ApiError(501, `Backend capability not connected. Entity resolution candidate review is scheduled for Phase 7.`);
  }
}
