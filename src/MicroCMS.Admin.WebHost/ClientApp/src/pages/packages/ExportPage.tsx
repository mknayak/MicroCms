import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { packagesApi } from '@/api/packages';
import { tenantsApi } from '@/api/tenants';
import { useSite } from '@/contexts/SiteContext';
import { useAuth } from '@/contexts/AuthContext';
import type { ExportOptions, Site } from '@/types';

// ─── Selectable category ──────────────────────────────────────────────────────

interface Category {
  key: keyof Omit<ExportOptions, 'tenantId' | 'siteId'>;
  label: string;
  description: string;
  icon: string;
  adminOnly?: boolean;
}

const CATEGORIES: Category[] = [
  { key: 'includeContentTypes', label: 'Content Types', description: 'Schemas and field definitions', icon: '📋' },
  { key: 'includeEntries',      label: 'Entries',       description: 'Published and draft content entries', icon: '📝' },
  { key: 'includePages',  label: 'Pages',         description: 'Page tree and routing', icon: '🗂️' },
  { key: 'includeLayouts',  label: 'Layouts',  description: 'Layout definitions and zone configuration', icon: '🖼️' },
  { key: 'includeComponents',   label: 'Components',    description: 'Component library schemas and templates', icon: '🧩' },
  { key: 'includeMediaMetadata',label: 'Media Metadata',description: 'Asset metadata (not binary files)', icon: '🖼' },
  { key: 'includeSiteSettings', label: 'Site Settings', description: 'Site configuration and locales', icon: '⚙️' },
  { key: 'includeUsers',        label: 'Users',         description: 'User accounts and role assignments', icon: '👤', adminOnly: true },
];

// ─── Page ─────────────────────────────────────────────────────────────────────

