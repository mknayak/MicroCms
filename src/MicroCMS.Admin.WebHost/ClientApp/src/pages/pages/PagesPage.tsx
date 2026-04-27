import { useState, useMemo } from 'react';
import { Link } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import toast from 'react-hot-toast';
import { pagesApi } from '@/api/pages';
import { layoutsApi } from '@/api/layouts';
import { contentTypesApi } from '@/api/contentTypes';
import { useSite } from '@/contexts/SiteContext';
import type { PageTreeNode, PageDto, ContentTypeListItem, LayoutListItem } from '@/types';
import { ApiError } from '@/api/client';

// ─── Schemas ──────────────────────────────────────────────────────────────────

const slugPattern = /^[a-z0-9]+(?:-[a-z0-9]+)*$/;

const staticSchema = z.object({
  title: z.string().min(1, 'Title is required'),
  slug: z.string().min(1).regex(slugPattern, 'Lowercase, numbers and hyphens only'),
  parentId: z.string().optional().transform((v) => (v === '' ? undefined : v)),
});

const collectionSchema = z.object({
  title: z.string().min(1, 'Title is required'),
  slug: z.string().min(1).regex(slugPattern, 'Lowercase, numbers and hyphens only'),
  contentTypeId: z.string().min(1, 'Content type is required'),
  routePattern: z.string().min(1, 'Route pattern is required'),
  parentId: z.string().optional().transform((v) => (v === '' ? undefined : v)),
});

type StaticForm = z.infer<typeof staticSchema>;
type CollectionForm = z.infer<typeof collectionSchema>;

// ─── Helpers ──────────────────────────────────────────────────────────────────

function flattenTree(nodes: PageTreeNode[], acc: PageTreeNode[] = []): PageTreeNode[] {
  for (const n of nodes) { acc.push(n); flattenTree(n.children, acc); }
  return acc;
}

function buildBreadcrumb(id: string, flat: PageTreeNode[]): PageTreeNode[] {
  const crumbs: PageTreeNode[] = [];
  let cur = flat.find((p) => p.id === id);
  while (cur) {
 crumbs.unshift(cur);
    cur = cur.parentId ? flat.find((p) => p.id === cur!.parentId) : undefined;
  }
return crumbs;
}

const STATUS_DOT: Record<string, string> = {
  Published: 'bg-green-400',
  Draft: 'bg-slate-300',
  PendingReview: 'bg-amber-400',
  Archived: 'bg-slate-200',
};

// ─── Left Sidebar — Page Tree ─────────────────────────────────────────────────

function SiteTreeNode({
  node, selectedId, onSelect, depth = 0,
}: {
  node: PageTreeNode; selectedId: string; onSelect: (id: string) => void; depth?: number;
}) {
  const [open, setOpen] = useState(depth < 2);
  const hasChildren = node.children.length > 0;
  const isSelected = node.id === selectedId;

  return (
    <li>
      <div
        className={`group flex items-center gap-1 rounded-md py-1 pr-2 text-xs transition-colors ${isSelected ? 'bg-brand-50 text-brand-700 font-semibold' : 'text-slate-600 hover:bg-slate-100'}`}
    style={{ paddingLeft: `${depth * 14 + 6}px` }}
      >
        {/* Expand toggle */}
   <button
          onClick={() => setOpen((o) => !o)}
          className={`flex h-4 w-4 flex-shrink-0 items-center justify-center rounded text-slate-400 ${hasChildren ? 'hover:text-slate-600' : 'invisible'}`}
      >
          <svg className={`h-3 w-3 transition-transform ${open ? 'rotate-90' : ''}`} fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
          </svg>
        </button>
        {/* Page icon */}
        <svg className="h-3.5 w-3.5 flex-shrink-0 text-slate-300" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12h6m-6 4h4M7 4h10a2 2 0 012 2v14a2 2 0 01-2 2H7a2 2 0 01-2-2V6a2 2 0 012-2z" />
        </svg>
        <button onClick={() => onSelect(node.id)} className="min-w-0 flex-1 truncate text-left">
          {node.title}
        </button>
        <span className={`h-1.5 w-1.5 flex-shrink-0 rounded-full ${node.pageType === 'Static' ? 'bg-green-400' : 'bg-amber-400'}`} title={node.pageType} />
      </div>
{open && hasChildren && (
   <ul>
{node.children.map((child) => (
<SiteTreeNode key={child.id} node={child} selectedId={selectedId} onSelect={onSelect} depth={depth + 1} />
       ))}
        </ul>
      )}
    </li>
  );
}

