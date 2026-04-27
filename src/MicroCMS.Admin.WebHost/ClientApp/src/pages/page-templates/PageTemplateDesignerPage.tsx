/**
 * PageTemplateDesignerPage — design reusable templates.
 *
 * Zones are loaded directly from the template's linked Layout.
 * Components dropped into zones are saved as the template's shared placements.
 * Pages that use this template inherit these placements automatically.
 */
import { useState, useCallback, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { siteTemplatesApi } from '@/api/siteTemplates';
import { layoutsApi } from '@/api/layouts';
import { componentsApi } from '@/api/components';
import { useSite } from '@/contexts/SiteContext';
import type { ComponentListItem, ComponentCategory, LayoutZoneNode } from '@/types';
import type { PlacementNode } from '../designer/designerTypes';
import { ApiError } from '@/api/client';

function uid() { return Math.random().toString(36).slice(2); }

const CAT_COLORS: Record<string, { bg: string; text: string }> = {
  Layout: { bg: 'bg-blue-100', text: 'text-blue-700' },
  Content: { bg: 'bg-emerald-100', text: 'text-emerald-700' },
  Media: { bg: 'bg-amber-100', text: 'text-amber-700' },
  Navigation: { bg: 'bg-indigo-900', text: 'text-indigo-200' },
  Interactive: { bg: 'bg-pink-100', text: 'text-pink-700' },
  Commerce: { bg: 'bg-orange-100', text: 'text-orange-700' },
};

// ─── Palette ──────────────────────────────────────────────────────────────────

function Palette({
  components,
  search,
  setSearch,
  onDragStart,
}: {
  components: ComponentListItem[];
  search: string;
  setSearch: (s: string) => void;
  onDragStart: (e: React.DragEvent, c: ComponentListItem) => void;
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
          <p className="py-6 text-center text-xs text-slate-400">No components found.</p>
 )}
        {filtered.map((comp) => {
          const colors = CAT_COLORS[comp.category] ?? { bg: 'bg-slate-100', text: 'text-slate-600' };
  return (
  <div key={comp.id} draggable onDragStart={(e) => onDragStart(e, comp)}
           className="mb-1 flex cursor-grab items-center gap-2 rounded-md border border-transparent px-2 py-1.5 hover:border-brand-200 hover:bg-brand-50 active:cursor-grabbing">
        <div className={`flex h-7 w-7 flex-shrink-0 items-center justify-center rounded ${colors.bg}`}>
        <span className={`text-[10px] font-bold ${colors.text}`}>{comp.name.charAt(0)}</span>
         </div>
       <div className="min-w-0">
     <p className="truncate text-xs font-semibold text-slate-800">{comp.name}</p>
     <p className="text-[10px] text-slate-400">{comp.category}</p>
              </div>
            </div>
          );
        })}
      </div>
    </aside>
  );
}

// ─── Zone strip ───────────────────────────────────────────────────────────────

function ZoneStrip({
  zone,
  placements,
  onDrop,
  onRemove,
}: {
  zone: LayoutZoneNode;
  placements: PlacementNode[];
  onDrop: (zoneName: string, comp: ComponentListItem) => void;
  onRemove: (localId: string) => void;
}) {
  const [over, setOver] = useState(false);

  return (
    <div className="mb-3">
  {/* Zone header */}
      <div className="mb-1 flex items-center gap-2">
 <span className={`flex h-4 w-4 items-center justify-center rounded text-[8px] font-black ${zone.type === 'grid-row' ? 'bg-purple-200 text-purple-700' : 'bg-brand-200 text-brand-700'}`}>
          {zone.type === 'grid-row' ? 'G' : 'Z'}
        </span>
        <span className="text-xs font-bold text-slate-700">{zone.label}</span>
        <span className="font-mono text-[10px] text-slate-400">{zone.name}</span>
      </div>

 {/* Drop area */}
      <div
    onDragOver={(e) => { e.preventDefault(); setOver(true); }}
        onDragLeave={() => setOver(false)}
      onDrop={(e) => {
       e.preventDefault(); setOver(false);
        const raw = e.dataTransfer.getData('application/microcms-comp');
          if (raw) try { onDrop(zone.name, JSON.parse(raw) as ComponentListItem); } catch { /* ignore */ }
        }}
    className={`min-h-[52px] rounded-lg border-2 border-dashed transition-colors ${over ? 'border-brand-400 bg-brand-50/40' : placements.length === 0 ? 'border-slate-200 bg-slate-50/60' : 'border-transparent'}`}
      >
  {placements.length === 0 && (
          <div className="flex h-12 items-center justify-center">
<span className="text-xs text-slate-300">Drop a component here</span>
          </div>
   )}
  <div className="space-y-1 p-1">
 {placements.map((p) => (
            <div key={p.localId} className="flex items-center justify-between rounded-md border border-slate-200 bg-white px-3 py-2">
     <div>
         <p className="text-xs font-semibold text-slate-800">{p.componentName}</p>
    <p className="font-mono text-[10px] text-slate-400">{p.componentKey}</p>
     </div>
  <button onClick={() => onRemove(p.localId)} className="text-slate-300 hover:text-red-500">
 <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
 <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
    </svg>
        </button>
            </div>
))}
        </div>
      </div>
    </div>
  );
}

