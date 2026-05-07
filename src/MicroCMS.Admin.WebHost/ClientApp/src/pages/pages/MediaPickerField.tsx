import { useState, useCallback } from 'react';
import { useQuery } from '@tanstack/react-query';
import { mediaApi } from '@/api/media';
import type { MediaAsset } from '@/types';

// ─── Stored value shape ───────────────────────────────────────────────────────
// This is what gets persisted in entry.fields[handle].
// Only `id` is required; transform props are optional — the delivery API
// falls back to the original asset when they are absent.

export type ImageFit = 'Contain' | 'Cover' | 'Fill';
export type ImageFormat = 'Original' | 'Jpeg' | 'Png' | 'WebP';

export interface AssetReferenceValue {
  /** MediaAsset.Id (GUID string) */
  id: string;
  width?: number;
  height?: number;
  fit?: ImageFit;
  format?: ImageFormat;
  quality?: number;
}

/** Parse a raw field value (could be a plain ID string from before this change, or the new object). */
function parseValue(raw: unknown): AssetReferenceValue | null {
  if (!raw) return null;
  if (typeof raw === 'string') return { id: raw };
  if (typeof raw === 'object' && raw !== null && 'id' in raw) return raw as AssetReferenceValue;
  return null;
}

// ─── Modal ────────────────────────────────────────────────────────────────────

