import { useState, useCallback } from 'react';
import { useQuery } from '@tanstack/react-query';
import { componentsApi } from '@/api/components';
import type { ComponentListItem, ComponentItemDto } from '@/types';

// ─── Modal ────────────────────────────────────────────────────────────────────

const STATUS_COLORS: Record<string, string> = {
  Draft:     'bg-slate-100 text-slate-600',
  Published: 'bg-green-100 text-green-700',
  Archived:  'bg-red-100 text-red-600',
};

function ComponentItemPickerModal({
  restrictToKey,
  onSelect,
  onClose,
}: {
  /** When set, skip the component-selection step and go straight to items. */
  restrictToKey?: string;
  onSelect: (item: ComponentItemDto) => void;
  onClose: () => void;
}) {
  const [selectedComponent, setSelectedComponent] = useState<ComponentListItem | null>(null);
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const PAGE_SIZE = 20;

  const { data: components, isLoading: compsLoading } = useQuery({
    queryKey: ['component-picker-list'],
    queryFn: () => componentsApi.list({ pageSize: 200 }),
    staleTime: 60_000,
    enabled: !restrictToKey,
  });

  // When a restrictToKey is set, resolve the component automatically
  const { data: restrictedComps } = useQuery({
    queryKey: ['component-picker-restricted', restrictToKey],
    queryFn: () => componentsApi.list({ pageSize: 200, search: restrictToKey }),
    staleTime: 60_000,
    enabled: !!restrictToKey,
  });

  const resolvedComponent =
    selectedComponent ??
    (restrictToKey
      ? (restrictedComps?.items.find((c) => c.key === restrictToKey) ?? null)
      : null);

  const { data: itemsData, isLoading: itemsLoading } = useQuery({
    queryKey: ['component-picker-items', resolvedComponent?.id, page],
    queryFn: () =>
      componentsApi.listItems(resolvedComponent!.id, { pageNumber: page, pageSize: PAGE_SIZE }),
    enabled: !!resolvedComponent?.id,
  });

  const items = search && itemsData
    ? {
        ...itemsData,
        items: itemsData.items.filter((it) =>
          it.slug.toLowerCase().includes(search.toLowerCase())
        ),
      }
    : itemsData;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
      <div className="flex h-[75vh] w-full max-w-2xl flex-col overflow-hidden rounded-2xl bg-white shadow-2xl">
        {/* Header */}
        <div className="flex items-center justify-between border-b border-slate-200 px-5 py-4">
          <div>
            <h2 className="text-sm font-semibold text-slate-800">
              {resolvedComponent ? `Select Item — ${resolvedComponent.name}` : 'Select Component Type'}
            </h2>
            {resolvedComponent && (
              <p className="mt-0.5 text-xs text-slate-400">
                {resolvedComponent.itemCount} item{resolvedComponent.itemCount !== 1 ? 's' : ''}
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

        {/* Step 1 — pick a component type */}
        {!resolvedComponent && (
          <div className="flex flex-1 flex-col overflow-hidden">
            <div className="border-b border-slate-100 px-5 py-2.5">
              <input
                type="search"
                placeholder="Search components…"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                className="form-input w-full text-sm"
              />
            </div>
            <div className="flex-1 overflow-y-auto">
              {compsLoading ? (
                <div className="flex items-center justify-center py-12 text-sm text-slate-400">
                  Loading…
                </div>
              ) : (
                <ul>
                  {(components?.items ?? [])
                    .filter((c) =>
                      !search ||
                      c.name.toLowerCase().includes(search.toLowerCase()) ||
                      c.key.toLowerCase().includes(search.toLowerCase())
                    )
                    .map((comp) => (
                      <li key={comp.id}>
                        <button
                          type="button"
                          onClick={() => { setSelectedComponent(comp); setSearch(''); setPage(1); }}
                          className="flex w-full items-center gap-3 px-5 py-3 text-left hover:bg-slate-50"
                        >
                          {comp.thumbnailDataUri ? (
                            <img
                              src={comp.thumbnailDataUri}
                              alt={comp.name}
                              className="h-9 w-9 rounded border border-slate-200 object-cover"
                            />
                          ) : (
                            <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded border border-slate-200 bg-slate-50 text-slate-400">
                              <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5}
                                  d="M4 5a1 1 0 011-1h4a1 1 0 011 1v4a1 1 0 01-1 1H5a1 1 0 01-1-1V5zM14 5a1 1 0 011-1h4a1 1 0 011 1v4a1 1 0 01-1 1h-4a1 1 0 01-1-1V5zM4 15a1 1 0 011-1h4a1 1 0 011 1v4a1 1 0 01-1 1H5a1 1 0 01-1-1v-4zM14 15a1 1 0 011-1h4a1 1 0 011 1v4a1 1 0 01-1 1h-4a1 1 0 01-1-1v-4z" />
                              </svg>
                            </div>
                          )}
                          <div className="min-w-0 flex-1">
                            <p className="truncate text-sm font-medium text-slate-800">{comp.name}</p>
                            <p className="truncate font-mono text-xs text-slate-400">
                              {comp.key} · {comp.itemCount} item{comp.itemCount !== 1 ? 's' : ''}
                            </p>
                          </div>
                          <svg className="h-4 w-4 text-slate-300" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
                          </svg>
                        </button>
                      </li>
                    ))}
                </ul>
              )}
            </div>
          </div>
        )}

        {/* Step 2 — pick an item */}
        {resolvedComponent && (
          <div className="flex flex-1 flex-col overflow-hidden">
            <div className="flex items-center gap-2 border-b border-slate-100 px-5 py-2.5">
              {!restrictToKey && (
                <button
                  type="button"
                  onClick={() => { setSelectedComponent(null); setSearch(''); }}
                  className="rounded p-0.5 text-slate-400 hover:text-slate-600"
                  aria-label="Back"
                >
                  <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
                  </svg>
                </button>
              )}
              <input
                type="search"
                placeholder="Search by slug…"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                className="form-input flex-1 text-sm"
              />
            </div>
            <div className="flex-1 overflow-y-auto">
              {itemsLoading ? (
                <div className="flex items-center justify-center py-12 text-sm text-slate-400">
                  Loading…
                </div>
              ) : (
                <ul>
                  {(items?.items ?? []).map((item) => (
                    <li key={item.id}>
                      <button
                        type="button"
                        onClick={() => onSelect(item)}
                        className="flex w-full items-center gap-3 px-5 py-3 text-left hover:bg-slate-50"
                      >
                        <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded border border-slate-200 bg-slate-50 text-slate-400">
                          <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5}
                              d="M4 5a1 1 0 011-1h4a1 1 0 011 1v4a1 1 0 01-1 1H5a1 1 0 01-1-1V5zM14 5a1 1 0 011-1h4a1 1 0 011 1v4a1 1 0 01-1 1h-4a1 1 0 01-1-1V5zM4 15a1 1 0 011-1h4a1 1 0 011 1v4a1 1 0 01-1 1H5a1 1 0 01-1-1v-4zM14 15a1 1 0 011-1h4a1 1 0 011 1v4a1 1 0 01-1 1h-4a1 1 0 01-1-1v-4z" />
                          </svg>
                        </div>
                        <div className="min-w-0 flex-1">
                          <p className="truncate font-mono text-sm text-slate-800">{item.slug}</p>
                          <p className="truncate font-mono text-[10px] text-slate-400">{item.id}</p>
                        </div>
                        <span
                          className={`shrink-0 rounded px-1.5 py-0.5 text-[9px] font-semibold uppercase ${STATUS_COLORS[item.status] ?? STATUS_COLORS.Draft}`}
                        >
                          {item.status}
                        </span>
                      </button>
                    </li>
                  ))}
                  {(items?.items ?? []).length === 0 && (
                    <li className="py-10 text-center text-sm text-slate-400">No items found</li>
                  )}
                </ul>
              )}
            </div>
            {/* Pagination */}
            {items && items.totalCount > PAGE_SIZE && (
              <div className="flex items-center justify-between border-t border-slate-100 px-5 py-3">
                <p className="text-xs text-slate-400">
                  {items.totalCount} total
                </p>
                <div className="flex gap-2">
                  <button
                    disabled={page === 1}
                    onClick={() => setPage((p) => p - 1)}
                    className="rounded border border-slate-200 px-2.5 py-1 text-xs text-slate-600 hover:bg-slate-50 disabled:opacity-40"
                  >
                    ← Prev
                  </button>
                  <button
                    disabled={page * PAGE_SIZE >= items.totalCount}
                    onClick={() => setPage((p) => p + 1)}
                    className="rounded border border-slate-200 px-2.5 py-1 text-xs text-slate-600 hover:bg-slate-50 disabled:opacity-40"
                  >
                    Next →
                  </button>
                </div>
              </div>
            )}
          </div>
        )}
      </div>
    </div>
  );
}

