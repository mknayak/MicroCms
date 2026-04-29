import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { sitesApi } from '@/api/sites';
import { apiClientsApi } from '@/api/apiClients';
import { ApiError } from '@/api/client';
import type { ApiClientDto, ApiClientCreatedDto, ApiKeyType } from '@/types';

// ─── Schemas ──────────────────────────────────────────────────────────────────

const generalSchema = z.object({
  name: z.string().min(1, 'Name is required').max(100),
  defaultLocale: z.string().min(2, 'Locale is required'),
  customDomain: z.string().optional(),
});

const featuresSchema = z.object({
  previewUrlTemplate: z.string().optional(),
  versioningEnabled: z.boolean(),
  workflowEnabled: z.boolean(),
  schedulingEnabled: z.boolean(),
  previewEnabled: z.boolean(),
  aiEnabled: z.boolean(),
});

const corsSchema = z.object({
  corsOriginsRaw: z.string(),
  localesRaw: z.string().min(2, 'At least one locale is required'),
});

const createKeySchema = z.object({
  name: z.string().min(1, 'Name is required'),
  keyType: z.enum(['Delivery', 'Management', 'Preview']),
});

type GeneralForm = z.infer<typeof generalSchema>;
type FeaturesForm = z.infer<typeof featuresSchema>;
type CorsForm = z.infer<typeof corsSchema>;
type CreateKeyForm = z.infer<typeof createKeySchema>;

// ─── Constants ────────────────────────────────────────────────────────────────

const LOCALES = ['en', 'de', 'fr', 'es', 'pt', 'it', 'nl', 'ja', 'zh', 'ko', 'ar'];

const KEY_TYPE_COLORS: Record<ApiKeyType, string> = {
  Delivery: 'bg-green-100 text-green-800',
  Management: 'bg-amber-100 text-amber-800',
  Preview: 'bg-purple-100 text-purple-800',
};

// ─── Utility components ───────────────────────────────────────────────────────

function CopyButton({ value, label = 'Copy' }: { value: string; label?: string }) {
  const [copied, setCopied] = useState(false);
  return (
    <button
      type="button"
      onClick={() => {
        void navigator.clipboard.writeText(value);
        setCopied(true);
        setTimeout(() => setCopied(false), 2000);
      }}
      className="inline-flex items-center gap-1 rounded border border-slate-200 bg-white px-2 py-1 text-xs font-medium text-slate-600 hover:bg-slate-50"
    >
      {copied ? '✓ Copied' : label}
    </button>
  );
}

function Toggle({
  label,
  description,
  checked,
  onChange,
}: {
  label: string;
  description?: string;
  checked: boolean;
  onChange: (v: boolean) => void;
}) {
  return (
    <label className="flex cursor-pointer items-start gap-3">
      <div className="relative mt-0.5 flex-shrink-0">
        <input
          type="checkbox"
          className="sr-only"
          checked={checked}
          onChange={(e) => onChange(e.target.checked)}
        />
        <div
          className={`h-5 w-9 rounded-full transition-colors ${checked ? 'bg-brand-600' : 'bg-slate-200'}`}
        />
        <div
          className={`absolute top-0.5 h-4 w-4 rounded-full bg-white shadow transition-transform ${checked ? 'translate-x-4' : 'translate-x-0.5'}`}
        />
      </div>
      <div>
        <p className="text-sm font-medium text-slate-900">{label}</p>
        {description && <p className="text-xs text-slate-500">{description}</p>}
      </div>
    </label>
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
          <div className="flex items-center gap-2 rounded-lg border border-amber-300 bg-amber-50 p-3">
            <code
              className={`flex-1 break-all font-mono text-xs text-amber-900 transition-all ${revealed ? '' : 'select-none blur-sm'}`}
            >
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
          <p className="text-xs text-slate-400">
            Type: <span className={`rounded-full px-2 py-0.5 text-[10px] font-bold ${KEY_TYPE_COLORS[result.client.keyType]}`}>{result.client.keyType}</span>
          </p>
        </div>
        <div className="flex justify-end border-t border-slate-100 px-6 py-4">
          <button onClick={onClose} className="btn-primary">Done — I've saved the key</button>
        </div>
      </div>
    </div>
  );
}

