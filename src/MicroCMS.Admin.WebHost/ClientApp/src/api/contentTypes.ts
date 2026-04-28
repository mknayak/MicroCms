import { get, post, put, del } from './client';
import type {
  ContentType,
  ContentTypeListItem,
  EnumOptionDto,
  PagedResult,
  PaginationParams,
} from '@/types';

export interface FieldDynamicSourceRequest {
  contentTypeHandle: string;
  labelField?: string;
  valueField?: string;
  statusFilter?: string;
}

export interface CreateContentTypeRequest {
  siteId: string;
  handle: string;
  displayName: string;
  description?: string;
  localizationMode?: string;
  kind?: string;
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
  isList?: boolean;
  sortOrder?: number;
  description?: string;
  /** Static options for Enum fields. */
  options?: string[];
  /** Dynamic source for Enum fields — mutually exclusive with options. */
  dynamicSource?: FieldDynamicSourceRequest;
}

export interface UpdateContentTypeRequest {
  displayName: string;
  description?: string;
  localizationMode?: string;
  kind?: string;
  layoutId?: string;
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

  /**
   * Resolves the effective option list for an Enum field.
   * For static fields returns stored options.
   * For dynamic fields queries published entries of the source content type.
   */
  getEnumOptions: (contentTypeId: string, fieldId: string, siteId: string): Promise<EnumOptionDto[]> =>
    get<EnumOptionDto[]>(`/content-types/${contentTypeId}/fields/${fieldId}/enum-options`, { params: { siteId } }),
};
