import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import toast from 'react-hot-toast';
import { taxonomyApi } from '@/api/taxonomy';
import type { Category, Tag } from '@/types';
import { ApiError } from '@/api/client';

// ─── Schemas ──────────────────────────────────────────────────────────────────

const categorySchema = z.object({
  name: z.string().min(1, 'Name is required'),
  slug: z.string().min(1, 'Slug is required').regex(/^[a-z0-9]+(?:-[a-z0-9]+)*$/),
  parentId: z.string().optional(),
});

const tagSchema = z.object({
  name: z.string().min(1, 'Name is required'),
  slug: z.string().min(1, 'Slug is required').regex(/^[a-z0-9]+(?:-[a-z0-9]+)*$/),
});

type CategoryForm = z.infer<typeof categorySchema>;
type TagForm = z.infer<typeof tagSchema>;

// ─── Category Tree ────────────────────────────────────────────────────────────

function CategoryItem({ cat, depth = 0 }: { cat: Category; depth?: number }) {
  const qc = useQueryClient();
  const del = useMutation({
    mutationFn: () => taxonomyApi.deleteCategory(cat.id),
    onSuccess: () => { toast.success('Category deleted.'); void qc.invalidateQueries({ queryKey: ['categories'] }); },
    onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Delete failed.'),
  });

  return (
    <li>
      <div
        className="flex items-center justify-between rounded-lg py-1.5 hover:bg-slate-50"
        style={{ paddingLeft: `${depth * 16 + 12}px` }}
      >
        <div className="flex items-center gap-2">
          {depth > 0 && <span className="text-slate-300">└</span>}
          <span className="text-sm font-medium text-slate-800">{cat.name}</span>
          <span className="font-mono text-xs text-slate-400">/{cat.slug}</span>
          <span className="badge-slate">{cat.entryCount}</span>
        </div>
        <button
          onClick={() => { if (confirm(`Delete "${cat.name}"?`)) del.mutate(); }}
          className="mr-2 text-xs text-red-400 hover:text-red-600"
        >
          Delete
        </button>
      </div>
      {cat.children && cat.children.length > 0 && (
        <ul>
          {cat.children.map((child) => (
            <CategoryItem key={child.id} cat={child} depth={depth + 1} />
          ))}
        </ul>
      )}
    </li>
  );
}

// ─── Page ─────────────────────────────────────────────────────────────────────

