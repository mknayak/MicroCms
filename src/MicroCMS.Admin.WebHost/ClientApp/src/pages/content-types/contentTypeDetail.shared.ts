// Shared constants used across ContentTypeDetail tab components

export const FIELD_TYPE_COLORS: Record<string, string> = {
  ShortText: 'bg-slate-100 text-slate-600',
  LongText: 'bg-slate-100 text-slate-600',
  RichText: 'bg-purple-100 text-purple-700',
  Markdown: 'bg-purple-100 text-purple-700',
  Integer: 'bg-blue-100 text-blue-700',
  Decimal: 'bg-blue-100 text-blue-700',
  Boolean: 'bg-green-100 text-green-700',
  DateTime: 'bg-orange-100 text-orange-700',
  Enum: 'bg-yellow-100 text-yellow-700',
  Reference: 'bg-pink-100 text-pink-700',
  AssetReference: 'bg-teal-100 text-teal-700',
Json: 'bg-gray-100 text-gray-700',
  Component: 'bg-indigo-100 text-indigo-700',
  Location: 'bg-emerald-100 text-emerald-700',
  Color: 'bg-red-100 text-red-700',
};

export const FIELD_TYPE_LABELS: Record<string, string> = {
  ShortText: 'Text',
  LongText: 'Long Text',
  RichText: 'Rich Text',
  Markdown: 'Markdown',
  Integer: 'Integer',
  Decimal: 'Decimal',
Boolean: 'Boolean',
  DateTime: 'DateTime',
  Enum: 'Enum',
  Reference: 'Entry Ref',
  AssetReference: 'Asset Ref',
  Json: 'JSON',
  Component: 'Component',
  Location: 'Location',
  Color: 'Color',
};

export const STATUS_STYLES: Record<string, string> = {
  Published: 'bg-green-100 text-green-700',
  Draft: 'bg-slate-100 text-slate-600',
  PendingReview: 'bg-amber-100 text-amber-700',
  Approved: 'bg-blue-100 text-blue-700',
  Unpublished: 'bg-slate-100 text-slate-500',
  Archived: 'bg-red-100 text-red-600',
  Scheduled: 'bg-purple-100 text-purple-700',
};
