import { useNavigate, useParams, useSearchParams, Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { contentTypesApi } from '@/api/contentTypes';
import { EntriesTab } from './EntriesTab';
import { SchemaTab } from './SchemaTab';
import { LocalizationTab } from './LocalizationTab';
import { ApiPreviewTab } from './ApiPreviewTab';

type Tab = 'entries' | 'schema' | 'localization' | 'api-preview';

export default function ContentTypeDetailPage() {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const [searchParams, setSearchParams] = useSearchParams();
  const activeTab = (searchParams.get('tab') as Tab) ?? 'entries';

  const setTab = (tab: Tab) => setSearchParams({ tab }, { replace: true });

  const { data: contentType, isLoading, error } = useQuery({
    queryKey: ['content-types', id],
    queryFn: () => contentTypesApi.getById(id!),
    enabled: !!id,
  });

  if (isLoading) {
    return (
      <div className="space-y-4">
        <div className="h-20 animate-pulse rounded-xl bg-slate-100" />
        <div className="h-10 animate-pulse rounded-lg bg-slate-100" />
        <div className="h-64 animate-pulse rounded-xl bg-slate-100" />
      </div>
    );
  }

  if (error || !contentType) {
    return (
      <div className="flex flex-col items-center justify-center gap-3 py-24 text-center">
        <p className="font-medium text-slate-600">Content type not found.</p>
        <button onClick={() => navigate('/content-types')} className="btn-secondary">← Back to Content Types</button>
      </div>
    );
  }

  const tabs: { key: Tab; label: string }[] = [
    { key: 'entries', label: 'Entries' },
    { key: 'schema', label: 'Schema' },
    { key: 'localization', label: 'Localization' },
    { key: 'api-preview', label: 'API Preview' },
  ];

  return (
    <div className="space-y-6">
      {/* Page header */}
      <div className="flex items-start justify-between gap-4">
        <div className="flex items-center gap-4">
          <div className="flex h-12 w-12 items-center justify-center rounded-xl bg-slate-100 text-2xl">📋</div>
          <div>
            <div className="flex items-center gap-1 text-xs text-slate-400">
              <Link to="/content-types" className="hover:underline">Content Types</Link>
              <span>/</span>
              <span className="text-slate-700 font-semibold text-xl">{contentType.displayName}</span>
            </div>
            <p className="mt-0.5 text-xs text-slate-400">
              {contentType.fields?.length ?? 0} fields · REST &amp; GraphQL enabled
            </p>
          </div>
        </div>
        <div className="flex items-center gap-2 shrink-0">
          <button onClick={() => setTab('schema')} className="btn-secondary text-sm flex items-center gap-1.5">
            ⚙ Edit Schema
          </button>
          <button onClick={() => navigate(`/entries/new?contentTypeId=${id}`)} className="btn-primary text-sm">
            + New Entry
          </button>
        </div>
      </div>

      {/* Tabs */}
      <div className="border-b border-slate-200">
        <nav className="flex -mb-px">
          {tabs.map((t) => (
            <button
              key={t.key}
              onClick={() => setTab(t.key)}
              className={`flex items-center gap-1.5 border-b-2 px-4 py-3 text-sm font-medium transition-colors ${
                activeTab === t.key
                  ? 'border-brand-600 text-brand-700'
                  : 'border-transparent text-slate-500 hover:border-slate-300 hover:text-slate-700'
              }`}
            >
              {t.label}
              {t.key === 'schema' && (contentType.fields?.length ?? 0) > 0 && (
                <span className="rounded-full bg-slate-100 px-1.5 py-0.5 text-xs text-slate-500">
                  {contentType.fields.length}
                </span>
              )}
            </button>
          ))}
        </nav>
      </div>

      {/* Tab content */}
      <div>
        {activeTab === 'entries' && <EntriesTab contentTypeId={contentType.id} />}
        {activeTab === 'schema' && <SchemaTab contentType={contentType} />}
        {activeTab === 'localization' && <LocalizationTab contentType={contentType} />}
        {activeTab === 'api-preview' && <ApiPreviewTab contentType={contentType} />}
      </div>
    </div>
  );
}
