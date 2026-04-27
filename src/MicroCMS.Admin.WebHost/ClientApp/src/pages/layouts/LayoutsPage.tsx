import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import toast from 'react-hot-toast';
import { layoutsApi } from '@/api/layouts';
import { useSite } from '@/contexts/SiteContext';
import type { LayoutDto, LayoutListItem, LayoutTemplateType } from '@/types';
import { ApiError } from '@/api/client';

// ─── Schema ───────────────────────────────────────────────────────────────────

const layoutSchema = z.object({
  name: z.string().min(1, 'Name is required').max(200),
  key: z.string().min(1, 'Key is required').max(100)
    .regex(/^[a-z0-9]+(?:-[a-z0-9]+)*$/, 'Lowercase letters, numbers and hyphens only'),
  templateType: z.enum(['Handlebars', 'Html'] as const),
});

type LayoutForm = z.infer<typeof layoutSchema>;

const TEMPLATE_TYPE_BADGE: Record<LayoutTemplateType, string> = {
  Handlebars: 'bg-amber-100 text-amber-800',
  Html: 'bg-slate-100 text-slate-700',
};

// ─── Editor panel (create / rename only) ─────────────────────────────────────

function LayoutEditor({ layout, siteId, onClose }: {
  layout: LayoutDto | null;
  siteId: string;
  onClose: () => void;
}) {
  const qc = useQueryClient();

  const form = useForm<LayoutForm>({
    resolver: zodResolver(layoutSchema),
    defaultValues: layout
      ? { name: layout.name, key: layout.key, templateType: layout.templateType }
      : { name: '', key: '', templateType: 'Handlebars' },
  });

  const templateType = form.watch('templateType');

  const createMutation = useMutation({
    mutationFn: (data: LayoutForm) => layoutsApi.create({ siteId, ...data }),
    onSuccess: () => { toast.success('Layout created.'); void qc.invalidateQueries({ queryKey: ['layouts', siteId] }); onClose(); },
    onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Create failed.'),
  });

  const updateMutation = useMutation({
    mutationFn: (data: LayoutForm) => layoutsApi.update(layout!.id, data),
    onSuccess: () => { toast.success('Layout saved.'); void qc.invalidateQueries({ queryKey: ['layouts', siteId] }); onClose(); },
    onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Save failed.'),
  });

  const isPending = createMutation.isPending || updateMutation.isPending;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/30">
      <div className="w-full max-w-md rounded-xl bg-white shadow-xl">
<div className="flex items-center justify-between border-b border-slate-200 px-6 py-4">
          <h2 className="text-base font-semibold text-slate-900">{layout ? `Edit: ${layout.name}` : 'New Layout'}</h2>
          <button onClick={onClose} className="rounded p-1 text-slate-400 hover:bg-slate-100">
      <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
     <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
          </button>
        </div>
      <form onSubmit={form.handleSubmit((d) => layout ? updateMutation.mutate(d) : createMutation.mutate(d))} className="space-y-4 px-6 py-5">
          <div>
            <label className="form-label">Name</label>
      <input className="form-input mt-1" {...form.register('name')} placeholder="Blog Layout" />
        {form.formState.errors.name && <p className="form-error">{form.formState.errors.name.message}</p>}
          </div>
          <div>
            <label className="form-label">Key</label>
 <input className="form-input mt-1 font-mono" {...form.register('key')} placeholder="blog-layout" disabled={!!layout} />
      {form.formState.errors.key && <p className="form-error">{form.formState.errors.key.message}</p>}
            {!layout && <p className="mt-1 text-xs text-slate-400">Cannot be changed after creation.</p>}
          </div>
          <div>
   <label className="form-label">Template Engine</label>
            <div className="mt-2 grid grid-cols-2 gap-2">
              {(['Handlebars', 'Html'] as LayoutTemplateType[]).map((t) => (
           <label key={t} className={`flex cursor-pointer flex-col rounded-lg border-2 p-3 transition-colors ${templateType === t ? 'border-brand-500 bg-brand-50' : 'border-slate-200 hover:border-slate-300'}`}>
 <input type="radio" value={t} {...form.register('templateType')} className="sr-only" />
                  <span className="text-sm font-semibold text-slate-800">{t}</span>
      <span className="mt-0.5 text-xs text-slate-500">{t === 'Handlebars' ? 'Recommended — full logic support' : 'Simple token replacement'}</span>
  </label>
           ))}
          </div>
      <p className="mt-2 text-xs text-slate-400">
            Zone structure is designed visually in the <strong>Layout Designer</strong> after creation. The HTML shell is auto-generated.
            </p>
          </div>
          <div className="flex justify-end gap-3 pt-2">
            <button type="button" onClick={onClose} className="btn-secondary">Cancel</button>
       <button type="submit" disabled={isPending} className="btn-primary">
    {isPending ? 'Saving…' : layout ? 'Save Changes' : 'Create Layout'}
        </button>
     </div>
        </form>
      </div>
    </div>
  );
}

