import { useQuery } from '@tanstack/react-query';
import { get } from '@/api/client';

interface AiUsageSummary {
  totalTokensToday: number;
  budgetMaxTokensPerDay: number;
  totalCostUsdToday: number;
}

async function fetchAiUsage(): Promise<AiUsageSummary> {
  return get<AiUsageSummary>('/ai/usage/today');
}

/**
 * Displays a slim banner showing today's AI token usage vs the configured daily budget.
 * Renders nothing if the usage endpoint is unavailable or the budget is unconfigured.
 */
export function AiBudgetBanner() {
  const { data, isError } = useQuery({
    queryKey: ['ai-usage-today'],
    queryFn: fetchAiUsage,
    staleTime: 60_000,
    retry: false,
  });

  if (isError || !data || data.budgetMaxTokensPerDay <= 0) return null;

  const pct = Math.min(100, Math.round((data.totalTokensToday / data.budgetMaxTokensPerDay) * 100));
  const isWarning = pct >= 80;
  const isFull = pct >= 100;

  return (
    <div
      className={`flex items-center gap-3 rounded-lg border px-3 py-2 text-xs ${
        isFull
          ? 'border-red-200 bg-red-50 text-red-700'
          : isWarning
          ? 'border-amber-200 bg-amber-50 text-amber-700'
          : 'border-slate-200 bg-slate-50 text-slate-600'
      }`}
    >
      <span className="text-sm">✦</span>
      <div className="flex-1 min-w-0">
        <div className="flex items-center justify-between gap-2">
          <span className="font-medium">AI Budget</span>
          <span className="tabular-nums">
            {data.totalTokensToday.toLocaleString()} / {data.budgetMaxTokensPerDay.toLocaleString()} tokens
          </span>
        </div>
        <div className="mt-1 h-1 w-full overflow-hidden rounded-full bg-slate-200">
          <div
            className={`h-full rounded-full transition-all ${
              isFull ? 'bg-red-500' : isWarning ? 'bg-amber-400' : 'bg-brand-500'
            }`}
            style={{ width: `${pct}%` }}
          />
        </div>
      </div>
      {isFull && <span className="shrink-0 font-semibold">Limit reached</span>}
    </div>
  );
}
