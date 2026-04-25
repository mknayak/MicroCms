import { useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import toast from 'react-hot-toast';
import { componentsApi } from '@/api/components';
import { ApiError } from '@/api/client';

// ─── Schema ───────────────────────────────────────────────────────────────────

const schema = z.object({
  title: z.string().min(1, 'Internal title is required'),
  fieldsJson: z.record(z.unknown()),
});

type ItemForm = z.infer<typeof schema>;

// ─── Page ─────────────────────────────────────────────────────────────────────

export default function ComponentItemEditorPage() {
  const { id: componentId, itemId } = useParams<{ id: string; itemId: string }>();
  const navigate = useNavigate();
  const qc = useQueryClient();
  const isNew = itemId === 'new';

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
    setValue,
    formState: { errors, isDirty },
  } = useForm<ItemForm>({
    resolver: zodResolver(schema),
    defaultValues: { title: '', fieldsJson: {} },
  });

  useEffect(() => {
    if (item) {
   reset({ title: item.title, fieldsJson: item.fieldsJson });
    }
  }, [item, reset]);

  const createMutation = useMutation({
    mutationFn: (data: ItemForm) =>
  componentsApi.createItem(componentId!, {
     title: data.title,
 fieldsJson: data.fieldsJson,
      }),
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
 componentsApi.updateItem(componentId!, itemId!, {
 title: data.title,
   fieldsJson: data.fieldsJson,
      }),
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
    <form onSubmit={handleSubmit(onSubmit)} className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
     <div className="flex items-center gap-2">
    <button
     type="button"
       onClick={() => navigate(`/components/${componentId}/items`)}
           className="text-sm text-slate-500 hover:text-slate-700"
        >
     ← {comp?.name ?? 'Component'}
  </button>
          <span className="text-slate-300">/</span>
 <h1 className="text-xl font-bold text-slate-900">
       {isNew ? 'New Item' : (item?.title ?? 'Edit Item')}
  </h1>
       </div>
  <div className="flex gap-3">
 <button
      type="button"
     className="btn-secondary text-sm"
    onClick={() => navigate(`/components/${componentId}/items`)}
          >
       Cancel
          </button>
        <button
 type="submit"
    className="btn-secondary"
       disabled={(!isDirty && !isNew) || createMutation.isPending || updateMutation.isPending}
          >
        {createMutation.isPending || updateMutation.isPending ? 'Saving…' : 'Save Draft'}
          </button>
          {!isNew && (
<button
         type="button"
              className="btn-primary"
   onClick={() => publishMutation.mutate()}
        disabled={publishMutation.isPending}
     >
      {publishMutation.isPending ? 'Publishing…' : 'Publish'}
          </button>
     )}
       </div>
  </div>

 <div className="grid grid-cols-3 gap-6">
 {/* Main: field values */}
  <div className="col-span-2 space-y-4">
          {/* Internal title */}
          <div className="card space-y-4">
          <div>
  <label className="form-label">
          Internal Title <span className="text-red-500">*</span>
            </label>
     <p className="mb-1 text-xs text-slate-400">Used for identification only — not displayed on site.</p>
      <input
         className="form-input mt-1"
     {...register('title')}
    placeholder="e.g. Summer Campaign Hero"
       />
{errors.title && <p className="form-error">{errors.title.message}</p>}
   </div>
          </div>

         {/* Dynamic fields */}
    <div className="card space-y-5">
      <h3 className="text-sm font-semibold text-slate-800">Field Values</h3>
    {(comp?.fields ?? []).map((field) => {
         const currentJson = watch('fieldsJson') ?? {};
   const fieldValue = currentJson[field.handle] as string | undefined;

    return (
                <div key={field.id}>
 <label className="form-label flex items-center gap-2">
      {field.label}
       <span className="rounded bg-slate-100 px-1.5 py-0.5 font-mono text-[10px] text-slate-400">
       {field.handle}
 </span>
  {field.isRequired && <span className="h-1.5 w-1.5 rounded-full bg-red-500" />}
  </label>

          {field.fieldType === 'RichText' ? (
         <textarea
       className="form-input mt-1"
      rows={4}
        value={fieldValue ?? ''}
            onChange={(e) =>
         setValue('fieldsJson', { ...currentJson, [field.handle]: e.target.value }, { shouldDirty: true })
     }
           />
     ) : field.fieldType === 'Boolean' ? (
       <label className="mt-1 flex items-center gap-2 text-sm cursor-pointer">
          <input
  type="checkbox"
  className="accent-brand-600"
       checked={!!currentJson[field.handle]}
          onChange={(e) =>
      setValue('fieldsJson', { ...currentJson, [field.handle]: e.target.checked }, { shouldDirty: true })
        }
         />
      {field.label}
     </label>
      ) : field.fieldType === 'Number' ? (
        <input
         type="number"
      className="form-input mt-1"
                   value={fieldValue ?? ''}
       onChange={(e) =>
       setValue('fieldsJson', { ...currentJson, [field.handle]: Number(e.target.value) }, { shouldDirty: true })
      }
    />
      ) : (
              <input
          type={field.fieldType === 'URL' ? 'url' : 'text'}
 className="form-input mt-1"
        value={fieldValue ?? ''}
    onChange={(e) =>
             setValue('fieldsJson', { ...currentJson, [field.handle]: e.target.value }, { shouldDirty: true })
   }
           placeholder={`Enter ${field.label}…`}
          />
  )}
    </div>
              );
    })}

        {(comp?.fields.length ?? 0) === 0 && (
  <p className="text-sm text-slate-400">
          This component has no fields defined. <button type="button" onClick={() => navigate(`/components/${componentId}/edit`)} className="text-brand-600 underline">Edit schema →</button>
      </p>
            )}
          </div>
        </div>

        {/* Sidebar */}
        <div className="space-y-4">
          {/* Status */}
{!isNew && item && (
            <div className="card">
         <h3 className="mb-3 text-sm font-semibold text-slate-800">Status</h3>
   <span className={`inline-block rounded-full px-3 py-1 text-xs font-semibold ${
            item.status === 'Published' ? 'bg-green-100 text-green-700' :
         item.status === 'Draft' ? 'bg-amber-100 text-amber-700' :
     'bg-slate-100 text-slate-500'
}`}>
      {item.status}
  </span>
   <div className="mt-3 space-y-1 text-xs text-slate-500">
         <div className="flex justify-between">
   <span>Used on pages</span>
    <strong>{item.usedOnPages}</strong>
     </div>
    <div className="flex justify-between">
          <span>Updated</span>
       <strong>{new Date(item.updatedAt).toLocaleDateString()}</strong>
       </div>
         </div>
            </div>
          )}

          {/* Component info */}
 {comp && (
<div className="card">
       <h3 className="mb-3 text-sm font-semibold text-slate-800">Component</h3>
              <div className="space-y-1 text-xs text-slate-500">
          <div className="flex justify-between">
       <span>Type</span>
         <span className="font-semibold text-slate-700">{comp.name}</span>
      </div>
      <div className="flex justify-between">
          <span>Category</span>
  <span>{comp.category}</span>
 </div>
        <div className="flex justify-between">
                  <span>Fields</span>
           <span>{comp.fields.length}</span>
      </div>
      </div>
           <button
            type="button"
        onClick={() => navigate(`/components/${componentId}/edit`)}
          className="mt-3 text-xs text-brand-600 hover:underline"
              >
        Edit schema →
     </button>
    </div>
          )}

       {/* Danger zone */}
          {!isNew && (
            <div className="rounded-lg border border-red-200 bg-white p-4">
  <h3 className="mb-2 text-xs font-semibold text-red-600">Danger Zone</h3>
       <button
      type="button"
                className="w-full rounded border border-red-200 bg-red-50 px-3 py-1.5 text-xs font-medium text-red-600 hover:bg-red-100"
     onClick={() => {
       if (confirm(`Delete "${item?.title}"? This cannot be undone.`)) {
      deleteMutation.mutate();
   }
         }}
       disabled={deleteMutation.isPending}
  >
          Delete this Item
              </button>
            </div>
 )}
     </div>
      </div>
    </form>
  );
}
