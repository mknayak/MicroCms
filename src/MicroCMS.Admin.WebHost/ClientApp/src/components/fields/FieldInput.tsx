/**
 * Shared field-value renderer used by both the Entry editor and the Component
 * Item editor.  Pass a field descriptor that satisfies AnyFieldDefinition and
 * the current value; the component will pick the right input widget.
 *
 * contentTypeId + fieldId are needed for the MultiList dual-pane picker when
 * the options are served by the dedicated endpoint
 * (GET /content-types/{id}/fields/{fieldId}/multilist-options).
 * When they are absent the component falls back to a handle-based entry search.
 */

import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { contentTypesApi } from '@/api/contentTypes';
import { entriesApi } from '@/api/entries';
import { RichTextEditor } from '@/components/ui/RichTextEditor';
import { MediaPickerField } from '@/pages/pages/MediaPickerField';
import { EntryPickerField } from '@/pages/pages/EntryPickerField';
import { ComponentItemPickerField } from '@/pages/components/ComponentItemPickerField';
import type { FieldDynamicSource, MultiListOptionDto } from '@/types';

// ─── Shared field descriptor ──────────────────────────────────────────────────

/** Minimal field shape shared by FieldDefinitionDto and ComponentFieldDefinition. */
export interface AnyFieldDefinition {
  id?: string;
  handle: string;
  label: string;
  fieldType: string;
  isList: boolean;
  isRequired?: boolean;
  description?: string;
  options?: string[];
  dynamicSource?: FieldDynamicSource;
  multiListSource?: FieldDynamicSource;
}

// ─── Char Counter ─────────────────────────────────────────────────────────────

export function CharCounter({ value, max }: { value: string; max: number }) {
  const len = typeof value === 'string' ? value.length : 0;
  return (
    <span className={`text-xs tabular-nums ${len > max ? 'text-red-500 font-semibold' : len > max * 0.85 ? 'text-amber-500' : 'text-slate-400'}`}>
      {len} / {max} chars
    </span>
  );
}

// ─── MultiList dual-pane picker ───────────────────────────────────────────────

