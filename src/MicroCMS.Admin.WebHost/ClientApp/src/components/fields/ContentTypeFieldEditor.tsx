/* eslint-disable @typescript-eslint/no-explicit-any */
import type { UseFieldArrayReturn } from 'react-hook-form';
import { FIELD_TYPES } from './fieldConstants';
import type { FieldRowValue } from './fieldConstants';

// ─── Types ────────────────────────────────────────────────────────────────────

export interface ContentTypeFieldEditorProps {
  /** Bound field array from useFieldArray({ name: 'fields' }). */
  fieldArray: Pick<UseFieldArrayReturn<any, 'fields', 'id'>, 'fields' | 'remove' | 'move' | 'append'>;
  /** react-hook-form register function from the parent form. */
  register: (path: any, options?: any) => any;
  /** react-hook-form errors from the parent form. */
  errors: any;
  /** react-hook-form watch from the parent form. */
  watch: (path: any) => any;
  /** Index of the currently expanded field row (null = all collapsed). */
  activeFieldIdx: number | null;
  /** Called when a row header is clicked to expand / collapse. */
  onActiveFieldChange: (idx: number | null) => void;
}

// ─── Flags shown in the expanded row ─────────────────────────────────────────

const FLAGS: { key: keyof Pick<FieldRowValue, 'isRequired' | 'isLocalized' | 'isIndexed' | 'isUnique' | 'isList'>; label: string }[] = [
  { key: 'isRequired',  label: 'Required' },
  { key: 'isLocalized', label: 'Localized' },
  { key: 'isIndexed',   label: 'Indexed' },
  { key: 'isUnique',    label: 'Unique' },
  { key: 'isList',      label: 'List (multi-value)' },
];

// ─── Component ────────────────────────────────────────────────────────────────

/**
 * Reusable field-list editor used by both ContentTypeEditPage and ComponentEditorPage.
 *
 * The parent is responsible for:
 * - Calling useFieldArray({ control, name: 'fields' }) and passing the result as `fieldArray`.
 * - Rendering the "+ Add Field" button and calling fieldArray.append(...) when clicked.
 * - The outer card / section heading (so each page can style its own wrapper).
 */
