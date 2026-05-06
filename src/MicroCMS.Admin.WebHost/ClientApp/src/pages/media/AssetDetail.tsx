import { useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { mediaApi } from '@/api/media';
import type { MediaAsset } from '@/types';
import { ApiError } from '@/api/client';
import { ScanStatusBadge } from './ScanStatusBadge';
import { FolderPickerModal } from './FolderPickerModal';

export interface UploadItem {
  file: File;
  progress: number;
  status: 'uploading' | 'done' | 'error';
}

export function AssetDetail({
  asset,
  onClose,
  onUpdated,
}: {
  asset: MediaAsset;
  onClose: () => void;
  onUpdated: () => void;
}): JSX.Element {
  const [altText, setAltText]     = useState(asset.altText ?? '');
  const [tags, setTags]           = useState((asset.tags ?? []).join(', '));
  const [signedUrl, setSignedUrl] = useState<string | null>(null);
  const [showFolderPicker, setShowFolderPicker] = useState(false);
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

  const moveMutation = useMutation({
    mutationFn: (folderId: string | null) => mediaApi.bulkMove([asset.id], folderId),
    onSuccess: () => {
      toast.success('Asset moved.');
      void qc.invalidateQueries({ queryKey: ['media'] });
      void qc.invalidateQueries({ queryKey: ['media-folders'] });
      onUpdated();
    },
    onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Move failed.'),
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
    <div className="flex w-72 flex-shrink-0 flex-col border-l border-slate-200 bg-white">
      <div className="flex items-center justify-between border-b border-slate-200 px-4 py-3">
        <h3 className="text-sm font-semibold text-slate-700">Asset Details</h3>
        <button onClick={onClose} className="text-slate-400 hover:text-slate-600">✕</button>
      </div>

      <div className="flex-1 overflow-y-auto">
        {asset.mediaType === 'image' && isAvailable ? (
          <div className="border-b border-slate-100 bg-slate-50 p-4">
            <img
              src={asset.thumbnailUrl ?? asset.url}
              alt={asset.altText ?? asset.fileName}
              className="mx-auto max-h-36 rounded object-contain"
            />
          </div>
        ) : (
          <div className="flex h-24 items-center justify-center border-b border-slate-100 bg-slate-50 text-4xl">
            {asset.mediaType === 'video' ? '🎬' : asset.mediaType === 'audio' ? '🎵' : '📄'}
          </div>
        )}

        <div className="space-y-4 p-4">
          <p className="break-all text-sm font-medium text-slate-900">{asset.fileName}</p>

          <dl className="grid grid-cols-2 gap-x-3 gap-y-2 text-xs">
            <dt className="text-slate-500">Size</dt>
            <dd className="font-medium text-slate-700">{sizeKb >= 1024 ? `${(sizeKb / 1024).toFixed(1)} MB` : `${sizeKb} KB`}</dd>
            <dt className="text-slate-500">Type</dt>
            <dd className="font-medium text-slate-700 uppercase">{asset.contentType.split('/')[1] ?? asset.contentType}</dd>
            <dt className="text-slate-500">Uploaded</dt>
            <dd className="font-medium text-slate-700">{new Date(asset.createdAt).toLocaleDateString()}</dd>
            <dt className="text-slate-500">Scan</dt>
            <dd><ScanStatusBadge status={asset.status ?? 'Unknown'} /></dd>
            {asset.width && <>
              <dt className="text-slate-500">Dimensions</dt>
              <dd className="font-medium text-slate-700">{asset.width}×{asset.height}</dd>
            </>}
          </dl>

          {asset.status === 'Quarantined' && (
            <div className="rounded-lg border border-red-200 bg-red-50 p-2.5 text-xs text-red-700">
              This file was quarantined and cannot be delivered.
            </div>
          )}

          {isAvailable && (
            <div className="space-y-3">
              <div>
                <div className="flex items-center justify-between">
                  <label className="form-label">Alt Text</label>
                  <span className="text-xs text-brand-600">Generate</span>
                </div>
                <textarea
                  className="form-input mt-1 resize-none text-xs"
                  rows={2}
                  value={altText}
                  onChange={(e) => setAltText(e.target.value)}
                  placeholder="Describe this image…"
                />
              </div>
              <div>
                <label className="form-label">Tags</label>
                <input
                  className="form-input mt-1 text-xs"
                  value={tags}
                  onChange={(e) => setTags(e.target.value)}
                  placeholder="hero, banner, product"
                />
                {tags.trim() && (
                  <div className="mt-1.5 flex flex-wrap gap-1">
                    {tags.split(',').map((t) => t.trim()).filter(Boolean).map((t) => (
                      <span key={t} className="inline-flex items-center gap-0.5 rounded-full bg-slate-100 px-2 py-0.5 text-xs text-slate-600">
                        {t}
                        <button
                          onClick={() => setTags(tags.split(',').map((x) => x.trim()).filter((x) => x !== t).join(', '))}
                          className="ml-0.5 text-slate-400 hover:text-slate-700"
                        >×</button>
                      </span>
                    ))}
                  </div>
                )}
              </div>
            </div>
          )}

          {isAvailable && (
            <div className="space-y-2">
              <div className="flex gap-1.5">
                <input className="form-input flex-1 font-mono text-xs" readOnly value={asset.url} />
                <button
                  onClick={() => { void navigator.clipboard.writeText(asset.url); toast.success('Copied!'); }}
                  className="btn-secondary px-2 text-xs"
                  title="Copy URL"
                >⎘</button>
              </div>
              {signedUrl ? (
                <div className="flex gap-1.5">
                  <input className="form-input flex-1 font-mono text-xs" readOnly value={signedUrl} />
                  <button
                    onClick={() => { void navigator.clipboard.writeText(signedUrl); toast.success('Copied!'); }}
                    className="btn-secondary px-2 text-xs"
                  >⎘</button>
                </div>
              ) : (
                <button
                  onClick={() => signedUrlMutation.mutate()}
                  disabled={signedUrlMutation.isPending}
                  className="btn-secondary w-full justify-center text-xs"
                >
                  {signedUrlMutation.isPending ? 'Generating…' : '⎘ Copy Signed URL'}
                </button>
              )}
            </div>
          )}
        </div>
      </div>

      <div className="space-y-2 border-t border-slate-200 p-3">
        {isAvailable && (
          <button
            onClick={() => updateMutation.mutate()}
            disabled={updateMutation.isPending}
            className="btn-primary w-full justify-center text-sm"
          >{updateMutation.isPending ? 'Saving…' : 'Save Changes'}</button>
        )}
        <div className="flex gap-2">
          <button
            onClick={() => setShowFolderPicker(true)}
            className="btn-secondary flex-1 justify-center text-xs"
          >↗ Move</button>
          <button
            onClick={() => { void navigator.clipboard.writeText(asset.url); toast.success('URL copied!'); }}
            className="btn-secondary flex-1 justify-center text-xs"
          >⎘ Copy URL</button>
        </div>
        <button
          onClick={() => { if (confirm('Permanently delete this asset? This cannot be undone.')) deleteMutation.mutate(); }}
          disabled={deleteMutation.isPending}
          className="w-full rounded-lg border border-red-200 bg-red-50 py-1.5 text-xs font-medium text-red-600 hover:bg-red-100"
        >🗑 Delete Asset</button>
      </div>

      {showFolderPicker && (
        <FolderPickerModal
          currentFolderId={asset.folderId}
          onPick={(folderId) => { setShowFolderPicker(false); moveMutation.mutate(folderId); }}
          onClose={() => setShowFolderPicker(false)}
        />
      )}
    </div>
  );
}
