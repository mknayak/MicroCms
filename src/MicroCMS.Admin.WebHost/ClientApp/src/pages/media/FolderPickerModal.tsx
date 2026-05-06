import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { mediaApi } from '@/api/media';
import type { MediaFolder } from '@/types';

function FolderPickerItem({
  folder,
  depth,
  currentFolderId,
  onPick,
}: {
  folder: MediaFolder;
  depth: number;
  currentFolderId?: string | null;
  onPick: (folderId: string | null) => void;
}) {
  const [expanded, setExpanded] = useState(false);
  const { data: children = [], isLoading } = useQuery({
    queryKey: ['media-folders', folder.id],
    queryFn: () => mediaApi.listFolders(folder.id),
    enabled: expanded,
  });
  const hasChildren = folder.childCount > 0;

  return (
    <div>
      <div
        className={`flex w-full items-center gap-1 rounded-lg px-2 py-1.5 text-sm hover:bg-slate-100 ${currentFolderId === folder.id ? 'font-semibold text-brand-700' : 'text-slate-700'}`}
        style={{ paddingLeft: `${8 + depth * 16}px` }}
      >
        <button
          onClick={() => setExpanded((v) => !v)}
          className={`mr-0.5 text-slate-400 transition-transform ${expanded ? 'rotate-90' : ''} ${hasChildren ? 'visible' : 'invisible'}`}
        >
          <svg className="h-3 w-3" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
          </svg>
        </button>
        <button onClick={() => onPick(folder.id)} className="flex flex-1 items-center gap-2 text-left">
          <svg className="h-4 w-4 flex-shrink-0 text-amber-400" fill="currentColor" viewBox="0 0 20 20">
            <path d="M2 6a2 2 0 012-2h5l2 2h5a2 2 0 012 2v6a2 2 0 01-2 2H4a2 2 0 01-2-2V6z" />
          </svg>
          <span className="truncate">{folder.name}</span>
        </button>
      </div>
      {expanded && (
        <div>
          {isLoading && <p className="px-3 py-1 text-xs text-slate-400" style={{ paddingLeft: `${24 + depth * 16}px` }}>Loading…</p>}
          {children.map((child) => (
            <FolderPickerItem key={child.id} folder={child} depth={depth + 1} currentFolderId={currentFolderId} onPick={onPick} />
          ))}
        </div>
      )}
    </div>
  );
}

export function FolderPickerModal({
  currentFolderId,
  onPick,
  onClose,
}: {
  currentFolderId?: string | null;
  onPick: (folderId: string | null) => void;
  onClose: () => void;
}) {
  const { data: rootFolders = [], isLoading } = useQuery({
    queryKey: ['media-folders', null],
    queryFn: () => mediaApi.listFolders(undefined),
  });

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
      <div className="w-80 rounded-xl border border-slate-200 bg-white shadow-xl">
        <div className="flex items-center justify-between border-b border-slate-200 px-4 py-3">
          <h3 className="text-sm font-semibold text-slate-900">Move to folder</h3>
          <button onClick={onClose} className="text-slate-400 hover:text-slate-600">✕</button>
        </div>
        <div className="max-h-72 overflow-y-auto p-2">
          <button
            onClick={() => onPick(null)}
            className={`flex w-full items-center gap-2 rounded-lg px-3 py-2 text-sm hover:bg-slate-100 ${currentFolderId === null ? 'font-semibold text-brand-700' : 'text-slate-700'}`}
          >
            <svg className="h-4 w-4 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
            </svg>
            All Assets (root)
          </button>
          {isLoading && <p className="px-3 py-2 text-xs text-slate-400">Loading…</p>}
          {rootFolders.map((f) => (
            <FolderPickerItem key={f.id} folder={f} depth={0} currentFolderId={currentFolderId} onPick={onPick} />
          ))}
        </div>
      </div>
    </div>
  );
}
