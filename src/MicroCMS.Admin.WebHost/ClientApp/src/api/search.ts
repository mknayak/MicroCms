import { get } from './client';
import type { SearchParams, SearchResults } from '@/types';

/**
 * Full-text / faceted search across published entries.
 * Maps to GET /api/v1/search (Sprint 9).
 */
export async function searchEntries(params: SearchParams): Promise<SearchResults> {
  return get<SearchResults>('/search', { params });
}