export default function TaxonomyPage() {
  const qc = useQueryClient();
  const [activeTab, setActiveTab] = useState<'categories' | 'tags'>('categories');

  const { data: categories, isLoading: catsLoading } = useQuery({
    queryKey: ['categories'],
    queryFn: taxonomyApi.getCategories,
  });

  const { data: tags, isLoading: tagsLoading } = useQuery({
    queryKey: ['tags'],
    queryFn: taxonomyApi.getTags,
  });

  // Category form
  const {
    register: regCat,
    handleSubmit: handleCat,
    reset: resetCat,
    formState: { errors: catErrors, isSubmitting: catSubmitting },
  } = useForm<CategoryForm>({ resolver: zodResolver(categorySchema) });

  const createCategoryMutation = useMutation({
    mutationFn: (data: CategoryForm) => taxonomyApi.createCategory(data),
    onSuccess: () => {
      toast.success('Category created.');
      void qc.invalidateQueries({ queryKey: ['categories'] });
      resetCat();
    },
    onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Failed.'),
  });

  // Tag form
  const {
    register: regTag,
    handleSubmit: handleTag,
    reset: resetTag,
    formState: { errors: tagErrors, isSubmitting: tagSubmitting },
  } = useForm<TagForm>({ resolver: zodResolver(tagSchema) });

  const createTagMutation = useMutation({
    mutationFn: (data: TagForm) => taxonomyApi.createTag(data),
    onSuccess: () => {
      toast.success('Tag created.');
      void qc.invalidateQueries({ queryKey: ['tags'] });
      resetTag();
    },
    onError: (err) => toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Failed.'),
  });

  const deleteTagMutation = useMutation({
    mutationFn: (id: string) => taxonomyApi.deleteTag(id),
    onSuccess: () => { toast.success('Tag deleted.'); void qc.invalidateQueries({ queryKey: ['tags'] }); },
  });

  return (
    <div className="space-y-6">
      <div>
        <h1 className="text-2xl font-bold text-slate-900">Taxonomy</h1>
        <p className="mt-1 text-sm text-slate-500">Organise content with categories and tags.</p>
      </div>

      {/* Tabs */}
      <div className="border-b border-slate-200">
        <nav className="-mb-px flex gap-4">
          {(['categories', 'tags'] as const).map((tab) => (
            <button
              key={tab}
              onClick={() => setActiveTab(tab)}
              className={`border-b-2 pb-2 text-sm font-medium capitalize transition-colors ${
                activeTab === tab
                  ? 'border-brand-600 text-brand-600'
                  : 'border-transparent text-slate-500 hover:text-slate-700'
              }`}
            >
              {tab}
            </button>
          ))}
        </nav>
      </div>

      {activeTab === 'categories' ? (
        <div className="grid grid-cols-3 gap-6">
          {/* Tree */}
          <div className="col-span-2 card">
            <h2 className="mb-4 text-base font-semibold text-slate-900">Category Tree</h2>
            {catsLoading ? (
              <div className="space-y-2">
                {Array.from({ length: 4 }).map((_, i) => <div key={i} className="h-8 animate-pulse rounded bg-slate-100" />)}
              </div>
            ) : (
              <ul className="divide-y divide-slate-50">
                {(categories ?? []).map((cat) => <CategoryItem key={cat.id} cat={cat} />)}
              </ul>
            )}
          </div>

          {/* New category form */}
          <div className="card">
            <h2 className="mb-4 text-base font-semibold text-slate-900">New Category</h2>
            <form onSubmit={handleCat((v) => createCategoryMutation.mutate(v))} className="space-y-3">
              <div>
                <label className="form-label">Name</label>
                <input className="form-input mt-1" {...regCat('name')} placeholder="Technology" />
                {catErrors.name && <p className="form-error">{catErrors.name.message}</p>}
              </div>
              <div>
                <label className="form-label">Slug</label>
                <input className="form-input mt-1 font-mono" {...regCat('slug')} placeholder="technology" />
                {catErrors.slug && <p className="form-error">{catErrors.slug.message}</p>}
              </div>
              <div>
                <label className="form-label">Parent (optional)</label>
                <select className="form-input mt-1" {...regCat('parentId')}>
                  <option value="">None (top-level)</option>
                  {(categories ?? []).map((cat) => (
                    <option key={cat.id} value={cat.id}>{cat.name}</option>
                  ))}
                </select>
              </div>
              <button type="submit" disabled={catSubmitting} className="btn-primary w-full justify-center">
                {catSubmitting ? 'Creating…' : 'Create Category'}
              </button>
            </form>
          </div>
        </div>
      ) : (
        <div className="grid grid-cols-3 gap-6">
          {/* Tags list */}
          <div className="col-span-2 card">
            <h2 className="mb-4 text-base font-semibold text-slate-900">Tags</h2>
            {tagsLoading ? (
              <div className="flex flex-wrap gap-2">
                {Array.from({ length: 8 }).map((_, i) => <div key={i} className="h-7 w-20 animate-pulse rounded-full bg-slate-200" />)}
              </div>
            ) : (
              <div className="flex flex-wrap gap-2">
                {(tags ?? []).map((tag: Tag) => (
                  <span key={tag.id} className="flex items-center gap-1.5 rounded-full bg-slate-100 px-3 py-1 text-sm text-slate-700">
                    #{tag.name}
                    <span className="text-xs text-slate-400">({tag.entryCount})</span>
                    <button
                      onClick={() => { if (confirm(`Delete tag "${tag.name}"?`)) deleteTagMutation.mutate(tag.id); }}
                      className="text-slate-300 hover:text-red-500"
                      aria-label={`Delete ${tag.name}`}
                    >×</button>
                  </span>
                ))}
              </div>
            )}
          </div>

          {/* New tag form */}
          <div className="card">
            <h2 className="mb-4 text-base font-semibold text-slate-900">New Tag</h2>
            <form onSubmit={handleTag((v) => createTagMutation.mutate(v))} className="space-y-3">
              <div>
                <label className="form-label">Name</label>
                <input className="form-input mt-1" {...regTag('name')} placeholder="React" />
                {tagErrors.name && <p className="form-error">{tagErrors.name.message}</p>}
              </div>
              <div>
                <label className="form-label">Slug</label>
                <input className="form-input mt-1 font-mono" {...regTag('slug')} placeholder="react" />
                {tagErrors.slug && <p className="form-error">{tagErrors.slug.message}</p>}
              </div>
              <button type="submit" disabled={tagSubmitting} className="btn-primary w-full justify-center">
                {tagSubmitting ? 'Creating…' : 'Create Tag'}
              </button>
            </form>
          </div>
        </div>
      )}
    </div>
  );
}
