import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm, FormProvider } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import toast from 'react-hot-toast';
import { componentsApi } from '@/api/components';
import { ApiError } from '@/api/client';
import type { ComponentCategory, RenderingTemplateType } from '@/types';
import { toCamelCase } from '@/pages/content-types/schemaTab.helpers';
import { schemaFormValidator, DEFAULT_GROUP } from '@/pages/content-types/schemaTab.types';
import type { SchemaFormValues } from '@/pages/content-types/schemaTab.types';
import { SchemaEditView } from '@/pages/content-types/SchemaEditView';

// ─── Constants ────────────────────────────────────────────────────────────────

const CATEGORIES: ComponentCategory[] = [
  'Layout', 'Content', 'Media', 'Navigation', 'Interactive', 'Commerce',
];

const TEMPLATE_TYPES: { value: RenderingTemplateType; label: string; ext: string }[] = [
  { value: 'Handlebars',   label: 'Handlebars (.hbs)', ext: '.hbs'   },
  { value: 'WebComponent', label: 'HTML (.html)',       ext: '.html'  },
];

// ─── Schema ───────────────────────────────────────────────────────────────────

const metaSchema = z.object({
  name: z.string().min(1, 'Name required').max(200),
  key: z.string().min(1, 'Key required').regex(/^[a-z0-9-]+$/),
  description: z.string().optional(),
  category: z.enum(CATEGORIES as [ComponentCategory, ...ComponentCategory[]]),
});

type MetaForm = z.infer<typeof metaSchema>;
type ActiveTab = 'fields' | 'template' | 'thumbnail' | 'meta';

// ─── Page ─────────────────────────────────────────────────────────────────────

