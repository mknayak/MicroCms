import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { formatDistanceToNow } from 'date-fns';
import toast from 'react-hot-toast';
import { entriesApi } from '@/api/entries';
import type { EntryListItem, EntryStatus } from '@/types';
import { ApiError } from '@/api/client';
import { useSite } from '@/contexts/SiteContext';
import { STATUS_STYLES } from './contentTypeDetail.shared';

// ─── Constants ────────────────────────────────────────────────────────────────

const STATUSES: EntryStatus[] = [
  'Draft', 'PendingReview', 'Approved', 'Published', 'Unpublished', 'Archived', 'Scheduled',
];
const LOCALES = ['en', 'en-US', 'fr-FR', 'de-DE', 'es', 'pt', 'ja', 'zh'];

// ─── Avatar ───────────────────────────────────────────────────────────────────

function Avatar({ name }: { name?: string }) {
  const initials = (name ?? '?').split(' ').map((w) => w[0]).slice(0, 2).join('').toUpperCase();
  const colors = ['bg-violet-500', 'bg-blue-500', 'bg-emerald-500', 'bg-amber-500', 'bg-rose-500'];
  const idx = (name ?? '').charCodeAt(0) % colors.length;
  return (
    <span className={`inline-flex h-7 w-7 items-center justify-center rounded-full text-xs font-semibold text-white ${colors[idx]}`}>
      {initials}
    </span>
  );
}

// ─── EntryRow ─────────────────────────────────────────────────────────────────

function EntryRow({
  entry,
  selected,
  onToggle,
  onEdit,
  onDelete,
}: {
  entry: EntryListItem;
  selected: boolean;
  onToggle: () => void;
  onEdit: () => void;
  onDelete: () => void;
}) {
const qc = useQueryClient();

  const publishMutation = useMutation({
    mutationFn: () => entriesApi.publish(entry.id),
 onSuccess: () => { toast.success('Published.'); void qc.invalidateQueries({ queryKey: ['entries'] }); },
    onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Failed.'),
  });

  const unpublishMutation = useMutation({
    mutationFn: () => entriesApi.unpublish(entry.id),
    onSuccess: () => { toast.success('Unpublished.'); void qc.invalidateQueries({ queryKey: ['entries'] }); },
    onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Failed.'),
  });

  return (
    <tr className={`hover:bg-slate-50 ${selected ? 'bg-brand-50/40' : ''}`}>
      <td className="w-10 px-4 py-4 text-center">
        <input type="checkbox" checked={selected} onChange={onToggle} className="h-4 w-4 rounded border-slate-300 text-brand-600" />
      </td>
      <td className="px-6 py-4">
   <button onClick={onEdit} className="text-left">
  <p className="font-medium text-slate-900 hover:text-brand-600">{entry.title ?? entry.slug}</p>
       <p className="font-mono text-xs text-slate-400">{entry.slug}</p>
    </button>
      </td>
      <td className="px-4 py-4">
<span className={`rounded-full px-2 py-0.5 text-xs font-medium ${STATUS_STYLES[entry.status] ?? 'bg-slate-100 text-slate-500'}`}>
       {entry.status === 'PendingReview' ? 'Pending Review' : entry.status}
        </span>
      </td>
      <td className="px-4 py-4 text-xs text-slate-500 font-mono">{entry.locale}</td>
      <td className="px-4 py-4">
        <div className="flex items-center gap-2">
    <Avatar name={entry.authorName} />
          <span className="text-xs text-slate-600 hidden xl:block">{entry.authorName ?? '—'}</span>
        </div>
      </td>
   <td className="px-4 py-4 text-xs text-slate-400">v{entry.currentVersionNumber}</td>
      <td className="px-4 py-4 text-xs text-slate-400 whitespace-nowrap">
  {formatDistanceToNow(new Date(entry.updatedAt), { addSuffix: true })}
      </td>
   <td className="px-4 py-4 text-right">
        <div className="flex items-center justify-end gap-2">
          <button onClick={onEdit} className="rounded px-2 py-1 text-xs font-medium text-brand-600 hover:bg-brand-50">
            Edit
          </button>
    {(entry.status === 'Draft' || entry.status === 'Approved') && (
            <button
           onClick={() => publishMutation.mutate()}
         disabled={publishMutation.isPending}
         className="rounded px-2 py-1 text-xs font-medium text-green-600 hover:bg-green-50 disabled:opacity-50"
            >
         Publish
            </button>
          )}
          {entry.status === 'Published' && (
  <button
              onClick={() => unpublishMutation.mutate()}
    disabled={unpublishMutation.isPending}
       className="rounded px-2 py-1 text-xs font-medium text-amber-600 hover:bg-amber-50 disabled:opacity-50"
            >
              Unpublish
          </button>
          )}
          <button onClick={onDelete} className="rounded px-2 py-1 text-xs font-medium text-red-600 hover:bg-red-50">
   Delete
          </button>
        </div>
      </td>
    </tr>
  );
}

