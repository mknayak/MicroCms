import { z } from 'zod';
import type { FieldType } from '@/types';

// ─── Field type options (shared across ContentType and Component editors) ─────

export const FIELD_TYPES: { value: FieldType; label: string }[] = [
  { value: 'ShortText',      label: 'Short Text' },
  { value: 'LongText',       label: 'Long Text' },
  { value: 'RichText',       label: 'Rich Text' },
  { value: 'Markdown',       label: 'Markdown' },
  { value: 'Integer',        label: 'Integer' },
  { value: 'Decimal',        label: 'Decimal' },
  { value: 'Boolean',        label: 'Boolean' },
  { value: 'DateTime',       label: 'Date & Time' },
  { value: 'Enum',           label: 'Select / Enum' },
  { value: 'Reference',      label: 'Reference' },
  { value: 'AssetReference', label: 'Asset' },
  { value: 'Json',           label: 'JSON' },
  { value: 'Component',      label: 'Component' },
  { value: 'Location',       label: 'Location' },
  { value: 'Color',          label: 'Color' },
];

export const FIELD_TYPE_VALUES = FIELD_TYPES.map((ft) => ft.value) as [FieldType, ...FieldType[]];

// ─── Canonical field schema (used by both ContentType and Component forms) ────

/**
 * Canonical Zod schema for a single field row.
 *
 * Property names match the Component editor convention (isRequired / isLocalized)
 * because they map directly to the API payload; ContentTypeEditPage is migrated
 * to use these same names.
 */
export const fieldRowSchema = z.object({
  /** Existing field id — present when editing, absent when adding a new field. */
  id: z.string().optional(),
  /** Display label shown in the admin UI (e.g. "Hero Title"). */
  name: z.string().min(1, 'Name is required'),
  fieldType: z.enum(FIELD_TYPE_VALUES),
  isRequired: z.boolean(),
  isLocalized: z.boolean(),
  isIndexed: z.boolean(),
  isUnique: z.boolean(),
  isList: z.boolean(),
  description: z.string().optional(),
});

export type FieldRowValue = z.infer<typeof fieldRowSchema>;

// ─── Utilities ────────────────────────────────────────────────────────────────

/** Converts a human label ("Hero Title") to a camelCase API handle ("heroTitle"). */
export function toCamelCase(str: string): string {
  return str
    .trim()
    .replace(/[^a-zA-Z0-9]+(.)/g, (_, chr: string) => chr.toUpperCase())
    .replace(/^[A-Z]/, (c) => c.toLowerCase())
    .replace(/[^a-zA-Z0-9]/g, '');
}
