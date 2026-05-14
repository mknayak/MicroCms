import { get, post, put, patch, del, apiClient } from './client';
import type {
  MediaAsset,
  MediaFolder,
  UpdateMediaAssetRequest,
  PagedResult,
} from '@/types';

export interface SignedUrlResponse {
  assetId: string;
  url: string;
  expiresAt: string;
}

export interface MediaListApiParams {
  page?: number;
  pageSize?: number;
  folderId?: string;
  search?: string;
}

export const mediaApi = {
  list: (params?: MediaListApiParams): Promise<PagedResult<MediaAsset>> =>
    get<PagedResult<MediaAsset>>('/media', { params }),

  getById: (id: string): Promise<MediaAsset> =>
    get<MediaAsset>(`/media/${id}`),

  upload: (
    file: File,
    options?: { altText?: string; tags?: string[]; folderId?: string },
    onProgress?: (pct: number) => void,
  ): Promise<MediaAsset> => {
    const form = new FormData();
    form.append('file', file);
    if (options?.altText) form.append('altText', options.altText);
    if (options?.tags) form.append('tags', JSON.stringify(options.tags));

    const url = options?.folderId
      ? `/media/upload?folderId=${encodeURIComponent(options.folderId)}`
      : '/media/upload';

    return apiClient
      .post<MediaAsset>(url, form, {
        headers: { 'Content-Type': 'multipart/form-data' },
        onUploadProgress: (e) => {
          if (onProgress && e.total) {
            onProgress(Math.round((e.loaded * 100) / e.total));
          }
        },
      })
      .then((r) => r.data);
  },

  update: (id: string, data: UpdateMediaAssetRequest): Promise<MediaAsset> =>
    put<MediaAsset>(`/media/${id}/metadata`, data),

  setAssetPath: (id: string, assetPath: string | null): Promise<MediaAsset> =>
    patch<MediaAsset>(`/media/${id}/path`, { assetPath }),

  delete: (id: string): Promise<void> =>
    del(`/media/${id}`),

  getSignedUrl: (id: string, expiryMinutes = 60): Promise<SignedUrlResponse> =>
    get<SignedUrlResponse>(`/media/${id}/signed-url`, { params: { expiryMinutes } }),

  bulkDelete: (ids: string[]): Promise<void> =>
    post<void>('/media/bulk/delete', { assetIds: ids }),

  bulkMove: (ids: string[], targetFolderId: string | null): Promise<void> =>
    post<void>('/media/bulk/move', { assetIds: ids, targetFolderId }),

  bulkRetag: (ids: string[], tags: string[]): Promise<void> =>
    post<void>('/media/bulk/retag', { assetIds: ids, tags }),

  // ── Folder CRUD ────────────────────────────────────────────────────────

  listFolders: (parentFolderId?: string): Promise<MediaFolder[]> =>
    get<MediaFolder[]>('/media/folders', { params: { parentFolderId } }),

  createFolder: (name: string, parentFolderId?: string): Promise<MediaFolder> =>
    post<MediaFolder>('/media/folders', { name, parentFolderId }),

  renameFolder: (id: string, newName: string): Promise<MediaFolder> =>
    patch<MediaFolder>(`/media/folders/${id}/rename`, { newName }),

  moveFolder: (id: string, newParentFolderId: string | null): Promise<MediaFolder> =>
    patch<MediaFolder>(`/media/folders/${id}/move`, { newParentFolderId }),

  deleteFolder: (id: string): Promise<void> =>
    del(`/media/folders/${id}`),
};
