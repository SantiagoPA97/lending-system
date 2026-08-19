import { useEffect, useState } from 'react'
import { Check, Info } from 'lucide-react'
import { MoneyValue } from '@/components/domain/money-value'
import { Card } from '@/components/ui/card'
import { SkeletonRows } from '@/components/ui/skeleton'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { formatDate, toDateInputValue } from '@/lib/format'
import { cn } from '@/lib/utils'
import { useSchedule, useSchedulePreview } from '@/lib/queries'
import type {
  Currency,
  FacilityDetailResponse,
  SchedulePreviewResponse,
  ScheduleItemResponse,
} from '@/types/api'

export function ScheduleTab({ facility }: { facility: FacilityDetailResponse }) {
  if (facility.status === 'Draft') return <DraftProjection facility={facility} />
  return <ActivatedSchedule facility={facility} />
}

function ActivatedSchedule({ facility }: { facility: FacilityDetailResponse }) {
  const { data, isLoading } = useSchedule(facility.id)
  const today = toDateInputValue(new Date())

  if (isLoading) {
    return (
      <Card>
        <SkeletonRows rows={6} />
      </Card>
    )
  }
  if (!data || data.items.length === 0) {
    return (
      <Card>
        <p className="px-6 py-10 text-center text-sm text-muted">No schedule was generated.</p>
      </Card>
    )
  }

  const highlightOverdue = facility.status === 'Active' || facility.status === 'Defaulted'

  return (
    <Card>
      <Table>
        <TableHeader>
          <TableRow className="hover:bg-transparent">
            <TableHead>#</TableHead>
            <TableHead>Due date</TableHead>
            <TableHead className="text-right">Principal</TableHead>
            <TableHead className="text-right">Interest</TableHead>
            <TableHead className="text-right">Total due</TableHead>
            <TableHead className="text-right">Remaining</TableHead>
            <TableHead className="text-right">Paid</TableHead>
            <TableHead className="text-right">Settled</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {data.items.map((item) => {
            const overdue = highlightOverdue && !item.isSettled && item.dueDate < today
            return (
              <ScheduleRow key={item.period} item={item} currency={data.currency} overdue={overdue} />
            )
          })}
        </TableBody>
      </Table>
      <TotalsFooter
        currency={data.currency}
        totalPrincipal={data.totalPrincipal}
        totalInterest={data.totalInterest}
        totalPayable={data.totalPayable}
      />
    </Card>
  )
}

function ScheduleRow({
  item,
  currency,
  overdue,
}: {
  item: ScheduleItemResponse
  currency: Currency
  overdue: boolean
}) {
  const paid = item.principalPaid + item.interestPaid
  return (
    <TableRow className={cn(overdue && 'bg-danger-soft/50 hover:bg-danger-soft/70')}>
      <TableCell className="tabular text-muted">{item.period}</TableCell>
      <TableCell className={cn('whitespace-nowrap', overdue && 'font-medium text-danger')}>
        {formatDate(item.dueDate)}
        {overdue && <span className="ml-2 text-xs font-semibold uppercase tracking-wide">Overdue</span>}
      </TableCell>
      <TableCell className="text-right">
        <MoneyValue amount={item.principalDue} currency={currency} />
      </TableCell>
      <TableCell className="text-right">
        <MoneyValue amount={item.interestDue} currency={currency} />
      </TableCell>
      <TableCell className="text-right">
        <MoneyValue amount={item.totalDue} currency={currency} />
      </TableCell>
      <TableCell className="text-right">
        <MoneyValue amount={item.remainingBalance} currency={currency} muted />
      </TableCell>
      <TableCell className="text-right">
        {paid > 0 ? (
          <MoneyValue amount={paid} currency={currency} />
        ) : (
          <span className="text-faint">—</span>
        )}
      </TableCell>
      <TableCell className="text-right">
        {item.isSettled && <Check className="ml-auto size-4 text-positive" aria-label="Settled" />}
      </TableCell>
    </TableRow>
  )
}

