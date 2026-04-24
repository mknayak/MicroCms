import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import toast from 'react-hot-toast';
import { pagesApi } from '@/api/pages';
import { contentTypesApi } from '@/api/contentTypes';
import { useSite } from '@/contexts/SiteContext';
import type { PageTreeNode, ContentType } from '@/types';
import { ApiError } from '@/api/client';

// ─── Schemas ──────────────────────────────────────────────────────────────────

const slugPattern = /^[a-z0-9]+(?:-[a-z0-9]+)*$/;

const staticSchema = z.object({
  title: z.string().min(1, 'Title is required'),
  slug: z.string().min(1, 'Slug is required').regex(slugPattern, 'Lowercase letters, numbers and hyphens only'),
  parentId: z.string().optional(),
});

const collectionSchema = z.object({
  title: z.string().min(1, 'Title is required'),
  slug: z.string().min(1, 'Slug is required').regex(slugPattern, 'Lowercase letters, numbers and hyphens only'),
  contentTypeId: z.string().min(1, 'Content type is required'),
  routePattern: z.string().min(1, 'Route pattern is required'),
  parentId: z.string().optional(),
});

type StaticForm = z.infer<typeof staticSchema>;
type CollectionForm = z.infer<typeof collectionSchema>;

// ─── Tree Node ────────────────────────────────────────────────────────────────

const PAGE_TYPE_BADGE: Record<string, string> = {
  Static: 'badge-brand',
  Collection: 'badge-amber',
};

function TreeNode({
  node,
  siteId,
  allNodes,
  depth = 0,
}: {
  node: PageTreeNode;
  siteId: string;
  allNodes: PageTreeNode[];
  depth?: number;
}) {
  const qc = useQueryClient();

  const deleteMutation = useMutation({
    mutationFn: () => pagesApi.delete(node.id),
    onSuccess: () => {
      toast.success('Page deleted.');
      void qc.invalidateQueries({ queryKey: ['pages', siteId] });
    },
    onError: (err) =>
      toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Delete failed.'),
  });

  return (
    <li>
      <div
        className="flex items-center justify-between rounded-lg py-2 pr-3 hover:bg-slate-50"
     style={{ paddingLeft: `${depth * 20 + 12}px` }}
      >
    <div className="flex items-center gap-2">
    {depth > 0 && <span className="text-slate-300">└</span>}
          <span className="text-sm font-medium text-slate-800">{node.title}</span>
          <span className="font-mono text-xs text-slate-400">/{node.slug}</span>
 <span className={PAGE_TYPE_BADGE[node.pageType] ?? 'badge-slate'}>{node.pageType}</span>
    </div>
        <button
          onClick={() => {
            if (confirm(`Delete page "${node.title}" and all its children?`)) deleteMutation.mutate();
       }}
          className="text-xs text-red-400 hover:text-red-600"
        >
          Delete
        </button>
      </div>
      {node.children.length > 0 && (
 <ul>
          {node.children.map((child) => (
            <TreeNode key={child.id} node={child} siteId={siteId} allNodes={allNodes} depth={depth + 1} />
    ))}
        </ul>
      )}
    </li>
  );
}

// ─── Flatten tree for parent selects ──────────────────────────────────────────

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

  const {
    data: tree,
    isLoading: treeLoading,
    isFetching,
  } = useQuery({
    queryKey: ['pages', siteId],
    queryFn: () => pagesApi.getTree(siteId),
    enabled: !!siteId,
  });

  const { data: contentTypes } = useQuery({
    queryKey: ['content-types'],
    queryFn: () => contentTypesApi.list(),
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
      Manage URL structure and navigation hierarchy.
        </p>
      </div>

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
    <TreeNode key={node.id} node={node} siteId={siteId} allNodes={flatPages} />
        ))}
 </ul>
          )}
        </div>

        {/* Create forms */}
 <div className="card space-y-4">
    {/* Tab toggle */}
          <div className="flex rounded-lg bg-slate-100 p-1">
         {(['static', 'collection'] as const).map((t) => (
              <button
    key={t}
 onClick={() => setCreateType(t)}
     className={`flex-1 rounded-md px-3 py-1.5 text-xs font-medium capitalize transition-colors ${
    createType === t
       ? 'bg-white text-slate-900 shadow-sm'
          : 'text-slate-500 hover:text-slate-700'
           }`}
 >
  {t}
    </button>
        ))}
          </div>

          {createType === 'static' ? (
    <form
       onSubmit={staticForm.handleSubmit((v) => createStaticMutation.mutate(v))}
    className="space-y-3"
            >
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
              <button
        type="submit"
   disabled={staticForm.formState.isSubmitting}
                className="btn-primary w-full justify-center"
      >
     {staticForm.formState.isSubmitting ? 'Creating…' : 'Create Page'}
            </button>
            </form>
          ) : (
            <form
  onSubmit={collectionForm.handleSubmit((v) => createCollectionMutation.mutate(v))}
  className="space-y-3"
    >
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
      {(contentTypes?.items ?? []).map((ct: ContentType) => (
               <option key={ct.id} value={ct.id}>{ct.name}</option>
     ))}
    </select>
      {collectionForm.formState.errors.contentTypeId && (
             <p className="form-error">{collectionForm.formState.errors.contentTypeId.message}</p>
      )}
    </div>
         <div>
     <label className="form-label">Route Pattern</label>
    <input
      className="form-input mt-1 font-mono"
                  {...collectionForm.register('routePattern')}
 placeholder="/blog/{slug}"
  />
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
  <button
          type="submit"
 disabled={collectionForm.formState.isSubmitting}
      className="btn-primary w-full justify-center"
     >
                {collectionForm.formState.isSubmitting ? 'Creating…' : 'Create Collection Page'}
        </button>
    </form>
    )}
        </div>
      </div>
    </div>
  );
}
