export function ScanStatusBadge({ status }: { status: string }) {
  const variants: Record<string, string> = {
    Available:   'bg-emerald-100 text-emerald-700',
    PendingScan: 'bg-amber-100  text-amber-700',
    Uploading:   'bg-blue-100   text-blue-700',
    Quarantined: 'bg-red-100    text-red-700',
    Deleted:     'bg-slate-100  text-slate-500',
  };
  const label: Record<string, string> = {
    Available:   'Available',
    PendingScan: 'Scanning…',
    Uploading:   'Uploading',
    Quarantined: 'Quarantined',
    Deleted:     'Deleted',
  };
  const cls = variants[status] ?? 'bg-slate-100 text-slate-500';
  return (
    <span className={`inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-xs font-medium ${cls}`}>
      {status === 'PendingScan' && (
        <svg className="h-3 w-3 animate-spin" viewBox="0 0 24 24" fill="none">
          <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
          <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z" />
        </svg>
      )}
      {status === 'Quarantined' && (
        <svg className="h-3 w-3" fill="currentColor" viewBox="0 0 20 20">
          <path fillRule="evenodd" d="M8.485 2.495c.673-1.167 2.357-1.167 3.03 0l6.28 10.875c.673 1.167-.17 2.625-1.516 2.625H3.72c-1.347 0-2.189-1.458-1.515-2.625L8.485 2.495zM10 5a.75.75 0 01.75.75v3.5a.75.75 0 01-1.5 0v-3.5A.75.75 0 0110 5zm0 9a1 1 0 100-2 1 1 0 000 2z" clipRule="evenodd" />
        </svg>
      )}
      {label[status] ?? status}
    </span>
  );
}
