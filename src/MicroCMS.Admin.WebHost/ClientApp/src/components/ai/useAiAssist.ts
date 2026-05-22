import { useState } from 'react';
import { useMutation } from '@tanstack/react-query';
import { aiWritingApi } from '@/api/ai';

export type AiAssistMode = 'draft' | 'rewrite' | 'tone' | 'summarize';

export interface UseAiAssistOptions {
  entryId?: string;
  fieldHandle: string;
}

export interface UseAiAssistResult {
  mode: AiAssistMode;
  setMode: (m: AiAssistMode) => void;
  prompt: string;
  setPrompt: (v: string) => void;
  instructions: string;
  setInstructions: (v: string) => void;
  tone: string;
  setTone: (v: string) => void;
  preview: string | null;
  setPreview: (v: string | null) => void;
  isPending: boolean;
  isError: boolean;
  errorMessage: string | null;
  generate: () => void;
  reset: () => void;
}

export const TONE_OPTIONS = ['Professional', 'Friendly', 'Formal', 'Casual', 'Persuasive', 'Concise'];

export function useAiAssist({ entryId, fieldHandle }: UseAiAssistOptions): UseAiAssistResult {
  const [mode, setMode] = useState<AiAssistMode>('draft');
  const [prompt, setPrompt] = useState('');
  const [instructions, setInstructions] = useState('');
  const [tone, setTone] = useState(TONE_OPTIONS[0]);
  const [preview, setPreview] = useState<string | null>(null);

  const mutation = useMutation({
    mutationFn: async () => {
      if (!entryId) throw new Error('Entry must be saved before using AI Assist.');
      switch (mode) {
        case 'draft':     return aiWritingApi.draft(entryId, prompt);
        case 'rewrite':   return aiWritingApi.rewrite(entryId, fieldHandle, instructions);
        case 'tone':      return aiWritingApi.tone(entryId, fieldHandle, tone);
        case 'summarize': return aiWritingApi.summarize(entryId, fieldHandle);
      }
    },
    onSuccess: (result) => { if (result) setPreview(result.content); },
  });

  const reset = () => {
    setMode('draft');
    setPrompt('');
    setInstructions('');
    setTone(TONE_OPTIONS[0]);
    setPreview(null);
    mutation.reset();
  };

  const errorMessage = mutation.isError
    ? (mutation.error instanceof Error ? mutation.error.message : 'AI request failed.')
    : null;

  return {
    mode, setMode,
    prompt, setPrompt,
    instructions, setInstructions,
    tone, setTone,
    preview, setPreview,
    isPending: mutation.isPending,
    isError: mutation.isError,
    errorMessage,
    generate: () => mutation.mutate(),
    reset,
  };
}
