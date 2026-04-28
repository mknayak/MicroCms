import { useState, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { pagesApi } from '@/api/pages';
import { entriesApi } from '@/api/entries';
import { contentTypesApi } from '@/api/contentTypes';
import { siteTemplatesApi } from '@/api/siteTemplates';
import { ApiError } from '@/api/client';
import { EntryFieldEditor } from './EntryFieldEditor';
import { LinkEntrySection } from './LinkEntrySection';
import type { PageDto, Entry } from '@/types';

type RightTab = 'content' | 'page';

// ─── SeoEditor ────────────────────────────────────────────────────────────────

function SeoEditor({ page, onSaved }: { page: PageDto; onSaved: () => void }) {
  const [metaTitle, setMetaTitle] = useState(page.seo?.metaTitle ?? '');
  const [metaDesc, setMetaDesc]   = useState(page.seo?.metaDescription ?? '');
  const [canonical, setCanonical] = useState(page.seo?.canonicalUrl ?? '');
  const [ogImage, setOgImage]  = useState(page.seo?.ogImage ?? '');

  useEffect(() => {
    setMetaTitle(page.seo?.metaTitle ?? '');
    setMetaDesc(page.seo?.metaDescription ?? '');
    setCanonical(page.seo?.canonicalUrl ?? '');
    setOgImage(page.seo?.ogImage ?? '');
  }, [page.id]);

  const isDirty =
    metaTitle !== (page.seo?.metaTitle ?? '') ||
    metaDesc  !== (page.seo?.metaDescription ?? '') ||
    canonical !== (page.seo?.canonicalUrl ?? '') ||
ogImage   !== (page.seo?.ogImage ?? '');

  const saveMutation = useMutation({
    mutationFn: () => pagesApi.setSeo(page.id, {
      metaTitle: metaTitle || null, metaDescription: metaDesc || null,
      canonicalUrl: canonical || null, ogImage: ogImage || null,
    }),
    onSuccess: () => { toast.success('SEO saved.'); onSaved(); },
    onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Failed.'),
  });

  return (
    <div className="space-y-3">
      <div>
        <label className="mb-1 block text-[11px] font-semibold text-slate-600">Meta Title</label>
        <input className="form-input text-xs" value={metaTitle}
          onChange={(e) => setMetaTitle(e.target.value)} placeholder="Page title for search engines" maxLength={120} />
        <p className="mt-0.5 text-[10px] text-slate-400">{metaTitle.length}/120</p>
    </div>
      <div>
        <label className="mb-1 block text-[11px] font-semibold text-slate-600">Meta Description</label>
        <textarea className="form-input resize-none text-xs" rows={3} value={metaDesc}
   onChange={(e) => setMetaDesc(e.target.value)} placeholder="Short description for search results" maxLength={300} />
        <p className="mt-0.5 text-[10px] text-slate-400">{metaDesc.length}/300</p>
      </div>
      <div>
        <label className="mb-1 block text-[11px] font-semibold text-slate-600">Canonical URL</label>
        <input className="form-input text-xs" type="url" value={canonical}
       onChange={(e) => setCanonical(e.target.value)} placeholder="https://example.com/canonical" />
      </div>
      <div>
    <label className="mb-1 block text-[11px] font-semibold text-slate-600">OG Image URL</label>
        <input className="form-input text-xs" type="url" value={ogImage}
        onChange={(e) => setOgImage(e.target.value)} placeholder="https://example.com/og.jpg" />
      </div>
      {isDirty && (
        <button onClick={() => saveMutation.mutate()} disabled={saveMutation.isPending}
          className="btn-primary w-full justify-center text-xs disabled:opacity-50">
   {saveMutation.isPending ? 'Saving…' : 'Save SEO'}
        </button>
      )}
    </div>
);
}

// ─── Main panel ───────────────────────────────────────────────────────────────

export function PageDetailPanel({
  pageId, siteId, onClose,
}: {
  pageId: string;
  siteId: string;
  onClose: () => void;
}) {
  const qc = useQueryClient();
  const navigate = useNavigate();
  const [tab, setTab] = useState<RightTab>('content');

  const [siteTemplateId, setSiteTemplateId]       = useState('');
  const [templateLoaded, setTemplateLoaded]       = useState(false);

  const { data: page, isLoading: pageLoading } = useQuery<PageDto>({
    queryKey: ['page-detail', pageId],
    queryFn: () => pagesApi.getPage(pageId),
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

  const { data: siteTemplates = [] } = useQuery({
    queryKey: ['site-templates', siteId],
    queryFn: () => siteTemplatesApi.list(siteId),
    enabled: !!siteId,
  });

  useEffect(() => {
    if (page && !templateLoaded) { setSiteTemplateId(page.siteTemplateId ?? ''); setTemplateLoaded(true); }
  }, [page, templateLoaded]);

  useEffect(() => { setTemplateLoaded(false); }, [pageId]);

  const setSiteTemplateMutation = useMutation({
    mutationFn: () => pagesApi.setSiteTemplate(pageId, { siteTemplateId: siteTemplateId || null }),
    onSuccess: () => {
      toast.success('Template linked.');
      void qc.invalidateQueries({ queryKey: ['page-detail', pageId] });
    },
    onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Failed.'),
  });

  const saveEntryMutation = useMutation({
    mutationFn: (fields: Record<string, unknown>) => entriesApi.update(linkedEntry!.id, { fields }),
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

  const assignedTemplate = siteTemplates.find((t) => t.id === page?.siteTemplateId);
  const hasLinkedEntry   = !!page?.linkedEntryId;
  const fields           = contentType?.fields ?? [];

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
      }`}>{page.pageType}</span>
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
   {(['content', 'page'] as RightTab[]).map((t) => (
       <button key={t} onClick={() => setTab(t)}
      className={`flex-1 py-2.5 text-xs font-semibold capitalize transition-colors ${
       tab === t ? 'border-b-2 border-brand-600 text-brand-700' : 'text-slate-500 hover:text-slate-700'
  }`}>
       {t}
   {t === 'content' && hasLinkedEntry && (
     <span className="ml-1.5 rounded-full bg-green-100 px-1.5 py-0.5 text-[9px] font-bold text-green-700">Linked</span>
            )}
          </button>
        ))}
   </div>

      {/* ── TAB: CONTENT ──────────────────────────────────────────────── */}
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
         : 'Collection pages render entries dynamically.'}
                </p>
              </div>
            <LinkEntrySection pageId={pageId} siteId={siteId} page={page} />
     </div>
          ) : !linkedEntry ? (
        <div className="flex-1 space-y-2 p-4">
        {Array.from({ length: 4 }).map((_, i) => <div key={i} className="h-8 animate-pulse rounded bg-slate-100" />)}
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

      {/* ── TAB: PAGE ─────────────────────────────────────────────────── */}
      {tab === 'page' && (
        <div className="flex-1 divide-y divide-slate-100 overflow-y-auto">

          {/* PAGE TEMPLATE */}
          <section className="px-4 py-4">
          <p className="mb-1 text-[10px] font-bold uppercase tracking-wider text-slate-400">Page Template</p>
            <p className="mb-2 text-[11px] text-slate-400">
       Inherit shared placements (nav, header, footer) from a reusable template.
       The template also defines the layout zones available on this page.
       </p>
            {/* Dropdown + Link button */}
          <div className="flex gap-2">
    <select className="form-input flex-1 text-xs" value={siteTemplateId}
        onChange={(e) => setSiteTemplateId(e.target.value)}>
     <option value="">— No template —</option>
     {siteTemplates.map((t) => (
  <option key={t.id} value={t.id}>{t.name} ({t.layoutName})</option>
     ))}
 </select>
<button onClick={() => setSiteTemplateMutation.mutate()} disabled={setSiteTemplateMutation.isPending}
  className="btn-secondary px-3 py-1.5 text-xs">
   {setSiteTemplateMutation.isPending ? '…' : 'Link'}
</button>
    </div>
      {/* Template badge + quick links */}
    {assignedTemplate && (
  <div className="mt-2 flex items-center justify-between rounded-md border border-brand-100 bg-brand-50 px-3 py-2">
<span className="text-[11px] font-semibold text-brand-800">{assignedTemplate.name}</span>
   <Link to={`/page-templates/${assignedTemplate.id}/designer`}
    className="text-[11px] font-semibold text-brand-600 hover:underline">
 View template →
    </Link>
   </div>
  )}
 {/* Design page button */}
   <button onClick={() => navigate(`/pages/${page.id}/designer`)}
              className="mt-2 flex w-full items-center justify-center gap-1.5 rounded-md border border-brand-200 bg-brand-50 px-3 py-2 text-xs font-semibold text-brand-700 hover:bg-brand-100">
  <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
       <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
   </svg>
          Design this page
         </button>
  </section>

   {/* PAGE INFO */}
          <section className="px-4 py-4">
  <p className="mb-2 text-[10px] font-bold uppercase tracking-wider text-slate-400">Page Info</p>
         <div className="space-y-1.5 text-xs">
              <div className="flex justify-between">
         <span className="text-slate-400">Slug</span>
                <span className="font-mono text-slate-700">/{page.slug}</span>
          </div>
    <div className="flex justify-between">
   <span className="text-slate-400">Type</span>
                <span className="text-slate-700">{page.pageType}</span>
   </div>
            {page.routePattern && (
              <div className="flex justify-between">
       <span className="text-slate-400">Route</span>
       <span className="font-mono text-slate-700">{page.routePattern}</span>
                </div>
              )}
        {page.linkedEntryId && (
                <div className="flex items-center justify-between">
         <span className="text-slate-400">Linked Entry</span>
                  <Link to={`/entries/${page.linkedEntryId}`}
        className="font-mono text-[10px] text-brand-600 hover:underline">
   {page.linkedEntryId.slice(0, 8)}…
  </Link>
                </div>
     )}
      </div>
          </section>

          {/* SEO */}
   <section className="px-4 py-4">
            <p className="mb-3 text-[10px] font-bold uppercase tracking-wider text-slate-400">SEO</p>
       <SeoEditor page={page}
              onSaved={() => void qc.invalidateQueries({ queryKey: ['page-detail', pageId] })} />
          </section>

          {/* CUSTOM CODE (read-only preview; edit in full entry editor) */}
       {hasLinkedEntry && linkedEntry && (
    <section className="px-4 py-4">
           <p className="mb-2 text-[10px] font-bold uppercase tracking-wider text-slate-400">Custom Code</p>
        <p className="mb-3 text-[11px] text-slate-400">
     Stored as entry fields.{' '}
       <Link to={`/entries/${linkedEntry.id}`} className="text-brand-600 hover:underline">
          Edit in full editor →
                </Link>
        </p>
         {(['customCss', 'customJs'] as const).map((field) => (
    <div key={field} className="mb-3">
       <label className="mb-1 block text-[11px] font-semibold text-slate-600">
        {field === 'customCss' ? 'Custom CSS' : 'Custom JS'}
   </label>
          <textarea
     className="form-input w-full resize-y font-mono text-[11px]"
     rows={3}
           readOnly
           value={(linkedEntry.fields[field] as string) ?? ''}
            placeholder={`No ${field === 'customCss' ? 'CSS' : 'JS'} set`}
      />
      </div>
))}
         </section>
     )}

   {/* DELETE */}
          <section className="px-4 py-4">
      <button
        onClick={() => { if (confirm(`Delete "${page.title}" and all its children?`)) deleteMutation.mutate(); }}
      disabled={deleteMutation.isPending}
           className="flex w-full items-center justify-center gap-1.5 rounded-md border border-red-200 bg-red-50 px-3 py-1.5 text-xs font-semibold text-red-600 hover:bg-red-100 disabled:opacity-50">
        <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
      </svg>
         {deleteMutation.isPending ? 'Deleting…' : 'Delete Page'}
            </button>
  </section>

        </div>
      )}
    </aside>
  );
}
