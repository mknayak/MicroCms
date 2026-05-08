import { get, post, put, del } from './client';
import type {
  LayoutDto,
  LayoutListItem,
  CreateLayoutRequest,
  UpdateLayoutRequest,
  UpdateLayoutZonesRequest,
  UpdateLayoutDefaultPlacementsRequest,
  UpdateLayoutConfigRequest,
  UpdateLayoutShellRequest,
  EditLock,
  AcquireLockRequest,
} from '@/types';

export const layoutsApi = {
  list: (): Promise<LayoutListItem[]> =>
    get<LayoutListItem[]>('/layouts'),

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

  updateConfig: (id: string, data: UpdateLayoutConfigRequest): Promise<LayoutDto> =>
    put<LayoutDto>(`/layouts/${id}/config`, data),

  updateShell: (id: string, data: UpdateLayoutShellRequest): Promise<LayoutDto> =>
    put<LayoutDto>(`/layouts/${id}/shell`, data),

  setDefault: (id: string): Promise<LayoutDto> =>
    post<LayoutDto>(`/layouts/${id}/set-default`, null),

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
