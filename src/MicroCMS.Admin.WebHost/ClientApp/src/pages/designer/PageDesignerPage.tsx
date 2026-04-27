import { useState, useRef, useCallback, useEffect } from 'react';
import { Link, useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { componentsApi } from '@/api/components';
import { pagesApi } from '@/api/pages';
import { layoutsApi } from '@/api/layouts';
import { siteTemplatesApi } from '@/api/siteTemplates';
import { useSite } from '@/contexts/SiteContext';
import type { ComponentListItem, ComponentCategory, LayoutZoneNode } from '@/types';
import type { ViewportSize, DesignerPlacement } from './designerTypes';
import { ApiError } from '@/api/client';

// ─── Helpers ──────────────────────────────────────────────────────────────────

function uid() {
    return Math.random().toString(36).slice(2);
}

const CATEGORY_COLORS: Record<string, { bg: string; text: string }> = {
    Layout: { bg: 'bg-blue-100', text: 'text-blue-700' },
    Content: { bg: 'bg-emerald-100', text: 'text-emerald-700' },
    Media: { bg: 'bg-amber-100', text: 'text-amber-700' },
    Navigation: { bg: 'bg-indigo-900', text: 'text-indigo-200' },
    Interactive: { bg: 'bg-pink-100', text: 'text-pink-700' },
    Commerce: { bg: 'bg-orange-100', text: 'text-orange-700' },
};

const CATEGORY_ICON_PATH: Record<string, string> = {
    Layout: 'M4 6h16M4 10h10M4 14h16M4 18h10',
    Content: 'M4 6h16M4 10h16M4 14h12M4 18h8',
    Media: 'M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z',
    Navigation: 'M4 6h16M4 12h16M4 18h8',
    Interactive: 'M8.228 9c.549-1.165 2.03-2 3.772-2 2.21 0 4 1.343 4 3 0 1.4-1.278 2.575-3.006 2.907-.542.104-.994.54-.994 1.093m0 3h.01',
    Commerce: 'M3 3h2l.4 2M7 13h10l4-8H5.4M7 13L5.4 5M7 13l-2.293 2.293c-.63.63-.184 1.707.707 1.707H17m0 0a2 2 0 100 4 2 2 0 000-4zm-8 2a2 2 0 11-4 0 2 2 0 014 0z',
};

// ─── Topbar ───────────────────────────────────────────────────────────────────

function DesignerTopbar({
    pageTitle,
    pageSlug,
    viewport, setViewport,
    zoom, changeZoom,
    previewMode, togglePreview,
    dirty, saving, onSave,
}: {
    pageTitle: string;
    pageSlug: string;
    viewport: ViewportSize; setViewport: (v: ViewportSize) => void;
    zoom: number; changeZoom: (d: number) => void;
    previewMode: boolean; togglePreview: () => void;
    dirty: boolean; saving: boolean; onSave: () => void;
}) {
    return (
        <header className="flex h-14 flex-shrink-0 items-stretch overflow-hidden border-b border-slate-200 bg-white">
            {/* LEFT */}
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
                    <p className="max-w-[220px] truncate text-sm font-bold text-slate-900">{pageTitle}</p>
                    <p className="font-mono text-xs text-slate-400">/{pageSlug}</p>
                </div>
            </div>

            {/* Centre — status */}
            <div className="flex min-w-0 flex-1 items-center gap-3 px-4">
                <span className={`flex items-center gap-1.5 text-xs ${dirty ? 'text-amber-600' : 'text-slate-400'}`}>
                    <span className={`h-1.5 w-1.5 rounded-full ${dirty ? 'bg-amber-400' : 'bg-green-400'}`} />
                    {dirty ? 'Unsaved changes' : 'Saved'}
                </span>
            </div>

            {/* RIGHT */}
            <div className="flex flex-shrink-0 items-center gap-2 px-3">
                <div className="flex overflow-hidden rounded-md border border-slate-200">
                    {(['desktop', 'tablet', 'mobile'] as ViewportSize[]).map((vp) => (
                        <button key={vp} onClick={() => setViewport(vp)} title={vp}
                            className={`px-2 py-1.5 transition-colors ${viewport === vp ? 'bg-brand-600 text-white' : 'bg-white text-slate-400 hover:bg-slate-50'} ${vp !== 'desktop' ? 'border-l border-slate-200' : ''}`}>
                            {vp === 'desktop' && <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><rect x="2" y="4" width="20" height="14" rx="2" strokeWidth="2" /><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M8 20h8M12 18v2" /></svg>}
                            {vp === 'tablet' && <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><rect x="4" y="2" width="16" height="20" rx="2" strokeWidth="2" /><circle cx="12" cy="18" r="1" fill="currentColor" /></svg>}
                            {vp === 'mobile' && <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><rect x="7" y="2" width="10" height="20" rx="2" strokeWidth="2" /><circle cx="12" cy="18" r="1" fill="currentColor" /></svg>}
                        </button>
                    ))}
                </div>
                <div className="flex items-center gap-1 text-xs text-slate-500">
                    <button onClick={() => changeZoom(-10)} className="flex h-5 w-5 items-center justify-center rounded border border-slate-200 hover:bg-slate-50">−</button>
                    <span className="w-10 text-center font-mono">{zoom}%</span>
                    <button onClick={() => changeZoom(+10)} className="flex h-5 w-5 items-center justify-center rounded border border-slate-200 hover:bg-slate-50">+</button>
                </div>
                <div className="h-5 w-px bg-slate-200" />
                <button onClick={togglePreview}
                    className={`flex items-center gap-1.5 rounded-md border px-2.5 py-1.5 text-xs font-semibold transition-colors ${previewMode ? 'border-brand-500 bg-brand-50 text-brand-700' : 'border-slate-200 bg-white text-slate-500 hover:border-brand-400 hover:text-brand-600'}`}>
                    <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
                    </svg>
                    {previewMode ? 'Exit Preview' : 'Preview'}
                </button>
                <button onClick={onSave} disabled={saving || !dirty} className="btn-primary h-8 px-3 py-0 text-xs disabled:opacity-50">
                    {saving ? 'Saving…' : 'Save'}
                </button>
            </div>
        </header>
    );
}

// ─── Palette ─────────────────────────────────────────────────────────────────

function PalettePanel({ components, search, setSearch, onDragStart }: {
    components: ComponentListItem[];
    search: string;
    setSearch: (s: string) => void;
    onDragStart: (e: React.DragEvent, comp: ComponentListItem) => void;
}) {
    const filtered = components.filter(
        (c) => !search || c.name.toLowerCase().includes(search.toLowerCase()),
    );
    return (
        <aside className="flex w-56 flex-shrink-0 flex-col overflow-hidden border-r border-slate-200 bg-white">
            <div className="border-b border-slate-200 px-3 py-3">
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
                {filtered.length === 0 && (
                    <p className="py-6 text-center text-xs text-slate-400">No components found.<br />Add some in Component Library.</p>
                )}
                {filtered.map((comp) => {
                    const colors = CATEGORY_COLORS[comp.category] ?? { bg: 'bg-slate-100', text: 'text-slate-600' };
                    const iconPath = CATEGORY_ICON_PATH[comp.category] ?? '';
                    return (
                        <div key={comp.id} draggable onDragStart={(e) => onDragStart(e, comp)}
                            className="mb-1 flex cursor-grab items-center gap-2 rounded-md border border-transparent px-2 py-1.5 hover:border-brand-200 hover:bg-brand-50 active:cursor-grabbing active:opacity-70">
                            <div className={`flex h-7 w-7 flex-shrink-0 items-center justify-center rounded ${colors.bg}`}>
                                <svg className={`h-3.5 w-3.5 ${colors.text}`} fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d={iconPath} />
                                </svg>
                            </div>
                            <div className="min-w-0">
                                <p className="truncate text-xs font-semibold text-slate-800">{comp.name}</p>
                                <p className="text-[10px] text-slate-400">{comp.fieldCount} fields · {comp.category}</p>
                            </div>
                        </div>
                    );
                })}
            </div>
        </aside>
    );
}

// ─── Canvas Zone ─────────────────────────────────────────────────────────────

function CanvasZone({
    zone, placements, selectedLocalId, onSelect, onMoveUp, onMoveDown, onRemove, onDrop, previewMode, isInherited,
}: {
    zone: LayoutZoneNode;
    placements: DesignerPlacement[];
    selectedLocalId: string | null;
    onSelect: (id: string) => void;
    onMoveUp: (id: string) => void;
    onMoveDown: (id: string) => void;
    onRemove: (id: string) => void;
    onDrop: (zone: string, comp: { id: string; name: string; key: string; category: string }) => void;
    previewMode: boolean;
    isInherited?: boolean;
}) {
    const [dragOver, setDragOver] = useState(false);

    return (
        <div className="mb-4">
            {/* Zone label */}
            {!previewMode && (
                <div className="mb-1 flex items-center gap-2">
                    <span className={`rounded px-1.5 py-0.5 text-[9px] font-bold uppercase tracking-widest ${zone.type === 'grid-row' ? 'bg-purple-100 text-purple-600' : 'bg-brand-100 text-brand-500'}`}>
                        {zone.type === 'grid-row' ? 'grid' : 'zone'}
                    </span>
                    <span className="text-xs font-bold text-slate-700">{zone.label}</span>
                    <span className="font-mono text-[10px] text-slate-400">{zone.name}</span>
                    {isInherited && <span className="rounded bg-amber-100 px-1.5 py-0.5 text-[9px] font-semibold text-amber-600">from template</span>}
                </div>
            )}
            {/* Drop area */}
            <div onDragOver={(e) => { e.preventDefault(); setDragOver(true); }}
                onDragLeave={() => setDragOver(false)}
                onDrop={(e) => {
                    e.preventDefault(); setDragOver(false);
                    const raw = e.dataTransfer.getData('application/microcms-comp');
                    if (raw) try { onDrop(zone.name, JSON.parse(raw)); } catch { /* ignore */ }
                }}
                className={`relative min-h-[60px] rounded-lg border-2 border-dashed transition-colors ${previewMode ? 'border-transparent'
                        : dragOver ? 'border-brand-400 bg-brand-50/40'
                            : placements.length === 0 ? 'border-slate-200 bg-slate-50/60'
                                : 'border-transparent hover:border-slate-200'}`}>

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

                {placements.map((p) => {
                    const isSelected = p.localId === selectedLocalId;
                    return (
                        <div key={p.localId} onClick={() => !previewMode && onSelect(p.localId)}
                            className={`group relative cursor-pointer border-2 transition-all ${previewMode ? 'border-transparent'
                                    : isSelected ? 'border-brand-500 shadow-[0_0_0_2px_rgba(99,102,241,0.15)]'
                                        : 'border-transparent hover:border-brand-300/60'}`}>
                            {!previewMode && (
                                <div className={`absolute right-0 top-0 z-20 flex items-center rounded-bl-md bg-brand-600 ${isSelected ? 'opacity-100' : 'opacity-0 group-hover:opacity-100'}`}>
                                    <span className="border-r border-white/20 px-2 py-1 text-[10px] font-bold text-white/90">{p.componentName}</span>
                                    {p.isLayoutDefault && <span className="border-r border-white/20 px-2 py-1 text-[9px] text-amber-200">inherited</span>}
                                    {!p.isLayoutDefault && (
                                        <>
                                            <button onClick={(e) => { e.stopPropagation(); onMoveUp(p.localId); }} className="px-1.5 py-1 text-white hover:bg-white/20">
                                                <svg className="h-3 w-3" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 15l7-7 7 7" /></svg>
                                            </button>
                                            <button onClick={(e) => { e.stopPropagation(); onMoveDown(p.localId); }} className="px-1.5 py-1 text-white hover:bg-white/20">
                                                <svg className="h-3 w-3" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" /></svg>
                                            </button>
                                            <button onClick={(e) => { e.stopPropagation(); onRemove(p.localId); }} className="px-1.5 py-1 text-red-300 hover:bg-white/20">
                                                <svg className="h-3 w-3" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" /></svg>
                                            </button>
                                        </>
                                    )}
                                </div>
                            )}
                            <ComponentPreview component={p} />
                        </div>
                    );
                })}
            </div>
        </div>
    );
}

// ─── Component Preview ────────────────────────────────────────────────────────

function ComponentPreview({ component }: { component: DesignerPlacement }) {
    const colors = CATEGORY_COLORS[component.componentCategory ?? 'Content'] ?? { bg: 'bg-slate-100', text: 'text-slate-600' };
    const iconPath = CATEGORY_ICON_PATH[component.componentCategory ?? 'Content'] ?? '';
    if (component.componentCategory === 'Navigation') {
        return (
            <div className="flex items-center justify-between bg-indigo-950 px-8 py-3.5">
                <span className="text-sm font-bold text-white">{component.componentName}</span>
                <span className="rounded-md bg-brand-600 px-3 py-1.5 text-xs font-semibold text-white">Get Started</span>
            </div>
        );
    }
    if (component.componentCategory === 'Layout') {
        return (
            <div className="relative bg-gradient-to-br from-indigo-950 to-brand-600 px-12 py-14">
                <div className="mb-3 h-7 w-3/4 rounded-md bg-white/20" />
                <div className="mb-5 h-2.5 w-5/6 rounded bg-white/10" />
                <span className="absolute right-3 top-1 text-[9px] font-semibold text-white/40">{component.componentName}</span>
            </div>
        );
    }
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

function PropertiesPanel({ selected, placements, onRemove }: {
    selected: DesignerPlacement | null;
    placements: DesignerPlacement[];
    onRemove: (id: string) => void;
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
                        <p className="mt-0.5 text-xs text-slate-400">Click a component or drag one from the palette.</p>
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
    const colors = CATEGORY_COLORS[selected.componentCategory ?? 'Content'] ?? { bg: 'bg-slate-100', text: 'text-slate-600' };
    const iconPath = CATEGORY_ICON_PATH[selected.componentCategory ?? 'Content'] ?? '';
    return (
        <aside className="flex w-64 flex-shrink-0 flex-col overflow-hidden border-l border-slate-200 bg-white">
            <div className="flex items-center gap-2.5 border-b border-slate-200 px-4 py-3">
                <div className={`flex h-7 w-7 items-center justify-center rounded-md ${colors.bg}`}>
                    <svg className={`h-3.5 w-3.5 ${colors.text}`} fill="none" viewBox="0 0 24 24" stroke="currentColor">
                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d={iconPath} />
                    </svg>
                </div>
                <div className="min-w-0">
                    <p className="truncate text-sm font-bold text-slate-900">{selected.componentName}</p>
                    <p className="text-[11px] text-slate-400">{selected.componentCategory} · {selected.zone}</p>
                </div>
            </div>
            <div className="flex-1 space-y-4 overflow-y-auto px-4 py-3">
                <div>
                    <p className="mb-1 text-[10px] font-bold uppercase tracking-wider text-slate-400">Zone</p>
                    <div className="rounded-md border border-slate-200 bg-slate-50 px-3 py-2 font-mono text-xs">{selected.zone}</div>
                </div>
                {selected.isLayoutDefault && (
                    <div className="rounded-md border border-amber-200 bg-amber-50 px-3 py-2 text-xs text-amber-700">
                        Inherited from template — cannot be removed here.
                    </div>
                )}
            </div>
            {!selected.isLayoutDefault && (
                <div className="flex-shrink-0 border-t border-slate-200 p-3">
                    <button onClick={() => onRemove(selected.localId)}
                        className="flex w-full items-center justify-center gap-1.5 rounded-md border border-red-200 bg-red-50 px-3 py-1.5 text-xs font-semibold text-red-600 hover:bg-red-100">
                        Remove from Zone
                    </button>
                </div>
            )}
        </aside>
    );
}

// ─── Main ─────────────────────────────────────────────────────────────────────

export default function PageDesignerPage() {
    const qc = useQueryClient();
    const { selectedSiteId } = useSite();
    const { pageId } = useParams<{ pageId: string }>();
    const navigate = useNavigate();

    const [placements, setPlacements] = useState<DesignerPlacement[]>([]);
    const [selectedLocalId, setSelectedLocalId] = useState<string | null>(null);
    const [dirty, setDirty] = useState(false);
    const [viewport, setViewport] = useState<ViewportSize>('desktop');
    const [zoom, setZoom] = useState(100);
    const [previewMode, setPreviewMode] = useState(false);
    const [paletteSearch, setPaletteSearch] = useState('');
    const [initialised, setInitialised] = useState(false);

    // ── Data ─────────────────────────────────────────────────────────────────
    const { data: componentsResult } = useQuery({
        queryKey: ['components', selectedSiteId],
        queryFn: () => componentsApi.list({ siteId: selectedSiteId!, pageSize: 200 }),
        enabled: !!selectedSiteId,
    });
    const allComponents = componentsResult?.items ?? [];

    const { data: pageDetail } = useQuery({
        queryKey: ['page-detail', pageId],
        queryFn: () => pagesApi.getPage(pageId!),
        enabled: !!pageId,
    });

    // Load the page's assigned layout to get its zones
    const { data: allLayouts = [] } = useQuery({
        queryKey: ['layouts', selectedSiteId],
        queryFn: () => layoutsApi.list(selectedSiteId!),
        enabled: !!selectedSiteId,
    });

    const assignedLayoutId = pageDetail?.layoutId
        ?? allLayouts.find((l) => l.isDefault)?.id;

    // Fetch the full LayoutDto (contains zones[]) for the assigned layout
    const { data: assignedLayout } = useQuery({
        queryKey: ['layout', assignedLayoutId],
        queryFn: () => layoutsApi.get(assignedLayoutId!),
        enabled: !!assignedLayoutId,
    });

    // Zones from the layout — fall back to a reasonable default only when no layout exists
    const zones: LayoutZoneNode[] = assignedLayout?.zones?.length
        ? [...assignedLayout.zones].sort((a, b) => a.sortOrder - b.sortOrder)
        : [
            { id: 'z1', type: 'zone', name: 'header', label: 'Header', sortOrder: 0 },
            { id: 'z2', type: 'zone', name: 'content', label: 'Content', sortOrder: 1 },
            { id: 'z3', type: 'zone', name: 'footer', label: 'Footer', sortOrder: 2 },
        ];

    // Load template for the page (includes template-inherited placements)
    const { data: loadedTemplate } = useQuery({
        queryKey: ['page-template', pageId],
        queryFn: async () => {
            try { return await pagesApi.getTemplate(pageId!); }
            catch (e) { if (e instanceof ApiError && e.status === 404) return null; throw e; }
        },
        enabled: !!pageId,
    });

    // Load site-template inherited placements (if the page is linked to a template)
    const siteTemplateId = (pageDetail as any)?.siteTemplateId as string | undefined;
    const { data: siteTemplate } = useQuery({
        queryKey: ['site-template', siteTemplateId],
        queryFn: () => siteTemplatesApi.get(siteTemplateId!),
        enabled: !!siteTemplateId,
        staleTime: 30_000,
    });

    // Initialise canvas: inherited placements (locked) + page-specific placements
    useEffect(() => {
        if (initialised || !pageId || loadedTemplate === undefined) return;

        // Inherited from site template (locked — cannot be removed)
        let inherited: DesignerPlacement[] = [];
        if (siteTemplate) {
            try {
                const parsed = JSON.parse(siteTemplate.placementsJson ?? '[]') as DesignerPlacement[];
                inherited = parsed.map((p) => {
                    const comp = allComponents.find((c) => c.id === p.componentId);
                    return { ...p, localId: `tpl-${uid()}`, componentName: comp?.name ?? p.componentName, isLayoutDefault: true };
                });
            } catch { /* ignore */ }
        }

        // Page-specific placements
        const pageSpecific: DesignerPlacement[] = (loadedTemplate?.placements ?? []).map((p) => {
            const comp = allComponents.find((c) => c.id === p.componentId);
            return {
                localId: uid(), type: 'component' as const,
                componentId: p.componentId, componentName: comp?.name ?? p.componentId,
                componentKey: comp?.key ?? '', componentCategory: comp?.category ?? 'Content',
                zone: p.zone, sortOrder: p.sortOrder, isLayoutDefault: false,
            };
        });

        setPlacements([...inherited, ...pageSpecific]);
        setInitialised(true);
        setDirty(false);
    }, [pageId, loadedTemplate, siteTemplate, allComponents, initialised]);

    // Reset when pageId changes
    const prevPageId = useRef('');
    if (pageId !== prevPageId.current) {
        prevPageId.current = pageId ?? '';
        if (initialised) {
            setInitialised(false);
            setPlacements([]);
            setDirty(false);
            setSelectedLocalId(null);
        }
    }

    // ── Save ─────────────────────────────────────────────────────────────────
    const saveMutation = useMutation({
        mutationFn: () =>
            pagesApi.saveTemplate(pageId!, {
                // Only save page-specific (non-inherited) placements
                placements: placements
                    .filter((p) => !p.isLayoutDefault)
                    .map((p) => ({ type: 'component' as const, componentId: p.componentId!, zone: p.zone, sortOrder: p.sortOrder })),
            }),
        onSuccess: () => {
            toast.success('Page template saved.');
            setDirty(false);
            void qc.invalidateQueries({ queryKey: ['page-template', pageId] });
        },
        onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Save failed.'),
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
        const sortOrder = placements.filter((p) => p.zone === zoneName && !p.isLayoutDefault).length;
        const np: DesignerPlacement = {
            localId: uid(), type: 'component',
            componentId: data.id, componentName: data.name,
            componentKey: data.key, componentCategory: (comp?.category ?? data.category) as ComponentCategory,
            zone: zoneName, sortOrder, isLayoutDefault: false,
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

    // ── Derived ──────────────────────────────────────────────────────────────
    const selectedPlacement = placements.find((p) => p.localId === selectedLocalId) ?? null;
    const canvasWrapClass = viewport !== 'desktop' ? 'bg-slate-300 p-6' : 'bg-white';
    const canvasFrameClass = viewport === 'tablet'
        ? 'mx-auto max-w-[768px] rounded-lg shadow-xl overflow-hidden'
        : viewport === 'mobile'
            ? 'mx-auto max-w-[375px] rounded-lg shadow-xl overflow-hidden'
            : 'w-full';

    if (!selectedSiteId) {
        return <div className="flex h-full items-center justify-center text-sm text-slate-500">No site selected.</div>;
    }

    if (!pageId) {
        return (
            <div className="flex h-full flex-col items-center justify-center gap-4 text-center">
                <p className="text-sm text-slate-500">No page specified. Access this designer from the Pages section.</p>
                <button onClick={() => navigate('/pages')} className="btn-primary">Go to Pages</button>
            </div>
        );
    }

    return (
        <div className="-m-6 flex h-[calc(100vh-4rem)] flex-col overflow-hidden bg-white">
            <DesignerTopbar
                pageTitle={pageDetail?.title ?? 'Page Designer'}
                pageSlug={pageDetail?.slug ?? '…'}
                viewport={viewport} setViewport={setViewport}
                zoom={zoom} changeZoom={(d) => setZoom((z) => Math.min(200, Math.max(50, z + d)))}
                previewMode={previewMode} togglePreview={() => setPreviewMode((p) => !p)}
                dirty={dirty} saving={saveMutation.isPending}
                onSave={() => saveMutation.mutate()}
            />

            <div className="flex min-h-0 flex-1 overflow-hidden">
                {/* LEFT — component palette only */}
                <PalettePanel
                    components={allComponents}
                    search={paletteSearch}
                    setSearch={setPaletteSearch}
                    onDragStart={handleDragStart}
                />

                {/* CENTRE — canvas */}
                <div className={`flex min-w-0 flex-1 flex-col overflow-auto ${canvasWrapClass}`} style={{ zoom: zoom / 100 }}>
                    {/* Canvas ruler */}
                    {!previewMode && (
                        <div className="sticky top-0 z-10 flex flex-shrink-0 items-center gap-2 border-b border-slate-200 bg-slate-100 px-4 py-1.5">
                            <span className="text-[11px] text-slate-400">
                                {pageDetail ? `Page Canvas — ${pageDetail.title}` : 'Loading…'}
                            </span>
                            {assignedLayout && (
                                <span className="rounded-full bg-purple-100 px-2 py-0.5 text-[10px] font-semibold text-purple-700">
                                    {assignedLayout.name}
                                </span>
                            )}
                            {siteTemplate && (
                                <span className="rounded-full bg-amber-100 px-2 py-0.5 text-[10px] font-semibold text-amber-700">
                                    Template: {siteTemplate.name}
                                </span>
                            )}
                            {!assignedLayout && (
                                <span className="rounded-full bg-slate-200 px-2 py-0.5 text-[10px] text-slate-500">
                                    No layout — assign one in Pages → Edit
                                </span>
                            )}
                            <span className="ml-auto text-[11px] text-slate-400">
                                {placements.filter((p) => !p.isLayoutDefault).length} page component{placements.filter((p) => !p.isLayoutDefault).length !== 1 ? 's' : ''
                                }          </span>
                        </div>
                    )}

                    <div className={canvasFrameClass}>
                        {siteTemplate && !previewMode && (
                            <div className="border-b border-amber-100 bg-amber-50 px-6 py-2 text-xs text-amber-700">
                                Greyed zones are inherited from template <strong>{siteTemplate.name}</strong> and cannot be edited here.
                            </div>
                        )}
                        <div className="p-6">
                            {zones.map((zone) => (
                                <CanvasZone
                                    key={zone.id}
                                    zone={zone}
                                    placements={placements.filter((p) => p.zone === zone.name)}
                                    selectedLocalId={selectedLocalId}
                                    onSelect={setSelectedLocalId}
                                    onMoveUp={handleMoveUp}
                                    onMoveDown={handleMoveDown}
                                    onRemove={handleRemove}
                                    onDrop={handleDrop}
                                    previewMode={previewMode}
                                    isInherited={placements.some((p) => p.zone === zone.name && p.isLayoutDefault)}
                                />
                            ))}
                        </div>
                    </div>
                </div>

                {/* RIGHT — properties */}
                <PropertiesPanel
                    selected={selectedPlacement}
                    placements={placements}
                    onRemove={handleRemove}
                />
            </div>

            {/* Status bar */}
            <div className="flex h-6 flex-shrink-0 items-center gap-4 border-t border-slate-200 bg-slate-50 px-4 text-[11px] text-slate-400">
                <span className="flex items-center gap-1">
                    <span className="h-1.5 w-1.5 rounded-full bg-green-400" />Page Designer
                </span>
                <span>{placements.filter((p) => !p.isLayoutDefault).length} page component{placements.filter((p) => !p.isLayoutDefault).length !== 1 ? 's' : ''}</span>
                {siteTemplate && <><span>·</span><span>{placements.filter((p) => p.isLayoutDefault).length} inherited from template</span></>}
                {assignedLayout && <><span>·</span><span>Layout: {assignedLayout.name}</span></>}
                <span className="ml-auto font-mono">/{pageDetail?.slug ?? '…'}</span>
            </div>
        </div>
    );
}
