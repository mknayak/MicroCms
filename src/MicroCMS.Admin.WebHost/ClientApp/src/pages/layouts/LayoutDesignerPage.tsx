import { useState, useCallback } from 'react';
import { useParams, Link } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { layoutsApi } from '@/api/layouts';
import { useSite } from '@/contexts/SiteContext';
import type {
  LayoutZoneNode,
  LayoutColumnDef,
  LayoutConfig,
  LayoutAsset,
  LayoutBodyAttribute,
  LayoutAssetType,
  LayoutAssetPosition,
} from '@/types';import { ApiError } from '@/api/client';

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

// ─── Config Tab — helpers ─────────────────────────────────────────────────────

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
  return {
    id: Math.random().toString(36).slice(2),
    order: 10,
    type: 'raw-html',
    position: 'head',
    content: '',
    nonce: false,
    attributes: {},
  };
}

function emptyBodyAttr(): LayoutBodyAttribute {
  return { attribute: '', value: '' };
}

// ─── Asset Row ────────────────────────────────────────────────────────────────

function AssetRow({
  asset,
  index,
  total,
  onChange,
  onRemove,
  onMove,
}: {
  asset: LayoutAsset;
  index: number;
  total: number;
  onChange: (a: LayoutAsset) => void;
  onRemove: () => void;
  onMove: (dir: -1 | 1) => void;
}) {
  const set = <K extends keyof LayoutAsset>(k: K, v: LayoutAsset[K]) => onChange({ ...asset, [k]: v });

  const placeholder =
    asset.type === 'raw-html'
      ? '<link href="https://cdn.jsdelivr.net/npm/bootstrap@5/dist/css/bootstrap.min.css" rel="stylesheet" integrity="sha384-..." crossorigin="anonymous">'
      : asset.type === 'inline-css'
      ? 'body { margin: 0; }'
      : 'console.log("hello");';

  return (
    <div className="rounded-lg border border-slate-200 bg-white p-3">
      <div className="mb-2 flex items-center gap-2">
        {/* reorder */}
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

        {/* type */}
        <select value={asset.type} onChange={(e) => set('type', e.target.value as LayoutAssetType)}
          className="form-input h-7 flex-1 py-0 text-xs">
          {ASSET_TYPE_OPTIONS.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
        </select>

        {/* position */}
        <select value={asset.position} onChange={(e) => set('position', e.target.value as LayoutAssetPosition)}
          className="form-input h-7 w-28 py-0 text-xs">
          {ASSET_POSITION_OPTIONS.map(o => <option key={o.value} value={o.value}>{o.label}</option>)}
        </select>

        {/* order */}
        <input type="number" value={asset.order} onChange={(e) => set('order', Number(e.target.value))}
          className="form-input h-7 w-16 py-0 text-xs" placeholder="Order" />

        <button onClick={onRemove}
          className="ml-auto rounded p-0.5 text-slate-300 hover:bg-red-50 hover:text-red-500">
          <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
          </svg>
        </button>
      </div>

      <textarea
        value={asset.content ?? ''}
        onChange={(e) => set('content', e.target.value)}
        rows={asset.type === 'raw-html' ? 3 : 5}
        className="form-input mt-1 w-full font-mono text-xs"
        placeholder={placeholder}
      />

      {asset.type === 'inline-js' && (
        <div className="mt-2 flex items-center gap-3 text-xs text-slate-600">
          <label className="flex cursor-pointer items-center gap-1.5">
            <input type="checkbox" checked={asset.nonce} onChange={(e) => set('nonce', e.target.checked)} className="rounded" />
            nonce <span className="text-slate-400">(Sprint 19)</span>
          </label>
        </div>
      )}
    </div>
  );
}

// ─── Config Panel ─────────────────────────────────────────────────────────────

function LayoutConfigPanel({
  config,
  onChange,
}: {
  config: LayoutConfig;
  onChange: (c: LayoutConfig) => void;
}) {
  const setAssets = (assets: LayoutAsset[]) => onChange({ ...config, assets });
  const setAttrs  = (bodyAttributes: LayoutBodyAttribute[]) => onChange({ ...config, bodyAttributes });

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

        {/* Assets section */}
        <section>
          <div className="mb-3 flex items-center justify-between">
            <div>
              <p className="text-sm font-bold text-slate-800">Assets</p>
              <p className="text-xs text-slate-400">Stylesheets, scripts, and snippets injected into the shell template.</p>
            </div>
            <button onClick={addAsset} className="btn-primary h-8 px-3 text-xs">
              + Add Asset
            </button>
          </div>

          {config.assets.length === 0 ? (
            <div className="flex flex-col items-center justify-center rounded-xl border-2 border-dashed border-slate-300 bg-white py-10 text-center">
              <p className="text-sm font-semibold text-slate-400">No assets defined</p>
              <p className="mt-1 text-xs text-slate-300">Add a CSS link, JS script, or inline snippet</p>
            </div>
          ) : (
            <div className="space-y-2">
              {config.assets.map((asset, i) => (
                <AssetRow
                  key={asset.id}
                  asset={asset}
                  index={i}
                  total={config.assets.length}
                  onChange={(a) => updateAsset(i, a)}
                  onRemove={() => removeAsset(i)}
                  onMove={(dir) => moveAsset(i, dir)}
                />
              ))}
            </div>
          )}
        </section>

        {/* Body Attributes section */}
        <section>
          <div className="mb-3 flex items-center justify-between">
            <div>
              <p className="text-sm font-bold text-slate-800">Body Attributes</p>
              <p className="text-xs text-slate-400">
                Extra attributes on the <code className="rounded bg-slate-100 px-1 text-[11px]">&lt;body&gt;</code> tag.
                Token placeholders like <code className="rounded bg-slate-100 px-1 text-[11px]">{'{{page:slug}}'}</code> are resolved at render time.
              </p>
            </div>
            <button onClick={addAttr} className="btn-primary h-8 px-3 text-xs">
              + Add Attribute
            </button>
          </div>

          {config.bodyAttributes.length === 0 ? (
            <div className="flex flex-col items-center justify-center rounded-xl border-2 border-dashed border-slate-300 bg-white py-10 text-center">
              <p className="text-sm font-semibold text-slate-400">No body attributes defined</p>
              <p className="mt-1 text-xs text-slate-300">
                e.g. <code className="rounded bg-slate-100 px-1 text-[11px]">id = {'{{page:slug}}'}</code>
              </p>
            </div>
          ) : (
            <div className="space-y-2">
              {config.bodyAttributes.map((attr, i) => (
                <div key={i} className="flex items-center gap-2 rounded-lg border border-slate-200 bg-white p-3">
                  <input
                    value={attr.attribute}
                    onChange={(e) => updateAttr(i, { attribute: e.target.value })}
                    className="form-input h-7 w-36 py-0 text-xs"
                    placeholder="attribute"
                  />
                  <span className="text-xs text-slate-400">=</span>
                  <input
                    value={attr.value}
                    onChange={(e) => updateAttr(i, { value: e.target.value })}
                    className="form-input h-7 flex-1 py-0 font-mono text-xs"
                    placeholder="value or {{page:slug}}"
                  />
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

        {/* Token reference */}
        <section className="rounded-lg border border-slate-200 bg-white p-4">
          <p className="mb-2 text-xs font-bold uppercase tracking-wider text-slate-400">Token Namespaces (design-time reference)</p>
          <div className="grid grid-cols-2 gap-x-6 gap-y-1 text-xs text-slate-600">
            {[
              ['{{page:*}}',     'Current page fields (slug, title, …)'],
              ['{{template:*}}', 'Template fields (key, name, …)'],
              ['{{site:*}}',     'Site settings (name, domain, …)'],
              ['{{user:*}}',     'Current user fields (id, email, …)'],
              ['{{data:*}}',     'Component / data-source fields'],
            ].map(([token, desc]) => (
              <div key={token} className="flex items-start gap-2">
                <code className="mt-0.5 rounded bg-slate-100 px-1.5 py-0.5 text-[11px] font-mono text-slate-700 whitespace-nowrap">{token}</code>
                <span className="text-slate-500">{desc}</span>
              </div>
            ))}
          </div>
          <p className="mt-3 text-[11px] text-slate-400">Tokens are stored verbatim and resolved during rendering (Sprint 19).</p>
        </section>

      </div>
    </div>
  );
}

// ─── Main Page ────────────────────────────────────────────────────────────────

export default function LayoutDesignerPage() {
  const { id } = useParams<{ id: string }>();
  const qc = useQueryClient();
  const { selectedSiteId } = useSite();

  const [activeTab, setActiveTab] = useState<'structure' | 'configuration' | 'shell'>('structure');

  // ── Zone state ──
  const [zones, setZones] = useState<LayoutZoneNode[]>([]);
  const [selectedId, setSelectedId] = useState<string | null>(null);
  const [zonesDirty, setZonesDirty] = useState(false);
  const [zonesInitialised, setZonesInitialised] = useState(false);

  // ── Config state ──
  const [config, setConfig] = useState<LayoutConfig>({ assets: [], bodyAttributes: [] });
  const [configDirty, setConfigDirty] = useState(false);
  const [configInitialised, setConfigInitialised] = useState(false);

  // ── Shell edit state ──
  const [shellText, setShellText] = useState('');
  const [shellDirty, setShellDirty] = useState(false);
  const [shellEditMode, setShellEditMode] = useState(false);
  const [shellInitialised, setShellInitialised] = useState(false);

  const dirty = activeTab === 'structure' ? zonesDirty : activeTab === 'configuration' ? configDirty : shellDirty;

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

  // Initialise config from loaded layout (once)
  if (layout && !configInitialised) {
    setConfig(layout.config ?? { assets: [], bodyAttributes: [] });
    setConfigInitialised(true);
  }

  // Initialise shell text from loaded layout (once)
  if (layout && !shellInitialised) {
    setShellText(layout.shellTemplate ?? '');
    setShellInitialised(true);
  }

  const saveZonesMutation = useMutation({
    mutationFn: () => layoutsApi.updateZones(id!, { zones }),
    onSuccess: (data) => {
      toast.success('Layout zones saved.');
      setZonesDirty(false);
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
      // Sync the shell text with what the server returned
      setShellText(data.shellTemplate ?? '');
      void qc.invalidateQueries({ queryKey: ['layout', id] });
      void qc.invalidateQueries({ queryKey: ['layouts', selectedSiteId] });
    },
    onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Save failed.'),
  });

  const handleSave = () => {
    if (activeTab === 'structure') {
      if (layout?.isShellCustomized &&
        !window.confirm('This layout has a hand-edited shell. Saving zones will overwrite it with a regenerated shell. Continue?'))
        return;
      saveZonesMutation.mutate();
    } else if (activeTab === 'configuration') {
      if (layout?.isShellCustomized &&
        !window.confirm('This layout has a hand-edited shell. Saving configuration will overwrite it with a regenerated shell. Continue?'))
        return;
      saveConfigMutation.mutate();
    } else if (activeTab === 'shell') {
      saveShellMutation.mutate();
    }
  };

  const isSaving = saveZonesMutation.isPending || saveConfigMutation.isPending || saveShellMutation.isPending;

  const mutateZones = useCallback((updater: (prev: LayoutZoneNode[]) => LayoutZoneNode[]) => {
    setZones(updater);
    setZonesDirty(true);
  }, []);

  const mutateConfig = (next: LayoutConfig) => {
    setConfig(next);
    setConfigDirty(true);
  };

  const addZone = () => {
    const name = `zone-${Math.random().toString(36).slice(2, 8)}`;
    mutateZones(prev => [...prev, {
      id: Math.random().toString(36).slice(2), type: 'zone', name, label: 'New Zone', sortOrder: prev.length,
    }]);
  };

  const addGridRow = (preset: GridPreset) => {
    const parentName = `grid-${Math.random().toString(36).slice(2, 8)}`;
    const columns: LayoutColumnDef[] = preset.columns.map((span, i) => ({
      span,
      zoneName: buildColumnZoneName(parentName, span, i),
    }));
    mutateZones(prev => [...prev, {
      id: Math.random().toString(36).slice(2), type: 'grid-row', name: parentName, label: `Grid ${preset.label}`,
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

  const saveLabel = isSaving ? 'Saving…' :
    activeTab === 'structure' ? 'Save Zones' :
    activeTab === 'configuration' ? 'Save Config' :
    'Save Shell';
  const saveVisible = activeTab !== 'shell' || shellEditMode;

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
        {saveVisible && (
          <button
            onClick={handleSave}
            disabled={isSaving || !dirty}
            className="btn-primary h-8 px-3 py-0 text-xs disabled:opacity-50"
          >
            {saveLabel}
          </button>
        )}
      </header>

      {/* Tabs */}
      <div className="flex flex-shrink-0 border-b border-slate-200 bg-white px-4">
        {(['structure', 'configuration', 'shell'] as const).map((tab) => {
          const label =
            tab === 'structure' ? 'Layout Structure' :
            tab === 'configuration' ? 'Layout Configuration' :
            'Generated Shell';
          const hasDot =
            (tab === 'structure' && zonesDirty) ||
            (tab === 'configuration' && configDirty) ||
            (tab === 'shell' && shellDirty);
          return (
            <button
              key={tab}
              onClick={() => setActiveTab(tab)}
              className={`mr-1 border-b-2 px-4 py-2.5 text-xs font-semibold transition-colors ${
                activeTab === tab
                  ? 'border-brand-500 text-brand-700'
                  : 'border-transparent text-slate-500 hover:text-slate-700'
              }`}
            >
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
                        <div key={i} className="h-4 rounded-sm bg-purple-200" style={{ flex: span }} />
                      ))}
                    </div>
                    <span className="font-semibold text-slate-600">{preset.label}</span>
                  </button>
                ))}
              </div>
            </div>

            </aside>

          {/* CENTER — canvas */}
          <div className="flex min-w-0 flex-1 flex-col overflow-auto bg-slate-50 p-6">
            <div className="mx-auto w-full max-w-3xl">
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
      ) : activeTab === 'configuration' ? (
        <LayoutConfigPanel config={config} onChange={mutateConfig} />
      ) : (
        /* ── Generated Shell tab ── */
        <div className="flex min-h-0 flex-1 overflow-auto bg-slate-50 p-6">
          <div className="mx-auto w-full max-w-4xl space-y-4">

            {/* Header row */}
            <div className="flex items-start justify-between gap-4">
              <div>
                <p className="text-sm font-bold text-slate-800">Generated Shell</p>
                <p className="text-xs text-slate-400">
                  {shellEditMode
                    ? 'Advanced mode — editing directly. Saving zones or configuration will overwrite this.'
                    : 'Regenerated automatically whenever zones or configuration are saved.'}
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
                }`}
              >
                {shellEditMode ? '✕ Exit Advanced Mode' : '⚙ Advanced Mode'}
              </button>
            </div>

            {/* Customized warning banner */}
            {layout?.isShellCustomized && !shellEditMode && (
              <div className="flex items-start gap-3 rounded-lg border border-amber-200 bg-amber-50 px-4 py-3">
                <svg className="mt-0.5 h-4 w-4 flex-shrink-0 text-amber-500" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                  <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 9v2m0 4h.01M10.29 3.86L1.82 18a2 2 0 001.71 3h16.94a2 2 0 001.71-3L13.71 3.86a2 2 0 00-3.42 0z" />
                </svg>
                <p className="text-xs text-amber-700">
                  <strong>Advanced mode shell active.</strong> This shell was hand-edited and is not auto-generated.
                  Saving zones or configuration will overwrite your custom shell.
                </p>
              </div>
            )}

            {/* Editor / viewer */}
            {shellEditMode ? (
              <textarea
                value={shellText}
                onChange={(e) => { setShellText(e.target.value); setShellDirty(true); }}
                className="w-full rounded-xl border border-brand-300 bg-white p-4 font-mono text-xs leading-relaxed text-slate-800 shadow-sm outline-none focus:ring-2 focus:ring-brand-400"
                rows={30}
                spellCheck={false}
              />
            ) : layout?.shellTemplate ? (
              <pre className="overflow-auto rounded-xl border border-slate-200 bg-white p-4 font-mono text-xs leading-relaxed text-slate-700 shadow-sm">
                {layout.shellTemplate}
              </pre>
            ) : (
              <div className="flex flex-col items-center justify-center rounded-xl border-2 border-dashed border-slate-300 bg-white py-16 text-center">
                <p className="text-sm font-semibold text-slate-400">No shell generated yet</p>
                <p className="mt-1 text-xs text-slate-300">Save zones or configuration to generate the shell.</p>
              </div>
            )}

          </div>
        </div>
      )}

      {/* Status bar */}
      <div className="flex h-6 flex-shrink-0 items-center gap-4 border-t border-slate-200 bg-slate-50 px-4 text-[11px] text-slate-400">
        <span>{zones.length} zone{zones.length !== 1 ? 's' : ''}</span>
        <span>·</span>
        <span>{zones.filter(z => z.type === 'grid-row').length} grid row{zones.filter(z => z.type === 'grid-row').length !== 1 ? 's' : ''}</span>
        <span>·</span>
        <span>{config.assets.length} asset{config.assets.length !== 1 ? 's' : ''}</span>
        <span>·</span>
        <span className="font-mono">{layout?.key}</span>
      </div>
    </div>
  );
}
