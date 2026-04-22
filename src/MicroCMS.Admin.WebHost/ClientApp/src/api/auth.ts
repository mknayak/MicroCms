import { post } from './client';
import type { AuthTokenResponse, ChangePasswordRequest } from '@/types';

export const authApi = {
  /** POST /api/v1/auth/login */
  login: (email: string, password: string): Promise<AuthTokenResponse> =>
    post<AuthTokenResponse>('/auth/login', { email, password }),

  /** POST /api/v1/auth/refresh — rotates the refresh token */
  refresh: (refreshToken: string): Promise<AuthTokenResponse> =>
    post<AuthTokenResponse>('/auth/refresh', { refreshToken }),

  /** POST /api/v1/auth/logout — revokes single device session */
  logout: (refreshToken: string): Promise<void> =>
    post<void>('/auth/logout', { refreshToken }),

  /** POST /api/v1/auth/logout-all — revokes all device sessions */
  logoutAll: (): Promise<void> =>
    post<void>('/auth/logout-all'),

  /** POST /api/v1/auth/change-password */
  changePassword: (data: ChangePasswordRequest): Promise<void> =>
    post<void>('/auth/change-password', data),

  /** POST /api/v1/auth/set-password — for newly invited users */
  setInitialPassword: (userId: string, newPassword: string): Promise<void> =>
    post<void>('/auth/set-password', { userId, newPassword }),
};
