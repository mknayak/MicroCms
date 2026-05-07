import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { formatDistanceToNow } from 'date-fns';
import toast from 'react-hot-toast';
import { entryGroupsApi, type CreateEntryGroupRequest, type UpdateEntryGroupRequest } from '@/api/contentTypes';
import { entriesApi } from '@/api/entries';
import type { ContentType, EntryGroupListItem, EntryGroupDto, EntryListItem } from '@/types';
import { ApiError } from '@/api/client';

// ─── Entry Picker (dual-pane) ─────────────────────────────────────────────────
// Loads entries of this content type directly — no MultiList field required.

function EntryPicker({
  contentTypeId,
  siteId,
  selected,
  onChange,
}: {
  contentTypeId: string;
  siteId: string;
  selected: string[];
  onChange: (ids: string[]) => void;
}) {
  const [search, setSearch] = useState('');

  const { data, isLoading } = useQuery<{ items: EntryListItem[] }>({
    queryKey: ['entries-for-group-picker', contentTypeId, siteId],
    queryFn: () => entriesApi.list({ contentTypeId, siteId, pageSize: 200 }),
  });

  const allEntries = data?.items ?? [];

  const available = allEntries.filter(
    (e) =>
      !selected.includes(e.id) &&
      (e.title ?? e.slug).toLowerCase().includes(search.toLowerCase()),
  );
  const selectedEntries = allEntries.filter((e) => selected.includes(e.id));

  const add = (id: string) => onChange([...selected, id]);
  const remove = (id: string) => onChange(selected.filter((s) => s !== id));

  return (
    <div className="grid grid-cols-2 gap-3">
      {/* Left — available */}
      <div className="flex flex-col gap-2">
        <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">
          Available
          <span className="ml-1 normal-case font-normal text-slate-400">({available.length})</span>
        </p>
        <input
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          placeholder="Search entries…"
          className="form-input text-sm"
        />
        <div className="h-52 overflow-y-auto rounded-lg border border-slate-200 bg-white">
          {isLoading && (
            <div className="flex h-full items-center justify-center text-slate-400 text-sm">Loading…</div>
          )}
          {!isLoading && available.length === 0 && (
            <div className="flex h-full items-center justify-center text-slate-400 text-sm">No entries available</div>
          )}
          {available.map((e) => (
            <button
              key={e.id}
              type="button"
              onClick={() => add(e.id)}
              className="flex w-full items-center gap-2 px-3 py-2 text-left text-sm hover:bg-brand-50 border-b border-slate-100 last:border-0 group"
            >
              <span className="flex-1 truncate text-slate-700">{e.title ?? e.slug}</span>
              <span className={`shrink-0 rounded px-1.5 py-0.5 text-xs font-medium ${
                e.status === 'Published' ? 'bg-green-100 text-green-700' : 'bg-slate-100 text-slate-500'
              }`}>{e.status}</span>
              <span className="text-slate-300 group-hover:text-brand-500">›</span>
            </button>
          ))}
        </div>
      </div>

      {/* Right — selected */}
      <div className="flex flex-col gap-2">
        <p className="text-xs font-semibold uppercase tracking-wide text-slate-500">
          Selected
          <span className="ml-1 normal-case font-normal text-slate-400">({selected.length})</span>
        </p>
        {/* Spacer to align with the search input on the left */}
        <div className="h-[2.125rem]" />
        <div className="h-52 overflow-y-auto rounded-lg border border-slate-200 bg-white">
          {selectedEntries.length === 0 && (
            <div className="flex h-full items-center justify-center text-slate-400 text-sm">None selected</div>
          )}
          {selectedEntries.map((e) => (
            <div
              key={e.id}
              className="flex items-center gap-2 px-3 py-2 border-b border-slate-100 last:border-0"
            >
              <span className="flex-1 text-sm truncate text-slate-700">{e.title ?? e.slug}</span>
              <button
                type="button"
                onClick={() => remove(e.id)}
                className="shrink-0 text-slate-300 hover:text-red-500 transition-colors text-base leading-none"
                title="Remove"
              >
                ✕
              </button>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}

// ─── Group form modal ─────────────────────────────────────────────────────────

function GroupModal({
  contentType,
  group,
  onClose,
}: {
  contentType: ContentType;
  group?: EntryGroupDto;
  onClose: () => void;
}) {
  const qc = useQueryClient();
  const isEdit = !!group;

  const [handle, setHandle] = useState(group?.handle ?? '');
  const [title, setTitle] = useState(group?.title ?? '');
  const [description, setDescription] = useState(group?.description ?? '');
  const [memberIds, setMemberIds] = useState<string[]>(group?.memberEntryIds ?? []);

  const createMutation = useMutation({
    mutationFn: (data: CreateEntryGroupRequest) => entryGroupsApi.create(contentType.id, data),
    onSuccess: () => {
      toast.success('Group created');
      qc.invalidateQueries({ queryKey: ['entry-groups', contentType.id] });
      onClose();
    },
    onError: (err: unknown) => {
      toast.error(err instanceof ApiError ? err.message : 'Failed to create group');
    },
  });

  const updateMutation = useMutation({
    mutationFn: (data: UpdateEntryGroupRequest) =>
      entryGroupsApi.update(contentType.id, group!.id, data),
    onSuccess: () => {
      toast.success('Group updated');
      qc.invalidateQueries({ queryKey: ['entry-groups', contentType.id] });
      onClose();
    },
    onError: (err: unknown) => {
      toast.error(err instanceof ApiError ? err.message : 'Failed to update group');
    },
  });

  const busy = createMutation.isPending || updateMutation.isPending;

  function handleSubmit(e: React.FormEvent) {
    e.preventDefault();
    const payload = {
      title: title.trim(),
      description: description.trim() || undefined,
      memberEntryIds: memberIds,
    };
    if (isEdit) {
      updateMutation.mutate(payload);
    } else {
      createMutation.mutate({ handle: handle.trim(), ...payload });
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
      <div className="w-full max-w-2xl rounded-2xl bg-white shadow-2xl flex flex-col max-h-[90vh]">
        {/* Header */}
        <div className="flex items-center justify-between border-b border-slate-100 px-6 py-4 shrink-0">
          <div>
            <h2 className="text-base font-semibold text-slate-800">
              {isEdit ? `Edit — ${group.title}` : 'New Group'}
            </h2>
            <p className="text-xs text-slate-400 mt-0.5">{contentType.displayName}</p>
          </div>
          <button
            type="button"
            onClick={onClose}
            className="rounded-lg p-1.5 text-slate-400 hover:bg-slate-100 hover:text-slate-600 transition-colors"
          >✕</button>
        </div>

        <form onSubmit={handleSubmit} className="flex flex-col gap-5 p-6 overflow-y-auto">
          <div className="grid grid-cols-2 gap-4">
          {/* Handle */}
          {!isEdit ? (
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">Handle <span className="text-red-500">*</span></label>
              <input
                value={handle}
                onChange={(e) => setHandle(e.target.value.toLowerCase().replace(/[^a-z0-9-]/g, '-'))}
                required
                placeholder="e.g. g7-countries"
                className="form-input"
              />
              <p className="mt-1 text-xs text-slate-400">Lowercase, numbers, hyphens only. Cannot be changed later.</p>
            </div>
          ) : (
            <div>
              <label className="block text-sm font-medium text-slate-700 mb-1">Handle</label>
              <input value={group.handle} disabled className="form-input bg-slate-50 text-slate-400 cursor-not-allowed" />
            </div>
          )}

          {/* Title */}
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1">Title <span className="text-red-500">*</span></label>
            <input
              value={title}
              onChange={(e) => setTitle(e.target.value)}
              required
              placeholder="e.g. G7 Countries"
              className="form-input"
            />
          </div>
          </div>

          {/* Description */}
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-1">Description</label>
            <textarea
              value={description}
              onChange={(e) => setDescription(e.target.value)}
              rows={2}
              placeholder="Optional description…"
              className="form-input resize-none"
            />
          </div>

          {/* Members dual-pane */}
          <div>
            <label className="block text-sm font-medium text-slate-700 mb-2">
              Members
              {memberIds.length > 0 && (
                <span className="ml-2 text-xs font-normal text-slate-400">{memberIds.length} selected</span>
              )}
            </label>
            <EntryPicker
              contentTypeId={contentType.id}
              siteId={contentType.siteId}
              selected={memberIds}
              onChange={setMemberIds}
            />
          </div>

          {/* Actions */}
          <div className="flex justify-end gap-2 border-t border-slate-100 pt-4">
            <button type="button" onClick={onClose} className="btn-secondary" disabled={busy}>Cancel</button>
            <button type="submit" className="btn-primary" disabled={busy}>
              {busy ? 'Saving…' : isEdit ? 'Save Changes' : 'Create Group'}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}

// ─── Group row ────────────────────────────────────────────────────────────────

function GroupRow({
  item,
  onEdit,
  onDelete,
}: {
  item: EntryGroupListItem;
  onEdit: () => void;
  onDelete: () => void;
}) {
  return (
    <tr className="hover:bg-slate-50 transition-colors group">
      <td className="px-4 py-3">
        <p className="font-medium text-slate-800 text-sm">{item.title}</p>
        <p className="text-xs text-slate-400 font-mono mt-0.5">{item.handle}</p>
      </td>
      <td className="px-4 py-3 text-sm text-slate-500 max-w-xs truncate">{item.description ?? <span className="italic text-slate-300">—</span>}</td>
      <td className="px-4 py-3 text-center">
        <span className="inline-flex items-center justify-center rounded-full bg-brand-50 px-2.5 py-0.5 text-xs font-semibold text-brand-700">
          {item.memberCount}
        </span>
      </td>
      <td className="px-4 py-3 text-sm text-slate-400 whitespace-nowrap">
        {formatDistanceToNow(new Date(item.updatedAt), { addSuffix: true })}
      </td>
      <td className="px-4 py-3">
        <div className="flex items-center justify-end gap-1 opacity-0 group-hover:opacity-100 transition-opacity">
          <button onClick={onEdit} className="rounded px-2 py-1 text-xs font-medium text-brand-600 hover:bg-brand-50 transition-colors">Edit</button>
          <button onClick={onDelete} className="rounded px-2 py-1 text-xs font-medium text-red-500 hover:bg-red-50 transition-colors">Delete</button>
        </div>
      </td>
    </tr>
  );
}

// ─── GroupsTab ────────────────────────────────────────────────────────────────

export function GroupsTab({ contentType }: { contentType: ContentType }) {
  const qc = useQueryClient();
  const [showCreate, setShowCreate] = useState(false);
  const [editGroup, setEditGroup] = useState<EntryGroupDto | null>(null);
  const [deletingId, setDeletingId] = useState<string | null>(null);

  const { data: groups = [], isLoading } = useQuery<EntryGroupListItem[]>({
    queryKey: ['entry-groups', contentType.id],
    queryFn: () => entryGroupsApi.list(contentType.id),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => entryGroupsApi.delete(contentType.id, id),
    onSuccess: () => {
      toast.success('Group deleted');
      qc.invalidateQueries({ queryKey: ['entry-groups', contentType.id] });
      setDeletingId(null);
    },
    onError: (err: unknown) => {
      toast.error(err instanceof ApiError ? err.message : 'Failed to delete group');
      setDeletingId(null);
    },
  });

  /** Fetch full group detail when opening the edit modal. */
  const openEdit = async (id: string) => {
    try {
      const g = await entryGroupsApi.getById(contentType.id, id);
      setEditGroup(g);
    } catch {
      toast.error('Failed to load group details');
    }
  };

  return (
    <div className="space-y-4">
      {/* Toolbar */}
      <div className="flex items-start justify-between gap-4">
        <p className="text-sm text-slate-500 max-w-lg">
          Logical groupings of{' '}
          <span className="font-medium text-slate-700">{contentType.displayName}</span> entries.
          Assign a group handle on a{' '}
          <span className="font-mono text-xs bg-slate-100 px-1 py-0.5 rounded">MultiList</span> or{' '}
          <span className="font-mono text-xs bg-slate-100 px-1 py-0.5 rounded">Reference</span>{' '}
          field to filter available entries by this group.
        </p>
        <button onClick={() => setShowCreate(true)} className="btn-primary text-sm shrink-0">
          + New Group
        </button>
      </div>

      {/* Table */}
      {isLoading ? (
        <div className="space-y-2">
          {[1, 2, 3].map((i) => (
            <div key={i} className="h-14 animate-pulse rounded-lg bg-slate-100" />
          ))}
        </div>
      ) : groups.length === 0 ? (
        <div className="flex flex-col items-center justify-center gap-3 rounded-xl border-2 border-dashed border-slate-200 py-16 text-center">
          <span className="text-4xl">🗂️</span>
          <p className="font-medium text-slate-600">No groups yet</p>
          <p className="text-sm text-slate-400">Create your first group to start organising entries.</p>
          <button onClick={() => setShowCreate(true)} className="btn-secondary text-sm">+ New Group</button>
        </div>
      ) : (
        <div className="overflow-hidden rounded-xl border border-slate-200 bg-white">
          <table className="w-full text-left">
            <thead className="border-b border-slate-100 bg-slate-50">
              <tr>
                <th className="px-4 py-3 text-xs font-semibold uppercase tracking-wide text-slate-500">Title / Handle</th>
                <th className="px-4 py-3 text-xs font-semibold uppercase tracking-wide text-slate-500">Description</th>
                <th className="px-4 py-3 text-center text-xs font-semibold uppercase tracking-wide text-slate-500">Members</th>
                <th className="px-4 py-3 text-xs font-semibold uppercase tracking-wide text-slate-500">Updated</th>
                <th className="px-4 py-3" />
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {groups.map((g) => (
                <GroupRow
                  key={g.id}
                  item={g}
                  onEdit={() => openEdit(g.id)}
                  onDelete={() => setDeletingId(g.id)}
                />
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Create modal */}
      {showCreate && (
        <GroupModal
          contentType={contentType}
          onClose={() => setShowCreate(false)}
        />
      )}

      {/* Edit modal */}
      {editGroup && (
        <GroupModal
          contentType={contentType}
          group={editGroup}
          onClose={() => setEditGroup(null)}
        />
      )}

      {/* Delete confirm */}
      {deletingId && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
          <div className="w-full max-w-sm rounded-2xl bg-white p-6 shadow-2xl space-y-4">
            <div className="flex items-start gap-3">
              <span className="text-2xl">🗑️</span>
              <div>
                <h2 className="font-semibold text-slate-800">Delete Group?</h2>
                <p className="mt-1 text-sm text-slate-500">This permanently removes the group and its membership data. Entries themselves are not deleted.</p>
              </div>
            </div>
            <div className="flex justify-end gap-2">
              <button className="btn-secondary" onClick={() => setDeletingId(null)} disabled={deleteMutation.isPending}>Cancel</button>
              <button
                className="btn-danger"
                onClick={() => deleteMutation.mutate(deletingId)}
                disabled={deleteMutation.isPending}
              >
                {deleteMutation.isPending ? 'Deleting…' : 'Delete'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
