import { useEffect, useRef, useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { searchEntries } from '@/api/search';
import type { SearchHit } from '@/types';

// ─── Types ────────────────────────────────────────────────────────────────────

interface GlobalSearchBarProps {
  /** Called when the user dismisses the bar (mobile close button). */
  onClose?: () => void;
}

// ─── Helpers ─────────────────────────────────────────────────────────────────

function useDebounce<T>(value: T, delayMs: number): T {
  const [debounced, setDebounced] = useState(value);
  useEffect(() => {
    const id = window.setTimeout(() => setDebounced(value), delayMs);
    return () => window.clearTimeout(id);
  }, [value, delayMs]);
  return debounced;
}

function StatusBadge({ status }: { status: string }) {
  const colours: Record<string, string> = {
    Published: 'bg-green-100 text-green-700',
    Draft: 'bg-amber-100 text-amber-700',
    Archived: 'bg-slate-100 text-slate-500',
  };
  const cls = colours[status] ?? 'bg-slate-100 text-slate-500';
  return (
    <span className={`rounded px-1.5 py-0.5 text-[10px] font-medium ${cls}`}>{status}</span>
  );
}

// ─── Component ────────────────────────────────────────────────────────────────

export function GlobalSearchBar({ onClose }: GlobalSearchBarProps) {
  const navigate = useNavigate();
  const [query, setQuery] = useState('');
  const [open, setOpen] = useState(false);
  const [activeIdx, setActiveIdx] = useState(-1);
  const inputRef = useRef<HTMLInputElement>(null);
  const panelRef = useRef<HTMLDivElement>(null);

  const debouncedQuery = useDebounce(query.trim(), 300);

  const { data, isFetching } = useQuery({
    queryKey: ['globalSearch', debouncedQuery],
queryFn: () => searchEntries({ query: debouncedQuery, pageSize: 8 }),
    enabled: debouncedQuery.length >= 2,
    staleTime: 30_000,
  });

  const hits: SearchHit[] = data?.hits ?? [];

  // Open / close popover
  useEffect(() => {
    if (debouncedQuery.length >= 2) {
      setOpen(true);
      setActiveIdx(-1);
    } else {
      setOpen(false);
    }
  }, [debouncedQuery]);

  // Close on outside click
  useEffect(() => {
    function handlePointerDown(e: PointerEvent) {
      if (
      panelRef.current &&
        !panelRef.current.contains(e.target as Node) &&
    !inputRef.current?.contains(e.target as Node)
      ) {
        setOpen(false);
      }
    }
    document.addEventListener('pointerdown', handlePointerDown);
    return () => document.removeEventListener('pointerdown', handlePointerDown);
  }, []);

  // Global ⌘K / Ctrl+K shortcut
  useEffect(() => {
    function handleKeydown(e: KeyboardEvent) {
      if ((e.metaKey || e.ctrlKey) && e.key === 'k') {
        e.preventDefault();
        inputRef.current?.focus();
   inputRef.current?.select();
      }
    }
    document.addEventListener('keydown', handleKeydown);
    return () => document.removeEventListener('keydown', handleKeydown);
  }, []);

  function handleKeyDown(e: React.KeyboardEvent<HTMLInputElement>) {
    if (!open || hits.length === 0) {
      if (e.key === 'Enter' && query.trim().length >= 2) {
        navigateToFullResults();
      }
      return;
 }

    if (e.key === 'ArrowDown') {
      e.preventDefault();
      setActiveIdx((i) => Math.min(i + 1, hits.length)); // +1 for "see all" row
    } else if (e.key === 'ArrowUp') {
      e.preventDefault();
  setActiveIdx((i) => Math.max(i - 1, -1));
    } else if (e.key === 'Enter') {
      e.preventDefault();
      if (activeIdx >= 0 && activeIdx < hits.length) {
        navigateToEntry(hits[activeIdx]);
   } else {
        navigateToFullResults();
      }
    } else if (e.key === 'Escape') {
      setOpen(false);
      inputRef.current?.blur();
    }
  }

  function navigateToEntry(hit: SearchHit) {
    setOpen(false);
    setQuery('');
    navigate(`/entries/${hit.entryId}/edit`);
  }

  function navigateToFullResults() {
    setOpen(false);
    navigate(`/search?q=${encodeURIComponent(query.trim())}`);
    setQuery('');
    onClose?.();
  }

  return (
    <div className="relative w-full max-w-md" role="search">
      {/* Input */}
      <div className="relative flex items-center">
        <svg
   className="pointer-events-none absolute left-3 h-4 w-4 text-slate-400"
    fill="none"
          viewBox="0 0 24 24"
          stroke="currentColor"
     aria-hidden="true"
        >
<path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2}
   d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
 </svg>

        <input
          ref={inputRef}
          type="search"
          role="combobox"
     aria-label="Global search"
          aria-expanded={open}
        aria-controls="global-search-panel"
          aria-autocomplete="list"
        aria-activedescendant={activeIdx >= 0 ? `search-hit-${activeIdx}` : undefined}
          value={query}
      onChange={(e) => setQuery(e.target.value)}
          onKeyDown={handleKeyDown}
          onFocus={() => { if (debouncedQuery.length >= 2) setOpen(true); }}
  placeholder="Search entries… ⌘K"
    className="h-9 w-full rounded-lg border border-slate-200 bg-slate-50 pl-9 pr-10 text-sm text-slate-700 placeholder-slate-400 focus:border-brand-500 focus:bg-white focus:outline-none focus:ring-1 focus:ring-brand-500"
          autoComplete="off"
          spellCheck={false}
  />

        {/* Loading spinner */}
        {isFetching && (
          <svg
            className="absolute right-3 h-4 w-4 animate-spin text-brand-500"
            fill="none"
            viewBox="0 0 24 24"
            aria-hidden="true"
          >
     <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
  <path className="opacity-75" fill="currentColor"
    d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
          </svg>
     )}

        {/* Clear button */}
        {!isFetching && query.length > 0 && (
          <button
            onClick={() => { setQuery(''); setOpen(false); inputRef.current?.focus(); }}
    className="absolute right-2.5 rounded p-0.5 text-slate-400 hover:text-slate-600"
    aria-label="Clear search"
            tabIndex={-1}
          >
     <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
     <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
        </button>
        )}
      </div>

      {/* Popover */}
      {open && (
        <div
          id="global-search-panel"
          ref={panelRef}
          role="listbox"
 aria-label="Search results"
          className="absolute top-11 z-50 w-full overflow-hidden rounded-xl border border-slate-200 bg-white shadow-lg"
        >
          {hits.length === 0 && !isFetching && (
      <p className="px-4 py-3 text-sm text-slate-500">
              No results for <span className="font-medium">"{debouncedQuery}"</span>
  </p>
          )}

   {hits.map((hit, idx) => (
        <button
 key={hit.entryId}
              id={`search-hit-${idx}`}
     role="option"
           aria-selected={idx === activeIdx}
    onClick={() => navigateToEntry(hit)}
       onMouseEnter={() => setActiveIdx(idx)}
  className={`flex w-full items-center gap-3 px-4 py-2.5 text-left transition-colors ${
 idx === activeIdx ? 'bg-brand-50 text-brand-700' : 'text-slate-700 hover:bg-slate-50'
              }`}
 >
        {/* Entry icon */}
            <svg className="h-4 w-4 flex-shrink-0 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
 <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2}
     d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
       </svg>

        <div className="min-w-0 flex-1">
       <p className="truncate text-sm font-medium">
         {hit.title ?? hit.slug}
    </p>
                {hit.excerpt && (
   <p className="truncate text-xs text-slate-500">{hit.excerpt}</p>
         )}
   </div>

           <div className="flex flex-shrink-0 items-center gap-1.5">
                <span className="text-xs text-slate-400">{hit.locale}</span>
      <StatusBadge status={hit.status} />
         </div>
  </button>
          ))}

          {/* "See all results" footer */}
          {(hits.length > 0 || isFetching) && (
     <button
     role="option"
        aria-selected={activeIdx === hits.length}
            onMouseEnter={() => setActiveIdx(hits.length)}
         onClick={navigateToFullResults}
        className={`flex w-full items-center justify-between border-t border-slate-100 px-4 py-2 text-sm transition-colors ${
         activeIdx === hits.length
          ? 'bg-brand-50 text-brand-700'
    : 'text-slate-500 hover:bg-slate-50'
              }`}
            >
    <span>See all results for "{debouncedQuery}"</span>
     <kbd className="rounded border border-slate-200 bg-slate-100 px-1.5 py-0.5 text-[10px] text-slate-500">
                ↵
              </kbd>
  </button>
          )}
  </div>
    )}
    </div>
  );
}
