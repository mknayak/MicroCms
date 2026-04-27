import { useState, useCallback } from 'react';
import { useParams, Link } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { layoutsApi } from '@/api/layouts';
import { useSite } from '@/contexts/SiteContext';
import type { LayoutZoneNode, LayoutColumnDef } from '@/types';
import { ApiError } from '@/api/client';

// ─── Types ────────────────────────────────────────────────────────────────────

type GridPreset = { label: string; columns: number[] };

const GRID_PRESETS: GridPreset[] = [
  { label: '6-6',  columns: [6, 6] },
  { label: '4-8',  columns: [4, 8] },
  { label: '8-4',  columns: [8, 4] },
  { label: '3-9',  columns: [3, 9] },
  { label: '9-3',  columns: [9, 3] },
  { label: '4-4-4', columns: [4, 4, 4] },
  { label: '3-6-3', columns: [3, 6, 3] },
];

function uid() { return Math.random().toString(36).slice(2); }

function buildColumnZoneName(parentName: string, span: number, idx: number) {
  return `${parentName}-col-${span}-${idx}`;
}

// ─── Zone Node Card ───────────────────────────────────────────────────────────

function ZoneCard({
  zone,
  isSelected,
  onSelect,
  onRemove,
}: {
  zone: LayoutZoneNode;
  isSelected: boolean;
  onSelect: () => void;
  onRemove: () => void;
}) {
  if (zone.type === 'grid-row') {
    const cols = zone.columns ?? [];
    return (
      <div
        onClick={onSelect}
        className={`relative cursor-pointer rounded-lg border-2 transition-all ${isSelected ? 'border-brand-500 bg-brand-50/30' : 'border-slate-200 hover:border-brand-300/60'}`}
 >
        <div className="flex items-center justify-between border-b border-slate-200 px-3 py-2">
     <div className="flex items-center gap-2">
          <span className="flex h-5 w-5 items-center justify-center rounded bg-purple-100">
              <svg className="h-3 w-3 text-purple-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
       <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 12h16M4 18h16" />
              </svg>
</span>
  <span className="text-xs font-semibold text-slate-700">Grid Row</span>
    <span className="rounded bg-purple-100 px-1.5 py-0.5 text-[10px] font-bold text-purple-700">
         {cols.map(c => c.span).join('-')}
       </span>
    </div>
          <button
            onClick={(e) => { e.stopPropagation(); onRemove(); }}
            className="rounded p-0.5 text-slate-300 hover:bg-red-50 hover:text-red-500"
  >
        <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
   </button>
     </div>
 <div className="flex gap-1 p-2">
          {cols.map((col, i) => (
    <div
              key={i}
  className="flex min-h-[40px] flex-1 items-center justify-center rounded border border-dashed border-slate-300 bg-slate-50/80 text-[10px] text-slate-400"
     style={{ flex: col.span }}
    >
  {col.zoneName}
  </div>
     ))}
        </div>
      </div>
    );
  }

  return (
    <div
   onClick={onSelect}
      className={`relative flex cursor-pointer items-center gap-3 rounded-lg border-2 px-3 py-2.5 transition-all ${isSelected ? 'border-brand-500 bg-brand-50/30' : 'border-slate-200 hover:border-brand-300/60'}`}
    >
   <span className="flex h-6 w-6 flex-shrink-0 items-center justify-center rounded bg-brand-100">
        <svg className="h-3 w-3 text-brand-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
     <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 6h16M4 12h16M4 18h7" />
        </svg>
      </span>
      <div className="min-w-0 flex-1">
        <p className="truncate text-xs font-semibold text-slate-800">{zone.label}</p>
    <p className="font-mono text-[10px] text-slate-400">{zone.name}</p>
      </div>
      <button
        onClick={(e) => { e.stopPropagation(); onRemove(); }}
   className="rounded p-0.5 text-slate-300 hover:bg-red-50 hover:text-red-500"
      >
     <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
 <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
        </svg>
      </button>
    </div>
  );
}

// ─── Properties Panel ─────────────────────────────────────────────────────────

