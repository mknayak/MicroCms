import { useState, useRef, useCallback, useEffect } from 'react';
import { Link, useSearchParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { componentsApi } from '@/api/components';
import { pagesApi } from '@/api/pages';
import { layoutsApi } from '@/api/layouts';
import { useSite } from '@/contexts/SiteContext';
import type { ComponentListItem, PageTreeNode, ComponentCategory, PageDto } from '@/types';
import type { ViewportSize, DesignerPlacement } from './designerTypes';
import { ApiError } from '@/api/client';

// ─── Helpers ──────────────────────────────────────────────────────────────────

function uid() {
  return Math.random().toString(36).slice(2);
}

const CATEGORY_COLORS: Record<ComponentCategory, { bg: string; text: string }> = {
  Layout:      { bg: 'bg-blue-100',    text: 'text-blue-700'    },
  Content:  { bg: 'bg-emerald-100', text: 'text-emerald-700' },
  Media:       { bg: 'bg-amber-100',   text: 'text-amber-700'   },
Navigation:  { bg: 'bg-indigo-900',  text: 'text-indigo-200'  },
  Interactive: { bg: 'bg-pink-100',    text: 'text-pink-700'    },
  Commerce:    { bg: 'bg-orange-100',  text: 'text-orange-700'  },
};

const CATEGORY_ICON_PATH: Record<ComponentCategory, string> = {
  Layout:   'M4 6h16M4 10h10M4 14h16M4 18h10',
  Content:     'M4 6h16M4 10h16M4 14h12M4 18h8',
  Media:    'M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z',
  Navigation:  'M4 6h16M4 12h16M4 18h8',
  Interactive: 'M8.228 9c.549-1.165 2.03-2 3.772-2 2.21 0 4 1.343 4 3 0 1.4-1.278 2.575-3.006 2.907-.542.104-.994.54-.994 1.093m0 3h.01',
  Commerce:    'M3 3h2l.4 2M7 13h10l4-8H5.4M7 13L5.4 5M7 13l-2.293 2.293c-.63.63-.184 1.707.707 1.707H17m0 0a2 2 0 100 4 2 2 0 000-4zm-8 2a2 2 0 11-4 0 2 2 0 014 0z',
};

function flattenPages(nodes: PageTreeNode[], acc: PageTreeNode[] = []): PageTreeNode[] {
  for (const n of nodes) { acc.push(n); flattenPages(n.children, acc); }
  return acc;
}

// ─── Topbar ───────────────────────────────────────────────────────────────────

function DesignerTopbar({
  page,
  viewport, setViewport,
  zoom, changeZoom,
  previewMode, togglePreview,
  dirty, saving, onSave,
}: {
  page: PageDto | undefined;
  viewport: ViewportSize; setViewport: (v: ViewportSize) => void;
  zoom: number; changeZoom: (d: number) => void;
  previewMode: boolean; togglePreview: () => void;
  dirty: boolean; saving: boolean; onSave: () => void;
}) {
  return (
    <header className="flex h-14 flex-shrink-0 items-stretch overflow-hidden border-b border-slate-200 bg-white">
      {/* LEFT — back + page context */}
      <div className="flex flex-shrink-0 items-center gap-3 border-r border-slate-200 px-4">
    <Link to="/pages" className="flex items-center rounded p-1 text-slate-400 hover:bg-slate-100 hover:text-slate-600" title="Back to Pages">
    <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 19l-7-7 7-7" />
          </svg>
</Link>
        <div className="flex h-7 w-7 flex-shrink-0 items-center justify-center rounded-md bg-brand-50">
  <svg className="h-3.5 w-3.5 text-brand-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
  <rect x="3" y="3" width="18" height="18" rx="2" strokeWidth="2" />
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 9h18M9 21V9" />
          </svg>
        </div>
        <div className="min-w-0">
          <p className="max-w-[200px] truncate text-sm font-bold text-slate-900">
            {page?.title ?? 'Page Designer'}
      </p>
          <p className="font-mono text-xs text-slate-400">
            {page ? `/${page.slug}` : 'Select a page'}
</p>
    </div>
  </div>

      {/* CENTER — status */}
 <div className="flex min-w-0 flex-1 items-center gap-3 px-4">
        <span className={`flex items-center gap-1.5 text-xs ${dirty ? 'text-amber-600' : 'text-slate-400'}`}>
 <span className={`h-1.5 w-1.5 rounded-full ${dirty ? 'bg-amber-400' : 'bg-green-400'}`} />
          {dirty ? 'Unsaved changes' : 'Saved'}
        </span>
 </div>

      {/* RIGHT — viewport + zoom + preview + save */}
      <div className="flex flex-shrink-0 items-center gap-2 px-3">
        {/* Viewport */}
    <div className="flex overflow-hidden rounded-md border border-slate-200">
 {(['desktop', 'tablet', 'mobile'] as ViewportSize[]).map((vp) => (
       <button key={vp} onClick={() => setViewport(vp)} title={vp.charAt(0).toUpperCase() + vp.slice(1)}
        className={`px-2 py-1.5 transition-colors ${viewport === vp ? 'bg-brand-600 text-white' : 'bg-white text-slate-400 hover:bg-slate-50'} ${vp !== 'desktop' ? 'border-l border-slate-200' : ''}`}>
     {vp === 'desktop' && <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><rect x="2" y="4" width="20" height="14" rx="2" strokeWidth="2" /><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 20h8M12 18v2" /></svg>}
       {vp === 'tablet'  && <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><rect x="4" y="2" width="16" height="20" rx="2" strokeWidth="2" /><circle cx="12" cy="18" r="1" fill="currentColor" /></svg>}
        {vp === 'mobile'  && <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><rect x="7" y="2" width="10" height="20" rx="2" strokeWidth="2" /><circle cx="12" cy="18" r="1" fill="currentColor" /></svg>}
            </button>
))}
        </div>
   {/* Zoom */}
        <div className="flex items-center gap-1 text-xs text-slate-500">
       <button onClick={() => changeZoom(-10)} className="flex h-5 w-5 items-center justify-center rounded border border-slate-200 hover:bg-slate-50">−</button>
          <span className="w-10 text-center font-mono">{zoom}%</span>
          <button onClick={() => changeZoom(10)}  className="flex h-5 w-5 items-center justify-center rounded border border-slate-200 hover:bg-slate-50">+</button>
</div>
        <div className="h-5 w-px bg-slate-200" />
        {/* Preview */}
        <button onClick={togglePreview}
       className={`flex items-center gap-1.5 rounded-md border px-2.5 py-1.5 text-xs font-semibold transition-colors ${previewMode ? 'border-brand-500 bg-brand-50 text-brand-700' : 'border-slate-200 bg-white text-slate-500 hover:border-brand-400 hover:text-brand-600'}`}>
       <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
          </svg>
          {previewMode ? 'Exit Preview' : 'Preview'}
    </button>
        {/* Save */}
        <button onClick={onSave} disabled={saving || !dirty}
 className="btn-primary h-8 px-3 py-0 text-xs disabled:opacity-50">
          {saving ? 'Saving…' : 'Save'}
   </button>
    </div>
    </header>
  );
}

// ─── Palette Panel ────────────────────────────────────────────────────────────

function PalettePanel({ components, search, setSearch, onDragStart }: {
  components: ComponentListItem[];
  search: string;
  setSearch: (s: string) => void;
  onDragStart: (e: React.DragEvent, comp: ComponentListItem) => void;
}) {
  const grouped = components
    .filter((c) => !search || c.name.toLowerCase().includes(search.toLowerCase()))
    .reduce<Record<string, ComponentListItem[]>>((acc, c) => {
      (acc[c.category] ??= []).push(c);
      return acc;
  }, {});

  return (
    <aside className="flex w-56 flex-shrink-0 flex-col overflow-hidden border-r border-slate-200 bg-white">
      <div className="flex-shrink-0 border-b border-slate-200 px-3 py-3">
        <p className="mb-2 text-[10px] font-bold uppercase tracking-wider text-slate-400">Components</p>
        <div className="relative">
          <svg className="absolute left-2 top-1/2 h-3 w-3 -translate-y-1/2 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
     <circle cx="11" cy="11" r="8" strokeWidth="2" />
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-4.35-4.35" />
          </svg>
        <input className="w-full rounded-md border border-slate-200 py-1.5 pl-7 pr-2 text-xs focus:border-brand-400 focus:outline-none"
   placeholder="Search…" value={search} onChange={(e) => setSearch(e.target.value)} />
    </div>
      </div>
      <div className="flex-1 overflow-y-auto px-2 py-2">
   {components.length === 0 && (
       <p className="py-6 text-center text-xs text-slate-400">No components yet.<br />Add some in Component Library.</p>
        )}
     {Object.entries(grouped).map(([cat, items]) => (
        <div key={cat} className="mb-3">
       <p className="mb-1 px-1 text-[9px] font-bold uppercase tracking-wider text-slate-400">{cat}</p>
   {items.map((comp) => {
          const colors = CATEGORY_COLORS[comp.category as ComponentCategory] ?? { bg: 'bg-slate-100', text: 'text-slate-600' };
            const iconPath = CATEGORY_ICON_PATH[comp.category as ComponentCategory] ?? '';
      return (
   <div key={comp.id} draggable onDragStart={(e) => onDragStart(e, comp)}
    className="mb-1 flex cursor-grab items-center gap-2 rounded-md border border-transparent px-2 py-1.5 transition-all hover:border-brand-200 hover:bg-brand-50 active:cursor-grabbing active:opacity-70">
         <div className={`flex h-7 w-7 flex-shrink-0 items-center justify-center rounded ${colors.bg}`}>
     <svg className={`h-3.5 w-3.5 ${colors.text}`} fill="none" viewBox="0 0 24 24" stroke="currentColor">
             <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d={iconPath} />
                  </svg>
    </div>
   <div className="min-w-0">
         <p className="truncate text-xs font-semibold text-slate-800">{comp.name}</p>
         <p className="text-[10px] text-slate-400">{comp.fieldCount} fields · {comp.category}</p>
         </div>
 <svg className="ml-auto h-3 w-3 flex-shrink-0 text-slate-300" fill="currentColor" viewBox="0 0 24 24">
      <circle cx="8" cy="5" r="1.5" /><circle cx="16" cy="5" r="1.5" />
         <circle cx="8" cy="12" r="1.5" /><circle cx="16" cy="12" r="1.5" />
     <circle cx="8" cy="19" r="1.5" /><circle cx="16" cy="19" r="1.5" />
             </svg>
       </div>
          );
     })}
  </div>
        ))}
  </div>
    </aside>
  );
}

// ─── Canvas Zone ──────────────────────────────────────────────────────────────

function CanvasZone({ zoneName, placements, selectedLocalId, onSelect, onMoveUp, onMoveDown, onRemove, onDrop, previewMode }: {
  zoneName: string;
  placements: DesignerPlacement[];
  selectedLocalId: string | null;
  onSelect: (localId: string) => void;
  onMoveUp: (localId: string) => void;
  onMoveDown: (localId: string) => void;
  onRemove: (localId: string) => void;
  onDrop: (zone: string, data: { id: string; name: string; key: string; category: string }) => void;
  previewMode: boolean;
}) {
  const [dragOver, setDragOver] = useState(false);

  return (
    <div onDragOver={(e) => { e.preventDefault(); setDragOver(true); }}
      onDragLeave={() => setDragOver(false)}
onDrop={(e) => {
        e.preventDefault(); setDragOver(false);
        const raw = e.dataTransfer.getData('application/microcms-comp');
        if (raw) try { onDrop(zoneName, JSON.parse(raw)); } catch { /* ignore */ }
      }}
    className={`relative min-h-[60px] border-2 border-dashed transition-colors ${
        previewMode ? 'border-transparent'
        : dragOver ? 'border-brand-400 bg-brand-50/40'
 : placements.length === 0 ? 'border-slate-200 bg-slate-50/60'
        : 'border-transparent hover:border-slate-200'}`}>

   {/* Zone label */}
      {!previewMode && (
     <span className="absolute left-2 top-0.5 z-10 select-none text-[9px] font-bold uppercase tracking-widest text-brand-300">{zoneName}</span>
      )}

      {/* Empty hint */}
 {placements.length === 0 && !previewMode && (
        <div className="flex h-14 items-center justify-center">
      <div className="flex items-center gap-1.5 rounded-md border border-dashed border-slate-300 px-3 py-2 text-xs text-slate-400">
     <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
            </svg>
  Drop a component here
          </div>
        </div>
      )}

      {/* Placed components */}
      {placements.map((p) => {
      const isSelected = p.localId === selectedLocalId;
        return (
       <div key={p.localId} onClick={() => !previewMode && onSelect(p.localId)}
       className={`group relative cursor-pointer border-2 transition-all ${
         previewMode ? 'border-transparent'
  : isSelected ? 'border-brand-500 shadow-[0_0_0_2px_rgba(99,102,241,0.15)]'
      : 'border-transparent hover:border-brand-300/60'}`}>
            {/* Toolbar */}
  {!previewMode && (
      <div className={`absolute right-0 top-0 z-20 flex items-center rounded-bl-md bg-brand-600 transition-opacity ${isSelected ? 'opacity-100' : 'opacity-0 group-hover:opacity-100'}`}>
    <span className="border-r border-white/20 px-2 py-1 text-[10px] font-bold text-white/90">{p.componentName}</span>
           <button onClick={(e) => { e.stopPropagation(); onMoveUp(p.localId); }} title="Move up" className="px-1.5 py-1 text-white hover:bg-white/20">
         <svg className="h-3 w-3" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 15l7-7 7 7" /></svg>
      </button>
   <button onClick={(e) => { e.stopPropagation(); onMoveDown(p.localId); }} title="Move down" className="px-1.5 py-1 text-white hover:bg-white/20">
       <svg className="h-3 w-3" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" /></svg>
    </button>
 <button onClick={(e) => { e.stopPropagation(); onRemove(p.localId); }} title="Remove" className="px-1.5 py-1 text-red-300 hover:bg-white/20">
   <svg className="h-3 w-3" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" /></svg>
             </button>
     </div>
 )}
 <div className="pointer-events-none"><ComponentPreview component={p} /></div>
          </div>
        );
      })}
    </div>
  );
}

// ─── Component Preview ────────────────────────────────────────────────────────

function ComponentPreview({ component }: { component: DesignerPlacement }) {
  if (component.componentCategory === 'Navigation') {
    return (
      <div className="flex items-center justify-between bg-indigo-950 px-8 py-3.5">
        <div className="flex items-center gap-2">
          <div className="flex h-6 w-6 items-center justify-center rounded bg-brand-600 text-[10px] font-black text-white">A</div>
          <span className="text-sm font-bold text-white">{component.componentName}</span>
     </div>
        <div className="flex gap-5">{['Home','Products','About','Blog'].map((l) => <span key={l} className="text-xs text-indigo-300">{l}</span>)}</div>
        <span className="rounded-md bg-brand-600 px-3 py-1.5 text-xs font-semibold text-white">Get Started</span>
   </div>
    );
  }
  if (component.componentCategory === 'Layout') {
 return (
      <div className="relative bg-gradient-to-br from-indigo-950 via-indigo-900 to-brand-600 px-12 py-14">
        <div className="max-w-lg">
        <div className="mb-3 h-7 w-3/4 rounded-md bg-white/20" />
        <div className="mb-2 h-2.5 w-full rounded bg-white/10" />
          <div className="mb-5 h-2.5 w-5/6 rounded bg-white/10" />
          <div className="flex gap-3"><div className="h-9 w-28 rounded-md bg-white" /><div className="h-9 w-24 rounded-md border border-white/30 bg-white/10" /></div>
        </div>
        <span className="absolute right-3 top-1 text-[9px] font-semibold text-white/40">{component.componentName}</span>
      </div>
    );
  }
if (component.componentCategory === 'Content') {
    return (
      <div className="relative bg-white px-12 py-10">
 <div className="mx-auto mb-2 h-5 w-56 rounded bg-slate-200" />
        <div className="mx-auto mb-7 h-3 w-48 rounded bg-slate-100" />
     <div className="grid grid-cols-3 gap-4">
   {[0,1,2].map((i) => (
       <div key={i} className="rounded-lg border border-slate-200 p-4">
           <div className="mb-3 h-8 w-8 rounded-lg bg-brand-100" />
     <div className="mb-2 h-3 w-3/4 rounded bg-slate-200" />
              <div className="h-2.5 w-full rounded bg-slate-100" />
              <div className="mt-1 h-2.5 w-5/6 rounded bg-slate-100" />
          </div>
   ))}
        </div>
        <span className="absolute right-3 top-1 text-[9px] font-semibold text-slate-300">{component.componentName}</span>
      </div>
    );
  }
  if (component.componentCategory === 'Media') {
    return (
   <div className="relative bg-slate-100 px-12 py-8">
 <div className="mx-auto mb-4 h-4 w-48 rounded bg-slate-300" />
        <div className="grid grid-cols-3 gap-3">
          {[0,1,2].map((i) => (
            <div key={i} className="flex aspect-video items-center justify-center rounded-md bg-slate-200">
       <svg className="h-6 w-6 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01" />
              </svg>
            </div>
          ))}
        </div>
        <span className="absolute right-3 top-1 text-[9px] font-semibold text-slate-400">{component.componentName}</span>
      </div>
    );
  }
  // Generic fallback
  const colors = CATEGORY_COLORS[component.componentCategory] ?? { bg: 'bg-slate-100', text: 'text-slate-600' };
  const iconPath = CATEGORY_ICON_PATH[component.componentCategory] ?? '';
  return (
    <div className={`flex items-center gap-3 px-6 py-8 ${colors.bg}`}>
      <svg className={`h-8 w-8 flex-shrink-0 ${colors.text}`} fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d={iconPath} />
      </svg>
      <div>
    <p className={`text-sm font-bold ${colors.text}`}>{component.componentName}</p>
        <p className="mt-0.5 text-xs text-slate-500">{component.componentCategory} · {component.zone}</p>
    </div>
    </div>
  );
}

// ─── Properties Panel ─────────────────────────────────────────────────────────

function PropertiesPanel({ selected, placements, allComponents, onRemove }: {
  selected: DesignerPlacement | null;
  placements: DesignerPlacement[];
  allComponents: ComponentListItem[];
  onRemove: (localId: string) => void;
}) {
  if (!selected) {
    return (
   <aside className="flex w-64 flex-shrink-0 flex-col overflow-hidden border-l border-slate-200 bg-white">
        <div className="flex flex-1 flex-col items-center justify-center gap-3 p-6 text-center">
   <svg className="h-9 w-9 text-slate-200" fill="none" viewBox="0 0 24 24" stroke="currentColor">
     <rect x="3" y="3" width="18" height="18" rx="2" strokeWidth="1.5" />
  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M9 3v18M3 9h6M3 15h6" />
    </svg>
          <div>
   <p className="text-sm font-semibold text-slate-600">No component selected</p>
            <p className="mt-0.5 text-xs text-slate-400">Click a component on the canvas or drag one from the palette.</p>
          </div>
 </div>
        {placements.length > 0 && (
   <div className="border-t border-slate-100 px-4 py-3">
            <p className="mb-2 text-[10px] font-bold uppercase tracking-wider text-slate-400">Canvas Summary</p>
      {Array.from(new Set(placements.map((p) => p.zone))).map((zone) => (
     <div key={zone} className="mb-1 flex items-center justify-between text-xs">
  <span className="font-mono text-slate-600">{zone}</span>
   <span className="rounded-full bg-slate-100 px-1.5 py-0.5 text-[10px] text-slate-500">
            {placements.filter((p) => p.zone === zone).length}
            </span>
              </div>
     ))}
          </div>
        )}
      </aside>
    );
  }

  const comp = allComponents.find((c) => c.id === selected.componentId);
  const colors = CATEGORY_COLORS[selected.componentCategory] ?? { bg: 'bg-slate-100', text: 'text-slate-600' };
  const iconPath = CATEGORY_ICON_PATH[selected.componentCategory] ?? '';

  return (
    <aside className="flex w-64 flex-shrink-0 flex-col overflow-hidden border-l border-slate-200 bg-white">
      <div className="flex items-center gap-2.5 border-b border-slate-200 px-4 py-3">
   <div className={`flex h-7 w-7 flex-shrink-0 items-center justify-center rounded-md ${colors.bg}`}>
        <svg className={`h-3.5 w-3.5 ${colors.text}`} fill="none" viewBox="0 0 24 24" stroke="currentColor">
 <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d={iconPath} />
 </svg>
    </div>
        <div className="min-w-0">
  <p className="truncate text-sm font-bold text-slate-900">{selected.componentName}</p>
     <p className="truncate text-[11px] text-slate-400">{selected.componentCategory} · {selected.zone}</p>
        </div>
      </div>

      <div className="flex-1 space-y-4 overflow-y-auto px-4 py-3">
        <div>
          <p className="mb-1 text-[10px] font-bold uppercase tracking-wider text-slate-400">Zone</p>
  <div className="rounded-md border border-slate-200 bg-slate-50 px-3 py-2 font-mono text-xs text-slate-700">{selected.zone}</div>
   </div>
      <div>
 <p className="mb-1 text-[10px] font-bold uppercase tracking-wider text-slate-400">Sort Order</p>
       <div className="rounded-md border border-slate-200 bg-slate-50 px-3 py-2 text-xs text-slate-700">Position {selected.sortOrder + 1} in zone</div>
        </div>
        {comp && (
          <div>
         <p className="mb-1.5 text-[10px] font-bold uppercase tracking-wider text-slate-400">Component</p>
          <div className="space-y-1.5 rounded-lg border border-slate-200 bg-slate-50 p-3 text-xs text-slate-600">
           <div className="flex justify-between"><span className="text-slate-400">Key</span><span className="font-mono text-slate-700">{comp.key}</span></div>
         <div className="flex justify-between"><span className="text-slate-400">Fields</span><span>{comp.fieldCount}</span></div>
        <div className="flex justify-between"><span className="text-slate-400">Template</span><span>{comp.templateType}</span></div>
  <div className="flex justify-between"><span className="text-slate-400">Used on</span><span>{comp.usageCount} page{comp.usageCount !== 1 ? 's' : ''}</span></div>
 </div>
        </div>
        )}
    {comp && comp.zones.length > 0 && (
        <div>
            <p className="mb-1.5 text-[10px] font-bold uppercase tracking-wider text-slate-400">Supported Zones</p>
         <div className="flex flex-wrap gap-1">
{comp.zones.map((z) => (
     <span key={z} className={`rounded px-1.5 py-0.5 font-mono text-[10px] font-medium ${z === selected.zone ? 'bg-brand-100 text-brand-700' : 'bg-slate-100 text-slate-600'}`}>{z}</span>
              ))}
      </div>
 </div>
        )}
        {comp && (
          <Link to={`/components/${comp.id}/edit`}
            className="flex items-center gap-1.5 rounded-md border border-slate-200 px-3 py-2 text-xs font-medium text-slate-600 hover:bg-slate-50">
         <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
           <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14" />
            </svg>
            View in Component Library
          </Link>
        )}
      </div>

      <div className="flex-shrink-0 border-t border-slate-200 p-3">
        <button onClick={() => onRemove(selected.localId)}
          className="flex w-full items-center justify-center gap-1.5 rounded-md border border-red-200 bg-red-50 px-3 py-1.5 text-xs font-semibold text-red-600 hover:bg-red-100">
          <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
          </svg>
     Remove from Zone
        </button>
      </div>
    </aside>
  );
}

// ─── Status Bar ───────────────────────────────────────────────────────────────

function StatusBar({ placements, page, layoutName }: { placements: DesignerPlacement[]; page: PageDto | undefined; layoutName?: string }) {
  const zones = new Set(placements.map((p) => p.zone)).size;
  return (
    <div className="flex h-6 flex-shrink-0 items-center gap-4 border-t border-slate-200 bg-slate-50 px-4 text-[11px] text-slate-400">
   <span className="flex items-center gap-1"><span className="h-1.5 w-1.5 rounded-full bg-green-400" />Page Template</span>
      <span>{placements.length} component{placements.length !== 1 ? 's' : ''}</span>
      <span>·</span>
      <span>{zones} zone{zones !== 1 ? 's' : ''}</span>
      {layoutName && <><span>·</span><span>Layout: {layoutName}</span></>}
      <span className="ml-auto font-mono">{page ? `/${page.slug}` : ''}</span>
    </div>
  );
}

// ─── Main Page ────────────────────────────────────────────────────────────────

export default function PageDesignerPage() {
  const qc = useQueryClient();
  const { selectedSiteId } = useSite();
  const siteId = selectedSiteId ?? '';
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();

  const initialPageId = searchParams.get('pageId') ?? '';

  // ── State ────────────────────────────────────────────────────────────────
  const [selectedPageId, setSelectedPageId] = useState(initialPageId);
  const [placements, setPlacements] = useState<DesignerPlacement[]>([]);
  const [selectedLocalId, setSelectedLocalId] = useState<string | null>(null);
  const [dirty, setDirty] = useState(false);
  const [viewport, setViewport] = useState<ViewportSize>('desktop');
  const [zoom, setZoom] = useState(100);
  const [previewMode, setPreviewMode] = useState(false);
  const [paletteSearch, setPaletteSearch] = useState('');
  const prevPageId = useRef('');

  // ── Data ─────────────────────────────────────────────────────────────────
const { data: componentsResult } = useQuery({
    queryKey: ['components', siteId],
    queryFn: () => componentsApi.list({ siteId, pageSize: 200 }),
 enabled: !!siteId,
  });
const allComponents = componentsResult?.items ?? [];

  const { data: pageTree = [] } = useQuery({
 queryKey: ['pages', siteId],
    queryFn: () => pagesApi.getTree(siteId),
    enabled: !!siteId,
  });
  const flatPages = flattenPages(pageTree);

  const { data: pageDetail } = useQuery({
    queryKey: ['page-detail', selectedPageId],
  queryFn: () => pagesApi.getPage(selectedPageId),
    enabled: !!selectedPageId,
  });

  const { data: layouts = [] } = useQuery({
    queryKey: ['layouts', siteId],
    queryFn: () => layoutsApi.list(siteId),
    enabled: !!siteId,
  });

  const { data: loadedTemplate } = useQuery({
queryKey: ['page-template', selectedPageId],
    queryFn: async () => {
   try { return await pagesApi.getTemplate(selectedPageId); }
      catch (e) { if (e instanceof ApiError && e.status === 404) return null; throw e; }
    },
    enabled: !!selectedPageId,
  });

  // Sync placements when template loads for a new page
  useEffect(() => {
    if (!selectedPageId || selectedPageId === prevPageId.current) return;
    prevPageId.current = selectedPageId;
    if (loadedTemplate === undefined) return; // still loading
    const mapped: DesignerPlacement[] = (loadedTemplate?.placements ?? []).map((p) => {
   const comp = allComponents.find((c) => c.id === p.componentId);
   return {
   localId: uid(),
componentId: p.componentId,
        componentName: comp?.name ?? p.componentId,
componentKey: comp?.key ?? '',
 componentCategory: comp?.category ?? 'Content',
        zone: p.zone,
        sortOrder: p.sortOrder,
      };
    });
    setPlacements(mapped);
    setDirty(false);
    setSelectedLocalId(null);
  }, [selectedPageId, loadedTemplate, allComponents]);

  // ── Save mutation ────────────────────────────────────────────────────────
  const saveMutation = useMutation({
    mutationFn: () =>
      pagesApi.saveTemplate(selectedPageId, {
        placements: placements.map((p) => ({
          componentId: p.componentId,
          zone: p.zone,
          sortOrder: p.sortOrder,
        })),
      }),
    onSuccess: () => {
      toast.success('Page template saved.');
      setDirty(false);
      void qc.invalidateQueries({ queryKey: ['page-template', selectedPageId] });
    },
    onError: (err) =>
      toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Save failed.'),
  });

  // ── Canvas interactions ──────────────────────────────────────────────────
  const handleDragStart = useCallback((e: React.DragEvent, comp: ComponentListItem) => {
    e.dataTransfer.setData('application/microcms-comp', JSON.stringify({
      id: comp.id, name: comp.name, key: comp.key, category: comp.category,
    }));
    e.dataTransfer.effectAllowed = 'copy';
  }, []);

  const handleDrop = useCallback((zoneName: string, data: { id: string; name: string; key: string; category: string }) => {
    const comp = allComponents.find((c) => c.id === data.id);
    const sortOrder = placements.filter((p) => p.zone === zoneName).length;
    const np: DesignerPlacement = {
      localId: uid(),
      componentId: data.id,
      componentName: data.name,
   componentKey: data.key,
      componentCategory: (comp?.category ?? data.category) as ComponentCategory,
      zone: zoneName,
   sortOrder,
    };
    setPlacements((prev) => [...prev, np]);
    setSelectedLocalId(np.localId);
    setDirty(true);
  }, [allComponents, placements]);

  const handleMoveUp = useCallback((localId: string) => {
    setPlacements((prev) => {
      const idx = prev.findIndex((p) => p.localId === localId);
      if (idx <= 0) return prev;
      const next = [...prev];
      [next[idx - 1], next[idx]] = [next[idx], next[idx - 1]];
      return next.map((p, i) => ({ ...p, sortOrder: i }));
});
    setDirty(true);
  }, []);

  const handleMoveDown = useCallback((localId: string) => {
    setPlacements((prev) => {
      const idx = prev.findIndex((p) => p.localId === localId);
      if (idx < 0 || idx >= prev.length - 1) return prev;
      const next = [...prev];
      [next[idx], next[idx + 1]] = [next[idx + 1], next[idx]];
      return next.map((p, i) => ({ ...p, sortOrder: i }));
    });
    setDirty(true);
}, []);

  const handleRemove = useCallback((localId: string) => {
    setPlacements((prev) => prev.filter((p) => p.localId !== localId));
    setSelectedLocalId((cur) => cur === localId ? null : cur);
    setDirty(true);
  }, []);

  const handleSelectPage = (id: string) => {
    if (dirty && !confirm('You have unsaved changes. Switch pages anyway?')) return;
    prevPageId.current = '';
    setSelectedPageId(id);
    setPlacements([]);
    setDirty(false);
    setSelectedLocalId(null);
    navigate(`/designer?pageId=${id}`, { replace: true });
  };

  // ── Derived ──────────────────────────────────────────────────────────────
  const usedZones = Array.from(new Set(placements.map((p) => p.zone)));
  const canvasZones = usedZones.length > 0
    ? usedZones
    : ['header-zone', 'hero-zone', 'content-zone', 'cta-zone', 'footer-zone'];

  const assignedLayout = pageDetail?.layoutId
    ? layouts.find((l) => l.id === pageDetail.layoutId)
    : layouts.find((l) => l.isDefault);

  const selectedPlacement = placements.find((p) => p.localId === selectedLocalId) ?? null;

  const canvasWrapClass = viewport !== 'desktop' ? 'bg-slate-300 p-6' : 'bg-white';
  const canvasFrameClass = viewport === 'tablet'
 ? 'mx-auto max-w-[768px] rounded-lg shadow-xl overflow-hidden'
    : viewport === 'mobile'
    ? 'mx-auto max-w-[375px] rounded-lg shadow-xl overflow-hidden'
    : 'w-full';

  // ── No site guard ────────────────────────────────────────────────────────
  if (!siteId) {
  return (
      <div className="flex h-full items-center justify-center">
        <p className="text-sm text-slate-500">No site selected.</p>
      </div>
    );
  }

  return (
    <div className="-m-6 flex h-[calc(100vh-4rem)] flex-col overflow-hidden bg-white">
      {/* Topbar */}
  <DesignerTopbar
        page={pageDetail}
        viewport={viewport} setViewport={setViewport}
        zoom={zoom} changeZoom={(d) => setZoom((z) => Math.min(200, Math.max(50, z + d)))}
        previewMode={previewMode} togglePreview={() => setPreviewMode((p) => !p)}
        dirty={dirty} saving={saveMutation.isPending}
        onSave={() => { if (selectedPageId) saveMutation.mutate(); else toast.error('Select a page first.'); }}
      />

      {/* Three-panel body */}
      <div className="flex min-h-0 flex-1 overflow-hidden">

        {/* LEFT: Page tree + component palette */}
     <aside className="flex w-56 flex-shrink-0 flex-col overflow-hidden border-r border-slate-200 bg-white">
          {/* Page tree */}
    <div className="border-b border-slate-200">
            <div className="px-3 py-2.5">
      <span className="text-[10px] font-bold uppercase tracking-wider text-slate-400">Pages</span>
       </div>
            <div className="max-h-52 overflow-y-auto px-2 pb-2">
        {flatPages.length === 0 && <p className="px-2 py-3 text-xs text-slate-400">No pages yet.</p>}
 {flatPages.map((p) => (
 <button key={p.id} onClick={() => handleSelectPage(p.id)}
   className={`flex w-full items-center gap-1.5 rounded-md py-1.5 text-left text-xs transition-colors ${
   selectedPageId === p.id ? 'bg-brand-50 text-brand-700 ring-1 ring-brand-200' : 'text-slate-700 hover:bg-slate-50'
        }`}
  style={{ paddingLeft: `${p.depth * 12 + 8}px` }}>
         <svg className="h-3 w-3 flex-shrink-0 text-slate-300" fill="none" viewBox="0 0 24 24" stroke="currentColor">
           <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12h6m-6 4h4M7 4h10a2 2 0 012 2v14a2 2 0 01-2 2H7a2 2 0 01-2-2V6a2 2 0 012-2z" />
    </svg>
      <div className="min-w-0">
         <p className="truncate font-semibold">{p.title}</p>
       <p className="font-mono text-[10px] text-slate-400">/{p.slug}</p>
        </div>
        </button>
              ))}
     </div>
  </div>

     {/* Component palette (scrollable remainder) */}
 <PalettePanel
        components={allComponents}
            search={paletteSearch}
   setSearch={setPaletteSearch}
         onDragStart={handleDragStart}
    />
        </aside>

        {/* CENTRE: Canvas */}
        <div className={`flex min-w-0 flex-1 flex-col overflow-auto ${canvasWrapClass}`} style={{ zoom: zoom / 100 }}>
    {/* Canvas ruler */}
          {!previewMode && (
            <div className="sticky top-0 z-10 flex flex-shrink-0 items-center gap-2 border-b border-slate-200 bg-slate-100 px-4 py-1.5">
      <span className="text-[11px] text-slate-400">
    {pageDetail ? `Template Canvas — ${pageDetail.title}` : 'Select a page to begin'}
     </span>
         {pageDetail && (
           <span className="ml-1 rounded-full bg-brand-100 px-2 py-0.5 text-[10px] font-bold text-brand-700">
                  PAGE TEMPLATE
       </span>
   )}
    {assignedLayout && (
                <span className="rounded-full bg-purple-100 px-2 py-0.5 text-[10px] font-semibold text-purple-700">
       {assignedLayout.name}
    </span>
              )}
 <span className="ml-auto text-[11px] text-slate-400">
{placements.length} component{placements.length !== 1 ? 's' : ''}
        </span>
     </div>
          )}

          {/* Canvas frame */}
          <div className={canvasFrameClass}>
    {!selectedPageId ? (
              <div className="flex flex-1 items-center justify-center py-32 text-center">
    <div>
         <svg className="mx-auto mb-3 h-10 w-10 text-slate-200" fill="none" viewBox="0 0 24 24" stroke="currentColor">
         <rect x="3" y="3" width="18" height="18" rx="2" strokeWidth="1.5" />
 <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M3 9h18M9 21V9" />
   </svg>
   <p className="text-sm font-medium text-slate-400">Select a page from the left panel</p>
             <p className="mt-1 text-xs text-slate-300">to start building its template</p>
       </div>
     </div>
 ) : (
        canvasZones.map((zone) => (
<CanvasZone key={zone} zoneName={zone}
  placements={placements.filter((p) => p.zone === zone)}
      selectedLocalId={selectedLocalId}
      onSelect={setSelectedLocalId}
           onMoveUp={handleMoveUp}
  onMoveDown={handleMoveDown}
       onRemove={handleRemove}
       onDrop={handleDrop}
                  previewMode={previewMode}
        />
              ))
            )}
          </div>
        </div>

        {/* RIGHT: Properties */}
        <PropertiesPanel
 selected={selectedPlacement}
          placements={placements}
          allComponents={allComponents}
       onRemove={handleRemove}
        />
      </div>

      {/* Status bar */}
      <StatusBar placements={placements} page={pageDetail} layoutName={assignedLayout?.name} />
    </div>
  );
}
