import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { tenantsApi } from '@/api/tenants';
import { apiClientsApi } from '@/api/apiClients';
import { useSite } from '@/contexts/SiteContext';
import { ApiError } from '@/api/client';
import type { ApiClientDto, ApiClientCreatedDto, ApiKeyType } from '@/types';

// ─── Schema ───────────────────────────────────────────────────────────────────

const settingsSchema = z.object({
  displayName: z.string().min(1, 'Organization name is required'),
  timeZoneId: z.string().min(1, 'Timezone is required'),
  defaultLocale: z.string().min(2, 'Default locale is required'),
  aiEnabled: z.boolean(),
});

type SettingsForm = z.infer<typeof settingsSchema>;

const createKeySchema = z.object({
  name: z.string().min(1, 'Name is required'),
  keyType: z.enum(['Delivery', 'Management', 'Preview']),
});
type CreateKeyForm = z.infer<typeof createKeySchema>;

const TIMEZONES = [
  'UTC', 'America/New_York', 'America/Chicago', 'America/Denver', 'America/Los_Angeles',
  'Europe/London', 'Europe/Paris', 'Europe/Berlin', 'Asia/Tokyo', 'Asia/Shanghai',
  'Asia/Kolkata', 'Australia/Sydney',
];

const LOCALES = ['en', 'de', 'fr', 'es', 'pt', 'it', 'nl', 'ja', 'zh', 'ko', 'ar'];

const KEY_TYPE_COLORS: Record<ApiKeyType, string> = {
  Delivery:   'bg-green-100 text-green-800',
  Management: 'bg-amber-100 text-amber-800',
  Preview:    'bg-purple-100 text-purple-800',
};

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

// ─── Raw key reveal modal ─────────────────────────────────────────────────────

function RawKeyModal({ result, onClose }: { result: ApiClientCreatedDto; onClose: () => void }) {
  const [revealed, setRevealed] = useState(false);
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
      <div className="w-full max-w-md rounded-xl bg-white shadow-2xl">
        <div className="border-b border-slate-100 px-6 py-4">
       <h2 className="text-base font-semibold text-slate-900">API Key Created</h2>
      <p className="mt-0.5 text-sm text-slate-500">
            Copy your key now — it will <strong>not</strong> be shown again.
       </p>
        </div>
   <div className="space-y-4 px-6 py-5">
          <div>
            <label className="form-label">Key Name</label>
     <p className="mt-1 text-sm font-medium text-slate-800">{result.client.name}</p>
          </div>
          <div>
  <label className="form-label">Type</label>
      <span className={`mt-1 inline-block rounded-full px-2.5 py-0.5 text-xs font-semibold ${KEY_TYPE_COLORS[result.client.keyType]}`}>
     {result.client.keyType}
       </span>
       </div>
     <div>
   <label className="form-label mb-1">Raw Key</label>
       <div className="flex items-center gap-2 rounded-lg border border-amber-300 bg-amber-50 p-3">
  <code className={`flex-1 break-all font-mono text-xs text-amber-900 ${revealed ? '' : 'select-none blur-sm'}`}>
         {result.rawKey}
         </code>
              <div className="flex flex-col gap-1">
        <button
      type="button"
     onClick={() => setRevealed((v) => !v)}
       className="rounded border border-amber-300 bg-white px-2 py-1 text-xs text-amber-700 hover:bg-amber-100"
      >
    {revealed ? 'Hide' : 'Show'}
     </button>
     <CopyButton value={result.rawKey} />
</div>
</div>
          </div>
        </div>
        <div className="flex justify-end border-t border-slate-100 px-6 py-4">
          <button onClick={onClose} className="btn-primary">Done</button>
        </div>
      </div>
    </div>
  );
}

// ─── API Keys tab ─────────────────────────────────────────────────────────────