function ZonePropertiesPanel({
  zone,
  onUpdateLabel,
  onUpdateGrid,
}: {
  zone: LayoutZoneNode | null;
  onUpdateLabel: (id: string, label: string) => void;
  onUpdateGrid: (id: string, preset: GridPreset) => void;
}) {
  if (!zone) {
    return (
      <aside className="flex w-64 flex-shrink-0 flex-col border-l border-slate-200 bg-white">
<div className="flex flex-1 flex-col items-center justify-center gap-3 p-6 text-center">
          <svg className="h-9 w-9 text-slate-200" fill="none" viewBox="0 0 24 24" stroke="currentColor">
      <rect x="3" y="3" width="18" height="18" rx="2" strokeWidth="1.5" />
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M3 9h18" />
     </svg>
       <p className="text-sm font-semibold text-slate-500">Select a zone to edit properties</p>
        </div>
    </aside>
    );
  }

  return (
    <aside className="flex w-64 flex-shrink-0 flex-col overflow-y-auto border-l border-slate-200 bg-white">
      <div className="border-b border-slate-200 px-4 py-3">
        <p className="text-xs font-bold uppercase tracking-wider text-slate-400">Zone Properties</p>
      </div>
      <div className="space-y-4 px-4 py-4">
        <div>
    <label className="form-label">Label</label>
    <input
   className="form-input mt-1"
      value={zone.label}
            onChange={(e) => onUpdateLabel(zone.id, e.target.value)}
     />
  </div>
        <div>
    <label className="form-label">Machine Name</label>
          <div className="mt-1 rounded-md border border-slate-200 bg-slate-50 px-3 py-2 font-mono text-xs text-slate-600">{zone.name}</div>
          <p className="mt-1 text-[10px] text-slate-400">Used as the token in the shell template. Cannot be changed.</p>
        </div>
        <div>
          <label className="form-label">Type</label>
     <div className={`mt-1 rounded-md px-2 py-1 text-xs font-semibold ${zone.type === 'grid-row' ? 'bg-purple-100 text-purple-700' : 'bg-brand-100 text-brand-700'}`}>
    {zone.type === 'grid-row' ? 'Grid Row' : 'Zone'}
          </div>
        </div>

        {zone.type === 'grid-row' && (
   <div>
     <label className="form-label">Grid Preset</label>
 <div className="mt-2 grid grid-cols-2 gap-1.5">
            {GRID_PRESETS.map((preset) => {
                const current = (zone.columns ?? []).map(c => c.span).join('-');
  const active = current === preset.columns.join('-');
 return (
 <button
                key={preset.label}
            onClick={() => onUpdateGrid(zone.id, preset)}
        className={`rounded-md border px-2 py-1.5 text-xs font-semibold transition-colors ${active ? 'border-brand-500 bg-brand-50 text-brand-700' : 'border-slate-200 hover:border-brand-300'}`}
       >
         {preset.label}
     </button>
     );
     })}
        </div>
          </div>
   )}

        {zone.type === 'grid-row' && zone.columns && (
          <div>
     <label className="form-label">Column Zones</label>
            <div className="mt-2 space-y-1">
       {zone.columns.map((col, i) => (
         <div key={i} className="flex items-center gap-2 rounded border border-slate-200 px-2 py-1.5">
        <span className="rounded bg-purple-100 px-1.5 py-0.5 text-[10px] font-bold text-purple-700">{col.span}/12</span>
      <span className="font-mono text-[10px] text-slate-600">{col.zoneName}</span>
      </div>
     ))}
       </div>
    </div>
        )}
      </div>
    </aside>
  );
}

// ─── Main Page ────────────────────────────────────────────────────────────────

