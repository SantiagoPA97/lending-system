import type { ReactNode } from 'react'
import { ChevronLeft, ChevronRight, Inbox } from 'lucide-react'
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from '@/components/ui/table'
import { SkeletonRows } from '@/components/ui/skeleton'
import { Button } from '@/components/ui/button'
import { cn } from '@/lib/utils'
import type { Paged } from '@/types/api'

export interface Column<T> {
  key: string
  header: ReactNode
  cell: (row: T) => ReactNode
  align?: 'left' | 'right'
  headerClassName?: string
  cellClassName?: string
}

export interface DataTableProps<T> {
  columns: Column<T>[]
  data: Paged<T> | undefined
  isLoading: boolean
  rowKey: (row: T) => string | number
  onRowClick?: (row: T) => void
  onPageChange?: (page: number) => void
  emptyTitle?: string
  emptyDescription?: string
  emptyAction?: ReactNode
}

export function DataTable<T>({
  columns,
  data,
  isLoading,
  rowKey,
  onRowClick,
  onPageChange,
  emptyTitle = 'Nothing here yet',
  emptyDescription,
  emptyAction,
}: DataTableProps<T>) {
  if (isLoading) return <SkeletonRows rows={6} />

  if (!data || data.items.length === 0) {
    return (
      <div className="flex flex-col items-center gap-2 px-6 py-14 text-center">
        <Inbox className="size-7 text-faint" />
        <p className="text-sm font-medium text-ink">{emptyTitle}</p>
        {emptyDescription && <p className="max-w-sm text-sm text-muted">{emptyDescription}</p>}
        {emptyAction && <div className="mt-2">{emptyAction}</div>}
      </div>
    )
  }

  const pageCount = Math.max(1, Math.ceil(data.total / data.pageSize))
  const from = (data.page - 1) * data.pageSize + 1
  const to = Math.min(data.page * data.pageSize, data.total)

  return (
    <div>
      <Table>
        <TableHeader>
          <TableRow className="hover:bg-transparent">
            {columns.map((col) => (
              <TableHead
                key={col.key}
                className={cn(col.align === 'right' && 'text-right', col.headerClassName)}
              >
                {col.header}
              </TableHead>
            ))}
          </TableRow>
        </TableHeader>
        <TableBody>
          {data.items.map((row) => (
            <TableRow
              key={rowKey(row)}
              onClick={onRowClick ? () => onRowClick(row) : undefined}
              className={cn(onRowClick && 'cursor-pointer')}
            >
              {columns.map((col) => (
                <TableCell
                  key={col.key}
                  className={cn(col.align === 'right' && 'text-right', col.cellClassName)}
                >
                  {col.cell(row)}
                </TableCell>
              ))}
            </TableRow>
          ))}
        </TableBody>
      </Table>
      {onPageChange && data.total > data.pageSize && (
        <div className="flex items-center justify-between border-t border-line px-5 py-2.5">
          <p className="tabular text-[13px] text-muted">
            {from}–{to} of {data.total}
          </p>
          <div className="flex items-center gap-1">
            <Button
              variant="ghost"
              size="sm"
              disabled={data.page <= 1}
              onClick={() => onPageChange(data.page - 1)}
              aria-label="Previous page"
            >
              <ChevronLeft className="size-4" />
            </Button>
            <span className="tabular px-1 text-[13px] text-body">
              {data.page} / {pageCount}
            </span>
            <Button
              variant="ghost"
              size="sm"
              disabled={data.page >= pageCount}
              onClick={() => onPageChange(data.page + 1)}
              aria-label="Next page"
            >
              <ChevronRight className="size-4" />
            </Button>
          </div>
        </div>
      )}
    </div>
  )
}
