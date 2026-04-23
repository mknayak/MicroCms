import React, { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import { installApi } from '@/api/install';

// ─── Types ────────────────────────────────────────────────────────────────────

type InstallState = 'checking' | 'needed' | 'done';

interface InstallContextValue {
  installState: InstallState;
  markInstalled: () => void;
}

// ─── Context ──────────────────────────────────────────────────────────────────

const InstallContext = createContext<InstallContextValue | null>(null);

// ─── Provider ─────────────────────────────────────────────────────────────────

export function InstallProvider({ children }: { children: React.ReactNode }) {
  const [installState, setInstallState] = useState<InstallState>('checking');

  useEffect(() => {
 let cancelled = false;

    installApi
      .getStatus()
      .then((res) => {
        if (!cancelled) {
          setInstallState(res.isInstalled ? 'done' : 'needed');
        }
      })
      .catch(() => {
    // If we cannot reach the API at all, assume the API isn't running yet.
     // Don't block the UI — show a loading state and let the user retry.
        if (!cancelled) setInstallState('needed');
      });

    return () => {
    cancelled = true;
    };
  }, []);

  const markInstalled = useCallback(() => setInstallState('done'), []);

  const value = useMemo<InstallContextValue>(
    () => ({ installState, markInstalled }),
    [installState, markInstalled],
  );

  return <InstallContext.Provider value={value}>{children}</InstallContext.Provider>;
}

// ─── Hook ─────────────────────────────────────────────────────────────────────

export function useInstall(): InstallContextValue {
  const ctx = useContext(InstallContext);
  if (!ctx) throw new Error('useInstall must be used within InstallProvider');
  return ctx;
}
