import { useState, useCallback } from 'react';
import { useDropzone } from 'react-dropzone';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { mediaApi } from '@/api/media';
import type { MediaAsset } from '@/types';
import { FolderSidebar } from './FolderSidebar';
import { AssetDetail } from './AssetDetail';
import type { UploadItem } from './AssetDetail';
import { BulkToolbar } from './BulkToolbar';
import { ScanStatusBadge } from './ScanStatusBadge';

// ─── Page ─────────────────────────────────────────────────────────────────────

export default function MediaPage() {
    const qc = useQueryClient();

    const [activeFolderId, setActiveFolderId] = useState<string | null>(null);
    const [breadcrumb, setBreadcrumb] = useState<{ id: string; name: string }[]>([]);
    const [view, setView] = useState<'grid' | 'list'>('grid');
    const [search, setSearch] = useState('');
    const [page, setPage] = useState(1);
    const [selected, setSelected] = useState<MediaAsset | null>(null);
    const [uploads, setUploads] = useState<UploadItem[]>([]);
    const [checkedIds, setCheckedIds] = useState<Set<string>>(new Set());

    const { data: subFolders = [] } = useQuery({
        queryKey: ['media-folders', activeFolderId],
        queryFn: () => mediaApi.listFolders(activeFolderId ?? undefined),
    });

    const { data, isLoading } = useQuery({
        queryKey: ['media', { search, page, folderId: activeFolderId }],
        queryFn: () =>
            mediaApi.list({
                search: search || undefined,
                page,
                pageSize: 30,
                folderId: activeFolderId ?? undefined,
            }),
    });

    function navigateToFolder(id: string | null, name?: string) {
        if (id === null) {
            setActiveFolderId(null);
            setBreadcrumb([]);
        } else {
            setActiveFolderId(id);
            setBreadcrumb((prev) => {
                const idx = prev.findIndex((b) => b.id === id);
                if (idx !== -1) return prev.slice(0, idx + 1);
                return [...prev, { id, name: name ?? id }];
            });
        }
        setPage(1);
        setSearch('');
        setCheckedIds(new Set());
        setSelected(null);
    }

    const bulkDeleteMutation = useMutation({
        mutationFn: () => mediaApi.bulkDelete([...checkedIds]),
        onSuccess: () => {
            toast.success(`${checkedIds.size} asset(s) permanently deleted.`);
            setCheckedIds(new Set());
            void qc.invalidateQueries({ queryKey: ['media'] });
        },
        onError: () => toast.error('Bulk delete failed.'),
    });

    const bulkMoveMutation = useMutation({
        mutationFn: (targetFolderId: string | null) => mediaApi.bulkMove([...checkedIds], targetFolderId),
        onSuccess: () => {
            toast.success(`${checkedIds.size} asset(s) moved.`);
            setCheckedIds(new Set());
            void qc.invalidateQueries({ queryKey: ['media'] });
            void qc.invalidateQueries({ queryKey: ['media-folders'] });
        },
        onError: () => toast.error('Move failed.'),
    });

    const onDrop = useCallback(
        (acceptedFiles: File[]) => {
            const items: UploadItem[] = acceptedFiles.map((f) => ({ file: f, progress: 0, status: 'uploading' }));
            setUploads((prev) => [...prev, ...items]);
            acceptedFiles.forEach((file, i) => {
                mediaApi
                    .upload(file, { folderId: activeFolderId ?? undefined }, (pct) => {
                        setUploads((prev) =>
                            prev.map((u, idx) => (idx === uploads.length + i ? { ...u, progress: pct } : u)),
                        );
                    })
                    .then(() => {
                        setUploads((prev) =>
                            prev.map((u, idx) => (idx === uploads.length + i ? { ...u, status: 'done' } : u)),
                        );
                        void qc.invalidateQueries({ queryKey: ['media'] });
                        void qc.invalidateQueries({ queryKey: ['media-folders'] });
                        toast.success(`${file.name} uploaded — virus scan in progress…`);
                    })
                    .catch(() => {
                        setUploads((prev) =>
                            prev.map((u, idx) => (idx === uploads.length + i ? { ...u, status: 'error' } : u)),
                        );
                        toast.error(`Failed to upload ${file.name}.`);
                    });
            });
        },
        [qc, uploads.length, activeFolderId],
    );

    const { getRootProps, getInputProps, isDragActive } = useDropzone({
        onDrop,
        accept: { 'image/*': [], 'video/*': [], 'application/pdf': [] },
        maxSize: 2 * 1024 * 1024 * 1024,
        noClick: false,
    });

    const toggleCheck = (id: string) =>
        setCheckedIds((prev) => {
            const next = new Set(prev);
            if (next.has(id)) next.delete(id); else next.add(id);
            return next;
        });

    return (
        <div className="-m-6 flex h-full min-h-0">
            {/* Left sidebar */}
            <div className="flex-shrink-0 border-r border-slate-200 bg-white overflow-y-auto py-4">
                <FolderSidebar
                    activeFolderId={activeFolderId}
                    onSelect={(id, name) => navigateToFolder(id, name)}
                />
            </div>

            {/* Main area */}
            <div className="flex min-w-0 flex-1 flex-col overflow-hidden">
                {/* Topbar */}
                <div className="flex items-center gap-3 border-b border-slate-200 bg-white px-5 py-3">
                    {/* Breadcrumb */}
                    <nav className="flex min-w-0 flex-1 items-center gap-1 text-sm">
                        <button
                            onClick={() => navigateToFolder(null)}
                            className="text-slate-500 hover:text-brand-600 flex-shrink-0"
                        >All Assets</button>
                        {breadcrumb.map((crumb) => (
                            <span key={crumb.id} className="flex items-center gap-1 min-w-0">
                                <span className="text-slate-300">/</span>
                                <button
                                    onClick={() => navigateToFolder(crumb.id, crumb.name)}
                                    className="truncate text-slate-700 hover:text-brand-600 font-medium"
                                >{crumb.name}</button>
                            </span>
                        ))}
                    </nav>

                    {/* Search */}
                    <input
                        type="search"
                        placeholder={activeFolderId ? 'Search in folder…' : 'Search assets…'}
                        value={search}
                        onChange={(e) => { setSearch(e.target.value); setPage(1); }}
                        className="form-input w-48 text-sm"
                    />

                    {/* Upload button */}
                    <div {...getRootProps()} className="relative">
                        <input {...getInputProps()} />
                        <button className="btn-primary flex items-center gap-2 text-sm">
                            <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12" />
                            </svg>
                            Upload Files
                        </button>
                    </div>

                    {/* View toggle */}
                    <div className="flex rounded-lg border border-slate-200">
                        <button
                            onClick={() => setView('grid')}
                            className={`rounded-l-lg px-2.5 py-1.5 ${view === 'grid' ? 'bg-brand-50 text-brand-600' : 'text-slate-400 hover:text-slate-600'}`}
                            aria-label="Grid view"
                        >
                            <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2V6zM14 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2V6zM4 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2v-2zM14 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2v-2z" />
                            </svg>
                        </button>
                        <button
                            onClick={() => setView('list')}
                            className={`rounded-r-lg px-2.5 py-1.5 ${view === 'list' ? 'bg-brand-50 text-brand-600' : 'text-slate-400 hover:text-slate-600'}`}
                            aria-label="List view"
                        >
                            <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 12h16M4 18h16" />
                            </svg>
                        </button>
                    </div>
                </div>

                {/* Upload progress bar */}
                {uploads.some((u) => u.status === 'uploading') && (
                    <div className="border-b border-slate-100 bg-white px-5 py-2 space-y-1.5">
                        {uploads.filter((u) => u.status === 'uploading').map((u, i) => (
                            <div key={i} className="flex items-center gap-3">
                                <span className="min-w-0 truncate text-xs text-slate-600">{u.file.name}</span>
                                <div className="flex-1 h-1 rounded-full bg-slate-200">
                                    <div className="h-1 rounded-full bg-brand-500 transition-all" style={{ width: `${u.progress}%` }} />
                                </div>
                                <span className="text-xs text-slate-400 w-8 text-right">{u.progress}%</span>
                            </div>
                        ))}
                    </div>
                )}

                {/* Drag overlay */}
                {isDragActive && (
                    <div className="absolute inset-0 z-30 flex items-center justify-center rounded-xl bg-brand-50/90 border-2 border-dashed border-brand-400">
                        <p className="text-lg font-semibold text-brand-700">Drop files to upload{activeFolderId ? ' into this folder' : ''}…</p>
                    </div>
                )}

                {/* Scrollable content */}
                <div className="flex-1 overflow-y-auto px-5 py-5 space-y-6">
                    {/* Subfolders */}
                    {subFolders.length > 0 && !search && (
                        <section>
                            <h2 className="mb-3 text-xs font-semibold uppercase tracking-wide text-slate-400">
                                Folders ({subFolders.length})
                            </h2>
                            <div className="grid grid-cols-2 gap-3 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-5">
                                {subFolders.map((folder) => (
                                    <button
                                        key={folder.id}
                                        onClick={() => navigateToFolder(folder.id, folder.name)}
                                        className="flex items-center gap-3 rounded-xl border border-slate-200 bg-white px-4 py-3 text-left hover:border-brand-300 hover:bg-brand-50 transition-colors"
                                    >
                                        <svg className="h-8 w-8 flex-shrink-0 text-amber-400" fill="currentColor" viewBox="0 0 20 20">
                                            <path d="M2 6a2 2 0 012-2h5l2 2h5a2 2 0 012 2v6a2 2 0 01-2 2H4a2 2 0 01-2-2V6z" />
                                        </svg>
                                        <div className="min-w-0">
                                            <p className="truncate text-sm font-medium text-slate-800">{folder.name}</p>
                                            <p className="text-xs text-slate-400">{folder.assetCount} files</p>
                                        </div>
                                    </button>
                                ))}
                            </div>
                        </section>
                    )}

                    {/* Files */}
                    <section>
                        {subFolders.length > 0 && !search && (
                            <h2 className="mb-3 text-xs font-semibold uppercase tracking-wide text-slate-400">
                                Files ({data?.totalCount ?? 0})
                            </h2>
                        )}

                        {isLoading ? (
                            <div className="grid grid-cols-4 gap-3">
                                {Array.from({ length: 12 }).map((_, i) => (
                                    <div key={i} className="aspect-[4/3] animate-pulse rounded-lg bg-slate-200" />
                                ))}
                            </div>
                        ) : !data?.items.length ? (
                            <div className="flex flex-col items-center justify-center rounded-xl border-2 border-dashed border-slate-200 py-16 text-slate-400">
                                <svg className="h-10 w-10 mb-3" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
                                </svg>
                                <p className="text-sm font-medium">{search ? 'No results' : 'No files here yet'}</p>
                                <p className="text-xs mt-1">Drop files above or click Upload Files to add assets</p>
                            </div>
                        ) : view === 'grid' ? (
                            <div className="grid grid-cols-3 gap-3 sm:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6">
                                {data.items.map((asset) => (
                                    <div key={asset.id} className="group relative">
                                        <input
                                            type="checkbox"
                                            checked={checkedIds.has(asset.id)}
                                            onChange={() => toggleCheck(asset.id)}
                                            onClick={(e) => e.stopPropagation()}
                                            className="absolute top-1.5 left-1.5 z-10 h-3.5 w-3.5 rounded border-slate-300 text-brand-600 opacity-0 group-hover:opacity-100 checked:opacity-100"
                                        />
                                        <button
                                            onClick={() => setSelected(asset)}
                                            className={`relative w-full overflow-hidden rounded-lg border bg-slate-100 transition-colors aspect-[4/3] hover:border-brand-400 ${selected?.id === asset.id ? 'border-brand-500 ring-2 ring-brand-200' :
                                                    checkedIds.has(asset.id) ? 'border-brand-400 ring-1 ring-brand-100' : 'border-slate-200'
                                                }`}
                                        >
                                            {asset.mediaType === 'image' && asset.status === 'Available' ? (
                                                <img
                                                    src={asset.thumbnailUrl ?? asset.url}
                                                    alt={asset.altText ?? asset.fileName}
                                                    className="h-full w-full object-cover"
                                                />
                                            ) : (
                                                <div className="flex h-full w-full flex-col items-center justify-center">
                                                    <span className="text-2xl">
                                                        {asset.status === 'PendingScan' ? '🔍'
                                                            : asset.status === 'Quarantined' ? '🚫'
                                                                : asset.mediaType === 'video' ? '🎬'
                                                                    : asset.mediaType === 'audio' ? '🎵' : '📄'}
                                                    </span>
                                                </div>
                                            )}
                                            <span className="absolute bottom-1 right-1 rounded bg-black/50 px-1 py-0.5 text-[10px] font-semibold uppercase text-white">
                                                {asset.mediaType}
                                            </span>
                                        </button>
                                        <p className="mt-0.5 truncate px-0.5 text-xs text-slate-500">{asset.fileName}</p>
                                    </div>
                                ))}
                            </div>
                        ) : (
                            <div className="rounded-xl border border-slate-200 overflow-hidden">
                                <table className="w-full text-sm">
                                    <thead className="border-b border-slate-100 bg-slate-50">
                                        <tr>
                                            <th className="px-4 py-2.5 w-8" />
                                            <th className="px-4 py-2.5 text-left text-xs font-semibold text-slate-500 uppercase tracking-wide">File</th>
                                            <th className="px-4 py-2.5 text-left text-xs font-semibold text-slate-500 uppercase tracking-wide">Status</th>
                                            <th className="px-4 py-2.5 text-left text-xs font-semibold text-slate-500 uppercase tracking-wide">Type</th>
                                            <th className="px-4 py-2.5 text-left text-xs font-semibold text-slate-500 uppercase tracking-wide">Size</th>
                                            <th className="px-4 py-2.5" />
                                        </tr>
                                    </thead>
                                    <tbody className="divide-y divide-slate-100">
                                        {data.items.map((asset) => (
                                            <tr
                                                key={asset.id}
                                                className={`hover:bg-slate-50 cursor-pointer ${selected?.id === asset.id ? 'bg-brand-50' : checkedIds.has(asset.id) ? 'bg-brand-50/50' : ''}`}
                                                onClick={() => setSelected(asset)}
                                            >
                                                <td className="px-4 py-2.5" onClick={(e) => e.stopPropagation()}>
                                                    <input type="checkbox" checked={checkedIds.has(asset.id)} onChange={() => toggleCheck(asset.id)} className="h-3.5 w-3.5 rounded border-slate-300 text-brand-600" />
                                                </td>
                                                <td className="px-4 py-2.5">
                                                    <div className="flex items-center gap-2.5">
                                                        {asset.mediaType === 'image' && asset.status === 'Available' ? (
                                                            <img src={asset.thumbnailUrl ?? asset.url} alt="" className="h-7 w-7 flex-shrink-0 rounded object-cover" />
                                                        ) : (
                                                            <div className="h-7 w-7 flex-shrink-0 rounded bg-slate-100 flex items-center justify-center text-base">
                                                                {asset.status === 'Quarantined' ? '🚫' : asset.mediaType === 'video' ? '🎬' : asset.mediaType === 'audio' ? '🎵' : '📄'}
                                                            </div>
                                                        )}
                                                        <span className="max-w-xs truncate font-medium text-slate-800">{asset.fileName}</span>
                                                    </div>
                                                </td>
                                                <td className="px-4 py-2.5"><ScanStatusBadge status={asset.status ?? 'Unknown'} /></td>
                                                <td className="px-4 py-2.5 text-slate-500 text-xs uppercase">{asset.contentType.split('/')[1] ?? asset.contentType}</td>
                                                <td className="px-4 py-2.5 text-slate-500 text-xs">{Math.round(asset.fileSize / 1024)} KB</td>
                                                <td className="px-4 py-2.5 text-right">
                                                    <button onClick={(e) => { e.stopPropagation(); setSelected(asset); }} className="text-xs text-brand-600 hover:underline">Details</button>
                                                </td>
                                            </tr>
                                        ))}
                                    </tbody>
                                </table>
                            </div>
                        )}

                        {/* Pagination */}
                        {data && data.totalPages > 1 && (
                            <div className="flex items-center justify-end gap-2 pt-2">
                                <button onClick={() => setPage((p) => p - 1)} disabled={page === 1} className="btn-secondary text-sm">← Prev</button>
                                <span className="text-xs text-slate-500">Page {page} of {data.totalPages}</span>
                                <button onClick={() => setPage((p) => p + 1)} disabled={page === data.totalPages} className="btn-secondary text-sm">Next →</button>
                            </div>
                        )}
                    </section>
                </div>
            </div>

            {/* Asset detail panel */}
            {selected && (
                <AssetDetail
                    asset={selected}
                    onClose={() => setSelected(null)}
                    onUpdated={() => { setSelected(null); void qc.invalidateQueries({ queryKey: ['media'] }); }}
                />
            )}

            {/* Bulk toolbar */}
            <BulkToolbar
                selectedIds={checkedIds}
                onDelete={() => { if (confirm(`Permanently delete ${checkedIds.size} asset(s)? This cannot be undone.`)) bulkDeleteMutation.mutate(); }}
                onClear={() => setCheckedIds(new Set())}
                onMoveTo={(folderId) => bulkMoveMutation.mutate(folderId)}
            />
        </div>
    );
}
