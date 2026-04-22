import axios, { AxiosError, type AxiosInstance, type AxiosRequestConfig } from 'axios';
import type { AuthTokenResponse, ProblemDetails } from '@/types';

const BASE_URL = '/api/v1';

// ─── Typed API Error ──────────────────────────────────────────────────────────

export class ApiError extends Error {
  readonly status: number;
  readonly problem: ProblemDetails;

  constructor(status: number, problem: ProblemDetails) {
    super(problem.detail ?? problem.title ?? `HTTP ${status}`);
    this.name = 'ApiError';
    this.status = status;
    this.problem = problem;
  }
}

// ─── Token Storage ────────────────────────────────────────────────────────────

const TOKEN_KEY = 'mcms_token';
const REFRESH_KEY = 'mcms_refresh';

export const tokenStorage = {
  get: (): string | null => sessionStorage.getItem(TOKEN_KEY),
  set: (token: string): void => sessionStorage.setItem(TOKEN_KEY, token),
  getRefresh: (): string | null => sessionStorage.getItem(REFRESH_KEY),
  setRefresh: (token: string): void => sessionStorage.setItem(REFRESH_KEY, token),
  clear: (): void => {
    sessionStorage.removeItem(TOKEN_KEY);
    sessionStorage.removeItem(REFRESH_KEY);
  },
};

// ─── Axios Instance ───────────────────────────────────────────────────────────

let isRefreshing = false;
let refreshQueue: Array<(token: string) => void> = [];

function processQueue(token: string) {
  refreshQueue.forEach((resolve) => resolve(token));
  refreshQueue = [];
}

function createApiClient(): AxiosInstance {
  const instance = axios.create({
    baseURL: BASE_URL,
    headers: {
      'Content-Type': 'application/json',
    },
    timeout: 30_000,
  });

  // Request interceptor — attach Bearer token
  instance.interceptors.request.use((config) => {
    const token = tokenStorage.get();
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  });

  // Response interceptor — on 401, attempt a silent token refresh before giving up
  instance.interceptors.response.use(
    (response) => response,
    async (error: AxiosError<ProblemDetails>) => {
      const originalRequest = error.config as AxiosRequestConfig & { _retry?: boolean };

      if (
        error.response?.status === 401 &&
        !originalRequest._retry &&
        !originalRequest.url?.includes('/auth/refresh') &&
        !originalRequest.url?.includes('/auth/login')
      ) {
        const rawRefresh = tokenStorage.getRefresh();

        if (!rawRefresh) {
          tokenStorage.clear();
          window.location.href = '/login';
          return Promise.reject(error);
        }

        if (isRefreshing) {
          // Queue the request until refresh completes
          return new Promise<string>((resolve) => {
            refreshQueue.push(resolve);
          }).then((newToken) => {
            originalRequest.headers = {
              ...originalRequest.headers,
              Authorization: `Bearer ${newToken}`,
            };
            return instance(originalRequest);
          });
        }

        originalRequest._retry = true;
        isRefreshing = true;

        try {
          const response = await instance.post<AuthTokenResponse>('/auth/refresh', {
            refreshToken: rawRefresh,
          });
          const newToken = response.data.accessToken;
          tokenStorage.set(newToken);
          tokenStorage.setRefresh(response.data.refreshToken);
          processQueue(newToken);
          originalRequest.headers = {
            ...originalRequest.headers,
            Authorization: `Bearer ${newToken}`,
          };
          return instance(originalRequest);
        } catch {
          tokenStorage.clear();
          refreshQueue = [];
          window.location.href = '/login';
          return Promise.reject(error);
        } finally {
          isRefreshing = false;
        }
      }

      const status = error.response?.status ?? 0;
      const problem: ProblemDetails = error.response?.data ?? {
        title: 'Network Error',
        detail: error.message,
        status,
      };

      return Promise.reject(new ApiError(status, problem));
    },
  );

  return instance;
}

export const apiClient: AxiosInstance = createApiClient();

// ─── Generic Helpers ──────────────────────────────────────────────────────────

export async function get<T>(url: string, config?: AxiosRequestConfig): Promise<T> {
  const response = await apiClient.get<T>(url, config);
  return response.data;
}

export async function post<T>(url: string, data?: unknown, config?: AxiosRequestConfig): Promise<T> {
  const response = await apiClient.post<T>(url, data, config);
  return response.data;
}

export async function put<T>(url: string, data?: unknown, config?: AxiosRequestConfig): Promise<T> {
  const response = await apiClient.put<T>(url, data, config);
  return response.data;
}

export async function patch<T>(url: string, data?: unknown, config?: AxiosRequestConfig): Promise<T> {
  const response = await apiClient.patch<T>(url, data, config);
  return response.data;
}

export async function del<T = void>(url: string, config?: AxiosRequestConfig): Promise<T> {
  const response = await apiClient.delete<T>(url, config);
  return response.data;
}
