import { get, put } from './client';
import type { Tenant, UpdateTenantRequest } from '@/types';

export const tenantsApi = {
  getCurrent: (): Promise<Tenant> =>
    get<Tenant>('/admin/tenants/current'),

  update: (id: string, data: UpdateTenantRequest): Promise<Tenant> =>
    put<Tenant>(`/admin/tenants/${id}/settings`, data),
};
