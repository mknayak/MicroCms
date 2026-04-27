import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import toast from 'react-hot-toast';
import { siteTemplatesApi } from '@/api/siteTemplates';
import { layoutsApi } from '@/api/layouts';
import { useSite } from '@/contexts/SiteContext';
import type { SiteTemplateListItem, SiteTemplateDto } from '@/types';
import { ApiError } from '@/api/client';

// ─── Schema ───────────────────────────────────────────────────────────────────

const schema = z.object({
  name: z.string().min(1, 'Name is required').max(200),
  layoutId: z.string().min(1, 'A layout must be selected'),
  description: z.string().max(500).optional(),
});
type FormValues = z.infer<typeof schema>;

// ─── Create/Edit modal ────────────────────────────────────────────────────────

function TemplateModal({
  existing,
  siteId,
  onClose,
}: {
  existing: SiteTemplateDto | null;
  siteId: string;
  onClose: () => void;
}) {
  const qc = useQueryClient();
  const { data: layouts = [] } = useQuery({
    queryKey: ['layouts', siteId],
    queryFn: () => layoutsApi.list(siteId),
    enabled: !!siteId,
  });

  const form = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: existing
      ? { name: existing.name, layoutId: existing.layoutId, description: existing.description ?? '' }
  : { name: '', layoutId: layouts[0]?.id ?? '', description: '' },
  });

  const createMutation = useMutation({
    mutationFn: (data: FormValues) => siteTemplatesApi.create({ siteId, ...data }),
    onSuccess: () => { toast.success('Template created.'); void qc.invalidateQueries({ queryKey: ['site-templates', siteId] }); onClose(); },
    onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Create failed.'),
  });

  const updateMutation = useMutation({
    mutationFn: (data: FormValues) => siteTemplatesApi.update(existing!.id, data),
    onSuccess: () => { toast.success('Template saved.'); void qc.invalidateQueries({ queryKey: ['site-templates', siteId] }); onClose(); },
  onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Save failed.'),
  });

  const isPending = createMutation.isPending || updateMutation.isPending;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/30">
      <div className="w-full max-w-md rounded-xl bg-white shadow-xl">
        <div className="flex items-center justify-between border-b border-slate-200 px-6 py-4">
          <h2 className="text-base font-semibold text-slate-900">{existing ? `Edit: ${existing.name}` : 'New Page Template'}</h2>
          <button onClick={onClose} className="rounded p-1 text-slate-400 hover:bg-slate-100">
            <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor"><path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" /></svg>
    </button>
        </div>
        <form
      onSubmit={form.handleSubmit((d) => existing ? updateMutation.mutate(d) : createMutation.mutate(d))}
          className="space-y-4 px-6 py-5"
  >
   <div>
        <label className="form-label">Template Name</label>
            <input className="form-input mt-1" {...form.register('name')} placeholder="Blog Layout — Standard" />
   {form.formState.errors.name && <p className="form-error">{form.formState.errors.name.message}</p>}
          </div>
        <div>
          <label className="form-label">Layout</label>
    <select className="form-input mt-1" {...form.register('layoutId')}>
          <option value="">— Select a layout —</option>
              {layouts.map((l) => (
      <option key={l.id} value={l.id}>{l.name}{l.isDefault ? ' (default)' : ''}</option>
    ))}
     </select>
         {form.formState.errors.layoutId && <p className="form-error">{form.formState.errors.layoutId.message}</p>}
            <p className="mt-1 text-xs text-slate-400">The layout defines the zones available in this template.</p>
          </div>
          <div>
      <label className="form-label">Description (optional)</label>
       <input className="form-input mt-1" {...form.register('description')} placeholder="Shared header/footer for all blog pages" />
</div>
          <div className="flex justify-end gap-3 pt-2">
<button type="button" onClick={onClose} className="btn-secondary">Cancel</button>
    <button type="submit" disabled={isPending} className="btn-primary">
              {isPending ? 'Saving…' : existing ? 'Save Changes' : 'Create Template'}
     </button>
          </div>
        </form>
      </div>
    </div>
  );
}

// ─── Template row ─────────────────────────────────────────────────────────────

function TemplateRow({
  template,
  onEdit,
  onDelete,
  onDesign,
}: {
  template: SiteTemplateListItem;
  onEdit: () => void;
  onDelete: () => void;
  onDesign: () => void;
}) {
  return (
    <tr className="hover:bg-slate-50">
      <td className="py-3 pl-4 pr-3">
        <p className="text-sm font-semibold text-slate-900">{template.name}</p>
     {template.description && <p className="text-xs text-slate-400">{template.description}</p>}
      </td>
      <td className="px-3 py-3 text-sm text-slate-600">{template.layoutName}</td>
 <td className="px-3 py-3">
        <span className="rounded-full bg-brand-100 px-2 py-0.5 text-[10px] font-semibold text-brand-700">
   {template.pageCount} page{template.pageCount !== 1 ? 's' : ''}
        </span>
      </td>
      <td className="px-3 py-3 text-xs text-slate-400">{new Date(template.updatedAt).toLocaleDateString()}</td>
      <td className="py-3 pl-3 pr-4">
        <div className="flex items-center justify-end gap-2">
          <button
    onClick={onDesign}
            className="flex items-center gap-1 rounded-md border border-brand-200 bg-brand-50 px-2.5 py-1 text-xs font-semibold text-brand-700 hover:bg-brand-100"
>
     <svg className="h-3 w-3" fill="none" viewBox="0 0 24 24" stroke="currentColor">
   <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 3h18v18H3zM3 9h18M9 21V9" />
            </svg>
    Design
          </button>
          <button onClick={onEdit} className="text-xs text-slate-500 hover:underline">Edit</button>
          <button onClick={onDelete} className="text-xs text-red-400 hover:text-red-600">Delete</button>
     </div>
      </td>
    </tr>
  );
}

