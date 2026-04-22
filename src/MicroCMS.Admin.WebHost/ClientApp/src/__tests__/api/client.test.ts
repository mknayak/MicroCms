import { describe, it, expect, beforeEach } from 'vitest';
import { tokenStorage, ApiError } from '@/api/client';

describe('tokenStorage', () => {
  beforeEach(() => {
    sessionStorage.clear();
  });

  it('stores and retrieves access token', () => {
    tokenStorage.set('test-token');
    expect(tokenStorage.get()).toBe('test-token');
  });

  it('stores and retrieves refresh token', () => {
    tokenStorage.setRefresh('test-refresh');
    expect(tokenStorage.getRefresh()).toBe('test-refresh');
  });

  it('clears both tokens', () => {
    tokenStorage.set('test-token');
    tokenStorage.setRefresh('test-refresh');
    tokenStorage.clear();
    expect(tokenStorage.get()).toBeNull();
    expect(tokenStorage.getRefresh()).toBeNull();
  });
});

describe('ApiError', () => {
  it('creates error with status and problem details', () => {
    const err = new ApiError(404, { title: 'Not Found', detail: 'Resource not found', status: 404 });
    expect(err.status).toBe(404);
    expect(err.problem.detail).toBe('Resource not found');
    expect(err.message).toBe('Resource not found');
    expect(err.name).toBe('ApiError');
  });

  it('falls back to title if no detail', () => {
    const err = new ApiError(500, { title: 'Server Error', status: 500 });
    expect(err.message).toBe('Server Error');
  });
});
