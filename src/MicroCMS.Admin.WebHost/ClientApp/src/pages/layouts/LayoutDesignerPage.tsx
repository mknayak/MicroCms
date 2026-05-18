import { useState, useCallback, useRef, useEffect, type MutableRefObject } from 'react';
import { useParams, Link } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { layoutsApi } from '@/api/layouts';
import { useSite } from '@/contexts/SiteContext';
import type {
  LayoutZoneNode,
  LayoutConfig,
  LayoutAsset,
  LayoutBodyAttribute,
  LayoutAssetType,
  LayoutAssetPosition,
} from '@/types';
import { ApiError } from '@/api/client';
import { PRESET_GROUPS, TAG_COLOR_MAP, type AnyPreset } from '@/data/layoutPresets';

// ─── Tree helpers ─────────────────────────────────────────────────────────────

function uid() { return Math.random().toString(36).slice(2, 10); }

function countDropZones(nodes: LayoutZoneNode[]): number {
  let count = 0;
  for (const n of nodes) {
    if (n.type === 'drop-zone' || n.type === 'zone') count++;
    if (n.children) count += countDropZones(n.children);
  }
  return count;
}

function findNode(nodes: LayoutZoneNode[], id: string): LayoutZoneNode | null {
  for (const n of nodes) {
    if (n.id === id) return n;
    if (n.children) {
      const found = findNode(n.children, id);
      if (found) return found;
    }
  }
  return null;
}

function removeNode(nodes: LayoutZoneNode[], id: string): LayoutZoneNode[] {
  return nodes
    .filter(n => n.id !== id)
    .map(n => n.children ? { ...n, children: removeNode(n.children, id) } : n)
    .map((n, i) => ({ ...n, sortOrder: i }));
}

function updateNode(nodes: LayoutZoneNode[], id: string, patch: Partial<LayoutZoneNode>): LayoutZoneNode[] {
  return nodes.map(n => {
    if (n.id === id) return { ...n, ...patch };
    if (n.children) return { ...n, children: updateNode(n.children, id, patch) };
    return n;
  });
}

function appendChildTo(nodes: LayoutZoneNode[], parentId: string, child: LayoutZoneNode): LayoutZoneNode[] {
  return nodes.map(n => {
    if (n.id === parentId) {
      const children = [...(n.children ?? []), { ...child, sortOrder: (n.children ?? []).length }];
      return { ...n, children };
    }
    if (n.children) return { ...n, children: appendChildTo(n.children, parentId, child) };
    return n;
  });
}

function moveNode(nodes: LayoutZoneNode[], id: string, dir: -1 | 1): LayoutZoneNode[] {
  const idx = nodes.findIndex(n => n.id === id);
  if (idx >= 0) {
    const j = idx + dir;
    if (j < 0 || j >= nodes.length) return nodes;
    const next = [...nodes];
    [next[idx], next[j]] = [next[j], next[idx]];
    return next.map((n, i) => ({ ...n, sortOrder: i }));
  }
  return nodes.map(n => n.children ? { ...n, children: moveNode(n.children, id, dir) } : n);
}

// ─── Drag payload ─────────────────────────────────────────────────────────────

type DropTarget =
  | { kind: 'root' }
  | { kind: 'into'; parentId: string };

// ─── Tag colour map ──────────────────────────────────────────────────────────

// Colour palette — still lives here since it drives rendering, not preset data
const TAG_COLORS: Record<string, { bg: string; text: string; border: string }> = {
  blue:   { bg: 'bg-blue-100',   text: 'text-blue-700',   border: 'border-blue-300' },
  indigo: { bg: 'bg-indigo-100', text: 'text-indigo-700', border: 'border-indigo-300' },
  violet: { bg: 'bg-violet-100', text: 'text-violet-700', border: 'border-violet-300' },
  teal:   { bg: 'bg-teal-100',   text: 'text-teal-700',   border: 'border-teal-300' },
  orange: { bg: 'bg-orange-100', text: 'text-orange-700', border: 'border-orange-300' },
  pink:   { bg: 'bg-pink-100',   text: 'text-pink-700',   border: 'border-pink-300' },
  cyan:   { bg: 'bg-cyan-100',   text: 'text-cyan-700',   border: 'border-cyan-300' },
  green:  { bg: 'bg-green-100',  text: 'text-green-700',  border: 'border-green-300' },
  rose:   { bg: 'bg-rose-100',   text: 'text-rose-700',   border: 'border-rose-300' },
  amber:  { bg: 'bg-amber-100',  text: 'text-amber-700',  border: 'border-amber-300' },
  slate:  { bg: 'bg-slate-100',  text: 'text-slate-600',  border: 'border-slate-300' },
};

// ─── Tag colour lookup (reads color names set in layoutPresets.json) ─────────

function tagColor(tag: string) {
  const key = TAG_COLOR_MAP[tag] ?? 'blue';
  return TAG_COLORS[key] ?? TAG_COLORS.blue;
}

// ─── Known HTML tags (derived from the 'Semantic HTML' preset group) ─────────

const HTML_TAG_PRESETS: { tag: string }[] =
  (PRESET_GROUPS.find(g => g.group === 'Semantic HTML')?.items ?? [])
    .filter((p): p is AnyPreset & { kind: 'tag'; tag: string } => p.kind === 'tag')
    .map(p => ({ tag: (p as { tag: string }).tag }));

// ─── Properties Panel ─────────────────────────────────────────────────────────

