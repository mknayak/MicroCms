import { NavLink } from 'react-router-dom';
import clsx from 'clsx';
import { useAuth } from '@/contexts/AuthContext';

// ─── Types ────────────────────────────────────────────────────────────────────

interface NavItem {
  label: string;
  href: string;
  icon: React.ReactNode;
  roles?: string[];
}

interface NavSection {
  title: string;
  roles?: string[];
  items: NavItem[];
}

// ─── Icon helper ──────────────────────────────────────────────────────────────

function Icon({ d, d2 }: { d: string; d2?: string }) {
  return (
    <svg className="h-5 w-5 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d={d} />
      {d2 && <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d={d2} />}
    </svg>
  );
}

// ─── Nav Sections ─────────────────────────────────────────────────────────────

const NAV_SECTIONS: NavSection[] = [
  // 1 ── Always visible
  {
    title: '',
    items: [
   {
        label: 'Dashboard',
        href: '/',
        icon: <Icon d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />,
    },
    ],
  },

  // 2 ── Content — site-level authoring
  {
    title: 'Content',
 items: [
      {
        label: 'Entries',
        href: '/entries',
        icon: <Icon d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />,
      },
      {
     label: 'Pages',
        href: '/pages',
        icon: <Icon d="M3 7h18M3 12h18M3 17h18" />,
      },
      {
        label: 'Media Library',
        href: '/media',
        icon: <Icon d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />,
 },
      {
        label: 'Taxonomy',
    href: '/taxonomy',
    icon: <Icon d="M7 7h.01M7 3h5c.512 0 1.024.195 1.414.586l7 7a2 2 0 010 2.828l-7 7a2 2 0 01-2.828 0l-7-7A1.994 1.994 0 013 12V7a4 4 0 014-4z" />,
   },
    ],
  },

  // 3 ── Structure — content modelling
  {
    title: 'Structure',
    roles: ['SystemAdmin', 'TenantAdmin', 'SiteAdmin'],
    items: [
      {
        label: 'Content Types',
        href: '/content-types',
        icon: <Icon d="M4 6h16M4 10h16M4 14h16M4 18h16" />,
      },
    ],
  },

  // 4 ── Tenant Management
  {
    title: 'Tenant Management',
    roles: ['SystemAdmin', 'TenantAdmin'],
    items: [
  {
        label: 'Users',
        href: '/users',
        roles: ['SystemAdmin', 'TenantAdmin'],
        icon: <Icon d="M12 4.354a4 4 0 110 5.292M15 21H3v-1a6 6 0 0112 0v1zm0 0h6v-1a6 6 0 00-9-5.197M13 7a4 4 0 11-8 0 4 4 0 018 0z" />,
      },
      {
        label: 'Settings',
    href: '/settings',
        roles: ['SystemAdmin', 'TenantAdmin'],
  icon: (
       <Icon
   d="M10.325 4.317c.426-1.756 2.924-1.756 3.35 0a1.724 1.724 0 002.573 1.066c1.543-.94 3.31.826 2.37 2.37a1.724 1.724 0 001.065 2.572c1.756.426 1.756 2.924 0 3.35a1.724 1.724 0 00-1.066 2.573c.94 1.543-.826 3.31-2.37 2.37a1.724 1.724 0 00-2.572 1.065c-.426 1.756-2.924 1.756-3.35 0a1.724 1.724 0 00-2.573-1.066c-1.543.94-3.31-.826-2.37-2.37a1.724 1.724 0 00-1.065-2.572c-1.756-.426-1.756-2.924 0-3.35a1.724 1.724 0 001.066-2.573c-.94-1.543.826-3.31 2.37-2.37.996.608 2.296.07 2.572-1.065z"
      d2="M15 12a3 3 0 11-6 0 3 3 0 016 0z"
 />
        ),
      },
    ],
  },

  // 5 ── Global Administration — SystemAdmin only
  {
    title: 'Global Administration',
    roles: ['SystemAdmin'],
    items: [
      {
        label: 'Tenants',
     href: '/tenants',
        roles: ['SystemAdmin'],
   icon: <Icon d="M19 21V5a2 2 0 00-2-2H7a2 2 0 00-2 2v16m14 0h2m-2 0h-5m-9 0H3m2 0h5M9 7h1m-1 4h1m4-4h1m-1 4h1m-2 10v-5a1 1 0 011-1h2a1 1 0 011 1v5m-4 0h4" />,
      },
],
  },
];

// ─── Component ────────────────────────────────────────────────────────────────

interface SidebarProps {
  collapsed?: boolean;
}

export function Sidebar({ collapsed = false }: SidebarProps) {
  const { user, hasRole } = useAuth();

  const canSeeSection = (section: NavSection) =>
    !section.roles || section.roles.some((r) => hasRole(r));

  const canSeeItem = (item: NavItem) =>
    !item.roles || item.roles.some((r) => hasRole(r));

  return (
    <aside
      className={clsx(
'flex h-full flex-col border-r border-slate-200 bg-white transition-all duration-200',
     collapsed ? 'w-16' : 'w-64',
      )}
 >
{/* Brand */}
      <div className="flex h-16 items-center gap-3 border-b border-slate-200 px-4">
        <div className="flex h-8 w-8 flex-shrink-0 items-center justify-center rounded-lg bg-brand-600 text-white">
          <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
       <path
  strokeLinecap="round"
         strokeLinejoin="round"
           strokeWidth={2}
     d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
            />
</svg>
        </div>
 {!collapsed && (
          <span className="text-sm font-bold text-slate-900">MicroCMS</span>
   )}
      </div>

      {/* Nav */}
      <nav className="flex-1 overflow-y-auto py-3">
 {NAV_SECTIONS.filter(canSeeSection).map((section, si) => {
     const visibleItems = section.items.filter(canSeeItem);
    if (visibleItems.length === 0) return null;

      return (
          <div key={si} className={clsx(si > 0 && 'mt-4')}>
              {/* Section label */}
      {section.title && !collapsed && (
    <p className="mb-1 px-4 text-[10px] font-semibold uppercase tracking-wider text-slate-400">
         {section.title}
          </p>
           )}
          {section.title && collapsed && (
  <div className="mx-3 mb-1 border-t border-slate-100" />
        )}

  <div className="space-y-0.5 px-2">
      {visibleItems.map((item) => (
       <NavLink
            key={item.href}
          to={item.href}
   end={item.href === '/'}
    className={({ isActive }) =>
             clsx(
              'flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition-colors',
              isActive
              ? 'bg-brand-50 text-brand-700'
       : 'text-slate-600 hover:bg-slate-100 hover:text-slate-900',
        collapsed && 'justify-center',
   )
      }
        title={collapsed ? item.label : undefined}
     >
        {item.icon}
      {!collapsed && <span>{item.label}</span>}
     </NavLink>
              ))}
         </div>
            </div>
          );
        })}
      </nav>

      {/* User footer */}
      {user && (
        <div
          className={clsx(
  'flex items-center gap-3 border-t border-slate-200 p-3',
 collapsed && 'justify-center',
       )}
     >
      <div className="flex h-8 w-8 flex-shrink-0 items-center justify-center rounded-full bg-brand-600 text-xs font-semibold text-white">
       {user.displayName.charAt(0).toUpperCase()}
          </div>
          {!collapsed && (
     <div className="min-w-0 flex-1">
    <p className="truncate text-xs font-medium text-slate-900">{user.displayName}</p>
    <p className="truncate text-xs text-slate-500">{user.email}</p>
   </div>
          )}
     </div>
      )}
    </aside>
  );
}
