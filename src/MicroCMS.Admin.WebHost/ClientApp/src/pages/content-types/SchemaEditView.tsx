import { useState } from 'react';
import { useFieldArray, useFormContext } from 'react-hook-form';
import type { UseFormRegister, UseFormWatch, UseFormSetValue, FieldErrors } from 'react-hook-form';
import { useQuery } from '@tanstack/react-query';
import { siteTemplatesApi } from '@/api/siteTemplates';
import { contentTypesApi } from '@/api/contentTypes';
import { FIELD_TYPE_COLORS, FIELD_TYPE_LABELS } from './contentTypeDetail.shared';
import { EnumOptionsEditor, ReferenceSourceEditor, MultiListSourceEditor, ComponentSourceEditor } from './SchemaFieldEditors';
import { FIELD_TYPES, DEFAULT_GROUP, makeBlankField } from './schemaTab.types';
import { orderedGroups } from './schemaTab.helpers';
import type { SchemaFormValues } from './schemaTab.types';
import type { FieldDefinitionDto } from '@/types';

// ─── FieldRow (single expandable field inside a group) ────────────────────────

function FieldRow({
    fieldArrayIndex,
    contentTypeId,
    fieldId,
    register,
    watch,
    setValue,
    errors,
    onRemove,
    onMoveUp,
    onMoveDown,
    canMoveUp,
    canMoveDown,
}: {
    fieldArrayIndex: number;
    contentTypeId: string;
    fieldId?: string;
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    register: UseFormRegister<SchemaFormValues>;
    watch: UseFormWatch<SchemaFormValues>;
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    setValue: UseFormSetValue<SchemaFormValues>;
    errors: FieldErrors<SchemaFormValues>;
    onRemove: () => void;
    onMoveUp: () => void;
    onMoveDown: () => void;
    canMoveUp: boolean;
    canMoveDown: boolean;
}) {
    const [expanded, setExpanded] = useState(false);
    const idx = fieldArrayIndex;
    const fieldErrors = errors.fields?.[idx];

    return (
        <div className={`rounded-lg border p-4 ${fieldErrors ? 'border-red-300 bg-red-50/30' : expanded ? 'border-brand-300 bg-brand-50/40' : 'border-slate-200'}`}>
            {/* Summary row */}
            <div className="flex cursor-pointer items-center justify-between" onClick={() => setExpanded(!expanded)}>
                <div className="flex items-center gap-2">
                    <span className="text-sm font-medium text-slate-800">
                        {watch(`fields.${idx}.name`) || <span className="text-slate-400">Unnamed field</span>}
                    </span>
                    <span className={`rounded px-1.5 py-0.5 text-xs font-medium ${FIELD_TYPE_COLORS[watch(`fields.${idx}.type`)] ?? 'bg-slate-100 text-slate-600'}`}>
                        {FIELD_TYPE_LABELS[watch(`fields.${idx}.type`)] ?? watch(`fields.${idx}.type`)}
                    </span>
                    {watch(`fields.${idx}.required`) && <span className="rounded bg-red-100 px-1.5 py-0.5 text-xs font-medium text-red-600">Required</span>}
                    {watch(`fields.${idx}.localized`) && <span className="rounded bg-brand-100 px-1.5 py-0.5 text-xs font-medium text-brand-700">Localized</span>}
                    {watch(`fields.${idx}.isIndexed`) && <span className="rounded bg-amber-100 px-1.5 py-0.5 text-xs font-medium text-amber-700">Indexed</span>}
                    {fieldErrors && !expanded && <span className="rounded bg-red-100 px-1.5 py-0.5 text-xs font-medium text-red-600">⚠ Needs attention</span>}
                </div>
                <div className="flex items-center gap-2" onClick={(e) => e.stopPropagation()}>
                    <button type="button" onClick={onMoveUp} disabled={!canMoveUp} className="text-slate-400 hover:text-slate-600 disabled:opacity-30" aria-label="Move up">↑</button>
                    <button type="button" onClick={onMoveDown} disabled={!canMoveDown} className="text-slate-400 hover:text-slate-600 disabled:opacity-30" aria-label="Move down">↓</button>
                    <button type="button" onClick={onRemove} className="text-red-400 hover:text-red-600" aria-label="Remove">✕</button>
                </div>
            </div>

            {/* Expanded detail */}
            {expanded && (
                <div className="mt-4 space-y-3">
                    <div className="grid grid-cols-2 gap-3">
                        <div>
                            <label className="form-label">Name</label>
                            <input className="form-input mt-1" {...register(`fields.${idx}.name`)} />
                            {fieldErrors?.name && <p className="form-error">{fieldErrors.name.message}</p>}
                        </div>
                        <div>
                            <label className="form-label">Type</label>
                            <select className="form-input mt-1" {...register(`fields.${idx}.type`)}>
                                {FIELD_TYPES.map((ft) => <option key={ft.value} value={ft.value}>{ft.label}</option>)}
                            </select>
                        </div>
                        <div className="col-span-2 flex flex-wrap items-center gap-4 pt-1">
                            <label className="flex items-center gap-2 text-sm text-slate-700">
                                <input type="checkbox" className="h-4 w-4 rounded border-slate-300 text-brand-600" {...register(`fields.${idx}.required`)} /> Required
                            </label>
                            <label className="flex items-center gap-2 text-sm text-slate-700">
                                <input type="checkbox" className="h-4 w-4 rounded border-slate-300 text-brand-600" {...register(`fields.${idx}.localized`)} /> Localized
                            </label>
                            <label className="flex items-center gap-2 text-sm text-slate-700">
                                <input type="checkbox" className="h-4 w-4 rounded border-slate-300 text-brand-600" {...register(`fields.${idx}.isIndexed`)} /> Indexed
                            </label>
                            <label className="flex items-center gap-2 text-sm text-slate-700">
                                <input type="checkbox" className="h-4 w-4 rounded border-slate-300 text-brand-600" {...register(`fields.${idx}.isUnique`)} /> Unique
                            </label>
                            <label className="flex items-center gap-2 text-sm text-slate-700">
                                <input type="checkbox" className="h-4 w-4 rounded border-slate-300 text-brand-600" {...register(`fields.${idx}.isList`)} /> List (multi-value)
                            </label>
                        </div>

                        {watch(`fields.${idx}.type`) === 'Enum' && (
                            <EnumOptionsEditor
                                contentTypeId={contentTypeId}
                                fieldId={fieldId}
                                enumMode={watch(`fields.${idx}.enumMode`) ?? 'static'}
                                staticOptions={watch(`fields.${idx}.staticOptions`) ?? []}
                                onEnumModeChange={(mode) => setValue(`fields.${idx}.enumMode`, mode)}
                                onStaticOptionsChange={(opts) => setValue(`fields.${idx}.staticOptions`, opts)}
                                register={register}
                                prefix={`fields.${idx}.dynamicSource`}
                            />
                        )}
                        {watch(`fields.${idx}.type`) === 'Reference' && (
                            <ReferenceSourceEditor register={register} prefix={`fields.${idx}.dynamicSource`} errors={errors.fields?.[idx]?.dynamicSource} />
                        )}
                        {watch(`fields.${idx}.type`) === 'Component' && (
                            <ComponentSourceEditor register={register} prefix={`fields.${idx}.componentSourceKey`} errors={errors.fields?.[idx]?.componentSourceKey} />
                        )}
                        {watch(`fields.${idx}.type`) === 'MultiList' && (
                            <MultiListSourceEditor
                                contentTypeId={contentTypeId}
                                fieldId={fieldId}
                                register={register}
                                prefix={`fields.${idx}.multiListSource`}
                                errors={errors.fields?.[idx]?.multiListSource}
                            />
                        )}
                    </div>
                </div>
            )}
        </div>
    );
}

