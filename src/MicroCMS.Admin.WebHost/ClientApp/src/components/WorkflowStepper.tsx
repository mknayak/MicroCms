import { useState } from 'react';
import type { EntryStatus } from '@/types';

// ─── Step config ──────────────────────────────────────────────────────────────

const STEPS: { status: EntryStatus; label: string }[] = [
  { status: 'Draft',        label: 'Draft' },
  { status: 'PendingReview', label: 'In Review' },
  { status: 'Approved',     label: 'Approved' },
  { status: 'Published',    label: 'Published' },
];

const STATUS_ORDER: Record<string, number> = {
  Draft: 0,
  PendingReview: 1,
  Approved: 2,
  Published: 3,
  Scheduled: 3,
  Unpublished: 2,
  Archived: -1,
};

// ─── Reject Modal ─────────────────────────────────────────────────────────────

function RejectModal({
  onConfirm,
  onClose,
}: {
  onConfirm: (reason: string) => void;
  onClose: () => void;
}) {
  const [reason, setReason] = useState('');
  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
      <div className="card mx-4 w-full max-w-sm space-y-4">
        <h3 className="text-sm font-semibold text-slate-900">Reject entry</h3>
        <textarea
          className="form-input w-full resize-none"
          rows={3}
          placeholder="Reason for rejection…"
 value={reason}
    onChange={(e) => setReason(e.target.value)}
        />
  <div className="flex justify-end gap-2">
          <button onClick={onClose} className="btn-secondary">Cancel</button>
     <button
  onClick={() => { if (reason.trim()) onConfirm(reason.trim()); }}
            disabled={!reason.trim()}
   className="btn-danger"
       >
            Reject
        </button>
        </div>
      </div>
    </div>
  );
}

// ─── Schedule Picker ──────────────────────────────────────────────────────────

function SchedulePicker({
  onConfirm,
  onClose,
}: {
  onConfirm: (publishAt: string, unpublishAt?: string) => void;
  onClose: () => void;
}) {
  const [publishAt, setPublishAt] = useState('');
  const [unpublishAt, setUnpublishAt] = useState('');
  return (
    <div className="mt-3 rounded-lg border border-slate-200 bg-slate-50 p-3 space-y-3">
      <div>
        <label className="form-label">Publish at</label>
    <input
          type="datetime-local"
          className="form-input mt-1 w-full"
          value={publishAt}
          onChange={(e) => setPublishAt(e.target.value)}
        />
      </div>
      <div>
        <label className="form-label text-slate-400">Unpublish at (optional)</label>
        <input
          type="datetime-local"
  className="form-input mt-1 w-full"
          value={unpublishAt}
          onChange={(e) => setUnpublishAt(e.target.value)}
        />
      </div>
      <div className="flex gap-2">
        <button onClick={onClose} className="btn-secondary flex-1">Cancel</button>
    <button
   onClick={() => { if (publishAt) onConfirm(new Date(publishAt).toISOString(), unpublishAt ? new Date(unpublishAt).toISOString() : undefined); }}
       disabled={!publishAt}
          className="btn-primary flex-1"
        >
        Schedule
        </button>
      </div>
    </div>
  );
}

// ─── WorkflowStepper ─────────────────────────────────────────────────────────

interface WorkflowStepperProps {
  entryId: string;
  currentStatus: EntryStatus;
  onSubmitForReview: () => void;
  onApprove: () => void;
  onReject: (reason: string) => void;
  onPublish: () => void;
  onUnpublish: () => void;
  onSchedule: (publishAt: string, unpublishAt?: string) => void;
}

export function WorkflowStepper({
  currentStatus,
  onSubmitForReview,
  onApprove,
  onReject,
  onPublish,
  onUnpublish,
  onSchedule,
}: WorkflowStepperProps) {
  const [showReject, setShowReject] = useState(false);
  const [showSchedule, setShowSchedule] = useState(false);
  const currentOrder = STATUS_ORDER[currentStatus] ?? 0;

  return (
    <div className="card space-y-4">
      <h3 className="text-sm font-semibold text-slate-900">Workflow</h3>

      {/* Step indicators */}
      <ol className="relative flex flex-col gap-0">
   {STEPS.map((step, idx) => {
      const stepOrder = STATUS_ORDER[step.status]!;
        const isCompleted = currentOrder > stepOrder;
          const isActive = currentStatus === step.status ||
   (step.status === 'Published' && currentStatus === 'Scheduled');

          return (
   <li key={step.status} className="flex items-center gap-3 pb-4 last:pb-0">
 {/* Connector */}
              {idx < STEPS.length - 1 && (
      <div className="absolute left-[17px] mt-6" style={{ height: '2rem', width: 2, background: isCompleted ? '#6366f1' : '#e2e8f0', top: `${idx * 52}px` }} />
        )}
  {/* Circle */}
  <span
 className={`relative z-10 flex h-9 w-9 shrink-0 items-center justify-center rounded-full border-2 text-xs font-bold ${
         isCompleted
           ? 'border-indigo-500 bg-indigo-500 text-white'
         : isActive
             ? 'border-indigo-500 bg-white text-indigo-600'
   : 'border-slate-200 bg-white text-slate-400'
                }`}
   >
        {isCompleted ? '✓' : idx + 1}
       </span>
      <span className={`text-sm ${isActive ? 'font-semibold text-slate-900' : 'text-slate-500'}`}>
  {step.label}
    </span>
         </li>
          );
        })}
      </ol>

      {/* Action buttons */}
      <div className="space-y-2 border-t border-slate-100 pt-3">
        {currentStatus === 'Draft' && (
          <button onClick={onSubmitForReview} className="btn-secondary w-full justify-center">
            Submit for Review
          </button>
        )}

        {currentStatus === 'PendingReview' && (
          <>
            <button onClick={onApprove} className="btn-primary w-full justify-center">Approve</button>
            <button onClick={() => setShowReject(true)} className="btn-danger w-full justify-center">Reject…</button>
    </>
        )}

        {currentStatus === 'Approved' && (
          <>
       <button onClick={onPublish} className="btn-primary w-full justify-center">Publish Now</button>
            <button onClick={() => setShowSchedule((v) => !v)} className="btn-secondary w-full justify-center">
   {showSchedule ? 'Cancel' : 'Schedule…'}
            </button>
     {showSchedule && (
              <SchedulePicker
       onConfirm={(p, u) => { setShowSchedule(false); onSchedule(p, u); }}
          onClose={() => setShowSchedule(false)}
       />
            )}
    </>
        )}

        {(currentStatus === 'Published' || currentStatus === 'Scheduled') && (
       <button onClick={onUnpublish} className="btn-secondary w-full justify-center">Unpublish</button>
        )}
      </div>

      {showReject && (
    <RejectModal
          onConfirm={(r) => { setShowReject(false); onReject(r); }}
          onClose={() => setShowReject(false)}
        />
      )}
    </div>
  );
}
