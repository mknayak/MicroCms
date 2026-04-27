import toast from 'react-hot-toast';
import type { ContentType } from '@/types';

const METHOD_COLORS: Record<string, string> = {
  GET: 'bg-blue-100 text-blue-700',
  POST: 'bg-green-100 text-green-700',
  PUT: 'bg-amber-100 text-amber-700',
  DELETE: 'bg-red-100 text-red-700',
};

export function ApiPreviewTab({ contentType }: { contentType: ContentType }) {
  const base = '/api/v1';

  const endpoints = [
    { method: 'GET',    path: `${base}/entries?contentTypeId=${contentType.id}`, description: 'List all entries of this type (paginated)' },
    { method: 'GET',    path: `${base}/entries/{id}`,      description: 'Get a single entry by ID' },
    { method: 'POST',   path: `${base}/entries`,     description: 'Create a new entry' },
    { method: 'PUT',    path: `${base}/entries/{id}`,          description: "Update an entry's fields" },
    { method: 'POST',   path: `${base}/entries/{id}/publish`,   description: 'Publish an entry' },
    { method: 'POST',   path: `${base}/entries/{id}/unpublish`,    description: 'Unpublish an entry' },
    { method: 'DELETE', path: `${base}/entries/{id}`,        description: 'Delete an entry' },
  ];

  return (
    <div className="space-y-6">
      {/* Info banner */}
      <div className="rounded-lg border border-slate-200 bg-white px-5 py-4 text-sm">
        <p className="font-semibold text-slate-900">{contentType.displayName} — REST &amp; GraphQL enabled</p>
        <p className="mt-1 text-slate-500">
          Content type handle:{' '}
  <code className="rounded bg-slate-100 px-1.5 py-0.5 font-mono text-xs">{contentType.handle}</code>
   {' · '}Content type ID:{' '}
          <code className="rounded bg-slate-100 px-1.5 py-0.5 font-mono text-xs">{contentType.id}</code>
      </p>
 </div>

      {/* REST endpoints */}
   <div className="rounded-lg border border-slate-200 bg-white">
      <div className="border-b border-slate-100 px-5 py-4">
   <p className="font-semibold text-slate-900">REST API Endpoints</p>
          <p className="text-xs text-slate-400 mt-0.5">
            Requires a Management or Delivery API key in the Authorization header.
          </p>
        </div>
      <div className="divide-y divide-slate-50">
     {endpoints.map((ep, i) => (
            <div key={i} className="flex items-center gap-4 px-5 py-3 hover:bg-slate-50">
       <span className={`shrink-0 rounded px-2 py-0.5 text-xs font-bold font-mono ${METHOD_COLORS[ep.method]}`}>
 {ep.method}
    </span>
    <code className="flex-1 font-mono text-xs text-slate-700 break-all">{ep.path}</code>
  <span className="shrink-0 text-xs text-slate-400 hidden md:block">{ep.description}</span>
  <button
     onClick={() => { void navigator.clipboard.writeText(ep.path); toast.success('Copied!'); }}
 className="shrink-0 rounded p-1 text-slate-300 hover:text-slate-600"
    aria-label="Copy"
              >
       📋
  </button>
 </div>
          ))}
        </div>
      </div>

{/* GraphQL */}
      <div className="rounded-lg border border-slate-200 bg-white">
     <div className="border-b border-slate-100 px-5 py-4">
          <p className="font-semibold text-slate-900">GraphQL</p>
          <p className="text-xs text-slate-400 mt-0.5">
            Endpoint: <code className="font-mono">/graphql</code>
          </p>
        </div>
        <div className="px-5 py-4 space-y-3">
          <p className="text-xs font-medium text-slate-500 uppercase">Example Query</p>
          <pre className="overflow-x-auto rounded-lg bg-slate-900 p-4 text-xs text-green-300">
{`query {
  entries(
    contentTypeId: "${contentType.id}"
    locale: "en-US"
    status: Published
    first: 10
  ) {
    nodes {
      id
      slug
      fields
      publishedAt
    }
    pageInfo { hasNextPage endCursor }
  }
}`}
          </pre>
        </div>
  </div>
    </div>
  );
}
