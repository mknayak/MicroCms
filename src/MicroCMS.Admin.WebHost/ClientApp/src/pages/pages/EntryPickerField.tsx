import { useState, useCallback } from 'react';
import { useQuery } from '@tanstack/react-query';
import { entriesApi } from '@/api/entries';
import { contentTypesApi } from '@/api/contentTypes';
import type { EntryListItem, EntryStatus, FieldDynamicSource } from '@/types';

// ─── Stored value shape ───────────────────────────────────────────────────────
// Persisted in entry.fields[handle] as a plain GUID string.

export interface EntryReferenceValue {
  /** Entry.Id (GUID string) */
  id: string;
}

function parseValue(raw: unknown): EntryReferenceValue | null {
  if (!raw) return null;
  if (typeof raw === 'string' && raw.length > 0) return { id: raw };
  if (typeof raw === 'object' && raw !== null && 'id' in raw) return raw as EntryReferenceValue;
  return null;
}

// ─── Status badge helpers ─────────────────────────────────────────────────────

const STATUS_COLORS: Record<string, string> = {
  Draft:         'bg-slate-100 text-slate-600',
  PendingReview: 'bg-amber-100 text-amber-700',
  Approved:      'bg-blue-100 text-blue-700',
  Published:     'bg-green-100 text-green-700',
  Unpublished:   'bg-slate-100 text-slate-500',
  Scheduled:     'bg-brand-100 text-brand-700',
  Archived:      'bg-red-100 text-red-600',
};

function StatusBadge({ status }: { status: string }) {
  return (
    <span className={`shrink-0 rounded px-1.5 py-0.5 text-[9px] font-semibold uppercase ${STATUS_COLORS[status] ?? 'bg-slate-100 text-slate-600'}`}>
      {status}
    </span>
  );
}

// ─── Modal ────────────────────────────────────────────────────────────────────