function DraftProjection({ facility }: { facility: FacilityDetailResponse }) {
  const preview = useSchedulePreview()
  const [projection, setProjection] = useState<SchedulePreviewResponse | null>(null)
  const [failed, setFailed] = useState(false)

  useEffect(() => {
    preview
      .mutateAsync({
        commitmentAmount: facility.commitmentAmount,
        currency: facility.currency,
        annualInterestRate: facility.annualInterestRate,
        termMonths: facility.termMonths,
        startDate: facility.startDate,
        repaymentType: facility.repaymentType,
      })
      .then(setProjection)
      .catch(() => setFailed(true))
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [facility.id, facility.updatedAtUtc])

  return (
    <Card>
      <div className="flex items-start gap-2 border-b border-line bg-paper px-5 py-3 text-[13px] text-muted">
        <Info className="mt-0.5 size-4 shrink-0" />
        <p>
          Projected schedule for the current draft terms. The definitive schedule is generated when
          the facility is activated.
        </p>
      </div>
      {failed && (
        <p className="px-6 py-10 text-center text-sm text-muted">
          A projection could not be computed for these terms.
        </p>
      )}
      {!failed && !projection && <SkeletonRows rows={6} />}
      {projection && (
        <>
          <Table>
            <TableHeader>
              <TableRow className="hover:bg-transparent">
                <TableHead>#</TableHead>
                <TableHead>Due date</TableHead>
                <TableHead className="text-right">Principal</TableHead>
                <TableHead className="text-right">Interest</TableHead>
                <TableHead className="text-right">Total due</TableHead>
                <TableHead className="text-right">Remaining</TableHead>
              </TableRow>
            </TableHeader>
            <TableBody>
              {projection.installments.map((item) => (
                <TableRow key={item.period}>
                  <TableCell className="tabular text-muted">{item.period}</TableCell>
                  <TableCell className="whitespace-nowrap">{formatDate(item.dueDate)}</TableCell>
                  <TableCell className="text-right">
                    <MoneyValue amount={item.principalDue} currency={projection.currency} />
                  </TableCell>
                  <TableCell className="text-right">
                    <MoneyValue amount={item.interestDue} currency={projection.currency} />
                  </TableCell>
                  <TableCell className="text-right">
                    <MoneyValue amount={item.totalDue} currency={projection.currency} />
                  </TableCell>
                  <TableCell className="text-right">
                    <MoneyValue amount={item.remainingBalance} currency={projection.currency} muted />
                  </TableCell>
                </TableRow>
              ))}
            </TableBody>
          </Table>
          <TotalsFooter
            currency={projection.currency}
            totalPrincipal={projection.totalPrincipal}
            totalInterest={projection.totalInterest}
            totalPayable={projection.totalPayable}
          />
        </>
      )}
    </Card>
  )
}

function TotalsFooter({
  currency,
  totalPrincipal,
  totalInterest,
  totalPayable,
}: {
  currency: Currency
  totalPrincipal: number
  totalInterest: number
  totalPayable: number
}) {
  return (
    <div className="flex flex-wrap items-center justify-end gap-x-6 gap-y-1 border-t border-line px-5 py-3 text-[13px]">
      <FooterStat label="Total principal" amount={totalPrincipal} currency={currency} />
      <FooterStat label="Total interest" amount={totalInterest} currency={currency} />
      <FooterStat label="Total payable" amount={totalPayable} currency={currency} strong />
    </div>
  )
}

function FooterStat({
  label,
  amount,
  currency,
  strong,
}: {
  label: string
  amount: number
  currency: Currency
  strong?: boolean
}) {
  return (
    <span className="flex items-baseline gap-2">
      <span className="text-xs uppercase tracking-wider text-muted">{label}</span>
      <MoneyValue amount={amount} currency={currency} className={cn(strong && 'font-semibold')} />
    </span>
  )
}
