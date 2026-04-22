import { useState, useCallback } from 'react';
import { useDropzone } from 'react-dropzone';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { mediaApi } from '@/api/media';
import type { MediaAsset } from '@/types';
import { ApiError } from '@/api/client';

// ─── Upload Progress ──────────────────────────────────────────────────────────

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
  const [altText, setAltText] = useState(asset.altText ?? '');
  const [tags, setTags] = useState(asset.tags.join(', '));
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

  const sizeKb = Math.round(asset.fileSize / 1024);

  return (
    <div className="fixed inset-y-0 right-0 z-40 flex w-80 flex-col border-l border-slate-200 bg-white shadow-xl">
      <div className="flex items-center justify-between border-b border-slate-200 px-4 py-3">
        <h3 className="truncate text-sm font-semibold text-slate-900">{asset.fileName}</h3>
        <button onClick={onClose} className="ml-2 text-slate-400 hover:text-slate-600">✕</button>
      </div>
      <div className="flex-1 overflow-y-auto p-4 space-y-4">
        {/* Preview */}
        {asset.mediaType === 'image' && (
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

        {/* Editable fields */}
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

        {/* URL */}
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
      </div>

      {/* Footer */}
      <div className="border-t border-slate-200 p-4 flex gap-2">
        <button
          onClick={() => updateMutation.mutate()}
          disabled={updateMutation.isPending}
          className="btn-primary flex-1 justify-center"
        >
          Save
        </button>
        <button
          onClick={() => {
            if (confirm('Delete this asset?')) deleteMutation.mutate();
          }}
          className="btn-danger"
        >
          Delete
        </button>
      </div>
    </div>
  );
}

// ─── Page ─────────────────────────────────────────────────────────────────────

export default function MediaPage() {
  const qc = useQueryClient();
  const [view, setView] = useState<'grid' | 'list'>('grid');
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [selected, setSelected] = useState<MediaAsset | null>(null);
  const [uploads, setUploads] = useState<UploadItem[]>([]);

  const { data, isLoading } = useQuery({
    queryKey: ['media', { search, page }],
    queryFn: () => mediaApi.list({ search: search || undefined, pageNumber: page, pageSize: 24 }),
  });

  const onDrop = useCallback(
    (acceptedFiles: File[]) => {
      const items: UploadItem[] = acceptedFiles.map((f) => ({
        file: f,
        progress: 0,
        status: 'uploading',
      }));
      setUploads((prev) => [...prev, ...items]);

      acceptedFiles.forEach((file, i) => {
        mediaApi
          .upload(
            file,
            {},
            (pct) => {
              setUploads((prev) =>
                prev.map((u, idx) => (idx === uploads.length + i ? { ...u, progress: pct } : u)),
              );
            },
          )
          .then(() => {
            setUploads((prev) =>
              prev.map((u, idx) => (idx === uploads.length + i ? { ...u, status: 'done' } : u)),
            );
            void qc.invalidateQueries({ queryKey: ['media'] });
            toast.success(`${file.name} uploaded.`);
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
    maxSize: 50 * 1024 * 1024,
  });

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">Media Library</h1>
          <p className="mt-1 text-sm text-slate-500">Upload and manage your assets.</p>
        </div>
        <div className="flex gap-2">
          <button
            onClick={() => setView('grid')}
            className={view === 'grid' ? 'btn-primary' : 'btn-secondary'}
            aria-label="Grid view"
          >
            <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2V6zM14 6a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2V6zM4 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2H6a2 2 0 01-2-2v-2zM14 16a2 2 0 012-2h2a2 2 0 012 2v2a2 2 0 01-2 2h-2a2 2 0 01-2-2v-2z" />
            </svg>
          </button>
          <button
            onClick={() => setView('list')}
            className={view === 'list' ? 'btn-primary' : 'btn-secondary'}
            aria-label="List view"
          >
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
        <p className="text-xs text-slate-400">Images, videos, PDFs up to 50 MB</p>
      </div>

      {/* Active uploads */}
      {uploads.some((u) => u.status === 'uploading') && (
        <div className="space-y-2">
          {uploads.filter((u) => u.status === 'uploading').map((u, i) => (
            <div key={i} className="flex items-center gap-3 rounded-lg border border-slate-200 p-3">
              <div className="flex-1 min-w-0">
                <p className="truncate text-sm font-medium text-slate-700">{u.file.name}</p>
                <div className="mt-1 h-1.5 w-full rounded-full bg-slate-200">
                  <div
                    className="h-1.5 rounded-full bg-brand-600 transition-all"
                    style={{ width: `${u.progress}%` }}
                  />
                </div>
              </div>
              <span className="text-xs text-slate-400">{u.progress}%</span>
            </div>
          ))}
        </div>
      )}

      {/* Search */}
      <input
        type="search"
        placeholder="Search media…"
        value={search}
        onChange={(e) => { setSearch(e.target.value); setPage(1); }}
        className="form-input w-64"
      />

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
            <button
              key={asset.id}
              onClick={() => setSelected(asset)}
              className="group relative aspect-square overflow-hidden rounded-lg border border-slate-200 bg-slate-100 hover:border-brand-400"
            >
              {asset.mediaType === 'image' ? (
                <img
                  src={asset.thumbnailUrl ?? asset.url}
                  alt={asset.altText ?? asset.fileName}
                  className="h-full w-full object-cover"
                />
              ) : (
                <div className="flex h-full w-full items-center justify-center text-3xl text-slate-400">
                  {asset.mediaType === 'video' ? '🎬' : asset.mediaType === 'audio' ? '🎵' : '📄'}
                </div>
              )}
              <div className="absolute inset-x-0 bottom-0 bg-gradient-to-t from-black/60 to-transparent p-2 opacity-0 transition-opacity group-hover:opacity-100">
                <p className="truncate text-xs text-white">{asset.fileName}</p>
              </div>
            </button>
          ))}
        </div>
      ) : (
        <div className="card overflow-hidden p-0">
          <table className="w-full text-sm">
            <thead className="border-b border-slate-100 bg-slate-50">
              <tr>
                <th className="px-6 py-3 text-left font-semibold text-slate-700">File</th>
                <th className="px-6 py-3 text-left font-semibold text-slate-700">Type</th>
                <th className="px-6 py-3 text-left font-semibold text-slate-700">Size</th>
                <th className="px-6 py-3 text-left font-semibold text-slate-700">Uploaded by</th>
                <th className="px-6 py-3" />
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {data?.items.map((asset) => (
                <tr key={asset.id} className="hover:bg-slate-50">
                  <td className="px-6 py-3 flex items-center gap-3">
                    {asset.mediaType === 'image' ? (
                      <img src={asset.thumbnailUrl ?? asset.url} alt="" className="h-8 w-8 rounded object-cover" />
                    ) : (
                      <div className="h-8 w-8 rounded bg-slate-100 flex items-center justify-center text-lg">📄</div>
                    )}
                    <span className="font-medium text-slate-900 truncate max-w-xs">{asset.fileName}</span>
                  </td>
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
        <AssetDetail
          asset={selected}
          onClose={() => setSelected(null)}
          onUpdated={() => setSelected(null)}
        />
      )}
    </div>
  );
}