// ─── General tab ──────────────────────────────────────────────────────────────

function GeneralTab({ siteId }: { siteId: string }) {
  const qc = useQueryClient();
  const { data: site, isLoading } = useQuery({
    queryKey: ['site', siteId],
    queryFn: () => sitesApi.getById(siteId),
  });

  const { register, handleSubmit, reset, formState: { errors, isDirty, isSubmitting } } =
    useForm<GeneralForm>({ resolver: zodResolver(generalSchema) });

  useEffect(() => {
    if (site) reset({ name: site.name, defaultLocale: site.defaultLocale, customDomain: site.customDomain ?? '' });
  }, [site, reset]);

  const mutation = useMutation({
    mutationFn: (data: GeneralForm) =>
      sitesApi.update(siteId, { name: data.name, defaultLocale: data.defaultLocale, customDomain: data.customDomain || undefined }),
    onSuccess: (updated) => {
      toast.success('Site updated.');
      void qc.invalidateQueries({ queryKey: ['site', siteId] });
      void qc.invalidateQueries({ queryKey: ['current-tenant'] });
      reset({ name: updated.name, defaultLocale: updated.defaultLocale, customDomain: updated.customDomain ?? '' });
    },
    onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Save failed.'),
  });

  if (isLoading) return <div className="space-y-3">{Array.from({ length: 4 }).map((_, i) => <div key={i} className="h-10 animate-pulse rounded-lg bg-slate-100" />)}</div>;
  if (!site) return null;

  return (
    <form onSubmit={handleSubmit((v) => mutation.mutate(v))} className="space-y-6">
      <div className="card space-y-4">
        <h2 className="text-base font-semibold text-slate-900">Site Identity</h2>

        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="form-label">Display Name</label>
            <input className="form-input mt-1" {...register('name')} />
            {errors.name && <p className="form-error">{errors.name.message}</p>}
          </div>
          <div>
            <label className="form-label">Handle</label>
            <input
              className="form-input mt-1 bg-slate-50 font-mono"
              value={site.handle}
              readOnly
            />
            <p className="mt-0.5 text-xs text-slate-400">Cannot be changed after creation</p>
          </div>
        </div>

        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="form-label">Default Locale</label>
            <select className="form-input mt-1" {...register('defaultLocale')}>
              {LOCALES.map((l) => <option key={l} value={l}>{l.toUpperCase()}</option>)}
            </select>
            {errors.defaultLocale && <p className="form-error">{errors.defaultLocale.message}</p>}
          </div>
          <div>
            <label className="form-label">Status</label>
            <span className={`mt-2 inline-flex items-center rounded-full px-3 py-1 text-sm font-semibold ${site.isActive ? 'bg-green-100 text-green-800' : 'bg-slate-100 text-slate-600'}`}>
              {site.isActive ? 'Active' : 'Inactive'}
            </span>
          </div>
        </div>

        <div>
          <label className="form-label">Custom Domain</label>
          <input className="form-input mt-1 font-mono" {...register('customDomain')} placeholder="e.g. cms.acme.com" />
          <p className="mt-0.5 text-xs text-slate-400">Leave blank to remove the custom domain</p>
        </div>
      </div>

      <div className="card space-y-3">
        <h2 className="text-base font-semibold text-slate-900">Identifiers</h2>
        <div className="flex items-center gap-2">
          <span className="text-xs text-slate-500 w-24">Site ID</span>
          <code className="rounded bg-slate-100 px-2 py-0.5 font-mono text-xs text-slate-700">{site.id}</code>
          <CopyButton value={site.id} />
        </div>
        <div className="flex items-center gap-2">
          <span className="text-xs text-slate-500 w-24">Tenant ID</span>
          <code className="rounded bg-slate-100 px-2 py-0.5 font-mono text-xs text-slate-700">{site.tenantId}</code>
          <CopyButton value={site.tenantId} />
        </div>
      </div>

      {site.environments.length > 0 && (
        <div className="card space-y-3">
          <h2 className="text-base font-semibold text-slate-900">Environments</h2>
          <div className="divide-y divide-slate-100 rounded-lg border border-slate-200">
            {site.environments.map((env) => (
              <div key={env.type} className="flex items-center gap-4 px-4 py-3">
                <span className="w-28 text-sm font-medium text-slate-700">{env.type}</span>
                <a href={env.url} target="_blank" rel="noopener noreferrer" className="flex-1 font-mono text-xs text-brand-600 hover:underline">
                  {env.url}
                </a>
                <span className={`rounded-full px-2 py-0.5 text-[10px] font-bold ${env.isLive ? 'bg-green-100 text-green-800' : 'bg-slate-100 text-slate-500'}`}>
                  {env.isLive ? 'Live' : 'Not Live'}
                </span>
                <span className="text-xs text-slate-400">{env.sslStatus}</span>
              </div>
            ))}
          </div>
        </div>
      )}

      <div className="flex justify-end gap-3">
        <button type="button" onClick={() => reset()} disabled={!isDirty} className="btn-secondary">Discard</button>
        <button type="submit" disabled={isSubmitting || !isDirty} className="btn-primary">
          {isSubmitting ? 'Saving…' : 'Save Changes'}
        </button>
      </div>
    </form>
  );
}

