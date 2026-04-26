import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import toast from 'react-hot-toast';
import { layoutsApi } from '@/api/layouts';
import { useSite } from '@/contexts/SiteContext';
import type { LayoutDto, LayoutListItem, LayoutTemplateType } from '@/types';
import { ApiError } from '@/api/client';

// ─── Schemas ──────────────────────────────────────────────────────────────────

const layoutSchema = z.object({
  name: z.string().min(1, 'Name is required').max(200),
  key: z
    .string()
    .min(1, 'Key is required')
    .max(100)
    .regex(/^[a-z0-9]+(?:-[a-z0-9]+)*$/, 'Lowercase letters, numbers and hyphens only'),
  templateType: z.enum(['Handlebars', 'Html'] as const),
  shellTemplate: z.string().optional(),
});

type LayoutForm = z.infer<typeof layoutSchema>;

// ─── Template type badge ──────────────────────────────────────────────────────

const TEMPLATE_TYPE_BADGE: Record<LayoutTemplateType, string> = {
  Handlebars: 'bg-amber-100 text-amber-800',
  Html: 'bg-slate-100 text-slate-700',
};

const TEMPLATE_PLACEHOLDER: Record<LayoutTemplateType, string> = {
  Handlebars: `<!DOCTYPE html>
<html>
<head>
  <title>{{seo.title}}</title>
  <meta name="description" content="{{seo.description}}">
</head>
<body>
  <nav><!-- your nav here --></nav>

  <section class="hero">
    {{{zone_hero_zone}}}
  </section>

  <main>
    {{{zone_content_zone}}}
  </main>

  <footer><!-- your footer here --></footer>
</body>
</html>`,
  Html: `<!DOCTYPE html>
<html>
<head>
  <title>{{seo:title}}</title>
  <meta name="description" content="{{seo:description}}">
</head>
<body>
  <nav><!-- your nav here --></nav>

  <section class="hero">
    {{zone:hero-zone}}
  </section>

  <main>
    {{zone:content-zone}}
  </main>

  <footer><!-- your footer here --></footer>
</body>
</html>`,
};

// ─── Editor panel ─────────────────────────────────────────────────────────────

