import { z } from 'zod';
import type { FieldType } from '@/types';

// ─── Constants ────────────────────────────────────────────────────────────────

export const FIELD_TYPES: { value: FieldType; label: string }[] = [
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
    { value: 'MultiList', label: 'Multi List' },
];

export const FIELD_TYPE_VALUES = FIELD_TYPES.map((ft) => ft.value) as [FieldType, ...FieldType[]];
export const API_KEY_REGEX = /^[a-z0-9][a-z0-9_-]*[a-z0-9]$|^[a-z0-9]$/;
export const DEFAULT_GROUP = 'Default';

// ─── Zod schemas ──────────────────────────────────────────────────────────────

export const dynamicSourceSchema = z.object({
    contentTypeHandle: z.string(),
    labelField: z.string().optional().default(''),
    valueField: z.string().optional().default(''),
    statusFilter: z.string().optional().default('Published'),
    groupHandle: z.string().optional().default(''),
});

export const fieldSchema = z.object({
    id: z.string().optional(),
    name: z.string().min(1, 'Name is required'),
    type: z.enum(FIELD_TYPE_VALUES),
    required: z.boolean(),
    localized: z.boolean(),
    isIndexed: z.boolean(),
    isUnique: z.boolean(),
    isList: z.boolean(),
    groupName: z.string().min(1, 'Group is required').default(DEFAULT_GROUP),
    /** 'static' | 'dynamic' — only relevant when type === 'Enum' */
    enumMode: z.enum(['static', 'dynamic']),
    /** Static option list for Enum */
    staticOptions: z.array(z.string()),
    /** Dynamic source config for Enum and Reference fields */
    dynamicSource: dynamicSourceSchema.optional(),
    /** Dynamic source config for MultiList fields */
    multiListSource: dynamicSourceSchema.optional(),
});

export const schemaFormSchema = z.object({
    name: z.string().min(1, 'Name is required').max(200),
    apiKey: z.string().min(1, 'API key is required').max(64).regex(API_KEY_REGEX, 'Lowercase, digits, hyphens only'),
    description: z.string().max(500).optional(),
    localizationMode: z.enum(['PerLocale', 'Shared']),
    kind: z.enum(['Content', 'Page']),
    siteTemplateId: z.string().optional(),
    /** UUID of the parent content type, or empty string for none. */
    parentContentTypeId: z.string().optional(),
    fields: z.array(fieldSchema),
});

export type SchemaFormValues = z.infer<typeof schemaFormSchema>;
export type FieldFormValue = z.infer<typeof fieldSchema>;

// Separate validator with cross-field rules; cast so useForm<SchemaFormValues> stays typed.
export const schemaFormValidator = schemaFormSchema.superRefine((values, ctx) => {
    values.fields.forEach((field, i) => {
        if (field.type === 'MultiList') {
            if (!field.multiListSource?.contentTypeHandle?.trim()) {
                ctx.addIssue({ code: z.ZodIssueCode.custom, message: 'Source content type is required', path: ['fields', i, 'multiListSource', 'contentTypeHandle'] });
            }
        }
        if (field.type === 'Reference') {
            if (!field.dynamicSource?.contentTypeHandle?.trim()) {
                ctx.addIssue({ code: z.ZodIssueCode.custom, message: 'Source content type is required', path: ['fields', i, 'dynamicSource', 'contentTypeHandle'] });
            }
        }
        if (field.type === 'Enum' && field.enumMode === 'dynamic') {
            if (!field.dynamicSource?.contentTypeHandle?.trim()) {
                ctx.addIssue({ code: z.ZodIssueCode.custom, message: 'Source content type is required', path: ['fields', i, 'dynamicSource', 'contentTypeHandle'] });
            }
            if (!field.dynamicSource?.labelField?.trim()) {
                ctx.addIssue({ code: z.ZodIssueCode.custom, message: 'Label field is required', path: ['fields', i, 'dynamicSource', 'labelField'] });
            }
            if (!field.dynamicSource?.valueField?.trim()) {
                ctx.addIssue({ code: z.ZodIssueCode.custom, message: 'Value field is required', path: ['fields', i, 'dynamicSource', 'valueField'] });
            }
        }
    });
}) as z.ZodType<SchemaFormValues>;

// ─── Blank field factory ──────────────────────────────────────────────────────

export function makeBlankField(groupName: string = DEFAULT_GROUP): FieldFormValue {
    return {
        name: '',
        type: 'ShortText',
        required: false,
        localized: false,
        isIndexed: false,
        isUnique: false,
        isList: false,
        groupName,
        enumMode: 'static',
        staticOptions: [],
        dynamicSource: { contentTypeHandle: '', labelField: '', valueField: '', statusFilter: 'Published', groupHandle: '' },
        multiListSource: { contentTypeHandle: '', labelField: '', valueField: '', statusFilter: 'Published', groupHandle: '' },
    };
}
