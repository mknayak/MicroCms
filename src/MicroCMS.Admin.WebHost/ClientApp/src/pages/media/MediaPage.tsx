import { useState, useCallback } from 'react';
import { useDropzone } from 'react-dropzone';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { mediaApi } from '@/api/media';
import type { MediaAsset } from '@/types';
import { ApiError } from '@/api/client';

// ─── Scan Status Badge ─────────────────────────────────────────────────────────

function ScanStatusBadge({ status }: { status: string }) {
  const variants: Record<string, string> = {
    Available:   'bg-emerald-100 text-emerald-700',
    PendingScan: 'bg-amber-100  text-amber-700',
    Uploading:   'bg-blue-100   text-blue-700',
    Quarantined: 'bg-red-100    text-red-700',
    Deleted:     'bg-slate-100  text-slate-500',
  };
  const label: Record<string, string> = {
    Available:   'Available',
    PendingScan: 'Scanning…',
    Uploading:   'Uploading',
    Quarantined: 'Quarantined',
    Deleted:     'Deleted',
  };
  const cls = variants[status] ?? 'bg-slate-100 text-slate-500';
  return (
    <span className={`inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-xs font-medium ${cls}`}>
      {status === 'PendingScan' && (
        <svg className="h-3 w-3 animate-spin" viewBox="0 0 24 24" fill="none">
          <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
          <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z" />
        </svg>
      )}
      {status === 'Quarantined' && (
        <svg className="h-3 w-3" fill="currentColor" viewBox="0 0 20 20">
          <path fillRule="evenodd" d="M8.485 2.495c.673-1.167 2.357-1.167 3.03 0l6.28 10.875c.673 1.167-.17 2.625-1.516 2.625H3.72c-1.347 0-2.189-1.458-1.515-2.625L8.485 2.495zM10 5a.75.75 0 01.75.75v3.5a.75.75 0 01-1.5 0v-3.5A.75.75 0 0110 5zm0 9a1 1 0 100-2 1 1 0 000 2z" clipRule="evenodd" />
        </svg>
      )}
      {label[status] ?? status}
    </span>
  );
}

// ─── Upload Progress ───────────────────────────────────────────────────────────

interface UploadItem {
  file: File;
  progress: number;
  status: 'uploading' | 'done' | 'error';
}

// ─── Asset Detail Panel ───────────────────────────────────────────────────────

