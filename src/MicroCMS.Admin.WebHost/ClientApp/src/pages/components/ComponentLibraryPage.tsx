import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import toast from 'react-hot-toast';
import { componentsApi } from '@/api/components';
import { useSite } from '@/contexts/SiteContext';
import { ApiError } from '@/api/client';
import type { ComponentCategory, ComponentListItem } from '@/types';

// ─── Constants ────────────────────────────────────────────────────────────────

const ZONE_OPTIONS = [
  'hero-zone',
  'features-zone',
  'content-zone',
  'media-zone',
  'testimonials-zone',
  'cta-zone',
  'header-zone',
  'footer-zone',
];

const CATEGORIES: ComponentCategory[] = [
  'Layout',
  'Content',
  'Media',
  'Navigation',
  'Interactive',
  'Commerce',
];

const CATEGORY_COLORS: Record<ComponentCategory, string> = {
  Layout: 'bg-blue-100 text-blue-700',
Content: 'bg-green-100 text-green-700',
  Media: 'bg-amber-100 text-amber-700',
  Navigation: 'bg-purple-100 text-purple-700',
  Interactive: 'bg-pink-100 text-pink-700',
  Commerce: 'bg-emerald-100 text-emerald-700',
};

// ─── Schema ───────────────────────────────────────────────────────────────────

const createSchema = z.object({
  name: z.string().min(1, 'Name is required').max(200),
  key: z
    .string()
    .min(1, 'Key is required')
    .regex(/^[a-z0-9-]+$/, 'Lowercase letters, numbers and hyphens only'),
  description: z.string().optional(),
  category: z.enum([
    'Layout',
    'Content',
    'Media',
  'Navigation',
    'Interactive',
    'Commerce',
  ] as const),
  zones: z.array(z.string()).min(1, 'Select at least one zone'),
});

type CreateForm = z.infer<typeof createSchema>;

// ─── Component Card ───────────────────────────────────────────────────────────

function ComponentCard({
  comp,
  onEdit,
  onDelete,
}: {
  comp: ComponentListItem;
  onEdit: () => void;
  onDelete: () => void;
}) {
  const navigate = useNavigate();

  return (
    <div
      className="group relative cursor-pointer rounded-xl border-2 border-slate-200 bg-white p-4 transition-all hover:-translate-y-0.5 hover:border-brand-400 hover:shadow-md"
 onClick={() => navigate(`/components/${comp.id}/items`)}
    >
      {/* Category badge */}
    <span
        className={`absolute right-3 top-3 rounded-full px-2 py-0.5 text-[10px] font-bold ${CATEGORY_COLORS[comp.category] ?? 'bg-slate-100 text-slate-600'}`}
      >
        {comp.category}
      </span>

  {/* Preview placeholder */}
  <div className="mb-3 flex h-20 items-center justify-center rounded-lg bg-gradient-to-br from-slate-50 to-brand-50">
        <svg
      className="h-8 w-8 text-brand-300"
          fill="none"
    viewBox="0 0 24 24"
      stroke="currentColor"
        >
          <path
     strokeLinecap="round"
         strokeLinejoin="round"
            strokeWidth={1.5}
            d="M20 7l-8-4-8 4m16 0l-8 4m8-4v10l-8 4m0-10L4 7m8 4v10M4 7v10l8 4"
          />
        </svg>
      </div>

      <div className="mb-1 pr-16 text-sm font-bold text-slate-900">{comp.name}</div>
   {comp.description && (
 <p className="mb-2 line-clamp-2 text-xs text-slate-500">{comp.description}</p>
      )}

      {/* Meta */}
  <div className="mb-2 flex gap-3 text-xs text-slate-500">
        <span>{comp.fieldCount} fields</span>
        <span>·</span>
        <span>Used on {comp.usageCount} pages</span>
      </div>

      {/* Zones */}
      <div className="mb-3 flex flex-wrap gap-1">
      {comp.zones.map((z) => (
  <span key={z} className="rounded px-1.5 py-0.5 text-[10px] font-semibold bg-blue-50 text-blue-600 border border-blue-100">
 {z}
          </span>
   ))}
      </div>

      {/* Footer */}
      <div className="flex items-center justify-between border-t border-slate-100 pt-2">
        <span className="text-xs text-slate-400">{comp.itemCount} items</span>
        <div className="flex gap-1 opacity-0 transition-opacity group-hover:opacity-100">
          <button
       className="rounded p-1 text-xs text-slate-500 hover:bg-slate-100"
    title="Edit schema"
    onClick={(e) => {
           e.stopPropagation();
     onEdit();
            }}
        >
      <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
       <path
          strokeLinecap="round"
   strokeLinejoin="round"
                strokeWidth={2}
            d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z"
          />
  </svg>
          </button>
 <button
       className="rounded p-1 text-xs text-red-400 hover:bg-red-50"
      title="Delete component"
      onClick={(e) => {
       e.stopPropagation();
  onDelete();
  }}
          >
       <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
       <path
       strokeLinecap="round"
     strokeLinejoin="round"
                strokeWidth={2}
          d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16"
    />
  </svg>
          </button>
        </div>
      </div>
    </div>
  );
}

