import { get, put } from './client';
import type { Tenant, UpdateTenantRequest } from '@/types';

export const tenantsApi = {
  getCurrent: (): Promise<Tenant> =>
    get<Tenant>('/admin/tenants/current'),

  update: (data: UpdateTenantRequest): Promise<Tenant> =>
    put<Tenant>('/admin/tenants/current', data),

  uploadLogo: (file: File): Promise<Tenant> => {
    const form = new FormData();
    form.append('logo', file);
    return import('./client').then(({ apiClient }) =>
      apiClient
        .post<Tenant>('/admin/tenants/current/logo', form, {
          headers: { 'Content-Type': 'multipart/form-data' },
        })
        .then((r) => r.data),
    );
  },
};