// ─── Right Detail Panel ───────────────────────────────────────────────────────

function PageDetailPanel({
  pageId, siteId, layouts, onClose,
}: {
  pageId: string; siteId: string; layouts: LayoutListItem[]; onClose: () => void;
}) {
  const qc = useQueryClient();
  const [layoutId, setLayoutId] = useState<string>('');
  const [layoutLoaded, setLayoutLoaded] = useState(false);

  const { data: page, isLoading } = useQuery<PageDto>({
    queryKey: ['page-detail', pageId],
    queryFn: () => pagesApi.getPage(pageId),
    enabled: !!pageId,
  });

  const { data: template } = useQuery({
    queryKey: ['page-template', pageId],
    queryFn: async () => {
      try { return await pagesApi.getTemplate(pageId); }
      catch (e) { if (e instanceof ApiError && e.status === 404) return null; throw e; }
    },
    enabled: !!pageId,
  });

  // Sync layout picker once page loads
  if (page && !layoutLoaded) {
    setLayoutId(page.layoutId ?? '');
    setLayoutLoaded(true);
  }

  const setLayoutMutation = useMutation({
    mutationFn: () => pagesApi.setLayout(pageId, { layoutId: layoutId || null }),
    onSuccess: () => {
      toast.success('Layout saved.');
   void qc.invalidateQueries({ queryKey: ['pages', siteId] });
      void qc.invalidateQueries({ queryKey: ['page-detail', pageId] });
    },
    onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Failed.'),
  });

  const deleteMutation = useMutation({
    mutationFn: () => pagesApi.delete(pageId),
    onSuccess: () => {
      toast.success('Page deleted.');
      onClose();
      void qc.invalidateQueries({ queryKey: ['pages', siteId] });
    },
    onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Failed.'),
  });

  const assignedLayout = layouts.find((l) => l.id === (page?.layoutId)) ?? layouts.find((l) => l.isDefault);
  const templateZones = Array.from(new Set((template?.placements ?? []).map((p) => p.zone)));
  const templateLabel = templateZones.length > 0
    ? templateZones.join(' · ')
    : 'No template';

  if (isLoading) {
    return (
      <aside className="w-72 flex-shrink-0 space-y-4 border-l border-slate-200 bg-white p-4">
        {Array.from({ length: 6 }).map((_, i) => <div key={i} className="h-8 animate-pulse rounded bg-slate-100" />)}
      </aside>
    );
  }

  if (!page) return null;

  return (
    <aside className="flex w-72 flex-shrink-0 flex-col overflow-hidden border-l border-slate-200 bg-white">
      {/* Header */}
      <div className="flex items-start justify-between border-b border-slate-200 px-4 py-3">
        <div className="min-w-0">
          <div className="flex items-center gap-2">
      <p className="truncate text-sm font-bold text-slate-900">{page.title}</p>
    <span className={`flex-shrink-0 rounded-full px-2 py-0.5 text-[10px] font-semibold ${page.pageType === 'Static' ? 'bg-brand-100 text-brand-700' : 'bg-amber-100 text-amber-700'}`}>
 {page.pageType === 'Static' ? 'Static Page' : 'Collection Route'}
  </span>
       </div>
          <p className="font-mono text-xs text-slate-400">/{page.slug}</p>
        </div>
        <button onClick={onClose} className="ml-2 flex-shrink-0 rounded p-1 text-slate-400 hover:bg-slate-100">
          <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
    </svg>
        </button>
      </div>

      <div className="flex-1 overflow-y-auto">
        {/* ── Page Structure ── */}
   <div className="border-b border-slate-100 px-4 py-4">
          <p className="mb-3 text-[10px] font-bold uppercase tracking-wider text-slate-400">Page Structure</p>

          {/* Global Layout */}
          <div className="mb-3">
  <p className="mb-1 text-[11px] font-semibold text-slate-500">Global Layout</p>
      <div className="flex items-center justify-between rounded-lg border border-slate-200 bg-slate-50 px-3 py-2">
      <div className="flex items-center gap-2 min-w-0">
             <svg className="h-4 w-4 flex-shrink-0 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
     <rect x="3" y="3" width="18" height="18" rx="2" strokeWidth="2" />
      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 9h18" />
       </svg>
 <span className="truncate text-xs font-medium text-slate-700">{assignedLayout?.name ?? 'Default Layout'}</span>
</div>
              <button onClick={() => { /* open layout picker inline */ }} className="ml-2 flex-shrink-0 text-[11px] font-semibold text-brand-600 hover:underline">
     Change
              </button>
 </div>
            {assignedLayout && (
           <p className="mt-1 text-[10px] text-slate-400">Header · Footer · Nav inherited from this layout</p>
            )}
            {/* Inline layout selector */}
         <div className="mt-2 flex gap-1">
       <select className="form-input text-xs flex-1"
                value={layoutId}
          onChange={(e) => setLayoutId(e.target.value)}>
   <option value="">— Site default —</option>
         {layouts.map((l) => <option key={l.id} value={l.id}>{l.name}{l.isDefault ? ' (default)' : ''}</option>)}
    </select>
           <button onClick={() => setLayoutMutation.mutate()} disabled={setLayoutMutation.isPending}
     className="btn-secondary px-2 py-1 text-xs">
   {setLayoutMutation.isPending ? '…' : 'Save'}
          </button>
  </div>
       </div>

   {/* Page Template */}
      <div>
        <p className="mb-1 text-[11px] font-semibold text-slate-500">Page Template</p>
        <div className="flex items-center justify-between rounded-lg border border-slate-200 bg-slate-50 px-3 py-2">
     <div className="flex items-center gap-2 min-w-0">
     <svg className="h-4 w-4 flex-shrink-0 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
 <rect x="3" y="3" width="18" height="18" rx="2" strokeWidth="2" />
 <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 9h18M9 21V9" />
      </svg>
 <span className="truncate text-xs font-medium text-slate-700">{templateLabel}</span>
     </div>
     <Link
         to={`/designer?pageId=${page.id}`}
       className="ml-2 flex-shrink-0 text-[11px] font-semibold text-brand-600 hover:underline"
>
        {template ? 'Edit →' : 'Design →'}
       </Link>
            </div>
        {template && (
           <p className="mt-1 text-[10px] text-slate-400">
     Used by {template.placements.length} placement{template.placements.length !== 1 ? 's' : ''}
   </p>
     )}
     <Link
   to={`/designer?pageId=${page.id}`}
  className="mt-2 flex w-full items-center justify-center gap-1.5 rounded-md border border-brand-200 bg-brand-50 px-3 py-1.5 text-xs font-semibold text-brand-700 hover:bg-brand-100"
      >
   <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
     <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
      </svg>
     Edit Template in Designer
          </Link>
          </div>
        </div>

    {/* ── Page Info ── */}
        <div className="border-b border-slate-100 px-4 py-4">
      <p className="mb-3 text-[10px] font-bold uppercase tracking-wider text-slate-400">Page Info</p>
  <div className="space-y-1.5 text-xs">
            <div className="flex justify-between">
  <span className="text-slate-400">Slug</span>
              <span className="font-mono text-slate-700">/{page.slug}</span>
            </div>
            {page.pageType === 'Collection' && page.routePattern && (
      <div className="flex justify-between">
          <span className="text-slate-400">Route</span>
              <span className="font-mono text-slate-700">{page.routePattern}</span>
     </div>
   )}
         <div className="flex justify-between">
              <span className="text-slate-400">Type</span>
  <span className="text-slate-700">{page.pageType}</span>
        </div>
     </div>
     </div>

        {/* ── SEO ── */}
        <div className="border-b border-slate-100 px-4 py-4">
          <div className="mb-2 flex items-center justify-between">
            <p className="text-[10px] font-bold uppercase tracking-wider text-slate-400">SEO</p>
          </div>
          {page.seo?.metaTitle || page.seo?.metaDescription ? (
       <div className="space-y-1.5 text-xs">
              {page.seo.metaTitle && (
       <div>
 <span className="text-slate-400">Meta Title</span>
           <p className="mt-0.5 text-slate-700">{page.seo.metaTitle}</p>
         </div>
       )}
  {page.seo.metaDescription && (
         <div>
              <span className="text-slate-400">Meta Description</span>
        <p className="mt-0.5 line-clamp-2 text-slate-700">{page.seo.metaDescription}</p>
      </div>
    )}
  </div>
          ) : (
<p className="text-xs text-slate-400 italic">No SEO overrides set.</p>
          )}
</div>

        {/* ── Danger ── */}
        <div className="px-4 py-4">
<button
          onClick={() => { if (confirm(`Delete "${page.title}" and all its children?`)) deleteMutation.mutate(); }}
 disabled={deleteMutation.isPending}
   className="flex w-full items-center justify-center gap-1.5 rounded-md border border-red-200 bg-red-50 px-3 py-1.5 text-xs font-semibold text-red-600 hover:bg-red-100 disabled:opacity-50"
          >
 <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
           <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
    </svg>
 {deleteMutation.isPending ? 'Deleting…' : 'Delete Page'}
          </button>
        </div>
      </div>
</aside>
  );
}