// ─── Layout row ───────────────────────────────────────────────────────────────

function LayoutRow({ layout, siteId, onEdit }: { layout: LayoutListItem; siteId: string; onEdit: (id: string) => void }) {
  const qc = useQueryClient();
  const navigate = useNavigate();

  const setDefaultMutation = useMutation({
    mutationFn: () => layoutsApi.setDefault(layout.id, siteId),
    onSuccess: () => { toast.success(`"${layout.name}" is now the default layout.`); void qc.invalidateQueries({ queryKey: ['layouts', siteId] }); },
    onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Failed.'),
  });

  const deleteMutation = useMutation({
    mutationFn: () => layoutsApi.delete(layout.id),
    onSuccess: () => { toast.success('Layout deleted.'); void qc.invalidateQueries({ queryKey: ['layouts', siteId] }); },
    onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Delete failed.'),
  });

  return (
<tr className="hover:bg-slate-50">
      <td className="py-3 pl-4 pr-3">
      <div className="flex items-center gap-2">
          <span className="text-sm font-medium text-slate-900">{layout.name}</span>
          {layout.isDefault && <span className="rounded-full bg-green-100 px-2 py-0.5 text-[10px] font-semibold text-green-700">Default</span>}
          {'zoneCount' in layout && (layout as { zoneCount: number }).zoneCount > 0 && (
            <span className="rounded-full bg-purple-100 px-2 py-0.5 text-[10px] font-semibold text-purple-700">
          {(layout as { zoneCount: number }).zoneCount} zones
    </span>
          )}
        </div>
        <p className="font-mono text-xs text-slate-400">{layout.key}</p>
      </td>
      <td className="px-3 py-3">
        <span className={`rounded px-2 py-0.5 text-xs font-medium ${TEMPLATE_TYPE_BADGE[layout.templateType]}`}>{layout.templateType}</span>
      </td>
      <td className="px-3 py-3 text-xs text-slate-500">{new Date(layout.updatedAt).toLocaleDateString()}</td>
    <td className="py-3 pl-3 pr-4">
        <div className="flex items-center justify-end gap-2">
        {/* Design Layout — the primary design action */}
       <button
            onClick={() => navigate(`/layouts/${layout.id}/designer`)}
 className="flex items-center gap-1 rounded-md border border-purple-200 bg-purple-50 px-2 py-1 text-xs font-semibold text-purple-700 hover:bg-purple-100"
          >
 <svg className="h-3 w-3" fill="none" viewBox="0 0 24 24" stroke="currentColor">
       <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M4 5a1 1 0 011-1h4a1 1 0 011 1v4a1 1 0 01-1 1H5a1 1 0 01-1-1V5zm10 0a1 1 0 011-1h4a1 1 0 011 1v4a1 1 0 01-1 1h-4a1 1 0 01-1-1V5zM4 15a1 1 0 011-1h4a1 1 0 011 1v4a1 1 0 01-1 1H5a1 1 0 01-1-1v-4zm10 0a1 1 0 011-1h4a1 1 0 011 1v4a1 1 0 01-1 1h-4a1 1 0 01-1-1v-4z" />
     </svg>
          Design
      </button>
      {!layout.isDefault && (
         <button onClick={() => setDefaultMutation.mutate()} disabled={setDefaultMutation.isPending} className="text-xs text-brand-600 hover:underline disabled:opacity-50">
     Set default
          </button>
    )}
      <button onClick={() => onEdit(layout.id)} className="text-xs text-slate-600 hover:underline">Edit</button>
  <button
      onClick={() => { if (confirm(`Delete "${layout.name}"?`)) deleteMutation.mutate(); }}
   disabled={deleteMutation.isPending}
          className="text-xs text-red-400 hover:text-red-600 disabled:opacity-50"
          >Delete</button>
     </div>
      </td>
    </tr>
  );
}

