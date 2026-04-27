import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { pagesApi } from '@/api/pages';
import { entriesApi } from '@/api/entries';
import { contentTypesApi } from '@/api/contentTypes';
import { ApiError } from '@/api/client';
import { EntryFieldEditor } from './EntryFieldEditor';
import { LinkEntrySection } from './LinkEntrySection';
import type { PageDto, LayoutListItem, Entry } from '@/types';

type RightTab = 'content' | 'page';

export function PageDetailPanel({
pageId, siteId, layouts, onClose,
}: {
  pageId: string;
  siteId: string;
  layouts: LayoutListItem[];
  onClose: () => void;
}) {
  const qc = useQueryClient();
  const [tab, setTab] = useState<RightTab>('content');
  const [layoutId, setLayoutId] = useState('');
  const [layoutLoaded, setLayoutLoaded] = useState(false);

  const { data: page, isLoading: pageLoading } = useQuery<PageDto>({
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

  const { data: linkedEntry } = useQuery<Entry>({
  queryKey: ['entry', page?.linkedEntryId],
    queryFn: () => entriesApi.getById(page!.linkedEntryId!),
    enabled: !!page?.linkedEntryId,
  });

  const contentTypeId = linkedEntry?.contentTypeId ?? page?.collectionContentTypeId;
  const { data: contentType } = useQuery({
    queryKey: ['content-type', contentTypeId],
    queryFn: () => contentTypesApi.getById(contentTypeId!),
    enabled: !!contentTypeId,
  });

  useEffect(() => {
    if (page && !layoutLoaded) {
      setLayoutId(page.layoutId ?? '');
      setLayoutLoaded(true);
    }
  }, [page, layoutLoaded]);

  useEffect(() => { setLayoutLoaded(false); }, [pageId]);

  const setLayoutMutation = useMutation({
    mutationFn: () => pagesApi.setLayout(pageId, { layoutId: layoutId || null }),
    onSuccess: () => {
      toast.success('Layout saved.');
      void qc.invalidateQueries({ queryKey: ['pages', siteId] });
   void qc.invalidateQueries({ queryKey: ['page-detail', pageId] });
    },
    onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Failed.'),
  });

  const saveEntryMutation = useMutation({
    mutationFn: (fields: Record<string, unknown>) =>
      entriesApi.update(linkedEntry!.id, { fields }),
    onSuccess: () => {
      toast.success('Entry saved.');
      void qc.invalidateQueries({ queryKey: ['entry', page?.linkedEntryId] });
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

  const assignedLayout = layouts.find((l) => l.id === page?.layoutId) ?? layouts.find((l) => l.isDefault);
  const templateZones = Array.from(new Set((template?.placements ?? []).map((p) => p.zone)));
  const hasLinkedEntry = !!page?.linkedEntryId;
  const fields = contentType?.fields ?? [];

  const tabItems: { key: RightTab; label: string }[] = [
 { key: 'content', label: 'Content' },
    { key: 'page', label: 'Page' },
  ];

  if (pageLoading) {
    return (
      <aside className="flex w-96 flex-shrink-0 flex-col border-l border-slate-200 bg-white">
        <div className="space-y-3 p-4">
    {Array.from({ length: 6 }).map((_, i) => <div key={i} className="h-8 animate-pulse rounded bg-slate-100" />)}
        </div>
 </aside>
    );
  }

  if (!page) return null;

  return (
    <aside className="flex w-96 flex-shrink-0 flex-col overflow-hidden border-l border-slate-200 bg-white">
      {/* Header */}
      <div className="flex flex-shrink-0 items-start justify-between border-b border-slate-200 px-4 py-3">
        <div className="min-w-0">
          <div className="flex items-center gap-2">
      <p className="truncate text-sm font-bold text-slate-900">{page.title}</p>
 <span className={`flex-shrink-0 rounded-full px-2 py-0.5 text-[10px] font-semibold ${
        page.pageType === 'Static' ? 'bg-brand-100 text-brand-700' : 'bg-amber-100 text-amber-700'
            }`}>
         {page.pageType === 'Static' ? 'Static' : 'Collection'}
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

      {/* Tabs */}
      <div className="flex flex-shrink-0 border-b border-slate-200">
     {tabItems.map((t) => (
       <button key={t.key} onClick={() => setTab(t.key)}
    className={`flex-1 py-2.5 text-xs font-semibold transition-colors ${
  tab === t.key
    ? 'border-b-2 border-brand-600 text-brand-700'
       : 'text-slate-500 hover:text-slate-700'
            }`}>
            {t.label}
         {t.key === 'content' && hasLinkedEntry && (
      <span className="ml-1.5 rounded-full bg-green-100 px-1.5 py-0.5 text-[9px] font-bold text-green-700">Linked</span>
  )}
      </button>
        ))}
      </div>

      {/* Tab: Content */}
      {tab === 'content' && (
        <div className="flex min-h-0 flex-1 flex-col overflow-hidden">
          {!hasLinkedEntry ? (
     <div className="flex flex-1 flex-col items-center justify-center gap-3 p-6 text-center">
            <svg className="h-10 w-10 text-slate-200" fill="none" viewBox="0 0 24 24" stroke="currentColor">
      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M9 12h6m-6 4h4M7 4h10a2 2 0 012 2v14a2 2 0 01-2 2H7a2 2 0 01-2-2V6a2 2 0 012-2z" />
            </svg>
      <div>
          <p className="text-sm font-semibold text-slate-700">No entry linked</p>
         <p className="mt-1 text-xs text-slate-400">
     {page.pageType === 'Static'
 ? 'Link a content entry to edit its fields here.'
           : 'Collection pages render entries dynamically. Select an entry to preview.'}
    </p>
              </div>
        <LinkEntrySection pageId={pageId} siteId={siteId} page={page} />
            </div>
     ) : !linkedEntry ? (
    <div className="flex flex-1 items-center justify-center p-6">
              <div className="space-y-2 w-full">
    {Array.from({ length: 4 }).map((_, i) => <div key={i} className="h-8 animate-pulse rounded bg-slate-100" />)}
              </div>
            </div>
   ) : (
      <EntryFieldEditor
    entry={linkedEntry}
fields={fields}
       onSave={(updated) => saveEntryMutation.mutate(updated)}
      saving={saveEntryMutation.isPending}
          />
       )}
        </div>
      )}

   {/* Tab: Page */}
      {tab === 'page' && (
        <div className="flex-1 overflow-y-auto">

          {/* Global Layout */}
  <div className="border-b border-slate-100 px-4 py-4">
            <p className="mb-3 text-[10px] font-bold uppercase tracking-wider text-slate-400">Global Layout</p>
          <div className="mb-2 flex items-center justify-between rounded-lg border border-slate-200 bg-slate-50 px-3 py-2">
              <div className="flex items-center gap-2 min-w-0">
    <svg className="h-4 w-4 flex-shrink-0 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
<rect x="3" y="3" width="18" height="18" rx="2" strokeWidth="2" />
  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 9h18" />
   </svg>
    <span className="truncate text-xs font-medium text-slate-700">{assignedLayout?.name ?? 'Default Layout'}</span>
    </div>
            </div>
    {assignedLayout && (
  <p className="mb-2 text-[10px] text-slate-400">Header · Footer · Nav inherited from this layout</p>
            )}
      <div className="flex gap-2">
              <select className="form-input flex-1 text-xs" value={layoutId} onChange={(e) => setLayoutId(e.target.value)}>
        <option value="">— Site default —</option>
            {layouts.map((l) => <option key={l.id} value={l.id}>{l.name}{l.isDefault ? ' (default)' : ''}</option>)}
      </select>
              <button onClick={() => setLayoutMutation.mutate()} disabled={setLayoutMutation.isPending}
              className="btn-secondary px-3 py-1 text-xs">
  {setLayoutMutation.isPending ? '…' : 'Save'}
    </button>
        </div>
      </div>

          {/* Page Template */}
          <div className="border-b border-slate-100 px-4 py-4">
            <p className="mb-3 text-[10px] font-bold uppercase tracking-wider text-slate-400">Page Template</p>
        <div className="mb-2 rounded-lg border border-slate-200 bg-slate-50 px-3 py-2">
     <div className="flex items-center justify-between">
         <div className="flex items-center gap-2 min-w-0">
  <svg className="h-4 w-4 flex-shrink-0 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <rect x="3" y="3" width="18" height="18" rx="2" strokeWidth="2" />
                 <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 9h18M9 21V9" />
  </svg>
    <span className="truncate text-xs font-medium text-slate-700">
                {templateZones.length > 0 ? templateZones.join(' · ') : 'No template set'}
        </span>
      </div>
    <Link to={`/designer?pageId=${page.id}`} className="ml-2 flex-shrink-0 text-[11px] font-semibold text-brand-600 hover:underline">
       {template ? 'Edit →' : 'Design →'}
    </Link>
 </div>
    {template && (
    <p className="mt-1 text-[10px] text-slate-400">{template.placements.length} placement{template.placements.length !== 1 ? 's' : ''}</p>
      )}
 </div>
      <Link to={`/designer?pageId=${page.id}`}
              className="flex w-full items-center justify-center gap-1.5 rounded-md border border-brand-200 bg-brand-50 px-3 py-2 text-xs font-semibold text-brand-700 hover:bg-brand-100">
          <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
           <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
         </svg>
   Edit Template in Designer
       </Link>
          </div>

 {/* Page Info */}
     <div className="border-b border-slate-100 px-4 py-4">
      <p className="mb-3 text-[10px] font-bold uppercase tracking-wider text-slate-400">Page Info</p>
            <div className="space-y-1.5 text-xs">
   <div className="flex justify-between"><span className="text-slate-400">Slug</span><span className="font-mono text-slate-700">/{page.slug}</span></div>
              <div className="flex justify-between"><span className="text-slate-400">Type</span><span className="text-slate-700">{page.pageType}</span></div>
           {page.routePattern && (
      <div className="flex justify-between"><span className="text-slate-400">Route</span><span className="font-mono text-slate-700">{page.routePattern}</span></div>
      )}
          {page.linkedEntryId && (
          <div className="flex items-center justify-between">
        <span className="text-slate-400">Linked Entry</span>
           <Link to={`/entries/${page.linkedEntryId}`} className="font-mono text-[10px] text-brand-600 hover:underline">
       {page.linkedEntryId.slice(0, 8)}…
          </Link>
             </div>
   )}
         </div>
 </div>

          {/* SEO */}
          <div className="border-b border-slate-100 px-4 py-4">
            <p className="mb-2 text-[10px] font-bold uppercase tracking-wider text-slate-400">SEO</p>
        {page.seo?.metaTitle || page.seo?.metaDescription ? (
     <div className="space-y-1.5 text-xs">
       {page.seo.metaTitle && <div><span className="text-slate-400">Title</span><p className="mt-0.5 text-slate-700">{page.seo.metaTitle}</p></div>}
           {page.seo.metaDescription && <div><span className="text-slate-400">Description</span><p className="mt-0.5 line-clamp-2 text-slate-700">{page.seo.metaDescription}</p></div>}
       </div>
            ) : (
              <p className="text-xs italic text-slate-400">No SEO overrides set.</p>
    )}
        </div>

   {/* Delete */}
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
      )}
    </aside>
  );
}
