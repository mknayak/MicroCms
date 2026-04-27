import { useState, useMemo } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { pagesApi } from '@/api/pages';
import { layoutsApi } from '@/api/layouts';
import { contentTypesApi } from '@/api/contentTypes';
import { useSite } from '@/contexts/SiteContext';
import { flattenTree, buildBreadcrumb } from './helpers';
import { PageDetailPanel } from './PageDetailPanel';
import { CreatePageModal } from './CreatePageModal';
import { SiteTreeNode } from './SiteTreeNode';
import { ChildCard } from './ChildCard';
import type { PageTreeNode } from '@/types';

export default function PagesPage() {
    const { selectedSiteId, selectedSite, isLoading: siteLoading } = useSite();
    const siteId = selectedSiteId ?? '';

    const [selectedId, setSelectedId] = useState('');
    const [detailOpen, setDetailOpen] = useState(false);
    const [createModal, setCreateModal] = useState<{ open: boolean; parentId?: string }>({ open: false });

    const { data: tree = [], isLoading: treeLoading } = useQuery({
        queryKey: ['pages', siteId],
        queryFn: () => pagesApi.getTree(siteId),
        enabled: !!siteId,
    });

    const { data: layouts = [] } = useQuery({
        queryKey: ['layouts', siteId],
        queryFn: () => layoutsApi.list(siteId),
        enabled: !!siteId,
    });

    const { data: ctResult } = useQuery({
        queryKey: ['content-types'],
        queryFn: () => contentTypesApi.list(),
    });
    const contentTypes = ctResult?.items ?? [];

    const flatPages = useMemo(() => flattenTree(tree), [tree]);
    const selectedNode = flatPages.find((p) => p.id === selectedId);
    const breadcrumb = selectedId ? buildBreadcrumb(selectedId, flatPages) : [];
    const displayedChildren: PageTreeNode[] = selectedNode ? selectedNode.children : tree;

    const handleSelectNode = (id: string) => { setSelectedId(id); setDetailOpen(false); };
    const handleCardClick = (page: PageTreeNode) => { setSelectedId(page.id); setDetailOpen(true); };
    const navigate = useNavigate();

    if (siteLoading) return (
        <div className="space-y-3">
            {Array.from({ length: 5 }).map((_, i) => <div key={i} className="h-10 animate-pulse rounded-lg bg-slate-100" />)}
        </div>
    );

    if (!siteId) return (
        <div className="flex flex-col items-center justify-center gap-3 py-24 text-center">
            <p className="text-sm text-slate-500">No site selected. Choose a site from the top bar.</p>
        </div>
    );

    return (
        <>
            {createModal.open && (
                <CreatePageModal siteId={siteId} parentId={createModal.parentId} flatPages={flatPages}
                    contentTypes={contentTypes} onClose={() => setCreateModal({ open: false })} />
            )}

            {/* 3-panel full-viewport layout */}
            <div className="-m-6 flex h-[calc(100vh-4rem)] overflow-hidden">

                {/* LEFT — narrow page tree */}
                <aside className="flex w-52 flex-shrink-0 flex-col overflow-hidden border-r border-slate-200 bg-white">
                    {/* Site badge */}
                    <div className="flex items-center gap-2 border-b border-slate-200 px-3 py-3">
                        <div className="flex h-5 w-5 flex-shrink-0 items-center justify-center rounded bg-brand-600 text-[9px] font-black text-white">
                            {selectedSite?.name?.charAt(0) ?? 'S'}
                        </div>
                        <span className="truncate text-xs font-bold text-slate-700">{selectedSite?.name}</span>
                    </div>
                    {/* Search */}
                    <div className="border-b border-slate-200 px-2 py-2">
                        <div className="relative">
                            <svg className="absolute left-2 top-1/2 h-3 w-3 -translate-y-1/2 text-slate-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                <circle cx="11" cy="11" r="8" strokeWidth="2" />
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M21 21l-4.35-4.35" />
                            </svg>
                            <input className="w-full rounded-md border border-slate-200 py-1.5 pl-7 pr-2 text-xs focus:border-brand-400 focus:outline-none" placeholder="Find page…" />
                        </div>
                    </div>
                    {/* Tree */}
                    <nav className="flex-1 overflow-y-auto px-1 py-2">
                        {treeLoading
                            ? Array.from({ length: 5 }).map((_, i) => <div key={i} className="mx-2 mb-1.5 h-6 animate-pulse rounded bg-slate-100" />)
                            : <ul>{tree.map((node) => <SiteTreeNode key={node.id} node={node} selectedId={selectedId} onSelect={handleSelectNode} />)}</ul>
                        }
                    </nav>
                    {/* Add page */}
                    <div className="border-t border-slate-200 p-2">
                        <button onClick={() => setCreateModal({ open: true })}
                            className="flex w-full items-center justify-center gap-1.5 rounded-md border border-dashed border-slate-300 py-1.5 text-xs text-slate-500 hover:border-brand-400 hover:text-brand-600">
                            <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
                            </svg>
                            Add page
                        </button>
                    </div>
                </aside>

                {/* CENTER — breadcrumb + card grid */}
                <div className="flex min-w-0 flex-1 flex-col overflow-hidden bg-slate-50">
                    {/* Breadcrumb bar */}
                    <div className="flex flex-shrink-0 items-center justify-between border-b border-slate-200 bg-white px-6 py-3">
                        <nav className="flex items-center gap-1.5 text-sm">
                            <button onClick={() => { setSelectedId(''); setDetailOpen(false); }}
                                className="flex items-center gap-1 text-slate-400 hover:text-slate-700">
                                <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 12l2-2m0 0l7-7 7 7M5 10v10a1 1 0 001 1h3m10-11l2 2m-2-2v10a1 1 0 01-1 1h-3m-6 0a1 1 0 001-1v-4a1 1 0 011-1h2a1 1 0 011 1v4a1 1 0 001 1m-6 0h6" />
                                </svg>
                                Home
                            </button>
                            {breadcrumb.map((crumb) => (
                                <span key={crumb.id} className="flex items-center gap-1.5">
                                    <svg className="h-3.5 w-3.5 text-slate-300" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
                                    </svg>
                                    <button onClick={() => handleSelectNode(crumb.id)} className="font-medium text-slate-700 hover:text-brand-600">{crumb.title}</button>
                                </span>
                            ))}
                        </nav>
                        <div className="flex items-center gap-2">
                            {selectedNode && (
                                <button onClick={() => setCreateModal({ open: true, parentId: selectedId })}
                                    className="btn-secondary py-1.5 px-3 text-xs">
                                    <svg className="mr-1 h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
                                    </svg>
                                    Add child
                                </button>
                            )}
                            <button onClick={() => setCreateModal({ open: true })} className="btn-primary py-1.5 px-3 text-xs">
                                <svg className="mr-1 h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
                                </svg>
                                New page
                            </button>
                        </div>
                    </div>

                    {/* Current page context banner */}
                    {selectedNode && (
                        <div className="flex-shrink-0 border-b border-slate-200 bg-white px-6 py-2">
                            <div className="flex items-center justify-between rounded-lg border border-slate-200 bg-slate-50 px-4 py-2.5">
                                <div className="flex items-center gap-3 min-w-0">
                                    <div className="flex h-8 w-8 flex-shrink-0 items-center justify-center rounded-lg bg-brand-100">
                                        <svg className="h-4 w-4 text-brand-600" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12h6m-6 4h4M7 4h10a2 2 0 012 2v14a2 2 0 01-2 2H7a2 2 0 01-2-2V6a2 2 0 012-2z" />
                                        </svg>
                                    </div>
                                    <div className="min-w-0">
                                        <span className="text-[11px] font-bold uppercase tracking-wider text-slate-400">Current Page</span>
                                        <p className="truncate text-sm font-bold text-slate-900">{selectedNode.title}</p>
                                        <p className="font-mono text-xs text-slate-400">/{selectedNode.slug}</p>
                                    </div>
                                </div>
                                <div className="flex flex-shrink-0 items-center gap-2">
                                    <span className="rounded-full bg-green-100 px-2 py-0.5 text-[10px] font-semibold text-green-700">Published</span>
                                    <button onClick={() => setDetailOpen(true)} className="flex items-center gap-1 text-xs font-medium text-brand-600 hover:underline">
                                        Edit
                                        <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M11 5H6a2 2 0 00-2 2v11a2 2 0 002 2h11a2 2 0 002-2v-5m-1.414-9.414a2 2 0 112.828 2.828L11.828 15H9v-2.828l8.586-8.586z" />
                                        </svg>
                                    </button>
                                    <button
                                        onClick={() => navigate(`/pages/${selectedId}/designer`)}
                                        className="flex items-center gap-1 rounded-md border border-brand-200 bg-brand-50 px-2.5 py-1 text-xs font-semibold text-brand-700 hover:bg-brand-100"
                                    >
                                        <svg className="h-3.5 w-3.5" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                            <rect x="3" y="3" width="18" height="18" rx="2" strokeWidth="2" />
                                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 9h18M9 21V9" />
                                        </svg>
                                        Design
                                    </button>
                                </div>
                            </div>
                        </div>
                    )}

                    {/* Card grid */}
                    <div className="flex-1 overflow-y-auto p-6">
                        {treeLoading ? (
                            <div className="grid grid-cols-4 gap-4">
                                {Array.from({ length: 8 }).map((_, i) => <div key={i} className="h-32 animate-pulse rounded-xl bg-slate-200" />)}
                            </div>
                        ) : displayedChildren.length === 0 ? (
                            <div className="flex flex-col items-center justify-center gap-3 py-24 text-center">
                                <svg className="h-10 w-10 text-slate-200" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M9 13h6m-3-3v6m5 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
                                </svg>
                                <p className="text-sm font-medium text-slate-400">No pages here yet</p>
                                <button onClick={() => setCreateModal({ open: true, parentId: selectedId || undefined })} className="btn-primary text-sm">
                                    Add your first page
                                </button>
                            </div>
                        ) : (
                            <>
                                {selectedNode && <p className="mb-3 text-xs font-medium text-slate-400">↓ Child pages ({displayedChildren.length})</p>}
                                <div className="grid grid-cols-2 gap-4 sm:grid-cols-3 xl:grid-cols-4">
                                    {displayedChildren.map((child) => (
                                        <ChildCard key={child.id} page={child}
                                            isSelected={selectedId === child.id && detailOpen}
                                            onClick={() => handleCardClick(child)} />
                                    ))}
                                    {/* Add child card */}
                                    <button
                                        onClick={() => setCreateModal({ open: true, parentId: selectedId || undefined })}
                                        className="flex min-h-[120px] flex-col items-center justify-center gap-2 rounded-xl border-2 border-dashed border-slate-200 p-4 text-slate-400 transition-colors hover:border-brand-300 hover:text-brand-600">
                                        <svg className="h-6 w-6" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                                            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M12 4v16m8-8H4" />
                                        </svg>
                                        <span className="text-xs font-medium">Add {selectedNode ? 'child' : 'root'} page</span>
                                    </button>
                                </div>
                            </>
                        )}
                    </div>
                </div>

                {/* RIGHT — detail panel */}
                {detailOpen && selectedId && (
                    <PageDetailPanel pageId={selectedId} siteId={siteId} layouts={layouts}
                        onClose={() => setDetailOpen(false)} />
                )}
            </div>
        </>
    );
}
