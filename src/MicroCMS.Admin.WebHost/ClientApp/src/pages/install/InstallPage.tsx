import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import toast from 'react-hot-toast';
import { installApi } from '@/api/install';
import { useInstall } from '@/contexts/InstallContext';
import { ApiError } from '@/api/client';

// ─── Validation schema ────────────────────────────────────────────────────────

const installSchema = z
  .object({
    // Step 1 — Organisation
    tenantDisplayName: z.string().min(2, 'Organisation name must be at least 2 characters'),
  tenantSlug: z
      .string()
      .min(2, 'Slug must be at least 2 characters')
   .max(63, 'Slug must be 63 characters or fewer')
      .regex(/^[a-z0-9-]+$/, 'Slug may only contain lowercase letters, numbers and hyphens'),
    defaultSiteName: z.string().min(1, 'Site name is required'),

    // Step 2 — Locale & Timezone
    defaultLocale: z.string().min(2, 'Locale is required'),
    timeZoneId: z.string().min(1, 'Time zone is required'),

    // Step 3 — Admin account
    adminDisplayName: z.string().min(2, 'Name must be at least 2 characters'),
  adminEmail: z.string().email('Enter a valid email address'),
    adminPassword: z
      .string()
      .min(8, 'Password must be at least 8 characters')
 .regex(/[A-Z]/, 'Password must contain at least one uppercase letter')
      .regex(/[0-9]/, 'Password must contain at least one number'),
    adminPasswordConfirm: z.string(),
  })
  .refine((d) => d.adminPassword === d.adminPasswordConfirm, {
    message: 'Passwords do not match',
    path: ['adminPasswordConfirm'],
  });

type InstallFormValues = z.infer<typeof installSchema>;

// ─── Step metadata ────────────────────────────────────────────────────────────

const STEPS = [
  { id: 1, label: 'Organisation' },
  { id: 2, label: 'Locale & Time' },
  { id: 3, label: 'Admin Account' },
  { id: 4, label: 'Review' },
] as const;

// ─── Common locales & timezones ───────────────────────────────────────────────

const LOCALES = [
  { value: 'en-US', label: 'English (US)' },
  { value: 'en-GB', label: 'English (UK)' },
  { value: 'fr-FR', label: 'French' },
  { value: 'de-DE', label: 'German' },
  { value: 'es-ES', label: 'Spanish' },
  { value: 'pt-BR', label: 'Portuguese (Brazil)' },
  { value: 'ja-JP', label: 'Japanese' },
  { value: 'zh-CN', label: 'Chinese (Simplified)' },
];

const TIMEZONES = [
  { value: 'UTC', label: 'UTC' },
  { value: 'America/New_York', label: 'Eastern Time (US)' },
  { value: 'America/Chicago', label: 'Central Time (US)' },
  { value: 'America/Denver', label: 'Mountain Time (US)' },
  { value: 'America/Los_Angeles', label: 'Pacific Time (US)' },
  { value: 'Europe/London', label: 'London' },
  { value: 'Europe/Paris', label: 'Paris / Berlin' },
  { value: 'Asia/Kolkata', label: 'India (IST)' },
  { value: 'Asia/Tokyo', label: 'Tokyo' },
  { value: 'Asia/Singapore', label: 'Singapore' },
  { value: 'Australia/Sydney', label: 'Sydney' },
];

// ─── Sub-components ───────────────────────────────────────────────────────────

function StepIndicator({ current }: { current: number }) {
  return (
    <nav aria-label="Installation steps" className="mb-8">
      <ol className="flex items-center gap-0">
        {STEPS.map((step, idx) => {
      const done = current > step.id;
       const active = current === step.id;
  return (
    <li key={step.id} className="flex flex-1 items-center">
              <div className="flex flex-col items-center gap-1">
       <div
     className={[
           'flex h-8 w-8 items-center justify-center rounded-full text-sm font-semibold ring-2',
              done
      ? 'bg-brand-600 text-white ring-brand-600'
              : active
          ? 'bg-white text-brand-600 ring-brand-600'
     : 'bg-white text-slate-400 ring-slate-200',
    ].join(' ')}
              >
    {done ? (
 <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2.5}>
         <path strokeLinecap="round" strokeLinejoin="round" d="M5 13l4 4L19 7" />
        </svg>
       ) : (
    step.id
                )}
        </div>
       <span
        className={[
 'text-xs font-medium',
        active ? 'text-brand-600' : done ? 'text-slate-600' : 'text-slate-400',
            ].join(' ')}
        >
           {step.label}
     </span>
     </div>
{idx < STEPS.length - 1 && (
     <div
          className={[
   'mb-5 h-0.5 flex-1',
    done ? 'bg-brand-600' : 'bg-slate-200',
        ].join(' ')}
        />
       )}
   </li>
        );
      })}
      </ol>
    </nav>
  );
}

