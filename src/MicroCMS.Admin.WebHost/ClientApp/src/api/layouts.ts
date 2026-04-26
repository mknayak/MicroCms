import { get, post, put, del } from './client';
import type {
  LayoutDto,
  LayoutListItem,
  CreateLayoutRequest,
  UpdateLayoutRequest,
} from '@/types';

export const layoutsApi = {
  list: (siteId: string): Promise<LayoutListItem[]> =>
  get<LayoutListItem[]>('/layouts', { params: { siteId } }),

  get: (id: string): Promise<LayoutDto> =>
    get<LayoutDto>(`/layouts/${id}`),

  create: (data: CreateLayoutRequest): Promise<LayoutDto> =>
    post<LayoutDto>('/layouts', data),

  update: (id: string, data: UpdateLayoutRequest): Promise<LayoutDto> =>
    put<LayoutDto>(`/layouts/${id}`, data),

  setDefault: (id: string, siteId: string): Promise<LayoutDto> =>
    post<LayoutDto>(`/layouts/${id}/set-default`, null, { params: { siteId } }),

  delete: (id: string): Promise<void> =>
    del(`/layouts/${id}`),
};
