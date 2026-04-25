import { useNavigate, useSearchParams } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { searchEntries } from '@/api/search';
import type { SearchHit } from '@/types';

// ─── Sub-components ───────────────────────────────────────────────────────────

function StatusBadge({ status }: { status: string }) {
  const colours: Record<string, string> = {
    Published: 'bg-green-100 text-green-700',
    Draft:     'bg-amber-100 text-amber-700',
 Archived:  'bg-slate-100 text-slate-500',
  };
  return (
    <span className={`rounded px-2 py-0.5 text-xs font-medium ${colours[status] ?? 'bg-slate-100 text-slate-500'}`}>
    {status}
    </span>
  );
}

function HitCard({ hit, onClick }: { hit: SearchHit; onClick: () => void }) {
return (
    <button
      onClick={onClick}
    className="flex w-full items-start gap-4 rounded-xl border border-slate-200 bg-white p-4 text-left shadow-sm transition hover:border-brand-300 hover:shadow-md focus:outline-none focus:ring-2 focus:ring-brand-500"
    >
      {/* Icon */}
      <div className="mt-0.5 flex h-9 w-9 flex-shrink-0 items-center justify-center rounded-lg bg-brand-50 text-brand-600">
        <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2}
          d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
        </svg>
      </div>

      <div className="min-w-0 flex-1">
        <div className="flex items-center gap-2">
          <p className="truncate font-semibold text-slate-800">{hit.title ?? hit.slug}</p>
          <StatusBadge status={hit.status} />
       <span className="ml-auto flex-shrink-0 text-xs text-slate-400">{hit.locale}</span>
        </div>
        {hit.excerpt && (
          <p className="mt-1 line-clamp-2 text-sm text-slate-500">{hit.excerpt}</p>
        )}
  {hit.publishedAt && (
        <p className="mt-1 text-xs text-slate-400">
         Published {new Date(hit.publishedAt).toLocaleDateString()}
          </p>
        )}
      </div>

      {/* Relevance score (dev / debug aid) */}
      <span className="ml-2 flex-shrink-0 self-start rounded-full bg-slate-100 px-2 py-0.5 text-[10px] font-mono text-slate-400">
        {hit.score.toFixed(3)}
      </span>
    </button>
  );
}

// ─── Page ─────────────────────────────────────────────────────────────────────

export default function SearchResultsPage() {
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();
  const q = searchParams.get('q') ?? '';
  const page = parseInt(searchParams.get('page') ?? '1', 10);

  const { data, isFetching, isError } = useQuery({
    queryKey: ['search', q, page],
    queryFn: () => searchEntries({ query: q, page, pageSize: 20 }),
    enabled: q.trim().length >= 2,
    staleTime: 30_000,
  });

  const hits: SearchHit[] = data?.hits ?? [];
  const totalCount = data?.totalCount ?? 0;
  const totalPages = Math.ceil(totalCount / 20);

  function handleHitClick(hit: SearchHit) {
    navigate(`/entries/${hit.entryId}/edit`);
  }

  function handlePageChange(next: number) {
    setSearchParams((prev) => {
      const updated = new URLSearchParams(prev);
      updated.set('page', String(next));
   return updated;
    });
    window.scrollTo(0, 0);
  }

  // ── Render ──────────────────────────────────────────────────────────────────

  return (
    <div className="mx-auto max-w-3xl">
      {/* Header */}
      <div className="mb-6">
 <h1 className="text-2xl font-bold text-slate-900">Search results</h1>
        {q && !isFetching && (
          <p className="mt-1 text-sm text-slate-500">
            {totalCount > 0
   ? `${totalCount.toLocaleString()} result${totalCount === 1 ? '' : 's'} for "${q}"`
              : `No results for "${q}"`}
          </p>
    )}
  </div>

      {/* Empty prompt — no query */}
    {!q && (
    <div className="flex flex-col items-center py-16 text-slate-400">
          <svg className="mb-3 h-12 w-12" fill="none" viewBox="0 0 24 24" stroke="currentColor">
     <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5}
              d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
       </svg>
          <p className="text-sm">Enter a search term to find entries.</p>
        </div>
      )}

      {/* Loading skeleton */}
      {isFetching && (
        <ul className="space-y-3" aria-busy="true" aria-label="Loading search results">
   {Array.from({ length: 5 }).map((_, i) => (
         <li key={i} className="h-20 animate-pulse rounded-xl bg-slate-100" />
          ))}
        </ul>
      )}

      {/* Error */}
      {isError && (
    <div className="rounded-xl border border-red-200 bg-red-50 p-4 text-sm text-red-600">
 Something went wrong fetching search results. Please try again.
    </div>
      )}

    {/* Results */}
      {!isFetching && hits.length > 0 && (
        <ul className="space-y-3">
          {hits.map((hit) => (
    <li key={hit.entryId}>
   <HitCard hit={hit} onClick={() => handleHitClick(hit)} />
    </li>
          ))}
   </ul>
      )}

      {/* Zero results */}
      {!isFetching && !isError && q.trim().length >= 2 && hits.length === 0 && (
        <div className="flex flex-col items-center py-16 text-slate-400">
 <svg className="mb-3 h-12 w-12" fill="none" viewBox="0 0 24 24" stroke="currentColor">
      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5}
              d="M9.172 16.172a4 4 0 015.656 0M9 10h.01M15 10h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
  </svg>
          <p className="text-sm">No entries matched your search.</p>
        </div>
      )}

      {/* Pagination */}
      {!isFetching && totalPages > 1 && (
        <div className="mt-8 flex items-center justify-between">
          <button
            disabled={page <= 1}
   onClick={() => handlePageChange(page - 1)}
   className="rounded-lg border border-slate-200 px-4 py-2 text-sm font-medium text-slate-600 hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-40"
          >
            ← Previous
</button>
<span className="text-sm text-slate-500">
            Page {page} of {totalPages}
          </span>
          <button
      disabled={page >= totalPages}
    onClick={() => handlePageChange(page + 1)}
            className="rounded-lg border border-slate-200 px-4 py-2 text-sm font-medium text-slate-600 hover:bg-slate-50 disabled:cursor-not-allowed disabled:opacity-40"
   >
        Next →
     </button>
   </div>
      )}
    </div>
  );
}
