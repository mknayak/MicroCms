import { useEffect, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { tenantsApi } from '@/api/tenants';
import { useSite } from '@/contexts/SiteContext';
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

// ─── Copy button ──────────────────────────────────────────────────────────────

function CopyButton({ value, label = 'Copy' }: { value: string; label?: string }) {
  const [copied, setCopied] = useState(false);
  const copy = () => {
    void navigator.clipboard.writeText(value);
    setCopied(true);
    setTimeout(() => setCopied(false), 2000);
  };
  return (
    <button
      type="button"
      onClick={copy}
      className="inline-flex items-center gap-1 rounded border border-slate-200 bg-white px-2 py-1 text-xs font-medium text-slate-600 hover:bg-slate-50"
    >
      {copied ? (
        <>
          <svg className="h-3.5 w-3.5 text-green-500" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
            <path strokeLinecap="round" strokeLinejoin="round" d="M5 13l4 4L19 7" />
          </svg>
          Copied
        </>
      ) : (
        <>
          <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
            <path strokeLinecap="round" strokeLinejoin="round" d="M8 16H6a2 2 0 01-2-2V6a2 2 0 012-2h8a2 2 0 012 2v2m-4 10h6a2 2 0 002-2v-8a2 2 0 00-2-2h-6a2 2 0 00-2 2v8a2 2 0 002 2z" />
          </svg>
          {label}
        </>
      )}
    </button>
  );
}

// ─── API Keys tab ─────────────────────────────────────────────────────────────

function ApiKeysTab() {
  const { selectedSiteId, selectedSite } = useSite();
  const navigate = useNavigate();

  if (!selectedSiteId) {
    return (
      <p className="text-sm text-slate-400">Select a site from the top bar to manage its API keys.</p>
    );
  }

  return (
    <div className="space-y-4">
      <div className="flex items-center justify-between">
        <div>
          <h2 className="text-base font-semibold text-slate-900">API Keys</h2>
          <p className="text-sm text-slate-500">
            Keys are scoped to <strong>{selectedSite?.name ?? 'this site'}</strong>.
            Manage them in the dedicated Site Settings page.
          </p>
        </div>
        <button
          type="button"
          className="btn-primary"
          onClick={() => navigate(`/sites/${selectedSiteId}`)}
        >
          Manage API Keys →
        </button>
      </div>
      <p className="rounded-lg border border-dashed border-slate-200 py-6 text-center text-sm text-slate-400">
        API key management has moved to{' '}
        <button
          type="button"
          className="text-brand-600 underline"
          onClick={() => navigate(`/sites/${selectedSiteId}`)}
        >
          Site Settings → API Keys
        </button>
        .
      </p>
    </div>
  );
}

// ─── Page ─────────────────────────────────────────────────────────────────────

type Tab = 'general' | 'apikeys';

export default function SettingsPage() {
  const qc = useQueryClient();
  const navigate = useNavigate();
  const [tab, setTab] = useState<Tab>('general');

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
        {Array.from({ length: 4 }).map((_, i) => (
          <div key={i} className="card h-24 animate-pulse" />
        ))}
      </div>
    );
  }

  const TABS: { id: Tab; label: string }[] = [
    { id: 'general', label: 'General' },
    { id: 'apikeys', label: 'API Keys' },
  ];

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-slate-900">Settings</h1>
        <p className="mt-1 text-sm text-slate-500">Manage your workspace and delivery credentials.</p>
      </div>

      {/* Tab bar */}
      <div className="flex border-b border-slate-200">
        {TABS.map((t) => (
          <button
            key={t.id}
            type="button"
            onClick={() => setTab(t.id)}
            className={`mr-6 border-b-2 pb-3 text-sm font-medium transition-colors ${
              tab === t.id
                ? 'border-brand-600 text-brand-600'
                : 'border-transparent text-slate-500 hover:text-slate-700'
            }`}
          >
            {t.label}
          </button>
        ))}
      </div>

      {/* ── General tab ── */}
      {tab === 'general' && (
        <form onSubmit={handleSubmit((v) => mutation.mutate(v))} className="space-y-6">
          {/* Organization */}
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
              <input
                type="checkbox"
                className="mt-0.5 h-4 w-4 rounded border-slate-300 text-brand-600"
                {...register('aiEnabled')}
              />
              <div>
                <p className="text-sm font-medium text-slate-900">Enable AI Co-pilot</p>
                <p className="text-xs text-slate-500">
                  AI-assisted content creation, SEO suggestions, and smart tagging.
                </p>
              </div>
            </label>
          </div>

          {/* Sites — clickable rows */}
          {tenant && tenant.sites.length > 0 && (
            <div className="card space-y-3">
              <h2 className="text-base font-semibold text-slate-900">Sites</h2>
              <p className="text-xs text-slate-400">
                Click a site to manage its settings, feature flags, CORS, and API keys.
              </p>
              <div className="overflow-x-auto">
                <table className="w-full text-sm">
                  <thead className="border-b border-slate-100">
                    <tr>
                      <th className="pb-2 text-left font-semibold text-slate-600">Name</th>
                      <th className="pb-2 text-left font-semibold text-slate-600">Handle</th>
                      <th className="pb-2 text-left font-semibold text-slate-600">Locale</th>
                      <th className="pb-2 text-left font-semibold text-slate-600">Site ID</th>
                      <th className="pb-2" />
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-100">
                    {tenant.sites.map((site) => (
                      <tr
                        key={site.id}
                        className="cursor-pointer hover:bg-slate-50 transition-colors"
                        onClick={() => navigate(`/sites/${site.id}`)}
                      >
                        <td className="py-2 font-medium text-slate-800">{site.name}</td>
                        <td className="py-2 font-mono text-slate-500">{site.handle}</td>
                        <td className="py-2 text-slate-500">{site.defaultLocale}</td>
                        <td className="py-2">
                          <div className="flex items-center gap-2">
                            <code className="rounded bg-slate-100 px-1.5 py-0.5 font-mono text-xs text-slate-700">
                              {site.id}
                            </code>
                            <CopyButton value={site.id} />
                          </div>
                        </td>
                        <td className="py-2 text-right">
                          <span className="text-xs font-medium text-brand-600">Configure →</span>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          )}

          {/* Actions */}
          <div className="flex justify-end gap-3">
            <button type="button" onClick={() => reset()} disabled={!isDirty} className="btn-secondary">
              Discard
            </button>
            <button type="submit" disabled={isSubmitting || !isDirty} className="btn-primary">
              {isSubmitting ? 'Saving…' : 'Save Settings'}
            </button>
          </div>
        </form>
      )}

      {/* ── API Keys tab ── */}
      {tab === 'apikeys' && <ApiKeysTab />}
    </div>
  );
}
