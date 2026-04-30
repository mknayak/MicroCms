import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import toast from 'react-hot-toast';
import { adminTenantsApi } from '@/api/adminTenants';
import type { Site } from '@/types';
import { ApiError } from '@/api/client';

// ─── Schemas ──────────────────────────────────────────────────────────────────

const settingsSchema = z.object({
  displayName: z.string().min(1, 'Required'),
  defaultLocale: z.string().min(2, 'Required'),
  timeZoneId: z.string().min(1, 'Required'),
  aiEnabled: z.boolean(),
  logoUrl: z.string().url('Must be a URL').optional().or(z.literal('')),
});

const siteSchema = z.object({
  name: z.string().min(1, 'Required'),
  handle: z
    .string()
    .min(1, 'Required')
    .regex(/^[a-z0-9]+(?:-[a-z0-9]+)*$/, 'Lowercase letters, numbers, hyphens'),
  defaultLocale: z.string().min(2, 'Required'),
});

type SettingsForm = z.infer<typeof settingsSchema>;
type SiteForm = z.infer<typeof siteSchema>;

// ─── Add Site Modal ───────────────────────────────────────────────────────────

function AddSiteModal({ tenantId, onClose }: { tenantId: string; onClose: () => void }) {
  const qc = useQueryClient();
  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<SiteForm>({
 resolver: zodResolver(siteSchema),
    defaultValues: { defaultLocale: 'en' },
  });

  const mutation = useMutation({
    mutationFn: (data: SiteForm) => adminTenantsApi.addSite(tenantId, data),
    onSuccess: () => {
      toast.success('Site added.');
      void qc.invalidateQueries({ queryKey: ['admin-tenant', tenantId] });
    onClose();
    },
    onError: (err) =>
    toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Failed.'),
  });

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="card mx-4 w-full max-w-md space-y-4">
 <div className="flex items-center justify-between">
          <h3 className="text-base font-semibold text-slate-900">Add Site</h3>
      <button onClick={onClose} className="text-slate-400 hover:text-slate-600">✕</button>
        </div>
        <form onSubmit={handleSubmit((v) => mutation.mutate(v))} className="space-y-4">
     <div>
  <label className="form-label">Name</label>
 <input className="form-input mt-1" {...register('name')} placeholder="Main Site" />
          {errors.name && <p className="form-error">{errors.name.message}</p>}
          </div>
          <div>
            <label className="form-label">Handle (unique key)</label>
            <input className="form-input mt-1 font-mono" {...register('handle')} placeholder="main" />
          {errors.handle && <p className="form-error">{errors.handle.message}</p>}
          </div>
          <div>
    <label className="form-label">Default Locale</label>
 <input className="form-input mt-1 font-mono" {...register('defaultLocale')} placeholder="en" />
   {errors.defaultLocale && <p className="form-error">{errors.defaultLocale.message}</p>}
  </div>
      <div className="flex justify-end gap-3">
            <button type="button" onClick={onClose} className="btn-secondary">Cancel</button>
          <button type="submit" disabled={isSubmitting} className="btn-primary">
   {isSubmitting ? 'Adding…' : 'Add Site'}
    </button>
        </div>
        </form>
      </div>
    </div>
  );
}

// ─── Page ─────────────────────────────────────────────────────────────────────