function PropertiesPanel({
  node,
  onUpdate,
}: {
  node: LayoutZoneNode | null;
  onUpdate: (id: string, patch: Partial<LayoutZoneNode>) => void;
}) {
  const [newAttrKey, setNewAttrKey] = useState('');
  const [newAttrVal, setNewAttrVal] = useState('');

  if (!node) {
    return (
      <aside className="flex w-72 flex-shrink-0 flex-col border-l border-slate-200 bg-white">
        <div className="flex flex-1 flex-col items-center justify-center gap-3 p-6 text-center">
          <svg className="h-9 w-9 text-slate-200" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <rect x="3" y="3" width="18" height="18" rx="2" strokeWidth="1.5" />
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M3 9h18" />
          </svg>
          <p className="text-sm font-semibold text-slate-400">Select a node to edit properties</p>
        </div>
      </aside>
    );
  }

  const attrs = node.htmlAttributes ?? {};

  const addAttr = () => {
    if (!newAttrKey.trim()) return;
    onUpdate(node.id, { htmlAttributes: { ...attrs, [newAttrKey.trim()]: newAttrVal } });
    setNewAttrKey('');
    setNewAttrVal('');
  };

  const removeAttr = (key: string) => {
    const next = { ...attrs };
    delete next[key];
    onUpdate(node.id, { htmlAttributes: next });
  };

  return (
    <aside className="flex w-72 flex-shrink-0 flex-col overflow-y-auto border-l border-slate-200 bg-white">
      <div className="border-b border-slate-200 px-4 py-3">
        <p className="text-xs font-bold uppercase tracking-wider text-slate-400">Properties</p>
      </div>
      <div className="space-y-4 px-4 py-4">
        <div>
          <label className="form-label">Label</label>
          <input className="form-input mt-1" value={node.label}
            onChange={(e) => onUpdate(node.id, { label: e.target.value })} />
        </div>
        <div>
          <label className="form-label">Machine Name</label>
          <div className="mt-1 rounded-md border border-slate-200 bg-slate-50 px-3 py-2 font-mono text-xs text-slate-600">{node.name}</div>
          <p className="mt-1 text-[10px] text-slate-400">Cannot be changed after creation.</p>
        </div>
        <div>
          <label className="form-label">Type</label>
          {node.type === 'html-element' ? (
            <div className={`mt-1 inline-flex items-center gap-1.5 rounded-md px-2 py-1 text-xs font-semibold ${tagColor(node.tag ?? 'div').bg} ${tagColor(node.tag ?? 'div').text}`}>
              <code>&lt;{node.tag ?? 'div'}&gt;</code> HTML Element
            </div>
          ) : (
            <div className="mt-1 inline-flex items-center gap-1.5 rounded-md bg-brand-100 px-2 py-1 text-xs font-semibold text-brand-700">
              ⬡ Drop Zone
            </div>
          )}
        </div>
        {node.type === 'html-element' && (
          <>
            <div>
              <label className="form-label">HTML Tag</label>
              <select className="form-input mt-1" value={node.tag ?? 'div'}
                onChange={(e) => onUpdate(node.id, { tag: e.target.value })}>
                {HTML_TAG_PRESETS.map(p => <option key={p.tag} value={p.tag}>{p.tag}</option>)}
                <option value="span">span</option>
                <option value="ul">ul</option>
                <option value="li">li</option>
              </select>
            </div>
            <div>
              <label className="form-label">CSS Class</label>
              <input className="form-input mt-1 font-mono text-xs"
                placeholder="e.g. container mx-auto flex"
                value={node.cssClass ?? ''}
                onChange={(e) => onUpdate(node.id, { cssClass: e.target.value })} />
            </div>
            <div>
              <label className="form-label">HTML Attributes</label>
              <div className="mt-2 space-y-1.5">
                {Object.entries(attrs).map(([k, v]) => (
                  <div key={k} className="flex items-center gap-1.5 rounded border border-slate-200 bg-slate-50 px-2 py-1">
                    <code className="flex-1 truncate text-[11px] text-slate-700">{k}=&quot;{v}&quot;</code>
                    <button onClick={() => removeAttr(k)} className="text-slate-300 hover:text-red-500">
                      <svg className="h-3 w-3" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                      </svg>
                    </button>
                  </div>
                ))}
                <div className="flex gap-1">
                  <input className="form-input h-7 w-24 py-0 text-xs" placeholder="attr"
                    value={newAttrKey} onChange={(e) => setNewAttrKey(e.target.value)}
                    onKeyDown={(e) => e.key === 'Enter' && addAttr()} />
                  <input className="form-input h-7 flex-1 py-0 font-mono text-xs" placeholder="value"
                    value={newAttrVal} onChange={(e) => setNewAttrVal(e.target.value)}
                    onKeyDown={(e) => e.key === 'Enter' && addAttr()} />
                  <button onClick={addAttr}
                    className="rounded border border-slate-200 px-2 text-xs font-semibold text-slate-600 hover:border-brand-400 hover:text-brand-700">+</button>
                </div>
              </div>
            </div>
          </>
        )}
      </div>
    </aside>
  );
}

// ─── Tree Node Card ───────────────────────────────────────────────────────────