export default function ComponentEditorPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const qc = useQueryClient();
  const [activeTab, setActiveTab] = useState<ActiveTab>('fields');

  // Template state — managed separately from the schema form
  const [templateType, setTemplateType] = useState<RenderingTemplateType>('RazorPartial');
  const [templateContent, setTemplateContent] = useState('');

  // Thumbnail state
  const [thumbnailDataUri, setThumbnailDataUri] = useState<string | null>(null);

  const { data: comp, isLoading } = useQuery({
 queryKey: ['component', id],
    queryFn: () => componentsApi.getById(id!),
    enabled: !!id,
  });

  // ── Meta form (name / key / category / description) ────────────────────────
  const metaForm = useForm<MetaForm>({
    resolver: zodResolver(metaSchema),
    defaultValues: { name: '', key: '', description: '', category: 'Layout' },
  });
  const { register: registerMeta, formState: { errors: metaErrors, isDirty: metaIsDirty }, reset: resetMeta } = metaForm;

  // ── Schema form (fields) — uses the same SchemaFormValues as content types ──
  const schemaForm = useForm<SchemaFormValues>({
    resolver: zodResolver(schemaFormValidator),
    defaultValues: {
      name: '', apiKey: '', description: '', localizationMode: 'PerLocale',
      kind: 'Content', siteTemplateId: '', parentContentTypeId: '', fields: [],
    },
  });
  const { formState: { isDirty: schemaDirty }, reset: resetSchema } = schemaForm;

  useEffect(() => {
    if (comp) {
      resetMeta({
        name: comp.name,
        key: comp.key,
        description: comp.description ?? '',
        category: comp.category,
      });
      resetSchema({
        name: comp.name,
        apiKey: comp.key,
        description: comp.description ?? '',
        localizationMode: 'PerLocale',
        kind: 'Content',
        siteTemplateId: '',
        parentContentTypeId: '',
        fields: comp.fields.map((f) => ({
          id: f.id,
          name: f.label,
          type: f.fieldType as SchemaFormValues['fields'][number]['type'],
          required: f.isRequired,
          localized: f.isLocalized,
          isIndexed: f.isIndexed,
          isUnique: f.isUnique ?? false,
          isList: f.isList ?? false,
          groupName: f.groupName ?? DEFAULT_GROUP,
          enumMode: (f.dynamicSource ? 'dynamic' : 'static') as 'static' | 'dynamic',
          staticOptions: f.options ?? [],
          dynamicSource: f.dynamicSource
            ? { contentTypeHandle: f.dynamicSource.contentTypeHandle ?? '', labelField: f.dynamicSource.labelField ?? '', valueField: f.dynamicSource.valueField ?? '', statusFilter: f.dynamicSource.statusFilter ?? 'Published', groupHandle: f.dynamicSource.groupHandle ?? '' }
            : { contentTypeHandle: '', labelField: '', valueField: '', statusFilter: 'Published', groupHandle: '' },
          multiListSource: f.multiListSource
            ? { contentTypeHandle: f.multiListSource.contentTypeHandle ?? '', labelField: f.multiListSource.labelField ?? '', valueField: f.multiListSource.valueField ?? '', statusFilter: f.multiListSource.statusFilter ?? '', groupHandle: f.multiListSource.groupHandle ?? '' }
            : { contentTypeHandle: '', labelField: '', valueField: '', statusFilter: '', groupHandle: '' },
          componentSourceKey: f.componentSource?.componentKey ?? '',
        })),
      });
      setTemplateType(comp.templateType);
      setTemplateContent(comp.templateContent ?? '');
      setThumbnailDataUri(comp.thumbnailDataUri ?? null);
    }
  }, [comp, resetMeta, resetSchema]);

  // ── Schema save ────────────────────────────────────────────────────────────
  const updateMutation = useMutation({
    mutationFn: (data: SchemaFormValues) =>
      componentsApi.update(id!, {
        name: comp!.name,
        description: comp?.description,
        category: comp!.category,
        fields: data.fields.map((f, i) => ({
          id: f.id ?? crypto.randomUUID(),
          handle: toCamelCase(f.name) || `field${i}`,
          label: f.name,
          fieldType: f.type,
          isRequired: f.required,
          isLocalized: f.localized,
          isIndexed: f.isIndexed,
          isUnique: f.isUnique,
          isList: f.isList,
          sortOrder: i,
          groupName: f.groupName ?? DEFAULT_GROUP,
          options: f.type === 'Enum' && f.enumMode === 'static' ? f.staticOptions : undefined,
          dynamicSource:
            (f.type === 'Enum' && f.enumMode === 'dynamic') || f.type === 'Reference'
              ? (f.dynamicSource?.contentTypeHandle?.trim() ? f.dynamicSource : undefined)
              : undefined,
          multiListSource: f.type === 'MultiList'
            ? (f.multiListSource?.contentTypeHandle?.trim() ? f.multiListSource : undefined)
            : undefined,
          componentSourceKey: f.type === 'Component' && f.componentSourceKey?.trim()
            ? f.componentSourceKey.trim()
            : undefined,
        })),
      }),
    onSuccess: () => {
      toast.success('Component fields saved.');
      void qc.invalidateQueries({ queryKey: ['component', id] });
      void qc.invalidateQueries({ queryKey: ['components'] });
    },
    onError: (err) =>
      toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Save failed.'),
  });

  // ── Meta save (name / category / description) ──────────────────────────────
  const updateMetaMutation = useMutation({
    mutationFn: (data: MetaForm) =>
      componentsApi.update(id!, {
        name: data.name,
        description: data.description,
        category: data.category,
        fields: comp?.fields ?? [],
      }),
    onSuccess: () => {
      toast.success('Component definition saved.');
      void qc.invalidateQueries({ queryKey: ['component', id] });
      void qc.invalidateQueries({ queryKey: ['components'] });
    },
    onError: (err) =>
      toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Save failed.'),
  });

  // ── Thumbnail save ─────────────────────────────────────────────────────────
  const thumbnailMutation = useMutation({
    mutationFn: () => componentsApi.updateThumbnail(id!, thumbnailDataUri),
    onSuccess: () => {
      toast.success('Thumbnail saved.');
      void qc.invalidateQueries({ queryKey: ['component', id] });
      void qc.invalidateQueries({ queryKey: ['components'] });
    },
    onError: (err) =>
      toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Save failed.'),
  });

  // ── Template save ──────────────────────────────────────────────────────────
  const templateMutation = useMutation({
    mutationFn: () =>
      componentsApi.updateTemplate(id!, { templateType, templateContent: templateContent || undefined }),
    onSuccess: () => {
      toast.success('Rendering template saved.');
      void qc.invalidateQueries({ queryKey: ['component', id] });
    },
    onError: (err) =>
toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Save failed.'),
  });

  if (isLoading) {
    return (
      <div className="space-y-4">
      {Array.from({ length: 6 }).map((_, i) => (
          <div key={i} className="h-10 animate-pulse rounded-lg bg-slate-100" />
    ))}
      </div>
    );
  }

  if (!comp) return <p className="text-sm text-slate-500">Component not found.</p>;

  const currentExt = TEMPLATE_TYPES.find((t) => t.value === templateType)?.ext ?? '';
  const schemaFields = schemaForm.watch('fields');
  const TABS: { key: ActiveTab; label: string }[] = [
    { key: 'fields',    label: `Fields (${schemaFields.length})` },
    { key: 'template',  label: 'Template' },
    { key: 'thumbnail', label: 'Thumbnail' },
    { key: 'meta',      label: 'Meta' },
  ];

  return (
    <div className="space-y-6">
      {/* ── Header ── */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <button type="button" onClick={() => navigate('/components')}
            className="text-sm text-slate-500 hover:text-slate-700">
            ← Component Library
          </button>
          <span className="text-slate-300">/</span>
          <h1 className="text-xl font-bold text-slate-900">{comp.name}</h1>
          <span className="rounded-full bg-slate-100 px-2 py-0.5 font-mono text-xs text-slate-500">
            comp/{comp.key}
          </span>
        </div>
        <div className="flex gap-2">
          <button type="button" onClick={() => navigate(`/components/${id}/items`)}
            className="btn-secondary text-sm">
            {comp.itemCount} Items
          </button>
          {activeTab === 'template' ? (
            <button type="button" className="btn-primary"
              disabled={templateMutation.isPending}
              onClick={() => templateMutation.mutate()}>
              {templateMutation.isPending ? 'Saving…' : 'Save Template'}
            </button>
          ) : activeTab === 'thumbnail' ? (
            <button type="button" className="btn-primary"
              disabled={thumbnailMutation.isPending}
              onClick={() => thumbnailMutation.mutate()}>
              {thumbnailMutation.isPending ? 'Saving…' : 'Save Thumbnail'}
            </button>
          ) : activeTab === 'meta' ? (
            <button type="button" className="btn-primary"
              disabled={!metaIsDirty || updateMetaMutation.isPending}
              onClick={metaForm.handleSubmit((v) => updateMetaMutation.mutate(v))}>
              {updateMetaMutation.isPending ? 'Saving…' : 'Save Meta'}
            </button>
          ) : (
            <button type="button" className="btn-primary"
              disabled={!schemaDirty || updateMutation.isPending}
              onClick={schemaForm.handleSubmit((v) => updateMutation.mutate(v))}>
              {updateMutation.isPending ? 'Saving…' : 'Save Fields'}
            </button>
          )}
        </div>
      </div>

      {/* ── Tabs ── */}
      <div className="flex border-b border-slate-200">
        {TABS.map((tab) => (
          <button key={tab.key} type="button" onClick={() => setActiveTab(tab.key)}
            className={`px-4 py-2 text-sm font-medium border-b-2 -mb-px transition-colors ${
              activeTab === tab.key
                ? 'border-brand-500 text-brand-600'
                : 'border-transparent text-slate-500 hover:text-slate-700'
            }`}>
            {tab.label}
 </button>
        ))}
  </div>

      {/* ── Tab bodies ── */}
      <div className="grid grid-cols-3 gap-6">
        <div className="col-span-2 space-y-4">

          {/* ── Fields — reuse the full content-type SchemaEditView ── */}
          {activeTab === 'fields' && (
            <FormProvider {...schemaForm}>
              <SchemaEditView
                contentTypeId={id ?? ''}
                contentTypeName={comp.name}
                onCancel={() => resetSchema()}
                isSubmitting={updateMutation.isPending}
                componentMode
              />
            </FormProvider>
          )}

          {/* ── Template ── */}
          {activeTab === 'template' && (
            <div className="card overflow-hidden p-0">
              {/* Toolbar */}
              <div className="flex items-center gap-3 border-b border-slate-200 bg-slate-50 px-4 py-2.5">
                <select
                  value={templateType}
                  onChange={(e) => setTemplateType(e.target.value as RenderingTemplateType)}
                  className="rounded border border-slate-200 bg-white px-2 py-1 text-xs font-semibold focus:outline-none focus:border-brand-500">
                  {TEMPLATE_TYPES.map((t) => (
                    <option key={t.value} value={t.value}>{t.label}</option>
                  ))}
                </select>
                <span className="font-mono text-xs text-slate-400">
                  {comp.key}{currentExt}
                </span>
                <div className="ml-auto flex items-center gap-1.5 text-xs text-slate-400">
                  <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2}
                      d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
                  </svg>
                  Use <code className="mx-1 rounded bg-slate-200 px-1">&#123;&#123;fieldHandle&#125;&#125;</code> to bind field values
                </div>
              </div>

              {/* Code editor */}
              <textarea
                value={templateContent}
                onChange={(e) => setTemplateContent(e.target.value)}
                spellCheck={false}
                placeholder={getTemplatePlaceholder(templateType, comp.key, schemaFields.map((f) => toCamelCase(f.name)).filter(Boolean))}
                className="block min-h-[420px] w-full resize-y bg-[#1e1e2e] p-4 font-mono text-sm leading-relaxed text-[#cdd6f4] placeholder-[#585b70] focus:outline-none"
              />

              {/* Bindings reference */}
              {schemaFields.length > 0 && (
                <div className="border-t border-slate-200 bg-slate-50 px-4 py-3">
                  <p className="mb-2 text-[11px] font-bold uppercase tracking-wider text-slate-400">
                    Available bindings
                  </p>
                  <div className="flex flex-wrap gap-2">
                    {schemaFields.map((f, i) => { const h = toCamelCase(f.name); return h ? (
                      <span key={i}
                        className="inline-flex items-center gap-1 rounded border border-slate-200 bg-white px-2 py-0.5 font-mono text-xs text-brand-600"
                        title={f.type}>
                        {getBindingExpression(templateType, h)}
                        <span className="text-[10px] text-slate-400">({f.type})</span>
                      </span>
                    ) : null; })}
                  </div>
                </div>
              )}

              {/* Example / placeholder */}
              <div className="border-t border-slate-200">
                <div className="flex items-center justify-between bg-slate-50 px-4 py-2.5">
                  <p className="text-[11px] font-bold uppercase tracking-wider text-slate-400">
                    Example
                  </p>
                  <span className="text-[10px] text-slate-400">
                    Starter template — paste into the editor above to get started
                  </span>
                </div>
                <pre className="overflow-auto bg-[#1e1e2e] p-4 font-mono text-xs leading-relaxed text-[#585b70]">
                  {getTemplatePlaceholder(templateType, comp.key, schemaFields.map((f) => toCamelCase(f.name)).filter(Boolean))}
                </pre>
              </div>
            </div>
          )}

      {/* ── Thumbnail ── */}
      {activeTab === 'thumbnail' && (
        <ThumbnailEditor
          value={thumbnailDataUri}
          onChange={setThumbnailDataUri}
          componentName={comp.name}
        />
      )}

  {/* ── Meta ── */}
  {activeTab === 'meta' && (
    <div className="card space-y-4">
      <div className="grid grid-cols-2 gap-4">
        <div>
          <label className="form-label">Display Name</label>
          <input className="form-input mt-1" {...registerMeta('name')} />
          {metaErrors.name && <p className="form-error">{metaErrors.name.message}</p>}
        </div>
        <div>
          <label className="form-label">Key</label>
          <div className="mt-1 flex items-center rounded-lg border border-slate-200 focus-within:border-brand-500">
            <span className="rounded-l-lg border-r border-slate-200 bg-slate-50 px-2.5 py-2 text-xs text-slate-500">
              comp/
            </span>
            <input className="flex-1 rounded-r-lg px-2.5 py-2 font-mono text-sm focus:outline-none"
              {...registerMeta('key')} />
          </div>
          {metaErrors.key && <p className="form-error">{metaErrors.key.message}</p>}
        </div>
      </div>
      <div>
        <label className="form-label">Category</label>
        <select className="form-input mt-1" {...registerMeta('category')}>
          {CATEGORIES.map((c) => <option key={c} value={c}>{c}</option>)}
        </select>
      </div>
      <div>
        <label className="form-label">Description</label>
        <textarea className="form-input mt-1" {...registerMeta('description')} rows={3} />
      </div>
    </div>
  )}
        </div>

        {/* ── Sidebar ── */}
 <div className="space-y-4">
    <div className="card">
        <h3 className="mb-3 text-sm font-semibold text-slate-800">Component Status</h3>
         <div className="space-y-1.5 text-xs">
     <div className="flex justify-between">
      <span className="text-slate-500">Template</span>
           <span className="rounded bg-slate-100 px-1.5 py-0.5 font-mono font-semibold text-slate-600">
        {comp.templateType}
           </span>
        </div>
     <div className="flex justify-between">
                <span className="text-slate-500">Items</span>
          <button type="button" onClick={() => navigate(`/components/${id}/items`)}
           className="font-semibold text-brand-600">
      {comp.itemCount} instances →
   </button>
      </div>
<div className="flex justify-between">
     <span className="text-slate-500">Used on pages</span>
        <span className="font-semibold">{comp.usageCount}</span>
</div>
    <div className="flex justify-between">
     <span className="text-slate-500">Last modified</span>
        <span className="font-semibold">{new Date(comp.updatedAt).toLocaleDateString()}</span>
              </div>
  </div>
          </div>

          {activeTab === 'template' && (
      <div className="card text-xs text-slate-600 space-y-2">
       <p className="font-semibold text-slate-800">Template guide</p>
          {templateType === 'RazorPartial' && (
         <p>Use <code className="rounded bg-slate-100 px-1">@Model.FieldHandle</code> to output field values. Asset refs expose <code className="rounded bg-slate-100 px-1">.Url</code>, <code className="rounded bg-slate-100 px-1">.Alt</code>.</p>
  )}
         {templateType === 'Handlebars' && (
     <div className="space-y-2">
       <p>Use <code className="rounded bg-slate-100 px-1">{'{{fieldHandle}}'}</code> to output values. Triple-stash <code className="rounded bg-slate-100 px-1">{'{{{fieldHandle}}}'}</code> for unescaped HTML.</p>
       <div>
         <p className="font-semibold text-slate-700 mb-1">Conditional (if / else)</p>
         <pre className="rounded bg-slate-100 px-2 py-1.5 font-mono text-[11px] text-slate-700 whitespace-pre-wrap">{`{{#if fieldHandle}}\n  shown when truthy\n{{else}}\n  shown when falsy\n{{/if}}`}</pre>
         <p className="mt-1 text-slate-500">Use <code className="rounded bg-slate-100 px-1">{'{{#unless fieldHandle}}'}</code> for the inverse.</p>
       </div>
       <div>
         <p className="font-semibold text-slate-700 mb-1">Loop (each)</p>
         <pre className="rounded bg-slate-100 px-2 py-1.5 font-mono text-[11px] text-slate-700 whitespace-pre-wrap">{`{{#each listField}}\n  {{this.title}} — {{@index}}\n{{/each}}`}</pre>
         <p className="mt-1 text-slate-500"><code className="rounded bg-slate-100 px-1">{'{{@index}}'}</code> = 0-based position · <code className="rounded bg-slate-100 px-1">{'{{@first}}'}</code> / <code className="rounded bg-slate-100 px-1">{'{{@last}}'}</code> = booleans.</p>
       </div>
       <div>
         <p className="font-semibold text-slate-700 mb-1">Nested properties</p>
         <pre className="rounded bg-slate-100 px-2 py-1.5 font-mono text-[11px] text-slate-700 whitespace-pre-wrap">{`{{image.url}}\n{{image.alt}}`}</pre>
       </div>
       <div>
         <p className="font-semibold text-slate-700 mb-1">Inspect / debug a field</p>
         <pre className="rounded bg-slate-100 px-2 py-1.5 font-mono text-[11px] text-slate-700 whitespace-pre-wrap">{`{{this}}`}</pre>
         <p className="mt-1 text-slate-500">Inside an <code className="rounded bg-slate-100 px-1">{'{{#each}}'}</code> block, <code className="rounded bg-slate-100 px-1">{'{{this}}'}</code> outputs all key/value pairs of the current item — useful for discovering available field names.</p>
       </div>

     </div>
    )}
  {templateType === 'React' && (
            <p>Props are typed from the field schema. Each field is passed as a prop. Asset refs are <code className="rounded bg-slate-100 px-1">AssetRef</code> objects with <code className="rounded bg-slate-100 px-1">url</code> and <code className="rounded bg-slate-100 px-1">alt</code>.</p>
   )}
        {templateType === 'WebComponent' && (
        <p>Field values are passed as HTML attributes. Use <code className="rounded bg-slate-100 px-1">this.getAttribute('fieldHandle')</code> inside your custom element.</p>
      )}
     </div>
          )}

          <div className="rounded-lg border border-amber-200 bg-amber-50 p-3 text-xs text-amber-700">
      ⚠️ Removing or renaming fields may break existing Component Items.
          </div>
        </div>
      </div>
  </div>
  );
}

