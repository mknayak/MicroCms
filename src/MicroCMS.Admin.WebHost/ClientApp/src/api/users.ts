import { get, post, put, del } from './client';
import type { User, InviteUserRequest, UpdateUserRolesRequest, PagedResult, PaginationParams } from '@/types';

export const usersApi = {
  list: (params?: PaginationParams): Promise<PagedResult<User>> =>
    get<PagedResult<User>>('/admin/users', { params }),

  getById: (id: string): Promise<User> =>
    get<User>(`/admin/users/${id}`),

  invite: (data: InviteUserRequest): Promise<User> =>
    post<User>('/admin/users/invite', data),

  updateRoles: (id: string, data: UpdateUserRolesRequest): Promise<void> =>
    put<void>(`/admin/users/${id}/roles`, data),

  deactivate: (id: string): Promise<void> =>
    post<void>(`/admin/users/${id}/deactivate`),

  activate: (id: string): Promise<void> =>
    post<void>(`/admin/users/${id}/activate`),

  delete: (id: string): Promise<void> =>
    del(`/admin/users/${id}`),
};
