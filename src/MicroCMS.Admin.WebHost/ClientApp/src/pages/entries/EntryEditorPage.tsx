import { useEffect, useState } from 'react';
import { useNavigate, useParams, useSearchParams } from 'react-router-dom';
import { useForm, Controller } from 'react-hook-form';
import type { Control } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { entriesApi } from '@/api/entries';
import { contentTypesApi } from '@/api/contentTypes';
import { aiWritingApi } from '@/api/ai';
import type { EntryVersion, EntryStatus, FieldDefinitionDto } from '@/types';
import { ApiError } from '@/api/client';
import { formatDistanceToNow } from 'date-fns';
import { useSite } from '@/contexts/SiteContext';
import { orderedGroups } from '@/pages/content-types/schemaTab.helpers';
import { DEFAULT_GROUP } from '@/pages/content-types/schemaTab.types';
import { FieldInput, CharCounter } from '@/components/fields/FieldInput';
import { AiBudgetBanner } from '@/components/ai/AiBudgetBanner';

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

// ─── Field Group Section (collapsible accordion) ──────────────────────────────

function FieldGroupSection({
  groupName,
  groupFields,
  watchedFields,
  control,
  contentTypeId,
}: {
  groupName: string;
  groupFields: FieldDefinitionDto[];
  watchedFields: Record<string, unknown>;
  control: Control<FormValues>;
  contentTypeId?: string;
}) {
  const [collapsed, setCollapsed] = useState(false);
  return (
    <div className="rounded-lg border border-slate-200 overflow-hidden">
      <button
        type="button"
        onClick={() => setCollapsed((c) => !c)}
        className="flex w-full items-center justify-between bg-slate-50 px-4 py-2.5 text-left hover:bg-slate-100 transition-colors border-b border-slate-200"
      >
        <span className="text-xs font-semibold uppercase tracking-wide text-slate-600">{groupName}</span>
        <span className="text-xs text-slate-400 select-none">{collapsed ? '▶' : '▼'}</span>
      </button>
      {!collapsed && (
        <div className="space-y-4 p-4">
          {groupFields.map((field) => (
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
                    <CharCounter value={typeof watchedFields?.[field.handle] === 'string' ? watchedFields[field.handle] as string : ''} max={field.fieldType === 'ShortText' ? 100 : 500} />
                  )}
                  <span className="text-xs text-slate-400">{field.fieldType}</span>
                </div>
              </div>
              {field.description && <p className="text-xs text-slate-400">{field.description}</p>}
              <Controller
                control={control}
                name={`fields.${field.handle}`}
                render={({ field: f }) => <FieldInput field={field} value={f.value} onChange={f.onChange} contentTypeId={contentTypeId} />}
              />
            </div>
          ))}
        </div>
      )}
    </div>
  );
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

// ─── AI Authoring Assist Panel ────────────────────────────────────────────────