// ─── Page ─────────────────────────────────────────────────────────────────────

export default function PageTemplatesPage() {
  const { selectedSiteId, selectedSite, isLoading: siteLoading } = useSite();
  const siteId = selectedSiteId ?? '';
  const navigate = useNavigate();
  const qc = useQueryClient();

  const [modalOpen, setModalOpen] = useState(false);
  const [editTarget, setEditTarget] = useState<SiteTemplateDto | null>(null);

  const { data: templates, isLoading } = useQuery({
    queryKey: ['site-templates', siteId],
    queryFn: () => siteTemplatesApi.list(siteId),
    enabled: !!siteId,
  });

  const deleteMutation = useMutation({
    mutationFn: (id: string) => siteTemplatesApi.delete(id),
    onSuccess: () => { toast.success('Template deleted.'); void qc.invalidateQueries({ queryKey: ['site-templates', siteId] }); },
    onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Delete failed.'),
  });

  const openCreate = () => { setEditTarget(null); setModalOpen(true); };
  const openEdit = async (id: string) => {
    try { const t = await siteTemplatesApi.get(id); setEditTarget(t); setModalOpen(true); }
    catch { toast.error('Could not load template.'); }
  };

  if (siteLoading) return <div className="space-y-3">{Array.from({ length: 3 }).map((_, i) => <div key={i} className="h-12 animate-pulse rounded-lg bg-slate-100" />)}</div>;
  if (!siteId) return <div className="py-24 text-center text-sm text-slate-500">No site selected.</div>;

  return (
    <>
      {modalOpen && <TemplateModal existing={editTarget} siteId={siteId} onClose={() => setModalOpen(false)} />}

      <div className="space-y-6">
        {/* Header */}
        <div className="flex items-center justify-between">
          <div>
            <h1 className="text-2xl font-bold text-slate-900">Page Templates</h1>
            <p className="mt-1 text-sm text-slate-500">
     Reusable templates for <span className="font-medium text-slate-700">{selectedSite?.name}</span>.
    A template is linked to a <strong>layout</strong> and pre-fills common components (header, footer, sidebar)
         inherited by every page that uses it.
      </p>
          </div>
     <button onClick={openCreate} className="btn-primary">
            <svg className="mr-2 h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
   <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
</svg>
         New Template
          </button>
        </div>

        {/* How it works callout */}
        <div className="rounded-lg border border-brand-100 bg-brand-50 px-5 py-4">
 <p className="text-xs font-bold text-brand-800 mb-1">How the three-tier hierarchy works</p>
  <div className="flex flex-wrap gap-4 text-xs text-brand-700">
            <span><strong>1. Layout</strong> — defines zones (Header, Content, Footer)</span>
     <span>→</span>
         <span><strong>2. Template</strong> — fills shared zones with common components (Nav bar, Footer block)</span>
         <span>→</span>
<span><strong>3. Page</strong> — inherits template, fills content zones with page-specific components</span>
       </div>
        </div>

        {/* Table */}
        <div className="card overflow-hidden p-0">
        {isLoading ? (
     <div className="space-y-2 p-4">{Array.from({ length: 3 }).map((_, i) => <div key={i} className="h-10 animate-pulse rounded bg-slate-100" />)}</div>
          ) : (templates ?? []).length === 0 ? (
            <div className="flex flex-col items-center gap-3 py-16 text-center">
      <svg className="h-8 w-8 text-slate-300" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M3 3h18v18H3zM3 9h18M9 21V9" />
              </svg>
     <p className="text-sm text-slate-500">No page templates yet.</p>
           <button onClick={openCreate} className="btn-secondary text-xs">Create your first template</button>
       </div>
          ) : (
      <table className="w-full text-left">
              <thead className="border-b border-slate-200 bg-slate-50 text-xs font-semibold uppercase tracking-wider text-slate-500">
                <tr>
        <th className="py-3 pl-4 pr-3">Name</th>
           <th className="px-3 py-3">Layout</th>
    <th className="px-3 py-3">Pages</th>
  <th className="px-3 py-3">Updated</th>
            <th className="py-3 pl-3 pr-4 text-right">Actions</th>
    </tr>
    </thead>
      <tbody className="divide-y divide-slate-100">
     {(templates ?? []).map((t) => (
   <TemplateRow
           key={t.id}
    template={t}
   onEdit={() => openEdit(t.id)}
    onDesign={() => navigate(`/page-templates/${t.id}/designer`)}
  onDelete={() => { if (confirm(`Delete template "${t.name}"?`)) deleteMutation.mutate(t.id); }}
      />
         ))}
              </tbody>
   </table>
          )}
        </div>
      </div>
    </>
  );
}