// ─── Page ─────────────────────────────────────────────────────────────────────

export default function ComponentLibraryPage() {
  const navigate = useNavigate();
  const qc = useQueryClient();
const { selectedSiteId } = useSite();
  const [showCreate, setShowCreate] = useState(false);
  const [search, setSearch] = useState('');
  const [activeCategory, setActiveCategory] = useState<ComponentCategory | 'All'>('All');

  const { data, isLoading } = useQuery({
    queryKey: ['components', selectedSiteId],
    queryFn: () =>
      componentsApi.list({ siteId: selectedSiteId ?? undefined, pageSize: 100 }),
    enabled: !!selectedSiteId,
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => componentsApi.delete(id),
    onSuccess: () => {
 toast.success('Component deleted.');
      void qc.invalidateQueries({ queryKey: ['components'] });
    },
    onError: (err) =>
      toast.error(
   err instanceof ApiError ? err.problem.detail ?? err.message : 'Delete failed.',
      ),
  });

  const createMutation = useMutation({
    mutationFn: (data: CreateForm) =>
      componentsApi.create({
      siteId: selectedSiteId!,
        ...data,
      }),
 onSuccess: (comp) => {
 toast.success('Component created.');
      setShowCreate(false);
      void qc.invalidateQueries({ queryKey: ['components'] });
navigate(`/components/${comp.id}/edit`);
    },
    onError: (err) =>
      toast.error(
        err instanceof ApiError ? err.problem.detail ?? err.message : 'Create failed.',
 ),
  });

  const {
    register,
 handleSubmit,
 watch,
    setValue,
    reset,
    formState: { errors },
  } = useForm<CreateForm>({
    resolver: zodResolver(createSchema),
    defaultValues: { category: 'Layout', zones: [] },
  });

  const selectedZones = watch('zones') ?? [];

  const toggleZone = (zone: string) => {
    const current = selectedZones;
    setValue(
      'zones',
      current.includes(zone) ? current.filter((z) => z !== zone) : [...current, zone],
    );
  };

  const filtered = (data?.items ?? []).filter((c) => {
const matchSearch =
      !search ||
      c.name.toLowerCase().includes(search.toLowerCase()) ||
 (c.description ?? '').toLowerCase().includes(search.toLowerCase());
    const matchCat = activeCategory === 'All' || c.category === activeCategory;
    return matchSearch && matchCat;
  });

  const counts = (data?.items ?? []).reduce<Record<string, number>>(
    (acc, c) => {
      acc[c.category] = (acc[c.category] ?? 0) + 1;
      return acc;
    },
    {},
  );

  if (!selectedSiteId) {
    return (
    <div className="flex flex-col items-center justify-center gap-3 py-24 text-center">
     <p className="text-sm font-medium text-slate-500">
          No site selected. Choose a site from the top bar.
        </p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-start justify-between">
     <div>
          <h1 className="text-2xl font-bold text-slate-900">Component Library</h1>
          <p className="mt-1 text-sm text-slate-500">
            Reusable UI rendering blocks — compose pages from these in the Page Designer.
          </p>
        </div>
        <button className="btn-primary" onClick={() => { reset(); setShowCreate(true); }}>
   <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
   <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
          </svg>
          New Component
        </button>
      </div>

      {/* Stats strip */}
      <div className="flex flex-wrap gap-4">
        {[
  { label: 'Components', value: data?.totalCount ?? 0, color: 'text-blue-600 bg-blue-50' },
          {
      label: 'Usages',
            value: (data?.items ?? []).reduce((s, c) => s + c.usageCount, 0),
 color: 'text-amber-600 bg-amber-50',
          },
          { label: 'Total Items', value: (data?.items ?? []).reduce((s, c) => s + c.itemCount, 0), color: 'text-green-600 bg-green-50' },
        ].map((s) => (
          <div key={s.label} className="flex items-center gap-3 rounded-lg border border-slate-200 bg-white px-4 py-2.5">
   <span className={`rounded-lg px-2 py-1 text-lg font-extrabold ${s.color}`}>
  {s.value}
            </span>
            <span className="text-sm text-slate-500">{s.label}</span>
          </div>
      ))}
      </div>

      {/* Category filter + search */}
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex flex-wrap gap-2">
          {(['All', ...CATEGORIES] as const).map((cat) => (
      <button
   key={cat}
     onClick={() => setActiveCategory(cat)}
     className={`rounded-full border px-3 py-1 text-xs font-semibold transition-colors ${
        activeCategory === cat
    ? 'border-brand-500 bg-brand-600 text-white'
       : 'border-slate-200 bg-white text-slate-600 hover:border-brand-400 hover:text-brand-600'
              }`}
 >
        {cat}
   {cat !== 'All' && counts[cat] != null && (
    <span className="ml-1 opacity-75">({counts[cat]})</span>
     )}
    {cat === 'All' && (
    <span className="ml-1 opacity-75">({data?.totalCount ?? 0})</span>
            )}
 </button>
   ))}
        </div>
        <input
          className="h-9 w-56 rounded-lg border border-slate-200 bg-white px-3 text-sm focus:border-brand-500 focus:outline-none focus:ring-1 focus:ring-brand-500"
      placeholder="Search components…"
     value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
      </div>

      {/* Grid */}
      {isLoading ? (
<div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4">
 {Array.from({ length: 8 }).map((_, i) => (
            <div key={i} className="h-52 animate-pulse rounded-xl bg-slate-100" />
        ))}
        </div>
      ) : filtered.length === 0 ? (
        <div className="flex flex-col items-center justify-center gap-3 py-20 text-center">
        <p className="text-sm text-slate-400">
   {data?.totalCount === 0
              ? 'No components yet. Create your first one.'
              : 'No components match your filters.'}
       </p>
        </div>
      ) : (
        <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4">
          {filtered.map((comp) => (
        <ComponentCard
   key={comp.id}
              comp={comp}
  onEdit={() => navigate(`/components/${comp.id}/edit`)}
  onDelete={() => {
         if (confirm(`Delete component "${comp.name}"? This cannot be undone.`)) {
  deleteMutation.mutate(comp.id);
}
           }}
   />
          ))}
     {/* Add new card */}
  <button
 onClick={() => { reset(); setShowCreate(true); }}
        className="flex min-h-[208px] flex-col items-center justify-center gap-2 rounded-xl border-2 border-dashed border-slate-300 text-slate-400 transition-colors hover:border-brand-400 hover:text-brand-500"
  >
            <svg className="h-7 w-7" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M12 4v16m8-8H4" />
         </svg>
 <span className="text-sm font-semibold">New Component</span>
          </button>
        </div>
      )}

      {/* Create modal */}
      {showCreate && (
    <div
       className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4"
  onClick={(e) => e.target === e.currentTarget && setShowCreate(false)}
      >
          <div className="w-full max-w-lg rounded-2xl bg-white shadow-xl">
            <div className="flex items-center justify-between border-b border-slate-200 px-6 py-4">
       <div>
       <h2 className="text-base font-bold text-slate-900">New Component</h2>
     <p className="text-xs text-slate-500">Define a reusable UI rendering block</p>
    </div>
   <button
       onClick={() => setShowCreate(false)}
     className="text-slate-400 hover:text-slate-600"
  >
        ✕
  </button>
</div>

     <form onSubmit={handleSubmit((v) => createMutation.mutate(v))}>
  <div className="max-h-[65vh] overflow-y-auto px-6 py-4 space-y-4">
                <div className="grid grid-cols-2 gap-4">
        <div>
       <label className="form-label">
                 Name <span className="text-red-500">*</span>
      </label>
               <input
       className="form-input mt-1"
       {...register('name')}
     placeholder="e.g. HeroBanner"
      onChange={(e) => {
       register('name').onChange(e);
      setValue(
   'key',
           e.target.value
       .toLowerCase()
        .replace(/\s+/g, '-')
    .replace(/[^a-z0-9-]/g, ''),
    );
          }}
            />
                {errors.name && <p className="form-error">{errors.name.message}</p>}
    </div>
      <div>
     <label className="form-label">
  Key <span className="text-red-500">*</span>
      </label>
    <div className="mt-1 flex items-center rounded-lg border border-slate-200 focus-within:border-brand-500 focus-within:ring-1 focus-within:ring-brand-500">
<span className="rounded-l-lg border-r border-slate-200 bg-slate-50 px-2.5 py-2 text-xs text-slate-500">
            comp/
                 </span>
  <input
    className="flex-1 rounded-r-lg px-2.5 py-2 text-sm font-mono focus:outline-none"
      {...register('key')}
     placeholder="hero-banner"
             />
            </div>
   {errors.key && <p className="form-error">{errors.key.message}</p>}
         </div>
         </div>

     <div>
      <label className="form-label">Description</label>
          <textarea
        className="form-input mt-1"
     {...register('description')}
             rows={2}
       placeholder="What does this component render?"
    />
         </div>

      <div>
                <label className="form-label">Category</label>
     <select className="form-input mt-1" {...register('category')}>
      {CATEGORIES.map((c) => (
     <option key={c} value={c}>
  {c}
     </option>
             ))}
      </select>
          </div>

    <div>
     <label className="form-label">
         Allowed Zones <span className="text-red-500">*</span>
 </label>
    <div className="mt-2 flex flex-wrap gap-2">
        {ZONE_OPTIONS.map((zone) => (
  <label
  key={zone}
     className="flex cursor-pointer items-center gap-1.5 text-xs"
  >
            <input
         type="checkbox"
      className="accent-brand-600"
           checked={selectedZones.includes(zone)}
  onChange={() => toggleZone(zone)}
         />
   {zone}
       </label>
    ))}
     </div>
 {errors.zones && <p className="form-error">{errors.zones.message}</p>}
       </div>
   </div>

              <div className="flex justify-end gap-3 border-t border-slate-200 px-6 py-4">
     <button type="button" className="btn-secondary" onClick={() => setShowCreate(false)}>
     Cancel
          </button>
      <button
     type="submit"
           className="btn-primary"
     disabled={createMutation.isPending}
            >
       {createMutation.isPending ? 'Creating…' : 'Create Component'}
                </button>
   </div>
    </form>
          </div>
        </div>
 )}
    </div>
  );
}
