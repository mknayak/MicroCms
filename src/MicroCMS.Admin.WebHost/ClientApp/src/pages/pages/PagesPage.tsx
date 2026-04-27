import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm, useFieldArray } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import toast from 'react-hot-toast';
import { pagesApi } from '@/api/pages';
import { layoutsApi } from '@/api/layouts';
import { contentTypesApi } from '@/api/contentTypes';
import { componentsApi } from '@/api/components';
import { entriesApi } from '@/api/entries';
import { useSite } from '@/contexts/SiteContext';
import type {
  PageTreeNode,
  PageDto,
  ContentTypeListItem,
  LayoutListItem,
  PageTemplateDto,
  PageTemplatePlacementInput,
  ComponentListItem,
} from '@/types';
import { ApiError } from '@/api/client';

// ─── Schemas ──────────────────────────────────────────────────────────────────

const slugPattern = /^[a-z0-9]+(?:-[a-z0-9]+)*$/;

const staticSchema = z.object({
  title: z.string().min(1, 'Title is required'),
  slug: z.string().min(1, 'Slug is required').regex(slugPattern, 'Lowercase letters, numbers and hyphens only'),
  parentId: z.string().optional().transform((v) => (v === '' ? undefined : v)),
});

const collectionSchema = z.object({
  title: z.string().min(1, 'Title is required'),
  slug: z.string().min(1, 'Slug is required').regex(slugPattern, 'Lowercase letters, numbers and hyphens only'),
  contentTypeId: z.string().min(1, 'Content type is required'),
  routePattern: z.string().min(1, 'Route pattern is required'),
  parentId: z.string().optional().transform((v) => (v === '' ? undefined : v)),
});

const templateSchema = z.object({
  placements: z.array(
    z.object({
      componentId: z.string().min(1, 'Component required'),
      zone: z.string().min(1, 'Zone required'),
      sortOrder: z.number().int().min(0),
    }),
  ),
});

const seoSchema = z.object({
  metaTitle: z.string().max(60, 'Max 60 characters').optional().or(z.literal('')),
  metaDescription: z.string().max(160, 'Max 160 characters').optional().or(z.literal('')),
  canonicalUrl: z.string().url('Must be a valid URL').optional().or(z.literal('')),
  ogImage: z.string().url('Must be a valid URL').optional().or(z.literal('')),
});

type StaticForm = z.infer<typeof staticSchema>;
type CollectionForm = z.infer<typeof collectionSchema>;
type TemplateForm = z.infer<typeof templateSchema>;
type SeoForm = z.infer<typeof seoSchema>;

// ─── Page detail panel ────────────────────────────────────────────────────────

