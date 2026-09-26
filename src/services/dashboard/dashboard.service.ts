import { apiClient } from '../api/client';
import { DashboardDto } from '../../types/dashboard';

export const dashboardService = {
  async getDashboard(caseId: string): Promise<DashboardDto> {
    return await apiClient.get<DashboardDto>(`/cases/${caseId}/dashboard`);
  }
};
