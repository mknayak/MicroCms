import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import toast from 'react-hot-toast';
import { adminTenantsApi } from '@/api/adminTenants';
import type { TenantListItem } from '@/types';
import { ApiError } from '@/api/client';
import { formatDistanceToNow } from 'date-fns';

// ─── Schema ───────────────────────────────────────────────────────────────────

const onboardSchema = z.object({
  slug: z
    .string()
 .min(2, 'Minimum 2 characters')
 .regex(/^[a-z0-9]+(?:-[a-z0-9]+)*$/, 'Lowercase letters, numbers and hyphens only'),
  displayName: z.string().min(1, 'Display name is required'),
  defaultLocale: z.string().min(2, 'Locale required'),
  timeZoneId: z.string().min(1, 'Timezone required'),
  adminEmail: z.string().email('Valid email required'),
  adminDisplayName: z.string().min(1, 'Admin name required'),
  defaultSiteName: z.string().min(1, 'Site name required'),
});

type OnboardForm = z.infer<typeof onboardSchema>;

const STATUS_BADGE: Record<string, string> = {
  Active: 'badge-green',
  Suspended: 'badge-amber',
  Provisioning: 'badge-brand',
};

// ─── Onboard Modal ────────────────────────────────────────────────────────────

function OnboardModal({ onClose }: { onClose: () => void }) {
  const qc = useQueryClient();
  const {
  register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<OnboardForm>({
    resolver: zodResolver(onboardSchema),
    defaultValues: {
      defaultLocale: 'en',
 timeZoneId: 'UTC',
  defaultSiteName: 'Main',
    },
  });

  const mutation = useMutation({
    mutationFn: (data: OnboardForm) => adminTenantsApi.onboard(data),
    onSuccess: (result) => {
 toast.success(`Tenant "${result.tenantId}" provisioned. Admin invite sent to ${result.adminEmail}.`);
      void qc.invalidateQueries({ queryKey: ['admin-tenants'] });
      onClose();
    },
    onError: (err) =>
      toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Onboarding failed.'),
  });

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="card mx-4 w-full max-w-lg space-y-4 overflow-y-auto max-h-screen">
<div className="flex items-center justify-between">
    <h3 className="text-base font-semibold text-slate-900">Onboard New Tenant</h3>
          <button onClick={onClose} className="text-slate-400 hover:text-slate-600">✕</button>
        </div>
        <form onSubmit={handleSubmit((v) => mutation.mutate(v))} className="space-y-4">
    <fieldset className="space-y-3">
       <legend className="text-xs font-semibold uppercase tracking-wide text-slate-500">Tenant</legend>
            <div className="grid grid-cols-2 gap-3">
         <div>
      <label className="form-label">Slug</label>
        <input className="form-input mt-1 font-mono" {...register('slug')} placeholder="acme-corp" />
      {errors.slug && <p className="form-error">{errors.slug.message}</p>}
      </div>
   <div>
            <label className="form-label">Display Name</label>
  <input className="form-input mt-1" {...register('displayName')} placeholder="Acme Corp" />
         {errors.displayName && <p className="form-error">{errors.displayName.message}</p>}
     </div>
       </div>
    <div className="grid grid-cols-2 gap-3">
              <div>
    <label className="form-label">Default Locale</label>
             <input className="form-input mt-1 font-mono" {...register('defaultLocale')} placeholder="en" />
                {errors.defaultLocale && <p className="form-error">{errors.defaultLocale.message}</p>}
              </div>
              <div>
      <label className="form-label">Timezone</label>
          <input className="form-input mt-1 font-mono" {...register('timeZoneId')} placeholder="UTC" />
         {errors.timeZoneId && <p className="form-error">{errors.timeZoneId.message}</p>}
    </div>
    </div>
      <div>
      <label className="form-label">Default Site Name</label>
              <input className="form-input mt-1" {...register('defaultSiteName')} placeholder="Main" />
       {errors.defaultSiteName && <p className="form-error">{errors.defaultSiteName.message}</p>}
     </div>
          </fieldset>

    <fieldset className="space-y-3">
     <legend className="text-xs font-semibold uppercase tracking-wide text-slate-500">Admin User</legend>
            <div>
 <label className="form-label">Email</label>
    <input className="form-input mt-1" type="email" {...register('adminEmail')} placeholder="admin@acme.com" />
    {errors.adminEmail && <p className="form-error">{errors.adminEmail.message}</p>}
         </div>
     <div>
           <label className="form-label">Display Name</label>
   <input className="form-input mt-1" {...register('adminDisplayName')} placeholder="Alice Admin" />
              {errors.adminDisplayName && <p className="form-error">{errors.adminDisplayName.message}</p>}
            </div>
   </fieldset>

          <div className="flex justify-end gap-3">
    <button type="button" onClick={onClose} className="btn-secondary">Cancel</button>
        <button type="submit" disabled={isSubmitting} className="btn-primary">
           {isSubmitting ? 'Provisioning…' : 'Onboard Tenant'}
      </button>
  </div>
        </form>
      </div>
    </div>
  );
}