function AssetDetail({
  asset,
  onClose,
  onUpdated,
}: {
  asset: MediaAsset;
  onClose: () => void;
  onUpdated: () => void;
}) {
  const [altText, setAltText]     = useState(asset.altText ?? '');
  const [tags, setTags]           = useState(asset.tags.join(', '));
  const [signedUrl, setSignedUrl] = useState<string | null>(null);
  const qc = useQueryClient();

  const updateMutation = useMutation({
    mutationFn: () =>
      mediaApi.update(asset.id, {
        altText: altText || undefined,
        tags: tags.split(',').map((t) => t.trim()).filter(Boolean),
      }),
    onSuccess: () => {
      toast.success('Asset updated.');
      void qc.invalidateQueries({ queryKey: ['media'] });
      onUpdated();
    },
    onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Update failed.'),
  });

  const deleteMutation = useMutation({
    mutationFn: () => mediaApi.delete(asset.id),
    onSuccess: () => {
      toast.success('Asset deleted.');
      void qc.invalidateQueries({ queryKey: ['media'] });
      onClose();
    },
    onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Delete failed.'),
  });

  const signedUrlMutation = useMutation({
    mutationFn: () => mediaApi.getSignedUrl(asset.id),
    onSuccess: (data) => {
      setSignedUrl(data.url);
      toast.success('Signed URL generated (valid 1 hr).');
    },
    onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Failed.'),
  });

  const sizeKb = Math.round(asset.fileSize / 1024);
  const isAvailable = asset.status === 'Available';

  return (
    <div className="fixed inset-y-0 right-0 z-40 flex w-80 flex-col border-l border-slate-200 bg-white shadow-xl">
      <div className="flex items-center justify-between border-b border-slate-200 px-4 py-3">
        <h3 className="truncate text-sm font-semibold text-slate-900">{asset.fileName}</h3>
        <button onClick={onClose} className="ml-2 text-slate-400 hover:text-slate-600">✕</button>
      </div>

      <div className="flex-1 overflow-y-auto p-4 space-y-4">
        {/* Scan status */}
        <div className="flex items-center gap-2">
          <span className="text-xs text-slate-500">Status:</span>
          <ScanStatusBadge status={asset.status ?? 'Unknown'} />
        </div>

        {/* Quarantine warning */}
        {asset.status === 'Quarantined' && (
          <div className="rounded-lg border border-red-200 bg-red-50 p-3 text-xs text-red-700">
            This file was quarantined by the virus scanner and cannot be delivered.
          </div>
        )}

        {/* Preview */}
        {asset.mediaType === 'image' && isAvailable && (
          <img
            src={asset.thumbnailUrl ?? asset.url}
            alt={asset.altText ?? asset.fileName}
            className="w-full rounded-lg object-cover"
          />
        )}

        {/* Metadata */}
        <div className="text-xs text-slate-500 space-y-1">
          <p><span className="font-medium">Type:</span> {asset.contentType}</p>
          <p><span className="font-medium">Size:</span> {sizeKb} KB</p>
          {asset.width && <p><span className="font-medium">Dimensions:</span> {asset.width}×{asset.height}</p>}
          <p><span className="font-medium">Uploaded by:</span> {asset.uploadedByName}</p>
        </div>

        {/* Editable fields — only for available assets */}
        {isAvailable && (
          <div className="space-y-3">
            <div>
              <label className="form-label">Alt Text</label>
              <input
                className="form-input mt-1"
                value={altText}
                onChange={(e) => setAltText(e.target.value)}
                placeholder="Describe this image…"
              />
            </div>
            <div>
              <label className="form-label">Tags (comma-separated)</label>
              <input
                className="form-input mt-1"
                value={tags}
                onChange={(e) => setTags(e.target.value)}
                placeholder="hero, banner, product"
              />
            </div>
          </div>
        )}

        {/* URL + Signed URL */}
        {isAvailable && (
          <div className="space-y-2">
            <div>
              <label className="form-label">URL</label>
              <div className="mt-1 flex gap-2">
                <input className="form-input flex-1 font-mono text-xs" readOnly value={asset.url} />
                <button
                  onClick={() => { void navigator.clipboard.writeText(asset.url); toast.success('Copied!'); }}
                  className="btn-secondary text-xs"
                >Copy</button>
              </div>
            </div>
            <div>
              <label className="form-label">Signed URL</label>
              {signedUrl ? (
                <div className="mt-1 flex gap-2">
                  <input className="form-input flex-1 font-mono text-xs" readOnly value={signedUrl} />
                  <button
                    onClick={() => { void navigator.clipboard.writeText(signedUrl); toast.success('Copied!'); }}
                    className="btn-secondary text-xs"
                  >Copy</button>
                </div>
              ) : (
                <button
                  onClick={() => signedUrlMutation.mutate()}
                  disabled={signedUrlMutation.isPending}
                  className="btn-secondary mt-1 w-full justify-center text-xs"
                >
                  {signedUrlMutation.isPending ? 'Generating…' : 'Generate Signed URL'}
                </button>
              )}
            </div>
          </div>
        )}
      </div>

      {/* Footer */}
      <div className="border-t border-slate-200 p-4 flex gap-2">
        {isAvailable && (
          <button
            onClick={() => updateMutation.mutate()}
            disabled={updateMutation.isPending}
            className="btn-primary flex-1 justify-center"
          >Save</button>
        )}
        <button
          onClick={() => { if (confirm('Delete this asset?')) deleteMutation.mutate(); }}
          className="btn-danger"
        >Delete</button>
      </div>
    </div>
  );
}

// ─── Bulk Action Toolbar ──────────────────────────────────────────────────────

function BulkToolbar({
  selectedIds,
  onDelete,
  onClear,
}: {
  selectedIds: Set<string>;
  onDelete: () => void;
  onClear: () => void;
}) {
  if (selectedIds.size === 0) return null;
  return (
    <div className="fixed bottom-6 left-1/2 -translate-x-1/2 z-50 flex items-center gap-3 rounded-xl border border-slate-200 bg-white px-4 py-3 shadow-xl">
      <span className="text-sm font-medium text-slate-700">{selectedIds.size} selected</span>
      <button onClick={onDelete} className="btn-danger text-sm">Delete</button>
      <button onClick={onClear}  className="text-xs text-slate-400 hover:text-slate-600 ml-1">✕ Clear</button>
    </div>
  );
}

// ─── Page ─────────────────────────────────────────────────────────────────────