function ApiKeysTab() {
  const qc = useQueryClient();
  const { selectedSiteId } = useSite();
  const siteId = selectedSiteId ?? '';

  const [showCreate, setShowCreate] = useState(false);
  const [rawKeyResult, setRawKeyResult] = useState<ApiClientCreatedDto | null>(null);

  const { data: clients, isLoading } = useQuery({
    queryKey: ['api-clients', siteId],
    queryFn: () => apiClientsApi.list(siteId),
    enabled: !!siteId,
  });

  const createForm = useForm<CreateKeyForm>({
    resolver: zodResolver(createKeySchema),
    defaultValues: { name: '', keyType: 'Delivery' },
  });

  const createMutation = useMutation({
  mutationFn: (data: CreateKeyForm) =>
   apiClientsApi.create({ siteId, ...data }),
    onSuccess: (result) => {
      setRawKeyResult(result);
      setShowCreate(false);
   createForm.reset();
      void qc.invalidateQueries({ queryKey: ['api-clients', siteId] });
    },
    onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Create failed.'),
  });

  const revokeMutation = useMutation({
    mutationFn: (id: string) => apiClientsApi.revoke(id),
  onSuccess: () => {
      toast.success('Key revoked.');
      void qc.invalidateQueries({ queryKey: ['api-clients', siteId] });
  },
    onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Revoke failed.'),
  });

  const regenMutation = useMutation({
    mutationFn: (id: string) => apiClientsApi.regenerate(id),
    onSuccess: (result) => {
      setRawKeyResult(result);
  void qc.invalidateQueries({ queryKey: ['api-clients', siteId] });
  },
    onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Regenerate failed.'),
  });

  if (!siteId) {
    return <p className="text-sm text-slate-400">Select a site from the top bar to manage its API keys.</p>;
  }

  return (
 <>
      {rawKeyResult && (
        <RawKeyModal result={rawKeyResult} onClose={() => setRawKeyResult(null)} />
    )}

      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <div>
        <h2 className="text-base font-semibold text-slate-900">API Keys</h2>
          <p className="text-sm text-slate-500">
  Keys authenticate requests to the Delivery WebHost. Pass them in the{' '}
   <code className="rounded bg-slate-100 px-1 py-0.5 text-xs">X-Api-Key</code> header.
          </p>
    </div>
    <button className="btn-primary" onClick={() => setShowCreate((v) => !v)}>
     {showCreate ? 'Cancel' : '+ New Key'}
  </button>
 </div>

        {/* Create form */}
      {showCreate && (
          <form
          onSubmit={createForm.handleSubmit((v) => createMutation.mutate(v))}
  className="rounded-lg border border-brand-200 bg-brand-50 p-4 space-y-3"
     >
            <h3 className="text-sm font-semibold text-slate-800">New API Key</h3>
     <div className="grid grid-cols-2 gap-3">
   <div>
       <label className="form-label">Name</label>
                <input className="form-input mt-1" placeholder="e.g. Next.js frontend" {...createForm.register('name')} />
 {createForm.formState.errors.name && (
   <p className="form-error">{createForm.formState.errors.name.message}</p>
         )}
       </div>
  <div>
      <label className="form-label">Type</label>
   <select className="form-input mt-1" {...createForm.register('keyType')}>
      <option value="Delivery">Delivery — read-only published content</option>
     <option value="Preview">Preview — read draft content</option>
           <option value="Management">Management — full read/write</option>
                </select>
       </div>
   </div>
            <div className="flex justify-end">
              <button type="submit" disabled={createMutation.isPending} className="btn-primary">
     {createMutation.isPending ? 'Creating…' : 'Create Key'}
       </button>
       </div>
          </form>
    )}

        {/* Keys list */}
        {isLoading ? (
          <div className="space-y-2">
            {Array.from({ length: 3 }).map((_, i) => (
   <div key={i} className="h-14 animate-pulse rounded-lg bg-slate-100" />
         ))}
          </div>
 ) : (clients ?? []).length === 0 ? (
          <p className="rounded-lg border border-dashed border-slate-200 py-8 text-center text-sm text-slate-400">
            No API keys yet. Create one to connect your frontend or CI pipeline.
    </p>
        ) : (
          <div className="divide-y divide-slate-100 rounded-lg border border-slate-200 bg-white">
 {(clients ?? []).map((client: ApiClientDto) => (
   <div key={client.id} className="flex items-center gap-4 px-4 py-3">
      <div className="flex-1 min-w-0">
           <div className="flex items-center gap-2">
        <span className="text-sm font-semibold text-slate-800 truncate">{client.name}</span>
                    <span className={`flex-shrink-0 rounded-full px-2 py-0.5 text-[10px] font-bold ${KEY_TYPE_COLORS[client.keyType]}`}>
      {client.keyType}
      </span>
  </div>
        <div className="mt-0.5 flex flex-wrap items-center gap-3 text-xs text-slate-400">
               <span>
           ID: <code className="font-mono">{client.id}</code>
     </span>
 <CopyButton value={client.id} label="Copy ID" />
    {client.scopes.length > 0 && (
          <span>Scopes: {client.scopes.join(', ')}</span>
     )}
               {client.expiresAt && (
 <span>Expires: {new Date(client.expiresAt).toLocaleDateString()}</span>
     )}
 <span>Created: {new Date(client.createdAt).toLocaleDateString()}</span>
         </div>
         </div>
      <div className="flex flex-shrink-0 items-center gap-2">
                  <button
        className="rounded border border-slate-200 bg-white px-2 py-1 text-xs font-medium text-slate-600 hover:bg-slate-50"
           disabled={regenMutation.isPending}
    onClick={() => {
     if (confirm(`Regenerate key for "${client.name}"? The current key will stop working immediately.`)) {
     regenMutation.mutate(client.id);
              }
            }}
    >
   Regenerate
           </button>
          <button
          className="rounded border border-red-200 bg-white px-2 py-1 text-xs font-medium text-red-600 hover:bg-red-50"
   disabled={revokeMutation.isPending}
 onClick={() => {
    if (confirm(`Revoke "${client.name}"? This cannot be undone.`)) {
          revokeMutation.mutate(client.id);
    }
    }}
     >
         Revoke
          </button>
        </div>
        </div>
          ))}
          </div>
      )}
      </div>
    </>
  );
}

