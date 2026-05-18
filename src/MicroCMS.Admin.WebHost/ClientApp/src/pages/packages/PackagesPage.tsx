import { Suspense, lazy, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { PageLoader } from '@/components/ui/ErrorBoundary';

const ExportPage = lazy(() => import('@/pages/packages/ExportPage'));
const ImportPage = lazy(() => import('@/pages/packages/ImportPage'));

type Tab = 'export' | 'import';

export default function PackagesPage() {
  const [searchParams, setSearchParams] = useSearchParams();
  const initialTab: Tab = searchParams.get('tab') === 'import' ? 'import' : 'export';
  const [activeTab, setActiveTab] = useState<Tab>(initialTab);

  const switchTab = (tab: Tab) => {
    setActiveTab(tab);
    setSearchParams({ tab }, { replace: true });
  };

  return (
    <div className="flex flex-col h-full">
      {/* Tab bar */}
      <div className="border-b border-slate-200 bg-white px-6 pt-4">
        <nav className="-mb-px flex gap-6" aria-label="Tabs">
          <button
            onClick={() => switchTab('export')}
            className={[
              'flex items-center gap-2 border-b-2 pb-3 text-sm font-medium transition-colors',
              activeTab === 'export'
                ? 'border-brand-600 text-brand-700'
                : 'border-transparent text-slate-500 hover:border-slate-300 hover:text-slate-700',
            ].join(' ')}
          >
            <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2}
                d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-4l-4 4m0 0l-4-4m4 4V4" />
            </svg>
            Export
          </button>
          <button
            onClick={() => switchTab('import')}
            className={[
              'flex items-center gap-2 border-b-2 pb-3 text-sm font-medium transition-colors',
              activeTab === 'import'
                ? 'border-brand-600 text-brand-700'
                : 'border-transparent text-slate-500 hover:border-slate-300 hover:text-slate-700',
            ].join(' ')}
          >
            <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2}
                d="M4 16v1a3 3 0 003 3h10a3 3 0 003-3v-1m-4-8l-4-4m0 0L8 8m4-4v12" />
            </svg>
            Import
          </button>
        </nav>
      </div>

      {/* Tab content */}
      <div className="flex-1 overflow-auto">
        <Suspense fallback={<PageLoader />}>
          {activeTab === 'export' ? <ExportPage /> : <ImportPage />}
        </Suspense>
      </div>
    </div>
  );
}
