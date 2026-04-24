import { useEffect } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { tenantsApi } from '@/api/tenants';
import { ApiError } from '@/api/client';

// ─── Schema ───────────────────────────────────────────────────────────────────

const settingsSchema = z.object({
  displayName: z.string().min(1, 'Organization name is required'),
  timeZoneId: z.string().min(1, 'Timezone is required'),
  defaultLocale: z.string().min(2, 'Default locale is required'),
  aiEnabled: z.boolean(),
});

type SettingsForm = z.infer<typeof settingsSchema>;

const TIMEZONES = [
  'UTC', 'America/New_York', 'America/Chicago', 'America/Denver', 'America/Los_Angeles',
  'Europe/London', 'Europe/Paris', 'Europe/Berlin', 'Asia/Tokyo', 'Asia/Shanghai',
  'Asia/Kolkata', 'Australia/Sydney',
];

const LOCALES = ['en', 'de', 'fr', 'es', 'pt', 'it', 'nl', 'ja', 'zh', 'ko', 'ar'];

// ─── Page ─────────────────────────────────────────────────────────────────────

export default function SettingsPage() {
  const qc = useQueryClient();

  const { data: tenant, isLoading } = useQuery({
    queryKey: ['tenant'],
    queryFn: tenantsApi.getCurrent,
  });

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting, isDirty },
  } = useForm<SettingsForm>({
    resolver: zodResolver(settingsSchema),
    defaultValues: { displayName: '', timeZoneId: 'UTC', defaultLocale: 'en', aiEnabled: false },
  });

  useEffect(() => {
    if (tenant) {
      reset({
        displayName: tenant.displayName,
        timeZoneId: tenant.timeZoneId,
        defaultLocale: tenant.defaultLocale,
        aiEnabled: tenant.aiEnabled,
      });
    }
  }, [tenant, reset]);

  const mutation = useMutation({
    mutationFn: (data: SettingsForm) =>
      tenantsApi.update(tenant!.id, {
        displayName: data.displayName,
        timeZoneId: data.timeZoneId,
        defaultLocale: data.defaultLocale,
        aiEnabled: data.aiEnabled,
        logoUrl: tenant?.logoUrl,
      }),
    onSuccess: () => {
      toast.success('Settings saved.');
      void qc.invalidateQueries({ queryKey: ['tenant'] });
    },
    onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Save failed.'),
  });

  if (isLoading) {
    return (
      <div className="space-y-4">
        {Array.from({ length: 4 }).map((_, i) => <div key={i} className="card h-24 animate-pulse" />)}
      </div>
    );
  }

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-slate-900">Tenant Settings</h1>
        <p className="mt-1 text-sm text-slate-500">Configure your workspace preferences.</p>
      </div>

      <form onSubmit={handleSubmit((v) => mutation.mutate(v))} className="space-y-6">
        {/* General */}
        <div className="card space-y-4">
          <h2 className="text-base font-semibold text-slate-900">General</h2>

          <div>
            <label className="form-label">Organization Name</label>
            <input className="form-input mt-1" {...register('displayName')} />
            {errors.displayName && <p className="form-error">{errors.displayName.message}</p>}
          </div>

          {tenant?.logoUrl && (
            <div>
              <label className="form-label">Logo</label>
              <img src={tenant.logoUrl} alt="Logo" className="mt-1 h-12 w-12 rounded-lg object-cover" />
            </div>
          )}

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="form-label">Slug</label>
              <input className="form-input mt-1 bg-slate-50 font-mono" value={tenant?.slug ?? ''} readOnly />
              <p className="mt-0.5 text-xs text-slate-400">Cannot be changed</p>
            </div>
            <div>
              <label className="form-label">Status</label>
              <input className="form-input mt-1 bg-slate-50 capitalize" value={tenant?.status ?? ''} readOnly />
            </div>
          </div>
        </div>

        {/* Localisation */}
        <div className="card space-y-4">
          <h2 className="text-base font-semibold text-slate-900">Localisation</h2>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="form-label">Default Locale</label>
              <select className="form-input mt-1" {...register('defaultLocale')}>
                {LOCALES.map((l) => <option key={l} value={l}>{l.toUpperCase()}</option>)}
              </select>
              {errors.defaultLocale && <p className="form-error">{errors.defaultLocale.message}</p>}
            </div>
            <div>
              <label className="form-label">Timezone</label>
              <select className="form-input mt-1" {...register('timeZoneId')}>
                {TIMEZONES.map((tz) => <option key={tz} value={tz}>{tz}</option>)}
              </select>
              {errors.timeZoneId && <p className="form-error">{errors.timeZoneId.message}</p>}
            </div>
          </div>
        </div>

        {/* AI */}
        <div className="card space-y-4">
          <h2 className="text-base font-semibold text-slate-900">AI Features</h2>
          <label className="flex items-start gap-3">
            <input type="checkbox" className="mt-0.5 h-4 w-4 rounded border-slate-300 text-brand-600" {...register('aiEnabled')} />
            <div>
              <p className="text-sm font-medium text-slate-900">Enable AI Co-pilot</p>
              <p className="text-xs text-slate-500">AI-assisted content creation, SEO suggestions, and smart tagging.</p>
            </div>
          </label>
        </div>

        {/* Sites (read-only) */}
        {tenant && tenant.sites.length > 0 && (
          <div className="card space-y-3">
            <h2 className="text-base font-semibold text-slate-900">Sites</h2>
            <table className="w-full text-sm">
              <thead className="border-b border-slate-100">
                <tr>
                  <th className="pb-2 text-left font-semibold text-slate-600">Name</th>
                  <th className="pb-2 text-left font-semibold text-slate-600">Handle</th>
                  <th className="pb-2 text-left font-semibold text-slate-600">Locale</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {tenant.sites.map((site) => (
                  <tr key={site.id}>
                    <td className="py-2 font-medium text-slate-800">{site.name}</td>
                    <td className="py-2 font-mono text-slate-500">{site.handle}</td>
                    <td className="py-2 text-slate-500">{site.defaultLocale}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        {/* Actions */}
        <div className="flex justify-end gap-3">
          <button type="button" onClick={() => reset()} disabled={!isDirty} className="btn-secondary">Discard Changes</button>
          <button type="submit" disabled={isSubmitting || !isDirty} className="btn-primary">
            {isSubmitting ? 'Saving…' : 'Save Settings'}
          </button>
        </div>
      </form>
    </div>
  );
}
