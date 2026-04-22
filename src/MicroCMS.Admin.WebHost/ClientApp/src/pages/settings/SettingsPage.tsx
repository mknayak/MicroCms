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
  name: z.string().min(1, 'Organization name is required'),
  timezone: z.string().min(1, 'Timezone is required'),
  defaultLocale: z.string().min(2, 'Default locale is required'),
  locales: z.string().min(2, 'At least one locale required'),
  aiEnabled: z.boolean(),
});

type SettingsForm = z.infer<typeof settingsSchema>;

const TIMEZONES = [
  'UTC',
  'America/New_York',
  'America/Chicago',
  'America/Denver',
  'America/Los_Angeles',
  'Europe/London',
  'Europe/Paris',
  'Europe/Berlin',
  'Asia/Tokyo',
  'Asia/Shanghai',
  'Asia/Kolkata',
  'Australia/Sydney',
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
    defaultValues: {
      name: '',
      timezone: 'UTC',
      defaultLocale: 'en',
      locales: 'en',
      aiEnabled: false,
    },
  });

  useEffect(() => {
    if (tenant) {
      reset({
        name: tenant.name,
        timezone: tenant.timezone,
        defaultLocale: tenant.defaultLocale,
        locales: tenant.locales.join(', '),
        aiEnabled: tenant.aiEnabled,
      });
    }
  }, [tenant, reset]);

  const mutation = useMutation({
    mutationFn: (data: SettingsForm) =>
      tenantsApi.update({
        name: data.name,
        timezone: data.timezone,
        defaultLocale: data.defaultLocale,
        locales: data.locales.split(',').map((l) => l.trim()).filter(Boolean),
        aiEnabled: data.aiEnabled,
      }),
    onSuccess: () => {
      toast.success('Settings saved.');
      void qc.invalidateQueries({ queryKey: ['tenant'] });
    },
    onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Save failed.'),
  });

  const handleLogoUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file) return;
    try {
      await tenantsApi.uploadLogo(file);
      toast.success('Logo uploaded.');
      void qc.invalidateQueries({ queryKey: ['tenant'] });
    } catch (err) {
      toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Upload failed.');
    }
  };

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
            <input className="form-input mt-1" {...register('name')} />
            {errors.name && <p className="form-error">{errors.name.message}</p>}
          </div>

          <div>
            <label className="form-label">Logo</label>
            <div className="mt-1 flex items-center gap-4">
              {tenant?.logoUrl && (
                <img src={tenant.logoUrl} alt="Logo" className="h-12 w-12 rounded-lg object-cover" />
              )}
              <label className="btn-secondary cursor-pointer">
                Upload Logo
                <input type="file" accept="image/*" className="sr-only" onChange={handleLogoUpload} />
              </label>
            </div>
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="form-label">Subdomain</label>
              <input
                className="form-input mt-1 bg-slate-50 font-mono"
                value={tenant?.subdomain ?? ''}
                readOnly
              />
              <p className="form-error mt-0.5 text-slate-400">Cannot be changed</p>
            </div>
            <div>
              <label className="form-label">Plan</label>
              <input className="form-input mt-1 bg-slate-50 capitalize" value={tenant?.plan ?? ''} readOnly />
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
              <select className="form-input mt-1" {...register('timezone')}>
                {TIMEZONES.map((tz) => <option key={tz} value={tz}>{tz}</option>)}
              </select>
            </div>
          </div>

          <div>
            <label className="form-label">Enabled Locales (comma-separated)</label>
            <input className="form-input mt-1 font-mono" {...register('locales')} placeholder="en, de, fr" />
            {errors.locales && <p className="form-error">{errors.locales.message}</p>}
            <p className="mt-1 text-xs text-slate-400">Example: en, de, fr, es</p>
          </div>
        </div>

        {/* AI */}
        <div className="card space-y-4">
          <h2 className="text-base font-semibold text-slate-900">AI Features</h2>
          <label className="flex items-start gap-3">
            <input
              type="checkbox"
              className="mt-0.5 h-4 w-4 rounded border-slate-300 text-brand-600"
              {...register('aiEnabled')}
            />
            <div>
              <p className="text-sm font-medium text-slate-900">Enable AI Co-pilot</p>
              <p className="text-xs text-slate-500">
                Allow editors to use AI-assisted content creation, SEO suggestions, and smart tagging.
              </p>
            </div>
          </label>
        </div>

        {/* Actions */}
        <div className="flex justify-end gap-3">
          <button type="button" onClick={() => reset()} disabled={!isDirty} className="btn-secondary">
            Discard Changes
          </button>
          <button type="submit" disabled={isSubmitting || !isDirty} className="btn-primary">
            {isSubmitting ? 'Saving…' : 'Save Settings'}
          </button>
        </div>
      </form>
    </div>
  );
}
