import { useState } from 'react';
import { Outlet, useNavigate } from 'react-router-dom';
import { Sidebar } from './Sidebar';
import { useAuth } from '@/contexts/AuthContext';
import { useSite } from '@/contexts/SiteContext';
import toast from 'react-hot-toast';

// ─── Component ────────────────────────────────────────────────────────────────

export function AppShell() {
  const [sidebarCollapsed, setSidebarCollapsed] = useState(false);
  const { user, logout } = useAuth();
  const { sites, selectedSiteId, setSelectedSiteId, isLoading: sitesLoading } = useSite();
  const navigate = useNavigate();

  const handleLogout = async () => {
    try {
      await logout();
      navigate('/login');
    } catch {
      toast.error('Logout failed. Please try again.');
    }
  };

  return (
    <div className="flex h-screen overflow-hidden bg-slate-50">
      {/* Sidebar */}
      <Sidebar collapsed={sidebarCollapsed} />

      {/* Main area */}
      <div className="flex flex-1 flex-col overflow-hidden">
        {/* Top bar */}
        <header className="flex h-16 flex-shrink-0 items-center justify-between border-b border-slate-200 bg-white px-6">
          {/* Left: collapse toggle + site picker */}
          <div className="flex items-center gap-4">
            <button
              onClick={() => setSidebarCollapsed((c) => !c)}
              className="rounded-lg p-1.5 text-slate-500 hover:bg-slate-100 hover:text-slate-700"
              aria-label="Toggle sidebar"
            >
              <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 12h16M4 18h16" />
              </svg>
            </button>

            {/* Site Picker */}
            {!sitesLoading && sites.length > 0 && (
              <div className="flex items-center gap-2">
                <span className="hidden text-xs font-medium text-slate-400 sm:block">Site</span>
                <div className="relative">
                  <select
                    value={selectedSiteId ?? ''}
                    onChange={(e) => setSelectedSiteId(e.target.value)}
                    className="h-8 appearance-none rounded-lg border border-slate-200 bg-slate-50 pl-3 pr-8 text-sm font-medium text-slate-700 focus:border-brand-500 focus:outline-none focus:ring-1 focus:ring-brand-500"
                  >
                    {sites.map((site) => (
                      <option key={site.id} value={site.id}>
                        {site.name}
                        {!site.isActive ? ' (inactive)' : ''}
                      </option>
                    ))}
                  </select>
                  {/* Chevron */}
                  <svg
                    className="pointer-events-none absolute right-2 top-1/2 h-3.5 w-3.5 -translate-y-1/2 text-slate-400"
                    fill="none"
                    viewBox="0 0 24 24"
                    stroke="currentColor"
                  >
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
                  </svg>
                </div>
                {/* Active site indicator dot */}
                <span className="h-2 w-2 rounded-full bg-green-400" title="Active site" />
              </div>
            )}
          </div>

          {/* Right: user menu */}
          <div className="flex items-center gap-3">
            {/* User menu */}
            <div className="relative flex items-center gap-2">
              <div className="flex h-8 w-8 items-center justify-center rounded-full bg-brand-600 text-xs font-semibold text-white">
                {user?.displayName.charAt(0).toUpperCase() ?? '?'}
              </div>
              <span className="hidden text-sm font-medium text-slate-700 sm:block">
                {user?.displayName}
              </span>
              <button
                onClick={handleLogout}
                className="ml-2 rounded-lg px-3 py-1.5 text-sm text-slate-500 hover:bg-slate-100 hover:text-slate-700"
              >
                Sign out
              </button>
            </div>
          </div>
        </header>

        {/* Page content */}
        <main className="flex-1 overflow-y-auto p-6">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
