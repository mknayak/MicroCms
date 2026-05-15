import { useEffect } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useForm, FormProvider } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import toast from 'react-hot-toast';
import { contentTypesApi } from '@/api/contentTypes';
import { siteTemplatesApi } from '@/api/siteTemplates';
import type { ContentType } from '@/types';
import { ApiError } from '@/api/client';
import { schemaFormValidator, DEFAULT_GROUP } from './schemaTab.types';
import type { SchemaFormValues } from './schemaTab.types';
import { toFormFields, toCamelCase } from './schemaTab.helpers';
import { SchemaReadOnlyView } from './SchemaReadOnlyView';
import { SchemaEditView } from './SchemaEditView';
import { useState } from 'react';

export function SchemaTab({ contentType }: { contentType: ContentType }) {
    const qc = useQueryClient();
    const [editing, setEditing] = useState(false);

    const methods = useForm<SchemaFormValues>({
        resolver: zodResolver(schemaFormValidator),
        defaultValues: {
            name: contentType.displayName,
            apiKey: contentType.handle,
            description: contentType.description ?? '',
            localizationMode: contentType.localizationMode === 'Shared' ? 'Shared' : 'PerLocale',
            kind: (contentType.kind === 'Page' ? 'Page' : 'Content') as 'Content' | 'Page',
            siteTemplateId: contentType.siteTemplateId ?? '',
            parentContentTypeId: contentType.parentContentTypeId ?? '',
            fields: toFormFields(contentType),
        },
    });

    const { reset, handleSubmit, formState: { isSubmitting } } = methods;

    useEffect(() => {
        reset({
            name: contentType.displayName,
            apiKey: contentType.handle,
            description: contentType.description ?? '',
            localizationMode: contentType.localizationMode === 'Shared' ? 'Shared' : 'PerLocale',
            kind: (contentType.kind === 'Page' ? 'Page' : 'Content') as 'Content' | 'Page',
            siteTemplateId: contentType.siteTemplateId ?? '',
            parentContentTypeId: contentType.parentContentTypeId ?? '',
            fields: toFormFields(contentType),
        });
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, [contentType.id, contentType.updatedAt]);

    const { data: siteTemplates } = useQuery({
        queryKey: ['site-templates'],
        queryFn: () => siteTemplatesApi.list(),
    });

    const saveMutation = useMutation({
        mutationFn: (values: SchemaFormValues) => {
            const originalParent = contentType.parentContentTypeId ?? '';
            const newParent = values.parentContentTypeId ?? '';
            return contentTypesApi.update(contentType.id, {
                displayName: values.name,
                description: values.description,
                localizationMode: values.localizationMode,
                kind: values.kind,
                siteTemplateId: values.kind === 'Page' && values.siteTemplateId ? values.siteTemplateId : undefined,
                parentContentTypeId: newParent || undefined,
                clearParent: !!originalParent && !newParent,
                fields: values.fields.map((f, idx) => ({
                    id: f.id,
                    handle: toCamelCase(f.name) || `field${idx}`,
                    label: f.name,
                    fieldType: f.type,
                    isRequired: f.required,
                    isLocalized: f.localized,
                    isUnique: f.isUnique,
                    isIndexed: f.isIndexed,
                    isList: f.isList,
                    sortOrder: idx,
                    groupName: f.groupName ?? DEFAULT_GROUP,
                    options: f.type === 'Enum' && f.enumMode === 'static' ? f.staticOptions : undefined,
                    dynamicSource: (f.type === 'Enum' && f.enumMode === 'dynamic') || f.type === 'Reference'
                        ? (f.dynamicSource?.contentTypeHandle?.trim() ? f.dynamicSource : undefined)
                        : undefined,
                    multiListSource: f.type === 'MultiList'
                        ? (f.multiListSource?.contentTypeHandle?.trim() ? f.multiListSource : undefined)
                        : undefined,
                    componentSourceKey: f.type === 'Component' && f.componentSourceKey?.trim()
                        ? f.componentSourceKey.trim()
                        : undefined,
                })),
            });
        },
        onSuccess: () => {
            toast.success('Schema saved.');
            void qc.invalidateQueries({ queryKey: ['content-types'] });
            setEditing(false);
        },
        onError: (err) => {
            toast.error(err instanceof ApiError ? err.problem.detail ?? err.message : 'Save failed.');
        },
    });

    const handleCancel = () => {
        reset();
        setEditing(false);
    };

    // Component-kind types are auto-created backing types — not directly editable.
    // This check is AFTER all hooks to comply with React Rules of Hooks.
    if (contentType.kind === 'Component') {
        return (
            <div className="space-y-4">
                <div className="flex items-start gap-2 rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-800">
                    <span className="mt-0.5 shrink-0">⚙️</span>
                    <div>
                        <p className="font-semibold">Component Backing Type</p>
                        <p className="mt-0.5 text-amber-700">
                            This content type was auto-created to store data for a Component. Its schema
                            is managed through the{' '}
                            <strong>Component Library</strong> — edit the component's fields there instead.
                        </p>
                    </div>
                </div>
            </div>
        );
    }

    if (editing) {
        return (
            <FormProvider {...methods}>
                <form
                    onSubmit={handleSubmit(
                        (v) => saveMutation.mutate(v),
                        (errs) => {
                            console.error('[SchemaTab] Validation errors:', errs);
                            toast.error('Please fix the highlighted fields before saving.');
                        }
                    )}
                >
                    <SchemaEditView
                        contentTypeId={contentType.id}
                        contentTypeName={contentType.displayName}
                        inheritedFields={(contentType.fields ?? []).filter((f) => f.isInherited)}
                        parentHandle={contentType.parentHandle}
                        onCancel={handleCancel}
                        isSubmitting={isSubmitting || saveMutation.isPending}
                    />
                </form>
            </FormProvider>
        );
    }

    return (
        <SchemaReadOnlyView
            contentType={contentType}
            siteTemplates={siteTemplates}
            onEdit={() => setEditing(true)}
        />
    );
}

