import type { ComponentCategory } from '@/types';

export type ViewportSize = 'desktop' | 'tablet' | 'mobile';

// ─── Layout zone tree (from layout definition) ────────────────────────────────

export interface LayoutColumnDef {
  span: number;          // 1–12; all columns in a row must sum to 12
  zoneName: string; // e.g. "content-col-8", "content-col-4"
}

export type LayoutZoneNodeType = 'html-element' | 'drop-zone' | 'zone' | 'grid-row';

export interface LayoutZoneNode {
  id: string;
  type: LayoutZoneNodeType;
  name: string;      // machine name used in HTML token, e.g. "header"
  label: string;         // display label in designer
  sortOrder: number;
  // html-element fields
  tag?: string;
  cssClass?: string;
  htmlAttributes?: Record<string, string>;
  children?: LayoutZoneNode[];
  // legacy grid-row fields
  columns?: LayoutColumnDef[];
}

// ─── Default placement on a layout (inherited by pages) ──────────────────────

export interface LayoutDefaultPlacement {
  componentId: string;
  componentName: string;
  componentCategory: ComponentCategory;
  zone: string;
  sortOrder: number;
  isLocked: boolean;  // if true, page designer cannot remove it — only rebind data
}

// ─── Page/Layout template placement tree ─────────────────────────────────────

export type PlacementNodeType = 'component' | 'grid-row';

export interface GridColumn {
  span: number;
  zoneName: string;
  placements: PlacementNode[];   // only 'component' type allowed at this depth
}

export interface PlacementNode {
  /** Local UUID for React key + DnD identification (not persisted) */
  localId: string;
  type: PlacementNodeType;
  zone: string;
  sortOrder: number;

  // component-only fields:
  componentId?: string;
  componentName?: string;
  componentKey?: string;
  componentCategory?: ComponentCategory;
  boundItemId?: string;        // template-level bound item (from SiteTemplate JSON)
  boundEntryId?: string;       // page-level override entry ID
  templateBoundItemId?: string; // original template default — used to detect & reset page overrides
  boundItemTitle?: string;     // denormalized display title
  isLayoutDefault?: boolean;   // inherited from layout — cannot be removed, only rebound

  // grid-row-only fields:
  columns?: GridColumn[];
}

// ─── Legacy alias kept for gradual migration ──────────────────────────────────
/** @deprecated Use PlacementNode instead */
export type DesignerPlacement = PlacementNode;