// ─── Page ─────────────────────────────────────────────────────────────────────

export default function LayoutsPage() {
  const { selectedSiteId, selectedSite, isLoading: siteLoading } = useSite();
  const siteId = selectedSiteId ?? '';
  const [editorOpen, setEditorOpen] = useState(false);
  const [editorLayout, setEditorLayout] = useState<LayoutDto | null>(null);

  const { data: layouts, isLoading } = useQuery({
    queryKey: ['layouts', siteId],
    queryFn: () => layoutsApi.list(siteId),
    enabled: !!siteId,
  });

  const openNew = () => { setEditorLayout(null); setEditorOpen(true); };
  const openEdit = async (id: string) => {
    try { const l = await layoutsApi.get(id); setEditorLayout(l); setEditorOpen(true); }
    catch { toast.error('Could not load layout.'); }
  };

  if (siteLoading) return <div className="space-y-3">{Array.from({ length: 4 }).map((_, i) => <div key={i} className="h-12 animate-pulse rounded-lg bg-slate-100" />)}</div>;

  if (!siteId) return (
    <div className="flex flex-col items-center justify-center gap-3 py-24 text-center">
      <p className="text-sm font-medium text-slate-500">No site selected.</p>
    </div>
  );

  return (
    <>
   {editorOpen && <LayoutEditor layout={editorLayout} siteId={siteId} onClose={() => setEditorOpen(false)} />}
      <div className="space-y-6">
        <div className="flex items-center justify-between">
          <div>
   <h1 className="text-2xl font-bold text-slate-900">Layouts</h1>
 <p className="mt-1 text-sm text-slate-500">
     Master HTML shells for <span className="font-medium text-slate-700">{selectedSite?.name}</span>. Zones are defined in the <strong>Layout Designer</strong>.
      </p>
      </div>
<button onClick={openNew} className="btn-primary">
            <svg className="mr-2 h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
       <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
  </svg>
            New Layout
 </button>
        </div>

    <div className="card overflow-hidden p-0">
  {isLoading ? (
 <div className="space-y-2 p-4">{Array.from({ length: 3 }).map((_, i) => <div key={i} className="h-10 animate-pulse rounded bg-slate-100" />)}</div>
          ) : (layouts ?? []).length === 0 ? (
            <div className="flex flex-col items-center gap-3 py-16 text-center">
            <p className="text-sm text-slate-500">No layouts yet.</p>
  <button onClick={openNew} className="btn-secondary text-xs">Create your first layout</button>
            </div>
          ) : (
            <table className="w-full text-left">
       <thead className="border-b border-slate-200 bg-slate-50 text-xs font-semibold uppercase tracking-wider text-slate-500">
                <tr>
 <th className="py-3 pl-4 pr-3">Name / Key</th>
      <th className="px-3 py-3">Type</th>
     <th className="px-3 py-3">Updated</th>
     <th className="py-3 pl-3 pr-4 text-right">Actions</th>
      </tr>
         </thead>
          <tbody className="divide-y divide-slate-100">
      {(layouts ?? []).map((l) => <LayoutRow key={l.id} layout={l} siteId={siteId} onEdit={openEdit} />)}
         </tbody>
   </table>
          )}
        </div>
      </div>
    </>
  );
}
