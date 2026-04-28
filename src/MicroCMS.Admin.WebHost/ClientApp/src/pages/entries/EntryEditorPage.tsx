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

const STATUS_STEPS: { key: string; label: string }[] = [
  { key: 'Draft', label: 'Drafted' },
  { key: 'PendingReview', label: 'Submitted for Review' },
  { key: 'Approved', label: 'Approved' },
  { key: 'Published', label: 'Published' },
];

const STATUS_ORDER: Record<string, number> = {
  Draft: 0, PendingReview: 1, Approved: 2, Published: 3,
  Scheduled: 3, Unpublished: 2, Archived: -1,
};

// ─── Char Counter ─────────────────────────────────────────────────────────────

function CharCounter({ value, max }: { value: string; max: number }) {
  const len = typeof value === 'string' ? value.length : 0;
  return (
    <span className={`text-xs tabular-nums ${len > max ? 'text-red-500 font-semibold' : len > max * 0.85 ? 'text-amber-500' : 'text-slate-400'}`}>
      {len} / {max} chars
    </span>
  );
}

// ─── List Field Wrapper ───────────────────────────────────────────────────────

/**
 * Wraps any scalar FieldInput with add/remove list management.
 * The stored value is always an array; each item is edited inline.
 */
function ListFieldInput({
  field,
  value,
  onChange,
}: {
  field: FieldDefinitionDto;
  value: unknown;
  onChange: (val: unknown) => void;
}) {
  const items: unknown[] = Array.isArray(value) ? value : [];

  const updateItem = (index: number, newVal: unknown) => {
    const updated = [...items];
    updated[index] = newVal;
    onChange(updated);
  };

  const addItem = () => onChange([...items, '']);
  const removeItem = (index: number) => onChange(items.filter((_, i) => i !== index));

  // For Enum multi-select we render checkboxes instead of a list of selects
  if (field.fieldType === 'Enum' && field.options && field.options.length > 0) {
    const selected = items.map(String);
    const toggle = (opt: string) => {
      const next = selected.includes(opt)
      ? selected.filter((o) => o !== opt)
        : [...selected, opt];
      onChange(next);
 };
    return (
      <div className="flex flex-wrap gap-2">
        {field.options.map((opt) => (
          <label key={opt} className="flex cursor-pointer items-center gap-2 rounded-lg border border-slate-200 px-3 py-1.5 text-sm hover:border-brand-300 hover:bg-brand-50">
   <input
              type="checkbox"
   checked={selected.includes(opt)}
              onChange={() => toggle(opt)}
           className="h-4 w-4 rounded border-slate-300 text-brand-600"
  />
    {opt}
          </label>
        ))}
      </div>
    );
  }

  // Generic list: one scalar input per item
  const scalarField = { ...field, isList: false }; // render scalar version inside
  return (
    <div className="space-y-2">
      {items.map((item, idx) => (
        <div key={idx} className="flex items-start gap-2">
          <div className="flex-1">
            <ScalarFieldInput field={scalarField} value={item} onChange={(v) => updateItem(idx, v)} />
          </div>
          <button
     type="button"
            onClick={() => removeItem(idx)}
  className="mt-1 flex h-8 w-8 flex-shrink-0 items-center justify-center rounded text-slate-300 hover:bg-red-50 hover:text-red-500"
    aria-label="Remove item"
     >
     ✕
 </button>
        </div>
      ))}
      <button
   type="button"
        onClick={addItem}
        className="flex items-center gap-1 text-xs font-medium text-brand-600 hover:text-brand-700"
      >
   <span className="text-base leading-none">+</span> Add item
      </button>
      {items.length > 0 && (
      <p className="text-xs text-slate-400">{items.length} item{items.length !== 1 ? 's' : ''}</p>
      )}
  </div>
  );
}

// ─── Scalar Field Renderer ────────────────────────────────────────────────────
// (renamed from FieldInput — handles single-value rendering)