function LayoutEditor({
  layout,
  siteId,
  onClose,
}: {
  layout: LayoutDto | null; // null = create mode
  siteId: string;
  onClose: () => void;
}) {
  const qc = useQueryClient();

  const form = useForm<LayoutForm>({
    resolver: zodResolver(layoutSchema),
    defaultValues: layout
      ? {
      name: layout.name,
    key: layout.key,
          templateType: layout.templateType,
       shellTemplate: layout.shellTemplate ?? '',
        }
      : {
      name: '',
          key: '',
      templateType: 'Handlebars',
          shellTemplate: '',
        },
  });

  const templateType = form.watch('templateType');

  const createMutation = useMutation({
    mutationFn: (data: LayoutForm) =>
      layoutsApi.create({ siteId, ...data }),
    onSuccess: () => {
      toast.success('Layout created.');
      void qc.invalidateQueries({ queryKey: ['layouts', siteId] });
      onClose();
    },
    onError: (err) =>
      toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Create failed.'),
  });

  const updateMutation = useMutation({
    mutationFn: (data: LayoutForm) =>
      layoutsApi.update(layout!.id, data),
    onSuccess: () => {
      toast.success('Layout saved.');
      void qc.invalidateQueries({ queryKey: ['layouts', siteId] });
      void qc.invalidateQueries({ queryKey: ['layout', layout!.id] });
      onClose();
  },
    onError: (err) =>
      toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Save failed.'),
  });

  const onSubmit = (data: LayoutForm) => {
    if (layout) {
      updateMutation.mutate(data);
    } else {
      createMutation.mutate(data);
    }
  };

  const isPending = createMutation.isPending || updateMutation.isPending;

  return (
    <div className="fixed inset-0 z-50 flex items-start justify-end bg-black/30">
      <div className="flex h-full w-full max-w-2xl flex-col bg-white shadow-xl">
        {/* Header */}
   <div className="flex items-center justify-between border-b border-slate-200 px-6 py-4">
          <h2 className="text-base font-semibold text-slate-900">
      {layout ? `Edit: ${layout.name}` : 'New Layout'}
          </h2>
       <button
 onClick={onClose}
            className="rounded-lg p-1.5 text-slate-400 hover:bg-slate-100 hover:text-slate-600"
   >
         <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
       <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
        </svg>
          </button>
      </div>

        {/* Body */}
        <form onSubmit={form.handleSubmit(onSubmit)} className="flex flex-1 flex-col overflow-hidden">
<div className="flex-1 overflow-y-auto space-y-5 px-6 py-5">
     {/* Name */}
    <div>
   <label className="form-label">Name</label>
    <input
  className="form-input mt-1"
 {...form.register('name')}
                placeholder="Blog Layout"
       />
              {form.formState.errors.name && (
  <p className="form-error">{form.formState.errors.name.message}</p>
      )}
         </div>

            {/* Key */}
  <div>
   <label className="form-label">Key</label>
     <input
                className="form-input mt-1 font-mono"
   {...form.register('key')}
           placeholder="blog-layout"
     disabled={!!layout}
           />
   {form.formState.errors.key && (
        <p className="form-error">{form.formState.errors.key.message}</p>
       )}
    <p className="mt-1 text-xs text-slate-400">Unique machine-readable identifier. Cannot be changed after creation.</p>
        </div>

     {/* Template type */}
            <div>
 <label className="form-label">Template Type</label>
     <div className="mt-2 grid grid-cols-2 gap-3">
{(['Handlebars', 'Html'] as LayoutTemplateType[]).map((t) => (
       <label
    key={t}
             className={`flex cursor-pointer flex-col rounded-lg border-2 p-3 transition-colors ${
     templateType === t
           ? 'border-brand-500 bg-brand-50'
    : 'border-slate-200 hover:border-slate-300'
        }`}
 >
        <input
     type="radio"
   value={t}
        {...form.register('templateType')}
         className="sr-only"
         />
      <span className="text-sm font-semibold text-slate-800">{t}</span>
        <span className="mt-0.5 text-xs text-slate-500">
       {t === 'Handlebars'
   ? '{{#if}}, {{#each}}, {{{zone_hero_zone}}} — recommended'
       : '{{zone:name}} token replacement — simple HTML'}
      </span>
      </label>
       ))}
     </div>
            </div>

            {/* Shell template */}
            <div className="flex flex-1 flex-col">
  <div className="flex items-center justify-between">
       <label className="form-label">Shell Template</label>
     <button
          type="button"
                  onClick={() =>
            form.setValue('shellTemplate', TEMPLATE_PLACEHOLDER[templateType])
   }
   className="text-xs text-brand-600 hover:underline"
        >
         Insert starter template
      </button>
        </div>
       <div className="mt-2 rounded-lg border border-slate-200 bg-slate-50 p-1">
             <div className="mb-1 flex items-center gap-2 px-2 pt-1">
         <span className={`rounded px-1.5 py-0.5 text-[10px] font-semibold ${TEMPLATE_TYPE_BADGE[templateType]}`}>
         {templateType}
       </span>
    <span className="text-[10px] text-slate-400">
 {templateType === 'Handlebars'
  ? 'Use {{{zone_hero_zone}}} (triple-stash, hyphens → underscores) for unescaped HTML'
    : 'Use {{zone:hero-zone}} tokens'}
       </span>
            </div>
                <textarea
  className="w-full rounded-md border-0 bg-white font-mono text-xs text-slate-800 focus:ring-0 p-3 outline-none resize-none"
     rows={20}
          placeholder={TEMPLATE_PLACEHOLDER[templateType]}
       {...form.register('shellTemplate')}
            />
   </div>
            <p className="mt-1 text-xs text-slate-400">
         SEO tokens: <code className="font-mono">{'{{seo:title}}'}</code>,{' '}
    <code className="font-mono">{'{{seo:description}}'}</code>,{' '}
      <code className="font-mono">{'{{seo:ogImage}}'}</code>
   </p>
         </div>
    </div>

          {/* Footer */}
 <div className="flex items-center justify-end gap-3 border-t border-slate-200 px-6 py-4">
         <button type="button" onClick={onClose} className="btn-secondary">
       Cancel
   </button>
            <button type="submit" disabled={isPending} className="btn-primary">
    {isPending ? 'Saving…' : layout ? 'Save Changes' : 'Create Layout'}
    </button>
          </div>
   </form>
      </div>
    </div>
  );
}

