import type { PageTreeNode } from '@/types';

// ─── Helpers ──────────────────────────────────────────────────────────────────

export function flattenTree(nodes: PageTreeNode[], acc: PageTreeNode[] = []): PageTreeNode[] {
  for (const n of nodes) { acc.push(n); flattenTree(n.children, acc); }
  return acc;
}

export function buildBreadcrumb(id: string, flat: PageTreeNode[]): PageTreeNode[] {
  const crumbs: PageTreeNode[] = [];
  let cur = flat.find((p) => p.id === id);
  while (cur) {
    crumbs.unshift(cur);
    cur = cur.parentId ? flat.find((p) => p.id === cur!.parentId) : undefined;
  }
  return crumbs;
}

export const STATUS_DOT: Record<string, string> = {
  Published: 'bg-green-400', Draft: 'bg-slate-300',
  PendingReview: 'bg-amber-400', Archived: 'bg-slate-200',
};
