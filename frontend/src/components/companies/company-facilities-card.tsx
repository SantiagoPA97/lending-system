import { Link, useNavigate } from 'react-router-dom'
import { Landmark, Plus } from 'lucide-react'
import { Card, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table'
import { StatusBadge } from '@/components/domain/status-badge'
import { MoneyValue } from '@/components/domain/money-value'
import { formatDate, percent } from '@/lib/format'
import type { CompanyDetailResponse } from '@/types/api'

const repaymentTypeLabels: Record<string, string> = {
  Bullet: 'Bullet',
  Amortizing: 'Amortizing',
  InterestOnly: 'Interest-only',
}

export function CompanyFacilitiesCard({ company }: { company: CompanyDetailResponse }) {
  const navigate = useNavigate()
  const inactive = company.status === 'Inactive'
  const facilities = company.facilities

  return (
    <Card>
      <CardHeader>
        <div className="flex items-baseline gap-2">
          <CardTitle>Facilities</CardTitle>
          <span className="tabular text-[13px] text-muted">{facilities.length}</span>
        </div>
        {inactive ? (
          <p className="text-[13px] text-muted">Inactive companies can’t receive new facilities</p>
        ) : (
          <Button
            size="sm"
            variant="secondary"
            onClick={() => navigate(`/facilities?new=1&companyId=${company.id}`)}
          >
            <Plus className="size-4" />
            New facility
          </Button>
        )}
      </CardHeader>
      {facilities.length === 0 ? (
        <div className="flex flex-col items-center gap-2 px-6 py-12 text-center">
          <Landmark className="size-7 text-faint" />
          <p className="text-sm font-medium text-ink">No facilities yet</p>
          <p className="max-w-sm text-sm text-muted">
            {inactive
              ? 'Reactivate the company to open its first facility.'
              : 'Open the first credit facility for this borrower.'}
          </p>
          {!inactive && (
            <Link
              to={`/facilities?new=1&companyId=${company.id}`}
              className="mt-1 text-sm font-medium text-accent hover:text-accent-hover"
            >
              Create a facility
            </Link>
          )}
        </div>
      ) : (
        <Table>
          <TableHeader>
            <TableRow className="hover:bg-transparent">
              <TableHead>Reference</TableHead>
              <TableHead>Type</TableHead>
              <TableHead className="text-right">Rate</TableHead>
              <TableHead className="text-right">Term</TableHead>
              <TableHead>Start</TableHead>
              <TableHead>Status</TableHead>
              <TableHead className="text-right">Outstanding</TableHead>
              <TableHead className="text-right">Commitment</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {facilities.map((f) => (
              <TableRow
                key={f.id}
                className="cursor-pointer"
                onClick={() => navigate(`/facilities/${f.id}`)}
              >
                <TableCell className="font-mono text-[13px] font-medium text-accent">
                  {f.reference}
                </TableCell>
                <TableCell>{repaymentTypeLabels[f.repaymentType] ?? f.repaymentType}</TableCell>
                <TableCell className="tabular text-right">{percent(f.annualInterestRate)}</TableCell>
                <TableCell className="tabular text-right">{f.termMonths} mo</TableCell>
                <TableCell className="whitespace-nowrap">{formatDate(f.startDate)}</TableCell>
                <TableCell>
                  <StatusBadge status={f.status} />
                </TableCell>
                <TableCell className="text-right">
                  <MoneyValue amount={f.outstandingPrincipal} currency={f.currency} />
                </TableCell>
                <TableCell className="text-right">
                  <MoneyValue amount={f.commitmentAmount} currency={f.currency} muted />
                </TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      )}
    </Card>
  )
}
