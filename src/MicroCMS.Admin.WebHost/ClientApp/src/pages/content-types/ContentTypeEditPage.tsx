import { useEffect, useState } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { useForm, useFieldArray } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { contentTypesApi } from '@/api/contentTypes';
import { siteTemplatesApi } from '@/api/siteTemplates';
import { ApiError } from '@/api/client';
import { fieldRowSchema, toCamelCase } from '@/components/fields/fieldConstants';
import ContentTypeFieldEditor from '@/components/fields/ContentTypeFieldEditor';

// ─── Schema ───────────────────────────────────────────────────────────────────

const API_KEY_REGEX = /^[a-z0-9][a-z0-9_-]*[a-z0-9]$|^[a-z0-9]$/;

const formSchema = z.object({
    name: z.string().min(1, 'Name is required').max(200),
    apiKey: z.string().min(1, 'API key is required').max(64).regex(API_KEY_REGEX, 'Lowercase, digits, hyphens only'),
    description: z.string().max(500).optional(),
    localizationMode: z.enum(['PerLocale', 'Shared']),
    kind: z.enum(['Content', 'Page'] as const),
    siteTemplateId: z.string().optional(),
    fields: z.array(fieldRowSchema),
});

type FormValues = z.infer<typeof formSchema>;

type EditorTab = 'general' | 'page-settings';

// ─── Page ─────────────────────────────────────────────────────────────────────