function FieldError({ message }: { message?: string }) {
  if (!message) return null;
  return <p className="form-error mt-1">{message}</p>;
}

// ─── Page ─────────────────────────────────────────────────────────────────────

export default function InstallPage() {
  const [step, setStep] = useState(1);
  const [isSubmitting, setIsSubmitting] = useState(false);
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirm, setShowConfirm] = useState(false);
  const { markInstalled } = useInstall();
  const navigate = useNavigate();

  const {
    register,
    handleSubmit,
    trigger,
    getValues,
    formState: { errors },
  } = useForm<InstallFormValues>({
    resolver: zodResolver(installSchema),
    defaultValues: {
      tenantSlug: '',
   tenantDisplayName: '',
    defaultSiteName: 'Main',
      defaultLocale: 'en-US',
      timeZoneId: 'UTC',
      adminDisplayName: '',
      adminEmail: '',
      adminPassword: '',
      adminPasswordConfirm: '',
    },
    mode: 'onTouched',
  });

  // Fields validated on each step's "Next" click
  const STEP_FIELDS: Record<number, (keyof InstallFormValues)[]> = {
    1: ['tenantDisplayName', 'tenantSlug', 'defaultSiteName'],
    2: ['defaultLocale', 'timeZoneId'],
    3: ['adminDisplayName', 'adminEmail', 'adminPassword', 'adminPasswordConfirm'],
  };

  const handleNext = async () => {
    const valid = await trigger(STEP_FIELDS[step]);
    if (valid) setStep((s) => s + 1);
  };

  const handleBack = () => setStep((s) => s - 1);

  const onSubmit = async (values: InstallFormValues) => {
    setIsSubmitting(true);
    try {
      await installApi.install({
     tenantSlug: values.tenantSlug,
        tenantDisplayName: values.tenantDisplayName,
        defaultLocale: values.defaultLocale,
     timeZoneId: values.timeZoneId,
        defaultSiteName: values.defaultSiteName,
        adminEmail: values.adminEmail,
        adminDisplayName: values.adminDisplayName,
        adminPassword: values.adminPassword,
      });

      markInstalled();
      toast.success('MicroCMS installed! Please sign in.');
      navigate('/login', { replace: true });
  } catch (err) {
      if (err instanceof ApiError) {
        toast.error(err.problem.detail ?? 'Installation failed. Please try again.');
   } else {
        toast.error('An unexpected error occurred.');
      }
    } finally {
  setIsSubmitting(false);
    }
  };

  const v = getValues();

  return (
 <div className="flex min-h-screen items-center justify-center bg-gradient-to-br from-brand-50 to-slate-100 px-4 py-12">
    <div className="w-full max-w-lg">
        {/* Header */}
   <div className="mb-8 text-center">
          <div className="inline-flex h-12 w-12 items-center justify-center rounded-xl bg-brand-600 text-white">
            <svg className="h-7 w-7" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2}
     d="M9 12h6m-6 4h6m2 5H7a2 2 0 01-2-2V5a2 2 0 012-2h5.586a1 1 0 01.707.293l5.414 5.414a1 1 0 01.293.707V19a2 2 0 01-2 2z" />
        </svg>
          </div>
          <h1 className="mt-4 text-2xl font-bold text-slate-900">Welcome to MicroCMS</h1>
          <p className="mt-1 text-sm text-slate-500">Complete the setup to get started</p>
    </div>

     <div className="card">
        <StepIndicator current={step} />

    <form onSubmit={handleSubmit(onSubmit)} noValidate>

            {/* ── Step 1: Organisation ── */}
 {step === 1 && (
    <div className="space-y-4">
                <h2 className="text-base font-semibold text-slate-900">Organisation details</h2>
        <p className="text-sm text-slate-500">
          This creates your first tenant — the top-level container for all your content and users.
   </p>

       <div>
            <label htmlFor="tenantDisplayName" className="form-label">Organisation name</label>
      <input
        id="tenantDisplayName"
      className="form-input mt-1"
     placeholder="Acme Corp"
       {...register('tenantDisplayName')}
           />
          <FieldError message={errors.tenantDisplayName?.message} />
    </div>

       <div>
  <label htmlFor="tenantSlug" className="form-label">
    Slug
          <span className="ml-1.5 text-xs font-normal text-slate-400">
 (used in URLs and the API — cannot be changed later)
         </span>
  </label>
        <div className="relative mt-1">
     <input
          id="tenantSlug"
       className="form-input lowercase"
     placeholder="acme-corp"
       {...register('tenantSlug', {
    onChange: (e) => {
          e.target.value = e.target.value.toLowerCase().replace(/[^a-z0-9-]/g, '-');
},
         })}
    />
    </div>
                  <FieldError message={errors.tenantSlug?.message} />
        </div>

        <div>
             <label htmlFor="defaultSiteName" className="form-label">Default site name</label>
              <input
           id="defaultSiteName"
  className="form-input mt-1"
           placeholder="Main"
                    {...register('defaultSiteName')}
  />
        <FieldError message={errors.defaultSiteName?.message} />
    </div>
         </div>
            )}

            {/* ── Step 2: Locale & Timezone ── */}
        {step === 2 && (
  <div className="space-y-4">
   <h2 className="text-base font-semibold text-slate-900">Locale & time zone</h2>
       <p className="text-sm text-slate-500">
  Sets the default locale for content and the time zone used for scheduled publishing.
   </p>

  <div>
          <label htmlFor="defaultLocale" className="form-label">Default locale</label>
     <select id="defaultLocale" className="form-select mt-1" {...register('defaultLocale')}>
             {LOCALES.map((l) => (
 <option key={l.value} value={l.value}>{l.label}</option>
              ))}
  </select>
           <FieldError message={errors.defaultLocale?.message} />
      </div>

           <div>
       <label htmlFor="timeZoneId" className="form-label">Time zone</label>
          <select id="timeZoneId" className="form-select mt-1" {...register('timeZoneId')}>
                    {TIMEZONES.map((tz) => (
<option key={tz.value} value={tz.value}>{tz.label}</option>
           ))}
         </select>
         <FieldError message={errors.timeZoneId?.message} />
       </div>
       </div>
            )}

            {/* ── Step 3: Admin account ── */}
{step === 3 && (
     <div className="space-y-4">
            <h2 className="text-base font-semibold text-slate-900">Admin account</h2>
       <p className="text-sm text-slate-500">
           This account will have full Tenant Admin access. You can create more users after setup.
       </p>

  <div>
 <label htmlFor="adminDisplayName" className="form-label">Full name</label>
           <input
        id="adminDisplayName"
      className="form-input mt-1"
     placeholder="Jane Smith"
    autoComplete="name"
         {...register('adminDisplayName')}
         />
      <FieldError message={errors.adminDisplayName?.message} />
      </div>

        <div>
         <label htmlFor="adminEmail" className="form-label">Email address</label>
             <input
            id="adminEmail"
   type="email"
             className="form-input mt-1"
 placeholder="admin@example.com"
      autoComplete="email"
    {...register('adminEmail')}
          />
       <FieldError message={errors.adminEmail?.message} />
                </div>

 <div>
    <label htmlFor="adminPassword" className="form-label">Password</label>
       <div className="relative mt-1">
             <input
     id="adminPassword"
  type={showPassword ? 'text' : 'password'}
               className="form-input pr-10"
     autoComplete="new-password"
           {...register('adminPassword')}
/>
         <button
            type="button"
       onClick={() => setShowPassword((p) => !p)}
   className="absolute inset-y-0 right-0 flex items-center pr-3 text-slate-400 hover:text-slate-600"
      aria-label={showPassword ? 'Hide password' : 'Show password'}
 >
       <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
          {showPassword ? (
      <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2}
              d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l3.59 3.59m0 0A9.953 9.953 0 0112 5c4.478 0 8.268 2.943 9.543 7a10.025 10.025 0 01-4.132 5.411m0 0L21 21" />
       ) : (
               <>
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
   <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2}
          d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
   </>
)}
           </svg>
       </button>
              </div>
 <p className="mt-1 text-xs text-slate-400">
         Min. 8 characters, at least one uppercase letter and one number.
                </p>
    <FieldError message={errors.adminPassword?.message} />
       </div>

            <div>
           <label htmlFor="adminPasswordConfirm" className="form-label">Confirm password</label>
     <div className="relative mt-1">
      <input
             id="adminPasswordConfirm"
       type={showConfirm ? 'text' : 'password'}
              className="form-input pr-10"
       autoComplete="new-password"
         {...register('adminPasswordConfirm')}
     />
     <button
           type="button"
          onClick={() => setShowConfirm((p) => !p)}
           className="absolute inset-y-0 right-0 flex items-center pr-3 text-slate-400 hover:text-slate-600"
                    aria-label={showConfirm ? 'Hide password' : 'Show password'}
       >
  <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor">
              {showConfirm ? (
            <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2}
      d="M13.875 18.825A10.05 10.05 0 0112 19c-4.478 0-8.268-2.943-9.543-7a9.97 9.97 0 011.563-3.029m5.858.908a3 3 0 114.243 4.243M9.878 9.878l4.242 4.242M9.88 9.88l-3.29-3.29m7.532 7.532l3.29 3.29M3 3l3.59 3.59m0 0A9.953 9.953 0 0112 5c4.478 0 8.268 2.943 9.543 7a10.025 10.025 0 01-4.132 5.411m0 0L21 21" />
  ) : (
     <>
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M15 12a3 3 0 11-6 0 3 3 0 016 0z" />
    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2}
  d="M2.458 12C3.732 7.943 7.523 5 12 5c4.478 0 8.268 2.943 9.542 7-1.274 4.057-5.064 7-9.542 7-4.477 0-8.268-2.943-9.542-7z" />
      </>
     )}
        </svg>
              </button>
   </div>
       <FieldError message={errors.adminPasswordConfirm?.message} />
          </div>
   </div>
            )}

       {/* ── Step 4: Review & install ── */}
    {step === 4 && (
              <div className="space-y-5">
     <h2 className="text-base font-semibold text-slate-900">Review & install</h2>
       <p className="text-sm text-slate-500">
        Everything looks good? Click <strong>Install</strong> to complete the setup.
 </p>

  <dl className="divide-y divide-slate-100 rounded-lg border border-slate-200 text-sm">
          {[
       { label: 'Organisation', value: v.tenantDisplayName },
     { label: 'Slug', value: v.tenantSlug },
       { label: 'Default site', value: v.defaultSiteName },
         { label: 'Locale', value: v.defaultLocale },
        { label: 'Time zone', value: v.timeZoneId },
 { label: 'Admin name', value: v.adminDisplayName },
  { label: 'Admin email', value: v.adminEmail },
       { label: 'Password', value: '••••••••' },
      ].map(({ label, value }) => (
              <div key={label} className="flex justify-between px-4 py-2.5">
         <dt className="font-medium text-slate-600">{label}</dt>
             <dd className="text-slate-900">{value}</dd>
            </div>
        ))}
              </dl>

     <div className="rounded-lg border border-amber-200 bg-amber-50 p-3 text-xs text-amber-800">
      <strong>Note:</strong> The tenant slug cannot be changed after installation.
   Make sure it is correct before proceeding.
                </div>
    </div>
            )}

 {/* ── Navigation buttons ── */}
         <div className="mt-8 flex justify-between gap-3">
        {step > 1 ? (
    <button type="button" onClick={handleBack} className="btn-secondary">
       ← Back
     </button>
        ) : (
         <span />
              )}

              {step < 4 ? (
         <button type="button" onClick={handleNext} className="btn-primary">
       Next →
         </button>
   ) : (
      <button
        type="submit"
  disabled={isSubmitting}
      className="btn-primary min-w-[120px] justify-center"
         >
                  {isSubmitting ? (
  <span className="flex items-center gap-2">
      <svg className="h-4 w-4 animate-spin" fill="none" viewBox="0 0 24 24">
        <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
     <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
      </svg>
    Installing…
     </span>
               ) : (
      '🚀 Install'
        )}
         </button>
     )}
          </div>
     </form>
        </div>

     <p className="mt-6 text-center text-xs text-slate-400">
 MicroCMS · First-run setup
        </p>
      </div>
</div>
  );
}
