import { z } from 'zod';

// ─── Schemas ──────────────────────────────────────────────────────────────────

export const slugPattern = /^[a-z0-9]+(?:-[a-z0-9]+)*$/;

export const staticSchema = z.object({
  title: z.string().min(1, 'Title is required'),
  slug: z.string().min(1).regex(slugPattern, 'Lowercase, numbers and hyphens only'),
  parentId: z.string().optional().transform((v) => (v === '' ? undefined : v)),
});

export const collectionSchema = z.object({
  title: z.string().min(1, 'Title is required'),
  slug: z.string().min(1).regex(slugPattern, 'Lowercase, numbers and hyphens only'),
  contentTypeId: z.string().min(1, 'Content type is required'),
  routePattern: z.string().min(1, 'Route pattern is required'),
  parentId: z.string().optional().transform((v) => (v === '' ? undefined : v)),
});

export type StaticForm = z.infer<typeof staticSchema>;
export type CollectionForm = z.infer<typeof collectionSchema>;