// ─── Features tab ─────────────────────────────────────────────────────────────

function FeaturesTab({ siteId }: { siteId: string }) {
  const qc = useQueryClient();
  const { data: settings, isLoading } = useQuery({
    queryKey: ['site-settings', siteId],
    queryFn: () => sitesApi.getSettings(siteId),
  });

  const { register, handleSubmit, reset, control, formState: { isDirty, isSubmitting } } =
    useForm<FeaturesForm>({ resolver: zodResolver(featuresSchema) });

  useEffect(() => {
    if (settings) {
      reset({
        previewUrlTemplate: settings.previewUrlTemplate ?? '',
        versioningEnabled: settings.versioningEnabled,
        workflowEnabled: settings.workflowEnabled,
        schedulingEnabled: settings.schedulingEnabled,
        previewEnabled: settings.previewEnabled,
        aiEnabled: settings.aiEnabled,
      });
    }
  }, [settings, reset]);

  const mutation = useMutation({
    mutationFn: (data: FeaturesForm) =>
      sitesApi.updateSettings(siteId, {
        previewUrlTemplate: data.previewUrlTemplate || undefined,
        versioningEnabled: data.versioningEnabled,
        workflowEnabled: data.workflowEnabled,
        schedulingEnabled: data.schedulingEnabled,
        previewEnabled: data.previewEnabled,
        aiEnabled: data.aiEnabled,
        corsOrigins: settings?.corsOrigins ?? [],
        locales: settings?.locales ?? ['en'],
      }),
    onSuccess: () => {
      toast.success('Feature settings saved.');
      void qc.invalidateQueries({ queryKey: ['site-settings', siteId] });
    },
    onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Save failed.'),
  });

  if (isLoading) return <div className="space-y-3">{Array.from({ length: 5 }).map((_, i) => <div key={i} className="h-10 animate-pulse rounded-lg bg-slate-100" />)}</div>;

  const flags: Array<{ field: keyof FeaturesForm; label: string; description: string }> = [
    { field: 'versioningEnabled', label: 'Entry Versioning', description: 'Keep a history of all changes to entries.' },
    { field: 'workflowEnabled', label: 'Editorial Workflow', description: 'Require review / approval before publishing.' },
    { field: 'schedulingEnabled', label: 'Scheduled Publishing', description: 'Allow entries to be published at a future date.' },
    { field: 'previewEnabled', label: 'Preview Mode', description: 'Allow frontend to request draft content via the Preview API.' },
    { field: 'aiEnabled', label: 'AI Co-pilot', description: 'AI-assisted writing, SEO, and tagging for this site.' },
  ];

  return (
    <form onSubmit={handleSubmit((v) => mutation.mutate(v))} className="space-y-6">
      <div className="card space-y-5">
        <h2 className="text-base font-semibold text-slate-900">Feature Flags</h2>
        {flags.map(({ field, label, description }) => (
          <Controller
            key={field}
            name={field as keyof FeaturesForm}
            control={control}
            render={({ field: f }) => (
              <Toggle
                label={label}
                description={description}
                checked={f.value as boolean}
                onChange={f.onChange}
              />
            )}
          />
        ))}
      </div>

      <div className="card space-y-4">
        <h2 className="text-base font-semibold text-slate-900">Preview URL</h2>
        <div>
          <label className="form-label">Preview URL Template</label>
          <input
            className="form-input mt-1 font-mono"
            placeholder="https://your-site.com/api/preview?secret={token}&slug={slug}"
            {...register('previewUrlTemplate')}
          />
          <p className="mt-1 text-xs text-slate-400">
            Use <code className="rounded bg-slate-100 px-1">{'{token}'}</code> and{' '}
            <code className="rounded bg-slate-100 px-1">{'{slug}'}</code> as placeholders.
          </p>
        </div>
      </div>

      <div className="flex justify-end gap-3">
        <button type="button" onClick={() => { if (settings) reset({ previewUrlTemplate: settings.previewUrlTemplate ?? '', versioningEnabled: settings.versioningEnabled, workflowEnabled: settings.workflowEnabled, schedulingEnabled: settings.schedulingEnabled, previewEnabled: settings.previewEnabled, aiEnabled: settings.aiEnabled }); }} disabled={!isDirty} className="btn-secondary">Discard</button>
        <button type="submit" disabled={isSubmitting || !isDirty} className="btn-primary">
          {isSubmitting ? 'Saving…' : 'Save Features'}
        </button>
      </div>
    </form>
  );
}

