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

// ─── Constants ────────────────────────────────────────────────────────────────

const STATUS_BADGE: Record<EntryStatus, string> = {
    Draft: 'badge-slate',
    PendingReview: 'badge-amber',
    Approved: 'badge-blue',
    Scheduled: 'badge-brand',
    Published: 'badge-green',
    Unpublished: 'badge-slate',
    Archived: 'badge-red',
};

const STATUS_OPTIONS: EntryStatus[] = [
    'Draft', 'PendingReview', 'Approved', 'Published', 'Unpublished', 'Archived', 'Scheduled',
];

const STATUS_LABELS: Record<EntryStatus, string> = {
    Draft: 'Draft',
    PendingReview: 'Pending Review',
    Approved: 'Approved',
    Published: 'Published',
    Unpublished: 'Unpublished',
    Archived: 'Archived',
    Scheduled: 'Scheduled',
};

const LOCALES = ['en', 'en-US', 'de', 'fr', 'es', 'pt', 'ja', 'zh'];
const PAGE_SIZE_OPTIONS = [10, 25, 50, 100];

// ─── Bulk Actions Bar ─────────────────────────────────────────────────────────

function BulkActionsBar({
    count,
    onPublish,
    onUnpublish,
    onExport,
    onDelete,
    onClear,
}: {
    count: number;
    onPublish: () => void;
    onUnpublish: () => void;
    onExport: () => void;
    onDelete: () => void;
    onClear: () => void;
}) {
    return (
        <div className="flex items-center gap-3 rounded-lg border border-brand-200 bg-brand-50 px-4 py-2 text-sm">
            <span className="font-medium text-brand-800">{count} selected</span>
            <div className="mx-1 h-4 w-px bg-brand-200" />
            <button onClick={onPublish} className="text-green-700 hover:underline">Publish</button>
            <button onClick={onUnpublish} className="text-slate-700 hover:underline">Unpublish</button>
            <button onClick={onExport} className="text-brand-700 hover:underline">Export</button>
            <button onClick={onDelete} className="text-red-700 hover:underline">Delete</button>
            <button onClick={onClear} className="ml-auto text-slate-500 hover:text-slate-700">✕</button>
        </div>
    );
}

// ─── Page ─────────────────────────────────────────────────────────────────────