function PageDetailPanel({
  page,
  siteId,
  layouts,
  components,
  onClose,
}: {
  page: PageTreeNode;
  siteId: string;
  layouts: LayoutListItem[];
  components: ComponentListItem[];
  onClose: () => void;
}) {
  const qc = useQueryClient();
  const isStatic = page.pageType === 'Static';
  const [tab, setTab] = useState<'layout' | 'template' | 'seo' | 'entry'>(
    isStatic ? 'entry' : 'layout'
  );
  const [selectedLayoutId, setSelectedLayoutId] = useState<string>(page.layoutId ?? '');

  // ── Fetch full page detail (for linkedEntryId + SEO) ────────────────────
  const { data: pageDetail } = useQuery<PageDto>({
    queryKey: ['page-detail', page.id],
    queryFn: () => pagesApi.getPage(page.id),
  });

  // ── Layout ──────────────────────────────────────────────────────────────
  const setLayoutMutation = useMutation({
    mutationFn: () => pagesApi.setLayout(page.id, { layoutId: selectedLayoutId || null }),
    onSuccess: () => {
    toast.success('Layout saved.');
      void qc.invalidateQueries({ queryKey: ['pages', siteId] });
    },
    onError: (err) =>
    toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Failed.'),
  });

  // ── Template ────────────────────────────────────────────────────────────
  const { data: existingTemplate, isLoading: templateLoading } = useQuery<PageTemplateDto | null>({
    queryKey: ['page-template', page.id],
    queryFn: async () => {
      try { return await pagesApi.getTemplate(page.id); }
      catch (e) {
     if (e instanceof ApiError && e.status === 404) return null;
        throw e;
    }
    },
  });

  const templateForm = useForm<TemplateForm>({
    resolver: zodResolver(templateSchema),
    values: {
   placements: (existingTemplate?.placements ?? []).map((p) => ({
     componentId: p.componentId,
        zone: p.zone,
        sortOrder: p.sortOrder,
      })),
    },
  });
  const { fields, append, remove } = useFieldArray({ control: templateForm.control, name: 'placements' });

  const saveTemplateMutation = useMutation({
    mutationFn: (data: TemplateForm) =>
      pagesApi.saveTemplate(page.id, { placements: data.placements as PageTemplatePlacementInput[] }),
    onSuccess: () => {
      toast.success('Page template saved.');
    void qc.invalidateQueries({ queryKey: ['page-template', page.id] });
  },
    onError: (err) =>
      toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Failed.'),
  });

  // ── SEO ─────────────────────────────────────────────────────────────────
  const seoForm = useForm<SeoForm>({
    resolver: zodResolver(seoSchema),
    values: {
      metaTitle: pageDetail?.seo?.metaTitle ?? '',
      metaDescription: pageDetail?.seo?.metaDescription ?? '',
      canonicalUrl: pageDetail?.seo?.canonicalUrl ?? '',
      ogImage: pageDetail?.seo?.ogImage ?? '',
    },
  });

  const saveSeoMutation = useMutation({
    mutationFn: (data: SeoForm) =>
      pagesApi.setSeo(page.id, {
        metaTitle: data.metaTitle || null,
        metaDescription: data.metaDescription || null,
canonicalUrl: data.canonicalUrl || null,
        ogImage: data.ogImage || null,
  }),
    onSuccess: (updated) => {
      toast.success('SEO settings saved.');
      seoForm.reset({
        metaTitle: updated.seo?.metaTitle ?? '',
      metaDescription: updated.seo?.metaDescription ?? '',
   canonicalUrl: updated.seo?.canonicalUrl ?? '',
        ogImage: updated.seo?.ogImage ?? '',
      });
   void qc.invalidateQueries({ queryKey: ['page-detail', page.id] });
    },
    onError: (err) =>
      toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Failed.'),
  });

  const metaTitleValue = seoForm.watch('metaTitle') ?? '';
  const metaDescValue = seoForm.watch('metaDescription') ?? '';

  // ── Linked entry ─────────────────────────────────────────────────────────
  const [entrySearch, setEntrySearch] = useState('');
  const [selectedEntryId, setSelectedEntryId] = useState<string>(
    pageDetail?.linkedEntryId ?? ''
  );
  // Sync selectedEntryId once pageDetail loads
  const linkedEntryId = pageDetail?.linkedEntryId ?? null;

  const { data: entryResults, isFetching: entriesFetching } = useQuery({
    queryKey: ['entries-search', siteId, entrySearch],
    queryFn: () => entriesApi.list({ siteId, search: entrySearch || undefined, pageSize: 30 }),
    enabled: isStatic && tab === 'entry',
 staleTime: 10_000,
  });

  const setLinkedEntryMutation = useMutation({
    mutationFn: () =>
      pagesApi.setLinkedEntry(page.id, { entryId: selectedEntryId || null }),
    onSuccess: (updated) => {
      toast.success(updated.linkedEntryId ? 'Entry linked.' : 'Entry link cleared.');
      void qc.invalidateQueries({ queryKey: ['page-detail', page.id] });
      void qc.invalidateQueries({ queryKey: ['pages', siteId] });
    },
    onError: (err) =>
      toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Failed.'),
  });

  const selectedLayout = layouts.find((l) => l.id === selectedLayoutId);
  const tabs = isStatic
    ? (['entry', 'layout', 'template', 'seo'] as const)
    : (['layout', 'template', 'seo'] as const);

  const tabLabel = (t: string) => {
 if (t === 'entry') return 'Linked Entry';
    if (t === 'layout') return 'Layout';
    if (t === 'template') return 'Zone Placements';
    return 'SEO';
  };

  return (
    <div className="fixed inset-0 z-50 flex items-start justify-end bg-black/30">
      <div className="flex h-full w-full max-w-xl flex-col bg-white shadow-xl">
        {/* Header */}
        <div className="flex items-center justify-between border-b border-slate-200 px-5 py-4">
     <div>
    <h2 className="text-base font-semibold text-slate-900">{page.title}</h2>
            <p className="font-mono text-xs text-slate-400">/{page.slug}</p>
          </div>
          <button onClick={onClose} className="rounded-lg p-1.5 text-slate-400 hover:bg-slate-100">
            <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
       </svg>
  </button>
        </div>

  {/* Tabs */}
  <div className="flex border-b border-slate-200 px-5">
          {tabs.map((t) => (
        <button
              key={t}
     onClick={() => setTab(t)}
      className={`mr-4 border-b-2 py-3 text-sm font-medium transition-colors ${
            tab === t
               ? 'border-brand-600 text-brand-600'
   : 'border-transparent text-slate-500 hover:text-slate-700'
       }`}
        >
      {tabLabel(t)}
            </button>
    ))}
        </div>

        {/* Body */}
        <div className="flex-1 overflow-y-auto p-5">

   {/* ── Linked Entry tab ── */}
   {tab === 'entry' && (
   <div className="space-y-4">
         <p className="text-sm text-slate-500">
      Link this Static page to a backend entry. The render pipeline uses the entry's
        fields and SEO metadata at delivery time.
      </p>

         {/* Current link indicator */}
{linkedEntryId ? (
    <div className="flex items-center justify-between rounded-lg border border-green-200 bg-green-50 px-3 py-2.5">
        <div>
  <p className="text-xs font-semibold text-green-800">Currently linked</p>
                <p className="font-mono text-[11px] text-green-700">{linkedEntryId}</p>
          </div>
            <button
    onClick={() => {
              setSelectedEntryId('');
   setLinkedEntryMutation.mutate();
    }}
               className="ml-3 flex-shrink-0 text-xs text-red-500 hover:text-red-700"
     >
         Clear
       </button>
    </div>
              ) : (
         <div className="rounded-lg border border-dashed border-slate-200 py-3 text-center text-xs text-slate-400">
            No entry linked — page will render without entry data.
        </div>
     )}

              {/* Search box */}
     <div>
                <label className="form-label">Search entries</label>
     <input
    className="form-input mt-1"
              placeholder="Type to filter by title or slug…"
      value={entrySearch}
            onChange={(e) => setEntrySearch(e.target.value)}
   />
           </div>

              {/* Entry list */}
    <div className="max-h-72 overflow-y-auto rounded-lg border border-slate-200 divide-y divide-slate-100">
           {entriesFetching && (
           <div className="space-y-1 p-2">
      {Array.from({ length: 4 }).map((_, i) => (
            <div key={i} className="h-8 animate-pulse rounded bg-slate-100" />
      ))}
      </div>
      )}
                {!entriesFetching && (entryResults?.items ?? []).length === 0 && (
        <p className="p-4 text-center text-xs text-slate-400">No entries found.</p>
    )}
     {!entriesFetching &&
    (entryResults?.items ?? []).map((entry) => {
             const isSelected = selectedEntryId === entry.id;
        const isCurrent = linkedEntryId === entry.id;
     return (
            <button
       key={entry.id}
             type="button"
              onClick={() => setSelectedEntryId(isSelected ? '' : entry.id)}
  className={`flex w-full items-center justify-between px-3 py-2.5 text-left text-sm transition-colors hover:bg-slate-50 ${
               isSelected ? 'bg-brand-50' : ''
        }`}
        >
      <div className="min-w-0">
      <p className="truncate font-medium text-slate-800">{entry.title}</p>
     <p className="font-mono text-[10px] text-slate-400">
    {entry.contentTypeName} · {entry.slug}
       </p>
     </div>
         <div className="ml-2 flex flex-shrink-0 gap-1.5">
          {isCurrent && (
              <span className="rounded-full bg-green-100 px-2 py-0.5 text-[10px] font-semibold text-green-700">
       current
             </span>
     )}
      <span
  className={`rounded-full px-2 py-0.5 text-[10px] font-semibold ${
             entry.status === 'Published'
     ? 'bg-green-100 text-green-700'
      : entry.status === 'Draft'
   ? 'bg-slate-100 text-slate-600'
          : 'bg-amber-100 text-amber-700'
       }`}
         >
               {entry.status}
       </span>
           </div>
       </button>
        );
})}
              </div>

          <button
              disabled={setLinkedEntryMutation.isPending || selectedEntryId === (linkedEntryId ?? '')}
       onClick={() => setLinkedEntryMutation.mutate()}
              className="btn-primary w-full justify-center disabled:opacity-50"
      >
       {setLinkedEntryMutation.isPending
    ? 'Saving…'
     : selectedEntryId
      ? 'Link Selected Entry'
      : 'Save (clear link)'}
       </button>
   </div>
          )}

          {/* ── Layout tab ── */}
       {tab === 'layout' && (
 <div className="space-y-4">
              <p className="text-sm text-slate-500">
           Choose which layout wraps this page. Leave empty to inherit the site default.
            </p>
    <div>
      <label className="form-label">Layout</label>
    <select
         className="form-input mt-1"
                value={selectedLayoutId}
       onChange={(e) => setSelectedLayoutId(e.target.value)}
                >
      <option value="">— Use site default —</option>
    {layouts.map((l) => (
      <option key={l.id} value={l.id}>
    {l.name} {l.isDefault ? '(default)' : ''}
      </option>
           ))}
    </select>
   </div>
           {selectedLayout && (
       <div className="rounded-lg border border-slate-200 bg-slate-50 p-3 text-xs text-slate-500">
     <span className="font-medium text-slate-700">{selectedLayout.name}</span>
       {' · '}
<span className="font-mono">{selectedLayout.key}</span>
      {' · '}
        <span
    className={`rounded px-1.5 py-0.5 text-[10px] font-semibold ${
   selectedLayout.templateType === 'Handlebars'
        ? 'bg-amber-100 text-amber-800'
     : 'bg-slate-200 text-slate-700'
          }`}
       >
          {selectedLayout.templateType}
        </span>
       </div>
      )}
     <button
     onClick={() => setLayoutMutation.mutate()}
      disabled={setLayoutMutation.isPending}
           className="btn-primary w-full justify-center"
              >
      {setLayoutMutation.isPending ? 'Saving…' : 'Save Layout'}
  </button>
 </div>
          )}

          {/* ── Zone Placements tab ── */}
     {tab === 'template' && (
  <form onSubmit={templateForm.handleSubmit((v) => saveTemplateMutation.mutate(v))} className="space-y-4">
              <p className="text-sm text-slate-500">
   Add components to layout zones. Lower sort-order renders first within each zone.
        </p>
    {templateLoading ? (
                <div className="space-y-2">
     {Array.from({ length: 3 }).map((_, i) => (
            <div key={i} className="h-12 animate-pulse rounded bg-slate-100" />
))}
           </div>
              ) : (
            <>
       {fields.length === 0 && (
          <p className="rounded-lg border border-dashed border-slate-200 py-6 text-center text-sm text-slate-400">
 No placements yet. Add components below.
     </p>
    )}
            <div className="space-y-3">
           {fields.map((field, idx) => (
     <div
        key={field.id}
    className="grid grid-cols-[1fr_1fr_4rem_2rem] gap-2 rounded-lg border border-slate-200 p-3"
        >
            <div>
  <label className="text-[10px] font-semibold uppercase text-slate-400">Component</label>
         <select className="form-input mt-0.5 text-xs" {...templateForm.register(`placements.${idx}.componentId`)}>
           <option value="">Select…</option>
   {components.map((c) => (
              <option key={c.id} value={c.id}>{c.name}</option>
                 ))}
              </select>
            </div>
            <div>
    <label className="text-[10px] font-semibold uppercase text-slate-400">Zone</label>
 <input className="form-input mt-0.5 font-mono text-xs" placeholder="hero-zone" {...templateForm.register(`placements.${idx}.zone`)} />
    </div>
 <div>
  <label className="text-[10px] font-semibold uppercase text-slate-400">Order</label>
        <input type="number" min={0} className="form-input mt-0.5 text-xs" {...templateForm.register(`placements.${idx}.sortOrder`, { valueAsNumber: true })} />
      </div>
   <div className="flex items-end pb-0.5">
      <button type="button" onClick={() => remove(idx)} className="text-red-400 hover:text-red-600">
     <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
        </svg>
    </button>
                 </div>
         </div>
         ))}
                  </div>
           <button type="button" onClick={() => append({ componentId: '', zone: '', sortOrder: fields.length })} className="btn-secondary w-full justify-center text-sm">
          <svg className="mr-1.5 h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
    </svg>
        Add Placement
       </button>
     <button type="submit" disabled={saveTemplateMutation.isPending} className="btn-primary w-full justify-center">
        {saveTemplateMutation.isPending ? 'Saving…' : 'Save Template'}
           </button>
      </>
    )}
            </form>
          )}

    {/* ── SEO tab ── */}
          {tab === 'seo' && (
    <form onSubmit={seoForm.handleSubmit((v) => saveSeoMutation.mutate(v))} className="space-y-5">
  <p className="text-sm text-slate-500">
   Page-level SEO overrides. These populate{' '}
    <code className="rounded bg-slate-100 px-1 text-xs">{`{{seo:title}}`}</code>,{' '}
                <code className="rounded bg-slate-100 px-1 text-xs">{`{{seo:description}}`}</code>, and{' '}
           <code className="rounded bg-slate-100 px-1 text-xs">{`{{seo:ogImage}}`}</code> tokens.
         Leave blank to inherit from the linked entry.
     </p>
   {/* SERP preview */}
      <div className="rounded-lg border border-slate-200 bg-white p-4">
            <p className="mb-2 text-[10px] font-bold uppercase tracking-wider text-slate-400">SERP Preview</p>
                <p className="truncate text-sm font-medium text-[#1a0dab]">{metaTitleValue || page.title}</p>
              <p className="mt-0.5 font-mono text-xs text-[#006621]">yoursite.com/{page.slug}</p>
         <p className="mt-1 line-clamp-2 text-xs text-slate-600">
         {metaDescValue || <span className="italic text-slate-400">No meta description set.</span>}
    </p>
    </div>
       <div>
      <div className="flex items-center justify-between">
      <label className="form-label">Meta Title</label>
        <span className={`text-xs font-medium ${metaTitleValue.length > 55 ? 'text-amber-600' : 'text-slate-400'}`}>{metaTitleValue.length}/60</span>
    </div>
     <input className="form-input mt-1" placeholder={page.title} {...seoForm.register('metaTitle')} />
    {seoForm.formState.errors.metaTitle && <p className="form-error">{seoForm.formState.errors.metaTitle.message}</p>}
              </div>
 <div>
       <div className="flex items-center justify-between">
            <label className="form-label">Meta Description</label>
       <span className={`text-xs font-medium ${metaDescValue.length > 150 ? 'text-amber-600' : 'text-slate-400'}`}>{metaDescValue.length}/160</span>
 </div>
       <textarea rows={3} className="form-input mt-1 resize-none" placeholder="Brief description…" {...seoForm.register('metaDescription')} />
                {seoForm.formState.errors.metaDescription && <p className="form-error">{seoForm.formState.errors.metaDescription.message}</p>}
        </div>
<div>
             <label className="form-label">Canonical URL</label>
    <input className="form-input mt-1 font-mono text-sm" placeholder="https://yoursite.com/page" {...seoForm.register('canonicalUrl')} />
         {seoForm.formState.errors.canonicalUrl && <p className="form-error">{seoForm.formState.errors.canonicalUrl.message}</p>}
           </div>
        <div>
       <label className="form-label">Open Graph Image URL</label>
         <input className="form-input mt-1 font-mono text-sm" placeholder="https://yoursite.com/og.png" {...seoForm.register('ogImage')} />
       {seoForm.formState.errors.ogImage && <p className="form-error">{seoForm.formState.errors.ogImage.message}</p>}
        </div>
        <button type="submit" disabled={saveSeoMutation.isPending} className="btn-primary w-full justify-center">
      {saveSeoMutation.isPending ? 'Saving…' : 'Save SEO Settings'}
         </button>
            </form>
          )}

        </div>
   </div>
    </div>
  );
}

