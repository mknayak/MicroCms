import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { itemsApi } from '@/api/items';
import type { ItemPickerResult } from '@/types';

// ─── Status badge ─────────────────────────────────────────────────────────────

const STATUS_BADGE: Record<string, string> = {
  Published: 'bg-green-100 text-green-700',
  Draft: 'bg-amber-100 text-amber-700',
  Archived: 'bg-slate-100 text-slate-500',
};

// ─── Component ────────────────────────────────────────────────────────────────

export interface ItemPickerModalProps {
  contentTypeId: string;
  componentName: string;
  onSelect: (item: ItemPickerResult) => void;
  onClose: () => void;
}

export default function ItemPickerModal({
  contentTypeId,
  componentName,
  onSelect,
  onClose,
}: ItemPickerModalProps) {
  const [search, setSearch] = useState('');
  const [status, setStatus] = useState<'All' | 'Draft' | 'Published' | 'Archived'>('All');
const [page, setPage] = useState(1);
  const pageSize = 10;

  const queryKey = ['item-picker', contentTypeId, search, status, page];

  const { data, isLoading, isFetching } = useQuery({
    queryKey,
    queryFn: () =>
      itemsApi.search({
     contentTypeId,
     search: search || undefined,
        status: status === 'All' ? undefined : status,
        page,
        pageSize,
      }),
    staleTime: 30_000,
  });

  const items = data?.items ?? [];
  const totalPages = data ? Math.ceil(data.totalCount / pageSize) : 0;

  const handleSearch = (v: string) => { setSearch(v); setPage(1); };
  const handleStatus = (v: typeof status) => { setStatus(v); setPage(1); };

  return (
  <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40 p-4">
      <div className="flex h-[560px] w-full max-w-2xl flex-col overflow-hidden rounded-xl bg-white shadow-2xl">

{/* Header */}
  <div className="flex flex-shrink-0 items-center justify-between border-b border-slate-200 px-5 py-4">
        <div>
     <p className="text-sm font-bold text-slate-900">Select content for <span className="text-brand-600">{componentName}</span></p>
     <p className="text-xs text-slate-400">Choose a published or draft item to bind to this placement.</p>
    </div>
    <button onClick={onClose} className="rounded-lg p-1.5 text-slate-400 hover:bg-slate-100">
      <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
 </svg>
   </button>
 </div>

 {/* Filters */}
      <div className="flex flex-shrink-0 items-center gap-3 border-b border-slate-100 bg-slate-50 px-5 py-3">
 {/* Search */}
        <div className="relative flex-1">
   <svg className="absolute left-2.5 top-1/2 h-3.5 w-3.5 -translate-y-1/2 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
       <circle cx="11" cy="11" r="8" strokeWidth="2" />
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-4.35-4.35" />
      </svg>
     <input
       className="w-full rounded-md border border-slate-200 bg-white py-1.5 pl-8 pr-3 text-xs focus:border-brand-400 focus:outline-none"
 placeholder="Search by title…"
    value={search}
          onChange={(e) => handleSearch(e.target.value)}
        />
        </div>
{/* Status pills */}
     <div className="flex gap-1">
  {(['All', 'Published', 'Draft', 'Archived'] as const).map((s) => (
       <button
      key={s}
onClick={() => handleStatus(s)}
    className={`rounded-full px-2.5 py-1 text-[11px] font-semibold transition-colors ${status === s ? 'bg-brand-600 text-white' : 'bg-white text-slate-500 hover:bg-slate-100 border border-slate-200'}`}
      >
   {s}
     </button>
          ))}
 </div>
      </div>

 {/* Results */}
      <div className="flex-1 overflow-y-auto">
 {isLoading ? (
          <div className="space-y-2 p-4">
            {Array.from({ length: 5 }).map((_, i) => (
     <div key={i} className="h-12 animate-pulse rounded-lg bg-slate-100" />
   ))}
     </div>
   ) : items.length === 0 ? (
          <div className="flex flex-col items-center justify-center gap-3 py-16 text-center">
      <svg className="h-8 w-8 text-slate-200" fill="none" viewBox="0 0 24 24" stroke="currentColor">
  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M9 13h6m-3-3v6m-9 1V7a2 2 0 012-2h6l2 2h6a2 2 0 012 2v8a2 2 0 01-2 2H5a2 2 0 01-2-2z" />
   </svg>
   <p className="text-sm text-slate-400">No items found</p>
       {search && <p className="text-xs text-slate-300">Try clearing your search</p>}
     </div>
      ) : (
       <div className="divide-y divide-slate-100">
       {items.map((item) => (
<div
         key={item.id}
           className="flex cursor-pointer items-center gap-4 px-5 py-3 transition-colors hover:bg-brand-50"
      onClick={() => { onSelect(item); onClose(); }}
           >
      <div className="min-w-0 flex-1">
              <p className="truncate text-sm font-semibold text-slate-800">{item.title}</p>
     <p className="font-mono text-[10px] text-slate-400">{item.id.slice(0, 8)}…</p>
           </div>
    <div className="flex flex-shrink-0 items-center gap-3">
  <span className={`rounded-full px-2 py-0.5 text-[10px] font-semibold ${STATUS_BADGE[item.status] ?? ''}`}>
     {item.status}
     </span>
              <span className="text-[11px] text-slate-400">{new Date(item.updatedAt).toLocaleDateString()}</span>
   <button
           onClick={(e) => { e.stopPropagation(); onSelect(item); onClose(); }}
    className="rounded-md border border-brand-200 bg-brand-50 px-2.5 py-1 text-xs font-semibold text-brand-700 hover:bg-brand-100"
        >
Select
   </button>
            </div>
 </div>
   ))}
       </div>
  )}
        {isFetching && !isLoading && (
     <div className="flex justify-center py-3">
<svg className="h-4 w-4 animate-spin text-brand-500" fill="none" viewBox="0 0 24 24">
   <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
            <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z" />
          </svg>
   </div>
    )}
      </div>

      {/* Pagination footer */}
      {totalPages > 1 && (
     <div className="flex flex-shrink-0 items-center justify-between border-t border-slate-200 bg-slate-50 px-5 py-3">
     <span className="text-xs text-slate-400">Page {page} of {totalPages}</span>
     <div className="flex gap-2">
          <button onClick={() => setPage(p => Math.max(1, p - 1))} disabled={page === 1}
          className="rounded border border-slate-200 bg-white px-2.5 py-1 text-xs text-slate-500 hover:bg-slate-50 disabled:opacity-40">
     ← Prev
  </button>
          <button onClick={() => setPage(p => Math.min(totalPages, p + 1))} disabled={page >= totalPages}
          className="rounded border border-slate-200 bg-white px-2.5 py-1 text-xs text-slate-500 hover:bg-slate-50 disabled:opacity-40">
       Next →
         </button>
          </div>
        </div>
      )}
    </div>
    </div>
  );
}