export default function LayoutDesignerPage() {
  const { id } = useParams<{ id: string }>();
  const qc = useQueryClient();
  const { selectedSiteId } = useSite();

  const [zones, setZones] = useState<LayoutZoneNode[]>([]);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [dirty, setDirty] = useState(false);
  const [zonesInitialised, setZonesInitialised] = useState(false);

  const { data: layout, isLoading } = useQuery({
 queryKey: ['layout', id],
    queryFn: () => layoutsApi.get(id!),
    enabled: !!id,
    staleTime: 0,
  });

  // Initialise zones from loaded layout (once)
  if (layout && !zonesInitialised) {
    setZones(layout.zones.length > 0 ? [...layout.zones] : []);
    setZonesInitialised(true);
  }

  const saveZonesMutation = useMutation({
    mutationFn: () => layoutsApi.updateZones(id!, { zones }),
    onSuccess: () => {
    toast.success('Layout zones saved.');
      setDirty(false);
      void qc.invalidateQueries({ queryKey: ['layout', id] });
      void qc.invalidateQueries({ queryKey: ['layouts', selectedSiteId] });
    },
  onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Save failed.'),
  });

  const mutateZones = useCallback((updater: (prev: LayoutZoneNode[]) => LayoutZoneNode[]) => {
    setZones(updater);
    setDirty(true);
  }, []);

  const addZone = () => {
    const name = `zone-${uid().slice(0, 6)}`;
 mutateZones(prev => [...prev, {
    id: uid(), type: 'zone', name, label: 'New Zone', sortOrder: prev.length,
    }]);
  };

  const addGridRow = (preset: GridPreset) => {
    const parentName = `grid-${uid().slice(0, 6)}`;
    const columns: LayoutColumnDef[] = preset.columns.map((span, i) => ({
      span,
      zoneName: buildColumnZoneName(parentName, span, i),
    }));
    mutateZones(prev => [...prev, {
      id: uid(), type: 'grid-row', name: parentName, label: `Grid ${preset.label}`,
      sortOrder: prev.length, columns,
    }]);
  };

  const removeZone = (zoneId: string) => {
    mutateZones(prev => prev.filter(z => z.id !== zoneId).map((z, i) => ({ ...z, sortOrder: i })));
    setSelectedId(cur => cur === zoneId ? null : cur);
  };

  const moveZone = (zoneId: string, dir: -1 | 1) => {
    mutateZones(prev => {
      const idx = prev.findIndex(z => z.id === zoneId);
      if (idx < 0) return prev;
      const newIdx = idx + dir;
      if (newIdx < 0 || newIdx >= prev.length) return prev;
      const next = [...prev];
      [next[idx], next[newIdx]] = [next[newIdx], next[idx]];
  return next.map((z, i) => ({ ...z, sortOrder: i }));
    });
  };

  const updateLabel = (zoneId: string, label: string) => {
    mutateZones(prev => prev.map(z => z.id === zoneId ? { ...z, label } : z));
  };

  const updateGrid = (zoneId: string, preset: GridPreset) => {
    mutateZones(prev => prev.map(z => {
      if (z.id !== zoneId) return z;
  const columns: LayoutColumnDef[] = preset.columns.map((span, i) => ({
        span,
        zoneName: buildColumnZoneName(z.name, span, i),
      }));
      return { ...z, columns };
    }));
  };

  const selectedZone = zones.find(z => z.id === selectedId) ?? null;

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
          <p className="font-mono text-xs text-slate-400">{zones.length} zone{zones.length !== 1 ? 's' : ''} defined</p>
        </div>
        <span className={`flex items-center gap-1.5 text-xs ${dirty ? 'text-amber-600' : 'text-slate-400'}`}>
          <span className={`h-1.5 w-1.5 rounded-full ${dirty ? 'bg-amber-400' : 'bg-green-400'}`} />
          {dirty ? 'Unsaved changes' : 'Saved'}
        </span>
        <button
          onClick={() => saveZonesMutation.mutate()}
          disabled={saveZonesMutation.isPending || !dirty}
          className="btn-primary h-8 px-3 py-0 text-xs disabled:opacity-50"
      >
      {saveZonesMutation.isPending ? 'Saving…' : 'Save Zones'}
        </button>
      </header>

      {/* Body */}
      <div className="flex min-h-0 flex-1 overflow-hidden">

 {/* LEFT — toolbox */}
      <aside className="flex w-56 flex-shrink-0 flex-col overflow-y-auto border-r border-slate-200 bg-white">
    <div className="border-b border-slate-200 px-3 py-3">
            <p className="mb-3 text-[10px] font-bold uppercase tracking-wider text-slate-400">Add to Layout</p>
 <button
         onClick={addZone}
        className="flex w-full items-center gap-2 rounded-lg border border-brand-200 bg-brand-50 px-3 py-2 text-xs font-semibold text-brand-700 hover:bg-brand-100"
>
     <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
     <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
   </svg>
Add Zone
    </button>
     </div>

          <div className="px-3 py-3">
      <p className="mb-2 text-[10px] font-bold uppercase tracking-wider text-slate-400">Grid Presets</p>
       <div className="space-y-1.5">
 {GRID_PRESETS.map((preset) => (
             <button
         key={preset.label}
onClick={() => addGridRow(preset)}
      className="flex w-full items-center gap-2 rounded-lg border border-slate-200 px-3 py-2 text-xs transition-colors hover:border-purple-300 hover:bg-purple-50"
            >
           <div className="flex flex-1 gap-0.5">
       {preset.columns.map((span, i) => (
    <div
         key={i}
               className="h-4 rounded-sm bg-purple-200"
             style={{ flex: span }}
               />
           ))}
           </div>
<span className="font-semibold text-slate-600">{preset.label}</span>
      </button>
    ))}
     </div>
  </div>

          {/* Shell preview */}
          {layout?.shellTemplate && (
            <div className="border-t border-slate-200 px-3 py-3">
           <p className="mb-2 text-[10px] font-bold uppercase tracking-wider text-slate-400">Generated Shell</p>
    <pre className="max-h-48 overflow-auto rounded-md border border-slate-200 bg-slate-50 p-2 text-[9px] text-slate-500">
      {layout.shellTemplate}
  </pre>
    </div>
          )}
        </aside>

{/* CENTER — canvas */}
  <div className="flex min-w-0 flex-1 flex-col overflow-auto bg-slate-50 p-6">
          <div className="mx-auto w-full max-w-3xl">
 {/* Ruler */}
  <div className="mb-4 flex items-center gap-2 rounded-lg border border-slate-200 bg-white px-4 py-2 text-xs text-slate-500">
        <svg className="h-4 w-4 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
             <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M9 17V7m0 10a2 2 0 01-2 2H5a2 2 0 01-2-2V7a2 2 0 012-2h2a2 2 0 012 2m0 10a2 2 0 002 2h2a2 2 0 002-2M9 7a2 2 0 012-2h2a2 2 0 012 2m0 10V7" />
        </svg>
 Layout Structure — drag components in Page Designer to fill zones
          </div>

            {zones.length === 0 && (
              <div className="flex flex-col items-center justify-center rounded-xl border-2 border-dashed border-slate-300 bg-white py-16 text-center">
  <svg className="mb-3 h-10 w-10 text-slate-200" fill="none" viewBox="0 0 24 24" stroke="currentColor">
      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M4 6h16M4 12h16M4 18h16" />
     </svg>
           <p className="text-sm font-semibold text-slate-400">No zones defined</p>
      <p className="mt-1 text-xs text-slate-300">Add a Zone or Grid from the left panel</p>
      </div>
    )}

         <div className="space-y-2">
          {zones.sort((a, b) => a.sortOrder - b.sortOrder).map((zone, idx) => (
     <div key={zone.id} className="group flex items-start gap-2">
 {/* Reorder buttons */}
           <div className="flex flex-col gap-0.5 pt-1 opacity-0 transition-opacity group-hover:opacity-100">
    <button
 onClick={() => moveZone(zone.id, -1)}
     disabled={idx === 0}
   className="rounded border border-slate-200 bg-white p-0.5 text-slate-400 hover:text-slate-600 disabled:opacity-30"
          >
     <svg className="h-3 w-3" fill="none" viewBox="0 0 24 24" stroke="currentColor">
           <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M5 15l7-7 7 7" />
   </svg>
  </button>
    <button
      onClick={() => moveZone(zone.id, 1)}
          disabled={idx === zones.length - 1}
      className="rounded border border-slate-200 bg-white p-0.5 text-slate-400 hover:text-slate-600 disabled:opacity-30"
        >
       <svg className="h-3 w-3" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                 <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M19 9l-7 7-7-7" />
              </svg>
             </button>
         </div>
         <div className="flex-1">
          <ZoneCard
               zone={zone}
        isSelected={selectedId === zone.id}
           onSelect={() => setSelectedId(zone.id === selectedId ? null : zone.id)}
      onRemove={() => removeZone(zone.id)}
      />
           </div>
       </div>
        ))}
   </div>
 </div>
    </div>

        {/* RIGHT — properties */}
        <ZonePropertiesPanel
          zone={selectedZone}
  onUpdateLabel={updateLabel}
          onUpdateGrid={updateGrid}
      />
      </div>

      {/* Status bar */}
      <div className="flex h-6 flex-shrink-0 items-center gap-4 border-t border-slate-200 bg-slate-50 px-4 text-[11px] text-slate-400">
        <span>{zones.length} zone{zones.length !== 1 ? 's' : ''}</span>
        <span>·</span>
  <span>{zones.filter(z => z.type === 'grid-row').length} grid row{zones.filter(z => z.type === 'grid-row').length !== 1 ? 's' : ''}</span>
        <span>·</span>
    <span className="font-mono">{layout?.key}</span>
 </div>
    </div>
  );
}
