import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { contentTypesApi } from '@/api/contentTypes';
import type { ContentTypeListItem } from '@/types';
import { formatDistanceToNow } from 'date-fns';
import { ApiError } from '@/api/client';
import { useSite } from '@/contexts/SiteContext';

// ─── Helpers ──────────────────────────────────────────────────────────────────

function cardColor(handle: string): string {
  const colors = [
  'bg-violet-100 text-violet-700',
    'bg-blue-100 text-blue-700',
    'bg-emerald-100 text-emerald-700',
    'bg-amber-100 text-amber-700',
    'bg-rose-100 text-rose-700',
    'bg-cyan-100 text-cyan-700',
    'bg-indigo-100 text-indigo-700',
    'bg-orange-100 text-orange-700',
  ];
  let hash = 0;
  for (let i = 0; i < handle.length; i++) hash = handle.charCodeAt(i) + ((hash << 5) - hash);
  return colors[Math.abs(hash) % colors.length];
}

const HANDLE_EMOJIS: Record<string, string> = {
  blog: '📝', post: '📝', article: '📰', page: '📄', product: '🛍️',
  category: '🗂️', tag: '🏷️', event: '📅', user: '👤', media: '🖼️',
};
function handleEmoji(handle: string): string {
  for (const [key, emoji] of Object.entries(HANDLE_EMOJIS)) {
    if (handle.toLowerCase().includes(key)) return emoji;
}
  return '📋';
}

// ─── Content Type Card ────────────────────────────────────────────────────────

function ContentTypeCard({
  ct,
  onDelete,
}: {
  ct: ContentTypeListItem;
  onDelete: () => void;
}) {
  const [menuOpen, setMenuOpen] = useState(false);
  const navigate = useNavigate();
  const colorCls = cardColor(ct.handle);

  return (
    <div className="card flex flex-col gap-3 p-5 hover:shadow-md transition-shadow cursor-pointer" onClick={() => navigate(`/content-types/${ct.id}`)}>
      {/* Icon + name row */}
 <div className="flex items-start justify-between gap-2">
        <div className="flex items-center gap-3">
          <span className={`flex h-10 w-10 items-center justify-center rounded-lg text-xl ${colorCls}`}>
    {handleEmoji(ct.handle)}
  </span>
          <div>
     <p className="font-semibold text-slate-900 leading-tight">{ct.displayName}</p>
    <p className="font-mono text-xs text-slate-400">{ct.handle}</p>
          </div>
        </div>
        <div className="flex items-center gap-2 shrink-0">
          <span className={ct.status === 'Active' ? 'badge-brand' : ct.status === 'Archived' ? 'badge-slate' : 'badge-amber'}>
            {ct.status}
 </span>
          {/* Kebab menu */}
          <div className="relative">
            <button
              onClick={() => setMenuOpen((o) => !o)}
     className="rounded p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-600"
      aria-label="Actions"
    >
       ⋯
          </button>
            {menuOpen && (
      <div className="absolute right-0 z-20 mt-1 w-36 rounded-lg border border-slate-100 bg-white py-1 shadow-lg">
             <button
     onClick={() => { setMenuOpen(false); navigate(`/content-types/${ct.id}/edit`); }}
      className="block w-full px-4 py-2 text-left text-sm text-slate-700 hover:bg-slate-50"
           >
      Edit schema
        </button>
        <button
                  onClick={() => { setMenuOpen(false); navigate(`/content-types/${ct.id}?tab=entries`); }}
             className="block w-full px-4 py-2 text-left text-sm text-slate-700 hover:bg-slate-50"
                >
             Browse entries
      </button>
 <hr className="my-1 border-slate-100" />
             <button
          onClick={() => { setMenuOpen(false); onDelete(); }}
       className="block w-full px-4 py-2 text-left text-sm text-red-600 hover:bg-red-50"
    >
      Delete
       </button>
     </div>
         )}
          </div>
  </div>
      </div>

  {/* Stats */}
      <p className="text-xs text-slate-500">
        <span className="font-medium text-slate-700">{ct.entryCount ?? 0}</span> entries ·{' '}
        <span className="font-medium text-slate-700">{ct.fieldCount}</span> fields ·{' '}
  <span className="font-medium text-slate-700">{ct.localeCount ?? 0}</span> locales
      </p>

      {/* Footer */}
      <div className="mt-auto flex items-center justify-between border-t border-slate-100 pt-3">
        <span className="text-xs text-slate-400">
          {formatDistanceToNow(new Date(ct.updatedAt), { addSuffix: true })}
        </span>
        <Link
          to={`/content-types/${ct.id}?tab=entries`}
        className="text-xs font-medium text-brand-600 hover:underline"
  onClick={(e) => e.stopPropagation()}
        >
          Browse →
        </Link>
      </div>
    </div>
  );
}

