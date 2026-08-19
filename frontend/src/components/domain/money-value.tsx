import { moneyAmount } from '@/lib/format'
import { cn } from '@/lib/utils'
import type { Currency } from '@/types/api'

export function MoneyValue({
  amount,
  currency,
  className,
  muted,
  hideCurrency,
}: {
  amount: number
  currency: Currency | string
  className?: string
  muted?: boolean
  hideCurrency?: boolean
}) {
  return (
    <span className={cn('tabular whitespace-nowrap', className)}>
      {!hideCurrency && (
        <span className={cn('mr-1 text-[0.8em] font-medium uppercase tracking-wide', muted ? 'text-faint' : 'text-muted')}>
          {currency}
        </span>
      )}
      <span className={cn(muted ? 'text-muted' : 'text-ink')}>{moneyAmount(amount)}</span>
    </span>
  )
}
