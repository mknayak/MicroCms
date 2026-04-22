import { get, post, put, del } from './client';
import type {
  ContentType,
  CreateContentTypeRequest,
  UpdateContentTypeRequest,
  PagedResult,
  PaginationParams,
} from '@/types';

export const contentTypesApi = {
  list: (params?: PaginationParams): Promise<PagedResult<ContentType>> =>
    get<PagedResult<ContentType>>('/content-types', { params }),

  getById: (id: string): Promise<ContentType> =>
    get<ContentType>(`/content-types/${id}`),

  create: (data: CreateContentTypeRequest): Promise<ContentType> =>
    post<ContentType>('/content-types', data),

  update: (id: string, data: UpdateContentTypeRequest): Promise<ContentType> =>
    put<ContentType>(`/content-types/${id}`, data),

  delete: (id: string): Promise<void> =>
    del(`/content-types/${id}`),
};
