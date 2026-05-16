import { useState } from 'react';
import toast from 'react-hot-toast';
import { contentTypesApi } from '@/api/contentTypes';

// ─── StaticTagInput ───────────────────────────────────────────────────────────

export function StaticTagInput({
    options,
    onChange,
}: {
    options: string[];
    onChange: (opts: string[]) => void;
}) {
    const [input, setInput] = useState('');

    const addTag = () => {
        const trimmed = input.trim();
        if (trimmed && !options.includes(trimmed)) onChange([...options, trimmed]);
        setInput('');
    };
    const removeTag = (opt: string) => onChange(options.filter((o) => o !== opt));

    return (
        <div className="space-y-2">
            <div className="flex flex-wrap gap-1.5 min-h-8">
                {options.map((opt) => (
                    <span key={opt} className="inline-flex items-center gap-1 rounded-full border border-brand-200 bg-brand-50 px-2.5 py-0.5 text-xs font-medium text-brand-700">
                        {opt}
                        <button type="button" onClick={() => removeTag(opt)} className="ml-0.5 text-brand-400 hover:text-brand-700">×</button>
                    </span>
                ))}
                {options.length === 0 && <p className="text-xs text-slate-400">No options yet.</p>}
            </div>
            <div className="flex gap-2">
                <input
                    type="text"
                    className="form-input flex-1 text-xs"
                    placeholder='Type an option and press Enter or ","'
                    value={input}
                    onChange={(e) => setInput(e.target.value)}
                    onKeyDown={(e) => { if (e.key === 'Enter' || e.key === ',') { e.preventDefault(); addTag(); } }}
                    onBlur={addTag}
                />
                <button type="button" onClick={addTag} className="btn-secondary text-xs">Add</button>
            </div>
            <p className="text-xs text-slate-400">
                Press <kbd className="rounded border border-slate-200 px-1 font-mono">Enter</kbd> or{' '}
                <kbd className="rounded border border-slate-200 px-1 font-mono">,</kbd> to add each option.
            </p>
        </div>
    );
}

// ─── DynamicSourceEditor ──────────────────────────────────────────────────────

export function DynamicSourceEditor({
    contentTypeId,
    fieldId,
    register,
    prefix,
}: {
    contentTypeId: string;
    fieldId?: string;
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    register: any;
    prefix: string;
}) {
    const [testing, setTesting] = useState(false);
    const [previewOptions, setPreviewOptions] = useState<Array<{ value: string; label: string }> | null>(null);

    const testQuery = async () => {
        if (!fieldId) { toast.error('Save the field first to test the dynamic source.'); return; }
        setTesting(true);
        try {
            const opts = await contentTypesApi.getEnumOptions(contentTypeId, fieldId);
            setPreviewOptions(opts);
            toast.success(`Resolved ${opts.length} option${opts.length !== 1 ? 's' : ''}.`);
        } catch {
            toast.error('Failed to resolve options. Check the source content type handle.');
            setPreviewOptions(null);
        } finally {
            setTesting(false);
        }
    };

    return (
        <div className="space-y-3 rounded-lg border border-dashed border-slate-300 bg-slate-50 p-3">
            <p className="text-xs font-semibold text-slate-600">Dynamic Source — queries published entries</p>
            <div className="grid grid-cols-2 gap-3">
                <div className="col-span-2">
                    <label className="form-label text-xs">Source Content Type Handle</label>
                    <input className="form-input mt-1 font-mono text-xs" placeholder="e.g. product, category" {...register(`${prefix}.contentTypeHandle`)} />
                </div>
                <div>
                    <label className="form-label text-xs">Label Field</label>
                    <input className="form-input mt-1 text-xs" placeholder="title" {...register(`${prefix}.labelField`)} />
                    <p className="mt-0.5 text-xs text-slate-400">Shown to authors in the dropdown.</p>
                </div>
                <div>
                    <label className="form-label text-xs">Value Field</label>
                    <input className="form-input mt-1 text-xs" placeholder="slug" {...register(`${prefix}.valueField`)} />
                    <p className="mt-0.5 text-xs text-slate-400">Stored in FieldsJson.</p>
                </div>
                <div>
                    <label className="form-label text-xs">Status Filter</label>
                    <select className="form-input mt-1 text-xs" {...register(`${prefix}.statusFilter`)}>
                        <option value="">All</option>
                        <option value="Published">Published</option>
                        <option value="Draft">Draft</option>
                        <option value="Approved">Approved</option>
                    </select>
                </div>
            </div>
            <div className="flex items-center gap-3">
                <button type="button" onClick={testQuery} disabled={testing} className="btn-secondary text-xs disabled:opacity-50">
                    {testing ? 'Testing…' : '▶ Test Query'}
                </button>
                {previewOptions !== null && (
                    <span className="text-xs text-slate-500">{previewOptions.length} option{previewOptions.length !== 1 ? 's' : ''} found</span>
                )}
            </div>
            {previewOptions !== null && previewOptions.length > 0 && (
                <div className="mt-1 flex flex-wrap gap-1">
                    {previewOptions.slice(0, 20).map((o) => (
                        <span key={o.value} className="inline-flex items-center gap-1 rounded border border-slate-200 bg-white px-2 py-0.5 text-xs text-slate-700" title={`value: ${o.value}`}>
                            {o.label} <span className="font-mono text-slate-400">({o.value})</span>
                        </span>
                    ))}
                    {previewOptions.length > 20 && <span className="text-xs text-slate-400">+{previewOptions.length - 20} more…</span>}
                </div>
            )}
            {previewOptions !== null && previewOptions.length === 0 && (
                <p className="text-xs text-amber-600">No published entries found for this source.</p>
            )}
        </div>
    );
}

