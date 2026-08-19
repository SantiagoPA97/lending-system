import { useState, type ReactNode } from 'react'
import { Link, useParams } from 'react-router-dom'
import { ArrowLeft, Ban, CalendarClock, Pencil, Plus, TriangleAlert } from 'lucide-react'
import { toast } from 'sonner'
import { ConfirmDialog } from '@/components/domain/confirm-dialog'
import { MoneyValue } from '@/components/domain/money-value'
import { PageHeader } from '@/components/domain/page-header'
import { StatusBadge } from '@/components/domain/status-badge'
import { AuditTab } from '@/components/facilities/audit-tab'
import { FacilityFormDialog } from '@/components/facilities/facility-form-dialog'
import { RecordRepaymentDialog } from '@/components/facilities/record-repayment-dialog'
import { RepaymentsTab } from '@/components/facilities/repayments-tab'
import { ScheduleTab } from '@/components/facilities/schedule-tab'
import { facilityErrorMessage } from '@/components/facilities/facility-errors'
import { Badge } from '@/components/ui/badge'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { SkeletonRows } from '@/components/ui/skeleton'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { ApiError } from '@/lib/api'
import { formatDate, percent, toDateInputValue } from '@/lib/format'
import {
  useActivateFacility,
  useCancelFacility,
  useDefaultFacility,
  useFacility,
} from '@/lib/queries'
import type { FacilityDetailResponse } from '@/types/api'

type PendingAction = 'activate' | 'cancel' | 'default' | null

const typeLabels: Record<string, string> = {
  Bullet: 'Bullet',
  Amortizing: 'Amortizing',
  InterestOnly: 'Interest only',
}

export default function FacilityDetail() {
  const { id = '' } = useParams<{ id: string }>()
  const facility = useFacility(id)

  if (facility.isLoading) {
    return (
      <Card>
        <SkeletonRows rows={7} />
      </Card>
    )
  }

  if (facility.isError || !facility.data) {
    const notFound = facility.error instanceof ApiError && facility.error.status === 404
    return (
      <Card>
        <CardContent className="flex flex-col items-center gap-3 py-14 text-center">
          <p className="text-sm font-medium text-ink">
            {notFound ? 'Facility not found' : 'The facility could not be loaded'}
          </p>
          <p className="text-sm text-muted">
            {notFound
              ? 'It may have been removed, or the link is out of date.'
              : facilityErrorMessage(facility.error)}
          </p>
          <Button variant="secondary" size="sm" onClick={() => void facility.refetch()}>
            Try again
          </Button>
        </CardContent>
      </Card>
    )
  }

  return <FacilityView facility={facility.data} />
}

