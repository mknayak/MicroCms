import { get, post } from './client';
import type { InstallRequest, InstallResult, InstallStatusResponse } from '@/types';

export const installApi = {
  /** GET /api/v1/install/status — anonymous, safe to call before auth */
  getStatus: (): Promise<InstallStatusResponse> =>
    get<InstallStatusResponse>('/install/status'),

  /** POST /api/v1/install — anonymous, only works when not yet installed */
  install: (data: InstallRequest): Promise<InstallResult> =>
    post<InstallResult>('/install', data),
};
