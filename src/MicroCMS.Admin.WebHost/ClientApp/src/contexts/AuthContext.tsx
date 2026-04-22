import React, { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState } from 'react';
import { authApi } from '@/api/auth';
import { tokenStorage } from '@/api/client';
import type { AuthTokenResponse, CurrentUser } from '@/types';

// ─── Types ────────────────────────────────────────────────────────────────────

interface AuthContextValue {
  user: CurrentUser | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => Promise<void>;
  hasRole: (role: string) => boolean;
}

// ─── Context ──────────────────────────────────────────────────────────────────

const AuthContext = createContext<AuthContextValue | null>(null);

// ─── Provider ─────────────────────────────────────────────────────────────────

interface AuthProviderProps {
  children: React.ReactNode;
}

export function AuthProvider({ children }: AuthProviderProps) {
  const [user, setUser] = useState<CurrentUser | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const refreshTimerRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  // Schedule automatic silent token refresh 60 s before expiry
  const scheduleRefresh = useCallback((expiryIso: string) => {
    if (refreshTimerRef.current) clearTimeout(refreshTimerRef.current);

    const expiresAt = new Date(expiryIso).getTime();
    const msUntilRefresh = Math.max(expiresAt - Date.now() - 60_000, 5_000);

    refreshTimerRef.current = setTimeout(async () => {
      const rawRefresh = tokenStorage.getRefresh();
      if (!rawRefresh) return;

      try {
        const response = await authApi.refresh(rawRefresh);
        applyTokenResponse(response);
        scheduleRefresh(response.accessTokenExpiry);
      } catch {
        // Refresh failed — clear session, user will be redirected by axios interceptor
        tokenStorage.clear();
        setUser(null);
      }
    }, msUntilRefresh);
  }, []);

  // Apply a fresh token response: store tokens + set user state
  const applyTokenResponse = useCallback(
    (response: AuthTokenResponse) => {
      tokenStorage.set(response.accessToken);
      tokenStorage.setRefresh(response.refreshToken);
      setUser({
        id: response.user.userId,
        email: response.user.email,
        displayName: response.user.displayName,
        roles: response.user.roles,
        tenantId: extractTenantId(response.accessToken),
      });
      scheduleRefresh(response.accessTokenExpiry);
    },
    [scheduleRefresh],
  );

  // On mount: attempt silent restore from stored token
  useEffect(() => {
    const token = tokenStorage.get();
    const refresh = tokenStorage.getRefresh();

    if (!token || !refresh) {
      setIsLoading(false);
      return;
    }

    // If access token is still valid, restore from claims
    const payload = parseJwt(token);
    if (payload && typeof payload['exp'] === 'number' && payload['exp'] * 1000 > Date.now()) {
      setUser(extractUserFromClaims(payload));
      // Infer expiry from JWT exp claim
      const expIso = new Date((payload['exp'] as number) * 1000).toISOString();
      scheduleRefresh(expIso);
      setIsLoading(false);
      return;
    }

    // Access token expired — try to refresh silently
    authApi
      .refresh(refresh)
      .then((response) => {
        applyTokenResponse(response);
      })
      .catch(() => {
        tokenStorage.clear();
      })
      .finally(() => {
        setIsLoading(false);
      });
  }, [scheduleRefresh, applyTokenResponse]);

  // Cleanup timer on unmount
  useEffect(() => {
    return () => {
      if (refreshTimerRef.current) clearTimeout(refreshTimerRef.current);
    };
  }, []);

  const login = useCallback(
    async (email: string, password: string): Promise<void> => {
      const response = await authApi.login(email, password);
      applyTokenResponse(response);
    },
    [applyTokenResponse],
  );

  const logout = useCallback(async (): Promise<void> => {
    const refreshToken = tokenStorage.getRefresh();
    if (refreshTimerRef.current) clearTimeout(refreshTimerRef.current);

    try {
      if (refreshToken) {
        await authApi.logout(refreshToken);
      }
    } finally {
      tokenStorage.clear();
      setUser(null);
    }
  }, []);

  const hasRole = useCallback(
    (role: string): boolean => user?.roles.includes(role) ?? false,
    [user],
  );

  const value = useMemo<AuthContextValue>(
    () => ({
      user,
      isAuthenticated: user !== null,
      isLoading,
      login,
      logout,
      hasRole,
    }),
    [user, isLoading, login, logout, hasRole],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

// ─── Hook ─────────────────────────────────────────────────────────────────────

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) {
    throw new Error('useAuth must be used within AuthProvider');
  }
  return ctx;
}

// ─── Helpers ──────────────────────────────────────────────────────────────────

function parseJwt(token: string): Record<string, unknown> | null {
  try {
    const parts = token.split('.');
    if (parts.length !== 3) return null;
    const base64 = parts[1].replace(/-/g, '+').replace(/_/g, '/');
    const json = atob(base64);
    return JSON.parse(json) as Record<string, unknown>;
  } catch {
    return null;
  }
}

function extractUserFromClaims(payload: Record<string, unknown>): CurrentUser {
  const rolesClaim = payload['role'];
  const roles = Array.isArray(rolesClaim)
    ? rolesClaim.map(String)
    : typeof rolesClaim === 'string'
      ? [rolesClaim]
      : [];

  return {
    id: String(payload['sub'] ?? ''),
    email: String(payload['email'] ?? ''),
    displayName: String(payload['display_name'] ?? payload['email'] ?? ''),
    roles,
    tenantId: String(payload['tenant_id'] ?? ''),
  };
}

/** Extracts tenant_id from a JWT without verifying the signature (trusted server-issued token). */
function extractTenantId(token: string): string {
  const payload = parseJwt(token);
  return payload ? String(payload['tenant_id'] ?? '') : '';
}
