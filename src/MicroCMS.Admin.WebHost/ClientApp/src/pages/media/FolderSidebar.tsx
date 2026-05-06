import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { mediaApi } from '@/api/media';
import type { MediaFolder } from '@/types';
import { ApiError } from '@/api/client';

// ─── FolderTreeNode ───────────────────────────────────────────────────────────

function FolderTreeNode({
  folder,
  depth,
  activeFolderId,
  onSelect,
  renamingId,
  renameValue,
  setRenameValue,
  onStartRename,
  onCommitRename,
  onDelete,
  creatingUnder,
  setCreatingUnder,
}: {
  folder: MediaFolder;
  depth: number;
  activeFolderId: string | null;
  onSelect: (id: string | null, name?: string) => void;
  renamingId: string | null;
  renameValue: string;
  setRenameValue: (v: string) => void;
  onStartRename: (f: MediaFolder) => void;
  onCommitRename: (id: string) => void;
  onDelete: (id: string, name: string) => void;
  creatingUnder: string | null;
  setCreatingUnder: (id: string | null) => void;
}) {
  const qc = useQueryClient();
  const [expanded, setExpanded] = useState(activeFolderId === folder.id);
  const [newChildName, setNewChildName] = useState('');
  const hasChildren = folder.childCount > 0 || creatingUnder === folder.id;

  const { data: children = [], isLoading: loadingChildren } = useQuery({
    queryKey: ['media-folders', folder.id],
    queryFn: () => mediaApi.listFolders(folder.id),
    enabled: expanded,
  });

  const createChildMutation = useMutation({
    mutationFn: () => mediaApi.createFolder(newChildName.trim(), folder.id),
    onSuccess: () => {
      toast.success('Folder created.');
      setCreatingUnder(null);
      setNewChildName('');
      void qc.invalidateQueries({ queryKey: ['media-folders', folder.id] });
      void qc.invalidateQueries({ queryKey: ['media-folders', null] });
    },
    onError: (err) =>
      toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Create failed.'),
  });

  const isActive = activeFolderId === folder.id;
  const indentPx = 12 + depth * 14;

  return (
    <div>
      <div
        className={`group flex items-center gap-1 rounded-lg py-1.5 text-sm transition-colors ${isActive ? 'bg-brand-50 text-brand-700' : 'text-slate-600 hover:bg-slate-100'}`}
        style={{ paddingLeft: `${indentPx}px`, paddingRight: '6px' }}
      >
        <button
          onClick={() => setExpanded((v) => !v)}
          className={`mr-0.5 flex-shrink-0 text-slate-400 transition-transform ${expanded ? 'rotate-90' : ''} ${hasChildren ? 'visible' : 'invisible'}`}
        >
          <svg className="h-3 w-3" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
          </svg>
        </button>

        {renamingId === folder.id ? (
          <input
            autoFocus
            value={renameValue}
            onChange={(e) => setRenameValue(e.target.value)}
            onBlur={() => onCommitRename(folder.id)}
            onKeyDown={(e) => {
              if (e.key === 'Enter') onCommitRename(folder.id);
              if (e.key === 'Escape') onStartRename({ ...folder, name: '' } as MediaFolder);
            }}
            className="form-input h-6 flex-1 py-0 text-xs"
          />
        ) : (
          <button
            className="flex flex-1 items-center gap-1.5 truncate text-left"
            onClick={() => { onSelect(folder.id, folder.name); setExpanded(true); }}
          >
            <svg className="h-4 w-4 flex-shrink-0 text-amber-400" fill="currentColor" viewBox="0 0 20 20">
              <path d="M2 6a2 2 0 012-2h5l2 2h5a2 2 0 012 2v6a2 2 0 01-2 2H4a2 2 0 01-2-2V6z" />
            </svg>
            <span className="truncate">{folder.name}</span>
            {folder.assetCount > 0 && (
              <span className="ml-auto flex-shrink-0 text-xs text-slate-400">{folder.assetCount}</span>
            )}
          </button>
        )}

        {renamingId !== folder.id && (
          <div className="hidden gap-0.5 group-hover:flex">
            <button
              onClick={(e) => { e.stopPropagation(); setCreatingUnder(folder.id); setExpanded(true); }}
              className="rounded p-0.5 text-slate-400 hover:text-brand-600"
              title="New subfolder"
            >
              <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
              </svg>
            </button>
            <button
              onClick={(e) => { e.stopPropagation(); onStartRename(folder); }}
              className="rounded p-0.5 text-slate-400 hover:text-brand-600"
              title="Rename"
            >
              <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
              </svg>
            </button>
            <button
              onClick={(e) => { e.stopPropagation(); onDelete(folder.id, folder.name); }}
              className="rounded p-0.5 text-slate-400 hover:text-red-600"
              title="Delete"
            >
              <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 7l-.867 12.142A2 2 0 0116.138 21H7.862a2 2 0 01-1.995-1.858L5 7m5 4v6m4-6v6m1-10V4a1 1 0 00-1-1h-4a1 1 0 00-1 1v3M4 7h16" />
              </svg>
            </button>
          </div>
        )}
      </div>

      {expanded && (
        <div>
          {loadingChildren && (
            <div className="space-y-1 py-1" style={{ paddingLeft: `${indentPx + 18}px` }}>
              {[1, 2].map((i) => <div key={i} className="h-6 animate-pulse rounded bg-slate-100" />)}
            </div>
          )}
          {creatingUnder === folder.id && (
            <form
              className="flex items-center gap-1 py-1"
              style={{ paddingLeft: `${indentPx + 18}px`, paddingRight: '6px' }}
              onSubmit={(e) => { e.preventDefault(); if (newChildName.trim()) createChildMutation.mutate(); }}
            >
              <input
                autoFocus
                value={newChildName}
                onChange={(e) => setNewChildName(e.target.value)}
                placeholder="Subfolder name…"
                className="form-input h-7 flex-1 py-0 text-xs"
                onKeyDown={(e) => { if (e.key === 'Escape') setCreatingUnder(null); }}
              />
              <button type="submit" disabled={createChildMutation.isPending || !newChildName.trim()}
                className="rounded bg-brand-600 px-1.5 py-1 text-xs text-white hover:bg-brand-700 disabled:opacity-40">✓</button>
              <button type="button" onClick={() => setCreatingUnder(null)}
                className="rounded px-1 py-1 text-xs text-slate-400 hover:text-slate-600">✕</button>
            </form>
          )}
          {children.map((child) => (
            <FolderTreeNode
              key={child.id}
              folder={child}
              depth={depth + 1}
              activeFolderId={activeFolderId}
              onSelect={onSelect}
              renamingId={renamingId}
              renameValue={renameValue}
              setRenameValue={setRenameValue}
              onStartRename={onStartRename}
              onCommitRename={onCommitRename}
              onDelete={onDelete}
              creatingUnder={creatingUnder}
              setCreatingUnder={setCreatingUnder}
            />
          ))}
        </div>
      )}
    </div>
  );
}