function MediaPickerModal({
  onSelect,
  onClose,
}: {
  onSelect: (asset: MediaAsset) => void;
  onClose: () => void;
}) {
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  // breadcrumb: array of { id, name } representing the path from root to current folder
  const [breadcrumb, setBreadcrumb] = useState<{ id: string; name: string }[]>([]);

  // Current folder is the last entry in the breadcrumb (or root when empty)
  const folderId = breadcrumb.length > 0 ? breadcrumb[breadcrumb.length - 1].id : undefined;

  const { data, isLoading } = useQuery({
    queryKey: ['media-picker', { search, page, folderId }],
    queryFn: () =>
      mediaApi.list({ search: search || undefined, page, pageSize: 24, folderId }),
  });

  const { data: folders = [] } = useQuery({
    queryKey: ['media-folders-picker', folderId],
    queryFn: () => mediaApi.listFolders(folderId),
  });

  function navigateTo(id: string, name: string) {
    setBreadcrumb((prev) => [...prev, { id, name }]);
    setPage(1);
    setSearch('');
  }

  function navigateToBreadcrumb(index: number | null) {
    // index === null → root; index N → slice to N+1
    setBreadcrumb((prev) => index === null ? [] : prev.slice(0, index + 1));
    setPage(1);
    setSearch('');
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
      <div className="flex h-[80vh] w-full max-w-3xl flex-col overflow-hidden rounded-2xl bg-white shadow-2xl">
        {/* Header */}
        <div className="flex items-center justify-between border-b border-slate-200 px-5 py-4">
          <h2 className="text-sm font-semibold text-slate-800">Select from Media Library</h2>
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
            placeholder="Search assets…"
            value={search}
            onChange={(e) => { setSearch(e.target.value); setPage(1); }}
            className="form-input w-full text-sm"
          />
        </div>

        {/* Breadcrumb */}
        <div className="flex items-center gap-1 border-b border-slate-100 px-5 py-2 text-xs">
          <button
            onClick={() => navigateToBreadcrumb(null)}
            className={`flex items-center gap-1 rounded px-1.5 py-0.5 transition-colors ${breadcrumb.length === 0 ? 'font-semibold text-slate-800' : 'text-slate-500 hover:bg-slate-100 hover:text-slate-700'}`}
          >
            <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 7a2 2 0 012-2h3.586a1 1 0 01.707.293L10.414 6.5A1 1 0 0011.121 6.793H19a2 2 0 012 2v9a2 2 0 01-2 2H5a2 2 0 01-2-2V7z" />
            </svg>
            All Assets
          </button>
          {breadcrumb.map((crumb, i) => (
            <span key={crumb.id} className="flex items-center gap-1 min-w-0">
              <svg className="h-3 w-3 shrink-0 text-slate-300" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
              </svg>
              <button
                onClick={() => navigateToBreadcrumb(i)}
                className={`max-w-[140px] truncate rounded px-1.5 py-0.5 transition-colors ${i === breadcrumb.length - 1 ? 'font-semibold text-slate-800' : 'text-slate-500 hover:bg-slate-100 hover:text-slate-700'}`}
              >
                {crumb.name}
              </button>
            </span>
          ))}
        </div>

        {/* Content */}
        <div className="flex-1 overflow-y-auto px-5 py-4 space-y-4">
          {/* Sub-folder chips */}
          {folders.length > 0 && !search && (
            <div className="flex flex-wrap gap-2">
              {folders.map((f) => (
                <button
                  key={f.id}
                  onClick={() => navigateTo(f.id, f.name)}
                  className="flex items-center gap-1 rounded-md border border-slate-200 px-2 py-1 text-xs text-slate-600 hover:border-brand-300 hover:bg-brand-50"
                >
                  <svg className="h-3.5 w-3.5 text-amber-400" fill="currentColor" viewBox="0 0 20 20">
                    <path d="M2 6a2 2 0 012-2h5l2 2h5a2 2 0 012 2v6a2 2 0 01-2 2H4a2 2 0 01-2-2V6z" />
                  </svg>
                  {f.name}
                  {f.assetCount > 0 && (
                    <span className="text-slate-400">({f.assetCount})</span>
                  )}
                </button>
              ))}
            </div>
          )}

          {/* Grid */}
          {isLoading ? (
            <div className="grid grid-cols-4 gap-3 sm:grid-cols-6">
              {Array.from({ length: 12 }).map((_, i) => (
                <div key={i} className="aspect-square animate-pulse rounded-lg bg-slate-200" />
              ))}
            </div>
          ) : !data?.items.length ? (
            <div className="flex flex-col items-center justify-center py-16 text-slate-400">
              <svg className="mb-3 h-10 w-10" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
              </svg>
              <p className="text-sm font-medium">No assets found</p>
            </div>
          ) : (
            <div className="grid grid-cols-4 gap-3 sm:grid-cols-6">
              {data.items.map((asset) => (
                <button
                  key={asset.id}
                  onClick={() => onSelect(asset)}
                  className="group relative aspect-square overflow-hidden rounded-lg border border-slate-200 bg-slate-100 transition-all hover:border-brand-400 hover:ring-2 hover:ring-brand-200 focus:outline-none focus:ring-2 focus:ring-brand-400"
                  title={asset.fileName}
                >
                  {asset.mediaType === 'image' ? (
                    <img
                      src={asset.thumbnailUrl ?? asset.url}
                      alt={asset.altText ?? asset.fileName}
                      className="h-full w-full object-cover"
                    />
                  ) : (
                    <div className="flex h-full w-full flex-col items-center justify-center text-2xl">
                      {asset.mediaType === 'video' ? '🎬'
                        : asset.mediaType === 'audio' ? '🎵'
                        : asset.mediaType === 'document' ? '📄' : '📁'}
                    </div>
                  )}
                  <div className="absolute inset-0 flex items-end bg-gradient-to-t from-black/50 to-transparent opacity-0 transition-opacity group-hover:opacity-100">
                    <p className="truncate px-1 pb-1 text-[10px] font-medium text-white">{asset.fileName}</p>
                  </div>
                </button>
              ))}
            </div>
          )}
        </div>

        {/* Pagination */}
        {data && data.totalCount > 24 && (
          <div className="flex items-center justify-between border-t border-slate-100 px-5 py-3">
            <p className="text-xs text-slate-500">
              {data.totalCount} asset{data.totalCount !== 1 ? 's' : ''}
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
                disabled={page * 24 >= data.totalCount}
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

// ─── Helpers ──────────────────────────────────────────────────────────────────

/**
 * `mediaApi.getById` returns `MediaAssetDto` (server shape), which has `mimeType`
 * but NO `mediaType` field — that field only exists on the list-item DTO.
 * Derive it from `mimeType` using the same logic as the server's MediaTypeFromMime.
 */
function deriveMediaType(asset: MediaAsset): string {
  // When loaded via getById the real JSON has `mimeType`; the TS type says `contentType`.
  const mime =
    (asset as unknown as { mimeType?: string }).mimeType ??
    asset.contentType ??
    '';
  if (mime.startsWith('image/')) return 'image';
  if (mime.startsWith('video/')) return 'video';
  if (mime.startsWith('audio/')) return 'audio';
  if (mime === 'application/pdf' || mime.startsWith('text/')) return 'document';
  // Fallback: trust whatever mediaType the list endpoint may have populated
  return asset.mediaType ?? 'other';
}

/** Build the variant preview URL using the admin API. */
function buildVariantUrl(id: string, v: AssetReferenceValue): string {
  const params = new URLSearchParams();
  if (v.width)  params.set('w', String(v.width));
  if (v.height) params.set('h', String(v.height));
  if (v.fit && v.fit !== 'Contain')       params.set('fit', v.fit);
  if (v.format && v.format !== 'Original') params.set('fmt', v.format);
  if (v.quality && v.quality !== 85)       params.set('q',   String(v.quality));
  const qs = params.toString();
  return `/api/v1/media/${id}/variant${qs ? `?${qs}` : ''}`;
}

// ─── Field ────────────────────────────────────────────────────────────────────

export function MediaPickerField({
  value: rawValue,
  onChange,
}: {
  /**
   * The raw stored value — either a plain GUID string (legacy) or an
   * AssetReferenceValue object. Pass `null` / `undefined` when empty.
   */
  value: unknown;
  onChange: (value: AssetReferenceValue | null) => void;
}) {
  const [open, setOpen] = useState(false);

  const value = parseValue(rawValue);

  // Resolve the stored ID → asset metadata for preview
  const { data: asset } = useQuery({
    queryKey: ['media-asset', value?.id],
    queryFn: () => mediaApi.getById(value!.id),
    enabled: !!value?.id,
    staleTime: 60_000,
  });

  const handleSelect = useCallback(
    (selected: MediaAsset) => {
      // Keep existing transform settings when swapping the asset
      onChange({ ...(value ?? {}), id: selected.id });
      setOpen(false);
    },
    [onChange, value],
  );

  const handleClear = useCallback(
    (e: React.MouseEvent) => {
      e.stopPropagation();
      onChange(null);
    },
    [onChange],
  );

  const setTransform = useCallback(
    (patch: Partial<Omit<AssetReferenceValue, 'id'>>) => {
      if (!value) return;
      const next = { ...value, ...patch };
      // Strip defaults to keep stored JSON lean
      if (!next.width)                            delete next.width;
      if (!next.height)                           delete next.height;
      if (!next.fit    || next.fit    === 'Contain')  delete next.fit;
      if (!next.format || next.format === 'Original') delete next.format;
      if (!next.quality || next.quality === 85)       delete next.quality;
      onChange(next);
    },
    [onChange, value],
  );

  if (!value?.id || !asset) {
    /* ── Empty / browse state ── */
    return (
      <>
        <button
          type="button"
          onClick={() => setOpen(true)}
          className="flex w-full flex-col items-center justify-center gap-2 rounded-lg border border-dashed border-slate-300 bg-slate-50 py-8 transition-colors hover:border-brand-400 hover:bg-brand-50 focus:outline-none focus:ring-2 focus:ring-brand-400"
        >
          <div className="flex h-10 w-10 items-center justify-center rounded-full bg-slate-200 text-slate-400">
            <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
            </svg>
          </div>
          <p className="text-xs text-slate-500">Select from media library</p>
          <span className="rounded-md border border-slate-300 bg-white px-3 py-1 text-xs font-semibold text-slate-700 shadow-sm hover:border-brand-400 hover:text-brand-600">
            Browse
          </span>
        </button>
        {open && <MediaPickerModal onSelect={handleSelect} onClose={() => setOpen(false)} />}
      </>
    );
  }

  /* ── Selected state — 2-column layout ── */
  const mediaType = deriveMediaType(asset);
  const isImage = mediaType === 'image';
  // Use the variant endpoint so the preview reflects the active transform settings
  const previewUrl = isImage ? buildVariantUrl(value.id, value) : null;

  return (
    <>
      <div className="overflow-hidden rounded-lg border border-slate-200 bg-white">
        <div className="flex min-h-[160px]">

          {/* ── Col 1: Preview ── */}
          <div className="group relative flex w-2/5 shrink-0 items-center justify-center overflow-hidden bg-slate-100">
            {isImage && previewUrl ? (
              <img
                key={previewUrl}  /* re-mount when transform changes so browser re-fetches */
                src={previewUrl}
                alt={asset.altText ?? asset.fileName}
                className="h-full w-full object-contain"
              />
            ) : (
              <span className="text-5xl">
                {mediaType === 'video'    ? '🎬'
                 : mediaType === 'audio'    ? '🎵'
                 : mediaType === 'document' ? '📄' : '📁'}
              </span>
            )}

            {/* Hover: Change / Remove */}
            <div className="absolute inset-0 flex flex-col items-center justify-center gap-1.5 bg-black/40 opacity-0 transition-opacity group-hover:opacity-100">
              <button
                onClick={() => setOpen(true)}
                className="rounded bg-white/90 px-2.5 py-1 text-[11px] font-semibold text-slate-700 hover:bg-white"
              >
                Change
              </button>
              <button
                onClick={handleClear}
                className="rounded bg-red-500/90 px-2.5 py-1 text-[11px] font-semibold text-white hover:bg-red-500"
              >
                Remove
              </button>
            </div>
          </div>

          {/* ── Col 2: Transform options ── */}
          <div className="flex flex-1 flex-col gap-2.5 border-l border-slate-200 bg-slate-50 px-3 py-3">
            {/* Filename + type */}
            <div className="flex items-start justify-between gap-1">
              <p className="truncate text-[11px] font-medium text-slate-700" title={asset.fileName}>{asset.fileName}</p>
              <span className="shrink-0 rounded bg-slate-200 px-1 py-0.5 text-[9px] font-semibold uppercase text-slate-500">
                {mediaType}
              </span>
            </div>

            {isImage ? (
              <>
                {/* Width / Height */}
                <div className="flex gap-1.5">
                  <label className="flex-1">
                    <span className="mb-0.5 block text-[10px] text-slate-500">Width</span>
                    <input
                      type="number" min={1} max={8000}
                      className="form-input w-full text-xs"
                      placeholder="auto"
                      value={value.width ?? ''}
                      onChange={(e) => setTransform({ width: e.target.value ? parseInt(e.target.value) : undefined })}
                    />
                  </label>
                  <label className="flex-1">
                    <span className="mb-0.5 block text-[10px] text-slate-500">Height</span>
                    <input
                      type="number" min={1} max={8000}
                      className="form-input w-full text-xs"
                      placeholder="auto"
                      value={value.height ?? ''}
                      onChange={(e) => setTransform({ height: e.target.value ? parseInt(e.target.value) : undefined })}
                    />
                  </label>
                </div>

                {/* Fit */}
                <label>
                  <span className="mb-0.5 block text-[10px] text-slate-500">Fit</span>
                  <select
                    className="form-input w-full text-xs"
                    value={value.fit ?? 'Contain'}
                    onChange={(e) => setTransform({ fit: e.target.value as ImageFit })}
                  >
                    <option value="Contain">Contain</option>
                    <option value="Cover">Cover (crop)</option>
                    <option value="Fill">Fill (stretch)</option>
                  </select>
                </label>

                {/* Format / Quality */}
                <div className="flex gap-1.5">
                  <label className="flex-1">
                    <span className="mb-0.5 block text-[10px] text-slate-500">Format</span>
                    <select
                      className="form-input w-full text-xs"
                      value={value.format ?? 'Original'}
                      onChange={(e) => setTransform({ format: e.target.value as ImageFormat })}
                    >
                      <option value="Original">Original</option>
                      <option value="WebP">WebP</option>
                      <option value="Jpeg">JPEG</option>
                      <option value="Png">PNG</option>
                    </select>
                  </label>
                  <label className="flex-1">
                    <span className="mb-0.5 block text-[10px] text-slate-500">Quality</span>
                    <input
                      type="number" min={1} max={100}
                      className="form-input w-full text-xs"
                      placeholder="85"
                      value={value.quality ?? ''}
                      onChange={(e) => setTransform({ quality: e.target.value ? parseInt(e.target.value) : undefined })}
                    />
                  </label>
                </div>
              </>
            ) : (
              <p className="text-[11px] text-slate-400">No transform options for {mediaType} files.</p>
            )}
          </div>
        </div>
      </div>

      {open && <MediaPickerModal onSelect={handleSelect} onClose={() => setOpen(false)} />}
    </>
  );
}
