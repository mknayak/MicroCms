import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { contentTypesApi } from '@/api/contentTypes';
import type { ContentType } from '@/types';
import { formatDistanceToNow } from 'date-fns';
import { ApiError } from '@/api/client';

// ─── Delete Confirmation ──────────────────────────────────────────────────────

function DeleteModal({
  contentType,
  onClose,
  onConfirm,
}: {
  contentType: ContentType;
  onClose: () => void;
  onConfirm: () => void;
}) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="card mx-4 w-full max-w-md">
        <h3 className="text-base font-semibold text-slate-900">Delete Content Type</h3>
        <p className="mt-2 text-sm text-slate-500">
          Are you sure you want to delete <strong>{contentType.name}</strong>? This action cannot be
          undone and will also delete all associated entries.
        </p>
        <div className="mt-4 flex justify-end gap-3">
          <button onClick={onClose} className="btn-secondary">
            Cancel
          </button>
          <button onClick={onConfirm} className="btn-danger">
            Delete
          </button>
        </div>
      </div>
    </div>
  );
}

// ─── Page ─────────────────────────────────────────────────────────────────────

export default function ContentTypesPage() {
  const qc = useQueryClient();
  const [toDelete, setToDelete] = useState<ContentType | null>(null);

  const { data, isLoading } = useQuery({
    queryKey: ['content-types'],
    queryFn: () => contentTypesApi.list({ pageSize: 100 }),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => contentTypesApi.delete(id),
    onSuccess: () => {
      toast.success('Content type deleted.');
      void qc.invalidateQueries({ queryKey: ['content-types'] });
      setToDelete(null);
    },
    onError: (err) => {
      toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Delete failed.');
    },
  });

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-bold text-slate-900">Content Types</h1>
          <p className="mt-1 text-sm text-slate-500">Define the structure of your content.</p>
        </div>
        <Link to="/content-types/new" className="btn-primary">
          <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
          </svg>
          New Content Type
        </Link>
      </div>

      {/* List */}
      <div className="card overflow-hidden p-0">
        {isLoading ? (
          <div className="space-y-px">
            {Array.from({ length: 4 }).map((_, i) => (
              <div key={i} className="h-16 animate-pulse bg-slate-50" />
            ))}
          </div>
        ) : data?.items.length === 0 ? (
          <div className="flex flex-col items-center justify-center gap-3 py-16 text-center">
            <div className="flex h-16 w-16 items-center justify-center rounded-full bg-slate-100 text-slate-400">
              <svg className="h-8 w-8" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 10h16M4 14h16M4 18h16" />
              </svg>
            </div>
            <p className="text-sm font-medium text-slate-600">No content types yet</p>
            <Link to="/content-types/new" className="btn-primary">
              Create your first
            </Link>
          </div>
        ) : (
          <table className="w-full text-sm">
            <thead className="border-b border-slate-100 bg-slate-50">
              <tr>
                <th className="px-6 py-3 text-left font-semibold text-slate-700">Name</th>
                <th className="px-6 py-3 text-left font-semibold text-slate-700">API Key</th>
                <th className="px-6 py-3 text-left font-semibold text-slate-700">Type</th>
                <th className="px-6 py-3 text-left font-semibold text-slate-700">Fields</th>
                <th className="px-6 py-3 text-left font-semibold text-slate-700">Updated</th>
                <th className="px-6 py-3" />
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {data?.items.map((ct) => (
                <tr key={ct.id} className="hover:bg-slate-50">
                  <td className="px-6 py-4 font-medium text-slate-900">{ct.name}</td>
                  <td className="px-6 py-4 font-mono text-slate-500">{ct.apiKey}</td>
                  <td className="px-6 py-4">
                    <span className={ct.isCollection ? 'badge-brand' : 'badge-slate'}>
                      {ct.isCollection ? 'Collection' : 'Single'}
                    </span>
                  </td>
                  <td className="px-6 py-4 text-slate-500">{ct.fields.length}</td>
                  <td className="px-6 py-4 text-slate-400">
                    {formatDistanceToNow(new Date(ct.updatedAt), { addSuffix: true })}
                  </td>
                  <td className="px-6 py-4 text-right">
                    <div className="flex items-center justify-end gap-2">
                      <Link
                        to={`/content-types/${ct.id}/edit`}
                        className="rounded px-2 py-1 text-xs font-medium text-brand-600 hover:bg-brand-50"
                      >
                        Edit
                      </Link>
                      <button
                        onClick={() => setToDelete(ct)}
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
