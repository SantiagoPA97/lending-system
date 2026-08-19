import { useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { ArrowLeft, CircleSlash, RotateCcw } from 'lucide-react'
import { toast } from 'sonner'
import { PageHeader } from '@/components/domain/page-header'
import { StatusBadge } from '@/components/domain/status-badge'
import { ConfirmDialog } from '@/components/domain/confirm-dialog'
import { CompanyDetailsCard } from '@/components/companies/company-details-card'
import { CompanyFacilitiesCard } from '@/components/companies/company-facilities-card'
import { AuditTrailCard } from '@/components/companies/audit-trail-card'
import { apiErrorMessage } from '@/components/companies/company-form'
import { Button } from '@/components/ui/button'
import { Card, CardContent } from '@/components/ui/card'
import { Skeleton } from '@/components/ui/skeleton'
import { ApiError } from '@/lib/api'
import { usePermissions } from '@/lib/auth'
import { useActivateCompany, useCompany, useDeactivateCompany } from '@/lib/queries'

export default function CompanyDetail() {
  const { id } = useParams<{ id: string }>()
  const { data: company, isLoading, isError, error, refetch } = useCompany(id ?? '')
  const deactivate = useDeactivateCompany()
  const activate = useActivateCompany()
  const [confirming, setConfirming] = useState<'deactivate' | 'activate' | null>(null)
  const { canOperate, canAdmin } = usePermissions()

  if (isLoading) {
    return (
      <div className="grid gap-4">
        <Skeleton className="h-16 w-2/3" />
        <Skeleton className="h-64" />
        <Skeleton className="h-40" />
      </div>
    )
  }

  if (isError || !company) {
    const notFound = error instanceof ApiError && error.status === 404
    return (
      <Card>
        <CardContent className="flex flex-col items-center gap-3 py-14 text-center">
          <p className="text-sm font-medium text-ink">
            {notFound ? 'Company not found' : 'The company could not be loaded'}
          </p>
          <p className="text-sm text-muted">
            {notFound
              ? 'It may have been removed, or the link is out of date.'
              : apiErrorMessage(error)}
          </p>
          {!notFound && (
            <Button variant="secondary" size="sm" onClick={() => void refetch()}>
              Try again
            </Button>
          )}
          <Link to="/companies" className="text-sm font-medium text-accent hover:text-accent-hover">
            Back to companies
          </Link>
        </CardContent>
      </Card>
    )
  }

  const active = company.status === 'Active'
  const mutation = confirming === 'deactivate' ? deactivate : activate

  const confirm = () => {
    if (!confirming) return
    mutation.mutate(company.id, {
      onSuccess: (updated) => {
        toast.success(
          updated.status === 'Inactive'
            ? `${updated.name} deactivated`
            : `${updated.name} reactivated`,
        )
        setConfirming(null)
      },
      onError: (err) => toast.error(apiErrorMessage(err)),
    })
  }

  return (
    <>
      <Link
        to="/companies"
        className="mb-3 inline-flex items-center gap-1.5 text-[13px] font-medium text-muted transition-colors hover:text-ink"
      >
        <ArrowLeft className="size-3.5" />
        Companies
      </Link>
      <PageHeader
        eyebrow="Borrower"
        title={
          <span className="inline-flex flex-wrap items-center gap-3">
            {company.name}
            <StatusBadge status={company.status} />
          </span>
        }
        description={
          <>
            {company.legalName} · Reg.{' '}
            <span className="font-mono text-[13px]">{company.registrationNumber}</span> ·{' '}
            <span className="font-mono text-[13px]">{company.country}</span> · {company.industry}
          </>
        }
        actions={
          active
            ? canAdmin && (
                <Button variant="secondary" onClick={() => setConfirming('deactivate')}>
                  <CircleSlash className="size-4" />
                  Deactivate
                </Button>
              )
            : canOperate && (
                <Button onClick={() => setConfirming('activate')}>
                  <RotateCcw className="size-4" />
                  Reactivate
                </Button>
              )
        }
      />
      <div className="grid items-start gap-4 lg:grid-cols-3">
        <div className="grid gap-4 lg:col-span-2">
          <CompanyFacilitiesCard company={company} />
          <AuditTrailCard entityType="Company" entityId={company.id} />
        </div>
        <CompanyDetailsCard company={company} />
      </div>
      <ConfirmDialog
        open={confirming !== null}
        onClose={() => setConfirming(null)}
        onConfirm={confirm}
        loading={mutation.isPending}
        title={confirming === 'deactivate' ? `Deactivate ${company.name}?` : `Reactivate ${company.name}?`}
        description={
          confirming === 'deactivate'
            ? 'Existing facilities keep operating and repayments can still be recorded, but the company can’t receive new facilities until it is reactivated.'
            : 'The company will be able to receive new facilities again.'
        }
        confirmLabel={confirming === 'deactivate' ? 'Deactivate' : 'Reactivate'}
      />
    </>
  )
}