// ─── CORS & Locales tab ───────────────────────────────────────────────────────

function CorsLocalesTab({ siteId }: { siteId: string }) {
  const qc = useQueryClient();
  const { data: settings, isLoading } = useQuery({
    queryKey: ['site-settings', siteId],
    queryFn: () => sitesApi.getSettings(siteId),
  });

  const { register, handleSubmit, reset, formState: { isDirty, isSubmitting, errors } } =
    useForm<CorsForm>({ resolver: zodResolver(corsSchema) });

  useEffect(() => {
    if (settings) {
      reset({
        corsOriginsRaw: settings.corsOrigins.join('\n'),
        localesRaw: settings.locales.join(', '),
      });
    }
  }, [settings, reset]);

  const mutation = useMutation({
    mutationFn: (data: CorsForm) => {
      const corsOrigins = data.corsOriginsRaw
        .split(/[\n,]+/)
        .map((s) => s.trim())
        .filter(Boolean);
      const locales = data.localesRaw
        .split(/[\n,]+/)
        .map((s) => s.trim())
        .filter(Boolean);
      return sitesApi.updateSettings(siteId, {
        previewUrlTemplate: settings?.previewUrlTemplate,
        versioningEnabled: settings?.versioningEnabled ?? true,
        workflowEnabled: settings?.workflowEnabled ?? true,
        schedulingEnabled: settings?.schedulingEnabled ?? true,
        previewEnabled: settings?.previewEnabled ?? true,
        aiEnabled: settings?.aiEnabled ?? true,
        corsOrigins,
        locales,
      });
    },
    onSuccess: () => {
      toast.success('CORS & locales saved.');
      void qc.invalidateQueries({ queryKey: ['site-settings', siteId] });
    },
    onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Save failed.'),
  });

  if (isLoading) return <div className="space-y-3">{Array.from({ length: 3 }).map((_, i) => <div key={i} className="h-20 animate-pulse rounded-lg bg-slate-100" />)}</div>;

  return (
    <form onSubmit={handleSubmit((v) => mutation.mutate(v))} className="space-y-6">
      <div className="card space-y-4">
        <h2 className="text-base font-semibold text-slate-900">CORS Origins</h2>
        <p className="text-xs text-slate-500">
          Allowed origins for Delivery API requests. Enter one URL per line (or comma-separated).
          Wildcards are not supported — use exact origins.
        </p>
        <div>
          <label className="form-label">Origins</label>
          <textarea
            className="form-input mt-1 font-mono text-xs"
            rows={5}
            placeholder={'https://www.acme.com\nhttps://staging.acme.com'}
            {...register('corsOriginsRaw')}
          />
        </div>
      </div>

      <div className="card space-y-4">
        <h2 className="text-base font-semibold text-slate-900">Supported Locales</h2>
        <p className="text-xs text-slate-500">
          Comma-separated locale codes supported by this site. The default locale is set in the General tab.
        </p>
        <div>
          <label className="form-label">Locales</label>
          <input
            className="form-input mt-1 font-mono"
            placeholder="en, de, fr"
            {...register('localesRaw')}
          />
          {errors.localesRaw && <p className="form-error">{errors.localesRaw.message}</p>}
        </div>
      </div>

      <div className="flex justify-end gap-3">
        <button type="button" onClick={() => { if (settings) reset({ corsOriginsRaw: settings.corsOrigins.join('\n'), localesRaw: settings.locales.join(', ') }); }} disabled={!isDirty} className="btn-secondary">Discard</button>
        <button type="submit" disabled={isSubmitting || !isDirty} className="btn-primary">
          {isSubmitting ? 'Saving…' : 'Save'}
        </button>
      </div>
    </form>
  );
}

