import { get, post } from './client';

export interface AiContentResult {
  content: string;
}

export const aiWritingApi = {
  draft: (entryId: string, prompt: string, locale?: string): Promise<AiContentResult> =>
    post<AiContentResult>(`/entries/${entryId}/draft`, { prompt, locale }),

  rewrite: (entryId: string, fieldHandle: string, instructions: string): Promise<AiContentResult> =>
    post<AiContentResult>(`/entries/${entryId}/rewrite`, { fieldHandle, instructions }),

  tone: (entryId: string, fieldHandle: string, tone: string): Promise<AiContentResult> =>
    post<AiContentResult>(`/entries/${entryId}/tone`, { fieldHandle, tone }),

  summarize: (entryId: string, fieldHandle: string, maxSentences = 3): Promise<AiContentResult> =>
    get<AiContentResult>(`/entries/${entryId}/summarize`, { params: { fieldHandle, maxSentences } }),
};