// ─── Page ─────────────────────────────────────────────────────────────────────

type Tab = 'general' | 'apikeys';

export default function SettingsPage() {
  const qc = useQueryClient();
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
        {Array.from({ length: 4 }).map((_, i) => <div key={i} className="card h-24 animate-pulse" />)}
      </div>
    );
  }

  const TABS: { id: Tab; label: string }[] = [
    { id: 'general',  label: 'General' },
    { id: 'apikeys',  label: 'API Keys' },
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

   {/* Sites — with ID column */}
      {tenant && tenant.sites.length > 0 && (
            <div className="card space-y-3">
    <h2 className="text-base font-semibold text-slate-900">Sites</h2>
   <p className="text-xs text-slate-400">
           Use the <span className="font-semibold">Site ID</span> as the{' '}
    <code className="rounded bg-slate-100 px-1">siteId</code> query parameter in Delivery API calls.
              </p>
        <div className="overflow-x-auto">
     <table className="w-full text-sm">
      <thead className="border-b border-slate-100">
         <tr>
           <th className="pb-2 text-left font-semibold text-slate-600">Name</th>
           <th className="pb-2 text-left font-semibold text-slate-600">Handle</th>
   <th className="pb-2 text-left font-semibold text-slate-600">Locale</th>
         <th className="pb-2 text-left font-semibold text-slate-600">Site ID</th>
         </tr>
      </thead>
      <tbody className="divide-y divide-slate-100">
 {tenant.sites.map((site) => (
         <tr key={site.id}>
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
   Discard Changes
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
