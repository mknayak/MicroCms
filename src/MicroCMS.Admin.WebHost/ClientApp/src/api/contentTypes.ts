import { get, post, put, del } from './client';
import type {
  ContentType,
  ContentTypeListItem,
  EnumOptionDto,
  MultiListOptionDto,
  EntryGroupListItem,
  EntryGroupDto,
  PagedResult,
  PaginationParams,
} from '@/types';

export interface FieldDynamicSourceRequest {
  contentTypeHandle: string;
  labelField?: string;
  valueField?: string;
  statusFilter?: string;
  groupHandle?: string;
}

export interface CreateContentTypeRequest {
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
  /** Dynamic source for MultiList fields. */
  multiListSource?: FieldDynamicSourceRequest;
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
  list: (params?: PaginationParams): Promise<PagedResult<ContentTypeListItem>> =>
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
  getEnumOptions: (contentTypeId: string, fieldId: string): Promise<EnumOptionDto[]> =>
    get<EnumOptionDto[]>(`/content-types/${contentTypeId}/fields/${fieldId}/enum-options`),

  /** Returns the candidate entry list (left pane) for a MultiList field picker. */
  getMultiListOptions: (contentTypeId: string, fieldId: string): Promise<MultiListOptionDto[]> =>
    get<MultiListOptionDto[]>(`/content-types/${contentTypeId}/fields/${fieldId}/multilist-options`),
};

// ─── Entry Groups API ─────────────────────────────────────────────────────────

export interface CreateEntryGroupRequest {
  handle: string;
  title: string;
  description?: string;
  memberEntryIds: string[];
}

export interface UpdateEntryGroupRequest {
  title: string;
  description?: string;
  memberEntryIds: string[];
}

export const entryGroupsApi = {
  list: (contentTypeId: string): Promise<EntryGroupListItem[]> =>
    get<EntryGroupListItem[]>(`/content-types/${contentTypeId}/groups`),

  getById: (contentTypeId: string, groupId: string): Promise<EntryGroupDto> =>
    get<EntryGroupDto>(`/content-types/${contentTypeId}/groups/${groupId}`),

  create: (contentTypeId: string, data: CreateEntryGroupRequest): Promise<EntryGroupDto> =>
    post<EntryGroupDto>(`/content-types/${contentTypeId}/groups`, data),

  update: (contentTypeId: string, groupId: string, data: UpdateEntryGroupRequest): Promise<EntryGroupDto> =>
    put<EntryGroupDto>(`/content-types/${contentTypeId}/groups/${groupId}`, data),

  delete: (contentTypeId: string, groupId: string): Promise<void> =>
    del(`/content-types/${contentTypeId}/groups/${groupId}`),

  getMultiListOptions: (contentTypeId: string, fieldId: string): Promise<MultiListOptionDto[]> =>
    get<MultiListOptionDto[]>(`/content-types/${contentTypeId}/fields/${fieldId}/multilist-options`),
};