// ─── Page ─────────────────────────────────────────────────────────────────────

export default function TenantsPage() {
  const [showOnboard, setShowOnboard] = useState(false);
  const [page, setPage] = useState(1);

  const { data, isLoading } = useQuery({
    queryKey: ['admin-tenants', page],
    queryFn: () => adminTenantsApi.list({ pageNumber: page, pageSize: 20 }),
  });

  return (
    <div className="space-y-6">
      {/* Header */}
    <div className="flex items-center justify-between">
      <div>
   <h1 className="text-2xl font-bold text-slate-900">Tenants</h1>
    <p className="mt-1 text-sm text-slate-500">System-level tenant management.</p>
     </div>
        <button onClick={() => setShowOnboard(true)} className="btn-primary">
       <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
          </svg>
 Onboard Tenant
        </button>
      </div>

      {/* Table */}
      <div className="card overflow-hidden p-0">
        {isLoading ? (
    <div className="space-y-px">
            {Array.from({ length: 5 }).map((_, i) => (
       <div key={i} className="h-14 animate-pulse bg-slate-50" />
            ))}
</div>
   ) : (
        <table className="w-full text-sm">
         <thead className="border-b border-slate-100 bg-slate-50">
      <tr>
    <th className="px-6 py-3 text-left font-semibold text-slate-700">Tenant</th>
   <th className="px-6 py-3 text-left font-semibold text-slate-700">Status</th>
          <th className="px-6 py-3 text-left font-semibold text-slate-700">Created</th>
         <th className="px-6 py-3" />
           </tr>
   </thead>
            <tbody className="divide-y divide-slate-100">
  {(data?.items ?? []).map((t: TenantListItem) => (
                <tr key={t.id} className="hover:bg-slate-50">
     <td className="px-6 py-4">
     <p className="font-medium text-slate-900">{t.displayName}</p>
 <p className="font-mono text-xs text-slate-400">{t.slug}</p>
 </td>
         <td className="px-6 py-4">
        <span className={STATUS_BADGE[t.status] ?? 'badge-slate'}>{t.status}</span>
  </td>
  <td className="px-6 py-4 text-slate-400">
        {formatDistanceToNow(new Date(t.createdAt), { addSuffix: true })}
            </td>
   <td className="px-6 py-4 text-right">
  <Link
    to={`/tenants/${t.id}`}
         className="text-xs text-brand-600 hover:underline"
     >
             Manage →
       </Link>
   </td>
 </tr>
     ))}
            </tbody>
          </table>
        )}
      </div>

      {/* Pagination */}
      {data && data.totalPages > 1 && (
        <div className="flex justify-end gap-2">
       <button onClick={() => setPage((p) => p - 1)} disabled={page === 1} className="btn-secondary">Previous</button>
          <button onClick={() => setPage((p) => p + 1)} disabled={page === data.totalPages} className="btn-secondary">Next</button>
</div>
 )}

    {showOnboard && <OnboardModal onClose={() => setShowOnboard(false)} />}
    </div>
  );
}
