import { get, post, put, del } from './client';
import type {
  ComponentDto,
ComponentListItem,
  ComponentItemDto,
  CreateComponentRequest,
  UpdateComponentRequest,
  CreateComponentItemRequest,
  UpdateComponentItemRequest,
  PagedResult,
  ComponentListParams,
  ComponentItemListParams,
} from '@/types';

export const componentsApi = {
  // ── Component definitions ────────────────────────────────────────────────

  list: (params?: ComponentListParams): Promise<PagedResult<ComponentListItem>> =>
    get<PagedResult<ComponentListItem>>('/components', { params }),

  getById: (id: string): Promise<ComponentDto> =>
    get<ComponentDto>(`/components/${id}`),

  create: (data: CreateComponentRequest): Promise<ComponentDto> =>
    post<ComponentDto>('/components', data),

  update: (id: string, data: UpdateComponentRequest): Promise<ComponentDto> =>
    put<ComponentDto>(`/components/${id}`, data),

  delete: (id: string): Promise<void> =>
    del(`/components/${id}`),

  // ── Component items (instances) ──────────────────────────────────────────

  listItems: (
    componentId: string,
    params?: ComponentItemListParams,
  ): Promise<PagedResult<ComponentItemDto>> =>
    get<PagedResult<ComponentItemDto>>(`/components/${componentId}/items`, { params }),

  getItem: (componentId: string, itemId: string): Promise<ComponentItemDto> =>
    get<ComponentItemDto>(`/components/${componentId}/items/${itemId}`),

  createItem: (
    componentId: string,
    data: CreateComponentItemRequest,
  ): Promise<ComponentItemDto> =>
    post<ComponentItemDto>(`/components/${componentId}/items`, data),

  updateItem: (
    componentId: string,
    itemId: string,
  data: UpdateComponentItemRequest,
  ): Promise<ComponentItemDto> =>
    put<ComponentItemDto>(`/components/${componentId}/items/${itemId}`, data),

  publishItem: (componentId: string, itemId: string): Promise<void> =>
    post<void>(`/components/${componentId}/items/${itemId}/publish`, {}),

  archiveItem: (componentId: string, itemId: string): Promise<void> =>
    post<void>(`/components/${componentId}/items/${itemId}/archive`, {}),

  deleteItem: (componentId: string, itemId: string): Promise<void> =>
    del(`/components/${componentId}/items/${itemId}`),
};
