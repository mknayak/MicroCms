import { useState, useEffect } from 'react';
import { Link } from 'react-router-dom';
import type { Entry, FieldDefinitionDto } from '@/types';

export function EntryFieldEditor({
  entry, fields, onSave, saving,
}: {
  entry: Entry;
  fields: FieldDefinitionDto[];
  onSave: (updated: Record<string, unknown>) => void;
  saving: boolean;
}) {
  const [draft, setDraft] = useState<Record<string, unknown>>({ ...entry.fields });

  useEffect(() => { setDraft({ ...entry.fields }); }, [entry.id]);

  const set = (key: string, value: unknown) =>
    setDraft((prev) => ({ ...prev, [key]: value }));

  const sortedFields = [...fields].sort((a, b) => a.sortOrder - b.sortOrder);
  const isDirty = JSON.stringify(draft) !== JSON.stringify(entry.fields);

return (
    <div className="flex flex-col gap-0 overflow-hidden">
      {/* Entry identity */}
   <div className="border-b border-slate-100 px-4 pb-3 pt-3">
  <div className="flex items-center justify-between">
  <div>
  <p className="text-xs font-semibold text-slate-700">{(entry.fields.title as string) ?? entry.slug}</p>
       <p className="font-mono text-[10px] text-slate-400">{entry.status} · v{entry.currentVersionNumber}</p>
     </div>
          <Link to={`/entries/${entry.id}`}
        className="flex items-center gap-1 text-[11px] font-semibold text-brand-600 hover:underline">
    Full editor
       <svg className="h-3 w-3" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14" />
          </svg>
        </Link>
  </div>
    </div>

      {/* Fields */}
      <div className="flex-1 space-y-3 overflow-y-auto px-4 py-3">
        {sortedFields.map((f) => {
          const val = draft[f.handle];
          const ft = f.fieldType.toLowerCase();
     return (
     <div key={f.id}>
      <label className="mb-1 flex items-center gap-1 text-[11px] font-semibold text-slate-600">
        {f.label}
   {f.isRequired && <span className="text-red-400">*</span>}
       {f.isLocalized && <span className="rounded bg-blue-50 px-1 py-0.5 text-[9px] font-bold text-blue-600">L10N</span>}
      </label>

   {(ft === 'shorttext' || ft === 'url' || ft === 'color') && (
        <input
     className="form-input text-xs"
          type={ft === 'url' ? 'url' : ft === 'color' ? 'color' : 'text'}
          value={(val as string) ?? ''}
                  onChange={(e) => set(f.handle, e.target.value)}
              />
   )}

              {(ft === 'longtext' || ft === 'richtext' || ft === 'markdown') && (
    <textarea
      className="form-input resize-none text-xs"
           rows={3}
           value={(val as string) ?? ''}
            onChange={(e) => set(f.handle, e.target.value)}
/>
    )}

  {(ft === 'integer' || ft === 'decimal') && (
         <input
        className="form-input text-xs"
 type="number"
        value={(val as number) ?? ''}
        onChange={(e) => set(f.handle, ft === 'integer' ? parseInt(e.target.value) : parseFloat(e.target.value))}
       />
         )}

           {ft === 'boolean' && (
                <label className="flex cursor-pointer items-center gap-2">
      <div className="relative">
     <input type="checkbox" className="sr-only" checked={!!(val)} onChange={(e) => set(f.handle, e.target.checked)} />
        <div className={`h-4 w-8 rounded-full transition-colors ${val ? 'bg-brand-600' : 'bg-slate-200'}`} />
     <div className={`absolute top-0.5 h-3 w-3 rounded-full bg-white shadow transition-transform ${val ? 'translate-x-4' : 'translate-x-0.5'}`} />
   </div>
    <span className="text-xs text-slate-600">{val ? 'Yes' : 'No'}</span>
        </label>
            )}

              {ft === 'datetime' && (
       <input
            className="form-input text-xs"
             type="datetime-local"
       value={val ? (val as string).slice(0, 16) : ''}
              onChange={(e) => set(f.handle, e.target.value ? new Date(e.target.value).toISOString() : null)}
          />
   )}

          {ft === 'enum' && f.options && (
        <select className="form-input text-xs" value={(val as string) ?? ''} onChange={(e) => set(f.handle, e.target.value)}>
   <option value="">— select —</option>
                  {f.options.map((o) => <option key={o} value={o}>{o}</option>)}
                </select>
          )}

       {(ft === 'assetreference' || ft === 'reference' || ft === 'json') && (
  <div className="rounded-md border border-dashed border-slate-200 px-3 py-2 text-[11px] text-slate-400">
       {ft === 'json' ? (
          <textarea className="w-full resize-none bg-transparent font-mono text-[10px] focus:outline-none" rows={2}
        value={typeof val === 'string' ? val : JSON.stringify(val ?? '', null, 2)}
     onChange={(e) => { try { set(f.handle, JSON.parse(e.target.value)); } catch { set(f.handle, e.target.value); } }} />
   ) : (
  <span>Edit in full editor → {f.fieldType}</span>
              )}
             </div>
  )}
    </div>
          );
        })}
    </div>

      {/* Save footer */}
   <div className="flex-shrink-0 border-t border-slate-200 px-4 py-3">
        <button
       onClick={() => onSave(draft)}
          disabled={saving || !isDirty}
          className="btn-primary w-full justify-center text-xs disabled:opacity-50"
        >
          {saving ? 'Saving…' : isDirty ? 'Save Entry Changes' : 'No Changes'}
        </button>
      </div>
    </div>
  );
}
