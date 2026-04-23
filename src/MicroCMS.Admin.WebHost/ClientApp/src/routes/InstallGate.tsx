import { useEffect } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { useInstall } from '@/contexts/InstallContext';
import { PageLoader } from '@/components/ui/ErrorBoundary';

interface InstallGateProps {
  children: React.ReactNode;
}

/**
 * Wraps the whole application and redirects to /install when the system
 * has not yet been set up. While the status check is in-flight a full-page
 * loader is shown so there is no flash of the login or dashboard.
 *
 * Once the system is installed this component renders its children unchanged
 * and never re-checks (the result is cached in InstallContext).
 */
export function InstallGate({ children }: InstallGateProps) {
  const { installState } = useInstall();
  const navigate = useNavigate();
  const location = useLocation();

  const isOnInstallPage = location.pathname.startsWith('/install');

  useEffect(() => {
    if (installState === 'needed' && !isOnInstallPage) {
      navigate('/install', { replace: true });
    }

    if (installState === 'done' && isOnInstallPage) {
      navigate('/login', { replace: true });
    }
  }, [installState, isOnInstallPage, navigate]);

  // Show a full-page loader while we wait for the status check
  if (installState === 'checking') {
    return <PageLoader />;
  }

  return <>{children}</>;
}