function MultiListPickerField({
  field,
  value,
  onChange,
  contentTypeId,
}: {
  field: AnyFieldDefinition;
  value: unknown;
  onChange: (val: unknown) => void;
  contentTypeId?: string;
}) {
  const selectedIds: string[] = Array.isArray(value) ? value.map(String) : [];
  const [availableSearch, setAvailableSearch] = useState('');
  const [selectedSearch, setSelectedSearch] = useState('');

  // If we have a dedicated endpoint, use it; otherwise fall back to entry search
  const handle = field.multiListSource?.contentTypeHandle ?? field.dynamicSource?.contentTypeHandle ?? '';
  const useEndpoint = Boolean(contentTypeId && field.id);

  const { data: endpointOptions = [], isLoading: endpointLoading } = useQuery<MultiListOptionDto[]>({
    queryKey: ['multilist-options', contentTypeId, field.id],
    queryFn: () => contentTypesApi.getMultiListOptions(contentTypeId!, field.id!),
    enabled: useEndpoint,
    staleTime: 30_000,
  });

  const { data: searchResults = [], isLoading: searchLoading } = useQuery({
    queryKey: ['multilist-search', handle, availableSearch],
    queryFn: () => entriesApi.list({ search: availableSearch, pageSize: 100 }),
    select: (d) => d.items.map((e) => ({ entryId: e.id, label: e.title ?? e.slug })) as MultiListOptionDto[],
    enabled: !useEndpoint && !!handle,
    staleTime: 30_000,
  });

  const isLoading = useEndpoint ? endpointLoading : searchLoading;
  const allOptions: MultiListOptionDto[] = useEndpoint ? endpointOptions : searchResults;

  const available = allOptions.filter((o) => !selectedIds.includes(o.entryId));
  const selected = allOptions.filter((o) => selectedIds.includes(o.entryId));

  const filteredAvailable = availableSearch.trim()
    ? available.filter((o) => o.label.toLowerCase().includes(availableSearch.toLowerCase()))
    : available;
  const filteredSelected = selectedSearch.trim()
    ? selected.filter((o) => o.label.toLowerCase().includes(selectedSearch.toLowerCase()))
    : selected;

  if (!handle && !useEndpoint) {
    return <p className="text-xs text-slate-400">No source content type configured for this MultiList field.</p>;
  }
  if (isLoading) return <div className="h-24 animate-pulse rounded-lg bg-slate-100" />;

  return (
    <div className="grid grid-cols-2 gap-3">
      {/* Available */}
      <div className="flex max-h-80 min-h-28 flex-col rounded-lg border border-slate-200">
        <div className="shrink-0 border-b border-slate-100 px-3 py-2">
          <div className="mb-1.5 flex items-center justify-between">
            <span className="text-xs font-medium text-slate-500">Available ({available.length})</span>
          </div>
          <input type="text" value={availableSearch} onChange={(e) => setAvailableSearch(e.target.value)}
            placeholder="Search…"
            className="w-full rounded border border-slate-200 bg-white px-2 py-1 text-xs placeholder:text-slate-300 focus:border-brand-400 focus:outline-none focus:ring-1 focus:ring-brand-400" />
        </div>
        <ul className="flex-1 divide-y divide-slate-50 overflow-y-auto">
          {filteredAvailable.length === 0 && (
            <li className="px-3 py-3 text-xs italic text-slate-400">{availableSearch.trim() ? 'No matches' : 'No items available'}</li>
          )}
          {filteredAvailable.map((o) => (
            <li key={o.entryId} className="flex items-center justify-between px-3 py-2 hover:bg-slate-50">
              <span className="truncate text-sm text-slate-700">{o.label}</span>
              <button type="button" onClick={() => onChange([...selectedIds, o.entryId])}
                className="ml-2 shrink-0 text-xs font-bold text-brand-600 hover:text-brand-800" aria-label={`Add ${o.label}`}>→</button>
            </li>
          ))}
        </ul>
      </div>
      {/* Selected */}
      <div className="flex max-h-80 min-h-28 flex-col rounded-lg border border-brand-200 bg-brand-50/30">
        <div className="shrink-0 border-b border-brand-100 px-3 py-2">
          <div className="mb-1.5 flex items-center justify-between">
            <span className="text-xs font-medium text-brand-700">Selected ({selected.length})</span>
          </div>
          <input type="text" value={selectedSearch} onChange={(e) => setSelectedSearch(e.target.value)}
            placeholder="Search…"
            className="w-full rounded border border-brand-200 bg-white px-2 py-1 text-xs placeholder:text-slate-300 focus:border-brand-400 focus:outline-none focus:ring-1 focus:ring-brand-400" />
        </div>
        <ul className="flex-1 divide-y divide-brand-50 overflow-y-auto">
          {filteredSelected.length === 0 && (
            <li className="px-3 py-3 text-xs italic text-brand-400">{selectedSearch.trim() ? 'No matches' : 'None selected'}</li>
          )}
          {filteredSelected.map((o) => (
            <li key={o.entryId} className="flex items-center justify-between px-3 py-2 hover:bg-brand-50">
              <button type="button" onClick={() => onChange(selectedIds.filter((id) => id !== o.entryId))}
                className="mr-2 shrink-0 text-xs font-bold text-slate-400 hover:text-red-500" aria-label={`Remove ${o.label}`}>←</button>
              <span className="flex-1 truncate text-sm text-slate-700">{o.label}</span>
            </li>
          ))}
        </ul>
      </div>
    </div>
  );
}

// ─── Scalar renderer (single value) ──────────────────────────────────────────