export default function ContentTypeEditPage() {
    const { id } = useParams();
    const isNew = !id;
    const navigate = useNavigate();
    const qc = useQueryClient();
    const [activeFieldIdx, setActiveFieldIdx] = useState<number | null>(null);
    const [activeTab, setActiveTab] = useState<EditorTab>('general');

    const { data: existing } = useQuery({
        queryKey: ['content-types', id],
        queryFn: () => contentTypesApi.getById(id!),
        enabled: !isNew,
    });

    const { data: siteTemplates } = useQuery({
        queryKey: ['site-templates'],
        queryFn: () => siteTemplatesApi.list(),
    });

    const {
        register, control, handleSubmit, watch, setValue,
        formState: { errors, isSubmitting },
    } = useForm<FormValues>({
        resolver: zodResolver(formSchema),
        defaultValues: {
            name: '', apiKey: '', localizationMode: 'PerLocale',
            kind: 'Content', siteTemplateId: '', fields: [],
        },
    });

    const { fields, append, remove, move } = useFieldArray({ control, name: 'fields' });

    useEffect(() => {
        if (existing) {
            setValue('name', existing.displayName);
            setValue('apiKey', existing.handle);
            setValue('description', existing.description ?? '');
            setValue('localizationMode', existing.localizationMode === 'Shared' ? 'Shared' : 'PerLocale');
            setValue('kind', (existing.kind === 'Page' ? 'Page' : 'Content') as 'Content' | 'Page');
            setValue('siteTemplateId', existing.siteTemplateId ?? '');
            setValue('fields', existing.fields.map((f) => ({
                id: f.id, name: f.label,
                fieldType: f.fieldType as FormValues['fields'][number]['fieldType'],
                isRequired: f.isRequired, isLocalized: f.isLocalized,
                isIndexed: f.isIndexed, isUnique: f.isUnique,
                isList: f.isList,
                description: f.description ?? '',
                enumMode: f.options && f.options.length > 0 ? 'static' : (f.dynamicSource ? 'dynamic' : 'static'),
                optionsText: (f.options ?? []).join('\n'),
                referenceHandle: f.fieldType === 'Component'
                    ? (f.componentSource?.componentKey ?? '')
                    : (f.dynamicSource?.contentTypeHandle ?? ''),
            })));
        }
    }, [existing, setValue]);

    const nameValue = watch('name');
    const kindValue = watch('kind');

    useEffect(() => {
        if (isNew && nameValue) {
            setValue('apiKey', nameValue.toLowerCase().replace(/[^a-z0-9]+/g, '-').replace(/^-+|-+$/g, '') || 'my-type');
        }
    }, [nameValue, isNew, setValue]);

    const mutation = useMutation({
        mutationFn: async (values: FormValues) => {
            if (isNew) {
                const created = await contentTypesApi.create({
                    handle: values.apiKey,
                    displayName: values.name, description: values.description,
                    localizationMode: values.localizationMode, kind: values.kind,
                });
                if (values.fields.length === 0) return created;
                return contentTypesApi.update(created.id, {
                    displayName: values.name, description: values.description,
                    localizationMode: values.localizationMode, kind: values.kind,
                    fields: values.fields.map((f, idx) => ({
                        handle: toCamelCase(f.name) || `field${idx}`,
                        label: f.name, fieldType: f.fieldType, isRequired: f.isRequired,
                        isLocalized: f.isLocalized, isUnique: f.isUnique,
                        isIndexed: f.isIndexed, isList: f.isList, sortOrder: idx,
                        options:
                            f.fieldType === 'Enum' && f.enumMode === 'static' && f.optionsText?.trim()
                              ? f.optionsText.trim().split('\n').map((s) => s.trim()).filter(Boolean)
                              : undefined,
                        dynamicSource:
                            (f.fieldType === 'Enum' && f.enumMode === 'dynamic' && f.referenceHandle?.trim()) ||
                            (f.fieldType === 'Reference' && f.referenceHandle?.trim())
                              ? { contentTypeHandle: f.referenceHandle!.trim(), labelField: '', valueField: '', statusFilter: 'Published' }
                              : undefined,
                        componentSourceKey:
                            f.fieldType === 'Component' && f.referenceHandle?.trim()
                              ? f.referenceHandle.trim()
                              : undefined,
                    })),
                });
            }
            return contentTypesApi.update(id!, {
                displayName: values.name, description: values.description,
                localizationMode: values.localizationMode,
                kind: values.kind,
                siteTemplateId: values.kind === 'Page' ? (values.siteTemplateId || undefined) : undefined,
                fields: values.fields.map((f, idx) => ({
                    id: f.id, handle: toCamelCase(f.name) || `field${idx}`,
                    label: f.name, fieldType: f.fieldType, isRequired: f.isRequired,
                    isLocalized: f.isLocalized, isUnique: f.isUnique,
                    isIndexed: f.isIndexed, isList: f.isList, sortOrder: idx,
                    options:
                        f.fieldType === 'Enum' && f.enumMode === 'static' && f.optionsText?.trim()
                          ? f.optionsText.trim().split('\n').map((s) => s.trim()).filter(Boolean)
                          : undefined,
                    dynamicSource:
                        (f.fieldType === 'Enum' && f.enumMode === 'dynamic' && f.referenceHandle?.trim()) ||
                        (f.fieldType === 'Reference' && f.referenceHandle?.trim())
                          ? { contentTypeHandle: f.referenceHandle!.trim(), labelField: '', valueField: '', statusFilter: 'Published' }
                          : undefined,
                    componentSourceKey:
                        f.fieldType === 'Component' && f.referenceHandle?.trim()
                          ? f.referenceHandle.trim()
                          : undefined,
                })),
            });
        },
        onSuccess: () => {
            toast.success(isNew ? 'Content type created.' : 'Content type updated.');
            void qc.invalidateQueries({ queryKey: ['content-types'] });
            navigate('/content-types');
        },
        onError: (err) => {
            toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : (err as Error).message ?? 'Save failed.');
        },
    });

    const addField = () => {
        append({ name: '', fieldType: 'ShortText', isRequired: false, isLocalized: false, isIndexed: false, isUnique: false, isList: false, description: '', enumMode: 'static', optionsText: '', referenceHandle: '' });
        setActiveFieldIdx(fields.length);
    };

    return (
        <div className="space-y-6">
            {/* Header */}
            <div className="flex items-center gap-3">
                <button onClick={() => navigate('/content-types')} className="btn-secondary">← Back</button>
                <h1 className="text-2xl font-bold text-slate-900">
                    {isNew ? 'New Content Type' : `Edit: ${existing?.displayName ?? '…'}`}
                </h1>
            </div>

            {/* Tabs */}
            {!isNew && (
                <div className="flex gap-1 border-b border-slate-200">
                    {([
                        { key: 'general', label: 'General' },
                        { key: 'page-settings', label: '📄 Page Settings' },
                    ] as { key: EditorTab; label: string }[]).map((tab) => (
                        <button
                            key={tab.key}
                            onClick={() => setActiveTab(tab.key)}
                            className={`px-4 py-2 text-sm font-medium transition-colors ${activeTab === tab.key ? 'border-b-2 border-brand-600 text-brand-700' : 'text-slate-500 hover:text-slate-700'}`}
                        >
                            {tab.label}
                        </button>
                    ))}
                </div>
            )}

            <form onSubmit={handleSubmit((v) => mutation.mutate(v))} className="space-y-6">

                {/* ── GENERAL TAB ───────────────────────────────────────────────── */}
                {(activeTab === 'general' || isNew) && (
                    <>
                        {/* Basic info */}
                        <div className="card space-y-4">
                            <h2 className="text-base font-semibold text-slate-900">Basic Information</h2>
                            <div className="grid grid-cols-2 gap-4">
                                <div>
                                    <label className="form-label">Display Name</label>
                                    <input className="form-input mt-1" {...register('name')} placeholder="Blog Post" />
                                    {errors.name && <p className="form-error">{errors.name.message}</p>}
                                </div>
                                <div>
                                    <label className="form-label">API Key</label>
                                    <div className="mt-1 flex">
                                        <span className="inline-flex items-center rounded-l-lg border border-r-0 border-slate-300 bg-slate-50 px-3 text-xs text-slate-500 select-none">api/v1/</span>
                                        <input className="form-input rounded-l-none font-mono" {...register('apiKey')} placeholder="blog-post" />
                                    </div>
                                    {errors.apiKey && <p className="form-error">{errors.apiKey.message}</p>}
                                </div>
                            </div>
                            <div>
                                <label className="form-label">Description (optional)</label>
                                <input className="form-input mt-1" {...register('description')} placeholder="Describe this content type…" />
                            </div>
                            <div className="grid grid-cols-2 gap-4">
                                <div>
                                    <label className="form-label">Localization Mode</label>
                                    <select className="form-input mt-1" {...register('localizationMode')}>
                                        <option value="PerLocale">Per-locale fields</option>
                                        <option value="Shared">Shared (locale-independent)</option>
                                    </select>
                                </div>
                                <div>
                                    <label className="form-label">Kind</label>
                                    <select className="form-input mt-1" {...register('kind')}>
                                        <option value="Content">Content — standard headless entry</option>
                                        <option value="Page">Page — linked to a site page</option>
                                    </select>
                                    <p className="mt-1 text-xs text-slate-400">Page-kind entries trigger the page creation wizard on new entry.</p>
                                </div>
                            </div>
                        </div>

                        {/* Fields */}
                        <div className="card space-y-4">
                            <div className="flex items-center justify-between">
                                <h2 className="text-base font-semibold text-slate-900">Fields</h2>
                                <button type="button" onClick={addField} className="btn-secondary text-xs">+ Add Field</button>
                            </div>
                            <ContentTypeFieldEditor
                                fieldArray={{ fields, append, remove, move }}
                                register={register}
                                errors={errors}
                                watch={watch}
                                activeFieldIdx={activeFieldIdx}
                                onActiveFieldChange={setActiveFieldIdx}
                            />
                        </div>
                    </>
                )}

                {/* ── PAGE SETTINGS TAB ─────────────────────────────────────────── */}
                {activeTab === 'page-settings' && !isNew && (
                    <div className="card space-y-5">
                        <div>
                            <h2 className="text-base font-semibold text-slate-900">Page Settings</h2>
                            <p className="mt-1 text-sm text-slate-500">Configure how entries of this type behave as pages.</p>
                        </div>

                        {/* Kind toggle */}
                        <div>
                            <label className="form-label">Content Kind</label>
                            <div className="mt-2 grid grid-cols-2 gap-3">
                                {(['Content', 'Page'] as const).map((k) => (
                                    <label key={k} className={`flex cursor-pointer flex-col rounded-lg border-2 p-3 transition-colors ${kindValue === k ? 'border-brand-500 bg-brand-50' : 'border-slate-200 hover:border-slate-300'}`}>
                                        <input type="radio" value={k} {...register('kind')} className="sr-only" />
                                        <span className="text-sm font-semibold text-slate-800">{k === 'Content' ? '📄 Content' : '🌐 Page'}</span>
                                        <span className="mt-0.5 text-xs text-slate-500">
                                            {k === 'Content' ? 'Standard headless content entry. No page wizard.' : 'Entries trigger the page creation wizard. Linked to a URL/slug.'}
                                        </span>
                                    </label>
                                ))}
                            </div>
                        </div>

                        {/* Template selector — only when kind === Page */}
                        {kindValue === 'Page' && (
                            <div>
                                <label className="form-label">Default Template</label>
                                <select className="form-input mt-1" {...register('siteTemplateId')}>
                                    <option value="">— No default template —</option>
                                    {(siteTemplates ?? []).map((t) => (
                                        <option key={t.id} value={t.id}>{t.name}</option>
                                    ))}
                                </select>
                                <p className="mt-1 text-xs text-slate-400">
                                    Pages of this content type inherit this template. Individual pages can override it.
                                </p>
                            </div>
                        )}

                        {kindValue === 'Page' && (
                            <div className="rounded-lg border border-brand-100 bg-brand-50 px-4 py-3">
                                <p className="text-xs font-semibold text-brand-700">What happens when someone creates an entry of this type?</p>
                                <ul className="mt-2 space-y-1 text-xs text-brand-600">
                                    <li>1. Author creates the entry as usual (filling all fields)</li>
                                    <li>2. When ready, author uses "Create Page" to open the page wizard</li>
                                    <li>3. The wizard asks to create a new page or link to an existing page</li>
                                    <li>4. The page inherits this template and the entry content</li>
                                </ul>
                            </div>
                        )}
                    </div>
                )}

                {/* Actions */}
                <div className="flex justify-end gap-3">
                    <button type="button" onClick={() => navigate('/content-types')} className="btn-secondary">Cancel</button>
                    <button type="submit" disabled={isSubmitting} className="btn-primary">
                        {isSubmitting ? 'Saving…' : isNew ? 'Create Content Type' : 'Save Changes'}
                    </button>
                </div>
            </form>
        </div>
    );
}
