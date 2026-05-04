import { get, put, del, post } from './client';
import type {
  SiteDetail,
  SiteSettingsDto,
  UpdateSiteRequest,
  UpdateSiteSettingsRequest,
} from '@/types';

export interface ConfigEntryDto {
  key: string;
  value: string;
  category?: string;
  isSecret: boolean;
}

export interface SiteConfigEntriesDto {
  entries: ConfigEntryDto[];
}

export interface UpsertConfigEntryRequest {
  value: string;
  category?: string;
  isSecret?: boolean;
}

export interface ExportConfigResponse {
  siteId: string;
  category?: string;
  exportedAt: string;
  entries: ImportConfigEntryItem[];
}

export interface ImportConfigEntryItem {
  key: string;
  value: string;
  category: string;
  isSecret: boolean;
}

export interface ImportConfigRequest {
  entries: ImportConfigEntryItem[];
}

export const sitesApi = {
  getById: (id: string): Promise<SiteDetail> =>
    get<SiteDetail>(`/sites/${id}`),

  update: (id: string, data: UpdateSiteRequest): Promise<SiteDetail> =>
    put<SiteDetail>(`/sites/${id}`, data),

  getSettings: (id: string): Promise<SiteSettingsDto> =>
    get<SiteSettingsDto>(`/sites/${id}/settings`),

  updateSettings: (id: string, data: UpdateSiteSettingsRequest): Promise<SiteSettingsDto> =>
    put<SiteSettingsDto>(`/sites/${id}/settings`, data),

  getConfig: (id: string, category?: string): Promise<SiteConfigEntriesDto> =>
    get<SiteConfigEntriesDto>(`/sites/${id}/config${category ? `?category=${encodeURIComponent(category)}` : ''}`),

  upsertConfigEntry: (id: string, key: string, data: UpsertConfigEntryRequest): Promise<ConfigEntryDto> =>
    put<ConfigEntryDto>(`/sites/${id}/config/${encodeURIComponent(key)}`, data),

  deleteConfigEntry: (id: string, key: string): Promise<void> =>
    del<void>(`/sites/${id}/config/${encodeURIComponent(key)}`),

  exportConfig: (id: string, category?: string): Promise<ExportConfigResponse> =>
    get<ExportConfigResponse>(`/sites/${id}/config/bulk-export${category ? `?category=${encodeURIComponent(category)}` : ''}`),

  importConfig: (id: string, data: ImportConfigRequest): Promise<SiteConfigEntriesDto> =>
    post<SiteConfigEntriesDto>(`/sites/${id}/config/bulk-import`, data),
};
