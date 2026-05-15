import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm, Controller } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import toast from 'react-hot-toast';
import { componentsApi } from '@/api/components';
import type { ComponentItemDto } from '@/types';
import { ApiError } from '@/api/client';
import { formatDistanceToNow } from 'date-fns';
import { FieldInput, CharCounter } from '@/components/fields/FieldInput';

// ─── Schema ───────────────────────────────────────────────────────────────────

const schema = z.object({
  slug: z.string()
    .min(1, 'Slug is required')
    .max(200, 'Slug must be 200 characters or fewer')
    .regex(/^[a-z0-9]+(?:-[a-z0-9]+)*$/, 'Slug may only contain lowercase letters, numbers, and hyphens'),
  fieldsJson: z.record(z.unknown()),
});

type ItemForm = z.infer<typeof schema>;

// ─── Status badge map ─────────────────────────────────────────────────────────

const STATUS_BADGE: Record<string, string> = {
  Draft: 'badge-slate',
  Published: 'badge-green',
  Archived: 'badge-red',
};

// ─── Page ─────────────────────────────────────────────────────────────────────

export default function ComponentItemEditorPage() {
  const { id: componentId, itemId } = useParams<{ id: string; itemId: string }>();
  const navigate = useNavigate();
  const qc = useQueryClient();
  const isNew = itemId === 'new';
  const [confirmDelete, setConfirmDelete] = useState(false);

  const { data: comp } = useQuery({
    queryKey: ['component', componentId],
    queryFn: () => componentsApi.getById(componentId!),
    enabled: !!componentId,
  });

  const { data: item, isLoading: itemLoading } = useQuery({
    queryKey: ['component-item', componentId, itemId],
    queryFn: () => componentsApi.getItem(componentId!, itemId!),
    enabled: !!componentId && !!itemId && !isNew,
  });

  const {
    register,
    handleSubmit,
    reset,
    watch,
    control,
    formState: { errors, isDirty, isSubmitting },
  } = useForm<ItemForm>({
    resolver: zodResolver(schema),
    defaultValues: { slug: '', fieldsJson: {} },
  });

  useEffect(() => {
    if (item) {
      reset({ slug: item.slug, fieldsJson: item.fieldsJson });
    }
  }, [item, reset]);

  const createMutation = useMutation({
    mutationFn: (data: ItemForm) =>
      componentsApi.createItem(componentId!, { slug: data.slug, fieldsJson: data.fieldsJson }),
    onSuccess: (created) => {
      toast.success('Item created.');
      void qc.invalidateQueries({ queryKey: ['component-items', componentId] });
      navigate(`/components/${componentId}/items/${created.id}`, { replace: true });
    },
    onError: (err) =>
      toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Create failed.'),
  });

  const updateMutation = useMutation({
    mutationFn: (data: ItemForm) =>
      componentsApi.updateItem(componentId!, itemId!, { fieldsJson: data.fieldsJson }),
    onSuccess: () => {
      toast.success('Item saved.');
      void qc.invalidateQueries({ queryKey: ['component-item', componentId, itemId] });
    },
    onError: (err) =>
      toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Save failed.'),
  });

  const publishMutation = useMutation({
    mutationFn: () => componentsApi.publishItem(componentId!, itemId!),
    onSuccess: () => {
      toast.success('Item published.');
      void qc.invalidateQueries({ queryKey: ['component-item', componentId, itemId] });
    },
    onError: (err) =>
      toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Publish failed.'),
  });

  const deleteMutation = useMutation({
    mutationFn: () => componentsApi.deleteItem(componentId!, itemId!),
    onSuccess: () => {
      toast.success('Item deleted.');
      navigate(`/components/${componentId}/items`, { replace: true });
    },
    onError: (err) =>
      toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Delete failed.'),
  });

  const onSubmit = (data: ItemForm) => {
    if (isNew) createMutation.mutate(data);
    else updateMutation.mutate(data);
  };

  const slugValue = watch('slug');
  const currentStatus = (item?.status ?? 'Draft') as ComponentItemDto['status'];

  if (!isNew && itemLoading) {
    return (
      <div className="space-y-4">
        {Array.from({ length: 5 }).map((_, i) => (
          <div key={i} className="h-10 animate-pulse rounded-lg bg-slate-100" />
        ))}
      </div>
    );
  }

  return (
    <div className="flex min-h-0 flex-col">
      {/* ── Top bar ────────────────────────────────────────────────────────── */}
      <div className="sticky top-0 z-10 border-b border-slate-200 bg-white">
        <div className="flex items-center justify-between px-6 py-3">
          <nav className="flex items-center gap-1.5 text-sm text-slate-500">
            <button onClick={() => navigate('/components')} className="hover:text-slate-700">
              Components
            </button>
            <span>/</span>
            <button
              onClick={() => navigate(`/components/${componentId}/items`)}
              className="hover:text-slate-700"
            >
              {comp?.name ?? '…'}
            </button>
            {(slugValue || item?.slug) && (
              <>
                <span>/</span>
                <span className="max-w-[200px] truncate font-mono text-xs font-medium text-slate-900">
                  {slugValue || item?.slug}
                </span>
              </>
            )}
          </nav>
          <div className="flex items-center gap-2">
            {!isNew && (
              <span className={`${STATUS_BADGE[currentStatus] ?? 'badge-slate'} text-xs`}>
                {currentStatus}
              </span>
            )}
            <button
              form="item-form"
              type="submit"
              disabled={isSubmitting || (!isDirty && !isNew)}
              className="btn-primary text-xs"
            >
              {isSubmitting ? 'Saving…' : isNew ? 'Create Item' : 'Save Draft'}
            </button>
            {!isNew && currentStatus !== 'Published' && (
              <button
                type="button"
                onClick={() => publishMutation.mutate()}
                disabled={publishMutation.isPending || isDirty}
                className="btn-secondary text-xs"
                title={isDirty ? 'Save changes before publishing' : undefined}
              >
                {publishMutation.isPending ? 'Publishing…' : 'Publish'}
              </button>
            )}
          </div>
        </div>
      </div>

      {/* ── Body ─────────────────────────────────────────────────────────── */}
      <form
        id="item-form"
        onSubmit={handleSubmit(onSubmit)}
        className="flex flex-1 gap-0 overflow-hidden"
      >
        {/* ── Main content ──────────────────────────────────────────────── */}
        <div className="flex-1 overflow-y-auto px-6 py-6 space-y-5">
          {/* Slug */}
          <div className="card space-y-2">
            <div className="flex items-center justify-between">
              <label className="form-label mb-0">
                Slug <span className="text-red-500">*</span>
              </label>
              {errors.slug && <p className="form-error text-xs">{errors.slug.message}</p>}
            </div>
            <p className="text-xs text-slate-400">
              URL-friendly identifier — lowercase letters, numbers, and hyphens only.
            </p>
            <input
              className="form-input font-mono"
              {...register('slug')}
              placeholder="e.g. summer-campaign-hero"
              disabled={!isNew}
            />
          </div>

          {/* Dynamic fields — one card per field */}
          {(comp?.fields ?? []).map((field) => (
            <div key={field.id} className="card space-y-2">
              <div className="flex items-center justify-between">
                <div className="flex items-center gap-2">
                  <label className="form-label mb-0">
                    {field.label}
                    {field.isRequired && <span className="ml-1 text-red-500">*</span>}
                  </label>
                  {field.isIndexed && <span className="badge-amber text-xs">Indexed</span>}
                </div>
                <div className="flex items-center gap-3">
                  {(field.fieldType === 'ShortText' || field.fieldType === 'LongText') && (
                    <Controller
                      control={control}
                      name={`fieldsJson.${field.handle}`}
                      render={({ field: f }) => (
                        <CharCounter
                          value={typeof f.value === 'string' ? f.value : ''}
                          max={field.fieldType === 'ShortText' ? 100 : 500}
                        />
                      )}
                    />
                  )}
                  <span className="text-xs text-slate-400">{field.fieldType}</span>
                </div>
              </div>
              {field.description && (
                <p className="text-xs text-slate-400">{field.description}</p>
              )}
              <Controller
                control={control}
                name={`fieldsJson.${field.handle}`}
                render={({ field: f }) => (
                  <FieldInput field={field} value={f.value} onChange={f.onChange} />
                )}
              />
            </div>
          ))}

          {(comp?.fields.length ?? 0) === 0 && (
            <div className="card">
              <p className="text-sm text-slate-400">
                This component has no fields defined.{' '}
                <button
                  type="button"
                  onClick={() => navigate(`/components/${componentId}/edit`)}
                  className="text-brand-600 underline"
                >
                  Edit schema →
                </button>
              </p>
            </div>
          )}


        </div>

        {/* ── Right sidebar ──────────────────────────────────────────────── */}
        <div className="w-72 shrink-0 overflow-y-auto border-l border-slate-200 bg-slate-50 px-4 py-5 space-y-4">
          {/* Status + actions */}
          <div className="card space-y-3">
            <div className="flex items-center justify-between">
              <h3 className="text-sm font-semibold text-slate-900">Status</h3>
              <span className={`${STATUS_BADGE[currentStatus] ?? 'badge-slate'} text-xs`}>
                {currentStatus}
              </span>
            </div>
            {!isNew && item && (
              <div className="grid grid-cols-[auto_1fr] gap-x-3 gap-y-1.5 text-xs">
                <span className="text-slate-400">Updated</span>
                <span className="text-slate-500">
                  {formatDistanceToNow(new Date(item.updatedAt), { addSuffix: true })}
                </span>
                <span className="text-slate-400">Created</span>
                <span className="text-slate-500">{new Date(item.createdAt).toLocaleDateString()}</span>
              </div>
            )}
            <div className="border-t border-slate-100 pt-3 space-y-2">
              <button
                form="item-form"
                type="submit"
                disabled={isSubmitting || (!isDirty && !isNew)}
                className="btn-secondary w-full justify-center text-sm"
              >
                {isSubmitting ? 'Saving…' : isNew ? 'Create Item' : 'Save Draft'}
              </button>
              {!isNew && currentStatus !== 'Published' && (
                <button
                  type="button"
                  onClick={() => publishMutation.mutate()}
                  disabled={publishMutation.isPending || isDirty}
                  className="w-full rounded-lg bg-green-600 px-3 py-2 text-xs font-medium text-white hover:bg-green-700 disabled:opacity-50"
                  title={isDirty ? 'Save changes before publishing' : undefined}
                >
                  {publishMutation.isPending ? 'Publishing…' : 'Publish Now'}
                </button>
              )}
              {!isNew && currentStatus === 'Published' && (
                <button
                  type="button"
                  onClick={() => publishMutation.mutate()}
                  disabled={publishMutation.isPending}
                  className="w-full rounded-lg border border-slate-200 bg-white px-3 py-2 text-xs font-medium text-slate-700 hover:bg-slate-50"
                >
                  Unpublish
                </button>
              )}
            </div>
          </div>

          {/* Component info */}
          {comp && (
            <div className="card space-y-3">
              <h3 className="text-sm font-semibold text-slate-900">Component</h3>
              <div className="grid grid-cols-[auto_1fr] gap-x-3 gap-y-1.5 text-xs">
                <span className="text-slate-400">Type</span>
                <span className="font-semibold text-slate-700">{comp.name}</span>
                <span className="text-slate-400">Category</span>
                <span className="text-slate-500">{comp.category}</span>
                <span className="text-slate-400">Fields</span>
                <span className="text-slate-500">{comp.fields.length}</span>
              </div>
              <button
                type="button"
                onClick={() => navigate(`/components/${componentId}/edit`)}
                className="text-xs text-brand-600 hover:underline"
              >
                Edit schema →
              </button>
            </div>
          )}

          {/* Danger zone */}
          {!isNew && (
            <div className="rounded-lg border border-red-200 bg-white p-4 space-y-2">
              <h3 className="text-xs font-semibold text-red-600">Danger Zone</h3>
              {!confirmDelete ? (
                <button
                  type="button"
                  onClick={() => setConfirmDelete(true)}
                  className="w-full rounded border border-red-200 bg-red-50 px-3 py-1.5 text-xs font-medium text-red-600 hover:bg-red-100"
                >
                  Delete this Item
                </button>
              ) : (
                <div className="space-y-2">
                  <p className="text-xs text-red-600">Are you sure? This cannot be undone.</p>
                  <div className="flex gap-2">
                    <button
                      type="button"
                      onClick={() => setConfirmDelete(false)}
                      className="flex-1 rounded border border-slate-200 bg-white px-3 py-1.5 text-xs font-medium text-slate-600 hover:bg-slate-50"
                    >
                      Cancel
                    </button>
                    <button
                      type="button"
                      onClick={() => deleteMutation.mutate()}
                      disabled={deleteMutation.isPending}
                      className="flex-1 rounded bg-red-600 px-3 py-1.5 text-xs font-medium text-white hover:bg-red-700 disabled:opacity-50"
                    >
                      {deleteMutation.isPending ? 'Deleting…' : 'Delete'}
                    </button>
                  </div>
                </div>
              )}
            </div>
          )}
        </div>
      </form>
    </div>
  );
}
