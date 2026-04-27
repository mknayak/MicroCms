import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useForm, useFieldArray } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { contentTypesApi } from '@/api/contentTypes';
import type { FieldType } from '@/types';
import { ApiError } from '@/api/client';

// ─── Schema ───────────────────────────────────────────────────────────────────

const FIELD_TYPES: { value: FieldType; label: string }[] = [
  { value: 'ShortText',      label: 'Short Text' },
  { value: 'LongText',       label: 'Long Text' },
  { value: 'RichText',       label: 'Rich Text' },
  { value: 'Markdown',       label: 'Markdown' },
  { value: 'Integer',        label: 'Integer' },
  { value: 'Decimal',        label: 'Decimal' },
  { value: 'Boolean',        label: 'Boolean' },
  { value: 'DateTime',   label: 'Date & Time' },
  { value: 'Enum',     label: 'Select / Enum' },
  { value: 'Reference',    label: 'Reference' },
  { value: 'AssetReference', label: 'Asset' },
  { value: 'Json',           label: 'JSON' },
  { value: 'Component',      label: 'Component' },
  { value: 'Location',       label: 'Location' },
  { value: 'Color',    label: 'Color' },
];

const FIELD_TYPE_VALUES = FIELD_TYPES.map((ft) => ft.value) as [FieldType, ...FieldType[]];

const fieldSchema = z.object({
  id: z.string().optional(),
  name: z.string().min(1, 'Name is required'),
  apiKey: z.string().min(1, 'API key is required').regex(/^[a-z][a-zA-Z0-9_]*$/, 'Must start with lowercase letter, only letters/numbers/underscores'),
  type: z.enum(FIELD_TYPE_VALUES),
  required: z.boolean(),
  localized: z.boolean(),
});

const formSchema = z.object({
  name: z.string().min(1, 'Name is required'),
  apiKey: z.string().min(1, 'API key is required').regex(/^[a-z][a-zA-Z0-9_]*$/, 'Must start with lowercase letter, only letters/numbers/underscores'),
  description: z.string().optional(),
  isCollection: z.boolean(),
  fields: z.array(fieldSchema),
});

type FormValues = z.infer<typeof formSchema>;

// ─── Page ─────────────────────────────────────────────────────────────────────