// ─── GroupSection ─────────────────────────────────────────────────────────────

function GroupSection({
    groupName,
    contentTypeId,
    allFieldIndices,
    register,
    watch,
    setValue,
    errors,
    onRemoveField,
    onMoveField,
    onRenameGroup,
    onDeleteGroup,
}: {
    groupName: string;
    contentTypeId: string;
    allFieldIndices: Array<{ globalIdx: number; fieldId?: string }>;
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    register: UseFormRegister<SchemaFormValues>;
    watch: UseFormWatch<SchemaFormValues>;
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
    setValue: UseFormSetValue<SchemaFormValues>;
    errors: FieldErrors<SchemaFormValues>;
    onRemoveField: (globalIdx: number) => void;
    onMoveField: (from: number, to: number) => void;
    onRenameGroup: (oldName: string, newName: string) => void;
    onDeleteGroup: (groupName: string) => void;
}) {
    const [collapsed, setCollapsed] = useState(false);
    const [renaming, setRenaming] = useState(false);
    const [renameValue, setRenameValue] = useState(groupName);
    const isDefault = groupName === DEFAULT_GROUP;

    const commitRename = () => {
        const trimmed = renameValue.trim();
        if (trimmed && trimmed !== groupName) onRenameGroup(groupName, trimmed);
        setRenaming(false);
    };

    return (
        <div className="rounded-lg border border-slate-200 overflow-hidden">
            {/* Group header */}
            <div className="flex items-center justify-between bg-slate-50 px-4 py-2.5 border-b border-slate-200">
                <div className="flex items-center gap-2 flex-1">
                    <button type="button" onClick={() => setCollapsed(!collapsed)} className="text-slate-400 hover:text-slate-600 text-xs">
                        {collapsed ? '▶' : '▼'}
                    </button>
                    {renaming ? (
                        <input
                            autoFocus
                            className="form-input text-xs py-0.5 h-6 w-40 font-semibold"
                            value={renameValue}
                            onChange={(e) => setRenameValue(e.target.value)}
                            onBlur={commitRename}
                            onKeyDown={(e) => { if (e.key === 'Enter') { e.preventDefault(); commitRename(); } if (e.key === 'Escape') { setRenaming(false); setRenameValue(groupName); } }}
                        />
                    ) : (
                        <span className="text-xs font-semibold text-slate-700 uppercase tracking-wide">{groupName}</span>
                    )}
                    <span className="text-xs text-slate-400">{allFieldIndices.length} field{allFieldIndices.length !== 1 ? 's' : ''}</span>
                </div>
                <div className="flex items-center gap-1">
                    {!renaming && (
                        <button type="button" onClick={() => { setRenaming(true); setRenameValue(groupName); }} className="rounded px-2 py-0.5 text-xs text-slate-500 hover:bg-slate-200" title="Rename group">✎</button>
                    )}
                    {!isDefault && allFieldIndices.length === 0 && (
                        <button type="button" onClick={() => onDeleteGroup(groupName)} className="rounded px-2 py-0.5 text-xs text-red-400 hover:bg-red-50" title="Delete empty group">✕</button>
                    )}
                </div>
            </div>

            {/* Fields list */}
            {!collapsed && (
                <div className="space-y-3 p-4">
                    {allFieldIndices.length === 0 && (
                        <p className="text-xs text-slate-400">No fields in this group. Use "+ Add Field" below to add one.</p>
                    )}
                    {allFieldIndices.map(({ globalIdx, fieldId }, posInGroup) => (
                        <FieldRow
                            key={globalIdx}
                            fieldArrayIndex={globalIdx}
                            contentTypeId={contentTypeId}
                            fieldId={fieldId}
                            register={register}
                            watch={watch}
                            setValue={setValue}
                            errors={errors}
                            onRemove={() => onRemoveField(globalIdx)}
                            onMoveUp={() => posInGroup > 0 ? onMoveField(globalIdx, allFieldIndices[posInGroup - 1].globalIdx) : undefined}
                            onMoveDown={() => posInGroup < allFieldIndices.length - 1 ? onMoveField(globalIdx, allFieldIndices[posInGroup + 1].globalIdx) : undefined}
                            canMoveUp={posInGroup > 0}
                            canMoveDown={posInGroup < allFieldIndices.length - 1}
                        />
                    ))}
                </div>
            )}
        </div>
    );
}