export default function EntriesPage() {
    const qc = useQueryClient();
    const { selectedSiteId, selectedSite, isLoading: siteLoading } = useSite();
    const siteId = selectedSiteId ?? '';

    const [search, setSearch] = useState('');
    const [status, setStatus] = useState<EntryStatus | ''>('');
    const [contentTypeId, setContentTypeId] = useState('');
    const [locale, setLocale] = useState('');
    const [page, setPage] = useState(1);
    const [pageSize, setPageSize] = useState(20);
    const [selectedIds, setSelectedIds] = useState<Set<string>>(new Set());

    const { data: contentTypes } = useQuery({
        queryKey: ['content-types'],
        queryFn: () => contentTypesApi.list({ pageSize: 100 }),
    });

    const { data, isLoading } = useQuery({
        queryKey: ['entries', { siteId, search, status, contentTypeId, locale, page, pageSize }],
        queryFn: () =>
            entriesApi.list({
                siteId: siteId || undefined,
                search: search || undefined,
                status: status || undefined,
                contentTypeId: contentTypeId || undefined,
                locale: locale || undefined,
                pageNumber: page,
                pageSize,
            }),
        enabled: !!siteId,
    });

    const publishMutation = useMutation({
        mutationFn: (id: string) => entriesApi.publish(id),
        onSuccess: () => { toast.success('Published.'); void qc.invalidateQueries({ queryKey: ['entries'] }); },
        onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Failed.'),
    });

    const unpublishMutation = useMutation({
        mutationFn: (id: string) => entriesApi.unpublish(id),
        onSuccess: () => { toast.success('Unpublished.'); void qc.invalidateQueries({ queryKey: ['entries'] }); },
        onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Failed.'),
    });

    const deleteMutation = useMutation({
        mutationFn: (id: string) => entriesApi.delete(id),
        onSuccess: () => { toast.success('Entry deleted.'); void qc.invalidateQueries({ queryKey: ['entries'] }); },
        onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Delete failed.'),
    });

    const items = data?.items ?? [];
    const allSelected = items.length > 0 && items.every((e) => selectedIds.has(e.id));

    function toggleAll() {
        if (allSelected) {
            setSelectedIds(new Set());
        } else {
            setSelectedIds(new Set(items.map((e) => e.id)));
        }
    }

    function toggleOne(id: string) {
        setSelectedIds((prev) => {
            const next = new Set(prev);
            next.has(id) ? next.delete(id) : next.add(id);
            return next;
        });
    }

    async function handleBulkPublish() {
        for (const id of selectedIds) await entriesApi.publish(id).catch(() => null);
        toast.success(`Published ${selectedIds.size} entries.`);
        setSelectedIds(new Set());
        void qc.invalidateQueries({ queryKey: ['entries'] });
    }

    async function handleBulkUnpublish() {
        for (const id of selectedIds) await entriesApi.unpublish(id).catch(() => null);
        toast.success(`Unpublished ${selectedIds.size} entries.`);
        setSelectedIds(new Set());
        void qc.invalidateQueries({ queryKey: ['entries'] });
    }

    async function handleBulkDelete() {
        if (!confirm(`Delete ${selectedIds.size} entries? This cannot be undone.`)) return;
        for (const id of selectedIds) await entriesApi.delete(id).catch(() => null);
        toast.success(`Deleted ${selectedIds.size} entries.`);
        setSelectedIds(new Set());
        void qc.invalidateQueries({ queryKey: ['entries'] });
    }

    function handleExport() {
        const url = `/api/v1/entries/export?siteId=${siteId}&format=Json`;
        window.open(url, '_blank');
    }

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
                <p className="text-sm font-medium text-slate-500">No site selected. Choose a site from the top bar.</p>
            </div>
        );
    }

    return (
        <div className="space-y-4">
            {/* Header */}
            <div className="flex items-center justify-between">
                <div>
                    <h1 className="text-2xl font-bold text-slate-900">Entries</h1>
                    <p className="mt-1 text-sm text-slate-500">
                        Content for <span className="font-medium text-slate-700">{selectedSite?.name}</span>
                    </p>
                </div>
                <div className="flex items-center gap-2">
                    <button onClick={handleExport} className="btn-secondary">Export</button>
                    <Link to="/entries/new" className="btn-primary">
                        <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
                        </svg>
                        New Entry
                    </Link>
                </div>
            </div>

            {/* Filters */}
            <div className="flex flex-wrap gap-2">
                <input
                    type="search"
                    placeholder="Search entries…"
                    value={search}
                    onChange={(e) => { setSearch(e.target.value); setPage(1); }}
                    className="form-input w-56"
                />
                <select value={contentTypeId} onChange={(e) => { setContentTypeId(e.target.value); setPage(1); }} className="form-input w-44">
                    <option value="">All content types</option>
                    {contentTypes?.items.map((ct) => (
                        <option key={ct.id} value={ct.id}>{ct.displayName}</option>
                    ))}
                </select>
                <select value={status} onChange={(e) => { setStatus(e.target.value as EntryStatus | ''); setPage(1); }} className="form-input w-40">
                    <option value="">All statuses</option>
                    {STATUS_OPTIONS.map((s) => <option key={s} value={s}>{STATUS_LABELS[s]}</option>)}
                </select>
                <select value={locale} onChange={(e) => { setLocale(e.target.value); setPage(1); }} className="form-input w-32">
                    <option value="">All locales</option>
                    {LOCALES.map((l) => <option key={l} value={l}>{l}</option>)}
                </select>
                <div className="ml-auto flex items-center gap-2">
                    <span className="text-xs text-slate-500">Per page:</span>
                    <select value={pageSize} onChange={(e) => { setPageSize(Number(e.target.value)); setPage(1); }} className="form-input w-20">
                        {PAGE_SIZE_OPTIONS.map((n) => <option key={n} value={n}>{n}</option>)}
                    </select>
                </div>
            </div>

            {/* Bulk bar */}
            {selectedIds.size > 0 && (
                <BulkActionsBar
                    count={selectedIds.size}
                    onPublish={handleBulkPublish}
                    onUnpublish={handleBulkUnpublish}
                    onExport={handleExport}
                    onDelete={handleBulkDelete}
                    onClear={() => setSelectedIds(new Set())}
                />
            )}

            {/* Table */}
            <div className="card overflow-hidden p-0">
                {isLoading ? (
                    <div className="space-y-px">
                        {Array.from({ length: 5 }).map((_, i) => <div key={i} className="h-14 animate-pulse bg-slate-50" />)}
                    </div>
                ) : items.length === 0 ? (
                    <div className="flex flex-col items-center justify-center gap-3 py-16 text-center">
                        <p className="text-sm font-medium text-slate-500">No entries found.</p>
                        <Link to="/entries/new" className="btn-primary">Create your first entry</Link>
                    </div>
                ) : (
                    <table className="w-full text-sm">
                        <thead className="border-b border-slate-100 bg-slate-50">
                            <tr>
                                <th className="w-10 px-4 py-3">
                                    <input type="checkbox" checked={allSelected} onChange={toggleAll} className="h-4 w-4 rounded border-slate-300 text-brand-600" />
                                </th>
                                <th className="px-6 py-3 text-left font-semibold text-slate-700">Title / Slug</th>
                                <th className="px-6 py-3 text-left font-semibold text-slate-700">Content Type</th>
                                <th className="px-4 py-3 text-left font-semibold text-slate-700">Locale</th>
                                <th className="px-4 py-3 text-left font-semibold text-slate-700">Ver.</th>
                                <th className="px-6 py-3 text-left font-semibold text-slate-700">Status</th>
                                <th className="px-6 py-3 text-left font-semibold text-slate-700">Author</th>
                                <th className="px-6 py-3 text-left font-semibold text-slate-700">Updated</th>
                                <th className="px-6 py-3" />
                            </tr>
                        </thead>
                        <tbody className="divide-y divide-slate-100">
                            {items.map((entry: EntryListItem) => (
                                <tr key={entry.id} className={`hover:bg-slate-50 ${selectedIds.has(entry.id) ? 'bg-brand-50/40' : ''}`}>
                                    <td className="px-4 py-4">
                                        <input
                                            type="checkbox"
                                            checked={selectedIds.has(entry.id)}
                                            onChange={() => toggleOne(entry.id)}
                                            className="h-4 w-4 rounded border-slate-300 text-brand-600"
                                        />
                                    </td>
                                    <td className="px-6 py-4">
                                        <p className="font-medium text-slate-900">{entry.title ?? entry.slug}</p>
                                        <p className="font-mono text-xs text-slate-400">{entry.slug}</p>
                                    </td>
                                    <td className="px-6 py-4 text-slate-500">{entry.contentTypeName ?? '—'}</td>
                                    <td className="px-4 py-4 text-xs text-slate-500">{entry.locale}</td>
                                    <td className="px-4 py-4 text-xs text-slate-400">v{entry.currentVersionNumber}</td>
                                    <td className="px-6 py-4">
                                        <span className={STATUS_BADGE[entry.status] ?? 'badge-slate'}>{STATUS_LABELS[entry.status] ?? entry.status}</span>
                                    </td>
                                    <td className="px-6 py-4 text-slate-500">{entry.authorName ?? '—'}</td>
                                    <td className="px-6 py-4 text-slate-400 whitespace-nowrap">
                                        {formatDistanceToNow(new Date(entry.updatedAt), { addSuffix: true })}
                                    </td>
                                    <td className="px-6 py-4 text-right">
                                        <div className="flex items-center justify-end gap-2">
                                            <Link to={`/entries/${entry.id}/edit`} className="rounded px-2 py-1 text-xs font-medium text-brand-600 hover:bg-brand-50">Edit</Link>
                                            {entry.status === 'Draft' && (
                                                <button onClick={() => publishMutation.mutate(entry.id)} className="rounded px-2 py-1 text-xs font-medium text-green-600 hover:bg-green-50">Publish</button>
                                            )}
                                            {entry.status === 'Published' && (
                                                <button onClick={() => unpublishMutation.mutate(entry.id)} className="rounded px-2 py-1 text-xs font-medium text-amber-600 hover:bg-amber-50">Unpublish</button>
                                            )}
                                            <button
                                                onClick={() => { if (confirm(`Delete "${entry.title ?? entry.slug}"?`)) deleteMutation.mutate(entry.id); }}
                                                className="rounded px-2 py-1 text-xs font-medium text-red-600 hover:bg-red-50"
                                            >Delete</button>
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
                        Showing {(page - 1) * pageSize + 1}–{Math.min(page * pageSize, data.totalCount)} of {data.totalCount}
                    </p>
                    <div className="flex gap-2">
                        <button onClick={() => setPage((p) => p - 1)} disabled={page === 1} className="btn-secondary">Previous</button>
                        <button onClick={() => setPage((p) => p + 1)} disabled={page === data.totalPages} className="btn-secondary">Next</button>
                    </div>
                </div>
            )}
        </div>
    );
}
