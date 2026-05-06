import { useState } from 'react';
import { FolderPickerModal } from './FolderPickerModal';

export function BulkToolbar({
  selectedIds,
  onDelete,
  onClear,
  onMoveTo,
}: {
  selectedIds: Set<string>;
  onDelete: () => void;
  onClear: () => void;
  onMoveTo: (folderId: string | null) => void;
}) {
  const [showPicker, setShowPicker] = useState(false);
  if (selectedIds.size === 0) return null;
  return (
    <>
      <div className="fixed bottom-6 left-1/2 z-50 flex -translate-x-1/2 items-center gap-3 rounded-xl border border-slate-200 bg-white px-4 py-3 shadow-xl">
        <span className="text-sm font-medium text-slate-700">{selectedIds.size} selected</span>
        <button onClick={() => setShowPicker(true)} className="btn-secondary text-sm">Move to…</button>
        <button onClick={onDelete} className="btn-danger text-sm">Delete</button>
        <button onClick={onClear} className="ml-1 text-xs text-slate-400 hover:text-slate-600">✕ Clear</button>
      </div>
      {showPicker && (
        <FolderPickerModal
          onPick={(folderId) => { setShowPicker(false); onMoveTo(folderId); }}
          onClose={() => setShowPicker(false)}
        />
      )}
    </>
  );
}