// ─── FolderSidebar ────────────────────────────────────────────────────────────

export function FolderSidebar({
  activeFolderId,
  onSelect,
}: {
  activeFolderId: string | null;
  onSelect: (id: string | null, name?: string) => void;
}) {
  const qc = useQueryClient();
  const [creating, setCreating] = useState(false);
  const [newName, setNewName] = useState('');
  const [renamingId, setRenamingId] = useState<string | null>(null);
  const [renameValue, setRenameValue] = useState('');
  const [creatingUnder, setCreatingUnder] = useState<string | null>(null);

  const { data: rootFolders = [], isLoading } = useQuery({
    queryKey: ['media-folders', null],
    queryFn: () => mediaApi.listFolders(undefined),
  });

  const createMutation = useMutation({
    mutationFn: () => mediaApi.createFolder(newName.trim()),
    onSuccess: () => {
      toast.success('Folder created.');
      setCreating(false);
      setNewName('');
      void qc.invalidateQueries({ queryKey: ['media-folders', null] });
    },
    onError: (err) =>
      toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Create failed.'),
  });

  const renameMutation = useMutation({
    mutationFn: (id: string) => mediaApi.renameFolder(id, renameValue.trim()),
    onSuccess: () => {
      toast.success('Folder renamed.');
      setRenamingId(null);
      void qc.invalidateQueries({ queryKey: ['media-folders'] });
    },
    onError: (err) =>
      toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Rename failed.'),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => mediaApi.deleteFolder(id),
    onSuccess: (_, id) => {
      toast.success('Folder deleted.');
      if (activeFolderId === id) onSelect(null);
      void qc.invalidateQueries({ queryKey: ['media-folders'] });
    },
    onError: (err) =>
      toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Delete failed.'),
  });

  function startRename(folder: MediaFolder) {
    setRenamingId(folder.id);
    setRenameValue(folder.name);
  }

  function commitRename(id: string) {
    if (renameValue.trim()) renameMutation.mutate(id);
    else setRenamingId(null);
  }

  function handleDelete(id: string, name: string) {
    if (confirm(`Delete folder "${name}"? Assets inside will be moved to root.`))
      deleteMutation.mutate(id);
  }

  return (
    <aside className="w-56 flex-shrink-0 space-y-1 overflow-y-auto overflow-x-hidden">
      <button
        onClick={() => onSelect(null)}
        className={`flex w-full items-center gap-2 rounded-lg px-3 py-2 text-sm font-medium transition-colors ${
          activeFolderId === null ? 'bg-brand-50 text-brand-700' : 'text-slate-600 hover:bg-slate-100'
        }`}
      >
        <svg className="h-4 w-4 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2}
            d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
        </svg>
        All Assets
      </button>

      <div className="flex items-center justify-between px-3 pt-3 pb-1">
        <span className="text-xs font-semibold uppercase tracking-wide text-slate-400">Folders</span>
        <button
          onClick={() => setCreating(true)}
          className="rounded p-0.5 text-slate-400 hover:bg-slate-100 hover:text-brand-600"
          title="New root folder"
        >
          <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
          </svg>
        </button>
      </div>

      {creating && (
        <form
          className="flex items-center gap-1 px-2"
          onSubmit={(e) => { e.preventDefault(); if (newName.trim()) createMutation.mutate(); }}
        >
          <input
            autoFocus
            value={newName}
            onChange={(e) => setNewName(e.target.value)}
            placeholder="Folder name…"
            className="form-input h-7 flex-1 py-0 text-xs"
            onKeyDown={(e) => { if (e.key === 'Escape') setCreating(false); }}
          />
          <button type="submit" disabled={createMutation.isPending || !newName.trim()}
            className="rounded bg-brand-600 px-1.5 py-1 text-xs text-white hover:bg-brand-700 disabled:opacity-40">✓</button>
          <button type="button" onClick={() => setCreating(false)}
            className="rounded px-1 py-1 text-xs text-slate-400 hover:text-slate-600">✕</button>
        </form>
      )}

      {isLoading && (
        <div className="space-y-1 px-3">
          {[1, 2, 3].map((i) => <div key={i} className="h-7 animate-pulse rounded bg-slate-100" />)}
        </div>
      )}

      {!isLoading && rootFolders.length === 0 && !creating && (
        <p className="px-3 text-xs text-slate-400">No folders yet.</p>
      )}

      {rootFolders.map((folder) => (
        <FolderTreeNode
          key={folder.id}
          folder={folder}
          depth={0}
          activeFolderId={activeFolderId}
          onSelect={onSelect}
          renamingId={renamingId}
          renameValue={renameValue}
          setRenameValue={setRenameValue}
          onStartRename={startRename}
          onCommitRename={commitRename}
          onDelete={handleDelete}
          creatingUnder={creatingUnder}
          setCreatingUnder={setCreatingUnder}
        />
      ))}
    </aside>
  );
}
