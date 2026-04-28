import { useEffect, useState } from 'react';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm, useFieldArray } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { formatDistanceToNow } from 'date-fns';
import toast from 'react-hot-toast';
import { contentTypesApi } from '@/api/contentTypes';
import type { ContentType, FieldDefinitionDto, FieldType } from '@/types';
import { ApiError } from '@/api/client';
import { FIELD_TYPE_COLORS, FIELD_TYPE_LABELS } from './contentTypeDetail.shared';
import { useSite } from '@/contexts/SiteContext';

// ─── Constants ────────────────────────────────────────────────────────────────

const FIELD_TYPES: { value: FieldType; label: string }[] = [
    { value: 'ShortText', label: 'Short Text' },
    { value: 'LongText', label: 'Long Text' },
    { value: 'RichText', label: 'Rich Text' },
    { value: 'Markdown', label: 'Markdown' },
    { value: 'Integer', label: 'Integer' },
    { value: 'Decimal', label: 'Decimal' },
    { value: 'Boolean', label: 'Boolean' },
    { value: 'DateTime', label: 'Date & Time' },
    { value: 'Enum', label: 'Select / Enum' },
    { value: 'Reference', label: 'Reference' },
    { value: 'AssetReference', label: 'Asset' },
    { value: 'Json', label: 'JSON' },
    { value: 'Component', label: 'Component' },
    { value: 'Location', label: 'Location' },
    { value: 'Color', label: 'Color' },
];

const FIELD_TYPE_VALUES = FIELD_TYPES.map((ft) => ft.value) as [FieldType, ...FieldType[]];
const API_KEY_REGEX = /^[a-z0-9][a-z0-9_-]*[a-z0-9]$|^[a-z0-9]$/;

function toCamelCase(str: string): string {
    return str
        .trim()
        .replace(/[^a-zA-Z0-9]+(.)/g, (_, chr: string) => chr.toUpperCase())
        .replace(/^[A-Z]/, (c) => c.toLowerCase())
        .replace(/[^a-zA-Z0-9]/g, '');
}

// ─── Zod schemas ──────────────────────────────────────────────────────────────

const dynamicSourceSchema = z.object({
    contentTypeHandle: z.string().min(1, 'Source content type required'),
  labelField: z.string().min(1),
    valueField: z.string().min(1),
    statusFilter: z.string().min(1),
});

const fieldSchema = z.object({
    id: z.string().optional(),
    name: z.string().min(1, 'Name is required'),
    type: z.enum(FIELD_TYPE_VALUES),
    required: z.boolean(),
    localized: z.boolean(),
    isIndexed: z.boolean(),
    isUnique: z.boolean(),
    isList: z.boolean(),
    /** 'static' | 'dynamic' — only relevant when type === 'Enum' */
    enumMode: z.enum(['static', 'dynamic']),
    /** Static option list for Enum */
    staticOptions: z.array(z.string()),
    /** Dynamic source config for Enum */
    dynamicSource: dynamicSourceSchema.optional(),
});

const schemaFormSchema = z.object({
    name: z.string().min(1, 'Name is required').max(200),
    apiKey: z.string().min(1, 'API key is required').max(64).regex(API_KEY_REGEX, 'Lowercase, digits, hyphens only'),
    description: z.string().max(500).optional(),
    localizationMode: z.enum(['PerLocale', 'Shared']),
    fields: z.array(fieldSchema),
});

type SchemaFormValues = z.infer<typeof schemaFormSchema>;

// ─── Helpers ──────────────────────────────────────────────────────────────────

function FieldCheck({ value }: { value: boolean }) {
    if (value) {
        return (
            <span className="inline-flex h-5 w-5 items-center justify-center rounded bg-green-500 text-white text-xs">
                ✓
            </span>
        );
    }
    return <span className="text-slate-300">—</span>;
}

// ─── ReadOnlyFieldRow ─────────────────────────────────────────────────────────

