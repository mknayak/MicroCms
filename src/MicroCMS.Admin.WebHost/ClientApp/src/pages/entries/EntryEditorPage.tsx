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
import type { EntryVersion, FieldDefinition } from '@/types';
import { ApiError } from '@/api/client';
import { formatDistanceToNow } from 'date-fns';

// ─── Form schema ──────────────────────────────────────────────────────────────

const baseSchema = z.object({
  slug: z
    .string()
    .min(1, 'Slug is required')
    .regex(/^[a-z0-9]+(?:-[a-z0-9]+)*$/, 'Slug must be lowercase with hyphens'),
  contentTypeId: z.string().min(1, 'Content type is required'),
  locale: z.string().min(1, 'Locale is required'),
  fields: z.record(z.unknown()),
});

type FormValues = z.infer<typeof baseSchema>;

// ─── Version History Drawer ───────────────────────────────────────────────────

function VersionDrawer({
  entryId,
  onClose,
  onRestore,
}: {
  entryId: string;
  onClose: () => void;
  onRestore: (versionId: string) => void;
}) {
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
        {isLoading ? (
          <div className="space-y-3">
            {Array.from({ length: 4 }).map((_, i) => <div key={i} className="h-16 animate-pulse rounded bg-slate-100" />)}
          </div>
        ) : (
          <ul className="space-y-2">
            {(data ?? []).map((v: EntryVersion) => (
              <li key={v.id} className="rounded-lg border border-slate-200 p-3">
                <div className="flex items-center justify-between">
                  <span className="text-xs font-semibold text-slate-700">v{v.version}</span>
                  <button
                    onClick={() => onRestore(v.id)}
                    className="text-xs text-brand-600 hover:underline"
                  >
                    Restore
                  </button>
                </div>
                <p className="mt-1 text-xs text-slate-500">
                  {v.createdBy} · {formatDistanceToNow(new Date(v.createdAt), { addSuffix: true })}
                </p>
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

function FieldInput({
  field,
  value,
  onChange,
}: {
  field: FieldDefinition;
  value: unknown;
  onChange: (val: unknown) => void;
}) {
  switch (field.type) {
    case 'richtext':
      return (
        <RichTextEditor
          value={typeof value === 'string' ? value : ''}
          onChange={onChange}
          placeholder={`Write ${field.name}…`}
        />
      );
    case 'boolean':
      return (
        <input
          type="checkbox"
          checked={Boolean(value)}
          onChange={(e) => onChange(e.target.checked)}
          className="h-4 w-4 rounded border-slate-300 text-brand-600"
        />
      );
    case 'number':
      return (
        <input
          type="number"
          value={typeof value === 'number' ? value : ''}
          onChange={(e) => onChange(e.target.valueAsNumber)}
          className="form-input"
        />
      );
    case 'date':
      return (
        <input
          type="datetime-local"
          value={typeof value === 'string' ? value : ''}
          onChange={(e) => onChange(e.target.value)}
          className="form-input"
        />
      );
    default:
      return (
        <input
          type="text"
          value={typeof value === 'string' ? value : ''}
          onChange={(e) => onChange(e.target.value)}
          className="form-input"
          placeholder={`Enter ${field.name}…`}
        />
      );
  }
}

// ─── Page ─────────────────────────────────────────────────────────────────────

export default function EntryEditorPage() {
  const { id } = useParams();
  const [searchParams] = useSearchParams();
  const isNew = !id;
  const navigate = useNavigate();
  const qc = useQueryClient();
  const [showVersions, setShowVersions] = useState(false);
  const [locale, setLocale] = useState('en');

  const { data: existing } = useQuery({
    queryKey: ['entries', id],
    queryFn: () => entriesApi.getById(id!, locale),
    enabled: !isNew,
  });

  const { data: contentTypes } = useQuery({
    queryKey: ['content-types'],
    queryFn: () => contentTypesApi.list({ pageSize: 100 }),
  });

  const {
    register,
    control,
    handleSubmit,
    watch,
    setValue,
    formState: { errors, isSubmitting, isDirty },
  } = useForm<FormValues>({
    resolver: zodResolver(baseSchema),
    defaultValues: {
      slug: '',
      contentTypeId: searchParams.get('contentTypeId') ?? '',
      locale: 'en',
      fields: {},
    },
  });

  const selectedContentTypeId = watch('contentTypeId');
  const { data: selectedContentType } = useQuery({
    queryKey: ['content-types', selectedContentTypeId],
    queryFn: () => contentTypesApi.getById(selectedContentTypeId),
    enabled: Boolean(selectedContentTypeId),
  });

  // Populate form on edit
  useEffect(() => {
    if (existing) {
      setValue('slug', existing.slug);
      setValue('contentTypeId', existing.contentTypeId);
      setValue('locale', existing.locale);
      setValue('fields', existing.fields);
    }
  }, [existing, setValue]);

  const saveMutation = useMutation({
    mutationFn: (values: FormValues) =>
      isNew
        ? entriesApi.create(values)
        : entriesApi.update(id!, { slug: values.slug, fields: values.fields }),
    onSuccess: (saved) => {
      toast.success(isNew ? 'Entry created.' : 'Entry saved.');
      void qc.invalidateQueries({ queryKey: ['entries'] });
      if (isNew) navigate(`/entries/${saved.id}/edit`);
    },
    onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Save failed.'),
  });

  const publishMutation = useMutation({
    mutationFn: () => entriesApi.publish(id!),
    onSuccess: () => {
      toast.success('Entry published.');
      void qc.invalidateQueries({ queryKey: ['entries', id] });
    },
    onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Publish failed.'),
  });

  const restoreVersionMutation = useMutation({
    mutationFn: (versionId: string) => entriesApi.restoreVersion(id!, versionId),
    onSuccess: () => {
      toast.success('Version restored.');
      void qc.invalidateQueries({ queryKey: ['entries', id] });
      setShowVersions(false);
    },
  });

  const LOCALES = ['en', 'de', 'fr', 'es', 'pt', 'ja', 'zh'];

  return (
    <div className="mx-auto max-w-4xl space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <button onClick={() => navigate('/entries')} className="btn-secondary">
            ← Back
          </button>
          <h1 className="text-2xl font-bold text-slate-900">
            {isNew ? 'New Entry' : existing?.title ?? '…'}
          </h1>
          {existing && (
            <span className={`badge-${
              existing.status === 'Published' ? 'green' : existing.status === 'Draft' ? 'slate' : 'amber'
            }`}>
              {existing.status}
            </span>
          )}
        </div>
        <div className="flex items-center gap-2">
          {!isNew && (
            <button
              onClick={() => setShowVersions((v) => !v)}
              className="btn-secondary"
            >
              History
            </button>
          )}
          {!isNew && existing?.status === 'Draft' && (
            <button
              onClick={() => publishMutation.mutate()}
              disabled={publishMutation.isPending}
              className="btn-primary"
            >
              {publishMutation.isPending ? 'Publishing…' : 'Publish'}
            </button>
          )}
        </div>
      </div>

      <form onSubmit={handleSubmit((v) => saveMutation.mutate(v))} className="grid grid-cols-3 gap-6">
        {/* Main content */}
        <div className="col-span-2 space-y-5">
          {/* Slug */}
          <div className="card space-y-4">
            <div className="grid grid-cols-2 gap-4">
              {isNew && (
                <div>
                  <label className="form-label">Content Type</label>
                  <select className="form-input mt-1" {...register('contentTypeId')}>
                    <option value="">Select a content type…</option>
                    {contentTypes?.items.map((ct) => (
                      <option key={ct.id} value={ct.id}>{ct.name}</option>
                    ))}
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
          </div>

          {/* Dynamic fields */}
          {selectedContentType?.fields.map((field) => (
            <div key={field.id} className="card space-y-2">
              <div className="flex items-center justify-between">
                <label className="form-label">
                  {field.name}
                  {field.required && <span className="ml-1 text-red-500">*</span>}
                  {field.localized && <span className="ml-2 badge-brand">Localized</span>}
                </label>
                <span className="text-xs text-slate-400">{field.type}</span>
              </div>
              <Controller
                control={control}
                name={`fields.${field.apiKey}`}
                render={({ field: f }) => (
                  <FieldInput
                    field={field}
                    value={f.value}
                    onChange={f.onChange}
                  />
                )}
              />
            </div>
          ))}
        </div>

        {/* Sidebar */}
        <div className="col-span-1 space-y-4">
          {/* Locale */}
          <div className="card space-y-3">
            <h3 className="text-sm font-semibold text-slate-900">Locale</h3>
            <select
              value={locale}
              onChange={(e) => {
                setLocale(e.target.value);
                setValue('locale', e.target.value);
              }}
              className="form-input"
            >
              {LOCALES.map((l) => <option key={l} value={l}>{l.toUpperCase()}</option>)}
            </select>
          </div>

          {/* Save / Publish actions */}
          <div className="card space-y-3">
            <h3 className="text-sm font-semibold text-slate-900">Actions</h3>
            <button
              type="submit"
              disabled={isSubmitting || !isDirty}
              className="btn-secondary w-full justify-center"
            >
              {isSubmitting ? 'Saving…' : 'Save Draft'}
            </button>
            {!isNew && existing?.status === 'Draft' && (
              <button
                type="button"
                onClick={() => entriesApi.submitForReview(id!).then(() => {
                  toast.success('Submitted for review.');
                  void qc.invalidateQueries({ queryKey: ['entries', id] });
                })}
                className="btn-secondary w-full justify-center"
              >
                Submit for Review
              </button>
            )}
          </div>

          {/* Metadata */}
          {existing && (
            <div className="card space-y-2 text-xs text-slate-500">
              <h3 className="text-sm font-semibold text-slate-900">Metadata</h3>
              <p>Created: {new Date(existing.createdAt).toLocaleDateString()}</p>
              <p>Updated: {formatDistanceToNow(new Date(existing.updatedAt), { addSuffix: true })}</p>
              <p>Author: {existing.authorName}</p>
              {existing.publishedAt && <p>Published: {new Date(existing.publishedAt).toLocaleDateString()}</p>}
            </div>
          )}
        </div>
      </form>

      {/* Version drawer */}
      {showVersions && id && (
        <VersionDrawer
          entryId={id}
          onClose={() => setShowVersions(false)}
          onRestore={(vid) => restoreVersionMutation.mutate(vid)}
        />
      )}
    </div>
  );
}
