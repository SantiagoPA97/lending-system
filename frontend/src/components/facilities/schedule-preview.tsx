import { CalendarRange } from 'lucide-react'
import { MoneyValue } from '@/components/domain/money-value'
import { Skeleton } from '@/components/ui/skeleton'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { formatDate } from '@/lib/format'
import type { SchedulePreviewResponse } from '@/types/api'

export type PreviewState = 'idle' | 'loading' | 'ready' | 'error'

export function SchedulePreviewPanel({
  state,
  preview,
}: {
  state: PreviewState
  preview: SchedulePreviewResponse | null
}) {
  return (
    <div className="flex h-full min-w-0 flex-col overflow-hidden rounded-md border border-line bg-paper">
      <div className="border-b border-line px-4 py-3">
        <p className="text-xs font-semibold uppercase tracking-wider text-muted">Schedule preview</p>
      </div>
      {state === 'idle' && (
        <div className="flex flex-1 flex-col items-center justify-center gap-2 px-6 py-10 text-center">
          <CalendarRange className="size-6 text-faint" />
          <p className="text-[13px] text-muted">
            Complete the terms to preview the repayment schedule before saving.
          </p>
        </div>
      )}
      {state === 'loading' && (
        <div className="space-y-2 p-4">
          <Skeleton className="h-12 w-full" />
          <Skeleton className="h-7 w-full" />
          <Skeleton className="h-7 w-full" />
          <Skeleton className="h-7 w-full" />
        </div>
      )}
      {state === 'error' && (
        <div className="flex flex-1 items-center justify-center px-6 py-10 text-center">
          <p className="text-[13px] text-muted">A schedule could not be computed for these terms.</p>
        </div>
      )}
      {state === 'ready' && preview && (
        <>
          <div className="flex flex-wrap gap-x-5 gap-y-2 border-b border-line px-4 py-3">
            <PreviewStat
              label={preview.installments.length > 1 ? 'Monthly payment' : 'Payment at maturity'}
              amount={preview.installments[0]?.totalDue ?? 0}
              currency={preview.currency}
            />
            <PreviewStat label="Total interest" amount={preview.totalInterest} currency={preview.currency} />
            <PreviewStat label="Total payable" amount={preview.totalPayable} currency={preview.currency} />
          </div>
          <div className="max-h-64 flex-1 overflow-auto">
            <Table className="text-[13px]">
              <TableHeader>
                <TableRow className="hover:bg-transparent">
                  <TableHead className="first:pl-4">#</TableHead>
                  <TableHead>Due</TableHead>
                  <TableHead className="text-right">Principal</TableHead>
                  <TableHead className="text-right">Interest</TableHead>
                  <TableHead className="text-right last:pr-4">Total</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {preview.installments.map((item) => (
                  <TableRow key={item.period}>
                    <TableCell className="tabular py-1.5 text-muted first:pl-4">{item.period}</TableCell>
                    <TableCell className="whitespace-nowrap py-1.5">{formatDate(item.dueDate)}</TableCell>
                    <TableCell className="py-1.5 text-right">
                      <MoneyValue amount={item.principalDue} currency={preview.currency} muted hideCurrency />
                    </TableCell>
                    <TableCell className="py-1.5 text-right">
                      <MoneyValue amount={item.interestDue} currency={preview.currency} muted hideCurrency />
                    </TableCell>
                    <TableCell className="py-1.5 text-right last:pr-4">
                      <MoneyValue amount={item.totalDue} currency={preview.currency} hideCurrency />
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          </div>
        </>
      )}
    </div>
  )
}

function PreviewStat({
  label,
  amount,
  currency,
}: {
  label: string
  amount: number
  currency: string
}) {
  return (
    <div>
      <p className="text-[11px] font-medium uppercase tracking-wider text-muted">{label}</p>
      <MoneyValue amount={amount} currency={currency} className="text-[13px]" />
    </div>
  )
}