// ─── API Keys tab ─────────────────────────────────────────────────────────────

function ApiKeysTab({ siteId }: { siteId: string }) {
  const qc = useQueryClient();
  const [showCreate, setShowCreate] = useState(false);
  const [newKey, setNewKey] = useState<ApiClientCreatedDto | null>(null);

  const { data: clients, isLoading } = useQuery({
    queryKey: ['api-clients', siteId],
    queryFn: () => apiClientsApi.list(siteId),
  });

  const createForm = useForm<CreateKeyForm>({
    resolver: zodResolver(createKeySchema),
    defaultValues: { name: '', keyType: 'Delivery' },
  });

  const createMutation = useMutation({
    mutationFn: (data: CreateKeyForm) =>
      apiClientsApi.create({ siteId, name: data.name, keyType: data.keyType }),
    onSuccess: (result) => {
      setNewKey(result);
      setShowCreate(false);
      createForm.reset();
      void qc.invalidateQueries({ queryKey: ['api-clients', siteId] });
    },
    onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Failed.'),
  });

  const revokeMutation = useMutation({
    mutationFn: (id: string) => apiClientsApi.revoke(id),
    onSuccess: () => {
      toast.success('Key revoked.');
      void qc.invalidateQueries({ queryKey: ['api-clients', siteId] });
    },
    onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Failed.'),
  });

  const regenMutation = useMutation({
    mutationFn: (id: string) => apiClientsApi.regenerate(id),
    onSuccess: (result) => {
      setNewKey(result);
      void qc.invalidateQueries({ queryKey: ['api-clients', siteId] });
    },
    onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Failed.'),
  });

  return (
    <>
      {newKey && <RawKeyModal result={newKey} onClose={() => setNewKey(null)} />}

      <div className="space-y-4">
        <div className="flex items-center justify-between">
          <div>
            <h2 className="text-base font-semibold text-slate-900">API Keys</h2>
            <p className="mt-0.5 text-xs text-slate-500">
              Keys are scoped to this site. Use the <strong>Site ID</strong> as the{' '}
              <code className="rounded bg-slate-100 px-1">siteId</code> parameter in Delivery API calls.
            </p>
          </div>
          <button
            type="button"
            onClick={() => setShowCreate((v) => !v)}
            className="btn-secondary text-sm"
          >
            {showCreate ? 'Cancel' : '+ New Key'}
          </button>
        </div>

        {showCreate && (
          <form
            onSubmit={createForm.handleSubmit((v) => createMutation.mutate(v))}
            className="rounded-lg border border-brand-200 bg-brand-50 p-4 space-y-3"
          >
            <h3 className="text-sm font-semibold text-slate-800">New API Key</h3>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <label className="form-label">Name</label>
                <input
                  className="form-input mt-1"
                  placeholder="e.g. Next.js frontend"
                  {...createForm.register('name')}
                />
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

        {isLoading ? (
          <div className="space-y-2">
            {Array.from({ length: 3 }).map((_, i) => (
              <div key={i} className="h-14 animate-pulse rounded-lg bg-slate-100" />
            ))}
          </div>
        ) : (clients ?? []).length === 0 ? (
          <p className="rounded-lg border border-dashed border-slate-200 py-10 text-center text-sm text-slate-400">
            No API keys yet. Create one to connect your frontend.
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
                    {!client.isActive && (
                      <span className="flex-shrink-0 rounded-full bg-red-100 px-2 py-0.5 text-[10px] font-bold text-red-700">
                        Revoked
                      </span>
                    )}
                  </div>
                  <div className="mt-0.5 flex flex-wrap items-center gap-3 text-xs text-slate-400">
                    <span>ID: <code className="font-mono">{client.id}</code></span>
                    <CopyButton value={client.id} label="Copy ID" />
                    <span>Created: {new Date(client.createdAt).toLocaleDateString()}</span>
                    {client.expiresAt && (
                      <span>Expires: {new Date(client.expiresAt).toLocaleDateString()}</span>
                    )}
                  </div>
                </div>
                {client.isActive && (
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
                )}
              </div>
            ))}
          </div>
        )}
      </div>
    </>
  );
}