// ─── Create Page Modal ────────────────────────────────────────────────────────

function CreatePageModal({
  siteId, parentId, flatPages, contentTypes, onClose,
}: {
  siteId: string; parentId?: string; flatPages: PageTreeNode[];
  contentTypes: ContentTypeListItem[]; onClose: () => void;
}) {
  const qc = useQueryClient();
  const [type, setType] = useState<'static' | 'collection'>('static');

  const staticForm = useForm<StaticForm>({
    resolver: zodResolver(staticSchema),
    defaultValues: { parentId: parentId ?? '' },
  });
  const collectionForm = useForm<CollectionForm>({
    resolver: zodResolver(collectionSchema),
    defaultValues: { parentId: parentId ?? '' },
  });

  const createStatic = useMutation({
    mutationFn: (d: StaticForm) => pagesApi.createStatic({ siteId, ...d }),
    onSuccess: () => { toast.success('Page created.'); void qc.invalidateQueries({ queryKey: ['pages', siteId] }); onClose(); },
    onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Failed.'),
  });
  const createCollection = useMutation({
    mutationFn: (d: CollectionForm) => pagesApi.createCollection({ siteId, ...d }),
    onSuccess: () => { toast.success('Collection page created.'); void qc.invalidateQueries({ queryKey: ['pages', siteId] }); onClose(); },
    onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Failed.'),
  });

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/30 p-4">
      <div className="w-full max-w-md rounded-xl bg-white shadow-xl">
 <div className="flex items-center justify-between border-b border-slate-200 px-5 py-4">
      <h2 className="text-base font-semibold text-slate-900">Add Page{parentId ? ' (child)' : ''}</h2>
          <button onClick={onClose} className="rounded p-1 text-slate-400 hover:bg-slate-100">
 <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
        </svg>
          </button>
        </div>

        <div className="p-5">
      {/* Type selector */}
          <div className="mb-4 flex rounded-lg bg-slate-100 p-1">
       {(['static', 'collection'] as const).map((t) => (
    <button key={t} onClick={() => setType(t)}
      className={`flex-1 rounded-md px-3 py-1.5 text-xs font-medium capitalize transition-colors ${type === t ? 'bg-white text-slate-900 shadow-sm' : 'text-slate-500 hover:text-slate-700'}`}>
             {t === 'static' ? 'Static Page' : 'Collection Page'}
    </button>
   ))}
     </div>

     {type === 'static' ? (
      <form onSubmit={staticForm.handleSubmit((v) => createStatic.mutate(v))} className="space-y-3">
  <div>
    <label className="form-label">Title</label>
    <input className="form-input mt-1" {...staticForm.register('title')} placeholder="About Us" />
     {staticForm.formState.errors.title && <p className="form-error">{staticForm.formState.errors.title.message}</p>}
            </div>
    <div>
   <label className="form-label">Slug</label>
 <input className="form-input mt-1 font-mono" {...staticForm.register('slug')} placeholder="about-us" />
       {staticForm.formState.errors.slug && <p className="form-error">{staticForm.formState.errors.slug.message}</p>}
         </div>
       <div>
       <label className="form-label">Parent</label>
 <select className="form-input mt-1" {...staticForm.register('parentId')}>
                  <option value="">None (root)</option>
   {flatPages.map((p) => <option key={p.id} value={p.id}>{'—'.repeat(p.depth)} {p.title}</option>)}
     </select>
</div>
   <button type="submit" disabled={createStatic.isPending} className="btn-primary w-full justify-center">
    {createStatic.isPending ? 'Creating…' : 'Create Static Page'}
              </button>
   </form>
          ) : (
   <form onSubmit={collectionForm.handleSubmit((v) => createCollection.mutate(v))} className="space-y-3">
              <div>
         <label className="form-label">Title</label>
  <input className="form-input mt-1" {...collectionForm.register('title')} placeholder="Blog" />
                {collectionForm.formState.errors.title && <p className="form-error">{collectionForm.formState.errors.title.message}</p>}
        </div>
       <div>
          <label className="form-label">Slug</label>
     <input className="form-input mt-1 font-mono" {...collectionForm.register('slug')} placeholder="blog" />
            {collectionForm.formState.errors.slug && <p className="form-error">{collectionForm.formState.errors.slug.message}</p>}
      </div>
           <div>
    <label className="form-label">Content Type</label>
            <select className="form-input mt-1" {...collectionForm.register('contentTypeId')}>
  <option value="">Select…</option>
             {contentTypes.map((ct) => <option key={ct.id} value={ct.id}>{ct.displayName}</option>)}
                </select>
     {collectionForm.formState.errors.contentTypeId && <p className="form-error">{collectionForm.formState.errors.contentTypeId.message}</p>}
       </div>
       <div>
     <label className="form-label">Route Pattern</label>
     <input className="form-input mt-1 font-mono" {...collectionForm.register('routePattern')} placeholder="/blog/{slug}" />
    {collectionForm.formState.errors.routePattern && <p className="form-error">{collectionForm.formState.errors.routePattern.message}</p>}
              </div>
  <div>
     <label className="form-label">Parent</label>
  <select className="form-input mt-1" {...collectionForm.register('parentId')}>
            <option value="">None (root)</option>
      {flatPages.map((p) => <option key={p.id} value={p.id}>{'—'.repeat(p.depth)} {p.title}</option>)}
    </select>
    </div>
        <button type="submit" disabled={createCollection.isPending} className="btn-primary w-full justify-center">
    {createCollection.isPending ? 'Creating…' : 'Create Collection Page'}
   </button>
            </form>
          )}
    </div>
      </div>
    </div>
  );
}

