import { get, put, del, apiClient } from './client';
import type { MediaAsset, UpdateMediaAssetRequest, PagedResult, MediaListParams } from '@/types';

export const mediaApi = {
  list: (params?: MediaListParams): Promise<PagedResult<MediaAsset>> =>
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
    if (options?.folderId) form.append('folderId', options.folderId);

    return apiClient
      .post<MediaAsset>('/media', form, {
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
    put<MediaAsset>(`/media/${id}`, data),

  delete: (id: string): Promise<void> =>
    del(`/media/${id}`),
};
