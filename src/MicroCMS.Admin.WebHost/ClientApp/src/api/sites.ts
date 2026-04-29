import { get, put } from './client';
import type {
  SiteDetail,
  SiteSettingsDto,
  UpdateSiteRequest,
  UpdateSiteSettingsRequest,
} from '@/types';

export const sitesApi = {
  getById: (id: string): Promise<SiteDetail> =>
    get<SiteDetail>(`/sites/${id}`),

  update: (id: string, data: UpdateSiteRequest): Promise<SiteDetail> =>
    put<SiteDetail>(`/sites/${id}`, data),

  getSettings: (id: string): Promise<SiteSettingsDto> =>
    get<SiteSettingsDto>(`/sites/${id}/settings`),

  updateSettings: (id: string, data: UpdateSiteSettingsRequest): Promise<SiteSettingsDto> =>
    put<SiteSettingsDto>(`/sites/${id}/settings`, data),
};
