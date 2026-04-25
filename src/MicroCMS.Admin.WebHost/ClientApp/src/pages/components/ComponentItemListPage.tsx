import { useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { componentsApi } from '@/api/components';
import { ApiError } from '@/api/client';
import type { ComponentItemDto } from '@/types';

const STATUS_PILL: Record<string, string> = {
  Published: 'bg-green-100 text-green-700',
  Draft: 'bg-amber-100 text-amber-700',
  Archived: 'bg-slate-100 text-slate-500',
};

export default function ComponentItemListPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const qc = useQueryClient();
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState('');
  const [selected, setSelected] = useState<Set<string>>(new Set());

  const { data: comp } = useQuery({
    queryKey: ['component', id],
    queryFn: () => componentsApi.getById(id!),
    enabled: !!id,
  });

  const { data, isLoading } = useQuery({
    queryKey: ['component-items', id, statusFilter],
    queryFn: () =>
      componentsApi.listItems(id!, {
        status: statusFilter as ComponentItemDto['status'] | undefined || undefined,
        pageSize: 50,
      }),
    enabled: !!id,
  });

  const deleteMutation = useMutation({
    mutationFn: (itemId: string) => componentsApi.deleteItem(id!, itemId),
    onSuccess: () => {
      toast.success('Item deleted.');
      void qc.invalidateQueries({ queryKey: ['component-items', id] });
      void qc.invalidateQueries({ queryKey: ['component', id] });
    },
    onError: (err) =>
  toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Delete failed.'),
  });

  const publishMutation = useMutation({
    mutationFn: (itemId: string) => componentsApi.publishItem(id!, itemId),
    onSuccess: () => {
  toast.success('Item published.');
      void qc.invalidateQueries({ queryKey: ['component-items', id] });
 },
    onError: (err) =>
      toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Publish failed.'),
  });

  const filtered = (data?.items ?? []).filter((item) => {
    if (!search) return true;
    const q = search.toLowerCase();
    return item.title.toLowerCase().includes(q);
  });

  const toggleSelect = (itemId: string) => {
    setSelected((prev) => {
    const next = new Set(prev);
  next.has(itemId) ? next.delete(itemId) : next.add(itemId);
      return next;
  });
  };

  const toggleAll = () => {
    if (selected.size === filtered.length) {
      setSelected(new Set());
    } else {
      setSelected(new Set(filtered.map((i) => i.id)));
    }
  };

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-start justify-between">
        <div className="flex items-center gap-3">
          <button
            onClick={() => navigate('/components')}
            className="text-sm text-slate-500 hover:text-slate-700"
   >
         ← Component Library
          </button>
          <span className="text-slate-300">/</span>
          {comp && (
            <>
         <h1 className="text-xl font-bold text-slate-900">{comp.name}</h1>
       <span className="rounded-full bg-slate-100 px-2 py-0.5 font-mono text-xs text-slate-500">
       {comp.key}
         </span>
     </>
    )}
        </div>
   <div className="flex gap-3">
          <button
         onClick={() => navigate(`/components/${id}/edit`)}
            className="btn-secondary text-sm"
  >
        Edit Schema
        </button>
        <button
            onClick={() => navigate(`/components/${id}/items/new`)}
       className="btn-primary"
 >
         + New Item
          </button>
        </div>
      </div>

      {/* Toolbar */}
      <div className="flex flex-wrap items-center gap-3">
        <input
          className="h-9 w-64 rounded-lg border border-slate-200 bg-white px-3 text-sm focus:border-brand-500 focus:outline-none"
        placeholder="Search items…"
       value={search}
      onChange={(e) => setSearch(e.target.value)}
  />
        <select
          className="h-9 rounded-lg border border-slate-200 bg-white px-3 text-sm focus:outline-none"
          value={statusFilter}
    onChange={(e) => setStatusFilter(e.target.value)}
        >
          <option value="">All Status</option>
          <option value="Published">Published</option>
          <option value="Draft">Draft</option>
          <option value="Archived">Archived</option>
        </select>
        <span className="ml-auto text-xs text-slate-400">
          {data?.totalCount ?? 0} items
        </span>
      </div>

      {/* Bulk action bar */}
      {selected.size > 0 && (
        <div className="flex items-center gap-3 rounded-lg bg-brand-50 border border-brand-200 px-4 py-2.5 text-sm">
  <strong className="text-brand-700">{selected.size} selected</strong>
          <button
            className="rounded border border-slate-200 bg-white px-3 py-1 text-xs font-medium hover:bg-slate-50"
    onClick={() => {
         selected.forEach((itemId) => publishMutation.mutate(itemId));
    setSelected(new Set());
     }}
          >
            Publish Selected
          </button>
 <button
   className="rounded border border-red-200 bg-white px-3 py-1 text-xs font-medium text-red-600 hover:bg-red-50"
            onClick={() => {
           if (confirm(`Delete ${selected.size} item(s)?`)) {
         selected.forEach((itemId) => deleteMutation.mutate(itemId));
      setSelected(new Set());
         }
        }}
          >
Delete Selected
          </button>
          <button className="ml-auto text-xs text-slate-400" onClick={() => setSelected(new Set())}>
            Clear
 </button>
        </div>
  )}

      {/* Table */}
      <div className="card overflow-hidden p-0">
        <table className="w-full text-sm">
<thead className="bg-slate-50 text-[11px] font-semibold uppercase tracking-wider text-slate-400">
     <tr>
          <th className="w-10 px-4 py-3">
        <input
         type="checkbox"
       className="accent-brand-600"
        checked={selected.size === filtered.length && filtered.length > 0}
    onChange={toggleAll}
     />
       </th>
<th className="px-4 py-3 text-left">Title</th>
        <th className="px-4 py-3 text-left">Status</th>
       <th className="px-4 py-3 text-left">Used In</th>
   <th className="px-4 py-3 text-left">Updated</th>
       <th className="w-20 px-4 py-3" />
            </tr>
          </thead>
     <tbody className="divide-y divide-slate-100">
       {isLoading ? (
        Array.from({ length: 5 }).map((_, i) => (
                <tr key={i}>
              <td colSpan={6} className="px-4 py-3">
          <div className="h-5 animate-pulse rounded bg-slate-100" />
         </td>
     </tr>
       ))
      ) : filtered.length === 0 ? (
          <tr>
         <td colSpan={6} className="py-16 text-center text-sm text-slate-400">
 No items found.
     </td>
  </tr>
            ) : (
              filtered.map((item) => (
  <tr
             key={item.id}
 className="group cursor-pointer hover:bg-slate-50"
          onClick={() => navigate(`/components/${id}/items/${item.id}`)}
           >
 <td className="px-4 py-3" onClick={(e) => { e.stopPropagation(); toggleSelect(item.id); }}>
    <input
    type="checkbox"
       className="accent-brand-600"
      checked={selected.has(item.id)}
 onChange={() => {}}
    />
       </td>
          <td className="px-4 py-3 font-medium text-slate-900">{item.title}</td>
          <td className="px-4 py-3">
           <span className={`rounded-full px-2 py-0.5 text-xs font-semibold ${STATUS_PILL[item.status]}`}>
    {item.status}
   </span>
       </td>
  <td className="px-4 py-3 text-slate-500">{item.usedOnPages} pages</td>
                  <td className="px-4 py-3 text-slate-400 text-xs">
      {new Date(item.updatedAt).toLocaleDateString()}
          </td>
          <td className="px-4 py-3">
   <div className="flex items-center justify-end gap-1 opacity-0 group-hover:opacity-100">
          <button
    className="rounded p-1 text-slate-400 hover:bg-slate-200"
         title="Edit"
    onClick={(e) => {
       e.stopPropagation();
      navigate(`/components/${id}/items/${item.id}`);
      }}
          >
                 <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
        </svg>
            </button>
              <button
           className="rounded p-1 text-red-300 hover:bg-red-50"
        title="Delete"
    onClick={(e) => {
              e.stopPropagation();
  if (confirm(`Delete "${item.title}"?`)) deleteMutation.mutate(item.id);
            }}
               >
  <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
         </svg>
           </button>
 </div>
  </td>
       </tr>
   ))
            )}
       </tbody>
        </table>
  </div>
    </div>
  );
}
