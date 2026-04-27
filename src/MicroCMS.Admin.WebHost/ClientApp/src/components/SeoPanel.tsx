import { useState } from 'react';
import { entriesApi } from '@/api/entries';
import type { SeoMetadata } from '@/types';

interface SeoPanelProps {
  entryId: string;
  initialSeo?: SeoMetadata | null;
}

function CharCounter({ value, max }: { value: string; max: number }) {
  const len = value.length;
  return (
    <span
      className={`text-xs ${
      len > max ? 'text-red-600 font-semibold' : len > max * 0.85 ? 'text-amber-600' : 'text-slate-400'
      }`}
    >
      {len}/{max}
    </span>
  );
}

export function SeoPanel({ entryId, initialSeo }: SeoPanelProps) {
  const [metaTitle, setMetaTitle] = useState(initialSeo?.metaTitle ?? '');
  const [metaDescription, setMetaDescription] = useState(initialSeo?.metaDescription ?? '');
  const [canonicalUrl, setCanonicalUrl] = useState(initialSeo?.canonicalUrl ?? '');
  const [ogImage, setOgImage] = useState(initialSeo?.ogImage ?? '');
  const [saving, setSaving] = useState(false);

  async function save() {
    setSaving(true);
 try {
      await entriesApi.updateSeo(entryId, { metaTitle, metaDescription, canonicalUrl, ogImage });
    } finally {
      setSaving(false);
    }
  }

  const previewTitle = metaTitle || 'Page Title';
  const previewDesc = metaDescription || 'No description set.';

  return (
  <div className="card space-y-4">
      <h3 className="text-sm font-semibold text-slate-900">SEO</h3>

      {/* Meta Title */}
      <div>
  <div className="flex items-center justify-between">
  <label className="form-label">Meta Title</label>
       <CharCounter value={metaTitle} max={60} />
        </div>
<input
          className="form-input mt-1 w-full"
    value={metaTitle}
          onChange={(e) => setMetaTitle(e.target.value)}
  onBlur={save}
     placeholder="Compelling page title…"
     maxLength={80}
        />
      </div>

      {/* Meta Description */}
      <div>
        <div className="flex items-center justify-between">
  <label className="form-label">Meta Description</label>
          <CharCounter value={metaDescription} max={160} />
        </div>
      <textarea
          className="form-input mt-1 w-full resize-none"
          rows={3}
        value={metaDescription}
   onChange={(e) => setMetaDescription(e.target.value)}
onBlur={save}
          placeholder="Brief description for search engines…"
          maxLength={200}
        />
      </div>

      {/* SERP Preview */}
      <div className="rounded-lg border border-slate-200 bg-white p-3 text-xs">
        <p className="font-medium text-[#1a0dab] truncate">{previewTitle}</p>
        <p className="text-[#006621] truncate">{canonicalUrl || 'https://example.com/your-page'}</p>
<p className="mt-0.5 text-slate-600 line-clamp-2">{previewDesc}</p>
      </div>

      {/* Canonical URL */}
      <div>
        <label className="form-label">Canonical URL</label>
        <input
      type="url"
          className="form-input mt-1 w-full"
          value={canonicalUrl}
          onChange={(e) => setCanonicalUrl(e.target.value)}
          onBlur={save}
      placeholder="https://…"
        />
   </div>

      {/* OG Image */}
      <div>
<label className="form-label">OG Image URL</label>
      <input
          type="url"
          className="form-input mt-1 w-full"
        value={ogImage}
          onChange={(e) => setOgImage(e.target.value)}
          onBlur={save}
        placeholder="https://…/og-image.jpg"
    />
        {ogImage && (
  <img src={ogImage} alt="OG preview" className="mt-2 h-24 w-full rounded object-cover" />
   )}
      </div>

{saving && <p className="text-xs text-slate-400">Saving…</p>}
    </div>
  );
}