function ScalarFieldInput({
  field,
  value,
  onChange,
  contentTypeId,
}: {
  field: AnyFieldDefinition;
  value: unknown;
  onChange: (val: unknown) => void;
  contentTypeId?: string;
}) {
  switch (field.fieldType) {
    case 'RichText':
      return (
        <div className="relative">
          <RichTextEditor value={typeof value === 'string' ? value : ''} onChange={onChange} placeholder={`Write ${field.label}…`} />
          <button type="button" className="absolute right-2 top-2 flex items-center gap-1 rounded-md border border-brand-200 bg-brand-50 px-2 py-1 text-xs font-medium text-brand-700 hover:bg-brand-100">
            <span>✦</span> AI Assist
          </button>
        </div>
      );
    case 'LongText':
    case 'Markdown':
      return <textarea value={typeof value === 'string' ? value : ''} onChange={(e) => onChange(e.target.value)} rows={6} className="form-input resize-y font-mono" placeholder={`Enter ${field.label}…`} />;
    case 'Json':
      return <textarea value={typeof value === 'string' ? value : JSON.stringify(value ?? {}, null, 2)} onChange={(e) => onChange(e.target.value)} rows={8} className="form-input resize-y font-mono text-xs" placeholder="{}" spellCheck={false} />;
    case 'Boolean':
      return (
        <label className="flex cursor-pointer items-center gap-2">
          <input type="checkbox" checked={Boolean(value)} onChange={(e) => onChange(e.target.checked)} className="h-4 w-4 rounded border-slate-300 text-brand-600" />
          <span className="text-sm text-slate-700">{field.label}</span>
        </label>
      );
    case 'Integer':
      return <input type="number" step="1" value={typeof value === 'number' ? value : ''} onChange={(e) => onChange(e.target.valueAsNumber)} className="form-input" placeholder="0" />;
    case 'Decimal':
      return <input type="number" step="any" value={typeof value === 'number' ? value : ''} onChange={(e) => onChange(e.target.valueAsNumber)} className="form-input" placeholder="0.00" />;
    case 'DateTime':
      return <input type="datetime-local" value={typeof value === 'string' ? value : ''} onChange={(e) => onChange(e.target.value)} className="form-input" />;
    case 'Color':
      return (
        <div className="flex items-center gap-3">
          <input type="color" value={typeof value === 'string' && value ? value : '#000000'} onChange={(e) => onChange(e.target.value)} className="h-9 w-16 cursor-pointer rounded border border-slate-300 p-0.5" />
          <input type="text" value={typeof value === 'string' ? value : ''} onChange={(e) => onChange(e.target.value)} className="form-input font-mono" placeholder="#000000" />
        </div>
      );
    case 'Enum':
      if (field.options && field.options.length > 0) {
        return (
          <select value={typeof value === 'string' ? value : ''} onChange={(e) => onChange(e.target.value)} className="form-input">
            <option value="">— select —</option>
            {field.options.map((opt) => <option key={opt} value={opt}>{opt}</option>)}
          </select>
        );
      }
      return <input type="text" value={typeof value === 'string' ? value : ''} onChange={(e) => onChange(e.target.value)} className="form-input" placeholder="Enter value…" />;
    case 'AssetReference':
      return <MediaPickerField value={value} onChange={onChange} />;
    case 'Reference':
      return <EntryPickerField value={value} onChange={onChange} source={field.dynamicSource} />;
    case 'Component':
      return <ComponentItemPickerField value={value} onChange={onChange} restrictToKey={field.dynamicSource?.contentTypeHandle} />;
    case 'Location':
      return (
        <div className="grid grid-cols-2 gap-2">
          <input type="number" step="any"
            value={typeof value === 'object' && value !== null && 'lat' in value ? (value as { lat: number }).lat : ''}
            onChange={(e) => onChange({ ...(typeof value === 'object' && value !== null ? value : {}), lat: e.target.valueAsNumber })}
            className="form-input" placeholder="Latitude" />
          <input type="number" step="any"
            value={typeof value === 'object' && value !== null && 'lng' in value ? (value as { lng: number }).lng : ''}
            onChange={(e) => onChange({ ...(typeof value === 'object' && value !== null ? value : {}), lng: e.target.valueAsNumber })}
            className="form-input" placeholder="Longitude" />
        </div>
      );
    case 'MultiList':
      return <MultiListPickerField field={field} value={value} onChange={onChange} contentTypeId={contentTypeId} />;
    default:
      return <input type="text" value={typeof value === 'string' ? value : ''} onChange={(e) => onChange(e.target.value)} className="form-input" placeholder={`Enter ${field.label}…`} />;
  }
}