export default function ContentTypeEditPage() {
  const { id } = useParams();
  const isNew = !id;
  const navigate = useNavigate();
  const qc = useQueryClient();
  const [activeFieldIdx, setActiveFieldIdx] = useState<number | null>(null);

  const { data: existing } = useQuery({
    queryKey: ['content-types', id],
    queryFn: () => contentTypesApi.getById(id!),
    enabled: !isNew,
  });

  const {
    register,
    control,
    handleSubmit,
    watch,
    setValue,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      name: '',
      apiKey: '',
      isCollection: true,
      fields: [],
    },
  });

  const { fields, append, remove, move } = useFieldArray({ control, name: 'fields' });

  // Populate on edit
  useEffect(() => {
    if (existing) {
      setValue('name', existing.displayName);
      setValue('apiKey', existing.handle);
      setValue('description', existing.description ?? '');
      setValue('isCollection', existing.status !== 'Archived'); // best-effort; edit page is cosmetic
      setValue('fields', existing.fields.map((f) => ({
        id: f.id,
        name: f.label,
        apiKey: f.handle,
    type: f.fieldType as FormValues['fields'][number]['type'],
    required: f.isRequired,
        localized: f.isLocalized,
      })));
    }
  }, [existing, setValue]);

  // Auto-generate API key from name
  const nameValue = watch('name');
  useEffect(() => {
    if (isNew && nameValue) {
      setValue(
        'apiKey',
        nameValue
          .toLowerCase()
          .replace(/[^a-z0-9 ]/g, '')
          .replace(/\s+/g, '_')
          .replace(/^[^a-z]/, (c) => `f_${c}`),
      );
    }
  }, [nameValue, isNew, setValue]);

  const mutation = useMutation({
    mutationFn: (values: FormValues) =>
      isNew
        ? contentTypesApi.create({
     siteId: '',
            handle: values.apiKey,
            displayName: values.name,
      description: values.description,
 })
        : contentTypesApi.update(id!, {
        displayName: values.name,
            description: values.description,
 fields: values.fields.map((f, idx) => ({
         id: f.id,
    handle: f.apiKey,
 label: f.name,
    fieldType: f.type,
   isRequired: f.required,
   isLocalized: f.localized,
    isUnique: false,
        sortOrder: idx,
         })),
   }),
    onSuccess: () => {
      toast.success(isNew ? 'Content type created.' : 'Content type updated.');
      void qc.invalidateQueries({ queryKey: ['content-types'] });
      navigate('/content-types');
    },
    onError: (err) => {
      toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Save failed.');
    },
  });

  const addField = () => {
    append({ name: '', apiKey: '', type: 'ShortText', required: false, localized: false });
    setActiveFieldIdx(fields.length);
  };

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      {/* Header */}
      <div className="flex items-center gap-3">
        <button onClick={() => navigate('/content-types')} className="btn-secondary">
          ← Back
        </button>
        <h1 className="text-2xl font-bold text-slate-900">
          {isNew ? 'New Content Type' : `Edit: ${existing?.displayName ?? '…'}`}
        </h1>
      </div>

      <form onSubmit={handleSubmit((v) => mutation.mutate(v))} className="space-y-6">
        {/* Basic info */}
        <div className="card space-y-4">
          <h2 className="text-base font-semibold text-slate-900">Basic Information</h2>
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="form-label">Name</label>
              <input className="form-input mt-1" {...register('name')} placeholder="Blog Post" />
              {errors.name && <p className="form-error">{errors.name.message}</p>}
            </div>
            <div>
              <label className="form-label">API Key</label>
              <input className="form-input mt-1 font-mono" {...register('apiKey')} placeholder="blog_post" />
              {errors.apiKey && <p className="form-error">{errors.apiKey.message}</p>}
            </div>
          </div>
          <div>
            <label className="form-label">Description (optional)</label>
            <input className="form-input mt-1" {...register('description')} placeholder="Describe this content type…" />
          </div>
          <div className="flex items-center gap-3">
            <input type="checkbox" id="isCollection" className="h-4 w-4 rounded border-slate-300 text-brand-600" {...register('isCollection')} />
            <label htmlFor="isCollection" className="text-sm font-medium text-slate-700">
              Collection (multiple entries) — uncheck for singleton
            </label>
          </div>
        </div>

        {/* Fields */}
        <div className="card space-y-4">
          <div className="flex items-center justify-between">
            <h2 className="text-base font-semibold text-slate-900">Fields</h2>
            <button type="button" onClick={addField} className="btn-secondary text-xs">
              + Add Field
            </button>
          </div>

          {fields.length === 0 && (
            <p className="text-sm text-slate-400">No fields yet. Click "Add Field" to start.</p>
          )}

          <div className="space-y-3">
            {fields.map((field, idx) => (
              <div
                key={field.id}
                className={`rounded-lg border p-4 ${activeFieldIdx === idx ? 'border-brand-300 bg-brand-50/40' : 'border-slate-200'}`}
              >
                <div
                  className="flex cursor-pointer items-center justify-between"
                  onClick={() => setActiveFieldIdx(activeFieldIdx === idx ? null : idx)}
                >
                  <div className="flex items-center gap-2">
                    <span className="text-sm font-medium text-slate-800">
                      {watch(`fields.${idx}.name`) || <span className="text-slate-400">Unnamed field</span>}
                    </span>
                    <span className="badge-slate text-xs">{watch(`fields.${idx}.type`)}</span>
                    {watch(`fields.${idx}.required`) && <span className="badge-red text-xs">Required</span>}
                    {watch(`fields.${idx}.localized`) && <span className="badge-brand text-xs">Localized</span>}
                  </div>
                  <div className="flex items-center gap-2">
                    <button
                      type="button"
                      onClick={(e) => { e.stopPropagation(); if (idx > 0) move(idx, idx - 1); }}
                      className="text-slate-400 hover:text-slate-600 disabled:opacity-30"
                      disabled={idx === 0}
                      aria-label="Move up"
                    >↑</button>
                    <button
                      type="button"
                      onClick={(e) => { e.stopPropagation(); if (idx < fields.length - 1) move(idx, idx + 1); }}
                      className="text-slate-400 hover:text-slate-600 disabled:opacity-30"
                      disabled={idx === fields.length - 1}
                      aria-label="Move down"
                    >↓</button>
                    <button
                      type="button"
                      onClick={(e) => { e.stopPropagation(); remove(idx); }}
                      className="text-red-400 hover:text-red-600"
                      aria-label="Remove field"
                    >✕</button>
                  </div>
                </div>

                {activeFieldIdx === idx && (
                  <div className="mt-4 grid grid-cols-2 gap-3">
                    <div>
                      <label className="form-label">Name</label>
                      <input className="form-input mt-1" {...register(`fields.${idx}.name`)} />
                      {errors.fields?.[idx]?.name && (
                        <p className="form-error">{errors.fields[idx]?.name?.message}</p>
                      )}
                    </div>
                    <div>
                      <label className="form-label">API Key</label>
                      <input className="form-input mt-1 font-mono" {...register(`fields.${idx}.apiKey`)} />
                      {errors.fields?.[idx]?.apiKey && (
                        <p className="form-error">{errors.fields[idx]?.apiKey?.message}</p>
                      )}
                    </div>
                    <div>
                      <label className="form-label">Type</label>
                      <select className="form-input mt-1" {...register(`fields.${idx}.type`)}>
                        {FIELD_TYPES.map((ft) => (
                          <option key={ft.value} value={ft.value}>{ft.label}</option>
                        ))}
                      </select>
                    </div>
                    <div className="flex items-end gap-4 pb-1">
                      <label className="flex items-center gap-2 text-sm text-slate-700">
                        <input type="checkbox" className="h-4 w-4 rounded border-slate-300 text-brand-600" {...register(`fields.${idx}.required`)} />
                        Required
                      </label>
                      <label className="flex items-center gap-2 text-sm text-slate-700">
                        <input type="checkbox" className="h-4 w-4 rounded border-slate-300 text-brand-600" {...register(`fields.${idx}.localized`)} />
                        Localized
                      </label>
                    </div>
                  </div>
                )}
              </div>
            ))}
          </div>
        </div>

        {/* Actions */}
        <div className="flex justify-end gap-3">
          <button type="button" onClick={() => navigate('/content-types')} className="btn-secondary">
            Cancel
          </button>
          <button type="submit" disabled={isSubmitting} className="btn-primary">
            {isSubmitting ? 'Saving…' : isNew ? 'Create Content Type' : 'Save Changes'}
          </button>
        </div>
      </form>
    </div>
  );
}