// ─── Tree node ────────────────────────────────────────────────────────────────

const PAGE_TYPE_BADGE: Record<string, string> = {
  Static: 'badge-brand',
  Collection: 'badge-amber',
};

function TreeNode({
  node, siteId, allNodes, layouts, components, depth = 0,
}: {
  node: PageTreeNode;
  siteId: string;
  allNodes: PageTreeNode[];
  layouts: LayoutListItem[];
  components: ComponentListItem[];
  depth?: number;
}) {
  const qc = useQueryClient();
  const [detailOpen, setDetailOpen] = useState(false);

  const deleteMutation = useMutation({
    mutationFn: () => pagesApi.delete(node.id),
    onSuccess: () => {
      toast.success('Page deleted.');
      void qc.invalidateQueries({ queryKey: ['pages', siteId] });
    },
    onError: (err) =>
      toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Delete failed.'),
  });

  const assignedLayout = layouts.find((l) => l.id === node.layoutId);

  return (
    <>
      {detailOpen && (
   <PageDetailPanel
          page={node}
    siteId={siteId}
          layouts={layouts}
          components={components}
          onClose={() => setDetailOpen(false)}
        />
 )}
  <li>
   <div
      className="flex items-center justify-between rounded-lg py-2 pr-3 hover:bg-slate-50"
          style={{ paddingLeft: `${depth * 20 + 12}px` }}
        >
 <div className="flex min-w-0 items-center gap-2">
      {depth > 0 && <span className="flex-shrink-0 text-slate-300">└</span>}
 <span className="truncate text-sm font-medium text-slate-800">{node.title}</span>
  <span className="flex-shrink-0 font-mono text-xs text-slate-400">/{node.slug}</span>
          <span className={`flex-shrink-0 ${PAGE_TYPE_BADGE[node.pageType] ?? 'badge-slate'}`}>
{node.pageType}
   </span>
            {assignedLayout && (
     <span className="flex-shrink-0 rounded-full bg-purple-100 px-2 py-0.5 text-[10px] font-medium text-purple-700">
  {assignedLayout.name}
         </span>
  )}
          </div>
      <div className="ml-2 flex flex-shrink-0 items-center gap-3">
  <button
      onClick={() => setDetailOpen(true)}
         className="text-xs text-brand-600 hover:underline"
            >
           Configure
            </button>
  <button
        onClick={() => {
                if (confirm(`Delete page "${node.title}" and all its children?`)) deleteMutation.mutate();
    }}
              className="text-xs text-red-400 hover:text-red-600"
    >
   Delete
      </button>
      </div>
        </div>
        {node.children.length > 0 && (
          <ul>
            {node.children.map((child) => (
         <TreeNode
     key={child.id}
   node={child}
                siteId={siteId}
                allNodes={allNodes}
    layouts={layouts}
     components={components}
   depth={depth + 1}
 />
    ))}
          </ul>
)}
      </li>
    </>
  );
}