// ─── Page ─────────────────────────────────────────────────────────────────────

type Tab = 'general' | 'features' | 'cors' | 'apikeys';

const TABS: { id: Tab; label: string }[] = [
  { id: 'general', label: 'General' },
  { id: 'features', label: 'Features' },
  { id: 'cors', label: 'CORS & Locales' },
  { id: 'apikeys', label: 'API Keys' },
];

export default function SiteSettingPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [tab, setTab] = useState<Tab>('general');

  const { data: site, isLoading } = useQuery({
    queryKey: ['site', id],
    queryFn: () => sitesApi.getById(id!),
    enabled: !!id,
  });

  if (!id) {
    navigate('/settings');
    return null;
  }

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      {/* Breadcrumb */}
      <div className="flex items-center gap-2 text-sm text-slate-500">
        <button onClick={() => navigate('/settings')} className="hover:text-slate-900">
          Settings
        </button>
        <span>/</span>
        <span className="text-slate-900 font-medium">
          {isLoading ? '…' : (site?.name ?? 'Site')}
        </span>
      </div>

      {/* Header */}
      <div>
        <h1 className="text-2xl font-bold text-slate-900">
          {isLoading ? (
            <span className="inline-block h-7 w-48 animate-pulse rounded bg-slate-200" />
          ) : (
            site?.name ?? 'Site Settings'
          )}
        </h1>
        {site && (
          <p className="mt-1 text-sm text-slate-500 font-mono">
            {site.handle}
            {site.customDomain && (
              <> · <a href={`https://${site.customDomain}`} target="_blank" rel="noopener noreferrer" className="text-brand-600 hover:underline">{site.customDomain}</a></>
            )}
          </p>
        )}
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

      {/* Tab content */}
      {tab === 'general' && <GeneralTab siteId={id} />}
      {tab === 'features' && <FeaturesTab siteId={id} />}
      {tab === 'cors' && <CorsLocalesTab siteId={id} />}
      {tab === 'apikeys' && <ApiKeysTab siteId={id} />}
    </div>
  );
}