function AiAuthoringPanel({ entryId, contentTypeId, fields }: { entryId?: string; contentTypeId?: string; fields: Record<string, unknown> }) {
  const [showDraftModal, setShowDraftModal] = useState(false);
  const [draftPrompt, setDraftPrompt] = useState('');

  const draftMutation = useMutation({
    mutationFn: () => aiWritingApi.generateDraft(contentTypeId ?? '', draftPrompt),
    onSuccess: () => { toast.success('Draft generated — reload fields to see changes.'); setShowDraftModal(false); },
    onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Draft generation failed.'),
  });

  const rewriteMutation = useMutation({
    mutationFn: () => {
      if (!entryId) throw new Error('Save the entry first.');
      const firstTextField = Object.keys(fields).find((k) => typeof fields[k] === 'string');
      if (!firstTextField) throw new Error('No text field found.');
      return aiWritingApi.rewrite(entryId, firstTextField, 'Improve clarity and tone');
    },
    onSuccess: () => toast.success('Use the field-level ✦ Generate button to preview the rewrite.'),
    onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Rewrite failed.'),
  });

  const summarizeMutation = useMutation({
    mutationFn: () => {
      if (!entryId) throw new Error('Save the entry first.');
      const firstTextField = Object.keys(fields).find((k) => typeof fields[k] === 'string');
      if (!firstTextField) throw new Error('No text field found.');
      return aiWritingApi.summarize(entryId, firstTextField);
    },
    onSuccess: (result) => { if (result) toast.success(`Summary: ${result.content.slice(0, 120)}…`); },
    onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Summarize failed.'),
  });

  const actions = [
    { icon: '✦', label: 'Generate Draft', desc: 'Create body from brief', onClick: () => setShowDraftModal(true), pending: draftMutation.isPending },
    { icon: '↺', label: 'Rewrite / Tone', desc: 'Format, friendly, shorter…', onClick: () => rewriteMutation.mutate(), pending: rewriteMutation.isPending },
    { icon: '≡', label: 'Summarize', desc: 'TL;DR, abstract, social', onClick: () => summarizeMutation.mutate(), pending: summarizeMutation.isPending },
    { icon: '◈', label: 'SEO Suggestions', desc: 'Use ✦ Generate on SEO fields', onClick: () => toast('Open the SEO field\'s ✦ Generate button.'), pending: false },
    { icon: '⇄', label: 'Translate', desc: 'Switch locale + use ✦ Generate', onClick: () => toast('Switch locale above then use ✦ Generate on each field.'), pending: false },
  ];

  return (
    <div className="card space-y-3">
      <div className="flex items-center gap-2">
        <span className="flex h-6 w-6 items-center justify-center rounded-full bg-brand-600 text-xs text-white">✦</span>
        <h3 className="text-sm font-semibold text-slate-900">AI Authoring Assist</h3>
      </div>
      <div className="space-y-1.5">
        {actions.map((action) => (
          <button
            key={action.label}
            type="button"
            onClick={action.onClick}
            disabled={action.pending}
            className="flex w-full items-start gap-2.5 rounded-lg p-2 text-left hover:bg-slate-50 disabled:opacity-50"
          >
            <span className="mt-0.5 text-sm text-brand-500">{action.icon}</span>
            <div>
              <p className="text-xs font-medium text-slate-800">{action.label}</p>
              <p className="text-xs text-slate-400">{action.pending ? 'Working…' : action.desc}</p>
            </div>
          </button>
        ))}
      </div>
      <p className="border-t border-slate-100 pt-2 text-xs text-slate-400">
        Powered by <span className="font-medium text-slate-600">OpenAI</span>
      </p>

      {showDraftModal && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
          <div className="card mx-4 w-full max-w-md space-y-4">
            <div className="flex items-center justify-between">
              <h3 className="text-sm font-semibold text-slate-900">✦ Generate Draft</h3>
              <button type="button" onClick={() => setShowDraftModal(false)} className="text-slate-400 hover:text-slate-700">✕</button>
            </div>
            <div className="space-y-1.5">
              <label className="form-label text-xs">Describe the content to create</label>
              <textarea
                className="form-input w-full resize-none"
                rows={4}
                placeholder="e.g. A blog post about the benefits of headless CMS for e-commerce teams…"
                value={draftPrompt}
                onChange={(e) => setDraftPrompt(e.target.value)}
              />
            </div>
            {draftMutation.isError && (
              <p className="text-xs text-red-600">{draftMutation.error instanceof Error ? draftMutation.error.message : 'Failed.'}</p>
            )}
            <div className="flex justify-end gap-2 border-t border-slate-100 pt-3">
              <button type="button" onClick={() => setShowDraftModal(false)} className="btn-secondary text-xs">Cancel</button>
              <button
                type="button"
                onClick={() => draftMutation.mutate()}
                disabled={draftMutation.isPending || !draftPrompt.trim()}
                className="rounded-lg bg-brand-600 px-3 py-2 text-xs font-medium text-white hover:bg-brand-700 disabled:opacity-50"
              >
                {draftMutation.isPending ? 'Generating…' : 'Generate'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

// ─── Quality Checks Panel ─────────────────────────────────────────────────────

function QualityChecksPanel({
  localeVariants,
  activeLocale,
  siteLocales,
  fields,
  fieldDefs,
}: {
  localeVariants: string[];
  activeLocale: string;
  siteLocales: string[];
  fields: Record<string, unknown>;
  fieldDefs: FieldDefinitionDto[];
}) {
  const missingLocales = siteLocales.filter((l) => !localeVariants.includes(l) && l !== activeLocale);

  const seoFields = fieldDefs.filter((f) => /seo|meta/i.test(f.handle));
  const seoEmpty = seoFields.some((f) => !fields[f.handle]);

  const richTextFields = fieldDefs.filter((f) => f.fieldType === 'RichText' || f.fieldType === 'LongText');
  const wordCount = richTextFields.reduce((sum, f) => {
    const text = typeof fields[f.handle] === 'string'
      ? (fields[f.handle] as string).replace(/<[^>]+>/g, ' ').trim()
      : '';
    return sum + (text ? text.split(/\s+/).length : 0);
  }, 0);
  const wordCountOk = wordCount === 0 || wordCount >= 100;

  const piiPattern = /\b[A-Z0-9._%+\-]+@[A-Z0-9.\-]+\.[A-Z]{2,}/i;
  const hasPii = fieldDefs.some((f) => piiPattern.test(String(fields[f.handle] ?? '')));

  const checks: { label: string; status: 'pass' | 'warn'; detail: string }[] = [
    {
      label: 'Locale coverage',
      status: missingLocales.length === 0 ? 'pass' : 'warn',
      detail: missingLocales.length > 0 ? `Missing: ${missingLocales.join(', ')}` : 'All locales present',
    },
    {
      label: 'SEO fields',
      status: seoEmpty ? 'warn' : 'pass',
      detail: seoFields.length === 0 ? 'No SEO fields defined' : seoEmpty ? 'Some fields empty' : 'Filled',
    },
    {
      label: 'Word count',
      status: wordCountOk ? 'pass' : 'warn',
      detail: wordCount === 0 ? 'No text yet' : `${wordCount} words${wordCount < 100 ? ' (< 100)' : ''}`,
    },
    {
      label: 'No PII detected',
      status: hasPii ? 'warn' : 'pass',
      detail: hasPii ? 'Possible email in content' : '',
    },
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
            {c.detail && <span className="ml-auto text-slate-400 truncate max-w-[110px]" title={c.detail}>{c.detail}</span>}
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
  // Content type is locked when it was pre-supplied via ?contentTypeId= in the URL.
  // This happens when navigating from a content type's entry list or from a component.
  const lockedContentTypeId = isNew ? (searchParams.get('contentTypeId') ?? '') : '';
  const isContentTypeLocked = isNew && lockedContentTypeId !== '';
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
  defaultValues: { slug: '', contentTypeId: lockedContentTypeId, locale: 'en', fields: {} },
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

  // ── Slug prefix ────────────────────────────────────────────────────────
  // Format: {domain}/{contentTypeHandle}/
  // Domain: custom domain when set, otherwise the site handle (never a dummy value).
  const siteDomain = selectedSite?.customDomain ?? selectedSite?.handle ?? '…';
  const contentTypeHandle = selectedContentType?.handle ?? '';
  const domainPrefix = contentTypeHandle
    ? `${siteDomain}/${contentTypeHandle}/`
    : `${siteDomain}/`;

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
       <button onClick={() => navigate(`/content-types/${existing?.contentTypeId ?? selectedContentTypeId}`)} className="hover:text-slate-700">{contentTypeName}</button>
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
            {isContentTypeLocked ? (
    // Pre-selected via URL — show a locked read-only badge so the user
          // knows which type they're creating for, but cannot change it.
       <div className="flex items-center gap-2 rounded-lg border border-slate-200 bg-slate-50 px-3 py-2">
     {contentTypes ? (
           <span className="flex-1 text-sm font-medium text-slate-800">
         {contentTypes.items.find((ct) => ct.id === lockedContentTypeId)?.displayName ?? lockedContentTypeId}
         </span>
      ) : (
 <span className="flex-1 h-4 w-32 animate-pulse rounded bg-slate-200" />
     )}
 <span className="rounded-full bg-slate-200 px-2 py-0.5 text-xs font-medium text-slate-500">
          Locked
  </span>
              </div>
     ) : (
              <>
        <select className="form-input" {...register('contentTypeId')}>
      <option value="">Select content type…</option>
   {contentTypes?.items.map((ct) => (
   <option key={ct.id} value={ct.id}>{ct.displayName}</option>
))}
            </select>
        {errors.contentTypeId && <p className="form-error">{errors.contentTypeId.message}</p>}
       </>
    )}
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

          {/* Dynamic fields – grouped accordion */}
          {selectedContentType && (() => {
            const allFields = selectedContentType.fields;
            const groups = orderedGroups(allFields.map((f) => ({ groupName: f.groupName ?? DEFAULT_GROUP })));
            return groups.map((group) => (
              <FieldGroupSection
                key={group}
                groupName={group}
                groupFields={allFields.filter((f) => (f.groupName ?? DEFAULT_GROUP) === group)}
                watchedFields={watchedFields ?? {}}
                control={control}
                contentTypeId={selectedContentTypeId}
              />
            ));
          })()}
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

          {/* AI Budget */}
          <AiBudgetBanner />

          {/* AI Authoring Assist */}
          <AiAuthoringPanel entryId={id} contentTypeId={selectedContentTypeId} fields={watchedFields ?? {}} />

   {/* Quality Checks */}
          {!isNew && (
            <QualityChecksPanel
              localeVariants={localeVariants}
              activeLocale={activeLocale}
              siteLocales={localeVariants}
              fields={watchedFields ?? {}}
              fieldDefs={selectedContentType?.fields ?? []}
            />
          )}

       {/* Version History */}
          {!isNew && id && (
            <VersionHistoryPanel entryId={id} onRestore={(vid) => restoreVersionMutation.mutate(vid)} />
          )}
        </div>
      </form>
    </div>
  );
}