// ─── Field ────────────────────────────────────────────────────────────────────

export function ComponentItemPickerField({
  value: rawValue,
  onChange,
  restrictToKey,
}: {
  value: unknown;
  onChange: (value: string | null) => void;
  /** Optional component key to restrict the picker to a specific component type. */
  restrictToKey?: string;
}) {
  const [open, setOpen] = useState(false);
  const valueId = typeof rawValue === 'string' && rawValue ? rawValue : null;

  const { data: resolvedItem } = useQuery({
    queryKey: ['component-item-ref-preview', valueId],
    queryFn: async () => {
      // We don't know the componentId from the item id alone — use a search via list
      // The value is just the item ID; the preview shows the slug from the stored id.
      return null; // Can't resolve without component id — fallback to showing raw id
    },
    enabled: false, // disabled — we show the raw id as preview
  });

  void resolvedItem;

  const handleSelect = useCallback(
    (item: ComponentItemDto) => {
      onChange(item.id);
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

  if (!valueId) {
    return (
      <>
        <button
          type="button"
          onClick={() => setOpen(true)}
          className="flex w-full flex-col items-center justify-center gap-2 rounded-lg border border-dashed border-slate-300 bg-slate-50 py-6 transition-colors hover:border-brand-400 hover:bg-brand-50 focus:outline-none focus:ring-2 focus:ring-brand-400"
        >
          <div className="flex h-9 w-9 items-center justify-center rounded-full bg-slate-200 text-slate-400">
            <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5}
                d="M4 5a1 1 0 011-1h4a1 1 0 011 1v4a1 1 0 01-1 1H5a1 1 0 01-1-1V5zM14 5a1 1 0 011-1h4a1 1 0 011 1v4a1 1 0 01-1 1h-4a1 1 0 01-1-1V5zM4 15a1 1 0 011-1h4a1 1 0 011 1v4a1 1 0 01-1 1H5a1 1 0 01-1-1v-4zM14 15a1 1 0 011-1h4a1 1 0 011 1v4a1 1 0 01-1 1h-4a1 1 0 01-1-1v-4z" />
            </svg>
          </div>
          <p className="text-xs text-slate-500">
            {restrictToKey ? `Select a ${restrictToKey} item` : 'Select a component item'}
          </p>
          <span className="rounded-md border border-slate-300 bg-white px-3 py-1 text-xs font-semibold text-slate-700 shadow-sm hover:border-brand-400 hover:text-brand-600">
            Browse
          </span>
        </button>
        {open && (
          <ComponentItemPickerModal
            restrictToKey={restrictToKey}
            onSelect={handleSelect}
            onClose={() => setOpen(false)}
          />
        )}
      </>
    );
  }

  return (
    <>
      <div className="group relative overflow-hidden rounded-lg border border-slate-200 bg-white">
        <div className="flex items-center gap-3 px-3 py-3">
          <div className="flex h-9 w-9 shrink-0 items-center justify-center rounded-md bg-brand-50 text-brand-500">
            <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5}
                d="M4 5a1 1 0 011-1h4a1 1 0 011 1v4a1 1 0 01-1 1H5a1 1 0 01-1-1V5zM14 5a1 1 0 011-1h4a1 1 0 011 1v4a1 1 0 01-1 1h-4a1 1 0 01-1-1V5zM4 15a1 1 0 011-1h4a1 1 0 011 1v4a1 1 0 01-1 1H5a1 1 0 01-1-1v-4zM14 15a1 1 0 011-1h4a1 1 0 011 1v4a1 1 0 01-1 1h-4a1 1 0 01-1-1v-4z" />
            </svg>
          </div>
          <div className="min-w-0 flex-1">
            <p className="truncate font-mono text-xs text-slate-400">{valueId}</p>
          </div>
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
        <ComponentItemPickerModal
          restrictToKey={restrictToKey}
          onSelect={handleSelect}
          onClose={() => setOpen(false)}
        />
      )}
    </>
  );
}
