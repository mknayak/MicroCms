import { get } from './client';
import type { DashboardStats, ActivityItem, PagedResult } from '@/types';

export const dashboardApi = {
  getStats: (): Promise<DashboardStats> =>
    get<DashboardStats>('/admin/dashboard/stats'),

  getActivity: (params?: { pageSize?: number }): Promise<PagedResult<ActivityItem>> =>
    get<PagedResult<ActivityItem>>('/admin/dashboard/activity', { params }),
};
