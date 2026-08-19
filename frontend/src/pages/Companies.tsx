import { useEffect, useState } from 'react'
import { useNavigate } from 'react-router-dom'
import { Plus, Search } from 'lucide-react'
import { PageHeader } from '@/components/domain/page-header'
import { DataTable, type Column } from '@/components/domain/data-table'
import { StatusBadge } from '@/components/domain/status-badge'
import { CompanyCreateDialog } from '@/components/companies/company-create-dialog'
import { Card } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { Input } from '@/components/ui/input'
import { Select } from '@/components/ui/select'
import { usePermissions } from '@/lib/auth'
import { formatDate } from '@/lib/format'
import { useCompanies } from '@/lib/queries'
import { COMPANY_STATUSES, type CompanyResponse } from '@/types/api'

const columns: Column<CompanyResponse>[] = [
  {
    key: 'name',
    header: 'Company',
    cell: (c) => (
      <div>
        <p className="font-medium text-ink">{c.name}</p>
        <p className="text-xs text-muted">{c.legalName}</p>
      </div>
    ),
  },
  {
    key: 'country',
    header: 'Country',
    cell: (c) => <span className="font-mono text-[13px]">{c.country}</span>,
  },
  { key: 'industry', header: 'Industry', cell: (c) => c.industry },
  {
    key: 'contactEmail',
    header: 'Contact',
    cell: (c) => <span className="text-muted">{c.contactEmail}</span>,
    cellClassName: 'max-w-56 truncate',
  },
  { key: 'status', header: 'Status', cell: (c) => <StatusBadge status={c.status} /> },
  {
    key: 'created',
    header: 'Since',
    cell: (c) => formatDate(c.createdAtUtc),
    cellClassName: 'whitespace-nowrap text-muted',
  },
]

export default function Companies() {
  const navigate = useNavigate()
  const [page, setPage] = useState(1)
  const [status, setStatus] = useState('')
  const [nameInput, setNameInput] = useState('')
  const [nameQuery, setNameQuery] = useState('')
  const [createOpen, setCreateOpen] = useState(false)
  const { canOperate } = usePermissions()

  useEffect(() => {
    const handle = setTimeout(() => {
      setNameQuery(nameInput.trim())
      setPage(1)
    }, 300)
    return () => clearTimeout(handle)
  }, [nameInput])

  const { data, isLoading } = useCompanies({
    page,
    pageSize: 20,
    status: status || undefined,
    q: nameQuery || undefined,
  })

  const filtered = status !== '' || nameQuery !== ''

  return (
    <>
      <PageHeader
        eyebrow="Borrowers"
        title="Companies"
        description="Every borrower on the book, with their standing and facilities."
        actions={
          canOperate && (
            <Button onClick={() => setCreateOpen(true)}>
              <Plus className="size-4" />
              New company
            </Button>
          )
        }
      />
      <Card>
        <div className="flex flex-wrap items-center gap-2 border-b border-line px-5 py-3">
          <div className="relative w-full max-w-xs">
            <Search className="pointer-events-none absolute left-2.5 top-1/2 size-4 -translate-y-1/2 text-faint" />
            <Input
              value={nameInput}
              onChange={(e) => setNameInput(e.target.value)}
              placeholder="Filter by name"
              className="pl-8"
              aria-label="Filter companies by name"
            />
          </div>
          <div className="w-40">
            <Select
              value={status}
              onChange={(e) => {
                setStatus(e.target.value)
                setPage(1)
              }}
              aria-label="Filter by status"
            >
              <option value="">All statuses</option>
              {COMPANY_STATUSES.map((s) => (
                <option key={s} value={s}>
                  {s}
                </option>
              ))}
            </Select>
          </div>
          {data && (
            <p className="tabular ml-auto text-[13px] text-muted">
              {data.total} {data.total === 1 ? 'company' : 'companies'}
            </p>
          )}
        </div>
        <DataTable
          columns={columns}
          data={data}
          isLoading={isLoading}
          rowKey={(c) => c.id}
          onRowClick={(c) => navigate(`/companies/${c.id}`)}
          onPageChange={setPage}
          emptyTitle={filtered ? 'No companies match' : 'No companies yet'}
          emptyDescription={
            filtered
              ? 'Loosen the filters or check the spelling.'
              : 'Register the first borrower to start building the book.'
          }
          emptyAction={
            filtered || !canOperate ? undefined : (
              <Button variant="secondary" size="sm" onClick={() => setCreateOpen(true)}>
                <Plus className="size-4" />
                New company
              </Button>
            )
          }
        />
      </Card>
      <CompanyCreateDialog open={createOpen} onClose={() => setCreateOpen(false)} />
    </>
  )
}