function FacilityView({ facility }: { facility: FacilityDetailResponse }) {
  const [editOpen, setEditOpen] = useState(false)
  const [repayOpen, setRepayOpen] = useState(false)
  const [pendingAction, setPendingAction] = useState<PendingAction>(null)

  const activate = useActivateFacility()
  const cancel = useCancelFacility()
  const markDefaulted = useDefaultFacility()

  const runAction = async (action: Exclude<PendingAction, null>) => {
    try {
      if (action === 'activate') {
        await activate.mutateAsync(facility.id)
        toast.success(`${facility.reference} activated — repayment schedule generated`)
      } else if (action === 'cancel') {
        await cancel.mutateAsync(facility.id)
        toast.success(`${facility.reference} cancelled`)
      } else {
        await markDefaulted.mutateAsync(facility.id)
        toast.success(`${facility.reference} marked as defaulted`)
      }
    } catch (err) {
      toast.error(facilityErrorMessage(err))
    } finally {
      setPendingAction(null)
    }
  }

  const actionLoading = activate.isPending || cancel.isPending || markDefaulted.isPending
  const repaidPct =
    facility.commitmentAmount > 0
      ? (facility.totalPrincipalPaid / facility.commitmentAmount) * 100
      : 0
  const today = toDateInputValue(new Date())
  const nextDue = facility.nextDueInstallment
  const nextDueOverdue = !!nextDue && nextDue.dueDate < today

  return (
    <>
      <Link
        to="/facilities"
        className="mb-3 inline-flex items-center gap-1.5 text-[13px] text-muted transition-colors hover:text-ink"
      >
        <ArrowLeft className="size-3.5" />
        All facilities
      </Link>
      <PageHeader
        eyebrow="Credit"
        title={
          <span className="flex flex-wrap items-center gap-3">
            <span className="font-mono">{facility.reference}</span>
            <StatusBadge status={facility.status} />
          </span>
        }
        description={
          <span className="flex flex-wrap items-center gap-x-2 gap-y-1">
            <Link
              to={`/companies/${facility.company.id}`}
              className="font-medium text-accent hover:underline"
            >
              {facility.company.name}
            </Link>
            {facility.company.status === 'Inactive' && <StatusBadge status="Inactive" />}
            <span className="text-faint">·</span>
            <span>{typeLabels[facility.repaymentType]}</span>
            <span className="text-faint">·</span>
            <span className="tabular">{percent(facility.annualInterestRate)}</span>
            <span className="text-faint">·</span>
            <span className="tabular">{facility.termMonths} months</span>
            <span className="text-faint">·</span>
            <span>starts {formatDate(facility.startDate)}</span>
          </span>
        }
        actions={<FacilityActions facility={facility} onEdit={() => setEditOpen(true)} onRepay={() => setRepayOpen(true)} onAction={setPendingAction} />}
      />

      <Card className="mb-6">
        <CardContent className="grid gap-x-6 gap-y-5 sm:grid-cols-2 lg:grid-cols-4">
          <Figure label="Commitment">
            <MoneyValue
              amount={facility.commitmentAmount}
              currency={facility.currency}
              className="text-lg font-semibold"
            />
          </Figure>
          <Figure label="Outstanding">
            {facility.status === 'Draft' ? (
              <span className="text-lg text-faint">Not yet drawn</span>
            ) : (
              <MoneyValue
                amount={facility.outstandingPrincipal}
                currency={facility.currency}
                className="text-lg font-semibold"
              />
            )}
          </Figure>
          <Figure label="Principal repaid">
            <MoneyValue
              amount={facility.totalPrincipalPaid}
              currency={facility.currency}
              className="text-lg font-semibold"
            />
            <div className="mt-2 h-1.5 w-full overflow-hidden rounded-full bg-line">
              <div
                className="h-full rounded-full bg-accent transition-all"
                style={{ width: `${Math.min(100, Math.max(0, repaidPct))}%` }}
              />
            </div>
            <p className="tabular mt-1 text-xs text-muted">
              {repaidPct.toFixed(1)}% of commitment
            </p>
          </Figure>
          <Figure label="Interest paid">
            <MoneyValue
              amount={facility.totalInterestPaid}
              currency={facility.currency}
              className="text-lg font-semibold"
            />
          </Figure>
        </CardContent>
        {nextDue && (
          <div className="flex flex-wrap items-center gap-2 border-t border-line px-5 py-3 text-sm">
            <CalendarClock className="size-4 text-muted" />
            <span className="text-muted">Next due</span>
            <span className="tabular text-body">
              #{nextDue.period} · {formatDate(nextDue.dueDate)}
            </span>
            <MoneyValue amount={nextDue.totalDue} currency={facility.currency} className="font-medium" />
            {nextDueOverdue && <Badge tone="danger">Overdue</Badge>}
          </div>
        )}
      </Card>

      <Tabs defaultValue="schedule">
        <TabsList>
          <TabsTrigger value="schedule">Schedule</TabsTrigger>
          <TabsTrigger value="repayments">Repayments</TabsTrigger>
          <TabsTrigger value="audit">Audit</TabsTrigger>
        </TabsList>
        <TabsContent value="schedule">
          <ScheduleTab facility={facility} />
        </TabsContent>
        <TabsContent value="repayments">
          <RepaymentsTab facility={facility} />
        </TabsContent>
        <TabsContent value="audit">
          <AuditTab facilityId={facility.id} />
        </TabsContent>
      </Tabs>

      {editOpen && (
        <FacilityFormDialog open onClose={() => setEditOpen(false)} facility={facility} />
      )}
      {repayOpen && (
        <RecordRepaymentDialog open onClose={() => setRepayOpen(false)} facility={facility} />
      )}
      <ConfirmDialog
        open={pendingAction === 'activate'}
        onClose={() => setPendingAction(null)}
        onConfirm={() => void runAction('activate')}
        title={`Activate ${facility.reference}`}
        description="Activation locks the terms, generates the repayment schedule and makes the full commitment outstanding."
        confirmLabel="Activate facility"
        loading={actionLoading}
      />
      <ConfirmDialog
        open={pendingAction === 'cancel'}
        onClose={() => setPendingAction(null)}
        onConfirm={() => void runAction('cancel')}
        title={`Cancel ${facility.reference}`}
        description="Cancelling closes this facility permanently. No further activity can be recorded."
        confirmLabel="Cancel facility"
        destructive
        loading={actionLoading}
      />
      <ConfirmDialog
        open={pendingAction === 'default'}
        onClose={() => setPendingAction(null)}
        onConfirm={() => void runAction('default')}
        title={`Mark ${facility.reference} as defaulted`}
        description="Marks the facility as defaulted. No further repayments can be recorded against it."
        confirmLabel="Mark defaulted"
        destructive
        loading={actionLoading}
      />
    </>
  )
}

function FacilityActions({
  facility,
  onEdit,
  onRepay,
  onAction,
}: {
  facility: FacilityDetailResponse
  onEdit: () => void
  onRepay: () => void
  onAction: (action: PendingAction) => void
}) {
  if (facility.status === 'Draft') {
    return (
      <>
        <Button variant="ghost" onClick={() => onAction('cancel')} className="text-danger hover:text-danger">
          <Ban className="size-4" />
          Cancel
        </Button>
        <Button variant="secondary" onClick={onEdit}>
          <Pencil className="size-4" />
          Edit terms
        </Button>
        <Button onClick={() => onAction('activate')}>Activate</Button>
      </>
    )
  }
  if (facility.status === 'Active') {
    return (
      <>
        <Button variant="ghost" onClick={() => onAction('default')} className="text-danger hover:text-danger">
          <TriangleAlert className="size-4" />
          Mark defaulted
        </Button>
        <Button variant="ghost" onClick={() => onAction('cancel')} className="text-danger hover:text-danger">
          <Ban className="size-4" />
          Cancel
        </Button>
        <Button onClick={onRepay}>
          <Plus className="size-4" />
          Record repayment
        </Button>
      </>
    )
  }
  return null
}

function Figure({ label, children }: { label: string; children: ReactNode }) {
  return (
    <div>
      <p className="mb-1 text-xs font-semibold uppercase tracking-wider text-muted">{label}</p>
      {children}
    </div>
  )
}
