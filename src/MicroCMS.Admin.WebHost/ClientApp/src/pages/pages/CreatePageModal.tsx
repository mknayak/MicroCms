import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { pagesApi } from '@/api/pages';
import { ApiError } from '@/api/client';
import { staticSchema, collectionSchema } from './schemas';
import type { StaticForm, CollectionForm } from './schemas';
import type { PageTreeNode, ContentTypeListItem } from '@/types';

export function CreatePageModal({
  siteId, parentId, flatPages, contentTypes, onClose,
}: {
  siteId: string;
  parentId?: string;
  flatPages: PageTreeNode[];
  contentTypes: ContentTypeListItem[];
  onClose: () => void;
}) {
  const qc = useQueryClient();
  const [type, setType] = useState<'static' | 'collection'>('static');

  const sf = useForm<StaticForm>({ resolver: zodResolver(staticSchema), defaultValues: { parentId: parentId ?? '' } });
  const cf = useForm<CollectionForm>({ resolver: zodResolver(collectionSchema), defaultValues: { parentId: parentId ?? '' } });

  const cs = useMutation({
    mutationFn: (d: StaticForm) => pagesApi.createStatic({ siteId, ...d }),
    onSuccess: () => { toast.success('Page created.'); void qc.invalidateQueries({ queryKey: ['pages', siteId] }); onClose(); },
    onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Failed.'),
  });
  const cc = useMutation({
    mutationFn: (d: CollectionForm) => pagesApi.createCollection({ siteId, ...d }),
    onSuccess: () => { toast.success('Collection page created.'); void qc.invalidateQueries({ queryKey: ['pages', siteId] }); onClose(); },
    onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Failed.'),
  });

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/30 p-4">
      <div className="w-full max-w-md rounded-xl bg-white shadow-xl">
      <div className="flex items-center justify-between border-b border-slate-200 px-5 py-4">
    <h2 className="text-base font-semibold text-slate-900">Add Page{parentId ? ' (child)' : ''}</h2>
          <button onClick={onClose} className="rounded p-1 text-slate-400 hover:bg-slate-100">
      <svg className="h-5 w-5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
            </svg>
       </button>
 </div>
        <div className="p-5">
   <div className="mb-4 flex rounded-lg bg-slate-100 p-1">
      {(['static', 'collection'] as const).map((t) => (
  <button key={t} onClick={() => setType(t)}
      className={`flex-1 rounded-md px-3 py-1.5 text-xs font-medium capitalize transition-colors ${type === t ? 'bg-white text-slate-900 shadow-sm' : 'text-slate-500 hover:text-slate-700'}`}>
  {t === 'static' ? 'Static Page' : 'Collection Page'}
   </button>
            ))}
     </div>

    {type === 'static' ? (
   <form onSubmit={sf.handleSubmit((v) => cs.mutate(v))} className="space-y-3">
    <div>
       <label className="form-label">Title</label>
 <input className="form-input mt-1" {...sf.register('title')} placeholder="About Us" />
      {sf.formState.errors.title && <p className="form-error">{sf.formState.errors.title.message}</p>}
     </div>
   <div>
    <label className="form-label">Slug</label>
       <input className="form-input mt-1 font-mono" {...sf.register('slug')} placeholder="about-us" />
        {sf.formState.errors.slug && <p className="form-error">{sf.formState.errors.slug.message}</p>}
      </div>
  <div>
  <label className="form-label">Parent</label>
    <select className="form-input mt-1" {...sf.register('parentId')}>
       <option value="">None (root)</option>
  {flatPages.map((p) => <option key={p.id} value={p.id}>{'—'.repeat(p.depth)} {p.title}</option>)}
     </select>
   </div>
  <button type="submit" disabled={cs.isPending} className="btn-primary w-full justify-center">
    {cs.isPending ? 'Creating…' : 'Create Static Page'}
   </button>
          </form>
   ) : (
   <form onSubmit={cf.handleSubmit((v) => cc.mutate(v))} className="space-y-3">
   <div>
    <label className="form-label">Title</label>
 <input className="form-input mt-1" {...cf.register('title')} placeholder="Blog" />
  {cf.formState.errors.title && <p className="form-error">{cf.formState.errors.title.message}</p>}
     </div>
            <div>
  <label className="form-label">Slug</label>
     <input className="form-input mt-1 font-mono" {...cf.register('slug')} placeholder="blog" />
    {cf.formState.errors.slug && <p className="form-error">{cf.formState.errors.slug.message}</p>}
  </div>
            <div>
    <label className="form-label">Content Type</label>
            <select className="form-input mt-1" {...cf.register('contentTypeId')}>
  <option value="">Select…</option>
     {contentTypes.map((ct) => <option key={ct.id} value={ct.id}>{ct.displayName}</option>)}
              </select>
      {cf.formState.errors.contentTypeId && <p className="form-error">{cf.formState.errors.contentTypeId.message}</p>}
      </div>
            <div>
      <label className="form-label">Route Pattern</label>
              <input className="form-input mt-1 font-mono" {...cf.register('routePattern')} placeholder="/blog/{slug}" />
         {cf.formState.errors.routePattern && <p className="form-error">{cf.formState.errors.routePattern.message}</p>}
  </div>
    <div>
      <label className="form-label">Parent</label>
        <select className="form-input mt-1" {...cf.register('parentId')}>
 <option value="">None (root)</option>
  {flatPages.map((p) => <option key={p.id} value={p.id}>{'—'.repeat(p.depth)} {p.title}</option>)}
   </select>
    </div>
    <button type="submit" disabled={cc.isPending} className="btn-primary w-full justify-center">
    {cc.isPending ? 'Creating…' : 'Create Collection Page'}
  </button>
          </form>
  )}
</div>
      </div>
    </div>
  );
}
