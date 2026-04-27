import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { entriesApi } from '@/api/entries';
import { pagesApi } from '@/api/pages';
import { ApiError } from '@/api/client';
import type { PageDto } from '@/types';

export function LinkEntrySection({
  pageId, siteId, page: _page,
}: {
  pageId: string;
  siteId: string;
  page: PageDto;
}) {
  const qc = useQueryClient();
  const [search, setSearch] = useState('');
  const [selectedEntryId, setSelectedEntryId] = useState('');

  const { data: results, isFetching } = useQuery({
    queryKey: ['entries-search', siteId, search],
queryFn: () => entriesApi.list({ siteId, search: search || undefined, pageSize: 20 }),
    enabled: true,
    staleTime: 10_000,
  });

  const linkMutation = useMutation({
    mutationFn: () => pagesApi.setLinkedEntry(pageId, { entryId: selectedEntryId || null }),
    onSuccess: () => {
  toast.success('Entry linked.');
   void qc.invalidateQueries({ queryKey: ['page-detail', pageId] });
    void qc.invalidateQueries({ queryKey: ['pages', siteId] });
    },
    onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Failed.'),
  });

  return (
 <div className="w-full space-y-3">
      <input className="form-input text-xs" placeholder="Search entries…"
  value={search} onChange={(e) => setSearch(e.target.value)} />
      <div className="max-h-48 overflow-y-auto rounded-lg border border-slate-200 divide-y divide-slate-100">
        {isFetching && <p className="p-3 text-center text-xs text-slate-400">Loading…</p>}
    {!isFetching && (results?.items ?? []).length === 0 && <p className="p-3 text-center text-xs text-slate-400">No entries found.</p>}
        {!isFetching && (results?.items ?? []).map((e) => (
          <button key={e.id} type="button" onClick={() => setSelectedEntryId(selectedEntryId === e.id ? '' : e.id)}
   className={`flex w-full items-center justify-between px-3 py-2 text-left text-xs hover:bg-slate-50 ${selectedEntryId === e.id ? 'bg-brand-50' : ''}`}>
            <div className="min-w-0">
            <p className="truncate font-medium text-slate-800">{e.title ?? e.slug}</p>
     <p className="font-mono text-[10px] text-slate-400">{e.contentTypeName} · {e.status}</p>
            </div>
    {selectedEntryId === e.id && (
   <span className="ml-2 rounded-full bg-brand-100 px-1.5 py-0.5 text-[9px] font-bold text-brand-700">✓</span>
     )}
          </button>
     ))}
      </div>
      <button onClick={() => linkMutation.mutate()} disabled={!selectedEntryId || linkMutation.isPending}
        className="btn-primary w-full justify-center text-xs disabled:opacity-50">
        {linkMutation.isPending ? 'Linking…' : 'Link Selected Entry'}
      </button>
    </div>
  );
}
