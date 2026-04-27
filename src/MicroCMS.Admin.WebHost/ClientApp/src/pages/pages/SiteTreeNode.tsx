import { useState } from 'react';
import type { PageTreeNode } from '@/types';

export function SiteTreeNode({ node, selectedId, onSelect, depth = 0 }: {
  node: PageTreeNode;
  selectedId: string;
  onSelect: (id: string) => void;
  depth?: number;
}) {
  const [open, setOpen] = useState(depth < 2);
  const hasChildren = node.children.length > 0;
  const isSelected = node.id === selectedId;

  return (
    <li>
      <div
   className={`group flex items-center gap-1 rounded-md py-1 pr-2 text-xs transition-colors ${isSelected ? 'bg-brand-50 font-semibold text-brand-700' : 'text-slate-600 hover:bg-slate-100'}`}
        style={{ paddingLeft: `${depth * 14 + 6}px` }}
      >
   <button onClick={() => setOpen((o) => !o)}
          className={`flex h-4 w-4 flex-shrink-0 items-center justify-center rounded text-slate-400 ${hasChildren ? 'hover:text-slate-600' : 'invisible'}`}>
          <svg className={`h-3 w-3 transition-transform ${open ? 'rotate-90' : ''}`} fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 5l7 7-7 7" />
          </svg>
        </button>
    <svg className="h-3.5 w-3.5 flex-shrink-0 text-slate-300" fill="none" viewBox="0 0 24 24" stroke="currentColor">
      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M9 12h6m-6 4h4M7 4h10a2 2 0 012 2v14a2 2 0 01-2 2H7a2 2 0 01-2-2V6a2 2 0 012-2z" />
     </svg>
  <button onClick={() => onSelect(node.id)} className="min-w-0 flex-1 truncate text-left">{node.title}</button>
   <span className={`h-1.5 w-1.5 flex-shrink-0 rounded-full ${node.pageType === 'Static' ? 'bg-brand-400' : 'bg-amber-400'}`} />
      </div>
  {open && hasChildren && (
        <ul>
       {node.children.map((child) => (
     <SiteTreeNode key={child.id} node={child} selectedId={selectedId} onSelect={onSelect} depth={depth + 1} />
       ))}
 </ul>
      )}
    </li>
  );
}
