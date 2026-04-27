import { get, post, put, del } from './client';
import type {
  ContentType,
  ContentTypeListItem,
  PagedResult,
  PaginationParams,
} from '@/types';

export interface CreateContentTypeRequest {
  siteId: string;
  handle: string;
  displayName: string;
  description?: string;
  localizationMode?: string;
}

export interface UpdateFieldRequest {
  id?: string;
  handle: string;
  label: string;
  fieldType: string;
  isRequired?: boolean;
  isLocalized?: boolean;
  isUnique?: boolean;
  isIndexed?: boolean;
  sortOrder?: number;
  description?: string;
}

export interface UpdateContentTypeRequest {
  displayName: string;
  description?: string;
  localizationMode?: string;
  fields?: UpdateFieldRequest[];
}

export interface ImportSchemaRequest {
  siteId: string;
  handle: string;
  displayName: string;
  description?: string;
  fields?: Array<{
    handle: string;
    label: string;
    fieldType: string;
    isRequired?: boolean;
    isLocalized?: boolean;
  }>;
}

export const contentTypesApi = {
  list: (params?: PaginationParams & { siteId?: string }): Promise<PagedResult<ContentTypeListItem>> =>
    get<PagedResult<ContentTypeListItem>>('/content-types', { params }),

  getById: (id: string): Promise<ContentType> =>
    get<ContentType>(`/content-types/${id}`),

  create: (data: CreateContentTypeRequest): Promise<ContentType> =>
    post<ContentType>('/content-types', data),

  update: (id: string, data: UpdateContentTypeRequest): Promise<ContentType> =>
    put<ContentType>(`/content-types/${id}`, data),

  importSchema: (data: ImportSchemaRequest): Promise<ContentType> =>
    post<ContentType>('/content-types/import', data),

  delete: (id: string): Promise<void> =>
    del(`/content-types/${id}`),
};
