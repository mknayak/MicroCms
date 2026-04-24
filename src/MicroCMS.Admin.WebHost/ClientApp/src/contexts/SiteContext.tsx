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
  setSelectedSiteId: (id: string) => void;
  isLoading: boolean;
}

// ─── Context ──────────────────────────────────────────────────────────────────

const SiteContext = createContext<SiteContextValue | null>(null);

const STORAGE_KEY = 'microcms:selectedSiteId';

// ─── Provider ─────────────────────────────────────────────────────────────────

export function SiteProvider({ children }: { children: React.ReactNode }) {
  const { isAuthenticated } = useAuth();

  const { data: tenant, isLoading } = useQuery({
    queryKey: ['current-tenant'],
    queryFn: () => tenantsApi.getCurrent(),
    enabled: isAuthenticated,
    staleTime: 5 * 60 * 1000,
  });

  const sites: Site[] = tenant?.sites ?? [];

  // Restore persisted selection; fall back to the first active site
  const [selectedSiteId, setSelectedSiteIdState] = useState<string | null>(() => {
    return localStorage.getItem(STORAGE_KEY);
  });

  // Once sites load, validate the persisted id or pick a default
  useEffect(() => {
    if (sites.length === 0) return;
    const ids = sites.map((s) => s.id);
    if (selectedSiteId && ids.includes(selectedSiteId)) return; // still valid
    const defaultSite = sites.find((s) => s.isActive) ?? sites[0];
    setSelectedSiteIdState(defaultSite.id);
    localStorage.setItem(STORAGE_KEY, defaultSite.id);
  }, [sites, selectedSiteId]);

  const setSelectedSiteId = useCallback((id: string) => {
    setSelectedSiteIdState(id);
    localStorage.setItem(STORAGE_KEY, id);
  }, []);

  const selectedSite = useMemo(
    () => sites.find((s) => s.id === selectedSiteId) ?? null,
    [sites, selectedSiteId],
  );

  const value = useMemo<SiteContextValue>(
    () => ({ sites, selectedSite, selectedSiteId, setSelectedSiteId, isLoading }),
    [sites, selectedSite, selectedSiteId, setSelectedSiteId, isLoading],
  );

  return <SiteContext.Provider value={value}>{children}</SiteContext.Provider>;
}

// ─── Hook ─────────────────────────────────────────────────────────────────────

export function useSite(): SiteContextValue {
const ctx = useContext(SiteContext);
  if (!ctx) throw new Error('useSite must be used within SiteProvider');
  return ctx;
}