// ─── Child Card ───────────────────────────────────────────────────────────────

function ChildCard({
  page, isSelected, onClick,
}: {
  page: PageTreeNode; isSelected: boolean; onClick: () => void;
}) {
  const typeIcon = page.pageType === 'Static'
    ? 'M9 12h6m-6 4h4M7 4h10a2 2 0 012 2v14a2 2 0 01-2 2H7a2 2 0 01-2-2V6a2 2 0 012-2z'
    : 'M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10';

  return (
    <button
      onClick={onClick}
      className={`flex flex-col rounded-xl border-2 p-4 text-left transition-all hover:shadow-md ${
      isSelected
    ? 'border-brand-400 bg-brand-50 shadow-md'
      : 'border-slate-200 bg-white hover:border-brand-200'
      }`}
    >
      <div className="mb-3 flex items-start justify-between">
   <div className={`flex h-9 w-9 items-center justify-center rounded-lg ${page.pageType === 'Static' ? 'bg-brand-100' : 'bg-amber-100'}`}>
          <svg className={`h-4.5 w-4.5 ${page.pageType === 'Static' ? 'text-brand-600' : 'text-amber-600'}`} fill="none" viewBox="0 0 24 24" stroke="currentColor">
     <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d={typeIcon} />
          </svg>
   </div>
        <span className={`h-2 w-2 rounded-full ${STATUS_DOT['Published']}`} />
      </div>
      <p className="truncate text-sm font-semibold text-slate-900">{page.title}</p>
      <p className="font-mono text-[11px] text-slate-400">/{page.slug}</p>
      <div className="mt-2 flex items-center justify-between text-[11px] text-slate-400">
        <span>{page.pageType === 'Static' ? 'Static Page' : 'Collection'}</span>
        <span>{page.children.length} child{page.children.length !== 1 ? 'ren' : ''}</span>
      </div>
    </button>
  );
}

