import { useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Plus, X } from 'lucide-react'
import { DataTable, type Column } from '@/components/domain/data-table'
import { MoneyValue } from '@/components/domain/money-value'
import { PageHeader } from '@/components/domain/page-header'
import { StatusBadge } from '@/components/domain/status-badge'
import { FacilityFormDialog } from '@/components/facilities/facility-form-dialog'
import { Button } from '@/components/ui/button'
import { Card } from '@/components/ui/card'
import { Select } from '@/components/ui/select'
import { usePermissions } from '@/lib/auth'
import { formatDate } from '@/lib/format'
import { useCompanies, useFacilities } from '@/lib/queries'
import { CURRENCIES, FACILITY_STATUSES, REPAYMENT_TYPES, type FacilityResponse } from '@/types/api'

const typeLabels: Record<string, string> = {
  Bullet: 'Bullet',
  Amortizing: 'Amortizing',
  InterestOnly: 'Interest only',
}

export default function Facilities() {
  const navigate = useNavigate()
  const [page, setPage] = useState(1)
  const [status, setStatus] = useState('')
  const [repaymentType, setRepaymentType] = useState('')
  const [currency, setCurrency] = useState('')
  const [companyId, setCompanyId] = useState('')
  const [createOpen, setCreateOpen] = useState(false)
  const { canOperate } = usePermissions()

  const facilities = useFacilities({
    page,
    pageSize: 20,
    status: status || undefined,
    repaymentType: repaymentType || undefined,
    currency: currency || undefined,
    companyId: companyId || undefined,
  })
  const companies = useCompanies({ pageSize: 100 })

  const hasFilters = !!(status || repaymentType || currency || companyId)
  const clearFilters = () => {
    setStatus('')
    setRepaymentType('')
    setCurrency('')
    setCompanyId('')
    setPage(1)
  }
  const setFilter = (setter: (value: string) => void) => (value: string) => {
    setter(value)
    setPage(1)
  }

  const columns: Column<FacilityResponse>[] = [
    {
      key: 'reference',
      header: 'Reference',
      cell: (f) => <span className="font-mono text-[13px] font-medium text-ink">{f.reference}</span>,
    },
    { key: 'company', header: 'Company', cell: (f) => f.companyName },
    {
      key: 'type',
      header: 'Type',
      cell: (f) => <span className="text-muted">{typeLabels[f.repaymentType]}</span>,
    },
    {
      key: 'amount',
      header: 'Commitment',
      align: 'right',
      cell: (f) => <MoneyValue amount={f.commitmentAmount} currency={f.currency} />,
    },
    {
      key: 'outstanding',
      header: 'Outstanding',
      align: 'right',
      cell: (f) =>
        f.status === 'Draft' ? (
          <span className="text-faint">—</span>
        ) : (
          <MoneyValue amount={f.outstandingPrincipal} currency={f.currency} />
        ),
    },
    { key: 'status', header: 'Status', cell: (f) => <StatusBadge status={f.status} /> },
    {
      key: 'startDate',
      header: 'Start date',
      cell: (f) => <span className="whitespace-nowrap text-muted">{formatDate(f.startDate)}</span>,
    },
  ]

  return (
    <>
      <PageHeader
        eyebrow="Credit"
        title="Facilities"
        description="Commitments, terms and outstanding balances across all borrowers."
        actions={
          canOperate && (
            <Button onClick={() => setCreateOpen(true)}>
              <Plus className="size-4" />
              New facility
            </Button>
          )
        }
      />
      <div className="mb-4 flex flex-wrap items-center gap-2">
        <Select
          aria-label="Filter by status"
          className="w-40"
          value={status}
          onChange={(e) => setFilter(setStatus)(e.target.value)}
        >
          <option value="">All statuses</option>
          {FACILITY_STATUSES.map((s) => (
            <option key={s} value={s}>
              {s}
            </option>
          ))}
        </Select>
        <Select
          aria-label="Filter by repayment type"
          className="w-40"
          value={repaymentType}
          onChange={(e) => setFilter(setRepaymentType)(e.target.value)}
        >
          <option value="">All types</option>
          {REPAYMENT_TYPES.map((t) => (
            <option key={t} value={t}>
              {typeLabels[t]}
            </option>
          ))}
        </Select>
        <Select
          aria-label="Filter by currency"
          className="w-32"
          value={currency}
          onChange={(e) => setFilter(setCurrency)(e.target.value)}
        >
          <option value="">All currencies</option>
          {CURRENCIES.map((c) => (
            <option key={c} value={c}>
              {c}
            </option>
          ))}
        </Select>
        <Select
          aria-label="Filter by company"
          className="w-52"
          value={companyId}
          onChange={(e) => setFilter(setCompanyId)(e.target.value)}
        >
          <option value="">All companies</option>
          {companies.data?.items.map((company) => (
            <option key={company.id} value={company.id}>
              {company.name}
            </option>
          ))}
        </Select>
        {hasFilters && (
          <Button variant="ghost" size="sm" onClick={clearFilters}>
            <X className="size-4" />
            Clear filters
          </Button>
        )}
      </div>
      <Card>
        <DataTable
          columns={columns}
          data={facilities.data}
          isLoading={facilities.isLoading}
          rowKey={(f) => f.id}
          onRowClick={(f) => navigate(`/facilities/${f.id}`)}
          onPageChange={setPage}
          emptyTitle={hasFilters ? 'No facilities match these filters' : 'No facilities yet'}
          emptyDescription={
            hasFilters
              ? 'Adjust or clear the filters to see more of the book.'
              : 'Create the first facility to start building the credit book.'
          }
          emptyAction={
            hasFilters ? (
              <Button variant="secondary" size="sm" onClick={clearFilters}>
                Clear filters
              </Button>
            ) : canOperate ? (
              <Button size="sm" onClick={() => setCreateOpen(true)}>
                <Plus className="size-4" />
                New facility
              </Button>
            ) : undefined
          }
        />
      </Card>
      {createOpen && <FacilityFormDialog open onClose={() => setCreateOpen(false)} />}
    </>
  )
}
