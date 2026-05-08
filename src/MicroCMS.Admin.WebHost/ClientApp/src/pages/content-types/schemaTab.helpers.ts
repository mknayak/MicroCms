import type { ContentType, FieldDefinitionDto } from '@/types';
import type { SchemaFormValues } from './schemaTab.types';
import { DEFAULT_GROUP } from './schemaTab.types';

export function toCamelCase(str: string): string {
    return str
        .trim()
        .replace(/[^a-zA-Z0-9]+(.)/g, (_, chr: string) => chr.toUpperCase())
        .replace(/^[A-Z]/, (c) => c.toLowerCase())
        .replace(/[^a-zA-Z0-9]/g, '');
}

export function toFormFields(ct: ContentType): SchemaFormValues['fields'] {
    return (ct.fields ?? [])
        .slice()
        .sort((a, b) => a.sortOrder - b.sortOrder)
        .map((f: FieldDefinitionDto) => ({
            id: f.id,
            name: f.label,
            type: f.fieldType as SchemaFormValues['fields'][number]['type'],
            required: f.isRequired,
            localized: f.isLocalized,
            isIndexed: f.isIndexed,
            isUnique: f.isUnique,
            isList: f.isList,
            groupName: f.groupName ?? DEFAULT_GROUP,
            enumMode: (f.dynamicSource ? 'dynamic' : 'static') as 'static' | 'dynamic',
            staticOptions: f.options ?? [],
            dynamicSource: f.dynamicSource
                ? {
                    contentTypeHandle: f.dynamicSource.contentTypeHandle ?? '',
                    labelField: f.dynamicSource.labelField ?? '',
                    valueField: f.dynamicSource.valueField ?? '',
                    statusFilter: f.dynamicSource.statusFilter ?? 'Published',
                    groupHandle: f.dynamicSource.groupHandle ?? '',
                }
                : { contentTypeHandle: '', labelField: '', valueField: '', statusFilter: 'Published', groupHandle: '' },
            multiListSource: f.multiListSource
                ? {
                    contentTypeHandle: f.multiListSource.contentTypeHandle ?? '',
                    labelField: f.multiListSource.labelField ?? '',
                    valueField: f.multiListSource.valueField ?? '',
                    statusFilter: f.multiListSource.statusFilter ?? 'Published',
                    groupHandle: f.multiListSource.groupHandle ?? '',
                }
                : { contentTypeHandle: '', labelField: '', valueField: '', statusFilter: 'Published', groupHandle: '' },
        }));
}

/** Returns unique group names ordered by the first field's SortOrder. "Default" is always first. */
export function orderedGroups(fields: Array<{ groupName: string }>): string[] {
    const seen = new Set<string>();
    const groups: string[] = [];
    if (fields.some((f) => f.groupName === DEFAULT_GROUP || !f.groupName)) {
        seen.add(DEFAULT_GROUP);
        groups.push(DEFAULT_GROUP);
    }
    for (const f of fields) {
        const g = f.groupName || DEFAULT_GROUP;
        if (!seen.has(g)) { seen.add(g); groups.push(g); }
    }
    return groups;
}