export default function MediaPage() {
  const qc = useQueryClient();
  const [view, setView]             = useState<'grid' | 'list'>('grid');
  const [search, setSearch]         = useState('');
  const [page, setPage]             = useState(1);
  const [selected, setSelected]     = useState<MediaAsset | null>(null);
  const [uploads, setUploads]       = useState<UploadItem[]>([]);
  const [checkedIds, setCheckedIds] = useState<Set<string>>(new Set());

  const { data, isLoading } = useQuery({
    queryKey: ['media', { search, page }],
    queryFn: () => mediaApi.list({ search: search || undefined, pageNumber: page, pageSize: 24 }),
  });

  const bulkDeleteMutation = useMutation({
    mutationFn: () => mediaApi.bulkDelete([...checkedIds]),
    onSuccess: () => {
      toast.success(`${checkedIds.size} asset(s) deleted.`);
      setCheckedIds(new Set());
      void qc.invalidateQueries({ queryKey: ['media'] });
    },
    onError: () => toast.error('Bulk delete failed.'),
  });

  const onDrop = useCallback(
    (acceptedFiles: File[]) => {
      const items: UploadItem[] = acceptedFiles.map((f) => ({ file: f, progress: 0, status: 'uploading' }));
      setUploads((prev) => [...prev, ...items]);

      acceptedFiles.forEach((file, i) => {
        mediaApi
          .upload(file, {}, (pct) => {
            setUploads((prev) =>
              prev.map((u, idx) => (idx === uploads.length + i ? { ...u, progress: pct } : u)),
            );
          })
          .then(() => {
            setUploads((prev) =>
              prev.map((u, idx) => (idx === uploads.length + i ? { ...u, status: 'done' } : u)),
            );
            void qc.invalidateQueries({ queryKey: ['media'] });
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
    [qc, uploads.length],
  );

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    accept: { 'image/*': [], 'video/*': [], 'application/pdf': [] },
    maxSize: 2 * 1024 * 1024 * 1024,
  });

  const toggleCheck = (id: string) =>
    setCheckedIds((prev) => {
      const next = new Set(prev);
      if (next.has(id)) next.delete(id); else next.add(id);
      return next;
    });

  return (
    <div className="space-y-6 pb-24">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">Media Library</h1>
          <p className="mt-1 text-sm text-slate-500">Upload and manage your assets.</p>
        </div>
        <div className="flex gap-2">
          <button onClick={() => setView('grid')} className={view === 'grid' ? 'btn-primary' : 'btn-secondary'} aria-label="Grid view">
            <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2V6zM14 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2V6zM4 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2v-2zM14 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2v-2z" />
            </svg>
          </button>
          <button onClick={() => setView('list')} className={view === 'list' ? 'btn-primary' : 'btn-secondary'} aria-label="List view">
            <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 12h16M4 18h16" />
            </svg>
          </button>
        </div>
      </div>

      {/* Drop zone */}
      <div
        {...getRootProps()}
        className={`rounded-xl border-2 border-dashed p-8 text-center transition-colors ${
          isDragActive ? 'border-brand-500 bg-brand-50' : 'border-slate-300 hover:border-brand-400 hover:bg-slate-50'
        }`}
      >
        <input {...getInputProps()} />
        <svg className="mx-auto h-10 w-10 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M7 16a4 4 0 01-.88-7.903A5 5 0 1115.9 6L16 6a5 5 0 011 9.9M15 13l-3-3m0 0l-3 3m3-3v12" />
        </svg>
        <p className="mt-2 text-sm font-medium text-slate-700">
          {isDragActive ? 'Drop files here…' : 'Drag & drop files, or click to select'}
        </p>
        <p className="text-xs text-slate-400">Images, videos, PDFs up to 2 GB — virus scanned automatically</p>
      </div>

      {/* Active uploads */}
      {uploads.some((u) => u.status === 'uploading') && (
        <div className="space-y-2">
          {uploads.filter((u) => u.status === 'uploading').map((u, i) => (
            <div key={i} className="flex items-center gap-3 rounded-lg border border-slate-200 p-3">
              <div className="flex-1 min-w-0">
                <p className="truncate text-sm font-medium text-slate-700">{u.file.name}</p>
                <div className="mt-1 h-1.5 w-full rounded-full bg-slate-200">
                  <div className="h-1.5 rounded-full bg-brand-600 transition-all" style={{ width: `${u.progress}%` }} />
                </div>
              </div>
              <span className="text-xs text-slate-400">{u.progress}%</span>
            </div>
          ))}
        </div>
      )}

      {/* Search */}
      <div className="flex items-center gap-4">
        <input
          type="search"
          placeholder="Search media…"
          value={search}
          onChange={(e) => { setSearch(e.target.value); setPage(1); }}
          className="form-input w-64"
        />
        {checkedIds.size > 0 && (
          <span className="text-sm text-slate-500">{checkedIds.size} selected</span>
        )}
      </div>

      {/* Assets */}
      {isLoading ? (
        <div className="grid grid-cols-4 gap-4">
          {Array.from({ length: 12 }).map((_, i) => (
            <div key={i} className="aspect-square animate-pulse rounded-lg bg-slate-200" />
          ))}
        </div>
      ) : view === 'grid' ? (
        <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 lg:grid-cols-4 xl:grid-cols-6">
          {data?.items.map((asset) => (
            <div key={asset.id} className="relative group">
              <input
                type="checkbox"
                checked={checkedIds.has(asset.id)}
                onChange={() => toggleCheck(asset.id)}
                onClick={(e) => e.stopPropagation()}
                className="absolute top-2 left-2 z-10 h-4 w-4 rounded border-slate-300 text-brand-600 opacity-0 group-hover:opacity-100 checked:opacity-100"
              />
              <button
                onClick={() => setSelected(asset)}
                className={`w-full aspect-square overflow-hidden rounded-lg border bg-slate-100 hover:border-brand-400 transition-colors ${
                  checkedIds.has(asset.id) ? 'border-brand-400 ring-2 ring-brand-200' : 'border-slate-200'
                }`}
              >
                {asset.mediaType === 'image' && asset.status === 'Available' ? (
                  <img src={asset.thumbnailUrl ?? asset.url} alt={asset.altText ?? asset.fileName} className="h-full w-full object-cover" />
                ) : (
                  <div className="flex h-full w-full flex-col items-center justify-center gap-1">
                    <span className="text-2xl text-slate-400">
                      {asset.status === 'PendingScan' ? '🔍'
                        : asset.status === 'Quarantined' ? '🚫'
                        : asset.mediaType === 'video' ? '🎬'
                        : asset.mediaType === 'audio' ? '🎵' : '📄'}
                    </span>
                  </div>
                )}
              </button>
              {/* Status badge — shown for non-Available assets */}
              {asset.status !== 'Available' && (
                <div className="absolute bottom-6 left-1 right-1 flex justify-center">
                  <ScanStatusBadge status={asset.status ?? 'Unknown'} />
                </div>
              )}
              <p className="mt-1 truncate px-0.5 text-xs text-slate-500">{asset.fileName}</p>
            </div>
          ))}
        </div>
      ) : (
        <div className="card overflow-hidden p-0">
          <table className="w-full text-sm">
            <thead className="border-b border-slate-100 bg-slate-50">
              <tr>
                <th className="px-4 py-3 w-8" />
                <th className="px-6 py-3 text-left font-semibold text-slate-700">File</th>
                <th className="px-6 py-3 text-left font-semibold text-slate-700">Status</th>
                <th className="px-6 py-3 text-left font-semibold text-slate-700">Type</th>
                <th className="px-6 py-3 text-left font-semibold text-slate-700">Size</th>
                <th className="px-6 py-3 text-left font-semibold text-slate-700">Uploaded by</th>
                <th className="px-6 py-3" />
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {data?.items.map((asset) => (
                <tr key={asset.id} className={`hover:bg-slate-50 ${checkedIds.has(asset.id) ? 'bg-brand-50' : ''}`}>
                  <td className="px-4 py-3">
                    <input
                      type="checkbox"
                      checked={checkedIds.has(asset.id)}
                      onChange={() => toggleCheck(asset.id)}
                      className="h-4 w-4 rounded border-slate-300 text-brand-600"
                    />
                  </td>
                  <td className="px-6 py-3 flex items-center gap-3">
                    {asset.mediaType === 'image' && asset.status === 'Available' ? (
                      <img src={asset.thumbnailUrl ?? asset.url} alt="" className="h-8 w-8 rounded object-cover" />
                    ) : (
                      <div className="h-8 w-8 rounded bg-slate-100 flex items-center justify-center text-lg">
                        {asset.status === 'Quarantined' ? '🚫' : '📄'}
                      </div>
                    )}
                    <span className="font-medium text-slate-900 truncate max-w-xs">{asset.fileName}</span>
                  </td>
                  <td className="px-6 py-3"><ScanStatusBadge status={asset.status ?? 'Unknown'} /></td>
                  <td className="px-6 py-3 text-slate-500">{asset.contentType}</td>
                  <td className="px-6 py-3 text-slate-500">{Math.round(asset.fileSize / 1024)} KB</td>
                  <td className="px-6 py-3 text-slate-500">{asset.uploadedByName}</td>
                  <td className="px-6 py-3 text-right">
                    <button onClick={() => setSelected(asset)} className="text-xs text-brand-600 hover:underline">Details</button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Pagination */}
      {data && data.totalPages > 1 && (
        <div className="flex justify-end gap-2">
          <button onClick={() => setPage((p) => p - 1)} disabled={page === 1} className="btn-secondary">Previous</button>
          <button onClick={() => setPage((p) => p + 1)} disabled={page === data.totalPages} className="btn-secondary">Next</button>
        </div>
      )}

      {/* Asset detail panel */}
      {selected && (
        <AssetDetail asset={selected} onClose={() => setSelected(null)} onUpdated={() => setSelected(null)} />
      )}

      {/* Bulk action toolbar */}
      <BulkToolbar
        selectedIds={checkedIds}
        onDelete={() => { if (confirm(`Delete ${checkedIds.size} asset(s)?`)) bulkDeleteMutation.mutate(); }}
        onClear={() => setCheckedIds(new Set())}
      />
    </div>
  );
}
