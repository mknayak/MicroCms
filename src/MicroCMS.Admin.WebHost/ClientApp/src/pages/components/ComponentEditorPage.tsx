import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm, useFieldArray } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import toast from 'react-hot-toast';
import { componentsApi } from '@/api/components';
import { ApiError } from '@/api/client';
import type { ComponentCategory, ComponentFieldType } from '@/types';

// ─── Constants ────────────────────────────────────────────────────────────────

const ZONE_OPTIONS = [
  'hero-zone', 'features-zone', 'content-zone', 'media-zone',
  'testimonials-zone', 'cta-zone', 'header-zone', 'footer-zone',
];

const FIELD_TYPES: ComponentFieldType[] = [
  'ShortText', 'LongText', 'RichText', 'Number', 'Boolean',
  'DateTime', 'URL', 'AssetRef', 'EntryRef', 'JSON', 'ComponentRef',
];

const CATEGORIES: ComponentCategory[] = [
  'Layout', 'Content', 'Media', 'Navigation', 'Interactive', 'Commerce',
];

// ─── Schema ───────────────────────────────────────────────────────────────────

const fieldSchema = z.object({
  id: z.string().optional(),
  handle: z.string().min(1, 'Handle required').regex(/^[a-zA-Z][a-zA-Z0-9_]*$/, 'camelCase only'),
  label: z.string().min(1, 'Label required'),
  fieldType: z.enum(FIELD_TYPES as [ComponentFieldType, ...ComponentFieldType[]]),
  isRequired: z.boolean(),
  isLocalized: z.boolean(),
  isIndexed: z.boolean(),
  sortOrder: z.number(),
  description: z.string().optional(),
});

const formSchema = z.object({
  name: z.string().min(1, 'Name required').max(200),
  key: z.string().min(1, 'Key required').regex(/^[a-z0-9-]+$/),
  description: z.string().optional(),
  category: z.enum(CATEGORIES as [ComponentCategory, ...ComponentCategory[]]),
  zones: z.array(z.string()).min(1, 'At least one zone required'),
  fields: z.array(fieldSchema),
});

type EditorForm = z.infer<typeof formSchema>;

// ─── Page ─────────────────────────────────────────────────────────────────────

