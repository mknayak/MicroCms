import { useCallback, useState } from 'react';
import { useDropzone } from 'react-dropzone';
import { useQuery } from '@tanstack/react-query';
import toast from 'react-hot-toast';
import { packagesApi } from '@/api/packages';
import { tenantsApi } from '@/api/tenants';
import { useSite } from '@/contexts/SiteContext';
import { useAuth } from '@/contexts/AuthContext';
import type {
  PackageAnalysisResult,
  ImportOptions,
  ImportProgress,
  ImportStepResult,
  Site,
} from '@/types';

// ─── Step indicators ──────────────────────────────────────────────────────────

const STEPS = ['Upload & Analyse', 'Configure Import', 'Results'] as const;
type Step = 0 | 1 | 2;

function StepBar({ current }: { current: Step }) {
  return (
    <ol className="flex items-center gap-2">
      {STEPS.map((label, i) => {
   const done = i < current;
  const active = i === current;
 return (
   <li key={label} className="flex items-center gap-2">
       {i > 0 && <div className={`h-px w-8 flex-shrink-0 ${done ? 'bg-brand-500' : 'bg-slate-200'}`} />}
  <div className={`flex h-7 w-7 items-center justify-center rounded-full text-xs font-bold ${
  done    ? 'bg-brand-500 text-white'
  : active ? 'border-2 border-brand-500 text-brand-600'
        : 'border-2 border-slate-200 text-slate-400'
 }`}>
     {done ? '✓' : i + 1}
     </div>
       <span className={`text-sm font-medium ${active ? 'text-slate-900' : done ? 'text-brand-600' : 'text-slate-400'}`}>
    {label}
          </span>
  </li>
    );
      })}
    </ol>
  );
}

// ─── Category toggle row ──────────────────────────────────────────────────────

interface ImportCategory {
  key: keyof Omit<ImportOptions, 'conflictResolution'>;
  label: string;
  icon: string;
  adminOnly?: boolean;
}

const IMPORT_CATEGORIES: ImportCategory[] = [
  { key: 'importContentTypes',  label: 'Content Types',  icon: '📋' },
  { key: 'importEntries',       label: 'Entries',        icon: '📝' },
  { key: 'importPages',         label: 'Pages',    icon: '🗂️' },
  { key: 'importLayouts',       label: 'Layouts',        icon: '🖼️' },
  { key: 'importComponents',    label: 'Components',     icon: '🧩' },
  { key: 'importMediaMetadata', label: 'Media Metadata', icon: '🖼' },
  { key: 'importSiteSettings',  label: 'Site Settings',  icon: '⚙️' },
  { key: 'importUsers',         label: 'Users',          icon: '👤', adminOnly: true },
];

// ─── Stat card ────────────────────────────────────────────────────────────────

function StatCard({
  category, totalInPackage, newItems, existingItems,
}: {
  category: string; totalInPackage: number; newItems: number; existingItems: number;
}) {
  return (
    <div className="rounded-lg border border-slate-200 bg-white p-4 space-y-2">
      <p className="text-sm font-semibold text-slate-800">{category}</p>
      <div className="flex items-center gap-4 text-xs">
        <span className="flex items-center gap-1 text-slate-500">
    <span className="font-medium text-slate-800">{totalInPackage}</span> total
     </span>
        <span className="flex items-center gap-1 text-green-700">
          <span className="font-medium">{newItems}</span> new
    </span>
     <span className="flex items-center gap-1 text-amber-700">
          <span className="font-medium">{existingItems}</span> existing
  </span>
  </div>
      <div className="h-2 w-full overflow-hidden rounded-full bg-slate-100">
     {totalInPackage > 0 && (
  <>
     <div
     className="h-2 rounded-full bg-green-500 inline-block"
   style={{ width: `${(newItems / totalInPackage) * 100}%` }}
  />
         <div
       className="h-2 rounded-full bg-amber-400 inline-block"
   style={{ width: `${(existingItems / totalInPackage) * 100}%` }}
    />
       </>
       )}
      </div>
    </div>
  );
}

// ─── Results step ─────────────────────────────────────────────────────────────

