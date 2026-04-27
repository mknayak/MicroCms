import { useEffect, useState } from 'react';
import { useNavigate, useParams, useSearchParams } from 'react-router-dom';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { entriesApi } from '@/api/entries';
import { contentTypesApi } from '@/api/contentTypes';
import { RichTextEditor } from '@/components/ui/RichTextEditor';
import { WorkflowStepper } from '@/components/WorkflowStepper';
import { SeoPanel } from '@/components/SeoPanel';
import type { EntryVersion, EntryStatus, FieldDefinitionDto } from '@/types';
import { ApiError } from '@/api/client';
import { formatDistanceToNow } from 'date-fns';
import { useSite } from '@/contexts/SiteContext';

// ─── Form schema ──────────────────────────────────────────────────────────────

const baseSchema = z.object({
  slug: z.string().min(1, 'Slug is required').regex(/^[a-z0-9]+(?:-[a-z0-9]+)*$/, 'Slug must be lowercase with hyphens'),
  contentTypeId: z.string().min(1, 'Content type is required'),
  locale: z.string().min(1, 'Locale is required'),
  fields: z.record(z.unknown()),
});

type FormValues = z.infer<typeof baseSchema>;

const STATUS_BADGE: Record<string, string> = {
  Draft: 'badge-slate', PendingReview: 'badge-amber', Approved: 'badge-blue',
  Published: 'badge-green', Unpublished: 'badge-slate', Scheduled: 'badge-brand', Archived: 'badge-red',
};

// ─── Version History Drawer ───────────────────────────────────────────────────

function VersionDrawer({ entryId, onClose, onRestore }: { entryId: string; onClose: () => void; onRestore: (id: string) => void }) {
  const { data, isLoading } = useQuery({
    queryKey: ['entries', entryId, 'versions'],
 queryFn: () => entriesApi.getVersions(entryId),
  });
  return (
 <div className="fixed inset-y-0 right-0 z-40 flex w-80 flex-col border-l border-slate-200 bg-white shadow-xl">
      <div className="flex items-center justify-between border-b border-slate-200 px-4 py-3">
     <h3 className="text-sm font-semibold text-slate-900">Version History</h3>
<button onClick={onClose} className="text-slate-400 hover:text-slate-600">✕</button>
      </div>
      <div className="flex-1 overflow-y-auto p-4">
 {isLoading ? <div className="space-y-3">{Array.from({ length: 4 }).map((_, i) => <div key={i} className="h-16 animate-pulse rounded bg-slate-100" />)}</div> : (
          <ul className="space-y-2">
    {(data ?? []).map((v: EntryVersion) => (
 <li key={v.id} className="rounded-lg border border-slate-200 p-3">
     <div className="flex items-center justify-between">
         <span className="text-xs font-semibold text-slate-700">v{v.versionNumber}</span>
    <button onClick={() => onRestore(v.id)} className="text-xs text-brand-600 hover:underline">Restore</button>
      </div>
      <p className="mt-1 text-xs text-slate-500">{v.authorId} · {formatDistanceToNow(new Date(v.createdAt), { addSuffix: true })}</p>
       {v.changeNote && <p className="mt-1 text-xs italic text-slate-400">{v.changeNote}</p>}
        </li>
            ))}
          </ul>
    )}
      </div>
  </div>
  );
}

// ─── Field Renderer ───────────────────────────────────────────────────────────