function ReadOnlyFieldRow({ field, onEdit }: { field: FieldDefinitionDto; onEdit: () => void }) {
    const validators: string[] = [];
    if (field.isUnique) validators.push('Unique');
    if (field.options?.length) validators.push(`Options: ${field.options.join(', ')}`);
    if (field.description) validators.push(field.description);

    return (
        <tr className="hover:bg-slate-50 group">
            <td className="px-3 py-3 text-slate-200">⠿</td>
            <td className="px-4 py-3">
                <p className="font-medium text-slate-800">{field.label}</p>
                {field.description && (
                    <p className="text-xs text-slate-400 truncate max-w-xs">{field.description}</p>
                )}
            </td>
            <td className="px-4 py-3 font-mono text-xs text-slate-500">{field.handle}</td>
            <td className="px-4 py-3">
                <span className={`rounded px-2 py-0.5 text-xs font-medium ${FIELD_TYPE_COLORS[field.fieldType] ?? 'bg-slate-100 text-slate-600'}`}>
                    {FIELD_TYPE_LABELS[field.fieldType] ?? field.fieldType}
                </span>
            </td>
            <td className="px-4 py-3 text-center"><FieldCheck value={field.isRequired} /></td>
            <td className="px-4 py-3 text-center"><FieldCheck value={field.isLocalized} /></td>
            <td className="px-4 py-3 text-center"><FieldCheck value={field.isIndexed} /></td>
            <td className="px-4 py-3 text-xs text-slate-400 max-w-[200px] truncate">
                {validators.length ? validators.join(' · ') : '—'}
            </td>
            <td className="px-4 py-3">
                <button
                    onClick={onEdit}
                    className="opacity-0 group-hover:opacity-100 transition-opacity rounded px-2 py-1 text-xs font-medium text-brand-600 hover:bg-brand-50"
                >
                    Edit
                </button>
            </td>
        </tr>
    );
}

// ─── EnumOptionsEditor ────────────────────────────────────────────────────────

function StaticTagInput({
    options,
    onChange,
}: {
    options: string[];
    onChange: (opts: string[]) => void;
}) {
    const [input, setInput] = useState('');

    const addTag = () => {
        const trimmed = input.trim();
        if (trimmed && !options.includes(trimmed)) {
            onChange([...options, trimmed]);
        }
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
                    onKeyDown={(e) => {
                        if (e.key === 'Enter' || e.key === ',') { e.preventDefault(); addTag(); }
                    }}
                    onBlur={addTag}
                />
                <button type="button" onClick={addTag} className="btn-secondary text-xs">Add</button>
            </div>
            <p className="text-xs text-slate-400">Press <kbd className="rounded border border-slate-200 px-1 font-mono">Enter</kbd> or <kbd className="rounded border border-slate-200 px-1 font-mono">,</kbd> to add each option.</p>
        </div>
    );
}

