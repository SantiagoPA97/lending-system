import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { toast } from 'sonner'
import { z } from 'zod'
import { MoneyValue } from '@/components/domain/money-value'
import { Button } from '@/components/ui/button'
import { Dialog, DialogBody, DialogFooter, DialogHeader } from '@/components/ui/dialog'
import { FieldError, Input, Label, Textarea } from '@/components/ui/input'
import { facilityErrorMessage } from '@/components/facilities/facility-errors'
import { ApiError } from '@/lib/api'
import { money, toDateInputValue } from '@/lib/format'
import { useRecordRepayment } from '@/lib/queries'
import type { FacilityDetailResponse } from '@/types/api'

const formSchema = z.object({
  amount: z
    .string()
    .min(1, 'Amount is required')
    .regex(/^\d{1,12}(\.\d{1,2})?$/, 'Use a positive amount with at most 2 decimals')
    .refine((v) => Number(v) > 0, 'Amount must be greater than zero'),
  paymentDate: z.string().min(1, 'Payment date is required'),
  note: z.string().max(500, 'Keep the note under 500 characters').optional(),
})

type FormValues = z.infer<typeof formSchema>

export function RecordRepaymentDialog({
  open,
  onClose,
  facility,
}: {
  open: boolean
  onClose: () => void
  facility: FacilityDetailResponse
}) {
  const recordRepayment = useRecordRepayment(facility.id)

  const {
    register,
    handleSubmit,
    setError,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(formSchema),
    defaultValues: { amount: '', paymentDate: toDateInputValue(new Date()), note: '' },
  })

  const onSubmit = handleSubmit(async (values) => {
    try {
      const result = await recordRepayment.mutateAsync({
        amount: Number(values.amount),
        currency: facility.currency,
        paymentDate: values.paymentDate,
        note: values.note?.trim() ? values.note.trim() : null,
      })
      toast.success('Repayment recorded', {
        description: `Interest ${money(result.interestPaid, facility.currency)} · Principal ${money(
          result.principalPaid,
          facility.currency,
        )} · Outstanding ${money(result.newOutstanding, facility.currency)}`,
      })
      if (result.facilityStatus === 'Completed') {
        toast.success('Facility fully repaid — status is now Completed')
      }
      onClose()
    } catch (err) {
      if (err instanceof ApiError && err.errors) {
        let matched = false
        for (const [rawKey, messages] of Object.entries(err.errors)) {
          const key = rawKey.charAt(0).toLowerCase() + rawKey.slice(1)
          if (key === 'amount' || key === 'paymentDate' || key === 'note') {
            setError(key, { message: messages.join(' ') })
            matched = true
          }
        }
        if (matched) return
      }
      toast.error(facilityErrorMessage(err))
    }
  })

  return (
    <Dialog open={open} onClose={onClose} className="max-w-md">
      <DialogHeader title="Record repayment" onClose={onClose}>
        <p className="mt-0.5 font-mono text-[13px] text-muted">{facility.reference}</p>
      </DialogHeader>
      <form onSubmit={onSubmit}>
        <DialogBody className="space-y-4">
          <div className="flex items-baseline justify-between rounded-sm bg-paper px-3.5 py-2.5">
            <span className="text-[13px] text-muted">Outstanding principal</span>
            <MoneyValue amount={facility.outstandingPrincipal} currency={facility.currency} />
          </div>
          <div>
            <Label htmlFor="repayment-amount">Amount</Label>
            <div className="relative">
              <span className="pointer-events-none absolute left-3 top-1/2 -translate-y-1/2 text-[13px] font-medium uppercase text-muted">
                {facility.currency}
              </span>
              <Input
                id="repayment-amount"
                inputMode="decimal"
                placeholder="0.00"
                className="tabular pl-12 text-right"
                aria-invalid={!!errors.amount}
                autoFocus
                {...register('amount')}
              />
            </div>
            <FieldError message={errors.amount?.message} />
            <p className="mt-1 text-xs text-muted">
              Allocated to unpaid interest due first, then to principal.
            </p>
          </div>
          <div>
            <Label htmlFor="repayment-date">Payment date</Label>
            <Input
              id="repayment-date"
              type="date"
              aria-invalid={!!errors.paymentDate}
              {...register('paymentDate')}
            />
            <FieldError message={errors.paymentDate?.message} />
          </div>
          <div>
            <Label htmlFor="repayment-note">Note</Label>
            <Textarea
              id="repayment-note"
              placeholder="Optional note for the ledger"
              {...register('note')}
            />
            <FieldError message={errors.note?.message} />
          </div>
        </DialogBody>
        <DialogFooter>
          <Button type="button" variant="secondary" onClick={onClose} disabled={isSubmitting}>
            Cancel
          </Button>
          <Button type="submit" loading={isSubmitting}>
            Record repayment
          </Button>
        </DialogFooter>
      </form>
    </Dialog>
  )
}
