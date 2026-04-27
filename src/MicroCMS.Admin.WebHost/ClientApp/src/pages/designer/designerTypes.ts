import type { ComponentCategory } from '@/types';

export type ViewportSize = 'desktop' | 'tablet' | 'mobile';

// ─── Layout zone tree (from layout definition) ────────────────────────────────

export interface LayoutColumnDef {
  span: number;          // 1–12; all columns in a row must sum to 12
  zoneName: string; // e.g. "content-col-8", "content-col-4"
}

export type LayoutZoneNodeType = 'zone' | 'grid-row';

export interface LayoutZoneNode {
  id: string;
  type: LayoutZoneNodeType;
  name: string;      // machine name used in HTML token, e.g. "header"
  label: string;         // display label in designer
  sortOrder: number;
  columns?: LayoutColumnDef[];  // only present when type === 'grid-row'
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
  boundItemId?: string;   // linked ComponentItem ID
  boundItemTitle?: string;     // denormalized display title
  isLayoutDefault?: boolean;   // inherited from layout — cannot be removed, only rebound

  // grid-row-only fields:
  columns?: GridColumn[];
}

// ─── Legacy alias kept for gradual migration ──────────────────────────────────
/** @deprecated Use PlacementNode instead */
export type DesignerPlacement = PlacementNode;
