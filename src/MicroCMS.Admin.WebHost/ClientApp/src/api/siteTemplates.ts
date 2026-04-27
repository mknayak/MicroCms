import { get, post, put, del } from './client';
import type { SiteTemplateDto, SiteTemplateListItem, CreateSiteTemplateRequest, UpdateSiteTemplateRequest, SaveSiteTemplateRequest } from '@/types';

export const siteTemplatesApi = {
  list: (siteId: string): Promise<SiteTemplateListItem[]> =>
    get<SiteTemplateListItem[]>('/site-templates', { params: { siteId } }),

  get: (id: string): Promise<SiteTemplateDto> =>
 get<SiteTemplateDto>(`/site-templates/${id}`),

  create: (data: CreateSiteTemplateRequest): Promise<SiteTemplateDto> =>
 post<SiteTemplateDto>('/site-templates', data),

  update: (id: string, data: UpdateSiteTemplateRequest): Promise<SiteTemplateDto> =>
  put<SiteTemplateDto>(`/site-templates/${id}`, data),

  savePlacements: (id: string, data: SaveSiteTemplateRequest): Promise<SiteTemplateDto> =>
    put<SiteTemplateDto>(`/site-templates/${id}/placements`, {
      placementsJson: JSON.stringify(data.placements),
    }),

  delete: (id: string): Promise<void> =>
    del(`/site-templates/${id}`),
};