// ─── Flatten tree ─────────────────────────────────────────────────────────────

function flattenTree(nodes: PageTreeNode[], acc: PageTreeNode[] = []): PageTreeNode[] {
  for (const n of nodes) {
    acc.push(n);
    flattenTree(n.children, acc);
  }
  return acc;
}

// ─── Page ─────────────────────────────────────────────────────────────────────

export default function PagesPage() {
  const qc = useQueryClient();
  const { selectedSiteId, selectedSite, isLoading: siteLoading } = useSite();
  const siteId = selectedSiteId ?? '';
  const [createType, setCreateType] = useState<'static' | 'collection'>('static');

  const { data: tree, isLoading: treeLoading, isFetching } = useQuery({
    queryKey: ['pages', siteId],
    queryFn: () => pagesApi.getTree(siteId),
    enabled: !!siteId,
  });

  const { data: contentTypes } = useQuery({
    queryKey: ['content-types'],
    queryFn: () => contentTypesApi.list(),
  });

  const { data: layouts } = useQuery({
    queryKey: ['layouts', siteId],
    queryFn: () => layoutsApi.list(siteId),
    enabled: !!siteId,
  });

  const { data: componentsData } = useQuery({
    queryKey: ['components', siteId],
    queryFn: () => componentsApi.list({ siteId, pageSize: 200 }),
    enabled: !!siteId,
  });

  const staticForm = useForm<StaticForm>({ resolver: zodResolver(staticSchema) });
  const createStaticMutation = useMutation({
    mutationFn: (data: StaticForm) => pagesApi.createStatic({ siteId, ...data }),
    onSuccess: () => {
      toast.success('Static page created.');
      staticForm.reset();
      void qc.invalidateQueries({ queryKey: ['pages', siteId] });
    },
    onError: (err) =>
    toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Failed.'),
  });

  const collectionForm = useForm<CollectionForm>({ resolver: zodResolver(collectionSchema) });
  const createCollectionMutation = useMutation({
    mutationFn: (data: CollectionForm) => pagesApi.createCollection({ siteId, ...data }),
    onSuccess: () => {
      toast.success('Collection page created.');
      collectionForm.reset();
      void qc.invalidateQueries({ queryKey: ['pages', siteId] });
    },
    onError: (err) =>
      toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Failed.'),
  });

  const flatPages = flattenTree(tree ?? []);
  const allLayouts = layouts ?? [];
  const allComponents = componentsData?.items ?? [];

  // ── No site selected guard ──────────────────────────────────────────────
  if (siteLoading) {
    return (
  <div className="space-y-4">
  {Array.from({ length: 6 }).map((_, i) => (
          <div key={i} className="h-10 animate-pulse rounded-lg bg-slate-100" />
     ))}
      </div>
    );
  }

  if (!siteId) {
    return (
      <div className="flex flex-col items-center justify-center gap-3 py-24 text-center">
 <svg className="h-10 w-10 text-slate-300" fill="none" viewBox="0 0 24 24" stroke="currentColor">
      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M3 7h18M3 12h18M3 17h18" />
        </svg>
        <p className="text-sm font-medium text-slate-500">No site selected. Choose a site from the top bar.</p>
    </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
<div>
        <h1 className="text-2xl font-bold text-slate-900">Pages</h1>
        <p className="mt-1 text-sm text-slate-500">
          Page tree for <span className="font-medium text-slate-700">{selectedSite?.name}</span>.
          Click <span className="font-medium">Configure</span> to assign a layout or manage zone placements.
        </p>
      </div>

  {/* Active layouts quick-reference */}
      {allLayouts.length > 0 && (
<div className="flex flex-wrap gap-2">
          {allLayouts.map((l) => (
            <span
    key={l.id}
  className="inline-flex items-center gap-1.5 rounded-full border border-slate-200 bg-white px-3 py-1 text-xs text-slate-600"
 >
   <span className={`h-2 w-2 rounded-full ${l.isDefault ? 'bg-green-400' : 'bg-slate-300'}`} />
            {l.name}
  <span
              className={`rounded px-1 text-[9px] font-semibold ${
        l.templateType === 'Handlebars' ? 'bg-amber-100 text-amber-700' : 'bg-slate-100 text-slate-500'
                }`}
              >
    {l.templateType}
          </span>
            </span>
          ))}
        </div>
 )}

      <div className="grid grid-cols-3 gap-6">
        {/* Tree */}
        <div className="col-span-2 card">
   <div className="mb-4 flex items-center justify-between">
     <h2 className="text-base font-semibold text-slate-900">Page Tree</h2>
  {isFetching && <span className="text-xs text-slate-400">Refreshing…</span>}
 </div>
          {treeLoading ? (
     <div className="space-y-2">
           {Array.from({ length: 5 }).map((_, i) => (
                <div key={i} className="h-8 animate-pulse rounded bg-slate-100" />
     ))}
 </div>
        ) : (tree ?? []).length === 0 ? (
            <p className="text-sm text-slate-400">No pages yet. Create one →</p>
  ) : (
            <ul className="divide-y divide-slate-50">
       {(tree ?? []).map((node) => (
                <TreeNode
          key={node.id}
     node={node}
siteId={siteId}
   allNodes={flatPages}
       layouts={allLayouts}
           components={allComponents}
      />
         ))}
            </ul>
          )}
        </div>

        {/* Create forms */}
        <div className="card space-y-4">
   <div className="flex rounded-lg bg-slate-100 p-1">
            {(['static', 'collection'] as const).map((t) => (
   <button
      key={t}
 onClick={() => setCreateType(t)}
        className={`flex-1 rounded-md px-3 py-1.5 text-xs font-medium capitalize transition-colors ${
                  createType === t ? 'bg-white text-slate-900 shadow-sm' : 'text-slate-500 hover:text-slate-700'
            }`}
       >
                {t}
              </button>
            ))}
          </div>

          {createType === 'static' ? (
        <form onSubmit={staticForm.handleSubmit((v) => createStaticMutation.mutate(v))} className="space-y-3">
        <h2 className="text-sm font-semibold text-slate-900">New Static Page</h2>
     <div>
           <label className="form-label">Title</label>
            <input className="form-input mt-1" {...staticForm.register('title')} placeholder="About Us" />
  {staticForm.formState.errors.title && (
    <p className="form-error">{staticForm.formState.errors.title.message}</p>
        )}
      </div>
          <div>
              <label className="form-label">Slug</label>
    <input className="form-input mt-1 font-mono" {...staticForm.register('slug')} placeholder="about-us" />
                {staticForm.formState.errors.slug && (
    <p className="form-error">{staticForm.formState.errors.slug.message}</p>
      )}
           </div>
    <div>
    <label className="form-label">Parent (optional)</label>
 <select className="form-input mt-1" {...staticForm.register('parentId')}>
 <option value="">None (root)</option>
       {flatPages.map((p) => (
  <option key={p.id} value={p.id}>{'—'.repeat(p.depth)} {p.title}</option>
    ))}
      </select>
      </div>
     <button type="submit" disabled={staticForm.formState.isSubmitting} className="btn-primary w-full justify-center">
      {staticForm.formState.isSubmitting ? 'Creating…' : 'Create Page'}
              </button>
            </form>
          ) : (
        <form onSubmit={collectionForm.handleSubmit((v) => createCollectionMutation.mutate(v))} className="space-y-3">
<h2 className="text-sm font-semibold text-slate-900">New Collection Page</h2>
  <div>
                <label className="form-label">Title</label>
          <input className="form-input mt-1" {...collectionForm.register('title')} placeholder="Blog" />
            {collectionForm.formState.errors.title && (
        <p className="form-error">{collectionForm.formState.errors.title.message}</p>
   )}
      </div>
   <div>
     <label className="form-label">Slug</label>
      <input className="form-input mt-1 font-mono" {...collectionForm.register('slug')} placeholder="blog" />
       {collectionForm.formState.errors.slug && (
      <p className="form-error">{collectionForm.formState.errors.slug.message}</p>
                )}
         </div>
 <div>
    <label className="form-label">Content Type</label>
                <select className="form-input mt-1" {...collectionForm.register('contentTypeId')}>
                  <option value="">Select…</option>
       {(contentTypes?.items ?? []).map((ct: ContentTypeListItem) => (
      <option key={ct.id} value={ct.id}>{ct.displayName}</option>
        ))}
           </select>
                {collectionForm.formState.errors.contentTypeId && (
      <p className="form-error">{collectionForm.formState.errors.contentTypeId.message}</p>
            )}
  </div>
    <div>
                <label className="form-label">Route Pattern</label>
            <input className="form-input mt-1 font-mono" {...collectionForm.register('routePattern')} placeholder="/blog/{slug}" />
    {collectionForm.formState.errors.routePattern && (
          <p className="form-error">{collectionForm.formState.errors.routePattern.message}</p>
            )}
     </div>
              <div>
    <label className="form-label">Parent (optional)</label>
       <select className="form-input mt-1" {...collectionForm.register('parentId')}>
    <option value="">None (root)</option>
          {flatPages.map((p) => (
     <option key={p.id} value={p.id}>{'—'.repeat(p.depth)} {p.title}</option>
           ))}
            </select>
        </div>
          <button type="submit" disabled={collectionForm.formState.isSubmitting} className="btn-primary w-full justify-center">
   {collectionForm.formState.isSubmitting ? 'Creating…' : 'Create Collection Page'}
      </button>
            </form>
          )}
     </div>
      </div>
    </div>
  );
}
