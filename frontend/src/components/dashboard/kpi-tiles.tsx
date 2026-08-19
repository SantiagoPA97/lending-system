import type { ReactNode } from 'react'
import { AlertTriangle, CheckCircle2 } from 'lucide-react'
import { Card, CardContent } from '@/components/ui/card'
import { MoneyValue } from '@/components/domain/money-value'
import { formatDate, moneyAmount } from '@/lib/format'
import { cn } from '@/lib/utils'
import type {
  CurrencyPortfolioResponse,
  CurrencyRepaymentsResponse,
  FacilityStatus,
  OverdueInstallmentsResponse,
} from '@/types/api'
import { FACILITY_STATUSES } from '@/types/api'

function TileLabel({ children }: { children: ReactNode }) {
  return (
    <p className="text-xs font-semibold uppercase tracking-[0.08em] text-muted">{children}</p>
  )
}

export function PortfolioTile({ entry }: { entry: CurrencyPortfolioResponse }) {
  const pct =
    entry.totalCommitted > 0
      ? Math.min(100, Math.round((entry.totalOutstanding / entry.totalCommitted) * 100))
      : 0
  return (
    <Card className="h-full">
      <CardContent className="flex h-full flex-col gap-3">
        <div className="flex items-baseline justify-between gap-2">
          <TileLabel>{entry.currency} outstanding</TileLabel>
          <span className="whitespace-nowrap text-xs text-faint">
            {entry.facilityCount} {entry.facilityCount === 1 ? 'facility' : 'facilities'}
          </span>
        </div>
        <p className="text-[22px] font-semibold leading-none text-ink">
          {moneyAmount(entry.totalOutstanding)}
        </p>
        <div className="mt-auto">
          <div className="h-1.5 w-full overflow-hidden rounded-xs bg-accent-soft">
            <div className="h-full rounded-xs bg-accent" style={{ width: `${pct}%` }} />
          </div>
          <p className="mt-1.5 text-xs text-muted">
            {pct}% of <MoneyValue amount={entry.totalCommitted} currency={entry.currency} muted />{' '}
            committed
          </p>
        </div>
      </CardContent>
    </Card>
  )
}

export function FacilitiesTile({
  counts,
}: {
  counts: Partial<Record<FacilityStatus, number>>
}) {
  const total = FACILITY_STATUSES.reduce((sum, status) => sum + (counts[status] ?? 0), 0)
  const breakdown = FACILITY_STATUSES.filter((status) => (counts[status] ?? 0) > 0)
    .map((status) => `${counts[status]} ${status}`)
    .join(' · ')
  return (
    <Card className="h-full">
      <CardContent className="flex h-full flex-col gap-3">
        <TileLabel>Facilities</TileLabel>
        <p className="text-[22px] font-semibold leading-none text-ink">
          {total}
          <span className="ml-1.5 text-sm font-normal text-muted">on the book</span>
        </p>
        <p className="mt-auto text-xs text-muted">{breakdown || 'None yet'}</p>
      </CardContent>
    </Card>
  )
}

export function RepaymentsTile({ rows }: { rows: CurrencyRepaymentsResponse[] }) {
  const totalCount = rows.reduce((sum, r) => sum + r.count, 0)
  return (
    <Card className="h-full">
      <CardContent className="flex h-full flex-col gap-3">
        <TileLabel>Repayments · last 30 days</TileLabel>
        <p className="text-[22px] font-semibold leading-none text-ink">
          {totalCount}
          <span className="ml-1.5 text-sm font-normal text-muted">
            {totalCount === 1 ? 'payment' : 'payments'}
          </span>
        </p>
        <div className="mt-auto space-y-1">
          {rows.length === 0 ? (
            <p className="text-xs text-muted">Nothing received in this window</p>
          ) : (
            rows.map((r) => (
              <p key={r.currency} className="text-xs text-muted">
                <MoneyValue amount={r.netAmount} currency={r.currency} muted /> net
              </p>
            ))
          )}
        </div>
      </CardContent>
    </Card>
  )
}

export function OverdueTile({ data }: { data: OverdueInstallmentsResponse }) {
  const overdue = data.count > 0
  return (
    <Card className={cn('h-full', overdue && 'border-danger/40')}>
      <CardContent className="flex h-full flex-col gap-3">
        <div className="flex items-baseline justify-between gap-2">
          <TileLabel>Overdue installments</TileLabel>
          {overdue ? (
            <AlertTriangle className="size-4 shrink-0 self-center text-danger" />
          ) : (
            <CheckCircle2 className="size-4 shrink-0 self-center text-positive" />
          )}
        </div>
        <p
          className={cn(
            'text-[22px] font-semibold leading-none',
            overdue ? 'text-danger' : 'text-ink',
          )}
        >
          {data.count}
        </p>
        <p className={cn('mt-auto text-xs', overdue ? 'text-danger' : 'text-muted')}>
          {overdue && data.oldestDueDate
            ? `Oldest due ${formatDate(data.oldestDueDate)}`
            : 'All installments current'}
        </p>
      </CardContent>
    </Card>
  )
}
