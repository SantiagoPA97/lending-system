import { useState } from 'react'
import { Undo2 } from 'lucide-react'
import { toast } from 'sonner'
import { ConfirmDialog } from '@/components/domain/confirm-dialog'
import { MoneyValue } from '@/components/domain/money-value'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import { SkeletonRows } from '@/components/ui/skeleton'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { facilityErrorMessage } from '@/components/facilities/facility-errors'
import { formatDate, money } from '@/lib/format'
import { cn } from '@/lib/utils'
import { useRepayments, useReverseRepayment } from '@/lib/queries'
import type { FacilityDetailResponse, RepaymentResponse } from '@/types/api'

export function RepaymentsTab({ facility }: { facility: FacilityDetailResponse }) {
  const { data, isLoading } = useRepayments(facility.id)
  const reverseRepayment = useReverseRepayment()
  const [reversing, setReversing] = useState<RepaymentResponse | null>(null)

  const confirmReverse = async () => {
    if (!reversing) return
    try {
      const result = await reverseRepayment.mutateAsync({ repaymentId: reversing.id })
      toast.success('Repayment reversed', {
        description: `Outstanding restored to ${money(result.newOutstanding, facility.currency)}`,
      })
      setReversing(null)
    } catch (err) {
      toast.error(facilityErrorMessage(err))
      setReversing(null)
    }
  }

  if (isLoading) {
    return (
      <Card>
        <SkeletonRows rows={5} />
      </Card>
    )
  }

  if (!data || data.length === 0) {
    return (
      <Card>
        <p className="px-6 py-10 text-center text-sm text-muted">
          No repayments recorded yet.
          {facility.status === 'Active' && ' Use “Record repayment” to post the first entry.'}
        </p>
      </Card>
    )
  }

  return (
    <Card>
      <Table>
        <TableHeader>
          <TableRow className="hover:bg-transparent">
            <TableHead>Date</TableHead>
            <TableHead>Entry</TableHead>
            <TableHead>Applied to</TableHead>
            <TableHead className="text-right">Interest</TableHead>
            <TableHead className="text-right">Principal</TableHead>
            <TableHead className="text-right">Amount</TableHead>
            <TableHead className="text-right" />
          </TableRow>
        </TableHeader>
        <TableBody>
          {data.map((repayment) => (
            <RepaymentRow
              key={repayment.id}
              repayment={repayment}
              currency={facility.currency}
              onReverse={() => setReversing(repayment)}
            />
          ))}
        </TableBody>
      </Table>
      <p className="border-t border-line px-5 py-3 text-xs text-muted">
        Repayments are immutable ledger entries — corrections are posted as reversals, never edits.
      </p>
      <ConfirmDialog
        open={!!reversing}
        onClose={() => setReversing(null)}
        onConfirm={confirmReverse}
        title="Reverse repayment"
        description={
          reversing && (
            <>
              Posts an offsetting entry of{' '}
              <span className="tabular font-medium text-ink">
                {money(reversing.amount, reversing.currency)}
              </span>{' '}
              and restores its principal to the outstanding balance. The original entry stays in the
              ledger.
            </>
          )
        }
        confirmLabel="Reverse repayment"
        destructive
        loading={reverseRepayment.isPending}
      />
    </Card>
  )
}

function RepaymentRow({
  repayment,
  currency,
  onReverse,
}: {
  repayment: RepaymentResponse
  currency: string
  onReverse: () => void
}) {
  const reversed = !!repayment.reversedByRepaymentId
  const periods = repayment.allocations.map((a) => `#${a.period}`).join(', ')

  return (
    <TableRow className={cn(repayment.isReversal && 'bg-paper/80')}>
      <TableCell className="whitespace-nowrap">{formatDate(repayment.paymentDate)}</TableCell>
      <TableCell>
        {repayment.isReversal ? (
          <Badge tone="warn">Reversal</Badge>
        ) : reversed ? (
          <Badge tone="neutral">Reversed</Badge>
        ) : (
          <span className="text-muted">Payment</span>
        )}
      </TableCell>
      <TableCell className="tabular text-[13px] text-muted">{periods || '—'}</TableCell>
      <TableCell className="text-right">
        <MoneyValue amount={repayment.interestApplied} currency={currency} muted={repayment.isReversal} />
      </TableCell>
      <TableCell className="text-right">
        <MoneyValue amount={repayment.principalApplied} currency={currency} muted={repayment.isReversal} />
      </TableCell>
      <TableCell className="text-right">
        <span className={cn(reversed && 'line-through decoration-danger/40')}>
          <MoneyValue
            amount={repayment.amount}
            currency={currency}
            muted={repayment.isReversal || reversed}
            className={cn(!repayment.isReversal && !reversed && 'font-medium')}
          />
        </span>
      </TableCell>
      <TableCell className="text-right">
        {!repayment.isReversal && !reversed && (
          <Button variant="ghost" size="sm" onClick={onReverse} className="text-danger hover:text-danger">
            <Undo2 className="size-4" />
            Reverse
          </Button>
        )}
      </TableCell>
    </TableRow>
  )
}
