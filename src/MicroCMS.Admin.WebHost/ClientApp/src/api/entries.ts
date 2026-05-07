import { get, post, put, del, apiClient } from './client';
import type {
  Entry,
  EntryListItem,
  CreateEntryRequest,
  UpdateEntryRequest,
  EntryVersion,
  PagedResult,
  EntryListParams,
} from '@/types';

export interface ImportEntriesResult {
  imported: number;
  skipped: number;
  errors: string[];
}

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

  /**
   * Downloads all entries as a ZIP file (containing entries.json).
   * Uses axios so the Bearer token is included, then triggers a browser save dialog.
   */
  exportZip: async (params?: { contentTypeId?: string }) => {
    const queryParams: Record<string, string> = { format: 'Json' };
    if (params?.contentTypeId) queryParams['contentTypeId'] = params.contentTypeId;

    const response = await apiClient.get<Blob>('/entries/export', {
      params: queryParams,
      responseType: 'blob',
    });

    const url = URL.createObjectURL(response.data);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'entries.zip';
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
  },

  /** Upload a ZIP file (containing entries.json) to import entries. */
  importZip: async (file: File): Promise<ImportEntriesResult> => {
    const formData = new FormData();
    formData.append('file', file);
    const response = await apiClient.post<ImportEntriesResult>('/entries/import', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
    return response.data;
  },
};


