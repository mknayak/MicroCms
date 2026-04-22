import { get, post, put, del } from './client';
import type {
  Entry,
  EntryListItem,
  CreateEntryRequest,
  UpdateEntryRequest,
  PublishEntryRequest,
  EntryVersion,
  PagedResult,
  EntryListParams,
} from '@/types';

export const entriesApi = {
  list: (params?: EntryListParams): Promise<PagedResult<EntryListItem>> =>
    get<PagedResult<EntryListItem>>('/entries', { params }),

  getById: (id: string, locale?: string): Promise<Entry> =>
    get<Entry>(`/entries/${id}`, { params: { locale } }),

  create: (data: CreateEntryRequest): Promise<Entry> =>
    post<Entry>('/entries', data),

  update: (id: string, data: UpdateEntryRequest): Promise<Entry> =>
    put<Entry>(`/entries/${id}`, data),

  publish: (id: string, data?: PublishEntryRequest): Promise<void> =>
    post<void>(`/entries/${id}/publish`, data),

  unpublish: (id: string): Promise<void> =>
    post<void>(`/entries/${id}/unpublish`),

  submitForReview: (id: string): Promise<void> =>
    post<void>(`/entries/${id}/review`),

  archive: (id: string): Promise<void> =>
    post<void>(`/entries/${id}/archive`),

  delete: (id: string): Promise<void> =>
    del(`/entries/${id}`),

  getVersions: (id: string): Promise<EntryVersion[]> =>
    get<EntryVersion[]>(`/entries/${id}/versions`),

  restoreVersion: (id: string, versionId: string): Promise<void> =>
    post<void>(`/entries/${id}/versions/${versionId}/restore`),
};
