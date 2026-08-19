import { useState, type ReactNode } from 'react'
import { X } from 'lucide-react'
import { Input } from '@/components/ui/input'
import { cn } from '@/lib/utils'
import { CURRENCIES, REPAYMENT_TYPES } from '@/types/api'

export type SearchFilterValues = {
  type: string
  status: string
  repaymentType: string
  currency: string
  minAmount: string
  maxAmount: string
}

const typeOptions = [
  { value: '', label: 'Everything' },
  { value: 'company', label: 'Companies' },
  { value: 'facility', label: 'Facilities' },
]

const statusOptions = ['Active', 'Draft', 'Completed', 'Cancelled', 'Defaulted', 'Inactive']

const repaymentTypeLabels: Record<string, string> = {
  Bullet: 'Bullet',
  Amortizing: 'Amortizing',
  InterestOnly: 'Interest-only',
}

function Chip({
  selected,
  onClick,
  children,
}: {
  selected: boolean
  onClick: () => void
  children: ReactNode
}) {
  return (
    <button
      type="button"
      onClick={onClick}
      aria-pressed={selected}
      className={cn(
        'inline-flex h-7 items-center rounded-sm border px-2.5 text-[13px] font-medium transition-colors',
        selected
          ? 'border-accent bg-accent-soft text-accent'
          : 'border-line-strong bg-surface text-body hover:border-faint hover:text-ink',
      )}
    >
      {children}
    </button>
  )
}

function ChipGroup({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div className="flex flex-wrap items-center gap-1.5">
      <span className="mr-1 w-20 shrink-0 text-xs font-semibold uppercase tracking-[0.08em] text-faint">
        {label}
      </span>
      {children}
    </div>
  )
}

export function SearchFilters({
  values,
  onChange,
  onClear,
}: {
  values: SearchFilterValues
  onChange: (key: keyof SearchFilterValues, value: string) => void
  onClear: () => void
}) {
  const [minDraft, setMinDraft] = useState(values.minAmount)
  const [maxDraft, setMaxDraft] = useState(values.maxAmount)
  const [prevAmounts, setPrevAmounts] = useState([values.minAmount, values.maxAmount])

  if (prevAmounts[0] !== values.minAmount || prevAmounts[1] !== values.maxAmount) {
    setPrevAmounts([values.minAmount, values.maxAmount])
    setMinDraft(values.minAmount)
    setMaxDraft(values.maxAmount)
  }

  const toggle = (key: keyof SearchFilterValues, value: string) => {
    onChange(key, values[key] === value ? '' : value)
  }

  const anyActive = Object.values(values).some((v) => v !== '')
  const facilityOnly =
    values.repaymentType !== '' ||
    values.currency !== '' ||
    values.minAmount !== '' ||
    values.maxAmount !== ''

  const commitAmounts = () => {
    if (minDraft !== values.minAmount) onChange('minAmount', minDraft)
    if (maxDraft !== values.maxAmount) onChange('maxAmount', maxDraft)
  }

  return (
    <div className="grid gap-2.5">
      <ChipGroup label="Show">
        {typeOptions.map((opt) => (
          <Chip
            key={opt.value}
            selected={values.type === opt.value}
            onClick={() => onChange('type', opt.value)}
          >
            {opt.label}
          </Chip>
        ))}
      </ChipGroup>
      <ChipGroup label="Status">
        {statusOptions.map((status) => (
          <Chip
            key={status}
            selected={values.status === status}
            onClick={() => toggle('status', status)}
          >
            {status}
          </Chip>
        ))}
      </ChipGroup>
      <ChipGroup label="Repayment">
        {REPAYMENT_TYPES.map((rt) => (
          <Chip
            key={rt}
            selected={values.repaymentType === rt}
            onClick={() => toggle('repaymentType', rt)}
          >
            {repaymentTypeLabels[rt]}
          </Chip>
        ))}
      </ChipGroup>
      <ChipGroup label="Currency">
        {CURRENCIES.map((currency) => (
          <Chip
            key={currency}
            selected={values.currency === currency}
            onClick={() => toggle('currency', currency)}
          >
            {currency}
          </Chip>
        ))}
      </ChipGroup>
      <div className="flex flex-wrap items-center gap-1.5">
        <span className="mr-1 w-20 shrink-0 text-xs font-semibold uppercase tracking-[0.08em] text-faint">
          Amount
        </span>
        <Input
          type="number"
          min={0}
          value={minDraft}
          onChange={(e) => setMinDraft(e.target.value)}
          onBlur={commitAmounts}
          onKeyDown={(e) => e.key === 'Enter' && commitAmounts()}
          placeholder="Min"
          aria-label="Minimum commitment amount"
          className="tabular h-7 w-28 text-[13px]"
        />
        <span className="text-xs text-faint">to</span>
        <Input
          type="number"
          min={0}
          value={maxDraft}
          onChange={(e) => setMaxDraft(e.target.value)}
          onBlur={commitAmounts}
          onKeyDown={(e) => e.key === 'Enter' && commitAmounts()}
          placeholder="Max"
          aria-label="Maximum commitment amount"
          className="tabular h-7 w-28 text-[13px]"
        />
        {facilityOnly && (
          <span className="text-xs text-faint">
            Type, currency and amount filters narrow results to facilities
          </span>
        )}
        {anyActive && (
          <button
            type="button"
            onClick={onClear}
            className="ml-auto inline-flex items-center gap-1 text-[13px] font-medium text-muted transition-colors hover:text-ink"
          >
            <X className="size-3.5" />
            Clear filters
          </button>
        )}
      </div>
    </div>
  )
}
