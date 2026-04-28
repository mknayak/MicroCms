import { get, post, put, del } from './client';
import type {
  Entry,
  EntryListItem,
  CreateEntryRequest,
  UpdateEntryRequest,
  EntryVersion,
  PagedResult,
  EntryListParams,
} from '@/types';

export const entriesApi = {
  list: (params?: EntryListParams): Promise<PagedResult<EntryListItem>> =>
    get<PagedResult<EntryListItem>>('/entries', { params }),

  getById: (id: string): Promise<Entry> =>
    get<Entry>(`/entries/${id}`),

  create: (data: CreateEntryRequest): Promise<Entry> =>
    post<Entry>('/entries', data),

  update: (id: string, data: UpdateEntryRequest): Promise<Entry> =>
    put<Entry>(`/entries/${id}`, data),

  publish: (id: string): Promise<Entry> =>
    post<Entry>(`/entries/${id}/publish`),

  unpublish: (id: string): Promise<Entry> =>
    post<Entry>(`/entries/${id}/unpublish`),

  submitForReview: (id: string): Promise<Entry> =>
    post<Entry>(`/entries/${id}/submit`),

  approve: (id: string): Promise<Entry> =>
    post<Entry>(`/entries/${id}/approve`),

  reject: (id: string, reason: string): Promise<Entry> =>
    post<Entry>(`/entries/${id}/reject`, { reason }),

  schedule: (id: string, publishAt: string, unpublishAt?: string): Promise<Entry> =>
    post<Entry>(`/entries/${id}/schedule`, { publishAt, unpublishAt }),

  cancelSchedule: (id: string): Promise<void> =>
    del(`/entries/${id}/schedule`),

  delete: (id: string): Promise<void> =>
    del(`/entries/${id}`),

  getVersions: (id: string): Promise<EntryVersion[]> =>
    get<EntryVersion[]>(`/entries/${id}/versions`),

  /** Restore a specific version by its GUID. */
  restoreVersion: (id: string, versionId: string): Promise<Entry> =>
    post<Entry>(`/entries/${id}/versions/${versionId}/restore`),

  getPreviewToken: (id: string): Promise<{ token: string; expiresAt: string }> =>
    get(`/entries/${id}/preview-token`),
};