function DynamicSourceEditor({
    contentTypeId,
    fieldId,
    siteId,
    register,
    prefix,
}: {
    contentTypeId: string;
    fieldId?: string;
    siteId: string;
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    register: any;
    prefix: string;
}) {
    const [testing, setTesting] = useState(false);
    const [previewOptions, setPreviewOptions] = useState<Array<{ value: string; label: string }> | null>(null);

    const testQuery = async () => {
        if (!fieldId || !siteId) { toast.error('Save the field first to test the dynamic source.'); return; }
        setTesting(true);
        try {
            const opts = await contentTypesApi.getEnumOptions(contentTypeId, fieldId, siteId);
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
                    <input
                        className="form-input mt-1 font-mono text-xs"
                        placeholder="e.g. product, category"
                        {...register(`${prefix}.contentTypeHandle`)}
                    />
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
                        <option value="Published">Published</option>
                        <option value="Draft">Draft</option>
                        <option value="Approved">Approved</option>
                    </select>
                </div>
            </div>

            {/* Test button */}
            <div className="flex items-center gap-3">
                <button
                    type="button"
                    onClick={testQuery}
                    disabled={testing}
                    className="btn-secondary text-xs disabled:opacity-50"
                >
                    {testing ? 'Testing…' : '▶ Test Query'}
                </button>
                {previewOptions !== null && (
                    <span className="text-xs text-slate-500">{previewOptions.length} option{previewOptions.length !== 1 ? 's' : ''} found</span>
                )}
            </div>

            {/* Preview results */}
            {previewOptions !== null && previewOptions.length > 0 && (
                <div className="mt-1 flex flex-wrap gap-1">
                    {previewOptions.slice(0, 20).map((o) => (
                        <span key={o.value} className="inline-flex items-center gap-1 rounded border border-slate-200 bg-white px-2 py-0.5 text-xs text-slate-700" title={`value: ${o.value}`}>
                            {o.label}
                            <span className="font-mono text-slate-400">({o.value})</span>
                        </span>
                    ))}
                    {previewOptions.length > 20 && (
                        <span className="text-xs text-slate-400">+{previewOptions.length - 20} more…</span>
                    )}
                </div>
            )}
            {previewOptions !== null && previewOptions.length === 0 && (
                <p className="text-xs text-amber-600">No published entries found for this source. Check the content type handle and status filter.</p>
            )}
        </div>
    );
}

function EnumOptionsEditor({
    contentTypeId,
    fieldId,
    siteId,
    enumMode,
    staticOptions,
    onEnumModeChange,
    onStaticOptionsChange,
    register,
    prefix,
}: {
    contentTypeId: string;
    fieldId?: string;
    siteId: string;
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
                {/* Mode toggle */}
                <div className="flex overflow-hidden rounded-md border border-amber-200 text-xs">
                    <button
                        type="button"
                        onClick={() => onEnumModeChange('static')}
                        className={`px-3 py-1 font-medium transition-colors ${enumMode === 'static' ? 'bg-amber-600 text-white' : 'bg-white text-amber-700 hover:bg-amber-100'}`}
                    >
                        Static list
                    </button>
                    <button
                        type="button"
                        onClick={() => onEnumModeChange('dynamic')}
                        className={`border-l border-amber-200 px-3 py-1 font-medium transition-colors ${enumMode === 'dynamic' ? 'bg-amber-600 text-white' : 'bg-white text-amber-700 hover:bg-amber-100'}`}
                    >
                        Dynamic query
                    </button>
                </div>
            </div>

            {enumMode === 'static' ? (
                <StaticTagInput options={staticOptions} onChange={onStaticOptionsChange} />
            ) : (
                <DynamicSourceEditor
                    contentTypeId={contentTypeId}
                    fieldId={fieldId}
                    siteId={siteId}
                    register={register}
                    prefix={prefix}
                />
            )}
        </div>
    );
}

// ─── SchemaTab ────────────────────────────────────────────────────────────────

export function SchemaTab({ contentType }: { contentType: ContentType }) {
    const qc = useQueryClient();
    const { selectedSiteId } = useSite();
  const siteId = selectedSiteId ?? '';
    const [editing, setEditing] = useState(false);
    const [activeFieldIdx, setActiveFieldIdx] = useState<number | null>(null);

    const toFormFields = (ct: ContentType): SchemaFormValues['fields'] =>
        (ct.fields ?? [])
    .slice()
       .sort((a, b) => a.sortOrder - b.sortOrder)
    .map((f) => ({
      id: f.id,
 name: f.label,
       type: f.fieldType as FieldType,
  required: f.isRequired,
      localized: f.isLocalized,
 isIndexed: f.isIndexed,
          isUnique: f.isUnique,
           isList: f.isList,
    enumMode: (f.dynamicSource ? 'dynamic' : 'static') as 'static' | 'dynamic',
       staticOptions: f.options ?? [],
    dynamicSource: f.dynamicSource
      ? {
        contentTypeHandle: f.dynamicSource.contentTypeHandle,
     labelField: f.dynamicSource.labelField,
 valueField: f.dynamicSource.valueField,
             statusFilter: f.dynamicSource.statusFilter,
       }
          : undefined,
            }));

    const {
    register,
        control,
        handleSubmit,
        watch,
     reset,
        setValue,
        formState: { errors, isSubmitting, isDirty },
    } = useForm<SchemaFormValues>({
        resolver: zodResolver(schemaFormSchema),
        defaultValues: {
       name: contentType.displayName,
            apiKey: contentType.handle,
            description: contentType.description ?? '',
            localizationMode: contentType.localizationMode === 'Shared' ? 'Shared' : 'PerLocale',
            fields: toFormFields(contentType),
        },
    });

    useEffect(() => {
        reset({
       name: contentType.displayName,
       apiKey: contentType.handle,
 description: contentType.description ?? '',
            localizationMode: contentType.localizationMode === 'Shared' ? 'Shared' : 'PerLocale',
            fields: toFormFields(contentType),
        });
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [contentType.id, contentType.updatedAt]);

    const { fields, append, remove, move } = useFieldArray({ control, name: 'fields' });

    const saveMutation = useMutation({
        mutationFn: (values: SchemaFormValues) =>
       contentTypesApi.update(contentType.id, {
        displayName: values.name,
      description: values.description,
        localizationMode: values.localizationMode,
     fields: values.fields.map((f, idx) => ({
      id: f.id,
     handle: toCamelCase(f.name) || `field${idx}`,
       label: f.name,
  fieldType: f.type,
     isRequired: f.required,
   isLocalized: f.localized,
         isUnique: f.isUnique,
       isIndexed: f.isIndexed,
      isList: f.isList,
          sortOrder: idx,
           options: f.type === 'Enum' && f.enumMode === 'static' ? f.staticOptions : undefined,
   dynamicSource: f.type === 'Enum' && f.enumMode === 'dynamic' ? f.dynamicSource : undefined,
       })),
     }),
  onSuccess: () => {
         toast.success('Schema saved.');
            void qc.invalidateQueries({ queryKey: ['content-types'] });
          setEditing(false);
   setActiveFieldIdx(null);
 },
        onError: (err) => {
            toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Save failed.');
      },
    });

    const handleCancel = () => {
        reset();
        setEditing(false);
        setActiveFieldIdx(null);
    };

    const addField = () => {
        append({ name: '', type: 'ShortText', required: false, localized: false, isIndexed: false, isUnique: false, isList: false, enumMode: 'static', staticOptions: [] });
      setActiveFieldIdx(fields.length);
    };

    // Component-kind types are auto-created backing types — not directly editable.
    // This check is AFTER all hooks to comply with React Rules of Hooks.
    if (contentType.kind === 'Component') {
        return (
    <div className="space-y-4">
            <div className="flex items-start gap-2 rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800">
          <span className="mt-0.5 shrink-0">⚙️</span>
                    <div>
            <p className="font-semibold">Component Backing Type</p>
        <p className="mt-0.5 text-amber-700">
       This content type was auto-created to store data for a Component. Its schema
  is managed through the{' '}
  <strong>Component Library</strong> — edit the component's fields there instead.
   </p>
     </div>
      </div>
            </div>
    );
    }

    // ── Read-only view ──────────────────────────────────────────────────────────
    const sortedFields = (contentType.fields ?? []).slice().sort((a, b) => a.sortOrder - b.sortOrder);

        return (
            <div className="space-y-4">
                {/* Header */}
                <div className="flex items-start justify-between gap-4">
                    <div>
                        <p className="text-sm font-medium text-slate-700">
                            Schema Definition — {contentType.displayName}
                        </p>
                        <p className="mt-0.5 text-xs text-slate-400">
                            Last edited {formatDistanceToNow(new Date(contentType.updatedAt), { addSuffix: true })}
                        </p>
                    </div>
                    <div className="flex items-center gap-2 shrink-0">
                        <button className="btn-secondary text-sm">Export JSON Schema</button>
                        <button onClick={() => setEditing(true)} className="btn-primary text-sm">
                            ✏ Edit Schema
                        </button>
                    </div>
                </div>

                {/* Info banner */}
                <div className="flex items-start gap-2 rounded-lg border border-blue-100 bg-blue-50 px-4 py-3 text-sm text-blue-700">
                    <span className="mt-0.5">ℹ</span>
                    <span>
                        Click <strong>Edit Schema</strong> to add, remove, or reorder fields.
                    </span>
                </div>

                {/* Basic info */}
                <div className="rounded-lg border border-slate-200 bg-white px-5 py-4">
                    <div className="grid grid-cols-2 gap-6 text-sm">
                        <div>
                            <p className="text-xs font-medium text-slate-500 uppercase mb-1">Display Name</p>
                            <p className="font-medium text-slate-800">{contentType.displayName}</p>
                        </div>
                        <div>
                            <p className="text-xs font-medium text-slate-500 uppercase mb-1">API Key</p>
                            <p className="font-mono text-slate-700">{contentType.handle}</p>
                        </div>
                        {contentType.description && (
                            <div className="col-span-2">
                                <p className="text-xs font-medium text-slate-500 uppercase mb-1">Description</p>
                                <p className="text-slate-600">{contentType.description}</p>
                            </div>
                        )}
                        <div>
                            <p className="text-xs font-medium text-slate-500 uppercase mb-1">Localization</p>
                            <p className="text-slate-700">
                                {contentType.localizationMode === 'Shared'
                                    ? 'Shared (locale-independent)'
                                    : 'Per-locale fields'}
                            </p>
                        </div>
                    </div>
                </div>

                {/* Fields table */}
                <div className="overflow-x-auto rounded-lg border border-slate-200">
                    <table className="min-w-full divide-y divide-slate-200 text-sm">
                        <thead className="bg-slate-50">
                            <tr>
                                <th className="w-6 px-3 py-3" />
                                <th className="px-4 py-3 text-left text-xs font-medium text-slate-500 uppercase">Field Name</th>
                                <th className="px-4 py-3 text-left text-xs font-medium text-slate-500 uppercase">API Key</th>
                                <th className="px-4 py-3 text-left text-xs font-medium text-slate-500 uppercase">Type</th>
                                <th className="px-4 py-3 text-center text-xs font-medium text-slate-500 uppercase">Required</th>
                                <th className="px-4 py-3 text-center text-xs font-medium text-slate-500 uppercase">Localized</th>
                                <th className="px-4 py-3 text-center text-xs font-medium text-slate-500 uppercase">Indexed</th>
                                <th className="px-4 py-3 text-left text-xs font-medium text-slate-500 uppercase">Validators</th>
                                <th className="px-4 py-3" />
                            </tr>
                        </thead>
                        <tbody className="divide-y divide-slate-100 bg-white">
                            {sortedFields.length === 0 ? (
                                <tr>
                                    <td colSpan={9} className="px-4 py-12 text-center text-slate-400">
                                        No fields defined yet.{' '}
                                        <button onClick={() => setEditing(true)} className="text-brand-600 hover:underline">
                                            Add the first field
                                        </button>.
                                    </td>
                                </tr>
                            ) : (
                                sortedFields.map((f) => (
                                    <ReadOnlyFieldRow key={f.id} field={f} onEdit={() => setEditing(true)} />
                                ))
                            )}
                        </tbody>
                    </table>
                </div>

                {/* Field type legend */}
                <div className="flex flex-wrap items-center gap-2 text-xs text-slate-500">
                    <span className="font-medium">Field Types:</span>
                    {Object.entries(FIELD_TYPE_LABELS).map(([k, v]) => (
                        <span
                            key={k}
                            className={`rounded px-2 py-0.5 font-medium ${FIELD_TYPE_COLORS[k] ?? 'bg-slate-100 text-slate-600'}`}
                        >
                            {v}
                        </span>
                    ))}
                </div>
            </div>
        );
    }

    // ── Edit view ───────────────────────────────────────────────────────────────
    return (
        <form onSubmit={handleSubmit((v) => saveMutation.mutate(v))} className="space-y-5">
            {/* Edit header */}
            <div className="flex items-center justify-between gap-4">
                <div>
                    <p className="text-sm font-semibold text-slate-800">
                        Editing Schema — {contentType.displayName}
                    </p>
                    <p className="text-xs text-slate-400 mt-0.5">Changes are saved when you click Save.</p>
                </div>
                <div className="flex items-center gap-2 shrink-0">
                    <button type="button" onClick={handleCancel} className="btn-secondary text-sm">
                        Cancel
                    </button>
                    <button
                        type="submit"
                        disabled={isSubmitting || !isDirty}
                        className="btn-primary text-sm disabled:opacity-50"
                    >
                        {isSubmitting ? 'Saving…' : 'Save Changes'}
                    </button>
                </div>
            </div>

            {/* Basic information */}
            <div className="rounded-lg border border-slate-200 bg-white px-5 py-4 space-y-4">
                <h3 className="text-sm font-semibold text-slate-800">Basic Information</h3>
                <div className="grid grid-cols-2 gap-4">
                    <div>
                        <label className="form-label">Display Name</label>
                        <input className="form-input mt-1" {...register('name')} placeholder="Blog Post" />
                        {errors.name && <p className="form-error">{errors.name.message}</p>}
                    </div>
                    <div>
                        <label className="form-label">API Key</label>
                        <div className="mt-1 flex">
                            <span className="inline-flex items-center rounded-l-lg border border-r-0 border-slate-300 bg-slate-50 px-3 text-xs text-slate-500 select-none">
                                api/v1/
                            </span>
                            <input
                                className="form-input rounded-l-none font-mono"
                                {...register('apiKey')}
                                placeholder="blog-post"
                            />
                        </div>
                        {errors.apiKey && <p className="form-error">{errors.apiKey.message}</p>}
                    </div>
                    <div className="col-span-2">
                        <label className="form-label">Description (optional)</label>
                        <input
                            className="form-input mt-1"
                            {...register('description')}
                            placeholder="Describe this content type…"
                        />
                    </div>
                    <div>
                        <label className="form-label">Localization Mode</label>
                        <select className="form-input mt-1" {...register('localizationMode')}>
                            <option value="PerLocale">Per-locale fields</option>
                            <option value="Shared">Shared (locale-independent)</option>
                        </select>
                    </div>
                </div>
            </div>

            {/* Fields editor */}
            <div className="rounded-lg border border-slate-200 bg-white px-5 py-4 space-y-4">
                <div className="flex items-center justify-between">
                    <h3 className="text-sm font-semibold text-slate-800">Fields</h3>
                    <button type="button" onClick={addField} className="btn-secondary text-xs">
                        + Add Field
                    </button>
                </div>

                {fields.length === 0 && (
                    <p className="text-sm text-slate-400">No fields yet. Click "+ Add Field" to start.</p>
                )}

                <div className="space-y-3">
                    {fields.map((field, idx) => (
                        <div
                            key={field.id}
                            className={`rounded-lg border p-4 ${activeFieldIdx === idx ? 'border-brand-300 bg-brand-50/40' : 'border-slate-200'
                                }`}
                        >
                            {/* Field summary row */}
                            <div
                                className="flex cursor-pointer items-center justify-between"
                                onClick={() => setActiveFieldIdx(activeFieldIdx === idx ? null : idx)}
                            >
                                <div className="flex items-center gap-2">
                                    <span className="text-sm font-medium text-slate-800">
                                        {watch(`fields.${idx}.name`) || (
                                            <span className="text-slate-400">Unnamed field</span>
                                        )}
                                    </span>
                                    <span
                                        className={`rounded px-1.5 py-0.5 text-xs font-medium ${FIELD_TYPE_COLORS[watch(`fields.${idx}.type`)] ?? 'bg-slate-100 text-slate-600'
                                            }`}
                                    >
                                        {FIELD_TYPE_LABELS[watch(`fields.${idx}.type`)] ?? watch(`fields.${idx}.type`)}
                                    </span>
                                    {watch(`fields.${idx}.required`) && (
                                        <span className="rounded bg-red-100 px-1.5 py-0.5 text-xs font-medium text-red-600">
                                            Required
                                        </span>
                                    )}
                                    {watch(`fields.${idx}.localized`) && (
                                        <span className="rounded bg-brand-100 px-1.5 py-0.5 text-xs font-medium text-brand-700">
                                            Localized
                                        </span>
                                    )}
                                    {watch(`fields.${idx}.isIndexed`) && (
                                        <span className="rounded bg-amber-100 px-1.5 py-0.5 text-xs font-medium text-amber-700">
                                            Indexed
                                        </span>
                                    )}
                                </div>
                                <div className="flex items-center gap-2" onClick={(e) => e.stopPropagation()}>
                                    <button
                                        type="button"
                                        onClick={() => { if (idx > 0) move(idx, idx - 1); }}
                                        disabled={idx === 0}
                                        className="text-slate-400 hover:text-slate-600 disabled:opacity-30"
                                        aria-label="Move up"
                                    >
                                        ↑
                                    </button>
                                    <button
                                        type="button"
                                        onClick={() => { if (idx < fields.length - 1) move(idx, idx + 1); }}
                                        disabled={idx === fields.length - 1}
                                        className="text-slate-400 hover:text-slate-600 disabled:opacity-30"
                                        aria-label="Move down"
                                    >
                                        ↓
                                    </button>
                                    <button
                                        type="button"
                                        onClick={() => remove(idx)}
                                        className="text-red-400 hover:text-red-600"
                                        aria-label="Remove"
                                    >
                                        ✕
                                    </button>
                                </div>
                            </div>

                            {/* Field detail (expanded) */}
                            {activeFieldIdx === idx && (
                                <div className="mt-4 space-y-3">
                                    <div className="grid grid-cols-2 gap-3">
                                        <div>
                                            <label className="form-label">Name</label>
                                            <input className="form-input mt-1" {...register(`fields.${idx}.name`)} />
                                            {errors.fields?.[idx]?.name && (
                                                <p className="form-error">{errors.fields[idx]?.name?.message}</p>
                                            )}
                                        </div>
                                        <div>
                                            <label className="form-label">Type</label>
                                            <select className="form-input mt-1" {...register(`fields.${idx}.type`)}>
                                                {FIELD_TYPES.map((ft) => (
                                                    <option key={ft.value} value={ft.value}>{ft.label}</option>
                                                ))}
                                            </select>
                                        </div>
                                        <div className="col-span-2 flex flex-wrap items-center gap-4 pt-1">
                                            <label className="flex items-center gap-2 text-sm text-slate-700">
                                                <input type="checkbox" className="h-4 w-4 rounded border-slate-300 text-brand-600" {...register(`fields.${idx}.required`)} />
                                                Required
                                            </label>
                                            <label className="flex items-center gap-2 text-sm text-slate-700">
                                                <input type="checkbox" className="h-4 w-4 rounded border-slate-300 text-brand-600" {...register(`fields.${idx}.localized`)} />
                                                Localized
                                            </label>
                                            <label className="flex items-center gap-2 text-sm text-slate-700">
                                                <input type="checkbox" className="h-4 w-4 rounded border-slate-300 text-brand-600" {...register(`fields.${idx}.isIndexed`)} />
                                                Indexed
                                            </label>
                                            <label className="flex items-center gap-2 text-sm text-slate-700">
                                                <input type="checkbox" className="h-4 w-4 rounded border-slate-300 text-brand-600" {...register(`fields.${idx}.isUnique`)} />
                                                Unique
                                            </label>
                                            <label className="flex items-center gap-2 text-sm text-slate-700">
                                                <input type="checkbox" className="h-4 w-4 rounded border-slate-300 text-brand-600" {...register(`fields.${idx}.isList`)} />
                                                List (multi-value)
                                            </label>
                                        </div>

                                        {/* Enum options — only shown when type is Enum */}
                                        {watch(`fields.${idx}.type`) === 'Enum' && (
                                            <EnumOptionsEditor
                                                contentTypeId={contentType.id}
                                                fieldId={field.id}
                                                siteId={siteId}
                                                enumMode={watch(`fields.${idx}.enumMode`) ?? 'static'}
                                                staticOptions={watch(`fields.${idx}.staticOptions`) ?? []}
                                                onEnumModeChange={(mode) => setValue(`fields.${idx}.enumMode`, mode)}
                                                onStaticOptionsChange={(opts) => setValue(`fields.${idx}.staticOptions`, opts)}
                                                register={register}
                                                prefix={`fields.${idx}.dynamicSource`}
                                            />
                                        )}
                                    </div>
                                </div>
                            )},

                        </div>
                    ))}
                </div>

                {/* Bottom save/cancel */}
                <div className="flex justify-end gap-3 pb-2">
                    <button type="button" onClick={handleCancel} className="btn-secondary">
                        Cancel
                    </button>
                    <button
                        type="submit"
                        disabled={isSubmitting || !isDirty}
                        className="btn-primary disabled:opacity-50"
                    >
                        {isSubmitting ? 'Saving…' : 'Save Changes'}
                    </button>
                </div>
            </div>
        </form>
    );
}