export default function ExportPage() {
  const { hasRole } = useAuth();
  const { selectedSiteId, sites } = useSite();
  const isAdmin = hasRole('SystemAdmin') || hasRole('TenantAdmin');

  const { data: tenant } = useQuery({
    queryKey: ['current-tenant'],
 queryFn: tenantsApi.getCurrent,
  });

  // Allow admin to switch tenant/site for export
  const [selectedTenantId] = useState<string>(tenant?.id ?? '');
  const [exportSiteId, setExportSiteId] = useState<string>(selectedSiteId ?? '');

  const [selections, setSelections] = useState<Record<string, boolean>>({
    includeContentTypes:  true,
    includeEntries:       true,
    includePages: true,
    includeLayouts:    true,
    includeComponents:    true,
    includeMediaMetadata: true,
    includeSiteSettings:  true,
  includeUsers:         false,
  });

  const [exporting, setExporting] = useState(false);

const visibleCategories = CATEGORIES.filter(c => !c.adminOnly || isAdmin);
  const allSelected = visibleCategories.every(c => selections[c.key]);

  const toggleAll = () => {
    const next = !allSelected;
    const updated = { ...selections };
    visibleCategories.forEach(c => { updated[c.key] = next; });
    setSelections(updated);
  };

  const handleExport = async () => {
    const tenantId = selectedTenantId || tenant?.id;
    const siteId   = exportSiteId    || selectedSiteId;

    if (!tenantId || !siteId) {
      toast.error('Please select a tenant and site before exporting.');
      return;
    }
    if (!visibleCategories.some(c => selections[c.key])) {
      toast.error('Select at least one category to export.');
      return;
    }

    setExporting(true);
    try {
   const options: ExportOptions = {
        tenantId,
        siteId,
  includeContentTypes:  selections.includeContentTypes  ?? false,
        includeEntries:       selections.includeEntries       ?? false,
    includePages:         selections.includePages         ?? false,
        includeLayouts:       selections.includeLayouts ?? false,
     includeComponents:    selections.includeComponents    ?? false,
     includeMediaMetadata: selections.includeMediaMetadata ?? false,
 includeSiteSettings:  selections.includeSiteSettings  ?? false,
        includeUsers:      selections.includeUsers       ?? false,
   };
      await packagesApi.export(options);
      toast.success('Package exported and downloaded.');
    } catch {
      toast.error('Export failed. Please try again.');
    } finally {
  setExporting(false);
    }
  };

  const exportSites: Site[] = tenant?.sites ?? sites;
  const tenantName = tenant?.displayName ?? 'Current Tenant';

  return (
  <div className="mx-auto max-w-2xl space-y-6">
      {/* Header */}
      <div>
   <h1 className="text-2xl font-bold text-slate-900">Export Package</h1>
        <p className="mt-1 text-sm text-slate-500">
          Download a ZIP archive of selected site data for backup or migration.
     </p>
  </div>

      {/* Tenant / Site selector */}
      <div className="rounded-lg border border-slate-200 bg-white px-5 py-4 space-y-4">
        <h2 className="text-sm font-semibold text-slate-800">Source</h2>
        <div className="grid grid-cols-2 gap-4">
      <div>
        <label className="form-label">Tenant</label>
            <input
    className="form-input mt-1 bg-slate-50"
      value={tenantName}
       readOnly
 />
          </div>
          <div>
            <label className="form-label">Site</label>
      <select
           className="form-input mt-1"
value={exportSiteId}
     onChange={e => setExportSiteId(e.target.value)}
      >
       <option value="">— Select site —</option>
        {exportSites.map(s => (
         <option key={s.id} value={s.id}>{s.name} ({s.handle})</option>
              ))}
   </select>
        </div>
        </div>
      </div>

      {/* Category selector */}
      <div className="rounded-lg border border-slate-200 bg-white px-5 py-4 space-y-4">
        <div className="flex items-center justify-between">
          <h2 className="text-sm font-semibold text-slate-800">What to Export</h2>
          <button
     type="button"
          onClick={toggleAll}
            className="text-xs text-brand-600 hover:underline"
        >
     {allSelected ? 'Deselect all' : 'Select all'}
          </button>
  </div>

        <div className="grid grid-cols-1 gap-2 sm:grid-cols-2">
          {visibleCategories.map(cat => (
  <label
        key={cat.key}
       className={`flex cursor-pointer items-start gap-3 rounded-lg border p-3 transition-colors ${
    selections[cat.key]
   ? 'border-brand-300 bg-brand-50'
       : 'border-slate-200 hover:border-slate-300 hover:bg-slate-50'
   }`}
            >
              <input
type="checkbox"
         className="mt-0.5 h-4 w-4 rounded border-slate-300 text-brand-600"
         checked={!!selections[cat.key]}
     onChange={e => setSelections(prev => ({ ...prev, [cat.key]: e.target.checked }))}
       />
     <div className="min-w-0">
          <div className="flex items-center gap-1.5 text-sm font-medium text-slate-800">
       <span>{cat.icon}</span>
                <span>{cat.label}</span>
    {cat.adminOnly && (
  <span className="rounded bg-amber-100 px-1.5 py-0.5 text-[10px] font-semibold text-amber-700">Admin</span>
           )}
</div>
      <p className="mt-0.5 text-xs text-slate-500">{cat.description}</p>
              </div>
            </label>
          ))}
   </div>
      </div>

      {/* Info banner */}
      <div className="flex items-start gap-2 rounded-lg border border-blue-100 bg-blue-50 px-4 py-3 text-sm text-blue-700">
     <span className="mt-0.5 shrink-0">ℹ</span>
        <span>
          The package is a <strong>.zip</strong> archive containing structured JSON files.
 Binary media files are <strong>not</strong> included — only metadata.
      You can re-import the package into any site using the Import page.
   </span>
    </div>

      {/* Action */}
      <div className="flex justify-end">
        <button
          type="button"
          onClick={handleExport}
       disabled={exporting || !exportSiteId}
       className="btn-primary disabled:opacity-50"
>
          {exporting ? (
            <span className="flex items-center gap-2">
      <svg className="h-4 w-4 animate-spin" viewBox="0 0 24 24" fill="none">
                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
     <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z" />
       </svg>
           Exporting…
            </span>
          ) : (
   '⬇ Export & Download'
          )}
        </button>
      </div>
    </div>
  );
}