// ─── Layout list row ──────────────────────────────────────────────────────────

function LayoutRow({
  layout,
  siteId,
  onEdit,
}: {
  layout: LayoutListItem;
  siteId: string;
  onEdit: (id: string) => void;
}) {
  const qc = useQueryClient();

  const setDefaultMutation = useMutation({
    mutationFn: () => layoutsApi.setDefault(layout.id, siteId),
    onSuccess: () => {
      toast.success(`"${layout.name}" is now the default layout.`);
      void qc.invalidateQueries({ queryKey: ['layouts', siteId] });
    },
    onError: (err) =>
      toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Failed.'),
  });

  const deleteMutation = useMutation({
    mutationFn: () => layoutsApi.delete(layout.id),
    onSuccess: () => {
      toast.success('Layout deleted.');
      void qc.invalidateQueries({ queryKey: ['layouts', siteId] });
    },
    onError: (err) =>
      toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Delete failed.'),
  });

  return (
    <tr className="hover:bg-slate-50">
    <td className="py-3 pl-4 pr-3">
        <div className="flex items-center gap-2">
 <span className="text-sm font-medium text-slate-900">{layout.name}</span>
          {layout.isDefault && (
            <span className="rounded-full bg-green-100 px-2 py-0.5 text-[10px] font-semibold text-green-700">
              Default
        </span>
    )}
        </div>
 <p className="font-mono text-xs text-slate-400">{layout.key}</p>
      </td>
      <td className="px-3 py-3">
        <span className={`rounded px-2 py-0.5 text-xs font-medium ${TEMPLATE_TYPE_BADGE[layout.templateType]}`}>
      {layout.templateType}
     </span>
      </td>
      <td className="px-3 py-3 text-xs text-slate-500">
        {new Date(layout.updatedAt).toLocaleDateString()}
      </td>
      <td className="py-3 pl-3 pr-4">
        <div className="flex items-center justify-end gap-2">
          {!layout.isDefault && (
    <button
  onClick={() => setDefaultMutation.mutate()}
         disabled={setDefaultMutation.isPending}
              className="text-xs text-brand-600 hover:underline disabled:opacity-50"
       >
   Set default
       </button>
     )}
          <button
      onClick={() => onEdit(layout.id)}
            className="text-xs text-slate-600 hover:text-slate-900 hover:underline"
        >
            Edit
   </button>
      <button
        onClick={() => {
              if (confirm(`Delete layout "${layout.name}"? Pages using it will fall back to the site default.`)) {
    deleteMutation.mutate();
     }
            }}
  disabled={deleteMutation.isPending}
  className="text-xs text-red-400 hover:text-red-600 disabled:opacity-50"
    >
         Delete
          </button>
        </div>
      </td>
    </tr>
  );
}

// ─── Page ─────────────────────────────────────────────────────────────────────

