import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { entriesApi } from '@/api/entries';
import { contentTypesApi } from '@/api/contentTypes';
import { useSite } from '@/contexts/SiteContext';
import type { EntryListItem, EntryStatus } from '@/types';
import { formatDistanceToNow } from 'date-fns';
import toast from 'react-hot-toast';
import { ApiError } from '@/api/client';

const STATUS_BADGE: Record<EntryStatus, string> = {
  Draft: 'badge-slate',
  Review: 'badge-amber',
  Scheduled: 'badge-brand',
  Published: 'badge-green',
  Archived: 'badge-red',
};

export default function EntriesPage() {
  const qc = useQueryClient();
  const { selectedSiteId, selectedSite, isLoading: siteLoading } = useSite();
  const siteId = selectedSiteId ?? '';

  const [search, setSearch] = useState('');
  const [status, setStatus] = useState<EntryStatus | ''>('');
  const [contentTypeId, setContentTypeId] = useState('');
  const [page, setPage] = useState(1);

  const { data: contentTypes } = useQuery({
    queryKey: ['content-types'],
    queryFn: () => contentTypesApi.list({ pageSize: 100 }),
  });

  const { data, isLoading } = useQuery({
    queryKey: ['entries', { siteId, search, status, contentTypeId, page }],
    queryFn: () =>
      entriesApi.list({
        siteId: siteId || undefined,
        search: search || undefined,
        status: status || undefined,
        contentTypeId: contentTypeId || undefined,
        pageNumber: page,
        pageSize: 20,
      }),
    enabled: !!siteId,
  });

  const publishMutation = useMutation({
    mutationFn: (id: string) => entriesApi.publish(id),
    onSuccess: () => {
      toast.success('Entry published.');
      void qc.invalidateQueries({ queryKey: ['entries'] });
    },
    onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Publish failed.'),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => entriesApi.delete(id),
    onSuccess: () => {
      toast.success('Entry deleted.');
      void qc.invalidateQueries({ queryKey: ['entries'] });
    },
    onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Delete failed.'),
  });

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
          <path
            strokeLinecap="round"
            strokeLinejoin="round"
            strokeWidth={1.5}
            d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z"
          />
        </svg>
        <p className="text-sm font-medium text-slate-500">No site selected. Choose a site from the top bar.</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">Entries</h1>
          <p className="mt-1 text-sm text-slate-500">
            Content entries for <span className="font-medium text-slate-700">{selectedSite?.name}</span>.
          </p>
        </div>
        <Link to="/entries/new" className="btn-primary">
          <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
          </svg>
          New Entry
        </Link>
      </div>

      {/* Filters */}
      <div className="flex flex-wrap gap-3">
        <input
          type="search"
          placeholder="Search entries…"
          value={search}
          onChange={(e) => { setSearch(e.target.value); setPage(1); }}
          className="form-input w-64"
        />
        <select
          value={contentTypeId}
          onChange={(e) => { setContentTypeId(e.target.value); setPage(1); }}
          className="form-input w-48"
        >
          <option value="">All content types</option>
          {contentTypes?.items.map((ct) => (
            <option key={ct.id} value={ct.id}>{ct.displayName}</option>
          ))}
        </select>
        <select
          value={status}
          onChange={(e) => { setStatus(e.target.value as EntryStatus | ''); setPage(1); }}
          className="form-input w-40"
        >
          <option value="">All statuses</option>
          {(['Draft', 'Review', 'Scheduled', 'Published', 'Archived'] as EntryStatus[]).map((s) => (
            <option key={s} value={s}>{s}</option>
          ))}
        </select>
      </div>

      {/* Table */}
      <div className="card overflow-hidden p-0">
        {isLoading ? (
          <div className="space-y-px">
            {Array.from({ length: 5 }).map((_, i) => <div key={i} className="h-14 animate-pulse bg-slate-50" />)}
          </div>
        ) : data?.items.length === 0 ? (
          <div className="flex flex-col items-center justify-center gap-3 py-16 text-center">
            <p className="text-sm font-medium text-slate-500">No entries found.</p>
            <Link to="/entries/new" className="btn-primary">Create your first entry</Link>
          </div>
        ) : (
          <table className="w-full text-sm">
            <thead className="border-b border-slate-100 bg-slate-50">
              <tr>
                <th className="px-6 py-3 text-left font-semibold text-slate-700">Title / Slug</th>
                <th className="px-6 py-3 text-left font-semibold text-slate-700">Content Type</th>
                <th className="px-6 py-3 text-left font-semibold text-slate-700">Status</th>
                <th className="px-6 py-3 text-left font-semibold text-slate-700">Author</th>
                <th className="px-6 py-3 text-left font-semibold text-slate-700">Updated</th>
                <th className="px-6 py-3" />
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {data?.items.map((entry: EntryListItem) => (
                <tr key={entry.id} className="hover:bg-slate-50">
                  <td className="px-6 py-4">
                    <p className="font-medium text-slate-900">{entry.title}</p>
                    <p className="text-xs text-slate-400 font-mono">{entry.slug}</p>
                  </td>
                  <td className="px-6 py-4 text-slate-500">{entry.contentTypeName}</td>
                  <td className="px-6 py-4">
                    <span className={STATUS_BADGE[entry.status]}>{entry.status}</span>
                  </td>
                  <td className="px-6 py-4 text-slate-500">{entry.authorName}</td>
                  <td className="px-6 py-4 text-slate-400">
                    {formatDistanceToNow(new Date(entry.updatedAt), { addSuffix: true })}
                  </td>
                  <td className="px-6 py-4 text-right">
                    <div className="flex items-center justify-end gap-2">
                      <Link
                        to={`/entries/${entry.id}/edit`}
                        className="rounded px-2 py-1 text-xs font-medium text-brand-600 hover:bg-brand-50"
                      >
                        Edit
                      </Link>
                      {entry.status === 'Draft' && (
                        <button
                          onClick={() => publishMutation.mutate(entry.id)}
                          className="rounded px-2 py-1 text-xs font-medium text-green-600 hover:bg-green-50"
                        >
                          Publish
                        </button>
                      )}
                      <button
                        onClick={() => {
                          if (confirm(`Delete "${entry.title}"?`)) {
                            deleteMutation.mutate(entry.id);
                          }
                        }}
                        className="rounded px-2 py-1 text-xs font-medium text-red-600 hover:bg-red-50"
                      >
                        Delete
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>

      {/* Pagination */}
      {data && data.totalPages > 1 && (
        <div className="flex items-center justify-between">
          <p className="text-sm text-slate-500">
            Showing {(page - 1) * 20 + 1}–{Math.min(page * 20, data.totalCount)} of {data.totalCount}
          </p>
          <div className="flex gap-2">
            <button onClick={() => setPage((p) => p - 1)} disabled={page === 1} className="btn-secondary">
              Previous
            </button>
            <button onClick={() => setPage((p) => p + 1)} disabled={page === data.totalPages} className="btn-secondary">
              Next
            </button>
          </div>
        </div>
      )}
    </div>
  );
}
