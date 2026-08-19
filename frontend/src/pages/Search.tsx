import { useEffect, useMemo, useState } from 'react'
import { useNavigate, useSearchParams } from 'react-router-dom'
import { Building2, ChevronLeft, ChevronRight, Landmark, SearchX, Search as SearchIcon, TriangleAlert } from 'lucide-react'
import { PageHeader } from '@/components/domain/page-header'
import { StatusBadge } from '@/components/domain/status-badge'
import { MoneyValue } from '@/components/domain/money-value'
import { SearchFilters, type SearchFilterValues } from '@/components/search/search-filters'
import { Card } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { SkeletonRows } from '@/components/ui/skeleton'
import { ApiError } from '@/lib/api'
import { repaymentTypeLabel } from '@/lib/labels'
import { useSearch } from '@/lib/queries'
import { isCompanyResult, normalizeAmountRange, searchResultPath } from '@/lib/search-result'
import type { SearchResultResponse } from '@/types/api'

const filterKeys = ['type', 'status', 'repaymentType', 'currency', 'minAmount', 'maxAmount'] as const

function parseAmount(value: string): number | undefined {
  const parsed = Number(value)
  return value !== '' && Number.isFinite(parsed) && parsed >= 0 ? parsed : undefined
}

function ResultRow({ result, onOpen }: { result: SearchResultResponse; onOpen: () => void }) {
  const isCompany = isCompanyResult(result)
  const Icon = isCompany ? Building2 : Landmark
  return (
    <li>
      <button
        type="button"
        onClick={onOpen}
        className="flex w-full items-center gap-3 px-5 py-3 text-left transition-colors hover:bg-paper focus:outline-none focus-visible:bg-paper"
      >
        <span className="flex size-8 shrink-0 items-center justify-center rounded-sm bg-accent-soft text-accent">
          <Icon className="size-4" />
        </span>
        <span className="min-w-0 flex-1">
          <span className="flex flex-wrap items-center gap-x-2 gap-y-0.5">
            <span className={isCompany ? 'font-medium text-ink' : 'font-mono text-[13px] font-medium text-ink'}>
              {result.name}
            </span>
            <StatusBadge status={result.status} />
          </span>
          <span className="mt-0.5 block truncate text-[13px] text-muted">
            {isCompany ? 'Company' : `Facility · ${result.companyName}`}
            {!isCompany && result.repaymentType ? ` · ${repaymentTypeLabel(result.repaymentType)}` : ''}
          </span>
        </span>
        {!isCompany && result.currency && (
          <span className="hidden shrink-0 flex-col items-end gap-0.5 sm:flex">
            {result.outstanding !== null && (
              <MoneyValue amount={result.outstanding} currency={result.currency} />
            )}
            {result.amount !== null && (
              <span className="text-xs">
                <MoneyValue amount={result.amount} currency={result.currency} muted />
                <span className="ml-1 text-faint">committed</span>
              </span>
            )}
          </span>
        )}
      </button>
    </li>
  )
}