export default function LayoutsPage() {
  const { selectedSiteId, selectedSite, isLoading: siteLoading } = useSite();
  const siteId = selectedSiteId ?? '';
  const [editorLayout, setEditorLayout] = useState<LayoutDto | null | 'new'>('new');
  const [editorOpen, setEditorOpen] = useState(false);

  const { data: layouts, isLoading } = useQuery({
    queryKey: ['layouts', siteId],
    queryFn: () => layoutsApi.list(siteId),
    enabled: !!siteId,
  });

  const openNew = () => {
    setEditorLayout(null);
    setEditorOpen(true);
  };

  const openEdit = async (id: string) => {
    try {
      const layout = await layoutsApi.get(id);
      setEditorLayout(layout);
      setEditorOpen(true);
    } catch {
      toast.error('Could not load layout.');
    }
  };

  if (siteLoading) {
    return (
      <div className="space-y-3">
      {Array.from({ length: 4 }).map((_, i) => (
          <div key={i} className="h-12 animate-pulse rounded-lg bg-slate-100" />
    ))}
      </div>
    );
  }

  if (!siteId) {
    return (
      <div className="flex flex-col items-center justify-center gap-3 py-24 text-center">
        <svg className="h-10 w-10 text-slate-300" fill="none" viewBox="0 0 24 24" stroke="currentColor">
 <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M4 5a1 1 0 011-1h4a1 1 0 011 1v4a1 1 0 01-1 1H5a1 1 0 01-1-1V5zm10 0a1 1 0 011-1h4a1 1 0 011 1v4a1 1 0 01-1 1h-4a1 1 0 01-1-1V5zM4 15a1 1 0 011-1h4a1 1 0 011 1v4a1 1 0 01-1 1H5a1 1 0 01-1-1v-4zm10 0a1 1 0 011-1h4a1 1 0 011 1v4a1 1 0 01-1 1h-4a1 1 0 01-1-1v-4z" />
        </svg>
        <p className="text-sm font-medium text-slate-500">No site selected. Choose a site from the top bar.</p>
   </div>
    );
  }

  return (
    <>
      {/* Editor panel */}
      {editorOpen && (
        <LayoutEditor
       layout={editorLayout === 'new' ? null : editorLayout}
          siteId={siteId}
          onClose={() => setEditorOpen(false)}
        />
      )}

      <div className="space-y-6">
        {/* Header */}
        <div className="flex items-center justify-between">
          <div>
      <h1 className="text-2xl font-bold text-slate-900">Layouts</h1>
      <p className="mt-1 text-sm text-slate-500">
   Master HTML shells for{' '}
        <span className="font-medium text-slate-700">{selectedSite?.name}</span>. Layouts
              wrap rendered page zones into complete HTML documents.
     </p>
          </div>
    <button onClick={openNew} className="btn-primary">
            <svg className="mr-2 h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
            </svg>
            New Layout
</button>
        </div>

        {/* Zone token reference */}
        <div className="rounded-lg border border-slate-200 bg-slate-50 p-4">
          <p className="text-xs font-semibold text-slate-600 mb-2">Template token reference</p>
        <div className="grid grid-cols-2 gap-x-8 gap-y-1 text-xs text-slate-500">
   <span><code className="font-mono text-slate-700">{'{{zone:name}}'}</code> — Html: inject zone HTML</span>
     <span><code className="font-mono text-slate-700">{'{{seo:title}}'}</code> — page/entry SEO title</span>
  <span><code className="font-mono text-slate-700">{'{{{zone_name}}}'}</code> — Handlebars: same (unescaped)</span>
            <span><code className="font-mono text-slate-700">{'{{seo:description}}'}</code> — meta description</span>
            <span><code className="font-mono text-slate-700">{'{{#if zone_hero}}'}</code> — Handlebars conditional</span>
    <span><code className="font-mono text-slate-700">{'{{seo:ogImage}}'}</code> — OpenGraph image URL</span>
          </div>
        </div>

        {/* Table */}
 <div className="card overflow-hidden p-0">
 {isLoading ? (
            <div className="space-y-2 p-4">
              {Array.from({ length: 3 }).map((_, i) => (
                <div key={i} className="h-10 animate-pulse rounded bg-slate-100" />
              ))}
 </div>
   ) : (layouts ?? []).length === 0 ? (
            <div className="flex flex-col items-center gap-3 py-16 text-center">
              <svg className="h-8 w-8 text-slate-300" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M4 5a1 1 0 011-1h14a1 1 0 011 1v2a1 1 0 01-1 1H5a1 1 0 01-1-1V5zm0 8a1 1 0 011-1h6a1 1 0 011 1v6a1 1 0 01-1 1H5a1 1 0 01-1-1v-6zm12 0a1 1 0 011-1h2a1 1 0 011 1v6a1 1 0 01-1 1h-2a1 1 0 01-1-1v-6z" />
              </svg>
         <p className="text-sm text-slate-500">No layouts yet.</p>
              <button onClick={openNew} className="btn-secondary text-xs">
            Create your first layout
       </button>
     </div>
     ) : (
            <table className="w-full text-left">
              <thead className="border-b border-slate-200 bg-slate-50 text-xs font-semibold text-slate-500 uppercase tracking-wider">
           <tr>
  <th className="py-3 pl-4 pr-3">Name / Key</th>
          <th className="px-3 py-3">Type</th>
         <th className="px-3 py-3">Updated</th>
         <th className="py-3 pl-3 pr-4 text-right">Actions</th>
                </tr>
          </thead>
          <tbody className="divide-y divide-slate-100">
      {(layouts ?? []).map((l) => (
           <LayoutRow key={l.id} layout={l} siteId={siteId} onEdit={openEdit} />
     ))}
     </tbody>
      </table>
    )}
     </div>
      </div>
    </>
  );
}