function ResultsView({ progress }: { progress: ImportProgress }) {
  const ok = progress.status === 'Completed';
  const withErrors = progress.status === 'CompletedWithErrors';

  return (
    <div className="space-y-4">
 {/* Status banner */}
      <div className={`flex items-center gap-3 rounded-lg border px-4 py-3 text-sm font-medium ${
        ok         ? 'border-green-200 bg-green-50 text-green-800'
        : withErrors ? 'border-amber-200 bg-amber-50 text-amber-800'
   : 'border-red-200 bg-red-50 text-red-800'
 }`}>
<span className="text-lg">{ok ? '✅' : withErrors ? '⚠️' : '❌'}</span>
   <span>
   {ok
    ? 'Import completed successfully.'
            : withErrors
            ? 'Import completed with some errors — review the details below.'
        : `Import failed: ${progress.errorMessage ?? 'Unknown error.'}`}
  </span>
      </div>

   {/* Per-category results */}
      {progress.stepResults.length > 0 && (
      <div className="space-y-2">
         {progress.stepResults.map((r: ImportStepResult) => (
         <div key={r.category} className="rounded-lg border border-slate-200 bg-white p-4">
     <div className="flex items-center justify-between">
     <p className="text-sm font-semibold text-slate-800">{r.category}</p>
    <div className="flex items-center gap-3 text-xs">
          <span className="text-green-700 font-medium">{r.imported} imported</span>
     <span className="text-amber-700 font-medium">{r.skipped} skipped</span>
<span className="text-brand-700 font-medium">{r.overwritten} overwritten</span>
    {r.failed > 0 && (
  <span className="text-red-700 font-medium">{r.failed} failed</span>
)}
    </div>
    </div>
  {r.errors.length > 0 && (
        <ul className="mt-2 space-y-1">
      {r.errors.map((err, i) => (
              <li key={i} className="text-xs text-red-600 bg-red-50 rounded px-2 py-1">{err}</li>
      ))}
   </ul>
   )}
         </div>
     ))}
     </div>
      )}
    </div>
  );
}

// ─── Page ─────────────────────────────────────────────────────────────────────

