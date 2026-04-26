import { get, post } from './client';
import type { ApiClientDto, ApiClientCreatedDto, CreateApiClientRequest } from '@/types';

export const apiClientsApi = {
  list: (siteId: string): Promise<ApiClientDto[]> =>
    get<ApiClientDto[]>('/api-clients', { params: { siteId } }),

  create: (data: CreateApiClientRequest): Promise<ApiClientCreatedDto> =>
    post<ApiClientCreatedDto>('/api-clients', data),

  revoke: (id: string): Promise<ApiClientDto> =>
    post<ApiClientDto>(`/api-clients/${id}/revoke`, {}),

  regenerate: (id: string): Promise<ApiClientCreatedDto> =>
    post<ApiClientCreatedDto>(`/api-clients/${id}/regenerate`, {}),
};