export default function TenantDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const qc = useQueryClient();
  const [showAddSite, setShowAddSite] = useState(false);

  const { data: tenant, isLoading } = useQuery({
  queryKey: ['admin-tenant', id],
    queryFn: () => adminTenantsApi.getById(id!),
    enabled: !!id,
  });

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting, isDirty },
  } = useForm<SettingsForm>({
    resolver: zodResolver(settingsSchema),
    defaultValues: { displayName: '', defaultLocale: 'en', timeZoneId: 'UTC', aiEnabled: false, logoUrl: '' },
  });

  useEffect(() => {
    if (tenant) {
      reset({
        displayName: tenant.displayName,
        defaultLocale: tenant.defaultLocale,
        timeZoneId: tenant.timeZoneId,
        aiEnabled: tenant.aiEnabled,
        logoUrl: tenant.logoUrl ?? '',
      });
    }
  }, [tenant, reset]);

  const updateMutation = useMutation({
    mutationFn: (data: SettingsForm) =>
      adminTenantsApi.updateSettings(id!, {
        ...data,
     logoUrl: data.logoUrl || undefined,
      }),
    onSuccess: () => {
      toast.success('Settings updated.');
      void qc.invalidateQueries({ queryKey: ['admin-tenant', id] });
    },
    onError: (err) =>
  toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Update failed.'),
  });

  if (isLoading) {
    return (
      <div className="space-y-4">
        {Array.from({ length: 3 }).map((_, i) => <div key={i} className="card h-24 animate-pulse" />)}
      </div>
    );
  }

  if (!tenant) {
    return (
      <div className="card text-center text-sm text-slate-500 py-12">
        Tenant not found.{' '}
        <button onClick={() => navigate('/tenants')} className="text-brand-600 hover:underline">
        Back to list
        </button>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center gap-4">
        <button onClick={() => navigate('/tenants')} className="text-slate-400 hover:text-slate-600">
          <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
        </svg>
        </button>
        <div>
          <h1 className="text-2xl font-bold text-slate-900">{tenant.displayName}</h1>
       <p className="font-mono text-sm text-slate-400">{tenant.slug}</p>
      </div>
        <span
      className={`ml-auto ${tenant.status === 'Active' ? 'badge-green' : 'badge-amber'}`}
        >
  {tenant.status}
   </span>
      </div>

      {/* Settings */}
      <form onSubmit={handleSubmit((v) => updateMutation.mutate(v))} className="card space-y-4">
   <h2 className="text-base font-semibold text-slate-900">Tenant Settings</h2>
      <div className="grid grid-cols-2 gap-4">
          <div>
     <label className="form-label">Display Name</label>
      <input className="form-input mt-1" {...register('displayName')} />
 {errors.displayName && <p className="form-error">{errors.displayName.message}</p>}
   </div>
        <div>
<label className="form-label">Default Locale</label>
    <input className="form-input mt-1 font-mono" {...register('defaultLocale')} />
 {errors.defaultLocale && <p className="form-error">{errors.defaultLocale.message}</p>}
    </div>
     <div>
  <label className="form-label">Timezone</label>
  <input className="form-input mt-1 font-mono" {...register('timeZoneId')} />
      {errors.timeZoneId && <p className="form-error">{errors.timeZoneId.message}</p>}
          </div>
          <div>
        <label className="form-label">Logo URL (optional)</label>
            <input className="form-input mt-1 font-mono" {...register('logoUrl')} placeholder="https://…" />
            {errors.logoUrl && <p className="form-error">{errors.logoUrl.message}</p>}
          </div>
        </div>
        <label className="flex items-center gap-2 text-sm">
          <input
            type="checkbox"
       className="h-4 w-4 rounded border-slate-300 text-brand-600"
         {...register('aiEnabled')}
          />
      <span className="font-medium text-slate-700">Enable AI Co-pilot</span>
        </label>
        <div className="flex justify-end gap-3">
          <button type="button" onClick={() => reset()} disabled={!isDirty} className="btn-secondary">
      Discard
          </button>
       <button type="submit" disabled={isSubmitting || !isDirty} className="btn-primary">
            {isSubmitting ? 'Saving…' : 'Save Settings'}
        </button>
    </div>
      </form>

      {/* Sites */}
      <div className="card space-y-4">
        <div className="flex items-center justify-between">
       <h2 className="text-base font-semibold text-slate-900">Sites</h2>
          <button onClick={() => setShowAddSite(true)} className="btn-secondary text-sm">
            + Add Site
       </button>
        </div>
        {tenant.sites.length === 0 ? (
  <p className="text-sm text-slate-400">No sites yet.</p>
        ) : (
<table className="w-full text-sm">
  <thead className="border-b border-slate-100 bg-slate-50">
     <tr>
             <th className="px-4 py-2 text-left font-semibold text-slate-700">Name</th>
     <th className="px-4 py-2 text-left font-semibold text-slate-700">Handle</th>
             <th className="px-4 py-2 text-left font-semibold text-slate-700">Locale</th>
        <th className="px-4 py-2 text-left font-semibold text-slate-700">Status</th>
              <th className="px-4 py-2 text-left font-semibold text-slate-700">Domain</th>
              </tr>
            </thead>
      <tbody className="divide-y divide-slate-100">
           {tenant.sites.map((site: Site) => (
            <tr key={site.id} className="hover:bg-slate-50">
                  <td className="px-4 py-3 font-medium text-slate-900">{site.name}</td>
      <td className="px-4 py-3 font-mono text-slate-500">{site.handle}</td>
 <td className="px-4 py-3 text-slate-500">{site.defaultLocale}</td>
          <td className="px-4 py-3">
          <span className={site.isActive ? 'badge-green' : 'badge-slate'}>
            {site.isActive ? 'Active' : 'Inactive'}
       </span>
   </td>
  <td className="px-4 py-3 font-mono text-xs text-slate-400">
     {site.customDomain ?? '—'}
      </td>
           </tr>
      ))}
          </tbody>
        </table>
        )}
      </div>

      {showAddSite && <AddSiteModal tenantId={id!} onClose={() => setShowAddSite(false)} />}
    </div>
  );
}