export default function ContentTypeFieldEditor({
  fieldArray,
  register,
  errors,
  watch,
  activeFieldIdx,
  onActiveFieldChange,
}: ContentTypeFieldEditorProps) {
  const { fields, remove, move } = fieldArray;

  if (fields.length === 0) {
    return (
      <p className="text-sm text-slate-400">No fields yet. Click "+ Add Field" to start.</p>
    );
  }

  return (
    <div className="space-y-3">
      {fields.map((field, idx) => {
        const isExpanded = activeFieldIdx === idx;

        return (
          <div
            key={field.id}
            className={`rounded-lg border p-4 ${isExpanded ? 'border-brand-300 bg-brand-50/40' : 'border-slate-200'}`}
          >
            {/* ── Row header (collapsed view) ── */}
            <div
              className="flex cursor-pointer items-center justify-between"
              onClick={() => onActiveFieldChange(isExpanded ? null : idx)}
            >
              <div className="flex items-center gap-2">
                <span className="text-sm font-medium text-slate-800">
                  {watch(`fields.${idx}.name`) || (
                    <span className="text-slate-400">Unnamed field</span>
                  )}
                </span>
                <span className="badge-slate text-xs">{watch(`fields.${idx}.fieldType`)}</span>
                {watch(`fields.${idx}.isRequired`)  && <span className="badge-red text-xs">Required</span>}
                {watch(`fields.${idx}.isLocalized`) && <span className="badge-brand text-xs">Localized</span>}
                {watch(`fields.${idx}.isIndexed`)   && <span className="badge-amber text-xs">Indexed</span>}
              </div>

              <div className="flex items-center gap-2">
                <button
                  type="button"
                  onClick={(e) => { e.stopPropagation(); if (idx > 0) move(idx, idx - 1); }}
                  className="text-slate-400 hover:text-slate-600 disabled:opacity-30"
                  disabled={idx === 0}
                  aria-label="Move up"
                >↑</button>
                <button
                  type="button"
                  onClick={(e) => { e.stopPropagation(); if (idx < fields.length - 1) move(idx, idx + 1); }}
                  className="text-slate-400 hover:text-slate-600 disabled:opacity-30"
                  disabled={idx === fields.length - 1}
                  aria-label="Move down"
                >↓</button>
                <button
                  type="button"
                  onClick={(e) => { e.stopPropagation(); remove(idx); }}
                  className="text-red-400 hover:text-red-600"
                  aria-label="Remove field"
                >✕</button>
              </div>
            </div>

            {/* ── Expanded editor ── */}
            {isExpanded && (
              <div className="mt-4 space-y-3">
                <div className="grid grid-cols-2 gap-3">
                  {/* Name */}
                  <div>
                    <label className="form-label">Name</label>
                    <input
                      className="form-input mt-1"
                      {...register(`fields.${idx}.name`)}
                      placeholder="e.g. Hero Title"
                    />
                    {errors.fields?.[idx]?.name && (
                      <p className="form-error">{errors.fields[idx]?.name?.message}</p>
                    )}
                  </div>

                  {/* Type */}
                  <div>
                    <label className="form-label">Type</label>
                    <select className="form-input mt-1" {...register(`fields.${idx}.fieldType`)}>
                      {FIELD_TYPES.map((ft) => (
                        <option key={ft.value} value={ft.value}>{ft.label}</option>
                      ))}
                    </select>
                  </div>

                  {/* Flags */}
                  <div className="col-span-2 flex flex-wrap items-end gap-4 pb-1">
                    {FLAGS.map(({ key, label }) => (
                      <label key={key} className="flex items-center gap-2 text-sm text-slate-700">
                        <input
                          type="checkbox"
                          className="h-4 w-4 rounded border-slate-300 text-brand-600"
                          {...register(`fields.${idx}.${key}`)}
                        />
                        {label}
                      </label>
                    ))}
                  </div>

                  {/* Description */}
                  <div className="col-span-2">
                    <label className="form-label">Description</label>
                    <input
                      className="form-input mt-1 text-sm"
                      placeholder="Optional hint shown beneath this field in the item editor"
                      {...register(`fields.${idx}.description`)}
                    />
                  </div>
                </div>

                {/* Enum-specific options */}
                {watch(`fields.${idx}.fieldType`) === 'Enum' && (
                  <div className="rounded-lg border border-slate-200 bg-slate-50 p-3 space-y-3">
                    <p className="text-xs font-semibold text-slate-600">Enum Options</p>
                    <div className="flex gap-4">
                      <label className="flex items-center gap-1.5 text-sm text-slate-700">
                        <input type="radio" value="static" className="text-brand-600"
                          {...register(`fields.${idx}.enumMode`)} />
                        Static list
                      </label>
                      <label className="flex items-center gap-1.5 text-sm text-slate-700">
                        <input type="radio" value="dynamic" className="text-brand-600"
                          {...register(`fields.${idx}.enumMode`)} />
                        Dynamic source
                      </label>
                    </div>
                    {watch(`fields.${idx}.enumMode`) === 'static' ? (
                      <div>
                        <label className="form-label">Options (one per line)</label>
                        <textarea
                          className="form-input mt-1 font-mono text-xs"
                          rows={4}
                          placeholder={`option-one\noption-two\noption-three`}
                          {...register(`fields.${idx}.optionsText`)}
                        />
                        <p className="mt-1 text-[10px] text-slate-400">Each non-empty line becomes a selectable value.</p>
                      </div>
                    ) : (
                      <div>
                        <label className="form-label">Source Content Type Handle</label>
                        <input
                          className="form-input mt-1 font-mono text-xs"
                          placeholder="e.g. blog_post"
                          {...register(`fields.${idx}.referenceHandle`)}
                        />
                        <p className="mt-1 text-[10px] text-slate-400">Options are resolved at runtime from published entries of this type.</p>
                      </div>
                    )}
                  </div>
                )}

                {/* Reference / Component target */}
                {(watch(`fields.${idx}.fieldType`) === 'Reference' || watch(`fields.${idx}.fieldType`) === 'Component') && (
                  <div className="rounded-lg border border-slate-200 bg-slate-50 p-3 space-y-2">
                    <p className="text-xs font-semibold text-slate-600">
                      {watch(`fields.${idx}.fieldType`) === 'Component' ? 'Component Key' : 'Content Type Handle'}
                    </p>
                    <input
                      className="form-input font-mono text-xs"
                      placeholder={watch(`fields.${idx}.fieldType`) === 'Component' ? 'e.g. hero-banner' : 'e.g. blog_post'}
                      {...register(`fields.${idx}.referenceHandle`)}
                    />
                    <p className="text-[10px] text-slate-400">
                      {watch(`fields.${idx}.fieldType`) === 'Component'
                        ? 'Restricts the picker to items of this component type.'
                        : 'Restricts the picker to entries of this content type.'}
                    </p>
                  </div>
                )}
              </div>
            )}
          </div>
        );
      })}
    </div>
  );
}
