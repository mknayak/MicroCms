import { useNavigate } from 'react-router-dom';
import { STATUS_DOT } from './helpers';
import type { PageTreeNode } from '@/types';

export function ChildCard({ page, isSelected, onClick }: {
  page: PageTreeNode;
  isSelected: boolean;
  onClick: () => void;
}) {
  const navigate = useNavigate();
  const typeIcon = page.pageType === 'Static'
    ? 'M9 12h6m-6 4h4M7 4h10a2 2 0 012 2v14a2 2 0 01-2 2H7a2 2 0 01-2-2V6a2 2 0 012-2z'
    : 'M19 11H5m14 0a2 2 0 012 2v6a2 2 0 01-2 2H5a2 2 0 01-2-2v-6a2 2 0 012-2m14 0V9a2 2 0 00-2-2M5 11V9a2 2 0 012-2m0 0V5a2 2 0 012-2h6a2 2 0 012 2v2M7 7h10';

  return (
    <div
      className={`flex flex-col rounded-xl border-2 text-left transition-all hover:shadow-md ${
        isSelected ? 'border-brand-400 bg-brand-50 shadow-md' : 'border-slate-200 bg-white hover:border-brand-200'
      }`}
    >
      {/* Card body — click to select/edit */}
      <button onClick={onClick} className="flex-1 p-4 text-left">
        <div className="mb-3 flex items-start justify-between">
          <div className={`flex h-9 w-9 items-center justify-center rounded-lg ${page.pageType === 'Static' ? 'bg-brand-100' : 'bg-amber-100'}`}>
            <svg className={`h-4 w-4 ${page.pageType === 'Static' ? 'text-brand-600' : 'text-amber-600'}`} fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d={typeIcon} />
            </svg>
          </div>
          <span className={`h-2 w-2 rounded-full ${STATUS_DOT['Published']}`} />
        </div>
        <p className="truncate text-sm font-semibold text-slate-900">{page.title}</p>
        <p className="font-mono text-[11px] text-slate-400">/{page.slug}</p>
        <div className="mt-2 flex items-center justify-between text-[11px] text-slate-400">
          <span>{page.pageType === 'Static' ? 'Static' : 'Collection'}</span>
          <span>{page.children.length} child{page.children.length !== 1 ? 'ren' : ''}</span>
        </div>
      </button>

      {/* Action strip */}
      <div className="flex border-t border-slate-100">
        <button
          onClick={onClick}
          className="flex flex-1 items-center justify-center gap-1 py-2 text-xs text-slate-400 hover:bg-slate-50 hover:text-slate-700 rounded-bl-xl"
        >
          <svg className="h-3 w-3" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
          </svg>
          Edit
        </button>
        <div className="w-px bg-slate-100" />
        <button
          onClick={(e) => { e.stopPropagation(); navigate(`/pages/${page.id}/designer`); }}
          className="flex flex-1 items-center justify-center gap-1 py-2 text-xs text-brand-600 hover:bg-brand-50 rounded-br-xl font-semibold"
        >
          <svg className="h-3 w-3" fill="none" viewBox="0 0 24 24" stroke="currentColor">
            <rect x="3" y="3" width="18" height="18" rx="2" strokeWidth="2" />
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 9h18M9 21V9" />
          </svg>
          Design
        </button>
      </div>
    </div>
  );
}