// ─── Delete Confirmation ──────────────────────────────────────────────────────

function DeleteModal({
  contentType,
  onClose,
  onConfirm,
}: {
  contentType: ContentTypeListItem;
  onClose: () => void;
  onConfirm: () => void;
}) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="card mx-4 w-full max-w-md">
        <h3 className="text-base font-semibold text-slate-900">Delete Content Type</h3>
        <p className="mt-2 text-sm text-slate-500">
          Are you sure you want to delete <strong>{contentType.displayName}</strong>? This cannot be
          undone. All entries must be deleted first.
      </p>
 <div className="mt-4 flex justify-end gap-3">
          <button onClick={onClose} className="btn-secondary">Cancel</button>
          <button onClick={onConfirm} className="btn-danger">Delete</button>
        </div>
      </div>
  </div>
  );
}

// ─── Page ─────────────────────────────────────────────────────────────────────

export default function ContentTypesPage() {
  const qc = useQueryClient();
  const { selectedSiteId } = useSite();
  const [toDelete, setToDelete] = useState<ContentTypeListItem | null>(null);
  const [search, setSearch] = useState('');

  const { data, isLoading } = useQuery({
    queryKey: ['content-types', { siteId: selectedSiteId }],
    queryFn: () => contentTypesApi.list({ pageSize: 100, siteId: selectedSiteId ?? undefined }),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => contentTypesApi.delete(id),
    onSuccess: () => {
    toast.success('Content type deleted.');
  void qc.invalidateQueries({ queryKey: ['content-types'] });
      setToDelete(null);
    },
    onError: (err) => {
      const msg = err instanceof ApiError ? err.problem.detail ?? err.message : 'Delete failed.';
      toast.error(msg);
  },
  });

  const items = (data?.items ?? []).filter(
    (ct) =>
      !search ||
      ct.displayName.toLowerCase().includes(search.toLowerCase()) ||
      ct.handle.toLowerCase().includes(search.toLowerCase()),
  );

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
    <div>
          <h1 className="text-2xl font-bold text-slate-900">Content Types</h1>
        <p className="mt-1 text-sm text-slate-500">Define the schema for your content.</p>
        </div>
  <div className="flex items-center gap-2">
        <Link to="/content-types/import" className="btn-secondary">
Import Schema
          </Link>
          <Link to="/content-types/new" className="btn-primary">
          <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
           <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
            </svg>
            New Content Type
 </Link>
        </div>
      </div>

      {/* Search */}
 <div>
        <input
          type="search"
  value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Search content types…"
       className="form-input w-72"
     />
</div>

      {/* Grid */}
      {isLoading ? (
        <div className="grid grid-cols-[repeat(auto-fill,minmax(300px,1fr))] gap-4">
          {Array.from({ length: 6 }).map((_, i) => (
            <div key={i} className="h-48 animate-pulse rounded-xl bg-slate-100" />
        ))}
        </div>
      ) : items.length === 0 && !search ? (
     <div className="flex flex-col items-center justify-center gap-3 py-24 text-center">
          <div className="flex h-16 w-16 items-center justify-center rounded-full bg-slate-100 text-3xl">📋</div>
          <p className="font-medium text-slate-600">No content types yet</p>
          <p className="text-sm text-slate-400">Create your first content type to start structuring your content.</p>
     <Link to="/content-types/new" className="btn-primary mt-2">Create your first</Link>
        </div>
      ) : (
      <div className="grid grid-cols-[repeat(auto-fill,minmax(300px,1fr))] gap-4">
 {items.map((ct) => (
            <ContentTypeCard
    key={ct.id}
    ct={ct}
          onDelete={() => setToDelete(ct)}
            />
          ))}
    {/* Create new card */}
        <Link
       to="/content-types/new"
          className="flex min-h-[12rem] flex-col items-center justify-center gap-2 rounded-xl border-2 border-dashed border-slate-200 text-slate-400 hover:border-brand-300 hover:bg-brand-50/30 hover:text-brand-600 transition-colors"
          >
            <span className="text-3xl">＋</span>
            <span className="text-sm font-medium">New Content Type</span>
      </Link>
      </div>
      )}

      {/* Delete modal */}
{toDelete && (
        <DeleteModal
      contentType={toDelete}
          onClose={() => setToDelete(null)}
          onConfirm={() => deleteMutation.mutate(toDelete.id)}
        />
)}
    </div>
  );
}
