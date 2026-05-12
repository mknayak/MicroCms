/**
 * Loads layout designer presets from layoutPresets.json and converts the
 * declarative JSON tree format into LayoutZoneNode runtime objects.
 *
 * To add or modify presets edit `src/data/layoutPresets.json` — no code
 * changes needed here unless the JSON schema changes.
 */

import type { LayoutZoneNode } from '@/types';
import presetsJson from '@/data/layoutPresets.json';

// ─── JSON schema types ────────────────────────────────────────────────────────

interface JsonZoneNode {
  kind: 'zone';
  label: string;
}

interface JsonElementNode {
  tag: string;
  label: string;
  cssClass?: string;
  htmlAttributes?: Record<string, string>;
  children?: JsonChild[];
}

type JsonChild = JsonZoneNode | JsonElementNode;

interface JsonTagPreset {
  kind: 'tag';
  tag: string;
  color: string;
}

interface JsonSnippetPreset {
  kind: 'snippet';
  label: string;
  color: string;
  tree: JsonElementNode;
}

type JsonPreset = JsonTagPreset | JsonSnippetPreset;

interface JsonGroup {
  group: string;
  items: JsonPreset[];
}

// ─── Runtime preset types (consumed by the designer) ─────────────────────────

export interface TagPreset {
  kind: 'tag';
  tag: string;
  color: string;
}

export interface SnippetPreset {
  kind: 'snippet';
  label: string;
  color: string;
  build: () => LayoutZoneNode;
}

export type AnyPreset = TagPreset | SnippetPreset;

export interface PresetGroup {
  group: string;
  items: AnyPreset[];
}

// ─── ID generator (duplicated here to keep this module self-contained) ────────

function uid() { return Math.random().toString(36).slice(2, 10); }

// ─── JSON → LayoutZoneNode converter ─────────────────────────────────────────

function buildChild(node: JsonChild): LayoutZoneNode {
  if ('kind' in node && node.kind === 'zone') {
    return {
      id: uid(),
      type: 'drop-zone',
      name: `zone-${uid()}`,
      label: node.label,
      sortOrder: 0,
    };
  }

  const el = node as JsonElementNode;
  return {
    id: uid(),
    type: 'html-element',
    name: `${el.tag}-${uid()}`,
    label: el.label,
    sortOrder: 0,
    tag: el.tag,
    cssClass: el.cssClass ?? '',
    htmlAttributes: el.htmlAttributes ?? {},
    children: (el.children ?? []).map((c, i) => ({ ...buildChild(c), sortOrder: i })),
  };
}

// ─── Tag colour map from JSON ─────────────────────────────────────────────────

export const TAG_COLOR_MAP: Record<string, string> =
  (presetsJson as { tagColors: Record<string, string>; groups: JsonGroup[] }).tagColors;

// ─── Resolved preset groups ───────────────────────────────────────────────────

export const PRESET_GROUPS: PresetGroup[] =
  (presetsJson as { tagColors: Record<string, string>; groups: JsonGroup[] }).groups.map(
    (g) => ({
      group: g.group,
      items: g.items.map((item): AnyPreset => {
        if (item.kind === 'tag') {
          return { kind: 'tag', tag: item.tag, color: item.color };
        }
        const snippet = item as JsonSnippetPreset;
        return {
          kind: 'snippet',
          label: snippet.label,
          color: snippet.color,
          // Each call to build() creates fresh nodes with new IDs
          build: () => buildChild(snippet.tree) as LayoutZoneNode,
        };
      }),
    }),
  );