function NodeCard({
  node,
  depth,
  selectedId,
  dragNodeId,
  onSelect,
  onRemove,
  onMove,
  onAddChild,
  onDrop,
  onDragStart,
}: {
  node: LayoutZoneNode;
  depth: number;
  selectedId: string | null;
  dragNodeId: MutableRefObject<string | null>;
  onSelect: (id: string) => void;
  onRemove: (id: string) => void;
  onMove: (id: string, dir: -1 | 1) => void;
  onAddChild: (parentId: string, type: 'html-element' | 'drop-zone') => void;
  onDrop: (target: DropTarget) => void;
  onDragStart: (id: string) => void;
}) {
  const [dropOver, setDropOver] = useState(false);
  const [collapsed, setCollapsed] = useState(false);
  const isElement = node.type === 'html-element';
  const hasChildren = isElement && (node.children ?? []).length > 0;
  const isSelected = selectedId === node.id;
  const tc = tagColor(node.tag ?? 'div');

  return (
    <div style={{ marginLeft: depth > 0 ? 12 : 0 }}>
      <div
        draggable
        onDragStart={(e) => { e.stopPropagation(); onDragStart(node.id); e.dataTransfer.effectAllowed = 'move'; }}
        onDragOver={(e) => { if (!isElement) return; e.preventDefault(); e.stopPropagation(); setDropOver(true); }}
        onDragLeave={() => setDropOver(false)}
        onDrop={(e) => { e.preventDefault(); e.stopPropagation(); setDropOver(false); if (isElement) onDrop({ kind: 'into', parentId: node.id }); }}
        onClick={(e) => { e.stopPropagation(); onSelect(node.id); }}
        className={`group relative cursor-pointer rounded-lg border-2 transition-all
          ${isSelected ? 'border-brand-500 bg-brand-50/40'
            : dropOver ? `${tc.border} bg-blue-50/30`
            : 'border-slate-200 hover:border-slate-300'}`}
      >
        {/* Header */}
        <div className="flex items-center gap-2 px-2.5 py-2">
          <span className="cursor-grab text-slate-300 hover:text-slate-500 active:cursor-grabbing">
            <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 8h16M4 16h16" />
            </svg>
          </span>
          {/* Collapse toggle — only for html-elements */}
          {isElement && (
            <button
              onClick={(e) => { e.stopPropagation(); setCollapsed(c => !c); }}
              className="flex-shrink-0 rounded p-0.5 text-slate-400 hover:text-slate-600"
              title={collapsed ? 'Expand' : 'Collapse'}
            >
              <svg className={`h-3 w-3 transition-transform ${collapsed ? '-rotate-90' : ''}`} fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
              </svg>
            </button>
          )}
          {isElement ? (
            <span className={`rounded px-1.5 py-0.5 font-mono text-[10px] font-bold ${tc.bg} ${tc.text}`}>
              &lt;{node.tag ?? 'div'}&gt;
            </span>
          ) : (
            <span className="rounded bg-brand-100 px-1.5 py-0.5 text-[10px] font-bold text-brand-700">⬡ zone</span>
          )}
          <div className="min-w-0 flex-1">
            <span className="truncate text-xs font-semibold text-slate-700">{node.label}</span>
            {node.cssClass && (
              <span className="ml-1.5 font-mono text-[10px] text-slate-400">.{node.cssClass.split(' ')[0]}{node.cssClass.includes(' ') ? '…' : ''}</span>
            )}
            {!isElement && (
              <span className="ml-1.5 font-mono text-[10px] text-slate-400">{node.name}</span>
            )}
            {isElement && collapsed && hasChildren && (
              <span className="ml-1.5 rounded bg-slate-100 px-1 py-0.5 text-[10px] text-slate-400">
                {(node.children ?? []).length} child{(node.children ?? []).length !== 1 ? 'ren' : ''}
              </span>
            )}
          </div>
          <div className="flex gap-0.5 opacity-0 group-hover:opacity-100">
            <button onClick={(e) => { e.stopPropagation(); onMove(node.id, -1); }}
              className="rounded border border-slate-200 p-0.5 text-slate-400 hover:text-slate-600">
              <svg className="h-2.5 w-2.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 15l7-7 7 7" />
              </svg>
            </button>
            <button onClick={(e) => { e.stopPropagation(); onMove(node.id, 1); }}
              className="rounded border border-slate-200 p-0.5 text-slate-400 hover:text-slate-600">
              <svg className="h-2.5 w-2.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
              </svg>
            </button>
          </div>
          <button onClick={(e) => { e.stopPropagation(); onRemove(node.id); }}
            className="rounded p-0.5 text-slate-300 opacity-0 hover:bg-red-50 hover:text-red-500 group-hover:opacity-100">
            <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>

        {/* Children area (html-element only) */}
        {isElement && !collapsed && (
          <div className={`border-t border-dashed border-slate-200 px-2.5 pb-2 pt-1.5 ${dropOver ? 'bg-blue-50/40' : ''}`}>
            {(node.children ?? []).length === 0 ? (
              <div className="flex items-center rounded border border-dashed border-slate-200 px-2 py-1.5 text-[10px] text-slate-400">
                Drop here or add a child below
              </div>
            ) : (
              <div className="space-y-1.5">
                {[...(node.children ?? [])].sort((a, b) => a.sortOrder - b.sortOrder).map((child) => (
                  <NodeCard key={child.id} node={child} depth={depth + 1} selectedId={selectedId}
                    dragNodeId={dragNodeId} onSelect={onSelect} onRemove={onRemove} onMove={onMove}
                    onAddChild={onAddChild} onDrop={onDrop} onDragStart={onDragStart} />
                ))}
              </div>
            )}
            <div className="mt-1.5 flex gap-1">
              <button onClick={(e) => { e.stopPropagation(); onAddChild(node.id, 'html-element'); }}
                className="flex items-center gap-1 rounded border border-slate-200 px-2 py-1 text-[10px] font-semibold text-slate-500 hover:border-blue-300 hover:text-blue-700">
                + Element
              </button>
              <button onClick={(e) => { e.stopPropagation(); onAddChild(node.id, 'drop-zone'); }}
                className="flex items-center gap-1 rounded border border-slate-200 px-2 py-1 text-[10px] font-semibold text-slate-500 hover:border-brand-300 hover:text-brand-700">
                + Zone
              </button>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}

// ─── Config Tab ───────────────────────────────────────────────────────────────

const ASSET_TYPE_OPTIONS: { value: LayoutAssetType; label: string }[] = [
  { value: 'raw-html',   label: 'Raw HTML' },
  { value: 'inline-css', label: 'Inline CSS' },
  { value: 'inline-js',  label: 'Inline JS' },
];

const ASSET_POSITION_OPTIONS: { value: LayoutAssetPosition; label: string }[] = [
  { value: 'head',       label: 'Head' },
  { value: 'body-start', label: 'Body Start' },
  { value: 'body-end',   label: 'Body End' },
];

function emptyAsset(): LayoutAsset {
  return { id: uid(), order: 10, type: 'raw-html', position: 'head', content: '', nonce: false, attributes: {} };
}

function emptyBodyAttr(): LayoutBodyAttribute {
  return { attribute: '', value: '' };
}

function AssetRow({ asset, index, total, onChange, onRemove, onMove }: {
  asset: LayoutAsset; index: number; total: number;
  onChange: (a: LayoutAsset) => void; onRemove: () => void; onMove: (dir: -1 | 1) => void;
}) {
  const set = <K extends keyof LayoutAsset>(k: K, v: LayoutAsset[K]) => onChange({ ...asset, [k]: v });
  const placeholder = asset.type === 'raw-html'
    ? '<link href="https://cdn.example.com/styles.css" rel="stylesheet">'
    : asset.type === 'inline-css' ? 'body { margin: 0; }' : 'console.log("hello");';

  return (
    <div className="rounded-lg border border-slate-200 bg-white p-3">
      <div className="mb-2 flex items-center gap-2">
        <div className="flex flex-col gap-0.5">
          <button disabled={index === 0} onClick={() => onMove(-1)}
            className="rounded border border-slate-200 p-0.5 text-slate-400 hover:text-slate-600 disabled:opacity-30">
            <svg className="h-3 w-3" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 15l7-7 7 7" />
            </svg>
          </button>
          <button disabled={index === total - 1} onClick={() => onMove(1)}
            className="rounded border border-slate-200 p-0.5 text-slate-400 hover:text-slate-600 disabled:opacity-30">
            <svg className="h-3 w-3" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
            </svg>
          </button>
        </div>
        <select value={asset.type} onChange={(e) => set('type', e.target.value as LayoutAssetType)}
          className="form-input h-7 flex-1 py-0 text-xs">
          {ASSET_TYPE_OPTIONS.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
        </select>
        <select value={asset.position} onChange={(e) => set('position', e.target.value as LayoutAssetPosition)}
          className="form-input h-7 w-28 py-0 text-xs">
          {ASSET_POSITION_OPTIONS.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
        </select>
        <input type="number" value={asset.order} onChange={(e) => set('order', Number(e.target.value))}
          className="form-input h-7 w-16 py-0 text-xs" placeholder="Order" />
        <button onClick={onRemove} className="ml-auto rounded p-0.5 text-slate-300 hover:bg-red-50 hover:text-red-500">
          <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
          </svg>
        </button>
      </div>
      <textarea value={asset.content ?? ''} onChange={(e) => set('content', e.target.value)}
        rows={asset.type === 'raw-html' ? 3 : 5}
        className="form-input mt-1 w-full font-mono text-xs" placeholder={placeholder} />
      {asset.type === 'inline-js' && (
        <label className="mt-2 flex cursor-pointer items-center gap-1.5 text-xs text-slate-600">
          <input type="checkbox" checked={asset.nonce} onChange={(e) => set('nonce', e.target.checked)} className="rounded" />
          nonce
        </label>
      )}
    </div>
  );
}

function LayoutConfigPanel({ config, onChange }: { config: LayoutConfig; onChange: (c: LayoutConfig) => void }) {
  const setAssets = (assets: LayoutAsset[]) => onChange({ ...config, assets });
  const setAttrs = (bodyAttributes: LayoutBodyAttribute[]) => onChange({ ...config, bodyAttributes });
  const addAsset = () => setAssets([...config.assets, emptyAsset()]);
  const removeAsset = (i: number) => setAssets(config.assets.filter((_, idx) => idx !== i));
  const updateAsset = (i: number, a: LayoutAsset) => setAssets(config.assets.map((x, idx) => idx === i ? a : x));
  const moveAsset = (i: number, dir: -1 | 1) => {
    const next = [...config.assets];
    const j = i + dir;
    if (j < 0 || j >= next.length) return;
    [next[i], next[j]] = [next[j], next[i]];
    setAssets(next.map((a, idx) => ({ ...a, order: (idx + 1) * 10 })));
  };
  const addAttr = () => setAttrs([...config.bodyAttributes, emptyBodyAttr()]);
  const removeAttr = (i: number) => setAttrs(config.bodyAttributes.filter((_, idx) => idx !== i));
  const updateAttr = (i: number, patch: Partial<LayoutBodyAttribute>) =>
    setAttrs(config.bodyAttributes.map((x, idx) => idx === i ? { ...x, ...patch } : x));

  return (
    <div className="flex min-h-0 flex-1 overflow-auto bg-slate-50 p-6">
      <div className="mx-auto w-full max-w-3xl space-y-8">
        <section>
          <div className="mb-3 flex items-center justify-between">
            <div>
              <p className="text-sm font-bold text-slate-800">Assets</p>
              <p className="text-xs text-slate-400">Stylesheets, scripts, and snippets injected into the shell template.</p>
            </div>
            <button onClick={addAsset} className="btn-primary h-8 px-3 text-xs">+ Add Asset</button>
          </div>
          {config.assets.length === 0 ? (
            <div className="flex flex-col items-center justify-center rounded-xl border-2 border-dashed border-slate-300 bg-white py-10 text-center">
              <p className="text-sm font-semibold text-slate-400">No assets defined</p>
            </div>
          ) : (
            <div className="space-y-2">
              {config.assets.map((asset, i) => (
                <AssetRow key={asset.id} asset={asset} index={i} total={config.assets.length}
                  onChange={(a) => updateAsset(i, a)} onRemove={() => removeAsset(i)} onMove={(dir) => moveAsset(i, dir)} />
              ))}
            </div>
          )}
        </section>

        <section>
          <div className="mb-3 flex items-center justify-between">
            <div>
              <p className="text-sm font-bold text-slate-800">Body Attributes</p>
              <p className="text-xs text-slate-400">Extra attributes on the <code className="rounded bg-slate-100 px-1 text-[11px]">&lt;body&gt;</code> tag.</p>
            </div>
            <button onClick={addAttr} className="btn-primary h-8 px-3 text-xs">+ Add Attribute</button>
          </div>
          {config.bodyAttributes.length === 0 ? (
            <div className="flex flex-col items-center justify-center rounded-xl border-2 border-dashed border-slate-300 bg-white py-10 text-center">
              <p className="text-sm font-semibold text-slate-400">No body attributes defined</p>
            </div>
          ) : (
            <div className="space-y-2">
              {config.bodyAttributes.map((attr, i) => (
                <div key={i} className="flex items-center gap-2 rounded-lg border border-slate-200 bg-white p-3">
                  <input value={attr.attribute} onChange={(e) => updateAttr(i, { attribute: e.target.value })}
                    className="form-input h-7 w-36 py-0 text-xs" placeholder="attribute" />
                  <span className="text-xs text-slate-400">=</span>
                  <input value={attr.value} onChange={(e) => updateAttr(i, { value: e.target.value })}
                    className="form-input h-7 flex-1 py-0 font-mono text-xs" placeholder="value or {{page:slug}}" />
                  <button onClick={() => removeAttr(i)}
                    className="rounded p-0.5 text-slate-300 hover:bg-red-50 hover:text-red-500">
                    <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                    </svg>
                  </button>
                </div>
              ))}
            </div>
          )}
        </section>
      </div>
    </div>
  );
}

// ─── Layout Preview ──────────────────────────────────────────────────────────

/** Inline styles injected into every preview iframe. */
const PREVIEW_EXTRA_CSS = `
  [data-zone]{
    outline: 2px dashed #6366f1;
    outline-offset: -2px;
    background: rgba(99,102,241,.06);
    min-height: 56px;
    position: relative;
  }
  [data-zone]::before{
    content: attr(data-zone-label);
    display: block;
    font-family: monospace;
    font-size: 10px;
    font-weight: 700;
    color: #6366f1;
    padding: 3px 6px 6px;
    letter-spacing: .04em;
  }
  [data-zone]:hover{ background: rgba(99,102,241,.13); }
`;

/**
 * Injects wireframe zone placeholders into a saved shell template.
 *
 * The shell already contains all <link>, <script>, body attrs etc.
 * We only replace the Scriban tokens that the generator emits for each
 * drop-zone:  {{ zone_name }}
 * Each data-zone wrapper div already exists in the shell; we add a
 * data-zone-label attribute so CSS ::before can label it, and empty the
 * token so nothing breaks.
 */
function shellToPreview(shell: string): string {
  // 1. Replace each bare {{ token }} with a synthetic [data-zone] wrapper
  //    so the preview overlay CSS can highlight and label it.
  //    Token names use underscores; convert back to hyphens for the label.
  let html = shell.replace(
    /\{\{([^}]+)\}\}/g,
    (_m, token: string) => {
      const name = token.trim().replace(/_/g, '-');
      return `<div data-zone="${name}" data-zone-label="⬡ ${name}"></div>`;
    },
  );

  // 2. Strip any {{page:*}} or {{site:*}} tokens (language, title, etc.)
  html = html.replace(/\{\{[^}]+\}\}/g, '');

  // 3. Inject our extra overlay CSS just before </head>
  const extraStyle = `<style>${PREVIEW_EXTRA_CSS}</style>`;
  if (html.includes('</head>')) {
    html = html.replace('</head>', `${extraStyle}\n</head>`);
  } else {
    html = extraStyle + html;
  }

  return html;
}

/**
 * Fallback: build a minimal preview from the unsaved node tree.
 * Used only when the layout has never been saved (no shellTemplate yet).
 */
function buildFallbackPreviewHtml(nodes: LayoutZoneNode[]): string {
  function renderNode(n: LayoutZoneNode): string {
    if (n.type === 'drop-zone' || n.type === 'zone') {
      return `<div data-zone="${n.name}" data-zone-label="⬡ ${n.label || n.name}"></div>`;
    }
    if (n.type === 'grid-row' && n.columns?.length) {
      const cols = n.columns
        .map(c => `<div class="col-${c.span}"><div data-zone="${c.zoneName}" data-zone-label="⬡ ${c.zoneName}"></div></div>`)
        .join('');
      return `<div class="row">${cols}</div>`;
    }
    const tag = n.tag ?? 'div';
    const cls = n.cssClass ? ` class="${n.cssClass}"` : '';
    const attrs = Object.entries(n.htmlAttributes ?? {})
      .map(([k, v]) => ` ${k}="${v}"`).join('');
    const children = [...(n.children ?? [])]
      .sort((a, b) => a.sortOrder - b.sortOrder)
      .map(renderNode).join('');
    return `<${tag}${cls}${attrs}>${children}</${tag}>`;
  }
  const body = [...nodes].sort((a, b) => a.sortOrder - b.sortOrder).map(renderNode).join('');
  return `<!DOCTYPE html><html lang="en"><head>
<meta charset="UTF-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<style>${PREVIEW_EXTRA_CSS}</style>
</head><body style="margin:0;padding:16px;background:#f8fafc;">${body}</body></html>`;
}

function LayoutPreview({
  nodes,
  shellTemplate,
  nodesDirty,
}: {
  nodes: LayoutZoneNode[];
  shellTemplate: string | null | undefined;
  nodesDirty: boolean;
}) {
  const iframeRef = useRef<HTMLIFrameElement>(null);

  // Use the real saved shell when available; fall back to tree-built HTML
  // when there is no shell yet.  Show a warning banner when the structure
  // has unsaved changes so the author knows the preview may be stale.
  const previewHtml = shellTemplate
    ? shellToPreview(shellTemplate)
    : buildFallbackPreviewHtml(nodes);

  const isFallback = !shellTemplate;
  const isStale = !isFallback && nodesDirty;

  useEffect(() => {
    const iframe = iframeRef.current;
    if (!iframe) return;
    const doc = iframe.contentDocument ?? iframe.contentWindow?.document;
    if (!doc) return;
    doc.open();
    doc.write(previewHtml);
    doc.close();
  }, [previewHtml]);

  if (nodes.length === 0 && !shellTemplate) {
    return (
      <div className="flex h-full flex-col items-center justify-center gap-3 text-slate-400">
        <svg className="h-12 w-12 text-slate-200" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M4 6h16M4 12h16M4 18h16" />
        </svg>
        <p className="text-sm font-semibold">No structure to preview yet</p>
        <p className="text-xs">Add elements in the Structure tab and save to generate the shell</p>
      </div>
    );
  }

  return (
    <div className="flex min-h-0 flex-1 flex-col overflow-hidden">
      {/* Info bar */}
      <div className="flex flex-shrink-0 items-center gap-2 border-b border-slate-200 bg-white px-4 py-2">
        <span className="flex h-5 w-5 items-center justify-center rounded bg-indigo-100">
          <svg className="h-3 w-3 text-indigo-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M2.458 12C3.732 7.943 7.523 5 12 5c4.477 0 8.268 2.943 9.542 7-1.274 4.057-5.065 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
          </svg>
        </span>
        <p className="text-xs font-semibold text-slate-700">Shell Preview</p>
        {isFallback ? (
          <p className="text-xs text-amber-600">⚠ Shell not generated yet — showing unsaved structure. Save to render the real shell with all assets.</p>
        ) : isStale ? (
          <p className="text-xs text-amber-600">⚠ Structure has unsaved changes — preview shows the last saved shell. Save to update.</p>
        ) : (
          <p className="text-xs text-slate-400">— Rendering the saved generated shell. All configured assets (CDN links, scripts) are active.</p>
        )}
      </div>
      <iframe
        ref={iframeRef}
        title="Layout Preview"
        sandbox="allow-same-origin allow-scripts"
        className="min-h-0 flex-1 w-full border-0 bg-white"
      />
    </div>
  );
}

// ─── Main Page ────────────────────────────────────────────────────────────────

export default function LayoutDesignerPage() {
  const { id } = useParams<{ id: string }>();
  const qc = useQueryClient();
  const { selectedSiteId } = useSite();

  const [activeTab, setActiveTab] = useState<'structure' | 'configuration' | 'shell' | 'preview'>('structure');

  const [nodes, setNodes] = useState<LayoutZoneNode[]>([]);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [nodesDirty, setNodesDirty] = useState(false);
  const [nodesInitialised, setNodesInitialised] = useState(false);

  const [config, setConfig] = useState<LayoutConfig>({ assets: [], bodyAttributes: [] });
  const [configDirty, setConfigDirty] = useState(false);
  const [configInitialised, setConfigInitialised] = useState(false);

  const [shellText, setShellText] = useState('');
  const [shellDirty, setShellDirty] = useState(false);
  const [shellEditMode, setShellEditMode] = useState(false);
  const [shellInitialised, setShellInitialised] = useState(false);

  const dragNodeId = useRef<string | null>(null);
  const [rootDropOver, setRootDropOver] = useState(false);

  const dirty = activeTab === 'structure' ? nodesDirty : activeTab === 'configuration' ? configDirty : activeTab === 'shell' ? shellDirty : false;

  const { data: layout, isLoading } = useQuery({
    queryKey: ['layout', id],
    queryFn: () => layoutsApi.get(id!),
    enabled: !!id,
    staleTime: 0,
  });

  if (layout && !nodesInitialised) {
    setNodes(layout.zones.length > 0 ? [...layout.zones] : []);
    setNodesInitialised(true);
  }
  if (layout && !configInitialised) {
    setConfig(layout.config ?? { assets: [], bodyAttributes: [] });
    setConfigInitialised(true);
  }
  if (layout && !shellInitialised) {
    setShellText(layout.shellTemplate ?? '');
    setShellInitialised(true);
  }

  const mutateNodes = useCallback((updater: (prev: LayoutZoneNode[]) => LayoutZoneNode[]) => {
    setNodes(updater);
    setNodesDirty(true);
  }, []);

  const addRootElement = (tag: string) => {
    mutateNodes(prev => [...prev, {
      id: uid(), type: 'html-element', name: `${tag}-${uid()}`,
      label: tag.charAt(0).toUpperCase() + tag.slice(1),
      sortOrder: prev.length, tag, cssClass: '', htmlAttributes: {}, children: [],
    }]);
  };

  const addRootSnippet = (build: () => LayoutZoneNode) => {
    mutateNodes(prev => [...prev, { ...build(), sortOrder: prev.length }]);
  };

  const addRootZone = () => {
    mutateNodes(prev => [...prev, {
      id: uid(), type: 'drop-zone', name: `zone-${uid()}`, label: 'New Zone', sortOrder: prev.length,
    }]);
  };

  const handleRemove = (nodeId: string) => {
    mutateNodes(prev => removeNode(prev, nodeId));
    setSelectedId(cur => cur === nodeId ? null : cur);
  };

  const handleMove = (nodeId: string, dir: -1 | 1) => {
    mutateNodes(prev => moveNode(prev, nodeId, dir));
  };

  const handleUpdate = (nodeId: string, patch: Partial<LayoutZoneNode>) => {
    mutateNodes(prev => updateNode(prev, nodeId, patch));
  };

  const handleAddChild = (parentId: string, type: 'html-element' | 'drop-zone') => {
    const child: LayoutZoneNode = type === 'html-element'
      ? { id: uid(), type: 'html-element', name: `div-${uid()}`, label: 'Div', sortOrder: 0, tag: 'div', cssClass: '', htmlAttributes: {}, children: [] }
      : { id: uid(), type: 'drop-zone', name: `zone-${uid()}`, label: 'New Zone', sortOrder: 0 };
    mutateNodes(prev => appendChildTo(prev, parentId, child));
    setSelectedId(child.id);
  };

  const handleDrop = useCallback((target: DropTarget) => {
    const nid = dragNodeId.current;
    if (!nid) return;
    mutateNodes(prev => {
      const node = findNode(prev, nid);
      if (!node) return prev;
      // Prevent dropping into itself
      if (target.kind === 'into' && target.parentId === nid) return prev;
      let next = removeNode(prev, nid);
      if (target.kind === 'root') {
        next = [...next, { ...node, sortOrder: next.length }];
      } else {
        next = appendChildTo(next, target.parentId, node);
      }
      return next;
    });
    dragNodeId.current = null;
  }, [mutateNodes]);

  const saveZonesMutation = useMutation({
    mutationFn: () => layoutsApi.updateZones(id!, { zones: nodes }),
    onSuccess: (data) => {
      toast.success('Layout structure saved.');
      setNodesDirty(false);
      setShellText(data.shellTemplate ?? '');
      setShellDirty(false);
      void qc.invalidateQueries({ queryKey: ['layout', id] });
      void qc.invalidateQueries({ queryKey: ['layouts', selectedSiteId] });
    },
    onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Save failed.'),
  });

  const saveConfigMutation = useMutation({
    mutationFn: () => layoutsApi.updateConfig(id!, { config }),
    onSuccess: (data) => {
      toast.success('Layout configuration saved.');
      setConfigDirty(false);
      setShellText(data.shellTemplate ?? '');
      setShellDirty(false);
      void qc.invalidateQueries({ queryKey: ['layout', id] });
      void qc.invalidateQueries({ queryKey: ['layouts', selectedSiteId] });
    },
    onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Save failed.'),
  });

  const saveShellMutation = useMutation({
    mutationFn: () => layoutsApi.updateShell(id!, { shellTemplate: shellText }),
    onSuccess: (data) => {
      toast.success('Shell template saved.');
      setShellDirty(false);
      setShellText(data.shellTemplate ?? '');
      void qc.invalidateQueries({ queryKey: ['layout', id] });
      void qc.invalidateQueries({ queryKey: ['layouts', selectedSiteId] });
    },
    onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Save failed.'),
  });

  const handleSave = () => {
    if (activeTab === 'structure') {
      if (layout?.isShellCustomized &&
        !window.confirm('This layout has a hand-edited shell. Saving will overwrite it. Continue?'))
        return;
      saveZonesMutation.mutate();
    } else if (activeTab === 'configuration') {
      if (layout?.isShellCustomized &&
        !window.confirm('This layout has a hand-edited shell. Saving will overwrite it. Continue?'))
        return;
      saveConfigMutation.mutate();
    } else {
      saveShellMutation.mutate();
    }
  };

  const isSaving = saveZonesMutation.isPending || saveConfigMutation.isPending || saveShellMutation.isPending;
  const selectedNode = selectedId ? findNode(nodes, selectedId) : null;
  const dropZoneCount = countDropZones(nodes);

  const saveLabel = isSaving ? 'Saving…'
    : activeTab === 'structure' ? 'Save Structure'
    : activeTab === 'configuration' ? 'Save Config'
    : 'Save Shell';
  const saveVisible = (activeTab !== 'shell' || shellEditMode) && activeTab !== 'preview';

  if (isLoading) {
    return <div className="flex h-full items-center justify-center text-sm text-slate-400">Loading layout…</div>;
  }

  return (
    <div className="-m-6 flex h-[calc(100vh-4rem)] flex-col overflow-hidden bg-white">
      {/* Topbar */}
      <header className="flex h-14 flex-shrink-0 items-center gap-3 border-b border-slate-200 bg-white px-4">
        <Link to="/layouts" className="rounded p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-600">
          <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
          </svg>
        </Link>
        <div className="flex h-7 w-7 flex-shrink-0 items-center justify-center rounded-md bg-purple-50">
          <svg className="h-3.5 w-3.5 text-purple-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 5a1 1 0 011-1h4a1 1 0 011 1v4a1 1 0 01-1 1H5a1 1 0 01-1-1V5zM14 5a1 1 0 011-1h4a1 1 0 011 1v4a1 1 0 01-1 1h-4a1 1 0 01-1-1V5zM4 15a1 1 0 011-1h4a1 1 0 011 1v4a1 1 0 01-1 1H5a1 1 0 01-1-1v-4zM14 15a1 1 0 011-1h4a1 1 0 011 1v4a1 1 0 01-1 1h-4a1 1 0 01-1-1v-4z" />
          </svg>
        </div>
        <div className="min-w-0 flex-1">
          <p className="text-sm font-bold text-slate-900">{layout?.name ?? 'Layout Designer'}</p>
          <p className="font-mono text-xs text-slate-400">{dropZoneCount} drop zone{dropZoneCount !== 1 ? 's' : ''}</p>
        </div>
        <span className={`flex items-center gap-1.5 text-xs ${dirty ? 'text-amber-600' : 'text-slate-400'}`}>
          <span className={`h-1.5 w-1.5 rounded-full ${dirty ? 'bg-amber-400' : 'bg-green-400'}`} />
          {dirty ? 'Unsaved changes' : 'Saved'}
        </span>
        {saveVisible && (
          <button onClick={handleSave} disabled={isSaving || !dirty}
            className="btn-primary h-8 px-3 py-0 text-xs disabled:opacity-50">
            {saveLabel}
          </button>
        )}
      </header>

      {/* Tabs */}
      <div className="flex flex-shrink-0 border-b border-slate-200 bg-white px-4">
        {(['structure', 'configuration', 'shell', 'preview'] as const).map((tab) => {
          const label = tab === 'structure' ? 'Layout Structure'
            : tab === 'configuration' ? 'Layout Configuration'
            : tab === 'preview' ? '👁 Preview'
            : 'Generated Shell';
          const hasDot = (tab === 'structure' && nodesDirty)
            || (tab === 'configuration' && configDirty)
            || (tab === 'shell' && shellDirty);
          return (
            <button key={tab} onClick={() => setActiveTab(tab)}
              className={`mr-1 border-b-2 px-4 py-2.5 text-xs font-semibold transition-colors ${
                activeTab === tab ? 'border-brand-500 text-brand-700' : 'border-transparent text-slate-500 hover:text-slate-700'
              }`}>
              {label}
              {hasDot && <span className="ml-1.5 inline-block h-1.5 w-1.5 rounded-full bg-amber-400" />}
            </button>
          );
        })}
      </div>

      {/* Tab body */}
      {activeTab === 'structure' ? (
        <div className="flex min-h-0 flex-1 overflow-hidden">
          {/* LEFT — toolbox */}
          <aside className="flex w-56 flex-shrink-0 flex-col overflow-y-auto border-r border-slate-200 bg-white">
            {/* Drop zone shortcut */}
            <div className="border-b border-slate-200 px-3 py-3">
              <button onClick={addRootZone}
                className="flex w-full items-center gap-2 rounded-lg border border-brand-200 bg-brand-50 px-3 py-2 text-xs font-semibold text-brand-700 hover:bg-brand-100">
                <span>⬡</span> Drop Zone
              </button>
            </div>

            {/* Grouped presets */}
            {PRESET_GROUPS.map((group) => (
              <div key={group.group} className="border-b border-slate-100 px-3 py-2.5">
                <p className="mb-1.5 text-[10px] font-bold uppercase tracking-wider text-slate-400">{group.group}</p>
                <div className="space-y-1">
                  {group.items.map((preset) => {
                    if (preset.kind === 'tag') {
                      const tc = TAG_COLORS[preset.color] ?? TAG_COLORS.blue;
                      return (
                        <button key={preset.tag} onClick={() => addRootElement(preset.tag)}
                          className={`flex w-full items-center gap-1.5 rounded border px-2 py-1 text-[11px] font-semibold transition-colors ${tc.border} ${tc.bg} ${tc.text} hover:opacity-80`}>
                          <code>&lt;{preset.tag}&gt;</code>
                        </button>
                      );
                    }
                    const tc = TAG_COLORS[preset.color] ?? TAG_COLORS.blue;
                    return (
                      <button key={preset.label} onClick={() => addRootSnippet(preset.build)}
                        className={`flex w-full items-center gap-1.5 rounded border px-2 py-1 text-[11px] font-semibold transition-colors ${tc.border} ${tc.bg} ${tc.text} hover:opacity-80`}>
                        <svg className="h-3 w-3 flex-shrink-0" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
                        </svg>
                        {preset.label}
                      </button>
                    );
                  })}
                </div>
              </div>
            ))}

            <div className="px-3 py-2 text-[10px] text-slate-400">
              <p className="font-semibold text-slate-500">Tips</p>
              <ul className="mt-1 space-y-1">
                <li>• Drag to reorder or nest nodes</li>
                <li>• Drop zones = component placeholders</li>
                <li>• Elements nest infinitely</li>
              </ul>
            </div>
          </aside>

          {/* CENTER — canvas */}
          <div
            className="flex min-w-0 flex-1 flex-col overflow-auto bg-slate-50 p-6"
            onDragOver={(e) => { e.preventDefault(); setRootDropOver(true); }}
            onDragLeave={() => setRootDropOver(false)}
            onDrop={(e) => { e.preventDefault(); setRootDropOver(false); handleDrop({ kind: 'root' }); }}
          >
            <div className="mx-auto w-full max-w-3xl">
              <div className="mb-4 flex items-center gap-2 rounded-lg border border-slate-200 bg-white px-4 py-2 text-xs text-slate-500">
                <svg className="h-4 w-4 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M9 17V7m0 10a2 2 0 01-2 2H5a2 2 0 01-2-2V7a2 2 0 012-2h2a2 2 0 012 2m0 10a2 2 0 002 2h2a2 2 0 002-2M9 7a2 2 0 012-2h2a2 2 0 012 2m0 10V7" />
                </svg>
                Build your HTML skeleton with elements, then place Drop Zones where components go
              </div>

              {nodes.length === 0 ? (
                <div className={`flex flex-col items-center justify-center rounded-xl border-2 border-dashed py-16 text-center transition-colors ${rootDropOver ? 'border-brand-400 bg-brand-50' : 'border-slate-300 bg-white'}`}>
                  <svg className="mb-3 h-10 w-10 text-slate-200" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M4 6h16M4 12h16M4 18h16" />
                  </svg>
                  <p className="text-sm font-semibold text-slate-400">No elements yet</p>
                  <p className="mt-1 text-xs text-slate-300">Add HTML elements or drop zones from the left panel</p>
                </div>
              ) : (
                <div className={`space-y-2 rounded-xl border-2 border-dashed p-3 transition-colors ${rootDropOver ? 'border-brand-400 bg-brand-50/40' : 'border-transparent'}`}>
                  {[...nodes].sort((a, b) => a.sortOrder - b.sortOrder).map((node) => (
                    <NodeCard
                      key={node.id}
                      node={node}
                      depth={0}
                      selectedId={selectedId}
                      dragNodeId={dragNodeId}
                      onSelect={(nid) => setSelectedId(nid === selectedId ? null : nid)}
                      onRemove={handleRemove}
                      onMove={handleMove}
                      onAddChild={handleAddChild}
                      onDrop={handleDrop}
                      onDragStart={(nid) => { dragNodeId.current = nid; }}
                    />
                  ))}
                </div>
              )}
            </div>
          </div>

          {/* RIGHT — properties */}
          <PropertiesPanel node={selectedNode} onUpdate={handleUpdate} />
        </div>
      ) : activeTab === 'configuration' ? (
        <LayoutConfigPanel config={config} onChange={(c) => { setConfig(c); setConfigDirty(true); }} />
      ) : activeTab === 'preview' ? (
        <LayoutPreview
          nodes={nodes}
          shellTemplate={shellEditMode ? shellText : layout?.shellTemplate}
          nodesDirty={nodesDirty}
        />
      ) : (
        <div className="flex min-h-0 flex-1 overflow-auto bg-slate-50 p-6">
          <div className="mx-auto w-full max-w-4xl space-y-4">
            <div className="flex items-start justify-between gap-4">
              <div>
                <p className="text-sm font-bold text-slate-800">Generated Shell</p>
                <p className="text-xs text-slate-400">
                  {shellEditMode
                    ? 'Advanced mode — editing directly. Saving structure or configuration will overwrite this.'
                    : 'Regenerated automatically whenever structure or configuration are saved.'}
                </p>
              </div>
              <button
                onClick={() => {
                  if (shellEditMode && shellDirty) {
                    if (!window.confirm('Discard unsaved shell edits?')) return;
                    setShellText(layout?.shellTemplate ?? '');
                    setShellDirty(false);
                  }
                  setShellEditMode(e => !e);
                }}
                className={`flex-shrink-0 rounded-lg border px-3 py-1.5 text-xs font-semibold transition-colors ${
                  shellEditMode
                    ? 'border-amber-300 bg-amber-50 text-amber-700 hover:bg-amber-100'
                    : 'border-slate-200 bg-white text-slate-600 hover:border-brand-300 hover:text-brand-700'
                }`}>
                {shellEditMode ? '✕ Exit Advanced Mode' : '⚙ Advanced Mode'}
              </button>
            </div>

            {layout?.isShellCustomized && !shellEditMode && (
              <div className="flex items-start gap-3 rounded-lg border border-amber-200 bg-amber-50 px-4 py-3">
                <svg className="mt-0.5 h-4 w-4 flex-shrink-0 text-amber-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z" />
                </svg>
                <p className="text-xs text-amber-700">
                  <strong>Advanced mode shell active.</strong> Saving structure or configuration will overwrite your custom shell.
                </p>
              </div>
            )}

            {shellEditMode ? (
              <textarea value={shellText}
                onChange={(e) => { setShellText(e.target.value); setShellDirty(true); }}
                className="w-full rounded-xl border border-brand-300 bg-white p-4 font-mono text-xs leading-relaxed text-slate-800 shadow-sm outline-none focus:ring-2 focus:ring-brand-400"
                rows={30} spellCheck={false} />
            ) : layout?.shellTemplate ? (
              <pre className="overflow-auto rounded-xl border border-slate-200 bg-white p-4 font-mono text-xs leading-relaxed text-slate-700 shadow-sm">
                {layout.shellTemplate}
              </pre>
            ) : (
              <div className="flex flex-col items-center justify-center rounded-xl border-2 border-dashed border-slate-300 bg-white py-16 text-center">
                <p className="text-sm font-semibold text-slate-400">No shell generated yet</p>
                <p className="mt-1 text-xs text-slate-300">Save structure or configuration to generate the shell.</p>
              </div>
            )}
          </div>
        </div>
      )}

      {/* Status bar */}
      <div className="flex h-6 flex-shrink-0 items-center gap-4 border-t border-slate-200 bg-slate-50 px-4 text-[11px] text-slate-400">
        <span>{nodes.length} root node{nodes.length !== 1 ? 's' : ''}</span>
        <span>·</span>
        <span>{dropZoneCount} drop zone{dropZoneCount !== 1 ? 's' : ''}</span>
        <span>·</span>
        <span>{config.assets.length} asset{config.assets.length !== 1 ? 's' : ''}</span>
        <span>·</span>
        <span className="font-mono">{layout?.key}</span>
      </div>
    </div>
  );
}

