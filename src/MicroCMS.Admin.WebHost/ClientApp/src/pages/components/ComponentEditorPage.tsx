import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm, useFieldArray } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import toast from 'react-hot-toast';
import { componentsApi } from '@/api/components';
import { ApiError } from '@/api/client';
import type { ComponentCategory, ComponentFieldType, RenderingTemplateType } from '@/types';

// ─── Constants ────────────────────────────────────────────────────────────────

const ZONE_OPTIONS = [
{ key: 'hero-zone', desc: 'Primary above-fold area' },
  { key: 'features-zone', desc: 'Feature / benefit blocks' },
  { key: 'content-zone', desc: 'Main body content area' },
  { key: 'media-zone', desc: 'Visual / gallery section' },
  { key: 'testimonials-zone', desc: 'Social proof section' },
  { key: 'cta-zone', desc: 'Call-to-action section' },
  { key: 'header-zone', desc: 'Page header' },
  { key: 'footer-zone', desc: 'Page footer' },
];

const FIELD_TYPES: ComponentFieldType[] = [
  'ShortText', 'LongText', 'RichText', 'Number', 'Boolean',
  'DateTime', 'URL', 'AssetRef', 'EntryRef', 'JSON', 'ComponentRef',
];

const CATEGORIES: ComponentCategory[] = [
  'Layout', 'Content', 'Media', 'Navigation', 'Interactive', 'Commerce',
];

const TEMPLATE_TYPES: { value: RenderingTemplateType; label: string; ext: string }[] = [
  { value: 'Handlebars',   label: 'Handlebars (.hbs)', ext: '.hbs'   },
  { value: 'WebComponent', label: 'HTML (.html)',       ext: '.html'  },
];

// ─── Schema ───────────────────────────────────────────────────────────────────

const fieldSchema = z.object({
  id: z.string().optional(),
  handle: z.string().min(1, 'Handle required').regex(/^[a-zA-Z][a-zA-Z0-9_]*$/, 'camelCase only'),
  label: z.string().min(1, 'Label required'),
  fieldType: z.enum(FIELD_TYPES as [ComponentFieldType, ...ComponentFieldType[]]),
  isRequired: z.boolean(),
  isLocalized: z.boolean(),
  isIndexed: z.boolean(),
  sortOrder: z.number(),
  description: z.string().optional(),
});

const formSchema = z.object({
  name: z.string().min(1, 'Name required').max(200),
  key: z.string().min(1, 'Key required').regex(/^[a-z0-9-]+$/),
  description: z.string().optional(),
  category: z.enum(CATEGORIES as [ComponentCategory, ...ComponentCategory[]]),
  zones: z.array(z.string()).min(1, 'At least one zone required'),
  fields: z.array(fieldSchema),
});

type EditorForm = z.infer<typeof formSchema>;
type ActiveTab = 'fields' | 'zones' | 'template' | 'preview' | 'meta';

// ─── Page ─────────────────────────────────────────────────────────────────────

