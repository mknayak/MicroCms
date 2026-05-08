import { formatDistanceToNow } from 'date-fns';
import type { ContentType, FieldDefinitionDto } from '@/types';
import { FIELD_TYPE_COLORS, FIELD_TYPE_LABELS } from './contentTypeDetail.shared';
import { orderedGroups } from './schemaTab.helpers';
import { DEFAULT_GROUP } from './schemaTab.types';

// ─── FieldCheck ───────────────────────────────────────────────────────────────

export function FieldCheck({ value }: { value: boolean }) {
    if (value) {
        return (
            <span className="inline-flex h-5 w-5 items-center justify-center rounded bg-green-500 text-white text-xs">✓</span>
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
        <tr className={`group ${field.isInherited ? 'bg-slate-50/60' : 'hover:bg-slate-50'}`}>
            <td className="px-3 py-3 text-slate-200">{field.isInherited ? '' : '⠿'}</td>
            <td className="px-4 py-3">
                <div className="flex items-center gap-2">
                    <p className={`font-medium ${field.isInherited ? 'text-slate-400' : 'text-slate-800'}`}>{field.label}</p>
                    {field.isInherited && (
                        <span className="rounded bg-slate-100 px-1.5 py-0.5 text-[10px] font-semibold uppercase tracking-wide text-slate-400">Inherited</span>
                    )}
                </div>
                {field.description && <p className="text-xs text-slate-400 truncate max-w-xs">{field.description}</p>}
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
                {!field.isInherited && (
                    <button onClick={onEdit} className="opacity-0 group-hover:opacity-100 transition-opacity rounded px-2 py-1 text-xs font-medium text-brand-600 hover:bg-brand-50">
                        Edit
                    </button>
                )}
            </td>
        </tr>
    );
}

// ─── GroupFieldsTable ─────────────────────────────────────────────────────────

function GroupFieldsTable({ fields, onEdit }: { groupName?: string; fields: FieldDefinitionDto[]; onEdit: () => void }) {
    return (
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
                    {fields.length === 0 ? (
                        <tr>
                            <td colSpan={9} className="px-4 py-6 text-center text-slate-400 text-xs">No fields in this group.</td>
                        </tr>
                    ) : (
                        fields.map((f) => <ReadOnlyFieldRow key={f.id} field={f} onEdit={onEdit} />)
                    )}
                </tbody>
            </table>
        </div>
    );
}

// ─── SchemaReadOnlyView ───────────────────────────────────────────────────────

export function SchemaReadOnlyView({
    contentType,
    siteTemplates,
    onEdit,
}: {
    contentType: ContentType;
    siteTemplates?: Array<{ id: string; name: string }>;
    onEdit: () => void;
}) {
    const sortedFields = (contentType.fields ?? []).slice().sort((a, b) => a.sortOrder - b.sortOrder);
    const groups = orderedGroups(sortedFields);

    return (
        <div className="space-y-4">
            {/* Header */}
            <div className="flex items-start justify-between gap-4">
                <div>
                    <p className="text-sm font-medium text-slate-700">Schema Definition — {contentType.displayName}</p>
                    <p className="mt-0.5 text-xs text-slate-400">
                        Last edited {formatDistanceToNow(new Date(contentType.updatedAt), { addSuffix: true })}
                    </p>
                </div>
                <div className="flex items-center gap-2 shrink-0">
                    <button onClick={onEdit} className="btn-primary text-sm">✏ Edit Schema</button>
                </div>
            </div>

            {/* Info banner */}
            <div className="flex items-start gap-2 rounded-lg border border-blue-100 bg-blue-50 px-4 py-3 text-sm text-blue-700">
                <span className="mt-0.5">ℹ</span>
                <span>Click <strong>Edit Schema</strong> to add, remove, or reorder fields.</span>
            </div>

            {/* Parent inheritance banner */}
            {contentType.parentContentTypeId && (
                <div className="flex items-center gap-2 rounded-lg border border-indigo-200 bg-indigo-50 px-4 py-2.5 text-sm text-indigo-800">
                    <span>🧬</span>
                    <span>
                        Inherits fields from{' '}
                        <strong>{contentType.parentHandle ?? contentType.parentContentTypeId}</strong>.{' '}
                        Inherited fields have an{' '}
                        <span className="rounded bg-slate-100 px-1 py-0.5 text-[10px] font-semibold uppercase tracking-wide text-slate-500">Inherited</span>{' '}
                        badge and cannot be edited here.
                    </span>
                </div>
            )}

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
                            {contentType.localizationMode === 'Shared' ? 'Shared (locale-independent)' : 'Per-locale fields'}
                        </p>
                    </div>
                    {contentType.kind === 'Page' && (
                        <div>
                            <p className="text-xs font-medium text-slate-500 uppercase mb-1">Default Template</p>
                            <p className="text-slate-700">
                                {contentType.siteTemplateId
                                    ? (siteTemplates?.find((t) => t.id === contentType.siteTemplateId)?.name ?? contentType.siteTemplateId)
                                    : <span className="text-slate-400">None</span>}
                            </p>
                        </div>
                    )}
                    {contentType.parentContentTypeId && (
                        <div>
                            <p className="text-xs font-medium text-slate-500 uppercase mb-1">Inherits From</p>
                            <p className="font-mono text-slate-700">{contentType.parentHandle ?? contentType.parentContentTypeId}</p>
                        </div>
                    )}
                </div>
            </div>

            {/* Fields grouped by section */}
            {groups.map((group) => {
                const groupFields = sortedFields.filter((f) => (f.groupName ?? DEFAULT_GROUP) === group);
                return (
                    <div key={group} className="rounded-lg border border-slate-200 overflow-hidden">
                        <div className="flex items-center justify-between bg-slate-50 px-4 py-2 border-b border-slate-200">
                            <span className="text-xs font-semibold text-slate-600 uppercase tracking-wide">{group}</span>
                            <span className="text-xs text-slate-400">{groupFields.length} field{groupFields.length !== 1 ? 's' : ''}</span>
                        </div>
                        <GroupFieldsTable groupName={group} fields={groupFields} onEdit={onEdit} />
                    </div>
                );
            })}

            {sortedFields.length === 0 && (
                <div className="rounded-lg border border-slate-200 bg-white px-4 py-12 text-center text-slate-400">
                    No fields defined yet.{' '}
                    <button onClick={onEdit} className="text-brand-600 hover:underline">Add the first field</button>.
                </div>
            )}

            {/* Field type legend */}
            <div className="flex flex-wrap items-center gap-2 text-xs text-slate-500">
                <span className="font-medium">Field Types:</span>
                {Object.entries(FIELD_TYPE_LABELS).map(([k, v]) => (
                    <span key={k} className={`rounded px-2 py-0.5 font-medium ${FIELD_TYPE_COLORS[k] ?? 'bg-slate-100 text-slate-600'}`}>{v}</span>
                ))}
            </div>
        </div>
    );
}