function EntryPickerModal({
  source,
  onSelect,
  onClose,
}: {
  source: FieldDynamicSource | undefined;
  onSelect: (entry: EntryListItem) => void;
  onClose: () => void;
}) {
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const PAGE_SIZE = 20;

  // Resolve contentTypeHandle → contentTypeId
  const { data: contentTypes } = useQuery({
    queryKey: ['content-types-picker-all'],
    queryFn: () => contentTypesApi.list({ pageSize: 200 }),
    staleTime: 60_000,
  });

  const contentTypeId = contentTypes?.items.find(
    (ct) => ct.handle === source?.contentTypeHandle,
  )?.id;

  const statusFilter = (source?.statusFilter ?? 'Published') as EntryStatus;

  const { data: rawData, isLoading } = useQuery({
    queryKey: ['entry-picker', { contentTypeId, statusFilter, page }],
    queryFn: () =>
      entriesApi.list({
        contentTypeId,
        status: statusFilter,
        pageNumber: page,
        pageSize: PAGE_SIZE,
      }),
    enabled: !!contentTypeId,
  });

  // Client-side filter by title/slug since the list endpoint has no search param
  const data = rawData
    ? {
        ...rawData,
        items: search
          ? rawData.items.filter((e) => {
              const q = search.toLowerCase();
              return (
                (e.title ?? '').toLowerCase().includes(q) ||
                e.slug.toLowerCase().includes(q)
              );
            })
          : rawData.items,
      }
    : rawData;

  const contentTypeName = contentTypes?.items.find((ct) => ct.id === contentTypeId)?.displayName;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
      <div className="flex h-[75vh] w-full max-w-2xl flex-col overflow-hidden rounded-2xl bg-white shadow-2xl">
        {/* Header */}
        <div className="flex items-center justify-between border-b border-slate-200 px-5 py-4">
          <div>
            <h2 className="text-sm font-semibold text-slate-800">Select Entry</h2>
            {contentTypeName && (
              <p className="mt-0.5 text-xs text-slate-400">
                {contentTypeName} · {statusFilter}
              </p>
            )}
          </div>
          <button
            onClick={onClose}
            className="rounded-md p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-600"
            aria-label="Close"
          >
            <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        {/* Search */}
        <div className="border-b border-slate-100 px-5 py-2.5">
          <input
            type="search"
            placeholder="Search by title or slug…"
            value={search}
            onChange={(e) => { setSearch(e.target.value); setPage(1); }}
            className="form-input w-full text-sm"
          />
        </div>

        {/* Content */}
        <div className="flex-1 overflow-y-auto">
          {!source?.contentTypeHandle ? (
            <div className="flex flex-col items-center justify-center py-16 text-slate-400">
              <svg className="mb-3 h-10 w-10" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M12 9v2m0 4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
              </svg>
              <p className="text-sm font-medium">No source content type configured</p>
              <p className="mt-1 text-xs">Edit the field definition to set a Reference source.</p>
            </div>
          ) : isLoading ? (
            <div className="space-y-2 px-5 py-4">
              {Array.from({ length: 8 }).map((_, i) => (
                <div key={i} className="h-12 animate-pulse rounded-lg bg-slate-100" />
              ))}
            </div>
          ) : !data?.items.length ? (
            <div className="flex flex-col items-center justify-center py-16 text-slate-400">
              <svg className="mb-3 h-10 w-10" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
              </svg>
              <p className="text-sm font-medium">No entries found</p>
            </div>
          ) : (
            <ul className="divide-y divide-slate-100">
              {data.items.map((entry) => (
                <li key={entry.id}>
                  <button
                    onClick={() => onSelect(entry)}
                    className="flex w-full items-center gap-3 px-5 py-3 text-left transition-colors hover:bg-brand-50 focus:outline-none focus:bg-brand-50"
                  >
                    {/* Icon */}
                    <div className="flex h-8 w-8 shrink-0 items-center justify-center rounded-md bg-slate-100 text-slate-400">
                      <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                      </svg>
                    </div>

                    {/* Title / slug */}
                    <div className="min-w-0 flex-1">
                      <p className="truncate text-sm font-medium text-slate-800">
                        {entry.title ?? entry.slug}
                      </p>
                      <p className="truncate font-mono text-[10px] text-slate-400">{entry.slug}</p>
                    </div>

                    {/* Status */}
                    <StatusBadge status={entry.status} />
                  </button>
                </li>
              ))}
            </ul>
          )}
        </div>

        {/* Pagination */}
        {data && data.totalCount > PAGE_SIZE && (
          <div className="flex items-center justify-between border-t border-slate-100 px-5 py-3">
            <p className="text-xs text-slate-500">
              {data.totalCount} entr{data.totalCount !== 1 ? 'ies' : 'y'}
            </p>
            <div className="flex gap-2">
              <button
                disabled={page <= 1}
                onClick={() => setPage((p) => p - 1)}
                className="rounded border border-slate-200 px-2.5 py-1 text-xs text-slate-600 hover:bg-slate-50 disabled:opacity-40"
              >
                ← Prev
              </button>
              <button
                disabled={page * PAGE_SIZE >= data.totalCount}
                onClick={() => setPage((p) => p + 1)}
                className="rounded border border-slate-200 px-2.5 py-1 text-xs text-slate-600 hover:bg-slate-50 disabled:opacity-40"
              >
                Next →
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}

// ─── Field ────────────────────────────────────────────────────────────────────

export function EntryPickerField({
  value: rawValue,
  onChange,
  source,
}: {
  /**
   * The raw stored value — a plain GUID string or an EntryReferenceValue object.
   * Pass `null` / `undefined` when empty.
   */
  value: unknown;
  onChange: (value: string | null) => void;
  /** Dynamic source config from the field definition — drives which entries to show. */
  source?: FieldDynamicSource;
}) {
  const [open, setOpen] = useState(false);

  const value = parseValue(rawValue);

  // Resolve the stored ID → entry metadata for the preview card
  const { data: entry } = useQuery({
    queryKey: ['entry-ref-preview', value?.id],
    queryFn: () => entriesApi.getById(value!.id),
    enabled: !!value?.id,
    staleTime: 60_000,
  });

  const handleSelect = useCallback(
    (selected: EntryListItem) => {
      onChange(selected.id);
      setOpen(false);
    },
    [onChange],
  );

  const handleClear = useCallback(
    (e: React.MouseEvent) => {
      e.stopPropagation();
      onChange(null);
    },
    [onChange],
  );

  if (!value?.id) {
    /* ── Empty / browse state ── */
    return (
      <>
        <button
          type="button"
          onClick={() => setOpen(true)}
          className="flex w-full flex-col items-center justify-center gap-2 rounded-lg border border-dashed border-slate-300 bg-slate-50 py-6 transition-colors hover:border-brand-400 hover:bg-brand-50 focus:outline-none focus:ring-2 focus:ring-brand-400"
        >
          <div className="flex h-9 w-9 items-center justify-center rounded-full bg-slate-200 text-slate-400">
            <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
            </svg>
          </div>
          <p className="text-xs text-slate-500">
            {source?.contentTypeHandle
              ? `Select a ${source.contentTypeHandle} entry`
              : 'Select an entry'}
          </p>
          <span className="rounded-md border border-slate-300 bg-white px-3 py-1 text-xs font-semibold text-slate-700 shadow-sm hover:border-brand-400 hover:text-brand-600">
            Browse
          </span>
        </button>
        {open && (
          <EntryPickerModal
            source={source}
            onSelect={handleSelect}
            onClose={() => setOpen(false)}
          />
        )}
      </>
    );
  }

  /* ── Selected state — compact preview card ── */
  const displayTitle = entry?.fields?.title as string | undefined;
  const displaySlug  = entry?.slug ?? value.id;

  return (
    <>
      <div className="group relative overflow-hidden rounded-lg border border-slate-200 bg-white">
        <div className="flex items-center gap-3 px-3 py-3">
          {/* Icon */}
          <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-brand-50 text-brand-500">
            <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
            </svg>
          </div>

          {/* Details */}
          <div className="min-w-0 flex-1">
            <p className="truncate text-sm font-medium text-slate-800">
              {displayTitle ?? displaySlug}
            </p>
            <p className="truncate font-mono text-[10px] text-slate-400">{value.id}</p>
          </div>

          {/* Status */}
          {entry && <StatusBadge status={entry.status} />}

          {/* Actions */}
          <div className="flex shrink-0 items-center gap-1 opacity-0 transition-opacity group-hover:opacity-100">
            <button
              type="button"
              onClick={() => setOpen(true)}
              className="rounded px-2 py-1 text-[11px] font-semibold text-slate-600 hover:bg-slate-100"
            >
              Change
            </button>
            <button
              type="button"
              onClick={handleClear}
              className="rounded px-2 py-1 text-[11px] font-semibold text-red-500 hover:bg-red-50"
            >
              Remove
            </button>
          </div>
        </div>
      </div>

      {open && (
        <EntryPickerModal
          source={source}
          onSelect={handleSelect}
          onClose={() => setOpen(false)}
        />
      )}
    </>
  );
}