// ─── InheritedFieldsSection ───────────────────────────────────────────────────

function InheritedFieldsSection({ fields, parentHandle }: { fields: FieldDefinitionDto[]; parentHandle?: string }) {
    const [collapsed, setCollapsed] = useState(false);
    const groups = orderedGroups(fields);

    return (
        <div className="rounded-lg border border-indigo-200 overflow-hidden">
            <button
                type="button"
                onClick={() => setCollapsed((c) => !c)}
                className="flex w-full items-center justify-between bg-indigo-50 px-4 py-2.5 text-left border-b border-indigo-200 hover:bg-indigo-100 transition-colors"
            >
                <div className="flex items-center gap-2">
                    <span className="text-xs font-semibold uppercase tracking-wide text-indigo-700">
                        Inherited from {parentHandle ?? 'parent'}
                    </span>
                    <span className="rounded bg-indigo-100 px-1.5 py-0.5 text-[10px] font-medium text-indigo-600">
                        {fields.length} field{fields.length !== 1 ? 's' : ''} · read-only
                    </span>
                </div>
                <span className="text-xs text-indigo-400 select-none">{collapsed ? '▶' : '▼'}</span>
            </button>
            {!collapsed && (
                <div className="divide-y divide-indigo-100 bg-white">
                    {groups.map((group) => {
                        const gFields = fields.filter((f) => (f.groupName ?? DEFAULT_GROUP) === group);
                        return (
                            <div key={group}>
                                {group !== DEFAULT_GROUP && (
                                    <p className="bg-slate-50 px-4 py-1.5 text-[10px] font-semibold uppercase tracking-wide text-slate-400 border-b border-slate-100">
                                        {group}
                                    </p>
                                )}
                                {gFields.map((f) => (
                                    <div key={f.id} className="flex items-center gap-3 px-4 py-2.5 text-sm text-slate-500">
                                        <span className={`rounded px-2 py-0.5 text-xs font-medium ${FIELD_TYPE_COLORS[f.fieldType] ?? 'bg-slate-100 text-slate-600'}`}>
                                            {FIELD_TYPE_LABELS[f.fieldType] ?? f.fieldType}
                                        </span>
                                        <span className="font-medium text-slate-600">{f.label}</span>
                                        <span className="font-mono text-xs text-slate-400">{f.handle}</span>
                                        {f.isRequired && <span className="rounded bg-red-50 px-1 py-0.5 text-[10px] font-medium text-red-500">Required</span>}
                                        {f.isLocalized && <span className="rounded bg-brand-50 px-1 py-0.5 text-[10px] font-medium text-brand-600">Localized</span>}
                                    </div>
                                ))}
                            </div>
                        );
                    })}
                </div>
            )}
        </div>
    );
}