// ─── Thumbnail Editor ─────────────────────────────────────────────────────────

function ThumbnailEditor({
  value,
  onChange,
  componentName,
}: {
  value: string | null;
  onChange: (v: string | null) => void;
  componentName: string;
}) {
  const [dragOver, setDragOver] = useState(false);

  function readFile(file: File) {
    if (!file.type.startsWith('image/') && file.type !== 'image/svg+xml') {
      toast.error('Please upload an image or SVG file.');
      return;
    }
    if (file.size > 512 * 1024) {
      toast.error('File must be under 512 KB. Use a small wireframe SVG for best results.');
      return;
    }
    const reader = new FileReader();
    reader.onload = () => onChange(reader.result as string);
    reader.readAsDataURL(file);
  }

  function handleDrop(e: React.DragEvent) {
    e.preventDefault();
    setDragOver(false);
    const file = e.dataTransfer.files[0];
    if (file) readFile(file);
  }

  return (
    <div className="card space-y-4">
      <div>
        <h3 className="text-sm font-semibold text-slate-900">Wireframe / Thumbnail</h3>
        <p className="mt-1 text-xs text-slate-500">
          Upload a small wireframe or screenshot (SVG recommended, max 512 KB). This image is shown
          in the component library browser and the page-designer palette — it is <em>not</em> the rendered output.
        </p>
      </div>

      <label
        className={`flex min-h-48 cursor-pointer flex-col items-center justify-center gap-3 rounded-xl border-2 border-dashed transition-colors ${
          dragOver ? 'border-brand-400 bg-brand-50' : 'border-slate-200 bg-slate-50 hover:border-brand-300'
        }`}
        onDragOver={(e) => { e.preventDefault(); setDragOver(true); }}
        onDragLeave={() => setDragOver(false)}
        onDrop={handleDrop}
      >
        <input
          type="file"
          accept="image/*,.svg"
          className="sr-only"
          onChange={(e) => { const f = e.target.files?.[0]; if (f) readFile(f); }}
        />
        {value ? (
          <img
            src={value}
            alt={`${componentName} thumbnail`}
            className="h-full w-full rounded object-cover"
          />
        ) : (
          <>
            <svg className="h-10 w-10 text-slate-300" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5}
                d="M4 16l4.586-4.586a2 2 0 012.828 0L16 16m-2-2l1.586-1.586a2 2 0 012.828 0L20 14m-6-6h.01M6 20h12a2 2 0 002-2V6a2 2 0 00-2-2H6a2 2 0 00-2 2v12a2 2 0 002 2z" />
            </svg>
            <div className="text-center">
              <p className="text-sm font-medium text-slate-600">Drop an image or SVG here</p>
              <p className="text-xs text-slate-400">or click to browse · max 512 KB</p>
            </div>
          </>
        )}
      </label>

      {value && (
        <div className="flex items-center gap-2">
          <span className="flex-1 truncate font-mono text-xs text-slate-400">
            {value.substring(0, 60)}…
          </span>
          <button
            type="button"
            onClick={() => onChange(null)}
            className="rounded border border-red-200 bg-red-50 px-3 py-1.5 text-xs font-medium text-red-600 hover:bg-red-100"
          >
            Remove
          </button>
        </div>
      )}
    </div>
  );
}