export default function Search() {
  const navigate = useNavigate()
  const [params, setParams] = useSearchParams()
  const urlQuery = params.get('q') ?? ''
  const [input, setInput] = useState(urlQuery)
  const [lastUrlQuery, setLastUrlQuery] = useState(urlQuery)

  if (urlQuery !== lastUrlQuery) {
    setLastUrlQuery(urlQuery)
    setInput(urlQuery)
  }

  useEffect(() => {
    const handle = setTimeout(() => {
      const trimmed = input.trim()
      if (trimmed === urlQuery) return
      setParams(
        (prev) => {
          const next = new URLSearchParams(prev)
          if (trimmed) next.set('q', trimmed)
          else next.delete('q')
          next.delete('page')
          return next
        },
        { replace: true },
      )
    }, 300)
    return () => clearTimeout(handle)
  }, [input, urlQuery, setParams])

  const filters: SearchFilterValues = useMemo(
    () =>
      Object.fromEntries(
        filterKeys.map((key) => [key, params.get(key) ?? '']),
      ) as SearchFilterValues,
    [params],
  )
  const page = Math.max(1, Number(params.get('page')) || 1)

  const setFilter = (key: keyof SearchFilterValues, value: string) => {
    setParams(
      (prev) => {
        const next = new URLSearchParams(prev)
        if (value) next.set(key, value)
        else next.delete(key)
        next.delete('page')
        return next
      },
      { replace: true },
    )
  }

  const clearFilters = () => {
    setParams(
      (prev) => {
        const next = new URLSearchParams(prev)
        for (const key of filterKeys) next.delete(key)
        next.delete('page')
        return next
      },
      { replace: true },
    )
  }

  const setPage = (nextPage: number) => {
    setParams(
      (prev) => {
        const next = new URLSearchParams(prev)
        if (nextPage > 1) next.set('page', String(nextPage))
        else next.delete('page')
        return next
      },
      { replace: true },
    )
  }

  const hasCriteria = urlQuery !== '' || filterKeys.some((key) => filters[key] !== '')
  const [minAmount, maxAmount] = normalizeAmountRange(
    parseAmount(filters.minAmount),
    parseAmount(filters.maxAmount),
  )

  const { data, isLoading, isFetching, isError, error, refetch } = useSearch(
    {
      q: urlQuery || undefined,
      type: filters.type || undefined,
      status: filters.status || undefined,
      repaymentType: filters.repaymentType || undefined,
      currency: filters.currency || undefined,
      minAmount,
      maxAmount,
      page,
      pageSize: 20,
    },
    hasCriteria,
  )

  const pageCount = data ? Math.max(1, Math.ceil(data.total / data.pageSize)) : 1

  return (
    <>
      <PageHeader
        eyebrow="Find"
        title="Search"
        description="One index across companies and facilities."
      />
      <Card className="mb-4">
        <div className="border-b border-line px-5 py-3">
          <div className="relative">
            <SearchIcon className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-faint" />
            <Input
              value={input}
              onChange={(e) => setInput(e.target.value)}
              placeholder="Company name, industry, or facility reference like FAC-00012"
              className="h-10 pl-9"
              aria-label="Search the portfolio"
              autoFocus
            />
          </div>
        </div>
        <div className="px-5 py-3.5">
          <SearchFilters values={filters} onChange={setFilter} onClear={clearFilters} />
        </div>
      </Card>
      {!hasCriteria ? (
        <Card>
          <div className="flex flex-col items-center gap-2 px-6 py-16 text-center">
            <SearchIcon className="size-7 text-faint" />
            <p className="text-sm font-medium text-ink">Search the whole book</p>
            <p className="max-w-md text-sm text-muted">
              Type a borrower name, an industry, or a facility reference — or pick a filter to
              browse a slice of the portfolio.
            </p>
          </div>
        </Card>
      ) : isLoading ? (
        <Card>
          <SkeletonRows rows={6} />
        </Card>
      ) : isError ? (
        <Card>
          <div className="flex flex-col items-center gap-2 px-6 py-16 text-center">
            <TriangleAlert className="size-7 text-danger" />
            <p className="text-sm font-medium text-ink">The search could not be run</p>
            <p className="max-w-md text-sm text-muted">
              {error instanceof ApiError
                ? error.detail || error.title
                : 'Something went wrong. Try again.'}
            </p>
            <Button variant="secondary" size="sm" className="mt-1" onClick={() => void refetch()}>
              Try again
            </Button>
          </div>
        </Card>
      ) : !data || data.items.length === 0 ? (
        <Card>
          <div className="flex flex-col items-center gap-2 px-6 py-16 text-center">
            <SearchX className="size-7 text-faint" />
            <p className="text-sm font-medium text-ink">
              {urlQuery ? `No matches for “${urlQuery}”` : 'No matches'}
            </p>
            <p className="max-w-md text-sm text-muted">
              Check the spelling, or widen the filters to search more of the book.
            </p>
            <Button variant="secondary" size="sm" className="mt-1" onClick={clearFilters}>
              Clear filters
            </Button>
          </div>
        </Card>
      ) : (
        <Card>
          <div className="flex items-center justify-between border-b border-line px-5 py-2.5">
            <p className="tabular text-[13px] text-muted">
              {data.total} {data.total === 1 ? 'result' : 'results'}
              {urlQuery && (
                <>
                  {' '}for <span className="font-medium text-ink">“{urlQuery}”</span>
                </>
              )}
            </p>
            {isFetching && <p className="text-[13px] text-faint">Updating…</p>}
          </div>
          <ul className="divide-y divide-line">
            {data.items.map((result) => (
              <ResultRow
                key={`${result.type}-${result.id}`}
                result={result}
                onOpen={() => navigate(searchResultPath(result))}
              />
            ))}
          </ul>
          {data.total > data.pageSize && (
            <div className="flex items-center justify-between border-t border-line px-5 py-2.5">
              <p className="tabular text-[13px] text-muted">
                Page {data.page} of {pageCount}
              </p>
              <div className="flex items-center gap-1">
                <Button
                  variant="ghost"
                  size="sm"
                  disabled={page <= 1}
                  onClick={() => setPage(page - 1)}
                  aria-label="Previous page"
                >
                  <ChevronLeft className="size-4" />
                </Button>
                <Button
                  variant="ghost"
                  size="sm"
                  disabled={page >= pageCount}
                  onClick={() => setPage(page + 1)}
                  aria-label="Next page"
                >
                  <ChevronRight className="size-4" />
                </Button>
              </div>
            </div>
          )}
        </Card>
      )}
    </>
  )
}