// ─── Main ─────────────────────────────────────────────────────────────────────

export default function PageTemplateDesignerPage() {
  const { id } = useParams<{ id: string }>();
  const qc = useQueryClient();
  const { selectedSiteId } = useSite();

  const [placements, setPlacements] = useState<PlacementNode[]>([]);
  const [initialised, setInitialised] = useState(false);
  const [dirty, setDirty] = useState(false);
  const [search, setSearch] = useState('');

  const { data: template, isLoading: templateLoading } = useQuery({
    queryKey: ['site-template', id],
    queryFn: () => siteTemplatesApi.get(id!),
    enabled: !!id,
    staleTime: 0,
  });

  const { data: layout } = useQuery({
    queryKey: ['layout', template?.layoutId],
    queryFn: () => layoutsApi.get(template!.layoutId),
    enabled: !!template?.layoutId,
  });

  const { data: componentsResult } = useQuery({
    queryKey: ['components', selectedSiteId],
    queryFn: () => componentsApi.list({ siteId: selectedSiteId!, pageSize: 200 }),
    enabled: !!selectedSiteId,
  });
  const allComponents = componentsResult?.items ?? [];

  // Load placements from template JSON (once)
  useEffect(() => {
 if (!template || initialised) return;
    try {
      const loaded = JSON.parse(template.placementsJson ?? '[]') as PlacementNode[];
      const hydrated = loaded.map((p) => {
      const comp = allComponents.find((c) => c.id === p.componentId);
 return { ...p, localId: p.localId ?? uid(), componentName: comp?.name ?? p.componentName, componentKey: comp?.key ?? p.componentKey };
    });
      setPlacements(hydrated);
    } catch { /* empty template */ }
    setInitialised(true);
  }, [template, initialised, allComponents]);

  // Zones from the linked layout; fall back to a single "content" zone if not yet loaded
  const zones: LayoutZoneNode[] = layout?.zones.length
    ? [...layout.zones].sort((a, b) => a.sortOrder - b.sortOrder)
    : [{ id: 'default', type: 'zone', name: 'content', label: 'Content', sortOrder: 0 }];

  const saveMutation = useMutation({
    mutationFn: () => siteTemplatesApi.savePlacements(id!, {
    placements: placements.map((p) => ({
   type: 'component' as const,
        zone: p.zone,
        sortOrder: p.sortOrder,
        componentId: p.componentId,
      })),
    }),
    onSuccess: () => { toast.success('Template saved.'); setDirty(false); void qc.invalidateQueries({ queryKey: ['site-template', id] }); },
    onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Save failed.'),
  });

  const handleDragStart = useCallback((e: React.DragEvent, comp: ComponentListItem) => {
    e.dataTransfer.setData('application/microcms-comp', JSON.stringify(comp));
    e.dataTransfer.effectAllowed = 'copy';
  }, []);

  const handleDrop = useCallback((zoneName: string, comp: ComponentListItem) => {
    const sortOrder = placements.filter((p) => p.zone === zoneName).length;
    const node: PlacementNode = {
      localId: uid(), type: 'component', zone: zoneName, sortOrder,
      componentId: comp.id, componentName: comp.name,
      componentKey: comp.key, componentCategory: comp.category as ComponentCategory,
    };
    setPlacements((prev) => [...prev, node]);
    setDirty(true);
  }, [placements]);

  const handleRemove = useCallback((localId: string) => {
    setPlacements((prev) => prev.filter((p) => p.localId !== localId));
    setDirty(true);
  }, []);

  if (templateLoading) {
    return <div className="flex h-full items-center justify-center text-sm text-slate-400">Loading template…</div>;
  }

  return (
    <div className="-m-6 flex h-[calc(100vh-4rem)] flex-col overflow-hidden bg-white">

      {/* Topbar */}
<header className="flex h-14 flex-shrink-0 items-center gap-3 border-b border-slate-200 bg-white px-4">
        <Link to="/page-templates" className="rounded p-1 text-slate-400 hover:bg-slate-100">
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
        <div className="min-w-0 flex-1">
          <p className="text-sm font-bold text-slate-900">{template?.name ?? 'Page Template Designer'}</p>
          <p className="text-xs text-slate-400">
            Layout: <span className="font-medium text-slate-600">{layout?.name ?? template?.layoutId ?? '…'}</span>
 {' · '}
            <span className="text-slate-400">{zones.length} zones</span>
       </p>
 </div>
        <span className={`flex items-center gap-1.5 text-xs ${dirty ? 'text-amber-600' : 'text-slate-400'}`}>
 <span className={`h-1.5 w-1.5 rounded-full ${dirty ? 'bg-amber-400' : 'bg-green-400'}`} />
          {dirty ? 'Unsaved changes' : 'Saved'}
        </span>
        <button onClick={() => saveMutation.mutate()} disabled={saveMutation.isPending || !dirty}
      className="btn-primary h-8 px-3 py-0 text-xs disabled:opacity-50">
          {saveMutation.isPending ? 'Saving…' : 'Save Template'}
  </button>
      </header>

      {/* Body */}
  <div className="flex min-h-0 flex-1 overflow-hidden">

        {/* LEFT — palette */}
 <Palette components={allComponents} search={search} setSearch={setSearch} onDragStart={handleDragStart} />

        {/* CENTRE — canvas */}
        <div className="flex min-w-0 flex-1 flex-col overflow-auto bg-slate-50 p-6">
    <div className="mx-auto w-full max-w-3xl">
     {/* Callout */}
 <div className="mb-4 rounded-lg border border-brand-100 bg-brand-50 px-4 py-3 text-xs text-brand-700">
              <strong>Template canvas:</strong> Components placed here are inherited by every page that uses this template.
     Pages can add further components on top in the Page Designer (Pages → Design).
          </div>

         {/* No layout warning */}
         {!layout && !templateLoading && (
    <div className="mb-4 rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-xs text-amber-700">
        ⚠ This template has no linked layout yet. Zone structure cannot be shown.
    <Link to="/page-templates" className="ml-2 font-semibold underline">Edit template →</Link>
      </div>
  )}

  {/* Zones */}
      {zones.map((zone) => (
   <ZoneStrip
       key={zone.id}
                zone={zone}
      placements={placements.filter((p) => p.zone === zone.name)}
         onDrop={handleDrop}
     onRemove={handleRemove}
  />
            ))}
          </div>
        </div>

        {/* RIGHT — info */}
        <aside className="flex w-56 flex-shrink-0 flex-col overflow-y-auto border-l border-slate-200 bg-white">
 <div className="border-b border-slate-200 px-4 py-3">
          <p className="text-xs font-bold uppercase tracking-wider text-slate-400">Template Info</p>
          </div>
  <div className="space-y-4 px-4 py-4 text-xs">
         <div>
           <p className="text-slate-400">Layout</p>
<p className="mt-0.5 font-semibold text-slate-700">{layout?.name ?? '—'}</p>
            </div>
            <div>
     <p className="text-slate-400">Zones available</p>
              <div className="mt-1 space-y-1">
         {zones.map((z) => (
               <div key={z.id} className="flex items-center gap-2">
        <span className={`h-1.5 w-1.5 rounded-full ${z.type === 'grid-row' ? 'bg-purple-400' : 'bg-brand-400'}`} />
      <span className="font-mono text-slate-600">{z.name}</span>
      <span className="text-slate-400">
          ({placements.filter((p) => p.zone === z.name).length})
        </span>
  </div>
     ))}
      </div>
            </div>
            <div>
          <p className="text-slate-400">Total placements</p>
   <p className="mt-0.5 text-lg font-bold text-slate-800">{placements.length}</p>
      </div>
          </div>
        </aside>
      </div>

   {/* Status bar */}
      <div className="flex h-6 flex-shrink-0 items-center gap-4 border-t border-slate-200 bg-slate-50 px-4 text-[11px] text-slate-400">
        <span>{placements.length} placement{placements.length !== 1 ? 's' : ''}</span>
     <span>·</span>
        <span>{zones.length} zone{zones.length !== 1 ? 's' : ''} from layout</span>
    <span>·</span>
        <span className="font-mono">{template?.name}</span>
      </div>
    </div>
  );
}