function FieldInput({ field, value, onChange }: { field: FieldDefinitionDto; value: unknown; onChange: (val: unknown) => void }) {
  switch (field.fieldType) {
    case 'RichText':
      return <RichTextEditor value={typeof value === 'string' ? value : ''} onChange={onChange} placeholder={`Write ${field.label}…`} />;
    case 'LongText':
    case 'Markdown':
      return <textarea value={typeof value === 'string' ? value : ''} onChange={(e) => onChange(e.target.value)} rows={6} className="form-input resize-y font-mono" placeholder={`Enter ${field.label}…`} />;
    case 'Json':
      return <textarea value={typeof value === 'string' ? value : JSON.stringify(value ?? {}, null, 2)} onChange={(e) => onChange(e.target.value)} rows={8} className="form-input resize-y font-mono text-xs" placeholder="{}" spellCheck={false} />;
    case 'Boolean':
      return <input type="checkbox" checked={Boolean(value)} onChange={(e) => onChange(e.target.checked)} className="h-4 w-4 rounded border-slate-300 text-brand-600" />;
    case 'Integer':
      return <input type="number" step="1" value={typeof value === 'number' ? value : ''} onChange={(e) => onChange(e.target.valueAsNumber)} className="form-input" placeholder="0" />;
    case 'Decimal':
 return <input type="number" step="any" value={typeof value === 'number' ? value : ''} onChange={(e) => onChange(e.target.valueAsNumber)} className="form-input" placeholder="0.00" />;
    case 'DateTime':
      return <input type="datetime-local" value={typeof value === 'string' ? value : ''} onChange={(e) => onChange(e.target.value)} className="form-input" />;
    case 'Color':
      return (
        <div className="flex items-center gap-3">
          <input type="color" value={typeof value === 'string' && value ? value : '#000000'} onChange={(e) => onChange(e.target.value)} className="h-9 w-16 cursor-pointer rounded border border-slate-300 p-0.5" />
          <input type="text" value={typeof value === 'string' ? value : ''} onChange={(e) => onChange(e.target.value)} className="form-input font-mono" placeholder="#000000" />
        </div>
 );
    case 'Enum':
      // Use <select> when options are provided; fall back to text input
      if (field.options && field.options.length > 0) {
        return (
        <select value={typeof value === 'string' ? value : ''} onChange={(e) => onChange(e.target.value)} className="form-input">
     <option value="">— select —</option>
      {field.options.map((opt) => <option key={opt} value={opt}>{opt}</option>)}
          </select>
        );
 }
      return <input type="text" value={typeof value === 'string' ? value : ''} onChange={(e) => onChange(e.target.value)} className="form-input" placeholder="Enter value…" />;
    case 'AssetReference':
      return (
        <div className="flex items-center gap-2">
       <input type="text" value={typeof value === 'string' ? value : ''} onChange={(e) => onChange(e.target.value)} className="form-input flex-1 font-mono" placeholder="Asset ID…" />
    <span className="text-xs text-slate-400">(asset picker — phase 2)</span>
        </div>
      );
    case 'Reference':
 return (
    <div className="flex items-center gap-2">
        <input type="text" value={typeof value === 'string' ? value : ''} onChange={(e) => onChange(e.target.value)} className="form-input flex-1 font-mono" placeholder="Entry ID…" />
          <span className="text-xs text-slate-400">(entry picker — phase 2)</span>
        </div>
      );
    case 'Location':
      return (
     <div className="grid grid-cols-2 gap-2">
          <input type="number" step="any" value={typeof value === 'object' && value !== null && 'lat' in value ? (value as { lat: number }).lat : ''} onChange={(e) => onChange({ ...(typeof value === 'object' && value !== null ? value : {}), lat: e.target.valueAsNumber })} className="form-input" placeholder="Latitude" />
          <input type="number" step="any" value={typeof value === 'object' && value !== null && 'lng' in value ? (value as { lng: number }).lng : ''} onChange={(e) => onChange({ ...(typeof value === 'object' && value !== null ? value : {}), lng: e.target.valueAsNumber })} className="form-input" placeholder="Longitude" />
     </div>
      );
    default:
      return <input type="text" value={typeof value === 'string' ? value : ''} onChange={(e) => onChange(e.target.value)} className="form-input" placeholder={`Enter ${field.label}…`} />;
  }
}

// ─── Page ─────────────────────────────────────────────────────────────────────

const LOCALES = ['en', 'en-US', 'de', 'fr', 'es', 'pt', 'ja', 'zh'];

