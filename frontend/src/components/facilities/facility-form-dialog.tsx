import { useEffect, useRef, useState } from 'react'
import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { useNavigate } from 'react-router-dom'
import { toast } from 'sonner'
import { z } from 'zod'
import { Button } from '@/components/ui/button'
import { Dialog, DialogBody, DialogFooter, DialogHeader } from '@/components/ui/dialog'
import { FieldError, Input, Label } from '@/components/ui/input'
import { Select } from '@/components/ui/select'
import { SchedulePreviewPanel, type PreviewState } from '@/components/facilities/schedule-preview'
import { facilityErrorMessage } from '@/components/facilities/facility-errors'
import { ApiError } from '@/lib/api'
import { toDateInputValue } from '@/lib/format'
import { repaymentTypeLabels } from '@/lib/labels'
import { cn } from '@/lib/utils'
import {
  useCompanies,
  useCreateFacility,
  useSchedulePreview,
  useUpdateFacility,
} from '@/lib/queries'
import { CURRENCIES, type FacilityDetailResponse, type FacilityTermsRequest, type SchedulePreviewResponse } from '@/types/api'

const MAX_AMOUNT = 999_999_999_999.99

const formSchema = z.object({
  companyId: z.string().min(1, 'Select a company'),
  commitmentAmount: z
    .string()
    .min(1, 'Amount is required')
    .regex(/^\d{1,12}(\.\d{1,2})?$/, 'Use a positive amount with at most 2 decimals')
    .refine((v) => Number(v) >= 0.01 && Number(v) <= MAX_AMOUNT, 'Amount is out of range'),
  currency: z.enum(['USD', 'EUR', 'GBP', 'COP']),
  annualInterestRate: z
    .string()
    .min(1, 'Rate is required')
    .regex(/^\d{1,3}(\.\d{1,4})?$/, 'Use at most 4 decimals')
    .refine((v) => Number(v) <= 100, 'Rate must be between 0 and 100'),
  termMonths: z
    .string()
    .min(1, 'Term is required')
    .regex(/^\d+$/, 'Whole months only')
    .refine((v) => Number(v) >= 1 && Number(v) <= 480, 'Term must be 1 to 480 months'),
  startDate: z.string().min(1, 'Start date is required'),
  repaymentType: z.enum(['Bullet', 'Amortizing', 'InterestOnly']),
})

type FormValues = z.infer<typeof formSchema>

const repaymentTypeOptions: { value: FormValues['repaymentType']; blurb: string }[] = [
  { value: 'Amortizing', blurb: 'Equal monthly installments of principal and interest.' },
  { value: 'InterestOnly', blurb: 'Monthly interest with a principal balloon at maturity.' },
  { value: 'Bullet', blurb: 'A single payment of principal and interest at maturity.' },
]

function toTerms(values: FormValues): FacilityTermsRequest {
  return {
    commitmentAmount: Number(values.commitmentAmount),
    currency: values.currency,
    annualInterestRate: Number(values.annualInterestRate),
    termMonths: Number(values.termMonths),
    startDate: values.startDate,
    repaymentType: values.repaymentType,
  }
}

