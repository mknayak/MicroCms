import { get, post, put } from './client';
import type {
  TenantListItem,
  TenantDetail,
  OnboardTenantRequest,
  TenantOnboardingResult,
  UpdateTenantSettingsRequest,
  Site,
  CreateSiteRequest,
  PagedResult,
  PaginationParams,
} from '@/types';

export const adminTenantsApi = {
  list: (params?: PaginationParams): Promise<PagedResult<TenantListItem>> =>
  get<PagedResult<TenantListItem>>('/admin/tenants', { params }),

  getById: (id: string): Promise<TenantDetail> =>
    get<TenantDetail>(`/admin/tenants/${id}`),

  onboard: (data: OnboardTenantRequest): Promise<TenantOnboardingResult> =>
    post<TenantOnboardingResult>('/admin/tenants/onboard', data),

  updateSettings: (id: string, data: UpdateTenantSettingsRequest): Promise<TenantDetail> =>
    put<TenantDetail>(`/admin/tenants/${id}/settings`, data),

  addSite: (tenantId: string, data: CreateSiteRequest): Promise<Site> =>
  post<Site>(`/admin/tenants/${tenantId}/sites`, data),
};
