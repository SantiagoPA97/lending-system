import { AlertTriangle, Landmark, RefreshCw } from 'lucide-react'
import { Link } from 'react-router-dom'
import { PageHeader } from '@/components/domain/page-header'
import {
  FacilitiesTile,
  OverdueTile,
  PortfolioTile,
  RepaymentsTile,
} from '@/components/dashboard/kpi-tiles'
import {
  ExposureChart,
  FacilitiesByStatusChart,
  UpcomingInstallmentsChart,
} from '@/components/dashboard/charts'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { ApiError } from '@/lib/api'
import { formatDateTime } from '@/lib/format'
import { useDashboardMetrics } from '@/lib/queries'
import { cn } from '@/lib/utils'
import type { DashboardMetricsResponse } from '@/types/api'
import { FACILITY_STATUSES } from '@/types/api'

export default function Dashboard() {
  const { data, isPending, isError, error, refetch, isFetching } = useDashboardMetrics()

  return (
    <>
      <PageHeader
        eyebrow="Portfolio"
        title="Dashboard"
        description="Committed and outstanding exposure across the book."
        actions={
          data && (
            <div className="flex items-center gap-1 text-xs text-muted">
              <span>As of {formatDateTime(data.generatedAtUtc)}</span>
              <Button
                variant="ghost"
                size="sm"
                aria-label="Refresh metrics"
                onClick={() => refetch()}
              >
                <RefreshCw className={cn('size-4', isFetching && 'animate-spin')} />
              </Button>
            </div>
          )
        }
      />
      {isPending ? (
        <DashboardSkeleton />
      ) : isError ? (
        <Card>
          <CardContent className="flex flex-col items-center gap-3 py-12 text-center">
            <AlertTriangle className="size-6 text-danger" />
            <p className="font-display text-lg font-semibold text-ink">
              Couldn't load portfolio metrics
            </p>
            <p className="max-w-sm text-sm text-muted">
              {error instanceof ApiError
                ? (error.detail ?? error.title)
                : 'Something went wrong talking to the API.'}
            </p>
            <Button variant="secondary" onClick={() => refetch()}>
              Try again
            </Button>
          </CardContent>
        </Card>
      ) : data ? (
        <DashboardContent metrics={data} />
      ) : null}
    </>
  )
}

function DashboardContent({ metrics }: { metrics: DashboardMetricsResponse }) {
  const totalFacilities = FACILITY_STATUSES.reduce(
    (sum, status) => sum + (metrics.facilitiesByStatus[status] ?? 0),
    0,
  )

  if (totalFacilities === 0 && metrics.portfolio.length === 0) {
    return (
      <Card>
        <CardContent className="flex flex-col items-center gap-3 py-14 text-center">
          <span className="flex size-11 items-center justify-center rounded-md bg-accent-soft">
            <Landmark className="size-5 text-accent" />
          </span>
          <p className="font-display text-lg font-semibold text-ink">The book is empty</p>
          <p className="max-w-sm text-sm text-muted">
            Create a borrower, then add its first credit facility. Portfolio exposure,
            upcoming installments and repayments will appear here.
          </p>
          <div className="mt-1 flex items-center gap-2">
            <Link to="/companies">
              <Button>Go to companies</Button>
            </Link>
            <Link to="/facilities">
              <Button variant="secondary">View facilities</Button>
            </Link>
          </div>
        </CardContent>
      </Card>
    )
  }

  return (
    <div className="space-y-4">
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        {metrics.portfolio.map((entry) => (
          <PortfolioTile key={entry.currency} entry={entry} />
        ))}
        <FacilitiesTile counts={metrics.facilitiesByStatus} />
        <RepaymentsTile rows={metrics.repaymentsLast30Days} />
        <OverdueTile data={metrics.overdueInstallments} />
      </div>

      <div className="grid gap-4 lg:grid-cols-2">
        <Card>
          <CardHeader>
            <CardTitle>Facilities by status</CardTitle>
            <span className="tabular text-xs text-muted">{totalFacilities} total</span>
          </CardHeader>
          <CardContent className="pt-3">
            <FacilitiesByStatusChart counts={metrics.facilitiesByStatus} />
          </CardContent>
        </Card>
        <Card>
          <CardHeader>
            <CardTitle>Due in the next 30 days</CardTitle>
            <span className="text-xs text-muted">Active facilities</span>
          </CardHeader>
          <CardContent className="pt-3">
            {metrics.upcomingInstallmentsNext30Days.length === 0 ? (
              <p className="py-10 text-center text-sm text-muted">
                Nothing falls due in the next 30 days.
              </p>
            ) : (
              <UpcomingInstallmentsChart rows={metrics.upcomingInstallmentsNext30Days} />
            )}
          </CardContent>
        </Card>
      </div>

      {metrics.topCompanyExposures.length > 0 && (
        <div className="grid gap-4 lg:grid-cols-2">
          {metrics.topCompanyExposures.map((group) => (
            <Card key={group.currency}>
              <CardHeader>
                <CardTitle>Top company exposure</CardTitle>
                <Badge tone="accent" className="font-mono">
                  {group.currency}
                </Badge>
              </CardHeader>
              <CardContent className="pt-3">
                <ExposureChart group={group} />
              </CardContent>
            </Card>
          ))}
        </div>
      )}
    </div>
  )
}

function DashboardSkeleton() {
  return (
    <div className="space-y-4">
      <div className="grid gap-4 sm:grid-cols-2 xl:grid-cols-4">
        {Array.from({ length: 4 }).map((_, i) => (
          <Card key={i}>
            <CardContent>
              <Skeleton className="h-3 w-24" />
              <Skeleton className="mt-4 h-6 w-32" />
              <Skeleton className="mt-4 h-3 w-full" />
            </CardContent>
          </Card>
        ))}
      </div>
      <div className="grid gap-4 lg:grid-cols-2">
        {Array.from({ length: 2 }).map((_, i) => (
          <Card key={i}>
            <CardContent className="space-y-3 py-5">
              <Skeleton className="h-4 w-40" />
              <Skeleton className="h-9 w-full" />
              <Skeleton className="h-9 w-4/5" />
              <Skeleton className="h-9 w-3/5" />
            </CardContent>
          </Card>
        ))}
      </div>
    </div>
  )
}
