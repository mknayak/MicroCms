import type { AiAssistMode } from './useAiAssist';
import { TONE_OPTIONS } from './useAiAssist';

interface AiAssistModalProps {
  fieldLabel: string;
  mode: AiAssistMode;
  onModeChange: (m: AiAssistMode) => void;
  prompt: string;
  onPromptChange: (v: string) => void;
  instructions: string;
  onInstructionsChange: (v: string) => void;
  tone: string;
  onToneChange: (v: string) => void;
  preview: string | null;
  isPending: boolean;
  isError: boolean;
  errorMessage: string | null;
  canGenerate: boolean;
  onGenerate: () => void;
  onApply: () => void;
  onClose: () => void;
  /** When true the preview is rendered as plain text; when false it is rendered as HTML. */
  plainText?: boolean;
}

const MODES: AiAssistMode[] = ['draft', 'rewrite', 'tone', 'summarize'];

export function AiAssistModal({
  fieldLabel,
  mode,
  onModeChange,
  prompt,
  onPromptChange,
  instructions,
  onInstructionsChange,
  tone,
  onToneChange,
  preview,
  isPending,
  isError,
  errorMessage,
  canGenerate,
  onGenerate,
  onApply,
  onClose,
  plainText = false,
}: AiAssistModalProps) {
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/40">
      <div className="card mx-4 w-full max-w-md space-y-4">
        {/* Header */}
        <div className="flex items-center justify-between">
          <h3 className="text-sm font-semibold text-slate-900">✦ AI Assist — {fieldLabel}</h3>
          <button type="button" onClick={onClose} className="text-slate-400 hover:text-slate-700">✕</button>
        </div>

        {/* Mode tabs */}
        <div className="flex gap-1 rounded-lg bg-slate-100 p-1">
          {MODES.map((m) => (
            <button
              key={m}
              type="button"
              onClick={() => onModeChange(m)}
              className={`flex-1 rounded-md px-2 py-1 text-xs font-medium capitalize transition-colors ${
                mode === m ? 'bg-white shadow-sm text-slate-900' : 'text-slate-500 hover:text-slate-700'
              }`}
            >
              {m}
            </button>
          ))}
        </div>

        {/* Mode-specific inputs */}
        {mode === 'draft' && (
          <div className="space-y-1.5">
            <label className="form-label text-xs">Describe what to write</label>
            <textarea
              className="form-input w-full resize-none"
              rows={3}
              placeholder="e.g. A blog intro about headless CMS benefits…"
              value={prompt}
              onChange={(e) => onPromptChange(e.target.value)}
            />
          </div>
        )}
        {mode === 'rewrite' && (
          <div className="space-y-1.5">
            <label className="form-label text-xs">Rewrite instructions</label>
            <textarea
              className="form-input w-full resize-none"
              rows={3}
              placeholder="e.g. Make it shorter and more engaging…"
              value={instructions}
              onChange={(e) => onInstructionsChange(e.target.value)}
            />
          </div>
        )}
        {mode === 'tone' && (
          <div className="space-y-1.5">
            <label className="form-label text-xs">Target tone</label>
            <select className="form-input" value={tone} onChange={(e) => onToneChange(e.target.value)}>
              {TONE_OPTIONS.map((t) => <option key={t} value={t}>{t}</option>)}
            </select>
          </div>
        )}
        {mode === 'summarize' && (
          <p className="text-xs text-slate-500">Summarizes the current field content into 3 sentences.</p>
        )}

        {/* Error */}
        {isError && (
          <p className="text-xs text-red-600">{errorMessage ?? 'AI request failed.'}</p>
        )}

        {/* Preview */}
        {preview !== null && (
          <div className="space-y-2">
            <p className="text-xs font-medium text-slate-700">Preview</p>
            {plainText ? (
              <div className="max-h-40 overflow-y-auto rounded-lg border border-slate-200 bg-slate-50 p-3 text-xs text-slate-700 whitespace-pre-wrap">
                {preview}
              </div>
            ) : (
              <div
                className="max-h-40 overflow-y-auto rounded-lg border border-slate-200 bg-slate-50 p-3 text-xs text-slate-700 prose prose-sm"
                dangerouslySetInnerHTML={{ __html: preview }}
              />
            )}
          </div>
        )}

        {/* Actions */}
        <div className="flex justify-end gap-2 border-t border-slate-100 pt-3">
          <button type="button" onClick={onClose} className="btn-secondary text-xs">Cancel</button>
          {preview !== null ? (
            <button
              type="button"
              onClick={onApply}
              className="rounded-lg bg-green-600 px-3 py-2 text-xs font-medium text-white hover:bg-green-700"
            >
              Apply
            </button>
          ) : (
            <button
              type="button"
              onClick={onGenerate}
              disabled={isPending || !canGenerate}
              className="rounded-lg bg-brand-600 px-3 py-2 text-xs font-medium text-white hover:bg-brand-700 disabled:opacity-50"
            >
              {isPending ? 'Generating…' : 'Generate'}
            </button>
          )}
        </div>
      </div>
    </div>
  );
}
