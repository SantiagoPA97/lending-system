import { moneyAmount } from '@/lib/format'
import { cn } from '@/lib/utils'
import type { Currency } from '@/types/api'

export function MoneyValue({
  amount,
  currency,
  className,
  muted,
}: {
  amount: number
  currency: Currency | string
  className?: string
  muted?: boolean
}) {
  return (
    <span className={cn('tabular whitespace-nowrap', className)}>
      <span className={cn('mr-1 text-[0.8em] font-medium uppercase tracking-wide', muted ? 'text-faint' : 'text-muted')}>
        {currency}
      </span>
      <span className={cn(muted ? 'text-muted' : 'text-ink')}>{moneyAmount(amount)}</span>
    </span>
  )
}