// ─── EntriesTab ───────────────────────────────────────────────────────────────

export function EntriesTab({ contentTypeId }: { contentTypeId: string }) {
  const navigate = useNavigate();
  const { selectedSiteId } = useSite();
  const qc = useQueryClient();
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [localeFilter, setLocaleFilter] = useState('');
  const [page, setPage] = useState(1);
  const [selected, setSelected] = useState<Set<string>>(new Set());

  const { data, isLoading } = useQuery({
    queryKey: ['entries', { contentTypeId, siteId: selectedSiteId, status: statusFilter || undefined, locale: localeFilter || undefined, search: search || undefined, pageNumber: page }],
    queryFn: () => entriesApi.list({
      siteId: selectedSiteId ?? undefined,
      contentTypeId,
    status: (statusFilter as EntryStatus) || undefined,
      locale: localeFilter || undefined,
      search: search || undefined,
      pageNumber: page,
      pageSize: 10,
    }),
    enabled: !!selectedSiteId,
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => entriesApi.delete(id),
    onSuccess: () => { toast.success('Entry deleted.'); void qc.invalidateQueries({ queryKey: ['entries'] }); },
    onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Delete failed.'),
  });

  const publishMutation = useMutation({
    mutationFn: (id: string) => entriesApi.publish(id),
 onSuccess: () => { toast.success('Entry published.'); void qc.invalidateQueries({ queryKey: ['entries'] }); },
    onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Publish failed.'),
  });

  const unpublishMutation = useMutation({
    mutationFn: (id: string) => entriesApi.unpublish(id),
    onSuccess: () => { toast.success('Entry unpublished.'); void qc.invalidateQueries({ queryKey: ['entries'] }); },
    onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Unpublish failed.'),
  });

  const items = data?.items ?? [];
  const allSelected = items.length > 0 && items.every((e) => selected.has(e.id));

  const toggleAll = () => {
    if (allSelected) setSelected(new Set());
    else setSelected(new Set(items.map((e) => e.id)));
  };

  const toggleOne = (id: string) => {
    const next = new Set(selected);
    if (next.has(id)) next.delete(id); else next.add(id);
    setSelected(next);
  };

const handleBulkPublish = () => { selected.forEach((id) => publishMutation.mutate(id)); setSelected(new Set()); };
  const handleBulkUnpublish = () => { selected.forEach((id) => unpublishMutation.mutate(id)); setSelected(new Set()); };
  const handleBulkDelete = () => {
    if (!confirm(`Delete ${selected.size} entries? This cannot be undone.`)) return;
    selected.forEach((id) => deleteMutation.mutate(id));
    setSelected(new Set());
  };

  return (
    <div className="space-y-4">
      {/* Toolbar */}
      <div className="flex items-center gap-2">
    <input
   type="search"
    value={search}
          onChange={(e) => { setSearch(e.target.value); setPage(1); }}
     placeholder="Search entries…"
          className="form-input w-56 shrink-0"
        />
 <select className="form-input w-auto shrink-0" value={statusFilter} onChange={(e) => { setStatusFilter(e.target.value); setPage(1); }}>
          <option value="">All Statuses</option>
       {STATUSES.map((s) => <option key={s} value={s}>{s}</option>)}
 </select>
        <select className="form-input w-auto shrink-0" value={localeFilter} onChange={(e) => { setLocaleFilter(e.target.value); setPage(1); }}>
<option value="">All Locales</option>
   {LOCALES.map((l) => <option key={l} value={l}>{l}</option>)}
        </select>
        <div className="ml-auto flex items-center gap-2">
  <button className="btn-secondary text-sm">↓ Export</button>
          <button
            onClick={() => navigate(`/entries/new?contentTypeId=${contentTypeId}&siteId=${selectedSiteId ?? ''}`)}
  className="btn-primary text-sm"
          >
            + New Entry
          </button>
    </div>
      </div>

      {/* Bulk action bar */}
 {selected.size > 0 && (
      <div className="flex items-center gap-3 rounded-lg border border-brand-200 bg-brand-50 px-4 py-2 text-sm">
          <span className="font-medium text-brand-700">{selected.size} selected</span>
          <span className="text-brand-400">·</span>
          <span className="text-brand-600">Choose a bulk action:</span>
          <button onClick={handleBulkPublish} className="font-medium text-brand-700 hover:underline">Publish</button>
          <button onClick={handleBulkUnpublish} className="font-medium text-slate-600 hover:underline">Unpublish</button>
      <button className="font-medium text-slate-600 hover:underline">Export</button>
          <button onClick={handleBulkDelete} className="font-medium text-red-600 hover:underline">Delete</button>
          <button onClick={() => setSelected(new Set())} className="ml-auto text-slate-400 hover:text-slate-600">✕ Clear</button>
        </div>
      )}

 {/* Table */}
   <div className="overflow-x-auto rounded-lg border border-slate-200">
        <table className="min-w-full divide-y divide-slate-200 text-sm">
          <thead className="bg-slate-50">
<tr>
              <th className="w-10 px-4 py-3 text-center">
                <input type="checkbox" checked={allSelected} onChange={toggleAll} className="h-4 w-4 rounded border-slate-300 text-brand-600" />
     </th>
      <th className="px-6 py-3 text-left font-semibold text-slate-700 text-sm">Title / Slug</th>
              <th className="px-4 py-3 text-left font-semibold text-slate-700 text-sm">Status</th>
   <th className="px-4 py-3 text-left font-semibold text-slate-700 text-sm">Locale</th>
        <th className="px-4 py-3 text-left font-semibold text-slate-700 text-sm">Author</th>
              <th className="px-4 py-3 text-left font-semibold text-slate-700 text-sm">Ver.</th>
   <th className="px-4 py-3 text-left font-semibold text-slate-700 text-sm">Updated</th>
              <th className="px-4 py-3" />
            </tr>
          </thead>
          <tbody className="divide-y divide-slate-100 bg-white">
   {isLoading
         ? Array.from({ length: 5 }).map((_, i) => (
       <tr key={i}>
      {Array.from({ length: 8 }).map((__, j) => (
           <td key={j} className="px-4 py-3">
                  <div className="h-4 w-full animate-pulse rounded bg-slate-100" />
                    </td>
    ))}
                </tr>
   ))
      : items.length === 0
    ? (
     <tr>
         <td colSpan={8} className="px-4 py-12 text-center text-slate-400">
   No entries found.{' '}
        <button
onClick={() => navigate(`/entries/new?contentTypeId=${contentTypeId}`)}
     className="text-brand-600 hover:underline"
              >
   Create the first one
    </button>.
     </td>
 </tr>
          )
                : items.map((entry) => (
              <EntryRow
              key={entry.id}
  entry={entry}
      selected={selected.has(entry.id)}
  onToggle={() => toggleOne(entry.id)}
  onEdit={() => navigate(`/entries/${entry.id}/edit`)}
     onDelete={() => { if (confirm('Delete this entry?')) deleteMutation.mutate(entry.id); }}
               />
    ))}
      </tbody>
        </table>
      </div>

      {/* Pagination */}
      {data && data.totalPages > 1 && (
      <div className="flex items-center justify-between text-sm text-slate-500">
          <span>Showing {(page - 1) * 10 + 1}–{Math.min(page * 10, data.totalCount)} of {data.totalCount} entries</span>
          <div className="flex items-center gap-1">
       <button onClick={() => setPage((p) => p - 1)} disabled={page === 1} className="btn-secondary text-xs disabled:opacity-40">← Prev</button>
    {Array.from({ length: Math.min(data.totalPages, 5) }).map((_, i) => {
    const p = i + 1;
         return (
                <button key={p} onClick={() => setPage(p)} className={`rounded px-3 py-1 text-xs font-medium ${page === p ? 'bg-brand-600 text-white' : 'hover:bg-slate-100 text-slate-600'}`}>{p}</button>
         );
            })}
            {data.totalPages > 5 && <span className="px-1">…</span>}
        {data.totalPages > 5 && (
           <button onClick={() => setPage(data.totalPages)} className="rounded px-3 py-1 text-xs font-medium hover:bg-slate-100 text-slate-600">{data.totalPages}</button>
            )}
     <button onClick={() => setPage((p) => p + 1)} disabled={page === data.totalPages} className="btn-secondary text-xs disabled:opacity-40">Next →</button>
          </div>
        <div className="flex items-center gap-2">
       <span>Per page:</span>
            <select className="form-input text-xs py-1">
         <option>10</option><option>25</option><option>50</option>
            </select>
       </div>
        </div>
      )}
    </div>
  );
}