// ─── SchemaEditView ───────────────────────────────────────────────────────────

export function SchemaEditView({
    contentTypeId,
    contentTypeName,
    inheritedFields,
    parentHandle,
    onCancel,
    isSubmitting,
    componentMode = false,
}: {
    contentTypeId: string;
    contentTypeName: string;
    inheritedFields?: FieldDefinitionDto[];
    parentHandle?: string;
    onCancel: () => void;
    isSubmitting: boolean;
    /** When true, hides the Basic Information card, save/cancel buttons, and content-type-specific options. */
    componentMode?: boolean;
}) {
    const { register, watch, setValue, control, formState: { errors } } = useFormContext<SchemaFormValues>();
    const { fields, append, remove, move } = useFieldArray({ control, name: 'fields' });
    const [newGroupName, setNewGroupName] = useState('');

    const { data: siteTemplates } = useQuery({
        queryKey: ['site-templates'],
        queryFn: () => siteTemplatesApi.list(),
    });

    const { data: allContentTypes } = useQuery({
        queryKey: ['content-types'],
        queryFn: () => contentTypesApi.list({ pageSize: 200 }),
        select: (d) => d.items.filter((ct) => ct.id !== contentTypeId && ct.kind !== 'Component'),
    });

    const currentFields = watch('fields');
    const groups = orderedGroups(currentFields ?? []);

    const addField = (groupName: string) => {
        append(makeBlankField(groupName));
    };

    const addGroup = () => {
        const trimmed = newGroupName.trim();
        if (!trimmed) return;
        setNewGroupName('');
        // Add a placeholder field so the group appears immediately
        append(makeBlankField(trimmed));
    };

    const renameGroup = (oldName: string, newName: string) => {
        currentFields.forEach((f, idx) => {
            if ((f.groupName ?? DEFAULT_GROUP) === oldName) {
                setValue(`fields.${idx}.groupName`, newName);
            }
        });
    };

    const deleteGroup = (groupName: string) => {
        // Remove all fields in that group (should only be called when empty, but be safe)
        const toRemove = currentFields
            .map((f, idx) => ({ idx, g: f.groupName ?? DEFAULT_GROUP }))
            .filter(({ g }) => g === groupName)
            .map(({ idx }) => idx)
            .reverse();
        toRemove.forEach((idx) => remove(idx));
    };

    return (
        <div className="space-y-5">
            {/* Edit header — hidden in component mode */}
            {!componentMode && (
            <div className="flex items-center justify-between gap-4">
                <div>
                    <p className="text-sm font-semibold text-slate-800">Editing Schema — {contentTypeName}</p>
                    <p className="text-xs text-slate-400 mt-0.5">Changes are saved when you click Save.</p>
                </div>
                <div className="flex items-center gap-2 shrink-0">
                    <button type="button" onClick={onCancel} className="btn-secondary text-sm">Cancel</button>
                    <button type="submit" disabled={isSubmitting} className="btn-primary text-sm disabled:opacity-50">
                        {isSubmitting ? 'Saving…' : 'Save Changes'}
                    </button>
                </div>
            </div>
            )}

            {/* Basic information — hidden in component mode */}
            {!componentMode && <div className="rounded-lg border border-slate-200 bg-white px-5 py-4 space-y-4">
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
                            <span className="inline-flex items-center rounded-l-lg border border-r-0 border-slate-300 bg-slate-50 px-3 text-xs text-slate-500 select-none">api/v1/</span>
                            <input className="form-input rounded-l-none font-mono" {...register('apiKey')} placeholder="blog-post" />
                        </div>
                        {errors.apiKey && <p className="form-error">{errors.apiKey.message}</p>}
                    </div>
                    <div className="col-span-2">
                        <label className="form-label">Description (optional)</label>
                        <input className="form-input mt-1" {...register('description')} placeholder="Describe this content type…" />
                    </div>
                    <div>
                        <label className="form-label">Localization Mode</label>
                        <select className="form-input mt-1" {...register('localizationMode')}>
                            <option value="PerLocale">Per-locale fields</option>
                            <option value="Shared">Shared (locale-independent)</option>
                        </select>
                    </div>
                    <div>
                        <label className="form-label">Kind</label>
                        <select className="form-input mt-1" {...register('kind')}>
                            <option value="Content">Content — standard headless entry</option>
                            <option value="Page">Page — linked to a site page</option>
                        </select>
                        <p className="mt-1 text-xs text-slate-400">Page-kind entries trigger the page creation wizard on new entry.</p>
                    </div>
                    {watch('kind') === 'Page' && (
                        <div className="col-span-2">
                            <label className="form-label">Default Template</label>
                            <select className="form-input mt-1" {...register('siteTemplateId')}>
                                <option value="">— No default template —</option>
                                {(siteTemplates ?? []).map((t) => <option key={t.id} value={t.id}>{t.name}</option>)}
                            </select>
                            <p className="mt-1 text-xs text-slate-400">Pages of this type inherit this template. Individual pages can override it.</p>
                        </div>
                    )}
                    <div className="col-span-2">
                        <label className="form-label">Inherits From (optional)</label>
                        <select className="form-input mt-1" {...register('parentContentTypeId')}>
                            <option value="">— No parent —</option>
                            {(allContentTypes ?? []).map((ct) => (
                                <option key={ct.id} value={ct.id}>{ct.displayName} ({ct.handle})</option>
                            ))}
                        </select>
                        <p className="mt-1 text-xs text-slate-400">Fields from the parent are merged in (read-only) at the top of the schema.</p>
                    </div>
                </div>
            </div>}

            {/* Inherited fields (read-only) */}
            {inheritedFields && inheritedFields.length > 0 && (
                <InheritedFieldsSection fields={inheritedFields} parentHandle={parentHandle} />
            )}

            {/* Field groups */}
            <div className="space-y-4">
                <div className="flex items-center justify-between">
                    <h3 className="text-sm font-semibold text-slate-800">Fields</h3>
                </div>

                {groups.map((group) => {
                    const groupFieldIndices = fields
                        .map((f, globalIdx) => ({ globalIdx, fieldId: f.id, groupName: currentFields?.[globalIdx]?.groupName ?? DEFAULT_GROUP }))
                        .filter(({ groupName }) => groupName === group);

                    return (
                        <div key={group}>
                            <GroupSection
                                groupName={group}
                                contentTypeId={contentTypeId}
                                allFieldIndices={groupFieldIndices}
                                register={register}
                                watch={watch}
                                setValue={setValue}
                                errors={errors}
                                onRemoveField={remove}
                                onMoveField={move}
                                onRenameGroup={renameGroup}
                                onDeleteGroup={deleteGroup}
                            />
                            <button
                                type="button"
                                onClick={() => addField(group)}
                                className="mt-2 ml-1 text-xs text-brand-600 hover:text-brand-800 font-medium"
                            >
                                + Add Field to {group}
                            </button>
                        </div>
                    );
                })}

                {/* Add new group */}
                <div className="rounded-lg border border-dashed border-slate-300 px-4 py-3 flex items-center gap-3">
                    <input
                        type="text"
                        className="form-input text-xs flex-1"
                        placeholder="New group name…"
                        value={newGroupName}
                        onChange={(e) => setNewGroupName(e.target.value)}
                        onKeyDown={(e) => { if (e.key === 'Enter') { e.preventDefault(); addGroup(); } }}
                    />
                    <button
                        type="button"
                        onClick={addGroup}
                        disabled={!newGroupName.trim()}
                        className="btn-secondary text-xs disabled:opacity-50"
                    >
                        + Add Group
                    </button>
                </div>
            </div>

            {/* Bottom save/cancel — hidden in component mode */}
            {!componentMode && (
            <div className="flex justify-end gap-3 pb-2">
                <button type="button" onClick={onCancel} className="btn-secondary">Cancel</button>
                <button type="submit" disabled={isSubmitting} className="btn-primary disabled:opacity-50">
                    {isSubmitting ? 'Saving…' : 'Save Changes'}
                </button>
            </div>
            )}
        </div>
    );
}