export default function EntryEditorPage() {
  const { id } = useParams();
  const [searchParams] = useSearchParams();
  const isNew = !id;
  const navigate = useNavigate();
  const qc = useQueryClient();
  const { selectedSiteId } = useSite();
  const [showVersions, setShowVersions] = useState(false);
  const [activeLocale, setActiveLocale] = useState('en');

  const { data: existing } = useQuery({
    queryKey: ['entries', id],
 queryFn: () => entriesApi.getById(id!),
    enabled: !isNew,
  });

  const { data: contentTypes } = useQuery({
    queryKey: ['content-types'],
    queryFn: () => contentTypesApi.list({ pageSize: 100 }),
  });

  const { register, control, handleSubmit, watch, setValue, formState: { errors, isSubmitting, isDirty } } = useForm<FormValues>({
    resolver: zodResolver(baseSchema),
    defaultValues: { slug: '', contentTypeId: searchParams.get('contentTypeId') ?? '', locale: 'en', fields: {} },
  });

  const selectedContentTypeId = watch('contentTypeId');
  const { data: selectedContentType } = useQuery({
    queryKey: ['content-types', selectedContentTypeId],
    queryFn: () => contentTypesApi.getById(selectedContentTypeId),
    enabled: Boolean(selectedContentTypeId),
  });

  useEffect(() => {
    if (existing) {
      setValue('slug', existing.slug);
      setValue('contentTypeId', existing.contentTypeId);
    setValue('locale', existing.locale);
      setActiveLocale(existing.locale);
      setValue('fields', existing.fields ?? {});
    }
  }, [existing, setValue]);

  // ── Mutations ────────────────────────────────────────────────────────────

  const saveMutation = useMutation({
    mutationFn: (values: FormValues) =>
 isNew
  ? entriesApi.create({ siteId: selectedSiteId ?? '', contentTypeId: values.contentTypeId, slug: values.slug, locale: values.locale, fields: values.fields })
        : entriesApi.update(id!, { newSlug: values.slug, fields: values.fields }),
    onSuccess: (saved) => {
      toast.success(isNew ? 'Entry created.' : 'Entry saved.');
      void qc.invalidateQueries({ queryKey: ['entries'] });
  if (isNew) navigate(`/entries/${saved.id}/edit`);
    },
    onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Save failed.'),
  });

  const wfMutation = useMutation({
    mutationFn: (action: () => Promise<unknown>) => action(),
    onSuccess: () => { void qc.invalidateQueries({ queryKey: ['entries', id] }); },
    onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Action failed.'),
  });

  const restoreVersionMutation = useMutation({
    mutationFn: (versionId: string) => entriesApi.restoreVersion(id!, versionId),
    onSuccess: () => { toast.success('Version restored.'); void qc.invalidateQueries({ queryKey: ['entries', id] }); setShowVersions(false); },
  });

  const openPreview = async () => {
  try {
      const { token } = await entriesApi.getPreviewToken(id!);
  window.open(`/entries/preview?token=${token}`, '_blank');
    } catch {
      toast.error('Could not generate preview token.');
    }
  };

  const localeVariants = existing?.localeVariants ?? [existing?.locale ?? 'en'];
  const currentStatus = (existing?.status ?? 'Draft') as EntryStatus;

  return (
    <div className="mx-auto max-w-5xl space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <button onClick={() => navigate('/entries')} className="btn-secondary">← Back</button>
          <h1 className="text-2xl font-bold text-slate-900">
    {isNew ? 'New Entry' : (typeof existing?.fields?.title === 'string' ? existing.fields.title : existing?.slug) ?? '…'}
          </h1>
  {existing && <span className={STATUS_BADGE[existing.status] ?? 'badge-slate'}>{existing.status}</span>}
  </div>
        <div className="flex items-center gap-2">
   {!isNew && <button onClick={() => setShowVersions((v) => !v)} className="btn-secondary">History</button>}
       {!isNew && <button onClick={openPreview} className="btn-secondary">Preview ↗</button>}
 </div>
      </div>

    <form onSubmit={handleSubmit((v) => saveMutation.mutate(v))} className="grid grid-cols-3 gap-6">
   {/* Main content */}
<div className="col-span-2 space-y-5">
  {/* Slug + locale pills */}
          <div className="card space-y-4">
       <div className="grid grid-cols-2 gap-4">
        {isNew && (
    <div>
 <label className="form-label">Content Type</label>
   <select className="form-input mt-1" {...register('contentTypeId')}>
         <option value="">Select…</option>
              {contentTypes?.items.map((ct) => <option key={ct.id} value={ct.id}>{ct.displayName}</option>)}
 </select>
          {errors.contentTypeId && <p className="form-error">{errors.contentTypeId.message}</p>}
            </div>
       )}
<div>
          <label className="form-label">Slug</label>
          <input className="form-input mt-1 font-mono" {...register('slug')} placeholder="my-entry-slug" />
            {errors.slug && <p className="form-error">{errors.slug.message}</p>}
              </div>
 </div>

            {/* Locale pills */}
            <div>
   <label className="form-label mb-2">Locale</label>
  <div className="flex flex-wrap gap-2">
                {(isNew ? LOCALES : localeVariants).map((loc) => (
 <button
         key={loc}
        type="button"
   onClick={() => { setActiveLocale(loc); setValue('locale', loc); }}
         className={`rounded-full border px-3 py-1 text-xs font-medium transition-colors ${
     activeLocale === loc
       ? 'border-brand-500 bg-brand-500 text-white'
        : 'border-slate-200 text-slate-600 hover:border-brand-300'
         }`}
   >
           {loc.toUpperCase()}
                {!isNew && !localeVariants.includes(loc) && <span className="ml-1 text-amber-400">⚠</span>}
      </button>
                ))}
         {!isNew && (
      <button
       type="button"
   onClick={() => {
            const newLoc = prompt('Enter locale code (e.g. fr, de, ja):');
         if (newLoc?.trim()) { setActiveLocale(newLoc.trim()); setValue('locale', newLoc.trim()); }
   }}
             className="rounded-full border border-dashed border-slate-300 px-3 py-1 text-xs text-slate-400 hover:border-brand-300 hover:text-brand-600"
    >
          + Add locale
         </button>
         )}
          </div>
    </div>
          </div>

{/* Dynamic fields */}
   {selectedContentType?.fields.map((field) => (
            <div key={field.id} className="card space-y-2">
           <div className="flex items-center justify-between">
            <label className="form-label">
        {field.label}
           {field.isRequired && <span className="ml-1 text-red-500">*</span>}
        {field.isLocalized && <span className="ml-2 badge-brand text-xs">Localized</span>}
           {field.isIndexed && <span className="ml-1 badge-amber text-xs">Indexed</span>}
           </label>
        <span className="text-xs text-slate-400">{field.fieldType}</span>
     </div>
<Controller
    control={control}
                name={`fields.${field.handle}`}
           render={({ field: f }) => <FieldInput field={field} value={f.value} onChange={f.onChange} />}
/>
            </div>
        ))}
        </div>

   {/* Sidebar */}
        <div className="col-span-1 space-y-4">
          {/* Save */}
          <div className="card space-y-3">
            <h3 className="text-sm font-semibold text-slate-900">Actions</h3>
<button type="submit" disabled={isSubmitting || !isDirty} className="btn-secondary w-full justify-center">
      {isSubmitting ? 'Saving…' : 'Save Draft'}
            </button>
          </div>

          {/* Workflow stepper */}
          {!isNew && existing && (
          <WorkflowStepper
       entryId={id!}
              currentStatus={currentStatus}
      onSubmitForReview={() => wfMutation.mutate(() => entriesApi.submitForReview(id!).then(() => toast.success('Submitted for review.')))}
       onApprove={() => wfMutation.mutate(() => entriesApi.approve(id!).then(() => toast.success('Approved.')))}
       onReject={(reason) => wfMutation.mutate(() => entriesApi.reject(id!, reason).then(() => toast.success('Rejected.')))}
     onPublish={() => wfMutation.mutate(() => entriesApi.publish(id!).then(() => toast.success('Published.')))}
  onUnpublish={() => wfMutation.mutate(() => entriesApi.unpublish(id!).then(() => toast.success('Unpublished.')))}
   onSchedule={(publishAt, unpublishAt) => wfMutation.mutate(() => entriesApi.schedule(id!, publishAt, unpublishAt).then(() => toast.success('Scheduled.')))}
            />
 )}

    {/* SEO Panel */}
   {!isNew && existing && (
            <SeoPanel entryId={id!} initialSeo={existing.seo} />
    )}

          {/* Metadata */}
          {existing && (
   <div className="card space-y-2 text-xs text-slate-500">
              <h3 className="text-sm font-semibold text-slate-900">Metadata</h3>
    <p>Created: {new Date(existing.createdAt).toLocaleDateString()}</p>
          <p>Updated: {formatDistanceToNow(new Date(existing.updatedAt), { addSuffix: true })}</p>
              <p>Author: {existing.authorId}</p>
 <p>Version: v{existing.currentVersionNumber}</p>
   {existing.publishedAt && <p>Published: {new Date(existing.publishedAt).toLocaleDateString()}</p>}
       {existing.scheduledPublishAt && <p>Scheduled: {new Date(existing.scheduledPublishAt).toLocaleString()}</p>}
          </div>
     )}
        </div>
      </form>

      {/* Version drawer */}
  {showVersions && id && (
        <VersionDrawer entryId={id} onClose={() => setShowVersions(false)} onRestore={(vid) => restoreVersionMutation.mutate(vid)} />
      )}
    </div>
  );
}
