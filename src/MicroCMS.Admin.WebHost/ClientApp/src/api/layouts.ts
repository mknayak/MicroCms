import { get, post, put, del } from './client';
import type {
  LayoutDto,
  LayoutListItem,
  CreateLayoutRequest,
  UpdateLayoutRequest,
  UpdateLayoutZonesRequest,
  UpdateLayoutDefaultPlacementsRequest,
  EditLock,
  AcquireLockRequest,
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

  updateZones: (id: string, data: UpdateLayoutZonesRequest): Promise<LayoutDto> =>
    put<LayoutDto>(`/layouts/${id}/zones`, data),

  updateDefaultPlacements: (id: string, data: UpdateLayoutDefaultPlacementsRequest): Promise<LayoutDto> =>
    put<LayoutDto>(`/layouts/${id}/default-placements`, data),

  setDefault: (id: string, siteId: string): Promise<LayoutDto> =>
    post<LayoutDto>(`/layouts/${id}/set-default`, null, { params: { siteId } }),

  delete: (id: string): Promise<void> =>
    del(`/layouts/${id}`),
};

export const locksApi = {
  acquire: (data: AcquireLockRequest): Promise<EditLock> =>
    post<EditLock>('/locks/acquire', data),

  release: (entityId: string): Promise<void> =>
    del(`/locks/${entityId}`),

  refresh: (entityId: string): Promise<EditLock> =>
    post<EditLock>(`/locks/${entityId}/refresh`, null),

  get: (entityId: string): Promise<EditLock | null> =>
    get<EditLock | null>(`/locks/${entityId}`),
};
