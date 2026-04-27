import { get, post, put, del } from './client';
import type {
  PageTreeNode,
  PageDto,
  CreateStaticPageRequest,
  CreateCollectionPageRequest,
  MovePageRequest,
  SetPageLayoutRequest,
  SetPageSeoRequest,
  SetPageLinkedEntryRequest,
  PageTemplateDto,
  SavePageTemplateRequest,
} from '@/types';

export const pagesApi = {
  getTree: (siteId: string): Promise<PageTreeNode[]> =>
    get<PageTreeNode[]>(`/pages`, { params: { siteId } }),

  getPage: (id: string): Promise<PageDto> =>
 get<PageDto>(`/pages/${id}`),

  createStatic: (data: CreateStaticPageRequest): Promise<PageDto> =>
    post<PageDto>('/pages/static', data),

  createCollection: (data: CreateCollectionPageRequest): Promise<PageDto> =>
    post<PageDto>('/pages/collection', data),

  move: (id: string, data: MovePageRequest): Promise<PageDto> =>
    put<PageDto>(`/pages/${id}/move`, data),

  delete: (id: string): Promise<void> =>
    del(`/pages/${id}`),

  setLayout: (id: string, data: SetPageLayoutRequest): Promise<PageDto> =>
    put<PageDto>(`/pages/${id}/layout`, data),

  setLinkedEntry: (id: string, data: SetPageLinkedEntryRequest): Promise<PageDto> =>
    put<PageDto>(`/pages/${id}/entry`, data),

  getTemplate: (id: string): Promise<PageTemplateDto> =>
    get<PageTemplateDto>(`/pages/${id}/template`),

  saveTemplate: (id: string, data: SavePageTemplateRequest): Promise<PageTemplateDto> =>
  put<PageTemplateDto>(`/pages/${id}/template`, data),

  setSeo: (id: string, data: SetPageSeoRequest): Promise<PageDto> =>
 put<PageDto>(`/pages/${id}/seo`, data),
};