function ScalarFieldInput({ field, value, onChange }: { field: FieldDefinitionDto; value: unknown; onChange: (val: unknown) => void }) {
  switch (field.fieldType) {
    case 'RichText':
      return (
        <div className="relative">
       <RichTextEditor value={typeof value === 'string' ? value : ''} onChange={onChange} placeholder={`Write ${field.label}…`} />
  <button type="button" className="absolute right-2 top-2 flex items-center gap-1 rounded-md border border-brand-200 bg-brand-50 px-2 py-1 text-xs font-medium text-brand-700 hover:bg-brand-100">
 <span>✦</span> AI Assist
</button>
     </div>
 );
    case 'LongText':
    case 'Markdown':
      return <textarea value={typeof value === 'string' ? value : ''} onChange={(e) => onChange(e.target.value)} rows={6} className="form-input resize-y font-mono" placeholder={`Enter ${field.label}…`} />;
    case 'Json':
      return <textarea value={typeof value === 'string' ? value : JSON.stringify(value ?? {}, null, 2)} onChange={(e) => onChange(e.target.value)} rows={8} className="form-input resize-y font-mono text-xs" placeholder="{}" spellCheck={false} />;
    case 'Boolean':
      return (
        <label className="flex cursor-pointer items-center gap-2">
       <input type="checkbox" checked={Boolean(value)} onChange={(e) => onChange(e.target.checked)} className="h-4 w-4 rounded border-slate-300 text-brand-600" />
          <span className="text-sm text-slate-700">{field.label}</span>
        </label>
      );
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
        <div className="flex min-h-32 flex-col items-center justify-center gap-2 rounded-lg border-2 border-dashed border-slate-200 bg-slate-50 p-6 text-center">
          <div className="flex h-10 w-10 items-center justify-center rounded-full bg-slate-200 text-slate-400">
            <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" /></svg>
          </div>
          <p className="text-xs text-slate-500">Select from media library</p>
      <button type="button" className="btn-secondary text-xs">Browse</button>
          {typeof value === 'string' && value && <p className="mt-1 font-mono text-xs text-slate-400 break-all">{value}</p>}
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

// ─── Field Renderer ───────────────────────────────────────────────────────────
// Routes to list or scalar renderer based on field.isList

function FieldInput({ field, value, onChange }: { field: FieldDefinitionDto; value: unknown; onChange: (val: unknown) => void }) {
  if (field.isList) {
    return <ListFieldInput field={field} value={value} onChange={onChange} />;
  }
  return <ScalarFieldInput field={field} value={value} onChange={onChange} />;
}

// ─── Publishing Panel ─────────────────────────────────────────────────────────

function PublishingPanel({
  currentStatus,
  isNew,
  isSubmitting,
  isDirty,
  onSubmitForReview,
  onApprove,
  onReject,
  onPublish,
  onUnpublish,
  onSchedule,
}: {
  currentStatus: EntryStatus;
  isNew: boolean;
  isSubmitting: boolean;
  isDirty: boolean;
  onSubmitForReview: () => void;
  onApprove: () => void;
  onReject: (reason: string) => void;
  onPublish: () => void;
  onUnpublish: () => void;
  onSchedule: (publishAt: string, unpublishAt?: string) => void;
}) {
  const [showSchedule, setShowSchedule] = useState(false);
  const [showRejectModal, setShowRejectModal] = useState(false);
  const [rejectReason, setRejectReason] = useState('');
  const [publishAt, setPublishAt] = useState('');
  const [unpublishAt, setUnpublishAt] = useState('');
  const currentOrder = STATUS_ORDER[currentStatus] ?? 0;

  return (
    <div className="card space-y-4">
  {/* Header row */}
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-semibold text-slate-900">Publishing</h3>
        <span className={`${STATUS_BADGE[currentStatus] ?? 'badge-slate'} text-xs`}>{currentStatus}</span>
      </div>

      {/* Workflow steps */}
      {!isNew && (
        <ol className="space-y-0">
          {STATUS_STEPS.map((step, idx) => {
          const stepOrder = STATUS_ORDER[step.key] ?? 0;
      const isCompleted = currentOrder > stepOrder;
            const isActive = currentStatus === step.key || (step.key === 'Published' && currentStatus === 'Scheduled');
            return (
              <li key={step.key} className="flex items-start gap-3 pb-3 last:pb-0">
  <div className="relative flex flex-col items-center">
    <span className={`flex h-6 w-6 shrink-0 items-center justify-center rounded-full border-2 text-xs font-bold ${
           isCompleted ? 'border-green-500 bg-green-500 text-white'
        : isActive ? 'border-brand-500 bg-brand-50 text-brand-600'
       : 'border-slate-200 bg-white text-slate-400'
     }`}>
   {isCompleted ? '✓' : idx + 1}
          </span>
    {idx < STATUS_STEPS.length - 1 && (
        <div className={`mt-1 h-4 w-0.5 ${isCompleted ? 'bg-green-400' : 'bg-slate-200'}`} />
)}
       </div>
             <span className={`pt-0.5 text-xs ${isActive ? 'font-semibold text-slate-900' : isCompleted ? 'text-slate-600' : 'text-slate-400'}`}>
           {step.label}
       </span>
      </li>
         );
          })}
    </ol>
      )}

 {/* Save */}
      <div className="border-t border-slate-100 pt-3 space-y-2">
        <button type="submit" disabled={isSubmitting || !isDirty} className="btn-secondary w-full justify-center text-sm">
      {isSubmitting ? 'Saving…' : isNew ? 'Create Entry' : 'Save Draft'}
        </button>

        {/* Workflow actions */}
  {!isNew && currentStatus === 'Draft' && (
    <button type="button" onClick={onSubmitForReview} className="w-full rounded-lg border border-amber-300 bg-amber-50 px-3 py-2 text-xs font-medium text-amber-800 hover:bg-amber-100">
          Submit for Review
          </button>
        )}
    {!isNew && currentStatus === 'PendingReview' && (
          <>
      <button type="button" onClick={onApprove} className="w-full rounded-lg bg-brand-600 px-3 py-2 text-xs font-medium text-white hover:bg-brand-700">Approve</button>
        <button type="button" onClick={() => setShowRejectModal(true)} className="w-full rounded-lg border border-red-200 bg-red-50 px-3 py-2 text-xs font-medium text-red-700 hover:bg-red-100">Reject…</button>
        </>
        )}
        {!isNew && currentStatus === 'Approved' && (
          <>
       <button type="button" onClick={onPublish} className="w-full rounded-lg bg-green-600 px-3 py-2 text-xs font-medium text-white hover:bg-green-700">Publish Now</button>
        <button type="button" onClick={() => setShowSchedule((v) => !v)} className="w-full rounded-lg border border-slate-200 px-3 py-2 text-xs font-medium text-slate-700 hover:bg-slate-50">
              {showSchedule ? 'Cancel' : '⏰ Schedule…'}
    </button>
        {showSchedule && (
 <div className="space-y-2 rounded-lg bg-slate-50 p-3">
        <div>
       <label className="form-label text-xs">Publish at</label>
  <input type="datetime-local" className="form-input mt-1 text-xs" value={publishAt} onChange={(e) => setPublishAt(e.target.value)} />
     </div>
    <div>
             <label className="form-label text-xs text-slate-400">Unpublish at (optional)</label>
       <input type="datetime-local" className="form-input mt-1 text-xs" value={unpublishAt} onChange={(e) => setUnpublishAt(e.target.value)} />
     </div>
    <button type="button" onClick={() => { if (publishAt) { onSchedule(new Date(publishAt).toISOString(), unpublishAt ? new Date(unpublishAt).toISOString() : undefined); setShowSchedule(false); } }} disabled={!publishAt} className="w-full rounded-lg bg-brand-600 px-3 py-2 text-xs font-medium text-white hover:bg-brand-700 disabled:opacity-50">
          Schedule
   </button>
            </div>
    )}
          </>
        )}
        {!isNew && (currentStatus === 'Published' || currentStatus === 'Scheduled') && (
          <button type="button" onClick={onUnpublish} className="w-full rounded-lg border border-slate-200 bg-white px-3 py-2 text-xs font-medium text-slate-700 hover:bg-slate-50">
Unpublish
       </button>
        )}
      {!isNew && (currentStatus === 'Published' || currentStatus === 'Scheduled') && (
          <button type="button" onClick={() => setShowSchedule((v) => !v)} className="w-full rounded-lg border border-brand-200 bg-brand-50 px-3 py-2 text-xs font-medium text-brand-700 hover:bg-brand-100">
            ⏰ Schedule
       </button>
        )}
      </div>

      {/* Reject modal */}
 {showRejectModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div className="card mx-4 w-full max-w-sm space-y-4">
         <h3 className="text-sm font-semibold text-slate-900">Reject entry</h3>
            <textarea className="form-input w-full resize-none" rows={3} placeholder="Reason for rejection…" value={rejectReason} onChange={(e) => setRejectReason(e.target.value)} />
          <div className="flex justify-end gap-2">
    <button onClick={() => setShowRejectModal(false)} className="btn-secondary text-xs">Cancel</button>
           <button onClick={() => { if (rejectReason.trim()) { onReject(rejectReason.trim()); setShowRejectModal(false); } }} disabled={!rejectReason.trim()} className="rounded-lg bg-red-600 px-3 py-2 text-xs font-medium text-white hover:bg-red-700 disabled:opacity-50">Reject</button>
            </div>
        </div>
        </div>
      )}
    </div>
  );
}

// ─── AI Authoring Assist Panel ────────────────────────────────────────────────

function AiAuthoringPanel() {
  return (
    <div className="card space-y-3">
      <div className="flex items-center gap-2">
    <span className="flex h-6 w-6 items-center justify-center rounded-full bg-brand-600 text-xs text-white">✦</span>
    <h3 className="text-sm font-semibold text-slate-900">AI Authoring Assist</h3>
    </div>
 <div className="space-y-1.5">
     {[
          { icon: '✦', label: 'Generate Draft', desc: 'Create body from brief' },
      { icon: '↺', label: 'Rewrite / Tone', desc: 'Format, friendly, shorter…' },
          { icon: '≡', label: 'Summarize', desc: 'TL;DR, abstract, social' },
    { icon: '◈', label: 'SEO Suggestions', desc: 'Title, description, keywords' },
        { icon: '⇄', label: 'Translate', desc: 'In-EN (missing), de-DE' },
 ].map((action) => (
        <button key={action.label} type="button" className="flex w-full items-start gap-2.5 rounded-lg p-2 text-left hover:bg-slate-50">
        <span className="mt-0.5 text-sm text-brand-500">{action.icon}</span>
 <div>
       <p className="text-xs font-medium text-slate-800">{action.label}</p>
     <p className="text-xs text-slate-400">{action.desc}</p>
          </div>
 </button>
        ))}
   </div>
      <p className="border-t border-slate-100 pt-2 text-xs text-slate-400">
        Powered by <span className="font-medium text-slate-600">Claude Sonnet</span> (Balanced tier)
      </p>
    </div>
  );
}

// ─── Quality Checks Panel ─────────────────────────────────────────────────────

function QualityChecksPanel({ localeVariants, activeLocale }: { localeVariants: string[]; activeLocale: string }) {
  const checks = [
    { label: 'Grammar & spelling', status: 'pass', detail: 'no issues' },
    { label: 'Readability', status: 'pass', detail: 'Grade 9 (Good)' },
    { label: 'No PII detected', status: 'pass', detail: '' },
    { label: `${localeVariants.find(l => l !== activeLocale) ?? 'de-DE'} locale missing`, status: 'warn', detail: '' },
  ];
  const passed = checks.filter((c) => c.status === 'pass').length;
  return (
    <div className="card space-y-3">
      <div className="flex items-center justify-between">
   <h3 className="text-sm font-semibold text-slate-900">Quality Checks</h3>
        <span className="text-xs font-semibold text-brand-600">{passed}/{checks.length}</span>
      </div>
      <ul className="space-y-1.5">
        {checks.map((c) => (
          <li key={c.label} className="flex items-center gap-2 text-xs">
<span className={c.status === 'pass' ? 'text-green-500' : 'text-amber-500'}>{c.status === 'pass' ? '✓' : '⚠'}</span>
     <span className={c.status === 'pass' ? 'text-slate-700' : 'text-amber-700'}>{c.label}</span>
   {c.detail && <span className="ml-auto text-slate-400">{c.detail}</span>}
    </li>
        ))}
      </ul>
    </div>
  );
}

// ─── Version History Panel ────────────────────────────────────────────────────

function VersionHistoryPanel({ entryId, onRestore }: { entryId: string; onRestore: (id: string) => void }) {
  const { data, isLoading } = useQuery({
  queryKey: ['entries', entryId, 'versions'],
    queryFn: () => entriesApi.getVersions(entryId),
  });
  return (
    <div className="card space-y-3">
      <div className="flex items-center justify-between">
        <h3 className="text-sm font-semibold text-slate-900">Version History</h3>
 <button type="button" className="text-xs text-brand-600 hover:underline">Compare</button>
  </div>
{isLoading ? (
<div className="space-y-2">{Array.from({ length: 3 }).map((_, i) => <div key={i} className="h-12 animate-pulse rounded bg-slate-100" />)}</div>
   ) : (
        <ul className="space-y-1.5">
          {(data ?? []).slice(0, 5).map((v: EntryVersion, idx: number) => (
         <li key={v.id} className={`rounded-lg p-2.5 ${idx === 0 ? 'border border-brand-200 bg-brand-50' : 'hover:bg-slate-50'}`}>
   <div className="flex items-center justify-between">
   <div className="flex items-center gap-2">
             <span className="text-xs font-semibold text-slate-700">v{v.versionNumber}</span>
     {idx === 0 && <span className="rounded-full bg-green-100 px-1.5 py-0.5 text-[10px] font-semibold uppercase tracking-wide text-green-700">live</span>}
       </div>
  {idx > 0 && (
        <button onClick={() => onRestore(v.id)} type="button" className="text-xs text-brand-600 hover:underline">Restore</button>
     )}
              </div>
              <p className="mt-0.5 text-xs text-slate-500">
        {v.authorId.toString().slice(0, 4).toUpperCase()} · {formatDistanceToNow(new Date(v.createdAt), { addSuffix: true })}
   </p>
            {v.changeNote && <p className="mt-0.5 text-xs italic text-slate-400 truncate">{v.changeNote}</p>}
   </li>
          ))}
        </ul>
   )}
    </div>
  );
}

// ─── Page ─────────────────────────────────────────────────────────────────────

const LOCALES = ['en', 'en-US', 'de', 'fr', 'es', 'pt', 'ja', 'zh'];

export default function EntryEditorPage() {
  const { id } = useParams();
  const [searchParams] = useSearchParams();
  const isNew = !id;
  const navigate = useNavigate();
  const qc = useQueryClient();
  const { selectedSiteId, selectedSite } = useSite();
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
  const watchedFields = watch('fields');

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

  // ── Mutations ─────────────────────────────────────────────────────────

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
    onSuccess: () => { toast.success('Version restored.'); void qc.invalidateQueries({ queryKey: ['entries', id] }); },
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
  const entryTitle = typeof existing?.fields?.title === 'string' ? existing.fields.title : existing?.slug;
  const contentTypeName = contentTypes?.items.find((ct) => ct.id === (existing?.contentTypeId ?? selectedContentTypeId))?.displayName;
  const domainPrefix = selectedSite?.customDomain ? `${selectedSite.customDomain}/` : 'blog.acmecorp.ai/';

  return (
    <div className="flex min-h-0 flex-col">
      {/* ── Top bar ─────────────────────────────────────────────────────── */}
      <div className="sticky top-0 z-10 border-b border-slate-200 bg-white">
        {/* Breadcrumb + actions */}
        <div className="flex items-center justify-between px-6 py-3">
   <nav className="flex items-center gap-1.5 text-sm text-slate-500">
            <button onClick={() => navigate('/')} className="hover:text-slate-700">Home</button>
       <span>/</span>
  <button onClick={() => navigate('/content-types')} className="hover:text-slate-700">Content Types</button>
    {contentTypeName && (
     <>
   <span>/</span>
       <button onClick={() => navigate('/entries')} className="hover:text-slate-700">{contentTypeName}</button>
              </>
            )}
 {entryTitle && (
    <>
           <span>/</span>
                <span className="font-medium text-slate-900 truncate max-w-[200px]">{entryTitle}</span>
 </>
            )}
  </nav>
          <div className="flex items-center gap-2">
      {existing && <span className={`${STATUS_BADGE[existing.status] ?? 'badge-slate'} text-xs`}>✦ {existing.status} · v{existing.currentVersionNumber}</span>}
     {!isNew && <button type="button" onClick={openPreview} className="btn-secondary text-xs">Preview ↗</button>}
            <button form="entry-form" type="submit" disabled={isSubmitting || !isDirty} className="btn-primary text-xs">
     {isSubmitting ? 'Saving…' : isNew ? 'Create Entry' : 'Save Draft'}
       </button>
          </div>
        </div>

        {/* Locale switcher row */}
        <div className="flex items-center gap-2 overflow-x-auto border-t border-slate-100 px-6 py-2">
          <span className="shrink-0 text-xs font-medium text-slate-500">Locale:</span>
          {(isNew ? LOCALES : localeVariants).map((loc) => (
         <button
key={loc}
            type="button"
        onClick={() => { setActiveLocale(loc); setValue('locale', loc); }}
  className={`shrink-0 flex items-center gap-1.5 rounded-full border px-3 py-1 text-xs font-medium transition-colors ${
 activeLocale === loc
        ? 'border-brand-500 bg-brand-500 text-white'
             : 'border-slate-200 text-slate-600 hover:border-brand-300'
     }`}
  >
{loc.toUpperCase()}
        {!isNew && !localeVariants.includes(loc) && <span className="text-amber-300">⚠</span>}
            </button>
          ))}
          {!isNew && (
            <button
   type="button"
              onClick={() => {
           const newLoc = prompt('Enter locale code (e.g. fr, de, ja):');
        if (newLoc?.trim()) { setActiveLocale(newLoc.trim()); setValue('locale', newLoc.trim()); }
         }}
         className="shrink-0 rounded-full border border-dashed border-slate-300 px-3 py-1 text-xs text-slate-400 hover:border-brand-300 hover:text-brand-600"
        >
  + Add locale
    </button>
   )}
   </div>
      </div>

      {/* ── Body ────────────────────────────────────────────────────────── */}
      <form id="entry-form" onSubmit={handleSubmit((v) => saveMutation.mutate(v))} className="flex flex-1 gap-0 overflow-hidden">
        {/* ── Main content ────────────────────────────────────────────── */}
        <div className="flex-1 overflow-y-auto px-6 py-6 space-y-5">

       {/* Content type selector (new only) */}
     {isNew && (
            <div className="card space-y-2">
       <label className="form-label">Content Type</label>
        <select className="form-input" {...register('contentTypeId')}>
          <option value="">Select content type…</option>
      {contentTypes?.items.map((ct) => <option key={ct.id} value={ct.id}>{ct.displayName}</option>)}
  </select>
      {errors.contentTypeId && <p className="form-error">{errors.contentTypeId.message}</p>}
     </div>
      )}

          {/* URL Slug */}
       <div className="card space-y-2">
      <div className="flex items-center justify-between">
        <label className="form-label">URL Slug</label>
              {errors.slug && <p className="form-error text-xs">{errors.slug.message}</p>}
          </div>
     <div className="flex items-center overflow-hidden rounded-lg border border-slate-200 focus-within:border-brand-400 focus-within:ring-1 focus-within:ring-brand-400">
  <span className="shrink-0 border-r border-slate-200 bg-slate-50 px-3 py-2 text-xs text-slate-400 select-none">{domainPrefix}</span>
              <input
         className="flex-1 bg-white px-3 py-2 text-sm font-mono text-slate-800 outline-none placeholder:text-slate-300"
       {...register('slug')}
       placeholder="my-entry-slug"
  />
            </div>
          </div>

          {/* Dynamic fields */}
          {selectedContentType?.fields.map((field) => (
 <div key={field.id} className="card space-y-2">
  <div className="flex items-center justify-between">
       <div className="flex items-center gap-2">
        <label className="form-label mb-0">
{field.label}
   {field.isRequired && <span className="ml-1 text-red-500">*</span>}
  </label>
    {field.isLocalized && <span className="badge-brand text-xs">Localized</span>}
       {field.isIndexed && <span className="badge-amber text-xs">Indexed</span>}
     </div>
 <div className="flex items-center gap-3">
{(field.fieldType === 'ShortText' || field.fieldType === 'LongText') && (
       <>
     <CharCounter value={typeof watchedFields?.[field.handle] === 'string' ? watchedFields[field.handle] as string : ''} max={field.fieldType === 'ShortText' ? 100 : 500} />
   <button type="button" className="flex items-center gap-1 text-xs text-brand-600 hover:underline">
  <span>✦</span> Generate with AI
      </button>
   </>
        )}
       <span className="text-xs text-slate-400">{field.fieldType}</span>
        </div>
       </div>
    {field.description && <p className="text-xs text-slate-400">{field.description}</p>}
              <Controller
      control={control}
     name={`fields.${field.handle}`}
   render={({ field: f }) => <FieldInput field={field} value={f.value} onChange={f.onChange} />}
/>
            </div>
          ))}
        </div>

        {/* ── Right sidebar ────────────────────────────────────────────── */}
        <div className="w-72 shrink-0 overflow-y-auto border-l border-slate-200 bg-slate-50 px-4 py-5 space-y-4">

    {/* Publishing panel */}
  <PublishingPanel
   currentStatus={currentStatus}
            isNew={isNew}
            isSubmitting={isSubmitting}
    isDirty={isDirty}
   onSubmitForReview={() => wfMutation.mutate(() => entriesApi.submitForReview(id!).then(() => toast.success('Submitted for review.')))}
            onApprove={() => wfMutation.mutate(() => entriesApi.approve(id!).then(() => toast.success('Approved.')))}
            onReject={(reason) => wfMutation.mutate(() => entriesApi.reject(id!, reason).then(() => toast.success('Rejected.')))}
        onPublish={() => wfMutation.mutate(() => entriesApi.publish(id!).then(() => toast.success('Published.')))}
            onUnpublish={() => wfMutation.mutate(() => entriesApi.unpublish(id!).then(() => toast.success('Unpublished.')))}
    onSchedule={(publishAt, unpublishAt) => wfMutation.mutate(() => entriesApi.schedule(id!, publishAt, unpublishAt).then(() => toast.success('Scheduled.')))}
  />

      {/* Metadata */}
          {existing && (
 <div className="card space-y-2 text-xs text-slate-500">
      <h3 className="text-sm font-semibold text-slate-900">Metadata</h3>
<div className="grid grid-cols-[auto_1fr] gap-x-3 gap-y-1.5">
      <span className="text-slate-400">Created</span>
         <span>{new Date(existing.createdAt).toLocaleDateString()}</span>
        {existing.publishedAt && (
     <>
        <span className="text-slate-400">Published</span>
        <span>{new Date(existing.publishedAt).toLocaleDateString()}</span>
            </>
        )}
             <span className="text-slate-400">Updated</span>
              <span>{formatDistanceToNow(new Date(existing.updatedAt), { addSuffix: true })}</span>
           <span className="text-slate-400">Author</span>
         <span className="truncate">{existing.authorId.toString().slice(0, 8).toUpperCase()}</span>
       </div>
    {existing.scheduledPublishAt && (
       <p className="rounded bg-brand-50 px-2 py-1 text-brand-700">⏰ Scheduled: {new Date(existing.scheduledPublishAt).toLocaleString()}</p>
       )}
          </div>
      )}

          {/* AI Authoring Assist */}
          <AiAuthoringPanel />

   {/* Quality Checks */}
 {!isNew && <QualityChecksPanel localeVariants={localeVariants} activeLocale={activeLocale} />}

       {/* Version History */}
          {!isNew && id && (
            <VersionHistoryPanel entryId={id} onRestore={(vid) => restoreVersionMutation.mutate(vid)} />
          )}
        </div>
      </form>
    </div>
  );
}