// ─── Helpers ──────────────────────────────────────────────────────────────────

function getBindingExpression(templateType: RenderingTemplateType, handle: string): string {
  switch (templateType) {
    case 'RazorPartial': return `@Model.${handle.charAt(0).toUpperCase() + handle.slice(1)}`;
    case 'Handlebars':   return `{{${handle}}}`;
    case 'React':  return `{props.${handle}}`;
    case 'WebComponent': return `getAttribute('${handle}')`;
    default:     return handle;
  }
}

function getTemplatePlaceholder(
  templateType: RenderingTemplateType,
  key: string,
  handles: string[],
): string {
  const pascal = key.replace(/(^|-)(.)/, (_, __, c: string) => c.toUpperCase()).replace(/-/g, '');
  switch (templateType) {
    case 'RazorPartial':
   return [
        `@* ${pascal} Component *@`,
      `@model MicroCms.Components.${pascal}Model`,
        '',
        `<div class="${key}">`,
        ...handles.map((h) => `  <p>@Model.${h.charAt(0).toUpperCase() + h.slice(1)}</p>`),
        '</div>',
      ].join('\n');
    case 'Handlebars':
      return [
        `{{! ${pascal} Component }}`,
        `<div class="${key}">`,
        ...handles.map((h) => `  <p>{{${h}}}</p>`),
        '</div>',
      ].join('\n');
    case 'React':
      return [
        `// ${pascal}.tsx`,
        `export default function ${pascal}(props: ${pascal}Props) {`,
        '  return (',
        `    <div className="${key}">`,
     ...handles.map((h) => `      <p>{props.${h}}</p>`),
   '    </div>',
     '  );',
        '}',
      ].join('\n');
    case 'WebComponent':
      return [
        `<!-- ${pascal} Web Component -->`,
        `<style>`,
        `  .${key} { font-family: sans-serif; padding: 1rem; }`,
        `</style>`,
        `<script>`,
        `  class ${pascal} extends HTMLElement {`,
        `    connectedCallback() {`,
        `      this.innerHTML = \``,
        `        <div class="${key}">`,
        ...handles.map((h) => `          <p>\${this.getAttribute('${h}') ?? ''}</p>`),
        `        </div>`,
        `      \`;`,
        `    }`,
        `  }`,
        `  customElements.define('${key}', ${pascal});`,
        `</script>`,
      ].join('\n');
  }
}