// ─── EnumOptionsEditor ────────────────────────────────────────────────────────

export function EnumOptionsEditor({
    contentTypeId,
    fieldId,
    enumMode,
    staticOptions,
    onEnumModeChange,
    onStaticOptionsChange,
    register,
    prefix,
}: {
    contentTypeId: string;
    fieldId?: string;
    enumMode: 'static' | 'dynamic';
    staticOptions: string[];
    onEnumModeChange: (mode: 'static' | 'dynamic') => void;
    onStaticOptionsChange: (opts: string[]) => void;
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    register: any;
    prefix: string;
}) {
    return (
        <div className="col-span-2 space-y-3 rounded-lg border border-amber-100 bg-amber-50 p-3">
            <div className="flex items-center justify-between">
                <p className="text-xs font-semibold text-amber-800">Enum Options</p>
                <div className="flex overflow-hidden rounded-md border border-amber-200 text-xs">
                    <button type="button" onClick={() => onEnumModeChange('static')} className={`px-3 py-1 font-medium transition-colors ${enumMode === 'static' ? 'bg-amber-600 text-white' : 'bg-white text-amber-700 hover:bg-amber-100'}`}>Static list</button>
                    <button type="button" onClick={() => onEnumModeChange('dynamic')} className={`border-l border-amber-200 px-3 py-1 font-medium transition-colors ${enumMode === 'dynamic' ? 'bg-amber-600 text-white' : 'bg-white text-amber-700 hover:bg-amber-100'}`}>Dynamic query</button>
                </div>
            </div>
            {enumMode === 'static' ? (
                <StaticTagInput options={staticOptions} onChange={onStaticOptionsChange} />
            ) : (
                <DynamicSourceEditor contentTypeId={contentTypeId} fieldId={fieldId} register={register} prefix={prefix} />
            )}
        </div>
    );
}

// ─── ReferenceSourceEditor ────────────────────────────────────────────────────

export function ReferenceSourceEditor({
    register,
    prefix,
    errors,
}: {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    register: any;
    prefix: string;
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    errors?: any;
}) {
    const handleError = errors?.contentTypeHandle;
    return (
        <div className={`col-span-2 space-y-3 rounded-lg border p-3 ${handleError ? 'border-red-300 bg-red-50' : 'border-dashed border-slate-300 bg-slate-50'}`}>
            <p className="text-xs font-semibold text-slate-600">Reference Source — which entries to show in the picker</p>
            <div className="grid grid-cols-2 gap-3">
                <div className="col-span-2">
                    <label className="form-label text-xs">Source Content Type Handle</label>
                    <input className={`form-input mt-1 font-mono text-xs ${handleError ? 'border-red-400 focus:ring-red-400' : ''}`} placeholder="e.g. page, author, category" {...register(`${prefix}.contentTypeHandle`)} />
                    {handleError ? <p className="mt-0.5 text-xs text-red-600">{handleError.message}</p> : <p className="mt-0.5 text-xs text-slate-400">The handle of the content type to pick entries from.</p>}
                </div>
                <div className="col-span-2">
                    <label className="form-label text-xs">Status Filter</label>
                    <select className="form-input mt-1 text-xs" {...register(`${prefix}.statusFilter`)}>
                        <option value="">All</option>
                        <option value="Published">Published</option>
                        <option value="Draft">Draft</option>
                        <option value="Approved">Approved</option>
                    </select>
                </div>
            </div>
        </div>
    );
}

// ─── ComponentSourceEditor

