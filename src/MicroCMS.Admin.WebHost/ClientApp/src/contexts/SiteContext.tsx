import React, {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useState,
} from 'react';
import { useQuery } from '@tanstack/react-query';
import { tenantsApi } from '@/api/tenants';
import type { Site } from '@/types';
import { useAuth } from './AuthContext';

// ─── Types ────────────────────────────────────────────────────────────────────

interface SiteContextValue {
  sites: Site[];
  selectedSite: Site | null;
  selectedSiteId: string | null;
  setSelectedSiteId: (id: string) => Promise<void>;
  isSwitching: boolean;
  isLoading: boolean;
}

// ─── Context ──────────────────────────────────────────────────────────────────

const SiteContext = createContext<SiteContextValue | null>(null);

// ─── Provider ─────────────────────────────────────────────────────────────────

export function SiteProvider({ children }: { children: React.ReactNode }) {
  const { isAuthenticated, user, switchSite } = useAuth();
  const [isSwitching, setIsSwitching] = useState(false);

  const { data: tenant, isLoading } = useQuery({
    queryKey: ['current-tenant'],
    queryFn: () => tenantsApi.getCurrent(),
    enabled: isAuthenticated,
    staleTime: 5 * 60 * 1000,
  });

  const sites: Site[] = tenant?.sites ?? [];

  // Auto-switch to the first active site when no site is embedded in the token
  useEffect(() => {
    if (!isAuthenticated || user?.siteId || sites.length === 0) return;
    const defaultSite = sites.find((s) => s.isActive) ?? sites[0];
    void switchSite(defaultSite.id);
  }, [isAuthenticated, user?.siteId, sites, switchSite]);

  const selectedSiteId = user?.siteId ?? null;

  const setSelectedSiteId = useCallback(
    async (id: string): Promise<void> => {
      if (id === selectedSiteId) return;
      setIsSwitching(true);
      try {
        await switchSite(id);
      } finally {
        setIsSwitching(false);
      }
    },
    [selectedSiteId, switchSite],
  );

  const selectedSite = useMemo(
    () => sites.find((s) => s.id === selectedSiteId) ?? null,
    [sites, selectedSiteId],
  );

  const value = useMemo<SiteContextValue>(
    () => ({ sites, selectedSite, selectedSiteId, setSelectedSiteId, isSwitching, isLoading }),
    [sites, selectedSite, selectedSiteId, setSelectedSiteId, isSwitching, isLoading],
  );

  return <SiteContext.Provider value={value}>{children}</SiteContext.Provider>;
}

// ─── Hook ─────────────────────────────────────────────────────────────────────

export function useSite(): SiteContextValue {
  const ctx = useContext(SiteContext);
  if (!ctx) throw new Error('useSite must be used within SiteProvider');
  return ctx;
}