export default function ImportPage() {
  const { hasRole } = useAuth();
  const { selectedSiteId, sites } = useSite();
  const isAdmin = hasRole('SystemAdmin') || hasRole('TenantAdmin');

  const { data: tenant } = useQuery({
    queryKey: ['current-tenant'],
    queryFn: tenantsApi.getCurrent,
  });

  const [step, setStep] = useState<Step>(0);
  const [file, setFile] = useState<File | null>(null);
  const [analysis, setAnalysis] = useState<PackageAnalysisResult | null>(null);
  const [progress, setProgress] = useState<ImportProgress | null>(null);
  const [analysing, setAnalysing] = useState(false);
  const [importing, setImporting] = useState(false);

  // Target site
  const [targetSiteId, setTargetSiteId] = useState(selectedSiteId ?? '');
  const importSites: Site[] = tenant?.sites ?? sites;

  // Import options
  const [options, setOptions] = useState<ImportOptions>({
    importContentTypes: true,
  importEntries: true,
  importPages: true,
    importLayouts: true,
    importMediaMetadata: true,
    importComponents: true,
    importUsers: false,
    importSiteSettings: true,
 conflictResolution: 'Skip',
  });

  // ── Dropzone ────────────────────────────────────────────────────────────

  const onDrop = useCallback((accepted: File[]) => {
    if (accepted[0]) { setFile(accepted[0]); setAnalysis(null); setProgress(null); }
  }, []);

  const { getRootProps, getInputProps, isDragActive } = useDropzone({
    onDrop,
    accept: { 'application/zip': ['.zip'], 'application/octet-stream': [] },
    maxFiles: 1,
    maxSize: 512 * 1024 * 1024,
  });

  // ── Analyse ─────────────────────────────────────────────────────────────

  const handleAnalyse = async () => {
    if (!file) { toast.error('Please upload a package ZIP file first.'); return; }
    const tenantId = tenant?.id;
    const siteId   = targetSiteId;
    if (!tenantId || !siteId) { toast.error('Please select a target site.'); return; }

  setAnalysing(true);
    try {
    const result = await packagesApi.analyse(file, tenantId, siteId);
   setAnalysis(result);
      setStep(1);
    } catch {
   toast.error('Failed to analyse package. Make sure it is a valid MicroCMS export.');
    } finally {
      setAnalysing(false);
    }
  };

  // ── Import ──────────────────────────────────────────────────────────────

  const handleImport = async () => {
    if (!file || !tenant?.id || !targetSiteId) return;
    setImporting(true);
    try {
      const result = await packagesApi.import(file, tenant.id, targetSiteId, options);
      setProgress(result);
 setStep(2);
   if (result.status === 'Completed') toast.success('Import completed.');
      else if (result.status === 'CompletedWithErrors') toast('Import completed with errors.', { icon: '⚠️' });
      else toast.error('Import failed.');
    } catch {
      toast.error('Import request failed. Please try again.');
    } finally {
setImporting(false);
    }
  };

  const reset = () => {
    setFile(null); setAnalysis(null); setProgress(null); setStep(0);
  };

  // ── Render ──────────────────────────────────────────────────────────────

  return (
    <div className="mx-auto max-w-2xl space-y-6">
      {/* Header */}
  <div>
        <h1 className="text-2xl font-bold text-slate-900">Import Package</h1>
        <p className="mt-1 text-sm text-slate-500">
          Upload a MicroCMS export ZIP, analyse it, and apply selected artefacts.
      </p>
      </div>

      {/* Step bar */}
      <StepBar current={step} />

      {/* ─── Step 0: Upload & Analyse ─── */}
  {step === 0 && (
        <div className="space-y-5">
    {/* Target site */}
          <div className="rounded-lg border border-slate-200 bg-white px-5 py-4 space-y-4">
     <h2 className="text-sm font-semibold text-slate-800">Target Site</h2>
            <div className="grid grid-cols-2 gap-4">
    <div>
      <label className="form-label">Tenant</label>
    <input className="form-input mt-1 bg-slate-50" value={tenant?.displayName ?? '…'} readOnly />
           </div>
              <div>
                <label className="form-label">Site</label>
             <select
          className="form-input mt-1"
        value={targetSiteId}
      onChange={e => setTargetSiteId(e.target.value)}
         >
       <option value="">— Select site —</option>
                  {importSites.map(s => (
   <option key={s.id} value={s.id}>{s.name} ({s.handle})</option>
        ))}
     </select>
     </div>
    </div>
      </div>

   {/* File drop zone */}
          <div
        {...getRootProps()}
         className={`rounded-xl border-2 border-dashed p-8 text-center transition-colors cursor-pointer ${
         isDragActive
           ? 'border-brand-500 bg-brand-50'
     : file
              ? 'border-green-400 bg-green-50'
       : 'border-slate-300 hover:border-brand-400 hover:bg-slate-50'
            }`}
          >
            <input {...getInputProps()} />
 {file ? (
   <div className="space-y-1">
<p className="text-2xl">📦</p>
     <p className="font-medium text-green-800">{file.name}</p>
         <p className="text-xs text-green-600">{(file.size / 1024 / 1024).toFixed(1)} MB — click or drag to replace</p>
         </div>
   ) : (
              <div className="space-y-2">
              <p className="text-2xl">📂</p>
 <p className="text-sm font-medium text-slate-700">
                  {isDragActive ? 'Drop the ZIP here…' : 'Drag & drop a MicroCMS export ZIP, or click to browse'}
      </p>
        <p className="text-xs text-slate-400">Max 512 MB</p>
 </div>
         )}
    </div>

          <div className="flex justify-end gap-3">
            <button
 type="button"
     onClick={handleAnalyse}
     disabled={!file || !targetSiteId || analysing}
          className="btn-primary disabled:opacity-50"
          >
              {analysing ? (
        <span className="flex items-center gap-2">
                  <svg className="h-4 w-4 animate-spin" viewBox="0 0 24 24" fill="none">
              <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
     <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z" />
    </svg>
          Analysing…
              </span>
   ) : '🔍 Analyse Package'}
        </button>
          </div>
        </div>
      )}

      {/* ─── Step 1: Configure ─── */}
      {step === 1 && analysis && (
      <div className="space-y-5">
    {/* Manifest info */}
        <div className="rounded-lg border border-slate-200 bg-white px-5 py-4 space-y-3">
          <h2 className="text-sm font-semibold text-slate-800">Package Info</h2>
         <div className="grid grid-cols-2 gap-x-6 gap-y-2 text-sm">
  <div><span className="text-slate-500">Site:</span> <span className="font-medium">{analysis.manifest.siteName}</span></div>
          <div><span className="text-slate-500">Tenant slug:</span> <span className="font-mono text-xs">{analysis.manifest.tenantSlug}</span></div>
       <div><span className="text-slate-500">Created:</span> <span>{new Date(analysis.manifest.createdAt).toLocaleString()}</span></div>
  <div><span className="text-slate-500">Version:</span> <span>{analysis.manifest.packageVersion}</span></div>
     </div>
      </div>

   {/* Warnings */}
     {analysis.warnings.length > 0 && (
 <div className="rounded-lg border border-amber-200 bg-amber-50 px-4 py-3 space-y-1">
      <p className="text-sm font-semibold text-amber-800">⚠️ Warnings</p>
      <ul className="space-y-1">
       {analysis.warnings.map((w, i) => (
     <li key={i} className="text-xs text-amber-700">{w}</li>
            ))}
       </ul>
       </div>
 )}

  {/* Stats grid */}
      {analysis.items.length > 0 && (
        <div className="grid grid-cols-2 gap-3 sm:grid-cols-3">
 {analysis.items.map(stat => (
        <StatCard key={stat.category} {...stat} />
     ))}
  </div>
          )}

          {/* Import options */}
          <div className="rounded-lg border border-slate-200 bg-white px-5 py-4 space-y-4">
    <h2 className="text-sm font-semibold text-slate-800">What to Import</h2>
       <div className="grid grid-cols-2 gap-2">
          {IMPORT_CATEGORIES
  .filter(c => !c.adminOnly || isAdmin)
           .map(cat => {
              const stat = analysis.items.find(s => s.category.toLowerCase() === cat.label.toLowerCase().replace(' ', ''));
 return (
      <label
   key={cat.key}
   className={`flex cursor-pointer items-start gap-3 rounded-lg border p-3 transition-colors ${
          options[cat.key]
      ? 'border-brand-300 bg-brand-50'
            : 'border-slate-200 hover:bg-slate-50'
        }`}
           >
      <input
type="checkbox"
      className="mt-0.5 h-4 w-4 rounded border-slate-300 text-brand-600"
      checked={!!options[cat.key]}
        onChange={e => setOptions(prev => ({ ...prev, [cat.key]: e.target.checked }))}
     />
    <div>
         <div className="flex items-center gap-1 text-sm font-medium text-slate-800">
            <span>{cat.icon}</span>
               <span>{cat.label}</span>
          {cat.adminOnly && (
      <span className="rounded bg-amber-100 px-1 text-[10px] font-semibold text-amber-700">Admin</span>
)}
           </div>
     {stat && (
             <p className="text-xs text-slate-500">{stat.totalInPackage} items · {stat.newItems} new</p>
            )}
              </div>
      </label>
    );
         })}
            </div>
      </div>

     {/* Conflict resolution */}
    <div className="rounded-lg border border-slate-200 bg-white px-5 py-4 space-y-3">
            <h2 className="text-sm font-semibold text-slate-800">Conflict Resolution</h2>
            <p className="text-xs text-slate-500">What to do when an item from the package already exists in this site.</p>
        <div className="flex gap-3">
        {(['Skip', 'Overwrite'] as const).map(mode => (
  <label key={mode} className={`flex flex-1 cursor-pointer items-start gap-3 rounded-lg border p-3 ${
   options.conflictResolution === mode
? 'border-brand-300 bg-brand-50'
            : 'border-slate-200 hover:bg-slate-50'
               }`}>
   <input
       type="radio"
          name="conflict"
 className="mt-0.5 h-4 w-4 text-brand-600"
        checked={options.conflictResolution === mode}
              onChange={() => setOptions(prev => ({ ...prev, conflictResolution: mode }))}
     />
      <div>
              <p className="text-sm font-medium text-slate-800">{mode}</p>
               <p className="text-xs text-slate-500">
    {mode === 'Skip'
      ? 'Leave existing items unchanged. Safe for re-runs.'
             : 'Replace existing items with package versions. This cannot be undone.'}
         </p>
        </div>
     </label>
    ))}
            </div>
          </div>

          {/* Actions */}
          <div className="flex justify-between">
     <button type="button" onClick={() => setStep(0)} className="btn-secondary">← Back</button>
            <button
       type="button"
    onClick={handleImport}
         disabled={importing}
       className="btn-primary disabled:opacity-50"
     >
   {importing ? (
       <span className="flex items-center gap-2">
         <svg className="h-4 w-4 animate-spin" viewBox="0 0 24 24" fill="none">
        <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
   <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v8H4z" />
     </svg>
                  Importing…
           </span>
   ) : '⬆ Apply Import'}
  </button>
      </div>
        </div>
      )}

 {/* ─── Step 2: Results ─── */}
      {step === 2 && progress && (
    <div className="space-y-5">
 <ResultsView progress={progress} />
    <div className="flex justify-end gap-3">
            <button type="button" onClick={reset} className="btn-secondary">Import another package</button>
        </div>
   </div>
      )}
    </div>
  );
}
