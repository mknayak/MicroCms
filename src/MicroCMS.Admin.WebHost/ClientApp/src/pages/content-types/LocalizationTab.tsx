import { useQuery } from '@tanstack/react-query';
import { entriesApi } from '@/api/entries';
import type { ContentType, EntryListItem } from '@/types';
import { useSite } from '@/contexts/SiteContext';
import { STATUS_STYLES } from './contentTypeDetail.shared';

const DEMO_LOCALES = [
  { code: 'en-US', flag: '🇺🇸', label: 'EN-US', primary: true },
  { code: 'fr-FR', flag: '🇫🇷', label: 'FR-FR', primary: false },
  { code: 'de-DE', flag: '🇩🇪', label: 'DE-DE', primary: false },
];

export function LocalizationTab({ contentType }: { contentType: ContentType }) {
  const { selectedSiteId } = useSite();

  const { data } = useQuery({
    queryKey: ['entries', { contentTypeId: contentType.id, siteId: selectedSiteId, pageSize: 10 }],
    queryFn: () => entriesApi.list({ contentTypeId: contentType.id, siteId: selectedSiteId ?? undefined, pageSize: 10 }),
    enabled: !!selectedSiteId,
  });

  const entries = data?.items ?? [];

  // Group entries by title/slug to create cross-locale rows
  const slugGroups = entries.reduce<Record<string, EntryListItem[]>>((acc, e) => {
    const key = e.title ?? e.slug;
    if (!acc[key]) acc[key] = [];
    acc[key].push(e);
    return acc;
  }, {});

  return (
 <div className="space-y-6">
      {/* Fallback chain */}
      <div className="rounded-lg border border-slate-200 bg-white">
        <div className="flex items-center justify-between border-b border-slate-100 px-5 py-4">
          <div>
            <p className="font-semibold text-slate-900">Locale Fallback Chain</p>
       <p className="text-xs text-slate-400 mt-0.5">{DEMO_LOCALES.length} active locales</p>
          </div>
      <button className="btn-secondary text-sm">Edit Chain</button>
        </div>
      <div className="flex flex-wrap items-center gap-3 px-5 py-5">
          {DEMO_LOCALES.map((loc, i) => (
            <div key={loc.code} className="flex items-center gap-3">
       <div className={`flex items-center gap-2 rounded-lg border-2 px-4 py-2 ${loc.primary ? 'border-brand-400 bg-brand-50 text-brand-700' : 'border-slate-200 bg-white text-slate-700'}`}>
<span className="text-base">{loc.flag}</span>
   <div>
         <p className="text-xs font-semibold">{loc.label}</p>
        {loc.primary
                    ? <p className="text-[10px] text-brand-500">Primary</p>
               : <p className="text-[10px] text-slate-400">Step {i}</p>
              }
     </div>
  </div>
       {i < DEMO_LOCALES.length - 1 && <span className="text-slate-300 text-lg font-light">→</span>}
            </div>
          ))}
          <button className="flex items-center gap-1 rounded-lg border-2 border-dashed border-slate-200 px-4 py-2 text-xs text-slate-400 hover:border-brand-300 hover:text-brand-500">
 + Add
          </button>
        </div>
      </div>

   {/* Translation coverage */}
      <div className="rounded-lg border border-slate-200 bg-white">
     <div className="flex items-center justify-between border-b border-slate-100 px-5 py-4">
          <p className="font-semibold text-slate-900">Translation Coverage</p>
     <button className="btn-primary text-sm">✨ Batch translate missing</button>
      </div>
        <div className="overflow-x-auto">
     <table className="min-w-full divide-y divide-slate-100 text-sm">
   <thead className="bg-slate-50">
         <tr>
       <th className="px-5 py-3 text-left text-xs font-medium text-slate-500 uppercase">Entry Title</th>
            {DEMO_LOCALES.map((l) => (
     <th key={l.code} className="px-4 py-3 text-left text-xs font-medium text-slate-500 uppercase">
        <span className="mr-1">{l.flag}</span>{l.label}
      </th>
              ))}
   <th className="px-4 py-3 text-left text-xs font-medium text-slate-500 uppercase">Missing</th>
           <th className="px-4 py-3" />
   </tr>
     </thead>
            <tbody className="divide-y divide-slate-100 bg-white">
              {Object.entries(slugGroups).length === 0 ? (
    <tr>
     <td colSpan={DEMO_LOCALES.length + 3} className="px-5 py-10 text-center text-slate-400">
           No entries to show translation coverage for.
   </td>
     </tr>
   ) : (
      Object.entries(slugGroups).map(([title, localeEntries]) => {
   const localeMap = Object.fromEntries(localeEntries.map((e) => [e.locale, e]));
   const missing = DEMO_LOCALES.filter((l) => !localeMap[l.code]).map((l) => l.label);
        return (
                    <tr key={title} className="hover:bg-slate-50">
 <td className="px-5 py-3 font-medium text-slate-800">{title}</td>
         {DEMO_LOCALES.map((l) => {
       const e = localeMap[l.code];
           return (
    <td key={l.code} className="px-4 py-3">
            {e ? (
           <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${STATUS_STYLES[e.status] ?? 'bg-slate-100 text-slate-500'}`}>
               {e.status === 'Published' ? '✓ Live' : e.status}
    </span>
             ) : (
 <span className="text-slate-300">—</span>
          )}
    </td>
          );
          })}
          <td className="px-4 py-3">
         {missing.length > 0 ? (
    <span className="rounded-full bg-red-100 px-2 py-0.5 text-xs font-medium text-red-600">
     {missing.join(', ')} missing
       </span>
       ) : (
       <span className="text-slate-300">—</span>
       )}
                 </td>
        <td className="px-4 py-3">
       {missing.length > 0 && (
       <button className="text-xs font-medium text-brand-600 hover:underline">
            ✨ Translate {missing.length > 1 ? 'all' : missing[0]}
          </button>
    )}
     </td>
            </tr>
      );
       })
    )}
            </tbody>
          </table>
        </div>

        {/* Overall coverage */}
      {Object.keys(slugGroups).length > 0 && (
       <div className="flex items-center justify-between border-t border-slate-100 px-5 py-3">
         <div className="flex items-center gap-3 text-sm text-slate-500">
          <span>Overall translation coverage:</span>
 <div className="h-2 w-48 rounded-full bg-slate-200">
           <div className="h-2 rounded-full bg-brand-500" style={{ width: '61%' }} />
 </div>
         <span className="font-medium text-slate-700">61%</span>
  </div>
   </div>
      )}
  </div>
    </div>
  );
}