export function ComponentSourceEditor({
    register,
    prefix,
    errors,
}: {
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    register: any;
    prefix: string;
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    errors?: any;
}) {
    const keyError = errors;
    return (
        <div className={`col-span-2 space-y-2 rounded-lg border p-3 ${keyError ? 'border-red-300 bg-red-50' : 'border-dashed border-violet-300 bg-violet-50'}`}>
            <p className="text-xs font-semibold text-violet-800">Component Source — which component type to pick items from</p>
            <div>
                <label className="form-label text-xs">Component Key <span className="text-red-500">*</span></label>
                <input
                    className={`form-input mt-1 font-mono text-xs ${keyError ? 'border-red-400 focus:ring-red-400' : ''}`}
                    placeholder="e.g. hero-banner, button"
                    {...register(prefix)}
                />
                {keyError
                    ? <p className="mt-0.5 text-xs text-red-600">{keyError.message}</p>
                    : <p className="mt-0.5 text-xs text-slate-400">The key of the component whose items will appear in the picker.</p>
                }
            </div>
        </div>
    );
}

export function MultiListSourceEditor({
    contentTypeId,
    fieldId,
    register,
    prefix,
    errors,
}: {
    contentTypeId: string;
    fieldId?: string;
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    register: any;
    prefix: string;
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    errors?: any;
}) {
    const [testing, setTesting] = useState(false);
    const [previewOptions, setPreviewOptions] = useState<Array<{ entryId: string; label: string; slug: string }> | null>(null);

    const testQuery = async () => {
        if (!fieldId) { toast.error('Save the field first to test the source.'); return; }
        setTesting(true);
        try {
            const opts = await contentTypesApi.getMultiListOptions(contentTypeId, fieldId);
            setPreviewOptions(opts);
            toast.success(`Resolved ${opts.length} entr${opts.length !== 1 ? 'ies' : 'y'}.`);
        } catch {
            toast.error('Failed to resolve entries. Check the source content type handle.');
            setPreviewOptions(null);
        } finally {
            setTesting(false);
        }
    };

    const handleError = errors?.contentTypeHandle;
    return (
        <div className={`col-span-2 space-y-3 rounded-lg border p-3 ${handleError ? 'border-red-300 bg-red-50' : 'border-dashed border-cyan-300 bg-cyan-50'}`}>
            <p className="text-xs font-semibold text-cyan-800">Multi List Source — entries for the dual-pane picker</p>
            <div className="grid grid-cols-2 gap-3">
                <div className="col-span-2">
                    <label className="form-label text-xs">Source Content Type Handle <span className="text-red-500">*</span></label>
                    <input className={`form-input mt-1 font-mono text-xs ${handleError ? 'border-red-400 focus:ring-red-400' : ''}`} placeholder="e.g. country, product" {...register(`${prefix}.contentTypeHandle`)} />
                    {handleError ? <p className="mt-0.5 text-xs text-red-600">{handleError.message}</p> : <p className="mt-0.5 text-xs text-slate-400">The handle of the content type to pick entries from.</p>}
                </div>
                <div>
                    <label className="form-label text-xs">Label Field</label>
                    <input className="form-input mt-1 text-xs" placeholder="title" {...register(`${prefix}.labelField`)} />
                    <p className="mt-0.5 text-xs text-slate-400">Shown in the picker list.</p>
                </div>
                <div>
                    <label className="form-label text-xs">Status Filter</label>
                    <select className="form-input mt-1 text-xs" {...register(`${prefix}.statusFilter`)}>
                        <option value="">All</option>
                        <option value="Published">Published</option>
                        <option value="Draft">Draft</option>
                        <option value="Approved">Approved</option>
                    </select>
                </div>
                <div className="col-span-2">
                    <label className="form-label text-xs">Group Handle <span className="text-slate-400">(optional)</span></label>
                    <input className="form-input mt-1 font-mono text-xs" placeholder="e.g. g7-countries" {...register(`${prefix}.groupHandle`)} />
                    <p className="mt-0.5 text-xs text-slate-400">Restrict the picker to entries belonging to a named group. Leave blank to show all entries.</p>
                </div>
            </div>
            <div className="flex items-center gap-3">
                <button type="button" onClick={testQuery} disabled={testing} className="btn-secondary text-xs disabled:opacity-50">
                    {testing ? 'Testing…' : '▶ Test Query'}
                </button>
                {previewOptions !== null && <span className="text-xs text-slate-500">{previewOptions.length} entr{previewOptions.length !== 1 ? 'ies' : 'y'} found</span>}
            </div>
            {previewOptions !== null && previewOptions.length > 0 && (
                <div className="mt-1 flex flex-wrap gap-1">
                    {previewOptions.slice(0, 20).map((o) => (
                        <span key={o.entryId} className="inline-flex items-center gap-1 rounded border border-slate-200 bg-white px-2 py-0.5 text-xs text-slate-700" title={`slug: ${o.slug}`}>
                            {o.label} <span className="font-mono text-slate-400">({o.slug})</span>
                        </span>
                    ))}
                    {previewOptions.length > 20 && <span className="text-xs text-slate-400">+{previewOptions.length - 20} more…</span>}
                </div>
            )}
            {previewOptions !== null && previewOptions.length === 0 && (
                <p className="text-xs text-amber-600">No entries found. Check the content type handle, status filter, and group handle.</p>
            )}
        </div>
    );
}