export default function ComponentEditorPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const qc = useQueryClient();
  const [activeTab, setActiveTab] = useState<ActiveTab>('fields');

  // Template state — managed separately from the schema form
  const [templateType, setTemplateType] = useState<RenderingTemplateType>('RazorPartial');
  const [templateContent, setTemplateContent] = useState('');

  const { data: comp, isLoading } = useQuery({
 queryKey: ['component', id],
    queryFn: () => componentsApi.getById(id!),
    enabled: !!id,
  });

  const {
    register, control, handleSubmit, watch, setValue, reset,
    formState: { errors, isDirty },
  } = useForm<EditorForm>({
    resolver: zodResolver(formSchema),
    defaultValues: { name: '', key: '', description: '', category: 'Layout', zones: [], fields: [] },
  });

  useEffect(() => {
    if (comp) {
      reset({
        name: comp.name,
        key: comp.key,
        description: comp.description ?? '',
        category: comp.category,
        zones: comp.zones,
        fields: comp.fields.map((f) => ({
          id: f.id,
     handle: f.handle,
   label: f.label,
          fieldType: f.fieldType,
          isRequired: f.isRequired,
          isLocalized: f.isLocalized,
isIndexed: f.isIndexed,
          sortOrder: f.sortOrder,
          description: f.description ?? '',
     })),
      });
    setTemplateType(comp.templateType);
      setTemplateContent(comp.templateContent ?? '');
    }
  }, [comp, reset]);

  const { fields, append, remove } = useFieldArray({ control, name: 'fields' });
  const selectedZones = watch('zones') ?? [];

  const toggleZone = (zone: string) => {
    setValue(
      'zones',
   selectedZones.includes(zone)
        ? selectedZones.filter((z) => z !== zone)
        : [...selectedZones, zone],
      { shouldDirty: true },
    );
  };

  // ── Schema save ────────────────────────────────────────────────────────────
  const updateMutation = useMutation({
    mutationFn: (data: EditorForm) =>
      componentsApi.update(id!, {
        name: data.name,
        description: data.description,
    category: data.category,
        zones: data.zones,
      fields: data.fields.map((f, i) => ({
          id: f.id ?? crypto.randomUUID(),
          handle: f.handle,
  label: f.label,
       fieldType: f.fieldType,
isRequired: f.isRequired,
          isLocalized: f.isLocalized,
          isIndexed: f.isIndexed,
        sortOrder: i,
   description: f.description,
        })),
      }),
    onSuccess: () => {
      toast.success('Component definition saved.');
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
  const TABS: { key: ActiveTab; label: string }[] = [
    { key: 'fields',   label: `Fields (${fields.length})` },
    { key: 'zones',    label: 'Zones' },
    { key: 'template', label: 'Template' },
    { key: 'preview',  label: 'Preview' },
    { key: 'meta',     label: 'Meta' },
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
          ) : (
    <button type="button" className="btn-primary"
    disabled={!isDirty || updateMutation.isPending}
    onClick={handleSubmit((v) => updateMutation.mutate(v))}>
     {updateMutation.isPending ? 'Saving…' : 'Save Definition'}
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

          {/* ── Fields ── */}
          {activeTab === 'fields' && (
            <div className="card overflow-hidden">
              <div className="grid grid-cols-[1fr_140px_70px_70px_70px_36px] gap-2 border-b border-slate-200 bg-slate-50 px-3 py-2 text-[10px] font-bold uppercase tracking-wider text-slate-400">
     <span>Field Handle</span><span>Type</span>
                <span className="text-center">Required</span>
    <span className="text-center">Localised</span>
       <span className="text-center">Indexed</span>
      <span />
       </div>
          {fields.length === 0 && (
         <p className="px-4 py-6 text-center text-sm text-slate-400">No fields yet.</p>
       )}
     {fields.map((field, index) => (
            <div key={field.id}
 className="grid grid-cols-[1fr_140px_70px_70px_70px_36px] items-center gap-2 border-b border-slate-100 px-3 py-2.5 last:border-0 hover:bg-slate-50">
    <input {...register(`fields.${index}.handle`)}
    className="w-full rounded border border-transparent bg-transparent px-2 py-1 font-mono text-sm font-semibold text-slate-800 hover:border-slate-200 focus:border-brand-500 focus:outline-none"
            placeholder="fieldName" />
   <select {...register(`fields.${index}.fieldType`)}
     className="rounded border border-slate-200 bg-white px-2 py-1 text-xs focus:outline-none focus:border-brand-500">
       {FIELD_TYPES.map((t) => <option key={t} value={t}>{t}</option>)}
        </select>
         <div className="flex justify-center">
       <input type="checkbox" {...register(`fields.${index}.isRequired`)} className="accent-brand-600" />
     </div>
      <div className="flex justify-center">
  <input type="checkbox" {...register(`fields.${index}.isLocalized`)} className="accent-brand-600" />
            </div>
      <div className="flex justify-center">
       <input type="checkbox" {...register(`fields.${index}.isIndexed`)} className="accent-brand-600" />
 </div>
<button type="button" onClick={() => remove(index)}
   className="flex h-7 w-7 items-center justify-center rounded text-slate-300 hover:bg-red-50 hover:text-red-500">
      <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
             <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M6 18L18 6M6 6l12 12" />
                    </svg>
                  </button>
          </div>
  ))}
           <div className="border-t border-dashed border-slate-200 bg-slate-50 px-3 py-2">
   <p className="mb-2 text-xs font-semibold text-slate-400">Add field</p>
            <div className="flex flex-wrap gap-1.5">
      {FIELD_TYPES.map((ft) => (
   <button key={ft} type="button"
            onClick={() => append({ handle: '', label: ft, fieldType: ft, isRequired: false, isLocalized: false, isIndexed: false, sortOrder: fields.length })}
        className="rounded border border-slate-200 bg-white px-2 py-1 text-xs font-medium text-slate-600 hover:border-brand-400 hover:text-brand-600">
            {ft}
       </button>
      ))}
      </div>
         </div>
    </div>
      )}

          {/* ── Zones ── */}
          {activeTab === 'zones' && (
            <div className="card space-y-2">
          <h3 className="mb-3 text-sm font-semibold text-slate-800">Allowed Zones</h3>
  {ZONE_OPTIONS.map(({ key: zone, desc }) => (
    <label key={zone}
      className={`flex cursor-pointer items-center gap-3 rounded-lg border-2 px-4 py-3 transition-colors ${
        selectedZones.includes(zone)
                 ? 'border-brand-500 bg-brand-50'
          : 'border-slate-200 bg-white hover:border-slate-300'
      }`}>
         <input type="checkbox" className="accent-brand-600"
            checked={selectedZones.includes(zone)} onChange={() => toggleZone(zone)} />
<div>
   <span className="text-sm font-semibold text-slate-700">{zone}</span>
          <span className="ml-2 text-xs text-slate-400">{desc}</span>
      </div>
   </label>
   ))}
              {errors.zones && <p className="form-error mt-2">{errors.zones.message}</p>}
   </div>
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
      placeholder={getTemplatePlaceholder(templateType, comp.key, fields.map((f) => f.handle).filter(Boolean))}
  className="block min-h-[420px] w-full resize-y bg-[#1e1e2e] p-4 font-mono text-sm leading-relaxed text-[#cdd6f4] placeholder-[#585b70] focus:outline-none"
              />

              {/* Bindings reference */}
   {fields.length > 0 && (
    <div className="border-t border-slate-200 bg-slate-50 px-4 py-3">
  <p className="mb-2 text-[11px] font-bold uppercase tracking-wider text-slate-400">
        Available bindings
        </p>
  <div className="flex flex-wrap gap-2">
 {fields.map((f) => f.handle && (
    <span key={f.id}
                className="inline-flex items-center gap-1 rounded border border-slate-200 bg-white px-2 py-0.5 font-mono text-xs text-brand-600"
      title={f.fieldType}>
     {getBindingExpression(templateType, f.handle)}
     <span className="text-[10px] text-slate-400">({f.fieldType})</span>
        </span>
     ))}
   </div>
 </div>
        )}
            </div>
          )}

      {/* ── Preview ── */}
   {activeTab === 'preview' && (
    <div className="overflow-hidden rounded-xl border border-slate-200 bg-white">
          <div className="flex items-center gap-2 border-b border-slate-200 bg-slate-50 px-4 py-2.5">
             <span className="flex-1 text-xs font-semibold text-slate-500">
     Live Preview — rendered from template + sample item data
    </span>
         <span className="rounded bg-amber-100 px-2 py-0.5 text-[10px] font-semibold text-amber-700">
           {templateType}
         </span>
        </div>
        <div className="p-6">
        {templateContent.trim() ? (
    <TemplatePreview
        templateType={templateType}
      templateContent={templateContent}
      fields={fields.map((f) => ({ handle: f.handle, fieldType: f.fieldType, label: f.label }))}
       />
     ) : (
       <div className="flex flex-col items-center py-12 text-slate-400">
           <svg className="mb-3 h-10 w-10" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5}
   d="M10 20l4-16m4 4l4 4-4 4M6 16l-4-4 4-4" />
  </svg>
             <p className="text-sm">No template yet. Add your template in the Template tab.</p>
      </div>
          )}
              </div>
         </div>
  )}

  {/* ── Meta ── */}
      {activeTab === 'meta' && (
            <div className="card space-y-4">
  <div className="grid grid-cols-2 gap-4">
      <div>
<label className="form-label">Display Name</label>
             <input className="form-input mt-1" {...register('name')} />
   {errors.name && <p className="form-error">{errors.name.message}</p>}
        </div>
       <div>
        <label className="form-label">Key</label>
  <div className="mt-1 flex items-center rounded-lg border border-slate-200 focus-within:border-brand-500">
          <span className="rounded-l-lg border-r border-slate-200 bg-slate-50 px-2.5 py-2 text-xs text-slate-500">
       comp/
        </span>
    <input className="flex-1 rounded-r-lg px-2.5 py-2 font-mono text-sm focus:outline-none"
        {...register('key')} />
        </div>
                  {errors.key && <p className="form-error">{errors.key.message}</p>}
    </div>
         </div>
  <div>
      <label className="form-label">Category</label>
    <select className="form-input mt-1" {...register('category')}>
   {CATEGORIES.map((c) => <option key={c} value={c}>{c}</option>)}
    </select>
              </div>
              <div>
             <label className="form-label">Description</label>
            <textarea className="form-input mt-1" {...register('description')} rows={3} />
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
     <p>Use <code className="rounded bg-slate-100 px-1">{'{{fieldHandle}}'}</code> to output values. Triple-stash <code className="rounded bg-slate-100 px-1">{'{{{}}}' }</code> for unescaped HTML.</p>
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
        `<template id="${key}-template">`,
`  <div class="${key}">`,
        ...handles.map((h) => `    <p id="${h}"></p>`),
      '  </div>',
      '</template>',
   '<script>',
        `  class ${pascal} extends HTMLElement {`,
        '    connectedCallback() {',
        ...handles.map((h) => `      this.querySelector('#${h}').textContent = this.getAttribute('${h}') ?? '';`),
     '    }',
        '  }',
        `  customElements.define('${key}', ${pascal});`,
        '</script>',
      ].join('\n');
  }
}

// ─── Template Preview ─────────────────────────────────────────────────────────

function TemplatePreview({
  templateType,
  templateContent,
  fields,
}: {
  templateType: RenderingTemplateType;
  templateContent: string;
  fields: { handle: string; fieldType: string; label: string }[];
}) {
  if (templateType === 'Handlebars') {
    // Client-side preview: replace {{handle}} with sample values
    const sample = Object.fromEntries(
      fields.map((f) => [f.handle, getSampleValue(f.fieldType, f.label)]),
    );
    let preview = templateContent;
    for (const [k, v] of Object.entries(sample)) {
      preview = preview.replace(new RegExp(`\\{\\{${k}\\}\\}`, 'g'), String(v));
    }
    return (
      <div
    className="prose prose-sm max-w-none"
dangerouslySetInnerHTML={{ __html: preview }}
      />
    );
  }

  // For Razor / React / Web Component — show source with bindings highlighted
  return (
    <div className="space-y-3">
 <div className="rounded-lg border border-blue-100 bg-blue-50 px-3 py-2 text-xs text-blue-700">
  Server-side rendering preview is not available in the editor.
    The template will be rendered by the {templateType} engine at runtime.
      </div>
      <pre className="overflow-auto rounded-lg bg-slate-900 p-4 text-xs leading-relaxed text-slate-300">
        {templateContent}
      </pre>
    </div>
  );
}

function getSampleValue(fieldType: string, label: string): string | number | boolean {
  switch (fieldType) {
    case 'Number':   return 0.5;
    case 'Boolean':  return true;
    case 'DateTime': return new Date().toLocaleDateString();
    case 'URL':    return 'https://example.com';
    case 'AssetRef': return '/sample-image.jpg';
    default:         return `Sample ${label}`;
  }
}