export function FacilityFormDialog({
  open,
  onClose,
  facility,
  initialCompanyId,
}: {
  open: boolean
  onClose: () => void
  facility?: FacilityDetailResponse
  initialCompanyId?: string
}) {
  const navigate = useNavigate()
  const createFacility = useCreateFacility()
  const updateFacility = useUpdateFacility(facility?.id ?? '')
  const schedulePreview = useSchedulePreview()
  const activeCompanies = useCompanies({ status: 'Active', pageSize: 100 })

  const {
    register,
    handleSubmit,
    watch,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(formSchema),
    defaultValues: facility
      ? {
          companyId: facility.company.id,
          commitmentAmount: String(facility.commitmentAmount),
          currency: facility.currency,
          annualInterestRate: String(facility.annualInterestRate),
          termMonths: String(facility.termMonths),
          startDate: facility.startDate,
          repaymentType: facility.repaymentType,
        }
      : {
          companyId: initialCompanyId ?? '',
          commitmentAmount: '',
          currency: 'USD',
          annualInterestRate: '',
          termMonths: '12',
          startDate: toDateInputValue(new Date()),
          repaymentType: 'Amortizing',
        },
  })

  const [preview, setPreview] = useState<SchedulePreviewResponse | null>(null)
  const [previewState, setPreviewState] = useState<PreviewState>('idle')
  const previewSeq = useRef(0)

  const termValues = watch([
    'commitmentAmount',
    'currency',
    'annualInterestRate',
    'termMonths',
    'startDate',
    'repaymentType',
  ])
  const termsKey = termValues.join('|')
  const selectedType = watch('repaymentType')

  useEffect(() => {
    const [commitmentAmount, currency, annualInterestRate, termMonths, startDate, repaymentType] =
      termsKey.split('|')
    const parsed = formSchema.omit({ companyId: true }).safeParse({
      commitmentAmount,
      currency,
      annualInterestRate,
      termMonths,
      startDate,
      repaymentType,
    })
    if (!parsed.success) {
      previewSeq.current += 1
      setPreviewState('idle')
      setPreview(null)
      return
    }
    const seq = ++previewSeq.current
    setPreviewState('loading')
    const timer = setTimeout(() => {
      schedulePreview
        .mutateAsync({
          commitmentAmount: Number(parsed.data.commitmentAmount),
          currency: parsed.data.currency,
          annualInterestRate: Number(parsed.data.annualInterestRate),
          termMonths: Number(parsed.data.termMonths),
          startDate: parsed.data.startDate,
          repaymentType: parsed.data.repaymentType,
        })
        .then((result) => {
          if (previewSeq.current !== seq) return
          setPreview(result)
          setPreviewState('ready')
        })
        .catch(() => {
          if (previewSeq.current !== seq) return
          setPreview(null)
          setPreviewState('error')
        })
    }, 400)
    return () => clearTimeout(timer)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [termsKey])

  const applyServerError = (err: unknown) => {
    if (err instanceof ApiError && err.errors) {
      const fields = Object.keys(formSchema.shape)
      let matched = false
      for (const [rawKey, messages] of Object.entries(err.errors)) {
        const key = rawKey.charAt(0).toLowerCase() + rawKey.slice(1)
        if (fields.includes(key)) {
          setError(key as keyof FormValues, { message: messages.join(' ') })
          matched = true
        }
      }
      if (matched) return
    }
    toast.error(facilityErrorMessage(err))
  }

  const onSubmit = handleSubmit(async (values) => {
    try {
      if (facility) {
        const updated = await updateFacility.mutateAsync(toTerms(values))
        toast.success(`${updated.reference} terms updated`)
        onClose()
      } else {
        const created = await createFacility.mutateAsync({
          companyId: values.companyId,
          ...toTerms(values),
        })
        toast.success(`${created.reference} created as draft`)
        onClose()
        navigate(`/facilities/${created.id}`)
      }
    } catch (err) {
      applyServerError(err)
    }
  })

  return (
    <Dialog open={open} onClose={onClose} className="max-w-4xl">
      <DialogHeader
        title={facility ? `Edit ${facility.reference}` : 'New facility'}
        onClose={onClose}
      >
        <p className="mt-0.5 text-[13px] text-muted">
          {facility
            ? 'Terms stay editable until the facility is activated.'
            : 'Facilities start as drafts — activation locks the terms and generates the schedule.'}
        </p>
      </DialogHeader>
      <form onSubmit={onSubmit}>
        <DialogBody className="grid gap-5 md:grid-cols-[minmax(0,1fr)_minmax(0,340px)]">
          <div className="space-y-4">
            {!facility && (
              <div>
                <Label htmlFor="facility-company">Company</Label>
                <Select
                  id="facility-company"
                  aria-invalid={!!errors.companyId}
                  {...register('companyId')}
                >
                  <option value="">Select an active company</option>
                  {activeCompanies.data?.items.map((company) => (
                    <option key={company.id} value={company.id}>
                      {company.name}
                    </option>
                  ))}
                </Select>
                <FieldError message={errors.companyId?.message} />
              </div>
            )}
            <div className="grid grid-cols-[minmax(0,1fr)_104px] gap-3">
              <div>
                <Label htmlFor="facility-amount">Commitment amount</Label>
                <Input
                  id="facility-amount"
                  inputMode="decimal"
                  placeholder="1000000.00"
                  aria-invalid={!!errors.commitmentAmount}
                  {...register('commitmentAmount')}
                />
                <FieldError message={errors.commitmentAmount?.message} />
              </div>
              <div>
                <Label htmlFor="facility-currency">Currency</Label>
                <Select id="facility-currency" {...register('currency')}>
                  {CURRENCIES.map((currency) => (
                    <option key={currency} value={currency}>
                      {currency}
                    </option>
                  ))}
                </Select>
              </div>
            </div>
            <div className="grid grid-cols-2 gap-3">
              <div>
                <Label htmlFor="facility-rate">Annual interest rate (%)</Label>
                <Input
                  id="facility-rate"
                  inputMode="decimal"
                  placeholder="7.35"
                  aria-invalid={!!errors.annualInterestRate}
                  {...register('annualInterestRate')}
                />
                <FieldError message={errors.annualInterestRate?.message} />
              </div>
              <div>
                <Label htmlFor="facility-term">Term (months)</Label>
                <Input
                  id="facility-term"
                  inputMode="numeric"
                  aria-invalid={!!errors.termMonths}
                  {...register('termMonths')}
                />
                <FieldError message={errors.termMonths?.message} />
              </div>
            </div>
            <div>
              <Label htmlFor="facility-start">Start date</Label>
              <Input
                id="facility-start"
                type="date"
                aria-invalid={!!errors.startDate}
                {...register('startDate')}
              />
              <FieldError message={errors.startDate?.message} />
            </div>
            <fieldset>
              <legend className="mb-1.5 block text-[13px] font-medium text-body">
                Repayment type
              </legend>
              <div className="space-y-2">
                {repaymentTypeOptions.map((option) => (
                  <label
                    key={option.value}
                    className={cn(
                      'flex cursor-pointer items-start gap-2.5 rounded-sm border px-3 py-2.5 transition-colors',
                      selectedType === option.value
                        ? 'border-accent bg-accent-soft/50'
                        : 'border-line-strong hover:border-faint',
                    )}
                  >
                    <input
                      type="radio"
                      value={option.value}
                      className="mt-1 accent-accent"
                      {...register('repaymentType')}
                    />
                    <span>
                      <span className="block text-sm font-medium text-ink">
                        {repaymentTypeLabels[option.value]}
                      </span>
                      <span className="block text-[13px] text-muted">{option.blurb}</span>
                    </span>
                  </label>
                ))}
              </div>
            </fieldset>
          </div>
          <SchedulePreviewPanel state={previewState} preview={preview} />
        </DialogBody>
        <DialogFooter>
          <Button type="button" variant="secondary" onClick={onClose} disabled={isSubmitting}>
            Cancel
          </Button>
          <Button type="submit" loading={isSubmitting}>
            {facility ? 'Save terms' : 'Create draft facility'}
          </Button>
        </DialogFooter>
      </form>
    </Dialog>
  )
}