export default function ComponentEditorPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const qc = useQueryClient();
  const [activeTab, setActiveTab] = useState<'fields' | 'zones' | 'meta'>('fields');

  const { data: comp, isLoading } = useQuery({
queryKey: ['component', id],
    queryFn: () => componentsApi.getById(id!),
  enabled: !!id,
  });

  const {
    register,
    control,
    handleSubmit,
    watch,
    setValue,
    reset,
    formState: { errors, isDirty },
  } = useForm<EditorForm>({
    resolver: zodResolver(formSchema),
    defaultValues: {
      name: '',
      key: '',
      description: '',
   category: 'Layout',
 zones: [],
      fields: [],
    },
  });

  useEffect(() => {
    if (comp) {
      reset({
        name: comp.name,
        key: comp.key,
    description: comp.description ?? '',
        category: comp.category,
        zones: comp.zones,
   fields: comp.fields.map((f) => ({
      id: f.id,
          handle: f.handle,
          label: f.label,
          fieldType: f.fieldType,
          isRequired: f.isRequired,
       isLocalized: f.isLocalized,
        isIndexed: f.isIndexed,
 sortOrder: f.sortOrder,
          description: f.description ?? '',
      })),
      });
    }
  }, [comp, reset]);

  const { fields, append, remove } = useFieldArray({ control, name: 'fields' });

  const selectedZones = watch('zones') ?? [];

  const toggleZone = (zone: string) => {
    const current = selectedZones;
    setValue(
'zones',
      current.includes(zone) ? current.filter((z) => z !== zone) : [...current, zone],
      { shouldDirty: true },
    );
  };

  const updateMutation = useMutation({
    mutationFn: (data: EditorForm) =>
      componentsApi.update(id!, {
        name: data.name,
        description: data.description,
        category: data.category,
        zones: data.zones,
        fields: data.fields.map((f, i) => ({
  id: f.id ?? crypto.randomUUID(),
          handle: f.handle,
          label: f.label,
          fieldType: f.fieldType,
 isRequired: f.isRequired,
    isLocalized: f.isLocalized,
          isIndexed: f.isIndexed,
  sortOrder: i,
          description: f.description,
        })),
      }),
    onSuccess: () => {
    toast.success('Component definition saved.');
 void qc.invalidateQueries({ queryKey: ['component', id] });
   void qc.invalidateQueries({ queryKey: ['components'] });
    },
    onError: (err) =>
      toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Save failed.'),
  });

  if (isLoading) {
    return (
      <div className="space-y-4">
        {Array.from({ length: 6 }).map((_, i) => (
    <div key={i} className="h-10 animate-pulse rounded-lg bg-slate-100" />
      ))}
      </div>
    );
  }

  if (!comp) {
    return <p className="text-sm text-slate-500">Component not found.</p>;
  }

  const TABS = [
    { key: 'fields', label: `Fields (${fields.length})` },
  { key: 'zones', label: 'Zones' },
    { key: 'meta', label: 'Meta' },
] as const;

  return (
    <form onSubmit={handleSubmit((v) => updateMutation.mutate(v))} className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
    <button
     type="button"
          onClick={() => navigate('/components')}
      className="text-sm text-slate-500 hover:text-slate-700"
          >
   ← Component Library
  </button>
      <span className="text-slate-300">/</span>
          <h1 className="text-xl font-bold text-slate-900">{comp.name}</h1>
        <span className="rounded-full bg-slate-100 px-2 py-0.5 font-mono text-xs text-slate-500">
   comp/{comp.key}
  </span>
        </div>
  <div className="flex gap-3">
        <button
         type="button"
            onClick={() => navigate(`/components/${id}/items`)}
  className="btn-secondary text-sm"
          >
   {comp.itemCount} Items
          </button>
          <button
        type="submit"
            className="btn-primary"
            disabled={!isDirty || updateMutation.isPending}
      >
        {updateMutation.isPending ? 'Saving…' : 'Save Definition'}
    </button>
        </div>
      </div>

      {/* Tabs */}
      <div className="flex border-b border-slate-200">
        {TABS.map((tab) => (
          <button
  key={tab.key}
            type="button"
            onClick={() => setActiveTab(tab.key)}
            className={`px-4 py-2 text-sm font-medium border-b-2 -mb-px transition-colors ${
  activeTab === tab.key
       ? 'border-brand-500 text-brand-600'
                : 'border-transparent text-slate-500 hover:text-slate-700'
     }`}
      >
       {tab.label}
          </button>
        ))}
      </div>

   <div className="grid grid-cols-3 gap-6">
        {/* Main panel */}
        <div className="col-span-2 space-y-4">
          {/* ── Fields tab ── */}
        {activeTab === 'fields' && (
  <div className="card overflow-hidden">
     {/* Column headers */}
    <div className="grid grid-cols-[1fr_140px_70px_70px_70px_36px] gap-2 border-b border-slate-200 bg-slate-50 px-3 py-2 text-[10px] font-bold uppercase tracking-wider text-slate-400">
           <span>Field Handle</span>
  <span>Type</span>
          <span className="text-center">Required</span>
                <span className="text-center">Localised</span>
      <span className="text-center">Indexed</span>
      <span />
              </div>

          {fields.length === 0 && (
     <p className="px-4 py-6 text-center text-sm text-slate-400">
           No fields yet. Add your first field below.
                </p>
     )}

              {fields.map((field, index) => (
           <div
     key={field.id}
   className="grid grid-cols-[1fr_140px_70px_70px_70px_36px] items-center gap-2 border-b border-slate-100 px-3 py-2.5 last:border-0 hover:bg-slate-50"
                >
       <input
         {...register(`fields.${index}.handle`)}
        className="w-full rounded border border-transparent bg-transparent px-2 py-1 font-mono text-sm font-semibold text-slate-800 hover:border-slate-200 focus:border-brand-500 focus:outline-none focus:ring-1 focus:ring-brand-200"
       placeholder="fieldName"
              />
      <select
        {...register(`fields.${index}.fieldType`)}
           className="rounded border border-slate-200 bg-white px-2 py-1 text-xs focus:outline-none focus:border-brand-500"
           >
         {FIELD_TYPES.map((t) => (
    <option key={t} value={t}>
      {t}
     </option>
        ))}
       </select>
                <div className="flex justify-center">
                  <input type="checkbox" {...register(`fields.${index}.isRequired`)} className="accent-brand-600" />
    </div>
         <div className="flex justify-center">
    <input type="checkbox" {...register(`fields.${index}.isLocalized`)} className="accent-brand-600" />
        </div>
   <div className="flex justify-center">
       <input type="checkbox" {...register(`fields.${index}.isIndexed`)} className="accent-brand-600" />
     </div>
        <button
       type="button"
     onClick={() => remove(index)}
   className="flex h-7 w-7 items-center justify-center rounded text-slate-300 hover:bg-red-50 hover:text-red-500"
       >
   <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
           </svg>
    </button>
      </div>
       ))}

          {/* Add field bar */}
          <div className="border-t border-dashed border-slate-200 bg-slate-50 px-3 py-2">
     <p className="mb-2 text-xs font-semibold text-slate-400">Add field</p>
          <div className="flex flex-wrap gap-1.5">
       {FIELD_TYPES.map((ft) => (
        <button
    key={ft}
    type="button"
           onClick={() =>
            append({
      handle: '',
             label: ft,
 fieldType: ft,
      isRequired: false,
        isLocalized: false,
 isIndexed: false,
      sortOrder: fields.length,
           })
          }
          className="rounded border border-slate-200 bg-white px-2 py-1 text-xs font-medium text-slate-600 hover:border-brand-400 hover:text-brand-600"
      >
     {ft}
        </button>
     ))}
     </div>
       </div>
            </div>
          )}

{/* ── Zones tab ── */}
    {activeTab === 'zones' && (
         <div className="card">
 <h3 className="mb-3 text-sm font-semibold text-slate-800">Allowed Zones</h3>
      <div className="space-y-2">
   {ZONE_OPTIONS.map((zone) => (
   <label
        key={zone}
         className={`flex cursor-pointer items-center gap-3 rounded-lg border-2 px-4 py-3 transition-colors ${
  selectedZones.includes(zone)
    ? 'border-brand-500 bg-brand-50'
          : 'border-slate-200 bg-white hover:border-slate-300'
     }`}
 >
             <input
       type="checkbox"
className="accent-brand-600"
           checked={selectedZones.includes(zone)}
    onChange={() => toggleZone(zone)}
            />
           <span className="text-sm font-medium text-slate-700">{zone}</span>
         </label>
                ))}
        </div>
     {errors.zones && <p className="mt-2 form-error">{errors.zones.message}</p>}
    </div>
          )}

          {/* ── Meta tab ── */}
    {activeTab === 'meta' && (
            <div className="card space-y-4">
          <div className="grid grid-cols-2 gap-4">
       <div>
          <label className="form-label">Display Name</label>
        <input className="form-input mt-1" {...register('name')} />
       {errors.name && <p className="form-error">{errors.name.message}</p>}
        </div>
         <div>
   <label className="form-label">Key</label>
       <div className="mt-1 flex items-center rounded-lg border border-slate-200 focus-within:border-brand-500">
         <span className="rounded-l-lg border-r border-slate-200 bg-slate-50 px-2.5 py-2 text-xs text-slate-500">
  comp/
        </span>
          <input
          className="flex-1 rounded-r-lg px-2.5 py-2 text-sm font-mono focus:outline-none"
           {...register('key')}
   />
          </div>
     {errors.key && <p className="form-error">{errors.key.message}</p>}
   </div>
          </div>
         <div>
                <label className="form-label">Category</label>
                <select className="form-input mt-1" {...register('category')}>
       {CATEGORIES.map((c) => (
       <option key={c} value={c}>{c}</option>
          ))}
    </select>
       </div>
       <div>
                <label className="form-label">Description</label>
     <textarea className="form-input mt-1" {...register('description')} rows={3} />
   </div>
        </div>
      )}
        </div>

  {/* Sidebar */}
     <div className="space-y-4">
          <div className="card">
            <h3 className="mb-3 text-sm font-semibold text-slate-800">Component Status</h3>
     <div className="space-y-1.5 text-xs">
          <div className="flex justify-between">
          <span className="text-slate-500">Items</span>
                <button
     type="button"
  onClick={() => navigate(`/components/${id}/items`)}
        className="font-semibold text-brand-600"
           >
  {comp.itemCount} instances →
     </button>
     </div>
   <div className="flex justify-between">
<span className="text-slate-500">Used on pages</span>
    <span className="font-semibold">{comp.usageCount}</span>
  </div>
              <div className="flex justify-between">
  <span className="text-slate-500">Last modified</span>
     <span className="font-semibold">
      {new Date(comp.updatedAt).toLocaleDateString()}
          </span>
              </div>
            </div>
          </div>

<div className="rounded-lg border border-amber-200 bg-amber-50 p-3 text-xs text-amber-700">
      ⚠️ Removing or renaming fields may break existing Component Items.
          </div>
        </div>
 </div>
    </form>
  );
}