// ─── List wrapper ─────────────────────────────────────────────────────────────

function ListFieldInput({
  field,
  value,
  onChange,
  contentTypeId,
}: {
  field: AnyFieldDefinition;
  value: unknown;
  onChange: (val: unknown) => void;
  contentTypeId?: string;
}) {
  const items: unknown[] = Array.isArray(value) ? value : [];
  const update = (i: number, v: unknown) => { const n = [...items]; n[i] = v; onChange(n); };
  const add = () => onChange([...items, '']);
  const remove = (i: number) => onChange(items.filter((_, idx) => idx !== i));

  // Enum isList → checkbox multi-select
  if (field.fieldType === 'Enum' && field.options && field.options.length > 0) {
    const selected = items.map(String);
    const toggle = (opt: string) =>
      onChange(selected.includes(opt) ? selected.filter((o) => o !== opt) : [...selected, opt]);
    return (
      <div className="flex flex-wrap gap-2">
        {field.options.map((opt) => (
          <label key={opt} className="flex cursor-pointer items-center gap-2 rounded-lg border border-slate-200 px-3 py-1.5 text-sm hover:border-brand-300 hover:bg-brand-50">
            <input type="checkbox" checked={selected.includes(opt)} onChange={() => toggle(opt)} className="h-4 w-4 rounded border-slate-300 text-brand-600" />
            {opt}
          </label>
        ))}
      </div>
    );
  }

  const scalarField = { ...field, isList: false };
  return (
    <div className="space-y-2">
      {items.map((item, idx) => (
        <div key={idx} className="flex items-start gap-2">
          <div className="flex-1">
            <ScalarFieldInput field={scalarField} value={item} onChange={(v) => update(idx, v)} contentTypeId={contentTypeId} />
          </div>
          <button type="button" onClick={() => remove(idx)}
            className="mt-1 flex h-8 w-8 shrink-0 items-center justify-center rounded text-slate-300 hover:bg-red-50 hover:text-red-500" aria-label="Remove item">✕</button>
        </div>
      ))}
      <button type="button" onClick={add}
        className="flex items-center gap-1 text-xs font-medium text-brand-600 hover:text-brand-700">
        <span className="text-base leading-none">+</span> Add item
      </button>
      {items.length > 0 && <p className="text-xs text-slate-400">{items.length} item{items.length !== 1 ? 's' : ''}</p>}
    </div>
  );
}

// ─── Public API ───────────────────────────────────────────────────────────────

/**
 * Top-level field renderer.  Routes to the list wrapper or scalar renderer.
 *
 * @param field          Any field descriptor (FieldDefinitionDto or ComponentFieldDefinition).
 * @param value          Current form value.
 * @param onChange       Called with the new value.
 * @param contentTypeId  Optional — enables the MultiList dedicated endpoint.
 */
export function FieldInput({
  field,
  value,
  onChange,
  contentTypeId,
}: {
  field: AnyFieldDefinition;
  value: unknown;
  onChange: (val: unknown) => void;
  contentTypeId?: string;
}) {
  if (field.fieldType === 'MultiList') {
    return <MultiListPickerField field={field} value={value} onChange={onChange} contentTypeId={contentTypeId} />;
  }
  if (field.isList) {
    return <ListFieldInput field={field} value={value} onChange={onChange} contentTypeId={contentTypeId} />;
  }
  return <ScalarFieldInput field={field} value={value} onChange={onChange} contentTypeId={contentTypeId} />;
}