// ─── Main Page ────────────────────────────────────────────────────────────────

export default function PagesPage() {
  const { selectedSiteId, selectedSite, isLoading: siteLoading } = useSite();
  const siteId = selectedSiteId ?? '';

  const [selectedId, setSelectedId] = useState<string>('');
  const [detailOpen, setDetailOpen] = useState(false);
  const [createModal, setCreateModal] = useState<{ open: boolean; parentId?: string }>({ open: false });

  const { data: tree = [], isLoading: treeLoading } = useQuery({
    queryKey: ['pages', siteId],
    queryFn: () => pagesApi.getTree(siteId),
    enabled: !!siteId,
  });

  const { data: layouts = [] } = useQuery({
    queryKey: ['layouts', siteId],
    queryFn: () => layoutsApi.list(siteId),
    enabled: !!siteId,
  });

  const { data: contentTypesResult } = useQuery({
    queryKey: ['content-types'],
    queryFn: () => contentTypesApi.list(),
  });
  const contentTypes = contentTypesResult?.items ?? [];

  const flatPages = useMemo(() => flattenTree(tree), [tree]);
  const selectedNode = flatPages.find((p) => p.id === selectedId);
  const breadcrumb = selectedId ? buildBreadcrumb(selectedId, flatPages) : [];

  const displayedChildren = selectedNode
    ? selectedNode.children
    : tree; // root level

  const handleSelectNode = (id: string) => {
    setSelectedId(id);
    setDetailOpen(false);
  };

  const handleCardClick = (page: PageTreeNode) => {
    setSelectedId(page.id);
    setDetailOpen(true);
  };

  if (siteLoading) {
    return <div className="space-y-3">{Array.from({ length: 5 }).map((_, i) => <div key={i} className="h-10 animate-pulse rounded-lg bg-slate-100" />)}</div>;
  }

  if (!siteId) {
    return (
      <div className="flex flex-col items-center justify-center gap-3 py-24 text-center">
   <p className="text-sm text-slate-500">No site selected. Choose a site from the top bar.</p>
      </div>
    );
  }

  return (
    <>
      {/* Create modal */}
      {createModal.open && (
        <CreatePageModal
   siteId={siteId}
       parentId={createModal.parentId}
          flatPages={flatPages}
          contentTypes={contentTypes}
     onClose={() => setCreateModal({ open: false })}
 />
      )}

      {/* Full-width 3-panel layout */}
  <div className="-m-6 flex h-[calc(100vh-4rem)] overflow-hidden">

        {/* LEFT: Site tree */}
        <aside className="flex w-52 flex-shrink-0 flex-col overflow-hidden border-r border-slate-200 bg-white">
       {/* Site header */}
          <div className="flex items-center justify-between border-b border-slate-200 px-3 py-3">
         <div className="flex items-center gap-2 min-w-0">
              <div className="flex h-5 w-5 flex-shrink-0 items-center justify-center rounded bg-brand-600 text-[9px] font-black text-white">
       {selectedSite?.name?.charAt(0) ?? 'S'}
       </div>
       <span className="truncate text-xs font-bold text-slate-700">{selectedSite?.name}</span>
   </div>
    </div>

          {/* Search */}
     <div className="border-b border-slate-200 px-2 py-2">
     <div className="relative">
  <svg className="absolute left-2 top-1/2 h-3 w-3 -translate-y-1/2 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
 <circle cx="11" cy="11" r="8" strokeWidth="2" />
      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-4.35-4.35" />
 </svg>
         <input className="w-full rounded-md border border-slate-200 py-1.5 pl-7 pr-2 text-xs focus:border-brand-400 focus:outline-none" placeholder="Find page…" />
    </div>
     </div>

          {/* Tree */}
    <nav className="flex-1 overflow-y-auto py-2 px-1">
      {treeLoading
     ? Array.from({ length: 5 }).map((_, i) => <div key={i} className="mx-2 mb-1.5 h-6 animate-pulse rounded bg-slate-100" />)
          : <ul>{tree.map((node) => <SiteTreeNode key={node.id} node={node} selectedId={selectedId} onSelect={handleSelectNode} />)}</ul>
            }
          </nav>

          {/* Add root page */}
          <div className="border-t border-slate-200 p-2">
            <button onClick={() => setCreateModal({ open: true })}
      className="flex w-full items-center justify-center gap-1.5 rounded-md border border-dashed border-slate-300 py-1.5 text-xs text-slate-500 hover:border-brand-400 hover:text-brand-600">
    <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
         </svg>
   Add page
     </button>
          </div>
 </aside>

      {/* CENTER: Content area */}
      <div className="flex min-w-0 flex-1 flex-col overflow-hidden bg-slate-50">
          {/* Breadcrumb + actions */}
          <div className="flex flex-shrink-0 items-center justify-between border-b border-slate-200 bg-white px-6 py-3">
            <nav className="flex items-center gap-1.5 text-sm">
    <button onClick={() => { setSelectedId(''); setDetailOpen(false); }} className="flex items-center gap-1 text-slate-400 hover:text-slate-600">
     <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />
          </svg>
          Home
           </button>
       {breadcrumb.map((crumb) => (
        <span key={crumb.id} className="flex items-center gap-1.5">
            <svg className="h-3.5 w-3.5 text-slate-300" fill="none" viewBox="0 0 24 24" stroke="currentColor">
         <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
             </svg>
      <button onClick={() => handleSelectNode(crumb.id)} className="font-medium text-slate-700 hover:text-brand-600">
    {crumb.title}
           </button>
        </span>
   ))}
            </nav>

        <div className="flex items-center gap-2">
              {selectedNode && (
      <button onClick={() => setCreateModal({ open: true, parentId: selectedId })}
      className="btn-secondary text-xs py-1.5 px-3">
         <svg className="mr-1 h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
     <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
    </svg>
          Add child page
       </button>
              )}
   <button onClick={() => setCreateModal({ open: true })} className="btn-primary text-xs py-1.5 px-3">
      <svg className="mr-1 h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
          </svg>
    New page
              </button>
      </div>
        </div>

        {/* Current page banner (when a page is selected) */}
        {selectedNode && (
            <div className="flex-shrink-0 border-b border-slate-200 bg-white px-6 py-3">
          <div className="flex items-center justify-between rounded-lg border border-slate-200 bg-slate-50 px-4 py-2.5">
          <div className="flex items-center gap-3 min-w-0">
        <div className="flex h-8 w-8 flex-shrink-0 items-center justify-center rounded-lg bg-brand-100">
<svg className="h-4 w-4 text-brand-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12h6m-6 4h4M7 4h10a2 2 0 012 2v14a2 2 0 01-2 2H7a2 2 0 01-2-2V6a2 2 0 012-2z" />
         </svg>
  </div>
  <div className="min-w-0">
             <div className="flex items-center gap-2">
   <span className="text-[11px] font-bold uppercase tracking-wider text-slate-400">Current Page</span>
         </div>
        <p className="truncate text-sm font-bold text-slate-900">{selectedNode.title}</p>
         <p className="font-mono text-xs text-slate-400">/{selectedNode.slug}</p>
     </div>
   </div>
      <div className="flex flex-shrink-0 items-center gap-2">
       <span className="rounded-full bg-green-100 px-2 py-0.5 text-[10px] font-semibold text-green-700">Published</span>
   <button
          onClick={() => { setDetailOpen(true); }}
              className="flex items-center gap-1 text-xs font-medium text-brand-600 hover:underline"
              >
    Edit
            <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
      </svg>
        </button>
    </div>
  </div>
       </div>
      )}

       {/* Child cards grid */}
          <div className="flex-1 overflow-y-auto p-6">
            {treeLoading ? (
        <div className="grid grid-cols-4 gap-4">
        {Array.from({ length: 8 }).map((_, i) => <div key={i} className="h-32 animate-pulse rounded-xl bg-slate-200" />)}
</div>
            ) : displayedChildren.length === 0 ? (
              <div className="flex flex-col items-center justify-center gap-3 py-24 text-center">
      <svg className="h-10 w-10 text-slate-200" fill="none" viewBox="0 0 24 24" stroke="currentColor">
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M9 13h6m-3-3v6m5 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
          </svg>
        <p className="text-sm font-medium text-slate-400">No pages here yet</p>
  <button onClick={() => setCreateModal({ open: true, parentId: selectedId || undefined })}
   className="btn-primary text-sm">
   Add your first page
            </button>
    </div>
            ) : (
    <>
         {selectedNode && (
       <p className="mb-3 text-xs font-medium text-slate-400">
   ↓ Child pages ({displayedChildren.length})
   </p>
 )}
     <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 xl:grid-cols-4">
          {displayedChildren.map((child) => (
         <ChildCard
        key={child.id}
            page={child}
     isSelected={selectedId === child.id && detailOpen}
                    onClick={() => handleCardClick(child)}
          />
       ))}

       {/* Add child card */}
           <button
  onClick={() => setCreateModal({ open: true, parentId: selectedId || undefined })}
     className="flex flex-col items-center justify_center gap-2 rounded-xl border-2 border-dashed border-slate-200 p-4 text-slate-400 transition-colors hover:border-brand-300 hover:text-brand-600 min-h-[120px]"
            >
  <svg className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
         </svg>
      <span className="text-xs font-medium">
            Add child page{selectedNode ? ` under ${selectedNode.title}` : ''}
        </span>
      </button>
         </div>
   </>
            )}
          </div>
  </div>

   {/* RIGHT: Detail panel */}
        {detailOpen && selectedId && (
     <PageDetailPanel
        pageId={selectedId}
      siteId={siteId}
       layouts={layouts}
    onClose={() => setDetailOpen(false)}
          />
        )}
      </div>
    </>
  );
}
